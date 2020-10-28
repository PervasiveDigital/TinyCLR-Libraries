using System;
using System.Collections;

namespace GHIElectronics.TinyCLR.AppFramework.IoC {
    public class Container {
        private static int counter = 0;

        public Component Register(Type service, Type component) => this.Register(service, component, (++counter).ToString());

        public Component Register(Type service, ProviderFunc func) => this.Register(service, func, (++counter).ToString());

        public Component Register(Type service, Type component, string name) {
            if (!this.names.Contains(service)) {
                this.names[service] = name;
            }
            return new Component(this, name, component);
        }

        public Component Register(Type service, ProviderFunc func, string name) {
            if (!this.names.Contains(service)) {
                this.names[service] = name;
            }
            return new Component(this, name, func);
        }

        public object Resolve(Type service) {
            if (this.names.Contains(service)) {
                return this.Resolve((string)this.names[service]);
            }
            return null;
        }

        public object Resolve(string name) {
            if (this.services.Contains(name)) {
                return ((ProviderFunc)this.services[name])();
            }
            return null;
        }

        public void Forget(Type service) {
            if (this.names.Contains(service)) {
                this.Forget((string)this.names[service]);
                this.names.Remove(service);
            }
        }

        public void Forget(string name) {
            if (this.services.Contains(name)) {
                this.services.Remove(name);
            }
        }

        public void Install(params IContainerInstaller[] installers) {
            foreach (var installer in installers) {
                installer.Install(this);
            }
        }

        public class Component {
            internal Component(Container container, string name, Type type) {
                this.resolvers = new ArrayList();
                this.container = container;
                this.name = name;
                ProviderFunc func = () => {
                    var parameters = new ArrayList();
                    var parameterTypes = new ArrayList();
                    foreach (ProviderFunc resolver in this.resolvers) {
                        var value = resolver();
                        parameters.Add(value);
                        parameterTypes.Add(value.GetType());
                    }
                    var constructor = type.GetConstructor((Type[])parameterTypes.ToArray(typeof(Type)));
                    if (constructor == null) {
                        throw new InvalidOperationException("A constructor matching the dependency chain for component '" + name +
                                                                                                "' with type '" + type.FullName + "' could not be found.");
                    }
                    return constructor.Invoke(parameters.ToArray());
                };
                container.services[name] = func;
            }

            internal Component(Container container, string name, ProviderFunc providerFunc) {
                this.resolvers = new ArrayList();
                this.container = container;
                this.name = name;
                var func = providerFunc;
                container.services[name] = func;
            }

            public Component AsSingleton() {
                object value = null;
                var service = (ProviderFunc)this.container.services[this.name];
                ProviderFunc func = () => value ?? (value = service());
                this.container.services[this.name] = func;
                return this;
            }

            public Component WithComponent(string component) {
                ProviderFunc func = () => this.container.Resolve(component);
                this.resolvers.Add(func);
                return this;
            }

            public Component WithService(Type service) {
                ProviderFunc func = () => this.container.Resolve(service);
                this.resolvers.Add(func);
                return this;
            }

            public Component WithValue(object value) {
                ProviderFunc func = () => value;
                this.resolvers.Add(func);
                return this;
            }

            private readonly Container container;
            private readonly IList resolvers;
            private readonly string name;
        }

        public delegate object ProviderFunc();

        private readonly IDictionary services = new Hashtable();
        private readonly IDictionary names = new Hashtable();
    }
}

using System;

using GHIElectronics.TinyCLR.AppFramework.IoC;

namespace GHIElectronics.TinyCLR.AppFramework {
    public class Installer : IContainerInstaller {
        public void Install(Container container) => _ = container.Register(typeof(INodeEngine), typeof(NodeEngine)).AsSingleton();
    }
}

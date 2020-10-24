using System;
using System.Text;

namespace GHIElectronics.TinyCLR.AppFramework.Events {
    public class GenericAppEvent : OperationsEvent {
        public GenericAppEvent(string severity, string message) : base(severity) => this.message = message;

        public string message;
    }
}

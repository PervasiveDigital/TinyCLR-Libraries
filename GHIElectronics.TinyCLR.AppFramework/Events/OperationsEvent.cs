using System;
using System.Text;

namespace GHIElectronics.TinyCLR.AppFramework.Events {
    public class OperationsEvent {
        public OperationsEvent(string sev) => this.severity = sev;

        public string severity;
    }
}

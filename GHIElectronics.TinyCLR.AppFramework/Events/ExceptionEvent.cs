using System;
using System.Text;

namespace GHIElectronics.TinyCLR.AppFramework.Events {
    public class ExceptionEvent : OperationsEvent {
        public ExceptionEvent(string severity, Exception exc) : base(severity) {
            this.message = exc.Message;
            this.stack = exc.StackTrace;
        }

        public string message;
        public string stack;
    }
}

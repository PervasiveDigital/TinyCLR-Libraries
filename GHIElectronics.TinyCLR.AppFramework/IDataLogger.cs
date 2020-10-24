using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.AppFramework {
    public interface IDataLogger {
        void LogEvent(string eventClass, object eventData);
    }
}

using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public enum DriverState {
        Uninitialized,
        Error,
        Starting,
        Running,
        Stopping,
        Stopped
    }

    public interface IDriver {
        DriverState State { get; }

        DriverState Start();

        DriverState Stop();
    }
}

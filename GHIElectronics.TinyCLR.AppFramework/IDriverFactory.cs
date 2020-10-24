using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public interface IDriverFactory {
        IDriver[] CreateDrivers();
    }
}

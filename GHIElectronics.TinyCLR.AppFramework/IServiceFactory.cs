using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public interface IServiceFactory
    {
        IService[] CreateServices();
    }
}

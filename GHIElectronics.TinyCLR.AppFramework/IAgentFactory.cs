using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public interface IAgentFactory {
        IAgent[] CreateAgentsForState(string state);
    }
}

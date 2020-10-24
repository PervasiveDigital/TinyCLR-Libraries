using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public static class EngineStates {
        public const string Startup = "startup";
        public const string Shutdown = "shutdown";
    }

    public interface INodeEngine {
        void Initialize();

        void ScheduleNextRun(IAgent agent, DateTime runAt);

        void Run();

        void Stop();

        void NavigateToEngineState(string newState);

        IDriver FindDriver(Type driverType);
    }
}

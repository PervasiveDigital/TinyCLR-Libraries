using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public static class EngineStates {
        public const string Startup = "startup";
        public const string Shutdown = "shutdown";
    }

    public interface INodeEngine {
        void ScheduleNextRun(IAgent agent, DateTime runAt);

        void Run();

        void Stop();

        void SetEngineState(string engineState);

        IDriver FindDriver(Type driverType);
    }
}

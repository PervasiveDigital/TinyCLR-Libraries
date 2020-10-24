using System;

namespace GHIElectronics.TinyCLR.AppFramework {
    public interface IAgent {
        /// <summary>
        /// Initialize internal start and prepare for process calls
        /// </summary>
        DateTime Start();

        /// <summary>
        /// Perform one cycle of processing. Return a value representing when this agent wants to be processed again
        /// </summary>
        /// <returns>Time this agent next needs processing</returns>
        DateTime Process(DateTime now);

        /// <summary>
        /// Clean up any resources created in 'start'
        /// </summary>
        void Stop();
    }
}

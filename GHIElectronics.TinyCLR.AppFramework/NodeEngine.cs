using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using GHIElectronics.TinyCLR.AppFramework;
using GHIElectronics.TinyCLR.AppFramework.Events;
using GHIElectronics.TinyCLR.AppFramework.IoC;

namespace GHIElectronics.TinyCLR.AppFramework.Core {
    public class NodeEngine : INodeEngine {
        private IDataLogger logger;
        private IDriver[] drivers;
        private IService[] services;
        private IAgent[] agents;
        private Hashtable allAgents = new Hashtable();
        private ArrayList queue = new ArrayList();
        private ArrayList mergeList = new ArrayList();
        private AutoResetEvent scheduleChangedEvent = new AutoResetEvent(false);
        private bool fShutdown = false;
        private Thread thread;

        public NodeEngine() {
        }

        public IDriver FindDriver(Type driverType) {
            foreach (var driver in this.drivers) {
                if (driver.GetType() == driverType) {
                    return driver;
                }
            }
            return null;
        }

        public void Initialize() {
            var driverFactory = (IDriverFactory)DiContainer.Instance.Resolve(typeof(IDriverFactory));
            var newDrivers = driverFactory.CreateDrivers();

            // Some drivers have to wait for other drivers, so keep looping until they are all initialized
            if (newDrivers != null) {
                var haveUninitializedDrivers = false;
                do {
                    haveUninitializedDrivers = false;
                    foreach (var driver in newDrivers) {
                        try {
                            if (driver.State == DriverState.Uninitialized) {
                                var state = driver.Start();
                                // If this driver is still uninit'd, then it must be waiting for some
                                //   other driver to come online - set the flag so that we do another pass.
                                if (state == DriverState.Uninitialized) {
                                    haveUninitializedDrivers = true;
                                }
                            }
                        }
                        catch (Exception exDriverStart) {
                            Debug.WriteLine("Exception during agent start : " + exDriverStart);
                            // can't log it - services aren't started yet and the logger is a service
                        }
                    }
                } while (haveUninitializedDrivers);
            }
            this.drivers = newDrivers;

            var serviceFactory = (IServiceFactory)DiContainer.Instance.Resolve(typeof(IServiceFactory));
            if (serviceFactory != null) {
                var newServices = serviceFactory.CreateServices();
                if (newServices != null) {
                    var haveUninitializedServices = false;
                    do {
                        haveUninitializedServices = false;
                        foreach (var service in newServices) {
                            try {
                                if (service.State == DriverState.Uninitialized) {
                                    var state = service.Start();
                                    // If this service is still uninit'd, then it must be waiting for some
                                    //   other service to come online - set the flag so that we do another pass.
                                    if (state == DriverState.Uninitialized) {
                                        haveUninitializedServices = true;
                                    }
                                }
                            }
                            catch (Exception exDriverStart) {
                                Debug.WriteLine("Exception during service start : " + exDriverStart);
                                // can't log it - services aren't fully started yet and the logger is a service
                            }
                        }
                    } while (haveUninitializedServices);
                }
                this.services = newServices;
            }

            var agentFactory = (IAgentFactory)DiContainer.Instance.Resolve(typeof(IAgentFactory));
            this.agents = agentFactory.CreateAgentsForState(EngineStates.Startup);

            if (this.agents != null) {
                foreach (var agent in this.agents) {
                    try {
                        var firstRun = agent.Start();
                        if (firstRun != DateTime.MaxValue)
                            this.ScheduleNextRun(agent, firstRun);
                    }
                    catch (Exception exAgentStart) {
                        Debug.WriteLine("Exception during agent start : " + exAgentStart);
                        this.Logger?.LogEvent(EventClass.OperationalEvent, new ExceptionEvent(EventSeverity.Error, exAgentStart));
                    }
                }
            }

            this.Logger?.LogEvent(EventClass.OperationalEvent, new GenericAppEvent(EventSeverity.Info, "App Startup : Node engine started"));
        }

        public void ScheduleNextRun(IAgent agent, DateTime runAt) {
            this.ScheduleNextRun(this.queue, agent, runAt);
            _ = this.scheduleChangedEvent.Set();
        }

        private void ScheduleNextRun(ArrayList list, IAgent agent, DateTime runAt) {
            ScheduleItem item = null;

            // dequeue the agent if it is already in the schedule
            for (var i = 0; i < list.Count; ++i) {
                if (((ScheduleItem)list[i]).Target == agent) {
                    item = (ScheduleItem)list[i];
                    list.RemoveAt(i);
                    item.RunAt = runAt;
                    break;
                }
            }

            if (item == null) {
                item = new ScheduleItem(agent, runAt);
            }

            this.ScheduleNextRun(list, item);
        }

        private void ScheduleNextRun(ArrayList list, ScheduleItem item) {
            var fAdded = false;
            for (var i = 0; i < list.Count; ++i) {
                if (((ScheduleItem)list[i]).RunAt > item.RunAt) {
                    list.Insert(i, item);
                    fAdded = true;
                    break;
                }
            }
            if (!fAdded)
                list.Add(item);
        }

        private void Merge() {
            foreach (var item in this.mergeList) {
                this.ScheduleNextRun(this.queue, (ScheduleItem)item);
            }
            this.mergeList.Clear();
        }

        public void Run() {
            this.thread = new Thread(this.Run_Internal);
            this.thread.Start();
        }

        private void Run_Internal() {
            while (true) {
                if (this.queue.Count == 0) {
                    // The queue is empty - wait for something to get inserted
                    this.scheduleChangedEvent.WaitOne();
                }
                else {
                    var nextTime = ((ScheduleItem)this.queue[0]).RunAt;
                    var delay = TimeSpan.FromTicks(nextTime.Ticks - DateTime.UtcNow.Ticks).Milliseconds;
                    if (delay > 0)
                        this.scheduleChangedEvent.WaitOne(delay, false);
                }

                if (this.fShutdown)
                    break;

                // Re-scheduled items get put in a side list (the mergeList) so that re-insertions with an immediate
                //   execution time don't monopolize the schedule. All 'ready' agents get run before re-scheduled agents
                //   even if the re-scheduled time would have passed.
                do {
                    var now = DateTime.UtcNow;

                    if (this.queue.Count == 0)
                        break;

                    // Examine the head item
                    var item = (ScheduleItem)this.queue[0];

                    if (item.RunAt > now)
                        break; // we need to wait a bit

                    try {
                        // Dequeue the head item
                        this.queue.RemoveAt(0);
                        // Process it
                        var runAt = item.Target.Process(now);
                        // Re-schedule it in the merge list
                        if (runAt != DateTime.MaxValue)
                            this.ScheduleNextRun(this.mergeList, item.Target, runAt);
                    }
                    catch (Exception exRun) {
                        Debug.WriteLine("An exception ocurred while attempting to run an agent : " + exRun.ToString());
                        this.Logger?.LogEvent(EventClass.OperationalEvent, new ExceptionEvent(EventSeverity.Error, exRun));

                        // Attempt to recover the agent
                        try {
                            item.Target.Stop();
                            item.RunAt = item.Target.Start();
                            if (item.RunAt != DateTime.MaxValue)
                                this.ScheduleNextRun(this.mergeList, item);
                        }
                        catch (Exception ex) {
                            Debug.WriteLine("Exception while trying to recover a faulted agent : " + ex.ToString());
                            this.Logger?.LogEvent(EventClass.OperationalEvent, new ExceptionEvent(EventSeverity.Error, ex));
                        }
                    }

                } while (!this.fShutdown);

                if (this.mergeList.Count > 0)
                    this.Merge();
            }

            // Execute shutdown code
            foreach (var agent in this.agents) {
                try {
                    agent.Stop();
                }
                catch (Exception exAgent) {
                    Debug.WriteLine("Exception during agent stop : " + exAgent);
                    this.Logger?.LogEvent(EventClass.OperationalEvent, new ExceptionEvent(EventSeverity.Error, exAgent));
                }
            }

            foreach (var driver in this.drivers) {
                try {
                    driver.Stop();
                }
                catch (Exception exDriver) {
                    Debug.WriteLine("Exception during agent start : " + exDriver);
                    this.Logger?.LogEvent(EventClass.OperationalEvent, new ExceptionEvent(EventSeverity.Error, exDriver));
                }
            }
        }

        public void Stop() {
            this.fShutdown = true;
            this.scheduleChangedEvent.Set();
        }

        public void NavigateToEngineState(string newState) =>
            //TODO: send a message to the run process that it shoudld shut down running agents and reconfigure to the new state. Use a synthetic/placeholder agent, defined in .Core, for this purpose
            throw new NotImplementedException();

        private IDataLogger Logger {
            get {
                if (this.logger == null) {
                    this.logger = (IDataLogger)DiContainer.Instance.Resolve(typeof(IDataLogger));
                }
                return this.logger;
            }
        }

        private class ScheduleItem {
            public ScheduleItem(IAgent target, DateTime runAt) {
                this.Target = target;
                this.RunAt = runAt;
            }

            public DateTime RunAt { get; set; }
            public IAgent Target { get; private set; }
        }
    }
}

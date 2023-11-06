using Communication;
using log4net;
using log4net.Config;
using Models;
using SensorUnits.TimingUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using DisplayUnit;
using Models.Config;
using SensorUnits.StartLightUnit;
using System.Text;
using Newtonsoft.Json;

namespace RaceManagement
{
    public class RaceManager
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ITimingUnit timing;
        private List<IDisplayUnit> displays = new List<IDisplayUnit>();
        private IRaceTracker tracker;
        private IStartLightUnit startLight;
        private CancellationTokenSource source = new CancellationTokenSource();
        private DateTime RaceStartTime = DateTime.Now;

        public Task<RaceSummary> CombinedTasks { get; private set; }

        /// <summary>
        /// State is produced by a running RaceTracker. Before the first Start call there is no meanigful state
        /// </summary>
        public bool HasState => CombinedTasks != null;

        /// <summary>
        /// Make a new RaceManager. This does not run anything yet
        /// You can (re)start the manager with any of the Start methods
        /// </summary>
        public RaceManager()
        {
        }

        /// <summary>
        /// Attempt to start the race manager from a locally stored config, for example for testing or fixed setups
        /// </summary>
        public void AttemptStartFromLocalConfig()
        {
            if (File.Exists("SimulationConfig.json"))
            {
                Log.Info("Starting simulation from SimulationConfig.json");
                using (FileStream stream = new FileStream("SimulationConfig.json", FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, false))
                    {
                        string jsonData = reader.ReadToEnd();

                        Start(JsonConvert.DeserializeObject<SimulationData>(jsonData), 1000, null);
                    }
                }
            }
            else if (File.Exists("HardwareConfig.json"))
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Simulates all the system events that happen during a race. Used to test interaction with ui
        /// </summary>
        /// <param name="simulationData"></param>
        /// <param name="delayMilliseconds">How many milliseconds to wait before starting simulation</param>
        /// <param name="overrideEventDelayMilliseconds">how many milliseconds to wait in between events, if not provided use value in events</param>
        public void Start(SimulationData simulationData, int delayMilliseconds, int? overrideEventDelayMilliseconds)
        {
            Stop();

            XmlConfigurator.Configure(new FileInfo("logConfig.xml"));

            SimulationTimingUnit timingUnit = new SimulationTimingUnit(simulationData);
            displays.Add(timingUnit);
            timing = timingUnit;

            tracker = new RaceTracker(timing, new TrackerConfig { StartTimingGateId = simulationData.StartGateId, EndTimingGateId = simulationData.EndGateId }, simulationData.Riders);
            HookEvents(tracker);

            Task<RaceSummary> trackTask = tracker.Run(source.Token);

            var timeTask = timingUnit.Run(source.Token, delayMilliseconds, overrideEventDelayMilliseconds);

            CombinedTasks = Task.Run(() =>
            {
                timeTask.Wait();
                source.Cancel();
                return trackTask.Result;
            });
        }

        /// <summary>
        /// Replays a race from a json that contains all the race events
        /// </summary>
        /// <param name="replayData"></param>
        public void Start(RaceSummary replayData)
        {
            Stop();

            XmlConfigurator.Configure(new FileInfo("logConfig.xml"));
            
            //we need the simulation specific methods in the constructor
            ReplayTimingUnit timingUnit = new ReplayTimingUnit(replayData);
            displays.Add(timingUnit);
            timing = timingUnit;

            timingUnit.Initialize();

            tracker = new RaceTracker(timing, replayData.Config, replayData.Riders);
            HookEvents(tracker);

            Task<RaceSummary> trackTask = tracker.Run(source.Token);

            var timeTask = timingUnit.Run(source.Token);

            CombinedTasks = Task.Run(() =>
            {
                timeTask.Wait();
                source.Cancel();
                return trackTask.Result;
            });
        }

        /// <summary>
        /// Manage a race from real sensor data
        /// </summary>
        public void Start(RaceConfig config, List<Rider> riders)
        {
            Stop();

            CommunicationManager CommunicationManager = new CommunicationManager(source.Token);

            SerialTimingUnit timer = new SerialTimingUnit(CommunicationManager.GetCommunicationDevice(config.TimingUnitId), "timerUnit", source.Token, config.StartTimingGateId, config.EndTimingGateId);
            timing = timer;
            displays.Add(timer);

            startLight = new BLEStartLightUnit(CommunicationManager.GetCommunicationDevice(config.StartLightUnitId), "lightUnit", source.Token);

            startLight.SetStartLightColor(StartLightColor.YELLOW);

            tracker = new RaceTracker(timing, config.ExtractTrackerConfig(), riders);
            HookEvents(tracker);

            CombinedTasks = tracker.Run(source.Token);
        }

        /// <summary>
        /// Test instrumentation
        /// </summary>
        /// <param name="tracker">Race tracker from unit test</param>
        /// <param name="displays">Display unit from unit test</param>
        public void Start(IRaceTracker tracker, List<IDisplayUnit> displays)
        {
            Stop();
            this.displays = displays;

            this.tracker = tracker;
            HookEvents(tracker);

            CombinedTasks = tracker.Run(source.Token);
        }

        private void HookEvents(IRaceTracker tracker)
        {
            tracker.OnRiderMatched += HandleFinish;

            tracker.OnRiderMatched += (o, e) => Log.Info($"Rider {e.Lap.Rider.Name} finished with a lap time of {e.Lap.GetLapTime()} microseconds");
            tracker.OnRiderDNF += (o, e) => Log.Info($"Rider {e.Lap.Rider.Name} did not finish");
            tracker.OnRiderWaiting += (o, e) => Log.Info($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Log.Info("Start box is empty");

            tracker.OnRiderWaiting += (o, e) => startLight?.SetStartLightColor(StartLightColor.GREEN);
            tracker.OnStartEmpty += (o, e) => startLight?.SetStartLightColor(StartLightColor.YELLOW);
        }

        private void HandleFinish(object o, LapCompletedEventArgs e)
        {
            foreach (var display in displays)
            {
                display.SetDisplayTime((int)(e.Lap.GetLapTime() / 1000));
            }
        }

        public RaceSummary Stop()
        {
            source.Cancel();
            RaceSummary summary = CombinedTasks?.Result;
            source = new CancellationTokenSource();
            displays.Clear();

            return summary;
        }

        /// <summary>
        /// Gets a summary of the current state of the race
        /// </summary>
        public (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) GetState => tracker.GetState;

        /// <summary>
        /// Returns all lap times driven so far
        /// </summary>
        /// <param name="start">the first lap to be returned. Default value (0) returns all laps driven</param>
        /// <returns></returns>
        public List<Lap> GetLapTimes(int start = 0)
        {
            return tracker.Laps.Skip(start).ToList();
        }

        /// <summary>
        /// Returns the best lap per rider, sorted by lap time
        /// </summary>
        /// <returns></returns>
        public List<Lap> GetBestLaps()
        {
            Dictionary<string, Lap> lapsByRider = new Dictionary<string, Lap>();

            foreach(Lap lap in tracker.Laps)
            {
                if (!lapsByRider.ContainsKey(lap.Rider.Name))
                {
                    lapsByRider.Add(lap.Rider.Name, lap);
                }
                else if (lap.CompareTo(lapsByRider[lap.Rider.Name]) <= 0)
                {
                    lapsByRider[lap.Rider.Name] = lap;
                }  
            }

            List<Lap> fastestLaps = lapsByRider.Select(p => p.Value).ToList();
            fastestLaps.Sort();

            return fastestLaps;
        }

        public List<Rider> GetKnownRiders()
        {
            return tracker.Riders;
        }

        public void AddEvent<T>(T manualEvent) where T : EventArgs
        {
            tracker?.AddEvent(manualEvent);
        }

        public void TriggerTimingEvent(int gateId)
        {
            double millis = DateTime.Now.Subtract(RaceStartTime).TotalMilliseconds;
            long micros = (long)(millis * 1000);
            Log.Info($"Manually triggered timing event from gate {gateId} at timestamp {micros} at {millis} after start");
            tracker?.AddEvent(new TimingTriggeredEventArgs(micros, gateId));
        }

        public void RemoveRider(Guid id)
        {
            tracker?.RemoveRider(id);
        }

        public void AddRider(Rider rider)
        {
            tracker?.AddRider(rider);
        }

        public void ChangePosition(Guid riderId, int targetPosition)
        {
            tracker?.ChangePosition(riderId, targetPosition);
        }

        public Rider GetRiderById(Guid id) => tracker?.GetRiderById(id);
    }
}

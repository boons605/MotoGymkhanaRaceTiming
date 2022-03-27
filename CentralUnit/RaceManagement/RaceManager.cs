using Communication;
using log4net;
using log4net.Config;
using Models;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using DisplayUnit;
using Models.Config;
using StartLightUnit;

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
        private IRiderIdUnit startGate, endGate;
        private IRaceTracker tracker;
        private IStartLightUnit startLight;
        private CancellationTokenSource source = new CancellationTokenSource();

        public Task CombinedTasks { get; private set; }

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
        /// Simulates a race from a json that contains all the race events
        /// </summary>
        /// <param name="simulationData"></param>
        public void Start(RaceSummary simulationData)
        {
            Stop();

            XmlConfigurator.Configure(new FileInfo("logConfig.xml"));
            
            //we need the simulation specific methods in the constructor
            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(simulationData.StartId, simulationData);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(simulationData.EndId, simulationData);
            SimulationTimingUnit timingUnit = new SimulationTimingUnit(simulationData);
            displays.Add(timingUnit);
            startGate = startId;
            endGate = endId;
            timing = timingUnit;

            startId.Initialize();
            endId.Initialize();
            timingUnit.Initialize();

            tracker = new RaceTracker(timing, startId, endId, simulationData.Config, simulationData.Riders);
            HookEvents(tracker);

            var trackTask = tracker.Run(source.Token);

            var startTask = startId.Run(source.Token);
            var endTask = endId.Run(source.Token);
            var timeTask = timingUnit.Run(source.Token);

            //will complete when all units run out of events to simulate
            var unitsTask = Task.WhenAll(startTask, endTask, timeTask);

            CombinedTasks = Task.Run(() =>
            {
                unitsTask.Wait();
                source.Cancel();
                trackTask.Wait();
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
            BLERiderIdUnit realStartId = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(config.StartIdUnitId), "startUnit", config.StartIdRange, source.Token);
            endGate = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(config.EndIdUnitId), "finishUnit", config.EndIdRange, source.Token);

            startGate = realStartId;
            startLight = realStartId;

            startGate?.ClearKnownRiders();
            endGate?.ClearKnownRiders();

            startLight.SetStartLightColor(StartLightColor.YELLOW);

            startGate.AddKnownRiders(riders);

            tracker = new RaceTracker(timing, startGate, endGate, config.ExtractTrackerConfig(), riders);
            HookEvents(tracker);

            CombinedTasks = tracker.Run(source.Token);
        }

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
            tracker.OnRiderFinished += HandleFinish;

            tracker.OnRiderFinished += (o, e) => Log.Info($"Rider {e.Lap.Rider.Name} finished with a lap time of {e.Lap.GetLapTime()} microseconds");
            tracker.OnRiderDNF += (o, e) => Log.Info($"Rider {e.Lap.Rider.Name} did not finish since {(e.Lap.End as UnitDNFEvent).OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Log.Info($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Log.Info("Start box is empty");

            tracker.OnRiderWaiting += (o, e) => startLight?.SetStartLightColor(StartLightColor.GREEN);
            tracker.OnStartEmpty += (o, e) => startLight?.SetStartLightColor(StartLightColor.YELLOW);
        }

        private void HandleFinish(object o, FinishedRiderEventArgs e)
        {
            foreach (var display in displays)
            {
                display.SetDisplayTime((int)(e.Lap.GetLapTime() / 1000));
            }
        }

        public void Stop()
        {
            startGate?.ClearKnownRiders();
            endGate?.ClearKnownRiders();
            //give the units time to process the commaands
            Thread.Sleep(1000);

            source.Cancel();
            CombinedTasks?.Wait();
            source = new CancellationTokenSource();
            displays.Clear();
        }

        /// <summary>
        /// Gets a summary of the current state of the race
        /// </summary>
        public (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState => tracker.GetState;

        /// <summary>
        /// Get the beacons currently detected by the start and end id units
        /// </summary>
        public (List<Beacon> startBeacons, List<Beacon> endBeacons) GetBeacons => (startGate?.Beacons ?? new List<Beacon>(), endGate?.Beacons ?? new List<Beacon>());

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
        public void AddEvent<T>(T manualEvent) where T : ManualEventArgs
        {
            tracker?.AddEvent(manualEvent);
        }

        public void RemoveRider(string name)
        {
            tracker?.RemoveRider(name);
        }

        public void AddRider(Rider rider)
        {
            tracker?.AddRider(rider);
        }
    }
}

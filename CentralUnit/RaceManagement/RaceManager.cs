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
        private CancellationTokenSource source = new CancellationTokenSource();

        //DNF of Finished
        private List<Lap> laps = new List<Lap>();

        public Task CombinedTasks { get; private set; }

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
        public void Start(string simulationData)
        {
            Stop();

            XmlConfigurator.Configure(new FileInfo("logConfig.xml"));

            RaceSummary race;
            using (Stream input = new FileStream(simulationData, FileMode.Open))
                race = RaceSummary.ReadSummary(input);
            
            //we need the simulation specific methods in the constructor
            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(race.StartId, race);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(race.EndId, race);
            SimulationTimingUnit timingUnit = new SimulationTimingUnit(race);
            displays.Add(timingUnit);
            startGate = startId;
            endGate = endId;
            timing = timingUnit;

            startId.Initialize();
            endId.Initialize();
            timingUnit.Initialize();

            tracker = new RaceTracker(timing, startId, endId, timingUnit.StartId, timingUnit.EndId, race.Riders);
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
        /// <param name="timingUnitId"></param>
        /// <param name="startIdUnitId"></param>
        /// <param name="endIdUnitId"></param>
        /// <param name="riders">These rider will be added to the start id unit and are elgibile to start right away</param>
        /// <param name="endTimingGateId">The id reported for the end timing gate by the timiing unit</param>
        /// <param name="startTimingGateId">The id reported for the start timing gate by the timiing unit</param>
        public void Start(string timingUnitId, string startIdUnitId, string endIdUnitId, int startTimingGateId, int endTimingGateId, List<Rider> riders)
        {
            Stop();

            CommunicationManager CommunicationManager = new CommunicationManager(source.Token);

            SerialTimingUnit timer = new SerialTimingUnit(CommunicationManager.GetCommunicationDevice(timingUnitId), "timerUnit", source.Token, startTimingGateId, endTimingGateId);
            timing = timer;
            displays.Add(timer);
            startGate = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(startIdUnitId), "startUnit", 2.0, source.Token);
            endGate = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(endIdUnitId), "finishUnit", 2.0, source.Token);
            
            startGate.AddKnownRiders(riders);
            endGate.AddKnownRiders(riders);

            tracker = new RaceTracker(timing, startGate, endGate, timing.StartId, timing.EndId, riders);
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
            tracker.OnRiderDNF += HandleDNF;

            tracker.OnRiderFinished += (o, e) => Log.Info($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Log.Info($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Log.Info($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Log.Info("Start box is empty");
        }

        private void HandleFinish(object o, FinishedRiderEventArgs e)
        {
            laps.Add(new Lap(e.Finish));

            foreach (var display in displays)
            {
                display.SetDisplayTime((int)(e.Finish.LapTime / 1000));
            }
        }

        private void HandleDNF(object o, DNFRiderEventArgs e)
        {
            laps.Add(new Lap(e.Dnf));
        }

        public void Stop()
        {
            source.Cancel();
            CombinedTasks?.Wait();
            source = new CancellationTokenSource();
            displays.Clear();
        }

        public (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState => tracker.GetState;

        /// <summary>
        /// Returns all lap times driven so far
        /// </summary>
        /// <param name="start">the first lap to be returned. Default value (0) returns all laps driven</param>
        /// <returns></returns>
        public List<Lap> GetLapTimes(int start = 0)
        {
            return laps.Skip(start).ToList();
        }

        /// <summary>
        /// Returns the best lap per rider, sorted by lap time
        /// </summary>
        /// <returns></returns>
        public List<Lap> GetBestLaps()
        {
            Dictionary<string, Lap> lapsByRider = new Dictionary<string, Lap>();

            foreach(Lap lap in laps)
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

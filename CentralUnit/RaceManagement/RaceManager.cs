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

namespace RaceManagement
{
    public class RaceManager
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ITimingUnit timing;
        private readonly IRiderIdUnit startGate, endGate;
        private RaceTracker tracker;
        private CancellationTokenSource source = new CancellationTokenSource();

        //DNF of Finished
        private List<Lap> laps = new List<Lap>();

        public Task combinedTasks { get; private set; }

        /// <summary>
        /// Simulates a race from a json that contains all the race events
        /// </summary>
        /// <param name="simulationData"></param>
        public RaceManager(string simulationData)
        {
            XmlConfigurator.Configure(new FileInfo("logConfig.xml"));

            RaceSummary race;
            using (Stream input = new FileStream(simulationData, FileMode.Open))
                race = RaceSummary.ReadSummary(input);
            
            //we need the simulation specific methods in the constructor
            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(true, race);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(false, race);
            SimulationTimingUnit timingUnit = new SimulationTimingUnit(race);
            startGate = startId;
            endGate = endId;
            timing = timingUnit;

            startId.Initialize();
            endId.Initialize();
            timingUnit.Initialize();

            tracker = new RaceTracker(timing, startId, endId, timingUnit.StartId, timingUnit.EndId);
            HookEvents(tracker);

            var trackTask = tracker.Run(source.Token);

            var startTask = startId.Run(source.Token);
            var endTask = endId.Run(source.Token);
            var timeTask = timingUnit.Run(source.Token);

            //will complete when all units run out of events to simulate
            var unitsTask = Task.WhenAll(startTask, endTask, timeTask);

            combinedTasks = Task.Run(() =>
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
        /// <param name="knownRiders"></param>
        public RaceManager(string timingUnitId, string startIdUnitId, string endIdUnitId, string knownRiders, int startTimingId, int endTimingId)
        {
            CommunicationManager CommunicationManager = new CommunicationManager(source.Token);

            List<Rider> riders;

            try
            {
                riders = ReadRidersFile(knownRiders);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not parse riders file {knownRiders}", ex);
                throw;
            }

            timing = new SerialTimingUnit(CommunicationManager.GetCommunicationDevice(timingUnitId), "timerUnit", source.Token, startTimingId, endTimingId);
            startGate = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(startIdUnitId), "startUnit", 2.0, source.Token);
            endGate = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(endIdUnitId), "finishUnit", 2.0, source.Token);
            
            startGate.AddKnownRiders(riders);
            endGate.AddKnownRiders(riders);

            tracker = new RaceTracker(timing, startGate, endGate, timing.StartId, timing.EndId);
            HookEvents(tracker);

            combinedTasks = tracker.Run(source.Token);
        }

        private void HookEvents(RaceTracker tracker)
        {
            tracker.OnRiderFinished += HandleFinish;

            tracker.OnRiderFinished += (o, e) => Log.Info($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Log.Info($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Log.Info($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Log.Info("Start box is empty");
        }

        private void HandleFinish(object o, FinishedRiderEventArgs e)
        {
            laps.Add(new Lap(e.Finish));
        }

        private void HandleDNF(object o, DNFRiderEventArgs e)
        {
            laps.Add(new Lap(e.Dnf));
        }

        private static List<Rider> ReadRidersFile(string file)
        {
            string[] lines = File.ReadAllLines(file);
            List<Rider> riderList = new List<Rider>();
            foreach (string line in lines)
            {
                riderList.Add(BasicRiderCSVHelper.ParseRiderLine(line));
            }

            return riderList;
        }

        public void Stop()
        {
            source.Cancel();
        }

        public (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState => tracker.GetState;

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
                else if (lap <= lapsByRider[lap.Rider.Name])
                {
                    lapsByRider[lap.Rider.Name] = lap;
                }  
            }

            List<Lap> fastestLaps = lapsByRider.Select(p => p.Value).ToList();
            fastestLaps.Sort();

            return fastestLaps;
        }
    }
}

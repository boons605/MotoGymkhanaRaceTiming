using Communication;
using log4net;
using log4net.Config;
using Models;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public Task combinedTasks { get; private set; }
        private CancellationTokenSource source;

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

            tracker.OnRiderFinished += (o, e) => Console.WriteLine($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Console.WriteLine($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Console.WriteLine($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Console.WriteLine("Start box is empty");

            source = new CancellationTokenSource();

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
            CancellationTokenSource source = new CancellationTokenSource();

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

            combinedTasks = tracker.Run(source.Token);
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
    }
}

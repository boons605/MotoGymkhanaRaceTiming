using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Models;
using RaceManagement;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;

namespace CLI
{
    /// <summary>
    /// This command allows you to simulate a race from a serialized RaceSummary
    /// </summary>
    public class SimulationCommand : CommandBase
    {
        [Required]
        [Argument(0, Description = "File path to a json that contains a serialzed RaceSummary")]
        public string SummaryFile { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            RaceSummary race;
            using (Stream input = new FileStream(SummaryFile, FileMode.Open))
                race = RaceSummary.ReadSummary(input);

            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(race.StartId, race);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(race.EndId, race);
            SimulationTimingUnit timing = new SimulationTimingUnit(race);

            startId.Initialize();
            endId.Initialize();
            timing.Initialize();

            RaceTracker tracker = new RaceTracker(timing, startId, endId, timing.StartId, timing.EndId, race.Riders);

            tracker.OnRiderFinished += (o, e) => Console.WriteLine($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Console.WriteLine($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Console.WriteLine($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Console.WriteLine("Start box is empty");

            CancellationTokenSource source = new CancellationTokenSource();

            var trackTask = tracker.Run(source.Token);

            var startTask = startId.Run(source.Token);
            var endTask = endId.Run(source.Token);
            var timeTask = timing.Run(source.Token);

            var unitsTask = Task.WhenAll(startTask, endTask, timeTask);

            //we can't really use .Wait() here since that would block the thread and we need this thread for the console output
            while (!unitsTask.IsCompleted)
            {
                Thread.Yield();
            }

            source.Cancel();
            trackTask.Wait();

            return 0;
        }
    }
}

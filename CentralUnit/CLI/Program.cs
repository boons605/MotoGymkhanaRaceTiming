// <copyright file="Program.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

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
    public class Program
    {
        public static void Main(string[] args) => CommandLineApplication.Execute<RootCommand>(args);
    }

    /// <summary>
    /// Root command of the tool. For different modes of operation, make more command classes and put them in [SubCommand] attributes
    /// </summary>
    [Command("mgk")]
    [Subcommand(typeof(SimulationCommand))]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public class RootCommand : CommandBase
    {
        /// <summary>
        /// All children of CommandBase must override this to implement their own behavior
        /// </summary>
        /// <param name="app">Class that contains all kinds of information about the way the program was started. Usually best not to touch</param>
        /// <returns>the desired exit code of the tool</returns>
        protected override int OnExecute(CommandLineApplication app)
        {
            Console.WriteLine("This is the root command, it does not do anything");
            app.ShowHelp();
            return 1;
        }

        /// <summary>
        /// Returns the Informational version of this assembly
        /// </summary>
        /// <returns></returns>
        private static string GetVersion()
            => typeof(RootCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }

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

            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(true, race);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(false, race);
            SimulationTimingUnit timing = new SimulationTimingUnit(race);

            startId.Initialize();
            endId.Initialize();
            timing.Initialize();

            RaceTracker tracker = new RaceTracker(timing, startId, endId, timing.StartId, timing.EndId);

            tracker.OnRiderFinished += (o, e) => Console.WriteLine($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Console.WriteLine($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Console.WriteLine($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Console.WriteLine("Start box is empty");

            CancellationTokenSource source = new CancellationTokenSource();

            var trackTask = tracker.Run(source.Token);

            var startTask = startId.Run(source.Token);
            var endTask = endId.Run(source.Token);
            var timeTask = timing.Run(source.Token);

            var unitsTask = Task.WhenAll(startTask, endTask, timeTask);

            //we can't really use .Wait() here since that would block the thread and we need this thread for the console output
            while(!unitsTask.IsCompleted)
            {
                Thread.Yield();
            }

            source.Cancel();
            trackTask.Wait();

            return 0;
        }
    }


    /// <summary>
    /// This class provides base behavior (such as the help option) for all following commands
    /// </summary>
    [HelpOption("--help")]
    public abstract class CommandBase
    {
        /// <summary>
        /// Override this to implement command behavior
        /// </summary>
        /// <param name="app">Class that contains all kinds of information about the way the program was started. Usually best not to touch</param>
        /// <returns>the desired exit code of the tool</returns>
        protected abstract int OnExecute(CommandLineApplication app);
    }
}

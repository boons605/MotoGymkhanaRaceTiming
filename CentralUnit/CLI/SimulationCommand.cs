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
            RaceManager manager = new RaceManager();
            manager.Start(SummaryFile);

            manager.CombinedTasks.Wait();

            return 0;
        }
    }
}

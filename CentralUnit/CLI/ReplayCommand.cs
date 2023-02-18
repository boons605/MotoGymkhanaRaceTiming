using System.ComponentModel.DataAnnotations;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Models;
using RaceManagement;

namespace CLI
{
    /// <summary>
    /// This command allows you to simulate a race from a serialized RaceSummary
    /// </summary>
    public class ReplayCommand : CommandBase
    {
        [Required]
        [Argument(0, Description = "File path to a json that contains a serialzed RaceSummary")]
        public string SummaryFile { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            RaceManager manager = new RaceManager();
            RaceSummary summary;

            using (Stream reader = new FileStream(SummaryFile, FileMode.Open))
            {
                summary = RaceSummary.ReadSummary(reader);
            }


            manager.Start(summary);

            manager.CombinedTasks.Wait();

            return 0;
        }
    }
}

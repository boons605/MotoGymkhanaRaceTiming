using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using WebPusher.WebInterfaces;

namespace WebPusher
{
    class Program
    {
        public static void Main(string[] args) => CommandLineApplication.Execute<RootCommand>(args);

        /// <summary>
        /// Root command of the tool. For different modes of operation, make more command classes and put them in [SubCommand] attributes
        /// </summary>
        [Command("webpush")]
        [Subcommand(typeof(RunCommand))]
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

        public class RunCommand : CommandBase
        {
            [Option("Base url for the gymkhana website defaults to https://timing.motogymkhana.nl")]
            public string WebApiUrl { get; set; } = "https://timing.motogymkhana.nl";

            [Option("Base url for the timing system web api")]
            public string TimingUrl { get; set; }

            [Option("Token to be used for authentication to the web api")]
            [Required]
            public string AuthToken { get; set; }

            protected override int OnExecute(CommandLineApplication app)
            {
                HttpClient client = new HttpClient();

                GymkhanaNL webInterface = new GymkhanaNL(client, AuthToken, WebApiUrl);
                Pusher pusher = new Pusher(client, webInterface, TimingUrl);

                // we do not really have a need for a better stop mechanism than ctrl+c right now
                pusher.Run(new System.Threading.CancellationToken()).Wait();

                return 0;
            }
        }
    }
}

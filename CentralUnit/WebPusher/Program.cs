using System;
using McMaster.Extensions.CommandLineUtils;

namespace WebPusher
{
    class Program
    {
        public static void Main(string[] args) => CommandLineApplication.Execute<RootCommand>(args);

        /// <summary>
        /// Root command of the tool. For different modes of operation, make more command classes and put them in [SubCommand] attributes
        /// </summary>
        [Command("webpush")]
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
    }
}

using System;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace CLI
{
    class Program
    {
        public static void Main(string[] args) => CommandLineApplication.Execute<RootCommand>(args);
    }

    [Command("mgk")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    class RootCommand : CommandBase
    {
        protected override int OnExecute(CommandLineApplication app)
        {
            Console.WriteLine("This is the root command, it does not do anything");
            app.ShowHelp();
            return 1;
        }

        private static string GetVersion()
            => typeof(RootCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }

    [HelpOption("--help")]
    abstract class CommandBase
    {
        protected abstract int OnExecute(CommandLineApplication app);
    }
}

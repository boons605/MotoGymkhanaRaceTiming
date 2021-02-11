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
    [Subcommand(typeof(TestUnitCommand))]
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
}

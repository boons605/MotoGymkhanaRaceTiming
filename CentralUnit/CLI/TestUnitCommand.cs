using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using Communication;
using log4net;
using log4net.Config;
using McMaster.Extensions.CommandLineUtils;
using Models;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;

namespace CLI
{
    public class TestUnitCommand : CommandBase
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static CommunicationManager CommunicationManager;

        private static List<Rider> riders;

        private static IRiderIdUnit startIdUnit;

        private static IRiderIdUnit finishIdUnit;

        private static ITimingUnit timer;

        private static long lastMicros;

        private static int unitsConnecting = 2;

        [Required]
        [Argument(0, "File path to xml that contains known riders")]
        public string RidersFile { get; set; }

        [Required]
        [Argument(1, "Timing unit identifier")]
        public string TimingId { get; set; }

        [Required]
        [Argument(2, "Start id unit identifier")]
        public string StartId { get; set; }

        [Argument(3, "end id unit identifier")]
        public string EndId { get; set; }

        [Option("Id for start gate of the timing unit")]
        public int StartGateId { get; set; } = 0;

        [Option("Id for end gate of the timing unit")]
        public int EndGateId { get; set; } = 1;



        private static void StartIdUnit_OnRiderExit(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Lost rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void StartIdUnit_OnRiderId(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Got rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void Timer_OnTrigger(object sender, TimingTriggeredEventArgs e)
        {
            Log.Info($"Got timing event with time: {e.Microseconds}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void PrintUsage()
        {
            Log.Info("Usage: ");
            Log.Info("CLITestWithUnits RIDERFILE TIMERSERIALPORT RIDERIDSTARTSERIALPORT [RIDERIDFINISHSERIALPORT]");

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

        protected override int OnExecute(CommandLineApplication app)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo("logConfig.xml"));

            CancellationTokenSource source = new CancellationTokenSource();

            CommunicationManager = new CommunicationManager(source.Token);

            try
            {
                riders = ReadRidersFile(RidersFile);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not parse riders file {RidersFile}", ex);
                Environment.Exit(-2);
            }

            timer = new SerialTimingUnit(CommunicationManager.GetCommunicationDevice(TimingId), "timerUnit", source.Token, StartGateId, EndGateId);
            timer.OnTrigger += Timer_OnTrigger;

            if (timer is AbstractCommunicatingUnit)
            {
                //((AbstractCommunicatingUnit)timer).
            }

            startIdUnit = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(StartId), "startUnit", 2.0, source.Token);
            startIdUnit.OnRiderId += StartIdUnit_OnRiderId;
            startIdUnit.OnRiderExit += StartIdUnit_OnRiderExit;

            if (!String.IsNullOrEmpty(EndId))
            {
                finishIdUnit = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(EndId), "finishUnit", 2.0, source.Token);
                unitsConnecting++;
            }

            startIdUnit.AddKnownRiders(riders);

            Console.ReadLine();

            source.Cancel();

            CommunicationManager.Dispose();

            return 0;
        }
    }
}

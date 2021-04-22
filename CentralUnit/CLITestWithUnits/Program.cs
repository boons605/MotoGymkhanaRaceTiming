

namespace CLITestWithUnits
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Communication;
    using log4net;
    using log4net.Config;
    using Models;
    using SensorUnits.RiderIdUnit;
    using SensorUnits.TimingUnit;

    class Program
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


        static void Main(string[] args)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo("logConfig.xml"));

            CancellationTokenSource source = new CancellationTokenSource();

            CommunicationManager = new CommunicationManager(source.Token);

            if (args.Length < 2)
            {
                Console.WriteLine($"Not enough arguments: {args.Length}");
                PrintUsage();
                Environment.Exit(-1);
            }

            try
            {
                riders = ReadRidersFile(args[0]);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not parse riders file {args[0]}", ex);
                Environment.Exit(-2);
            }

            if (args[1] != "none")
            {
                timer = new SerialTimingUnit(CommunicationManager.GetCommunicationDevice(args[1]), "timerUnit", source.Token, 0, 1);
                timer.OnTrigger += Timer_OnTrigger;

                if (timer is AbstractCommunicatingUnit)
                {
                    //((AbstractCommunicatingUnit)timer).
                }
            }

            startIdUnit = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(args[2]), "startUnit", 2.0, source.Token);
            startIdUnit.OnRiderId += StartIdUnit_OnRiderId;
            startIdUnit.OnRiderExit += StartIdUnit_OnRiderExit;

            if (args.Length > 3)
            {
                finishIdUnit = new BLERiderIdUnit(CommunicationManager.GetCommunicationDevice(args[3]), "finishUnit", 2.0, source.Token);
                finishIdUnit.OnRiderId += FinishIdUnit_OnRiderId;
                finishIdUnit.OnRiderExit += FinishIdUnit_OnRiderExit;
                unitsConnecting++;
            }

            startIdUnit.AddKnownRiders(riders);
            finishIdUnit.AddKnownRiders(riders);

            Console.ReadLine();

            source.Cancel();

            CommunicationManager.Dispose();

            Environment.Exit(0);
        }

        private static void FinishIdUnit_OnRiderExit(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Finish lost rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void FinishIdUnit_OnRiderId(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Finish got rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void StartIdUnit_OnRiderExit(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Start lost rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
        }

        private static void StartIdUnit_OnRiderId(object sender, RiderIdEventArgs e)
        {
            Log.Info($"Start got rider: {e.Rider}, received at {e.Received:yyyy-MM-ddThh:mm:ss.fff}");
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
    }
}

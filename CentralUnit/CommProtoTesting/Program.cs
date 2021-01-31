using Communication;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SensorUnits.RiderIdUnit;

namespace CommProtoTesting
{
    class Program
    {
        private static ISerialCommunication serialPort;
        private static CommunicationProtocol proto;

        private static string latestLine = "";
        private static DateTime lastTimeLatestLine = DateTime.UnixEpoch;
        private static int countLatestLine = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            CommunicationManager cm = new CommunicationManager();

            string comPort = "directserial:COM5";

            if (args.Length == 1)
            {
                comPort = args[0];
            }

            serialPort = cm.GetCommunicationDevice(comPort);

            proto = new CommunicationProtocol(serialPort);
            proto.NewDataArrived += Proto_NewDataArrived;
            proto.ConnectionStateChanged += Proto_ConnectionStateChanged;

            string line = Console.ReadLine();

            while (line != "Q")
            {
                CommandData cmd = null;
                switch (line.ToLowerInvariant())
                {
                    case "listallowed":
                        cmd = new CommandData(0x0003, 0x0000, new byte[2]);
                        break;
                    case "detectall":
                        cmd = new CommandData(0x0004, 0x0000, new byte[2]);
                        break;
                    case "addallowed":
                        cmd = new CommandData(0x0001, 0x0000, GetCommandData(Console.ReadLine()));
                        break;
                    case "removeallowed":
                        cmd = new CommandData(0x0002, 0x0000, GetCommandData(Console.ReadLine()));
                        break;
                    case "getclosest":
                        cmd = new CommandData(0x0005, 0x0000, new byte[2]);
                        break;
                    default:
                        break;

                }

                if (cmd != null)
                {
                    if (proto.ReadyToSend())
                    {
                        proto.SendCommand(cmd);
                    }
                    else
                    {
                        WriteLineToConsole("Proto not ready to send, try again later");
                    }
                }

                line = Console.ReadLine();
            }

            Console.WriteLine("Goodbye World!");
        }

        private static byte[] GetCommandData(string line)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            if (!string.IsNullOrEmpty(line))
            {
                string[] elements = line.Split(';');

                writer.Write(TextToMacBytes(elements[0]));
                if (elements.Length > 1)
                {
                    writer.Write(Convert.ToInt16(elements[1]));
                }
                else
                {
                    writer.Write(Convert.ToInt16(0));
                }
            }
            return stream.ToArray();
        }

        private static string macRegEx = "([0-9a-fA-F]{2})(?:[-:]){0,1}";

        private static byte[] TextToMacBytes(string text)
        {
            byte[] macBytes = new byte[6];

            if (!string.IsNullOrEmpty(text))
            {
                if (Regex.IsMatch(text, macRegEx))
                {
                    MatchCollection bytes = Regex.Matches(text, macRegEx);

                    if (bytes.Count == 6)
                    {
                        for (int i = 0; i < bytes.Count; i++)
                        {
                            if (bytes[i].Groups.Count == 2)
                            {
                                macBytes[i] = Convert.ToByte("0x" + bytes[i].Groups[1], 16);
                            }
                        }
                    }
                }
                else
                {
                    WriteLineToConsole("Not a valid address: " + text);
                }
            }

            return macBytes;
        }

        private static void Proto_ConnectionStateChanged(object sender, EventArgs e)
        {
            Console.WriteLine($"Serial port connection state: {serialPort.Connected}");
        }

        private static void WriteLineToConsole(string line)
        {
            if ((line != latestLine) ||
                ((line == latestLine) && (DateTime.Now.Subtract(lastTimeLatestLine).TotalSeconds > 10)))
            {
                lastTimeLatestLine = DateTime.Now;
                if (line == latestLine)
                {
                    Console.WriteLine($"Skipped same line {countLatestLine} times");
                }
                latestLine = line;
                Console.WriteLine(line);
            }
            else
            {
                countLatestLine++;
            }
        }

        private static void Proto_NewDataArrived(object sender, EventArgs e)
        {
            CommandData latestData;
            while ((latestData = proto.NextPacket) != null)
            {

                switch (latestData.CommandType)
                {
                    case 1:
                        HandleAddAllowedResponse(latestData);
                        break;
                    case 2:
                        HandleRemoveAllowedResponse(latestData);
                        break;
                    case 3:
                        HandleListAllowedDevices(latestData);
                        break;
                    case 4:
                        HandleListDetectedDevices(latestData);
                        break;
                    case 5:
                        HandleGetClosestDevice(latestData);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void HandleGetClosestDevice(CommandData latestData)
        {
            if (latestData.Status != 0)
            {
                WriteLineToConsole($"Get Closest Device returned: {latestData.Status}");
            }
            else
            {
                List<Beacon> beacons = RiderIDCommandDataParser.ParseClosestDeviceResponse(latestData.Status, latestData.Data);
                foreach (Beacon b in beacons)
                {
                    WriteLineToConsole($"Got closest device: {b.ToString()}");
                }
            }
        }

        private static void HandleListDetectedDevices(CommandData latestData)
        {
            switch (latestData.Status)
            {
                case 8:
                    HandleListDetectedDevicesProgress(latestData.Data);
                    break;
                case 0:

                        WriteLineToConsole("Started listing all devices");
                    break;
                case 1:
                    if (latestData.Data.Length > 2)
                    {
                        byte[] devicesData = new byte[latestData.Data.Length - 2];
                        Array.Copy(latestData.Data, 2, devicesData, 0, devicesData.Length);
                        HandleListDetectedDevicesDeviceData(devicesData);
                    }
                    else
                    {
                            WriteLineToConsole("Data not long enough");
                    }
                    break;
                default:
                        WriteLineToConsole(String.Format("Error response: {0}", latestData.Status));
                    break;
            }
        }

        private static void HandleListDetectedDevicesDeviceData(byte[] devicesData)
        {
            List<Beacon> beacons = RiderIDCommandDataParser.ParseClosestDeviceResponse(0, devicesData);
            foreach (Beacon b in beacons)
            {
                WriteLineToConsole($"Found device: {b.ToString()}");
            }
        }

        private static void HandleListDetectedDevicesProgress(byte[] data)
        {
            WriteLineToConsole(String.Format("Detection progress: {0:D}%", data[0]));
        }

        private static void HandleListAllowedDevices(CommandData latestData)
        {
            if ((latestData.Data.Length > 2) && (latestData.Status == 1))
            {

                if (latestData.Data[0] == 0)
                {
                    WriteLineToConsole("Got allowed devices");
                }
                byte[] devicesData = new byte[latestData.Data.Length - 2];
                Array.Copy(latestData.Data, 2, devicesData, 0, devicesData.Length);
                List<Beacon> devices = RiderIDCommandDataParser.ParseClosestDeviceResponse(0, devicesData);
                foreach (Beacon b in devices)
                {
                    WriteLineToConsole($"Got allowed device: {b.ToString()}");
                }

            }
            else
            {

                    WriteLineToConsole(String.Format("Data length or error response: {0}", latestData.Status));
            }
        }

        private static void HandleRemoveAllowedResponse(CommandData latestData)
        {
            WriteLineToConsole($"Got allowed devices remove response with status {latestData.Status} and data {BitConverter.ToString(latestData.Data)}");
        }

        private static void HandleAddAllowedResponse(CommandData latestData)
        {
            WriteLineToConsole($"Got allowed devices add response with status {latestData.Status} and data {BitConverter.ToString(latestData.Data)}");
        }
    }
}

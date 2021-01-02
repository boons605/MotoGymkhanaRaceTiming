using Communication;
using System;
using System.Text;
using XBeeLibrary.Core.Models;
using XBeeLibrary.Core.Packet;
using XBeeLibrary.Core.Packet.Common;
using XBeeLibrary.Core.Packet.Raw;

namespace XBeeLibraryTesting
{
    class Program
    {

        private static ISerialCommunication serialPort;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            CommunicationManager cm = new CommunicationManager();

            serialPort = cm.GetCommunicationDevice("xbee:COM4:0013A20041BB64A6");
            serialPort.Failure += SerialPort_Failure;
            serialPort.ConnectionStateChanged += SerialPort_ConnectionStateChanged;
            serialPort.DataReceived += SerialPort_DataReceived;

            string line = Console.ReadLine();

            while (line != "Q")
            {
                serialPort.Write(Encoding.ASCII.GetBytes(line));
                line = Console.ReadLine();
            }

            Console.WriteLine("Goodbye World!");

        }

        private static void SerialPort_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Received data {0}", Encoding.ASCII.GetString(e.Data));
        }

        private static void SerialPort_ConnectionStateChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Connection state change to: {0}", serialPort.Connected);
        }

        private static void SerialPort_Failure(object sender, EventArgs e)
        {
            Console.WriteLine("FAILURE!!!!!");
        }



    }
}

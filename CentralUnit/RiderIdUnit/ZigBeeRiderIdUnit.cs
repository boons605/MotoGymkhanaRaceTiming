using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using ZigBeeNet;
using ZigBeeNet.Tranport.SerialPort;
using ZigBeeNet.Hardware.Digi.XBee;

namespace RiderIdUnit
{
    public class ZigBeeRiderIdUnit
    {
        public void Run()
        {
            try
            {
                ZigBeeSerialPort zigbeePort = new ZigBeeSerialPort("COM4");

                ZigBeeDongleXBee dongle = new ZigBeeNet.Hardware.Digi.XBee.ZigBeeDongleXBee(zigbeePort);

                ZigBeeNetworkManager networkManager = new ZigBeeNetworkManager(dongle);

                // Initialise the network
                networkManager.Initialize();

                networkManager.AddSupportedCluster(0x06);

                ZigBeeStatus startupSucceded = networkManager.Startup(false);

                if (startupSucceded == ZigBeeStatus.SUCCESS)
                {
                    Log.Logger.Information("ZigBee console starting up ... [OK]");
                }
                else
                {
                    Log.Logger.Information("ZigBee console starting up ... [FAIL]");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

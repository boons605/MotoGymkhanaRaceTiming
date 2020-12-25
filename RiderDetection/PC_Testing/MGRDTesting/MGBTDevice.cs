using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGRDTesting
{
    public class MGBTDevice
    {

        private byte[] address = new byte[6];
        private Int16 rssi;
        private Int16 measuredPower;
        private Int16 measurePowerCorrection;

        private const double distEnvFactor = 4.0;

        public double Distance
        {
            get
            {
                return Math.Pow(10, (((double)measuredPower-(double)rssi)/(10.0*distEnvFactor)));
            }
        }

        public byte[] Address
        {
            get
            {
                return address;
            }
        }
        
        public static List<MGBTDevice> FromArray(byte[] data)
        {
            List<MGBTDevice> devices = new List<MGBTDevice>();
            if ((data != null) && (data.Length > 0))
            {
                try
                {
                    var reader = new BinaryReader(new MemoryStream(data));

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var s = new MGBTDevice();

                        s.address = reader.ReadBytes(s.address.Length);
                        s.rssi = reader.ReadInt16();
                        s.measuredPower = reader.ReadInt16();
                        s.measurePowerCorrection = reader.ReadInt16();

                        devices.Add(s);
                    }
                }
                catch (Exception e)
                {
                    
                }
            }
            return devices;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < address.Length; i++)
            {
                builder.AppendFormat("{0:X2}", address[i]);
                if (i < (address.Length-1))
                {
                    builder.Append(":");
                }
            }
            builder.AppendFormat(", P: {0:D2}(-{1:D2})", measuredPower, measurePowerCorrection);
            builder.AppendFormat(", R: {0:D2}", rssi);
            builder.AppendFormat(", D: {0:F3}", Distance);

            return builder.ToString();
        }


    }
}

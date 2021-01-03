// <copyright file="RiderIDCommandDataParser.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace RiderIdUnit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using log4net;
    using Models;

    /// <summary>
    /// Parser for Rider ID Unit command data.
    /// </summary>
    public class RiderIDCommandDataParser
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Parses the data from <paramref name="data"/> into a <see cref="Beacon"/> object.
        /// When status is not 0, it means that the ID Unit indicates an error, in that case null is returned as the 
        /// </summary>
        /// <param name="status">The status code received from the ID Unit</param>
        /// <param name="data">The data received from the ID unit.</param>
        /// <returns>A <see cref="Beacon"/> when the data is valid, null when the status code is non-zero</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="status"/> is 0 and <paramref name="data"/> is less than 12 bytes long</exception>
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Used for communication, exact sizing required.")]
        public static List<Beacon> ParseClosestDeviceResponse(int status, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            List<Beacon> foundBeacons = new List<Beacon>();

            if (status == 0)
            {
                if (data.Length < 12)
                {
                    throw new ArgumentException($"Data length shall be at least 12 bytes, but got {data.Length}", "data");
                }

                try
                {
                    var reader = new BinaryReader(new MemoryStream(data));

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte[] address = reader.ReadBytes(Beacon.IdentifierLength);
                        Int16 rssi = reader.ReadInt16();
                        Int16 measuredPower = reader.ReadInt16();
                        Int16 measurePowerCorrection = reader.ReadInt16();

                        foundBeacons.Add(new Beacon(address, measurePowerCorrection) { Rssi = rssi, MeasuredPower = measuredPower });
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("Exception while parsing data received from Rider ID unit", e);
                }
            }

            return foundBeacons;
        }
    }
}

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
            List<Beacon> foundBeacons = new List<Beacon>();

            if (status == 0)
            {
                CheckDataInput(data, 12);

                try
                {
                    var reader = new BinaryReader(new MemoryStream(data));

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte[] address = reader.ReadBytes(Beacon.IdentifierLength);
                        Int16 rssi = reader.ReadInt16();
                        Int16 measuredPower = reader.ReadInt16();
                        Int16 measurePowerCorrection = reader.ReadInt16();

                        foundBeacons.Add(new Beacon(address, (UInt16)measurePowerCorrection) { Rssi = rssi, MeasuredPower = measuredPower });
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("Exception while parsing data received from Rider ID unit", e);
                }
            }

            return foundBeacons;
        }

        /// <summary>
        /// Parse the response to a a Remove Allowed Device or Add Allowed Device response.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="data">The data to check</param>
        /// <returns>A new <see cref="Beacon"/></returns>
        /// <exception cref="ArgumentException">Data is less than 8 bytes long</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Used for communication, exact sizing required.")]
        public static Beacon ParseAllowedDeviceOperationResponse(int status, byte[] data)
        {
            Beacon beacon = null;

            CheckDataInput(data, 8);

            try
            {
                var reader = new BinaryReader(new MemoryStream(data));

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    byte[] address = reader.ReadBytes(Beacon.IdentifierLength);
                    Int16 measurePowerCorrection = reader.ReadInt16();

                    beacon = new Beacon(address, (UInt16)measurePowerCorrection);
                }
            }
            catch (Exception e)
            {
                Log.Warn("Exception while parsing data received from Rider ID unit", e);
            }

            return beacon;
        }

        /// <summary>
        /// Check the data input for null and data size.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="minLength">Minimum data length.</param>
        /// <exception cref="ArgumentException">Data is less than <paramref name="minLength"/> long</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        private static void CheckDataInput(byte[] data, int minLength)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length < minLength)
            {
                throw new ArgumentException($"Data length shall be at least {minLength} bytes, but got {data.Length}", "data");
            }
        }
    }
}

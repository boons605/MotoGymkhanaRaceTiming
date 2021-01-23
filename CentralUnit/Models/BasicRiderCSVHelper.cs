// <copyright file="BasicRiderCSVHelper.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using log4net;

    /// <summary>
    /// Basic CSV rider list parser.
    /// </summary>
    public class BasicRiderCSVHelper
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Regex to validate and split a string representation of a MAC address.
        /// </summary>
        private static string macRegEx = "([0-9a-fA-F]{2})(?:[-:]){0,1}";

        /// <summary>
        /// Parse a rider line.
        /// </summary>
        /// <param name="line">A rider line, semicolon separated: MACAddress;Correction;RiderName</param>
        /// <returns>A Rider object.</returns>
        public static Rider ParseRiderLine(string line)
        {
            Rider rider = null;

            if (!string.IsNullOrEmpty(line))
            {
                string[] elements = line.Split(';');

                byte[] ident = TextToMacBytes(elements[0]);
                ushort correction = 0;
                if (elements.Length > 1)
                {
                    correction = Convert.ToUInt16(elements[1]);
                }

                Beacon beacon = new Beacon(ident, correction);

                if (elements.Length > 2)
                {
                    rider = new Rider(elements[2], beacon);
                }
                else
                {
                    Log.Warn($"Could not parse rider, not enough elements in line: {line}");
                }
            }

            return rider;
        }

        /// <summary>
        /// Parses a string representation of a MAC address to a byte array.
        /// </summary>
        /// <param name="text">The MAC Address in a hexadecimal string</param>
        /// <returns>The MAC address as a byte array</returns>
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
                    Log.Warn($"Not a valid address: {text}");
                }
            }

            return macBytes;
        }
    }
}

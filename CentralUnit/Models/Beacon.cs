// <copyright file="Beacon.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// This class represents a BLE Beacon used for identifying riders.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Used for communication, exact sizing required.")]
    public class Beacon
    {
        /// <summary>
        /// Length of a BLE MAC address.
        /// </summary>
        public const int IdentifierLength = 6;

        /// <summary>
        /// Environmental correction factor for the distance calculation.
        /// </summary>
        private const double DistanceEnvironmentFactor = 4.0;

        /// <summary>
        /// The BLE MAC address of this beacon.
        /// </summary>
        public byte[] Identifier { get; private set; }

        /// <summary>
        /// The Received Signal Strength Indicator for this beacon.
        /// This is a negative number expressed in <c>dBm</c>.
        /// </summary>
        public int Rssi;

        /// <summary>
        /// The calibrated RSSI at 1m distance, as provided by the manufacturer and sent in the payload of the beacon broadcast message.
        /// </summary>
        public int MeasuredPower;

        /// <summary>
        /// The correction factor for the <see cref="measuredPower"/>, since manufacturers of cheap Chinese iBeacons don't always put the correct
        /// data in the beacon broadcast message.
        /// </summary>
        public UInt16 CorrectionFactor { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Beacon" /> class based on an identifier and a correction factor for 
        /// </summary>
        /// <param name="identifier">The 6-byte BLE MAC address</param>
        /// <param name="correctionFactor">The correction factor for cheap Chinese iBeacons</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="identifier"/> is null</exception>
        /// <exception cref="ArgumentException">When the <paramref name="identifier"/> is not exactly 6 bytes long</exception>
        public Beacon(byte[] identifier, UInt16 correctionFactor)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }

            if (identifier.Length != IdentifierLength)
            {
                throw new ArgumentException($"Invalid identifier length ({identifier.Length}). Identifier must be 6 bytes to represent a BLE MAC address");
            }

            this.Identifier = identifier;
            this.CorrectionFactor = correctionFactor;
        }

        /// <summary>
        /// Gets the calculated the distance based on the calibrated RSSI at 1m distance and the measured RSSI.
        /// </summary>
        public double Distance
        {
            get
            {
                if ((this.MeasuredPower == 0) || (this.Rssi == 0))
                {
                    return 0.0;
                }

                return Math.Pow(10, ((double)this.MeasuredPower - (double)this.Rssi) / (10.0 * DistanceEnvironmentFactor));
            }
        }

        /// <summary>
        /// Represents this Beacon as a string.
        /// </summary>
        /// <returns>String representation of this Beacon object.</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append("Id: ");
            output.Append(BitConverter.ToString(this.Identifier));
            output.Append(";MeasuredPower: ");
            output.Append(this.MeasuredPower);
            output.Append("(-");
            output.Append(this.CorrectionFactor);
            output.Append(")");
            output.Append(";RSSI: ");
            output.Append(this.Rssi);
            output.Append(";Distance: ");
            output.Append(this.Distance);

            return output.ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Identifier.GetHashCode() ^ this.CorrectionFactor.GetHashCode() ^ this.MeasuredPower.GetHashCode() ^ this.Rssi.GetHashCode();
        }

        /// <summary>
        /// Checks if this Beacon is equal to another object, based on the identifier.
        /// </summary>
        /// <param name="obj">The other Beacon object.</param>
        /// <returns>true if identifiers match. false if <paramref name="obj"/> is not a Beacon or the identifiers don't match.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (obj is Beacon)
                {
                    Beacon otherBeacon = (Beacon)obj;
                    for (int i = 0; i < this.Identifier.Length; i++)
                    {
                        if (this.Identifier[i] != otherBeacon.Identifier[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

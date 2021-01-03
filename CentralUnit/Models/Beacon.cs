// <copyright file="Beacon.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// This class represents a BLE Beacon used for identifying riders.
    /// </summary>
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
        private byte[] identifier;

        /// <summary>
        /// The Received Signal Strength Indicator for this beacon.
        /// This is a negative number expressed in <c>dBm</c>.
        /// </summary>
        private int rssi;

        /// <summary>
        /// The calibrated RSSI at 1m distance, as provided by the manufacturer and sent in the payload of the beacon broadcast message.
        /// </summary>
        private int measuredPower;

        /// <summary>
        /// The correction factor for the <see cref="measuredPower"/>, since manufacturers of cheap Chinese iBeacons don't always put the correct
        /// data in the beacon broadcast message.
        /// </summary>
        private int correctionFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="Beacon" /> class based on an identifier and a correction factor for 
        /// </summary>
        /// <param name="identifier">The 6-byte BLE MAC address</param>
        /// <param name="correctionFactor">The correction factor for cheap Chinese iBeacons</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="identifier"/> is null</exception>
        /// <exception cref="ArgumentException">When the <paramref name="identifier"/> is not exactly 6 bytes long</exception>
        public Beacon(byte[] identifier, int correctionFactor)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }

            if (identifier.Length != IdentifierLength)
            {
                throw new ArgumentException($"Invalid identifier length ({identifier.Length}). Identifier must be 6 bytes to represent a BLE MAC address");
            }

            this.identifier = identifier;
            this.correctionFactor = correctionFactor;
        }

        /// <summary>
        /// Gets the calculated the distance based on the calibrated RSSI at 1m distance and the measured RSSI.
        /// </summary>
        public double Distance
        {
            get
            {
                if ((this.measuredPower == 0) || (this.rssi == 0))
                {
                    return 0.0;
                }

                return Math.Pow(10, ((double)this.measuredPower - (double)this.rssi) / (10.0 * DistanceEnvironmentFactor));
            }
        }

        /// <summary>
        /// Gets or sets the Received Signal Strength Indicator for this beacon.
        /// </summary>
        public int Rssi { get => this.rssi; set => this.rssi = value; }

        /// <summary>
        /// Gets or sets the calibrated RSSI at 1m distance.
        /// </summary>
        public int MeasuredPower { get => this.measuredPower; set => this.measuredPower = value; }

        /// <summary>
        /// Represents this Beacon as a string.
        /// </summary>
        /// <returns>String representation of this Beacon object.</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append("Id: ");
            output.Append(BitConverter.ToString(this.identifier));
            output.Append(";MeasuredPower: ");
            output.Append(this.measuredPower);
            output.Append("(-");
            output.Append(this.correctionFactor);
            output.Append(")");
            output.Append(";RSSI: ");
            output.Append(this.rssi);
            output.Append(";Distance: ");
            output.Append(this.Distance);

            return output.ToString();
        }
    }
}

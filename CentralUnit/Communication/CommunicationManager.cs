// <copyright file="CommunicationManager.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// Management of communication devices.
    /// </summary>
    public class CommunicationManager : IDisposable
    {
        /// <summary>
        /// Regular expression to verify identifiers and split them into groups.
        /// </summary>
        private const string IdentifierRegex = "([a-zA-Z]+):([a-zA-Z0-9/]+)(?::)?([a-fA-F0-9]{16})?";

        /// <summary>
        /// List of <see cref="XbeeNetwork"/> objects.
        /// </summary>
        private Dictionary<string, XbeeNetwork> xbeeNetworks;

        /// <summary>
        /// List of <see cref="ISerialCommunication"/> objects.
        /// </summary>
        private Dictionary<string, ISerialCommunication> devices;

        /// <summary>
        /// The cancellation token for this unit.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationManager" /> class.
        /// </summary>
        /// <param name="token">The cancellation token for this unit</param>
        public CommunicationManager(CancellationToken token)
        {
            this.cancellationToken = token;
            this.xbeeNetworks = new Dictionary<string, XbeeNetwork>();
            this.devices = new Dictionary<string, ISerialCommunication>();
        }

        /// <summary>
        /// Closes all connections.
        /// </summary>
        public void Dispose()
        {
            foreach (XbeeNetwork net in this.xbeeNetworks.Values)
            {
                net.Dispose();
            }

            this.xbeeNetworks.Clear();

            foreach (ISerialCommunication dev in this.devices.Values)
            {
                dev.Close();
            }

            this.devices.Clear();
        }

        /// <summary>
        /// Gets the serial communication device with the specified identifier.
        /// Prefix <c>directserial</c> (i.e. <c>directserial:COM1</c> or <c>directserial:/dev/ttyS0</c>) gets a direct serial connection.
        /// Prefix <c>xbee</c> followed by a serial port and a 64-bit <c>Xbee</c> address gets an <c>Xbee</c> connection. (e.g.: <c>xbee:COM4:0013A20041BB64A6</c> or <c>xbee:/dev/ttyS0:0013A20041BB64A6</c>)
        /// </summary>
        /// <param name="identifier">The device to get.</param>
        /// <returns>An ISerialCommunication device.</returns>
        public ISerialCommunication GetCommunicationDevice(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException("identifier");
            }

            if (!Regex.IsMatch(identifier, IdentifierRegex))
            {
                throw new ArgumentException($"Channel identifier {identifier} does not match the format type:port(:address) or regex {IdentifierRegex}", "identifier");
            }

            GroupCollection identifierParts = Regex.Match(identifier, IdentifierRegex).Groups;

            ISerialCommunication communicationDevice = null;

            if (identifierParts.Count > 2)
            {
                string communicationType = identifierParts[1].Value;
                switch (communicationType)
                {
                    case "xbee":
                        if (identifierParts.Count != 4)
                        {
                            throw new ArgumentException($"Not enough argument supplied for communication type xbee: {identifier}");
                        }
                        else
                        {
                            communicationDevice = this.GetXbeeDevice(identifierParts[2].Value, identifierParts[3].Value);
                        }

                        break;
                    case "directserial":
                        communicationDevice = this.GetSerialDevice(identifierParts[2].Value);
                        break;
                    default:
                        throw new ArgumentException($"Invalid communication type: {communicationType} in identifier {identifier}");
                }
            }

            return communicationDevice;
        }

        /// <summary>
        /// Gets or creates a new <see cref="DirectSerialCommunication"/> device with the specified port name.
        /// </summary>
        /// <param name="portName">The name of the serial port (e.g. <c>COM1</c> or <c>/dev/ttyS0</c>)</param>
        /// <returns>The corresponding <see cref="DirectSerialCommunication"/> instance</returns>
        private ISerialCommunication GetSerialDevice(string portName)
        {
            if (!this.devices.ContainsKey(portName))
            {
                this.devices.Add(portName, new DirectSerialCommunication(portName, this.cancellationToken));
            }

            return this.devices[portName];
        }

        /// <summary>
        /// Gets or creates a new <see cref="XbeeSerialCommunication"/> device with specified port name and address.
        /// When needed, a new <see cref="XbeeNetwork"/> is initialized on the specified port.
        /// </summary>
        /// <param name="networkNameOrPort">The serial port (use as network name) the device is connected to (e.g. <c>COM1</c> or <c>/dev/ttyS0</c>)</param>
        /// <param name="deviceName">The 64-bit <c>Xbee</c> address in hexadecimal string format (e.g. 0013A20041BB64A6)</param>
        /// <returns>The corresponding <see cref="XbeeSerialCommunication"/> instance</returns>
        private ISerialCommunication GetXbeeDevice(string networkNameOrPort, string deviceName)
        {
            if (!this.xbeeNetworks.ContainsKey(networkNameOrPort))
            {
                this.xbeeNetworks.Add(networkNameOrPort, new XbeeNetwork(networkNameOrPort, this.cancellationToken));
            }

            XbeeNetwork xbeeNet = this.xbeeNetworks[networkNameOrPort];
            return xbeeNet.GetDevice(deviceName);
        }
    }
}

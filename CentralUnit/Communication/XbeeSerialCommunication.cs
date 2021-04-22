// <copyright file="XbeeSerialCommunication.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using XBeeLibrary.Core.Models;
    using XBeeLibrary.Core.Packet.Common;

    /// <summary>
    /// Implementation of ISerialCommunication for an device connected through <c>Xbee</c>.
    /// </summary>
    public class XbeeSerialCommunication : ISerialCommunication
    {
        /// <summary>
        /// The 64-bit address of the devices.
        /// </summary>
        private XBee64BitAddress xbee64address;

        /// <summary>
        /// Connection state of this devices.
        /// </summary>
        private bool connected;

        /// <summary>
        /// The transmit frame ID to be used by this device.
        /// </summary>
        private byte frameId;

        /// <summary>
        /// The <see cref="XbeeNetwork" /> this devices belongs to.
        /// </summary>
        private XbeeNetwork network;

        /// <summary>
        /// Initializes a new instance of the <see cref="XbeeSerialCommunication" /> class.
        /// Only for use through the <see cref="XbeeNetwork" /> class.
        /// </summary>
        /// <param name="address">The 64-bit address of this device.</param>
        /// <param name="xbeeNetwork">The network this devices belongs to.</param>
        /// <param name="transmitFrameId">The frame ID to be used by this device</param>
        internal XbeeSerialCommunication(XBee64BitAddress address, XbeeNetwork xbeeNetwork, byte transmitFrameId)
        {
            this.xbee64address = address;
            this.network = xbeeNetwork;
            this.frameId = transmitFrameId;
        }

        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc/>
        public event EventHandler Failure;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <inheritdoc/>
        public bool Connected { get => this.connected; }

        /// <inheritdoc/>
        public byte FrameID { get => this.frameId; }

        /// <inheritdoc/>
        public string Name => this.network.Name + ":" + this.xbee64address.ToString();

        /// <summary>
        /// Gets the 64-bit address of the device.
        /// </summary>
        public XBee64BitAddress Xbee64address { get => this.xbee64address; }

        /// <summary>
        /// Closes the communication channel.
        /// Notifies the <see cref="XbeeNetwork" /> of device closure.
        /// </summary>
        public void Close()
        {
            this.network.CloseXbeeDevice(this.xbee64address);
        }

        /// <inheritdoc/>
        public void Write(byte[] input)
        {
            // 0xFFFE means '16-bit address unknown or broadcast'
            // We don't care about the 16-bit address here, since we use the 64-bit address, which is hardware-bound.
            XBee16BitAddress x16a = new XBee16BitAddress("FFFE");
            /*
             Arg 1: Frame ID > 0x01 = We do want feedback about the transmission, assigned at initialization.
             Arg 2 and 3: The 64-bit address and the 16-bit dummy address
             Arg 4: Broadcast radius. Set to 0, but irrelevant because we are not sending broadcast messages.
             Arg 5: Transmit options. Set to 0, irrelevant.
             Arg 6: The actual data to transmit.
            */
            TransmitPacket tp = new TransmitPacket(this.frameId, this.xbee64address, x16a, 0x0, 0x0, input);
            this.network.Write(tp.GenerateByteArrayEscaped());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Xbee comm channel {this.Xbee64address} on port {this.network}";
        }

        /// <summary>
        /// Processes data received from this <c>Xbee</c> module as parsed by the <see cref="XbeeNetwork" /> 
        /// </summary>
        /// <param name="receivedRfData">The data that was received, stripped from any <c>Xbee</c> API data</param>
        internal void OnDataReceived(byte[] receivedRfData)
        {
            this.connected = true;
            this.DataReceived?.Invoke(this, new DataReceivedEventArgs(receivedRfData));
        }

        /// <summary>
        /// Notifies the user of this communication channel of changes in connection states of either the
        /// serial communication channel that this channel is connected to or <c>Xbee</c> devices connection state (association or disassociation
        /// from the network)
        /// </summary>
        /// <param name="connectionState">The connection state of the channel.</param>
        internal void OnConnectionStateChanged(ConnectionStateChangedEventArgs connectionState)
        {
            this.connected = connectionState.Connected;
            this.ConnectionStateChanged?.Invoke(this, connectionState);
        }

        /// <summary>
        /// Notifies the user of this communication channel about failures detected by the network.
        /// Might be serial port failure, but might also be communication failure.
        /// </summary>
        internal void OnFailure()
        {
            this.Failure?.Invoke(this, new EventArgs());
        }
    }
}

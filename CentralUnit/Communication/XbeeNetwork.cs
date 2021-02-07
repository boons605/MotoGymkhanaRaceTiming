// <copyright file="XbeeNetwork.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using log4net;
    using XBeeLibrary.Core.Models;
    using XBeeLibrary.Core.Packet;
    using XBeeLibrary.Core.Packet.Common;

    /// <summary>
    /// A network of <c>Xbee</c> devices accessed through a single serial communication channel.
    /// </summary>
    public class XbeeNetwork : IDisposable
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// List of <c>Xbee</c> devices registered with this network.
        /// </summary>
        private List<XbeeSerialCommunication> devices = new List<XbeeSerialCommunication>();

        /// <summary>
        /// The highest frame ID assigned to devices.
        /// </summary>
        private byte highestFrameID;

        /// <summary>
        /// The serial communication channel utilized by this network.
        /// </summary>
        private ISerialCommunication communicationChannel;

        /// <summary>
        /// Queue for outgoing messages.
        /// </summary>
        private ConcurrentQueue<byte[]> transmitQueue;

        /// <summary>
        /// Queue for incoming messages.
        /// </summary>
        private ConcurrentQueue<byte[]> receiveQueue;

        /// <summary>
        /// The current packet.
        /// </summary>
        private byte[] currentPacket = null;

        /// <summary>
        /// Buffer position in current packet.
        /// </summary>
        private int currentPacketPosition = 0;

        /// <summary>
        /// Object to lock on.
        /// </summary>
        private object packetLock = new object();

        /// <summary>
        /// Communication thread.
        /// </summary>
        private Thread commThread;

        /// <summary>
        /// Packet parser.
        /// </summary>
        private XBeePacketParser parser;

        /// <summary>
        /// The cancellation token for this unit.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="XbeeNetwork" /> class.
        /// </summary>
        /// <param name="portName">The name of the serial port to use</param>
        /// <param name="token">The cancellation token for this unit</param>
        public XbeeNetwork(string portName, CancellationToken token)
        {
            this.cancellationToken = token;
            this.Initialize(new DirectSerialCommunication(portName, this.cancellationToken));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XbeeNetwork" /> class.
        /// </summary>
        /// <param name="communication">The serial communication channel utilized by this network</param>
        public XbeeNetwork(ISerialCommunication communication)
        {
            this.Initialize(communication);
        }

        /// <summary>
        /// Gets the name of this network.
        /// </summary>
        public string Name { get => this.communicationChannel.Name; }

        /// <summary>
        /// Gets a device from the network or adds this device to the network.
        /// </summary>
        /// <param name="address64bit">The 64-bit address to identify the device, in hex format: (0x)0013A20041BB64A6</param>
        /// <returns>An <see cref="XbeeSerialCommunication"/> communication channel</returns>
        public XbeeSerialCommunication GetDevice(string address64bit)
        {
            XBee64BitAddress address = new XBee64BitAddress(address64bit);
            return this.GetDevice(address);
        }

        /// <summary>
        /// Gets a device from the network or adds this device to the network.
        /// </summary>
        /// <param name="address">The 64-bit address to identify the device</param>
        /// <returns>An <see cref="XbeeSerialCommunication"/> communication channel</returns>
        public XbeeSerialCommunication GetDevice(XBee64BitAddress address)
        {
            XbeeSerialCommunication device = null;
            if (this.devices.Any(dev => dev.Xbee64address.Equals(address)))
            {
                device = this.devices.First(dev => dev.Xbee64address.Equals(address));
            }
            else
            {
                this.highestFrameID++;
                device = new XbeeSerialCommunication(address, this, this.highestFrameID);
                this.devices.Add(device);
            }

            return device;
        }

        /// <summary>
        /// Clean up the devices and close the serial channel.
        /// </summary>
        public void Dispose()
        {
            this.devices.Clear();
            this.communicationChannel.Close();
        }

        /// <summary>
        /// Write data, to be used by <see cref="XbeeSerialCommunication"/> devices.
        /// </summary>
        /// <param name="data">The <c>Xbee</c> API data to write to the serial communication channel.</param>
        internal void Write(byte[] data)
        {
            this.transmitQueue.Enqueue(data);
        }

        /// <summary>
        /// Removes the <see cref="XbeeSerialCommunication"/> device from the network.
        /// </summary>
        /// <param name="address">The 64-bit address to identify the device to be removed.</param>
        internal void CloseXbeeDevice(XBee64BitAddress address)
        {
            if (this.devices.Any(dev => dev.Xbee64address.Equals(address)))
            {
                this.devices.Remove(this.devices.First(dev => dev.Xbee64address.Equals(address)));
            }
        }

        /// <summary>
        /// Performs initialization tasks.
        /// </summary>
        /// <param name="communication">The communication channel to work with.</param>
        private void Initialize(ISerialCommunication communication)
        {
            this.communicationChannel = communication;
            this.communicationChannel.ConnectionStateChanged += this.CommunicationChannel_ConnectionStateChanged;
            this.communicationChannel.DataReceived += this.CommunicationChannel_DataReceived;
            this.communicationChannel.Failure += this.CommunicationChannel_Failure;
            this.transmitQueue = new ConcurrentQueue<byte[]>();
            this.receiveQueue = new ConcurrentQueue<byte[]>();
            this.commThread = new Thread(this.RunXbeeNetwork) { IsBackground = true };
            this.commThread.Start();
            this.parser = new XBeePacketParser();
        }

        /// <summary>
        /// Notifies all devices in this network of a failure in the serial communication.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">EventArgs supplied with the event</param>
        private void CommunicationChannel_Failure(object sender, EventArgs e)
        {
            foreach (XbeeSerialCommunication xbee in this.devices)
            {
                xbee.OnFailure();
            }
        }

        /// <summary>
        /// Processes data received from the communication channel.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">EventArgs supplied with the event contains a byte array with data</param>
        private void CommunicationChannel_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (this.packetLock)
            {
                if (this.currentPacket == null)
                {
                    this.StartNewPacket();
                }

                for (int index = 0; index < e.Data.Length; index++)
                {
                    if (e.Data[index] == (byte)SpecialByte.HEADER_BYTE)
                    {
                        this.StartNewPacket();
                    }

                    this.currentPacket[this.currentPacketPosition] = e.Data[index];
                    this.currentPacketPosition++;
                }
            }
        }

        /// <summary>
        /// Check if the packet in the buffer is complete.
        /// First check is done based on length, since the <c>Xbee</c> library contains a long timeout.
        /// </summary>
        /// <param name="buffer">The buffer to check</param>
        /// <param name="position">The buffer position if it is the current receive buffer to be checked, buffer length if it is a queued buffer</param>
        /// <returns>An <see cref="XBeePacket"/> or null if the packet is incomplete</returns>
        private XBeePacket CheckPacketComplete(byte[] buffer, int position)
        {
            XBeePacket packet = null;
            if (buffer.Length > 3)
            {
                ushort packetLength = (ushort)(((ushort)buffer[1]) << 8);
                packetLength |= (ushort)buffer[2];

                if (position >= packetLength)
                {
                    try
                    {
                        packet = this.parser.ParsePacket(buffer, OperatingMode.API_ESCAPE);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while parsing packet: ", ex);
                    }
                }
            }

            if (packet == null)
            {
                Log.Error($"Packet in queue was invalid, data: {BitConverter.ToString(buffer)}");
            }

            return packet;
        }

        /// <summary>
        /// Starts a new packet by allocating a new buffer and resetting the index to 0.
        /// If index is greater than 0, enqueues the current packet to be processed and distributed among devices.
        /// </summary>
        private void StartNewPacket()
        {
            if (this.currentPacketPosition > 0)
            {
                this.receiveQueue.Enqueue(this.currentPacket);
            }

            this.currentPacket = new byte[1024];
            this.currentPacketPosition = 0;
        }

        /// <summary>
        /// Handles change in communication channel connection state.
        /// Notifies all devices in this network of the new connection state.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">EventArgs supplied with the event</param>
        private void CommunicationChannel_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            foreach (XbeeSerialCommunication xbee in this.devices)
            {
                xbee.OnConnectionStateChanged(e);
            }
        }

        /// <summary>
        /// Thread main method.
        /// </summary>
        private void RunXbeeNetwork()
        {
            try
            {
                DateTime threadStartTime = DateTime.Now;

                // Wait for up to 10 seconds for a connection. This happens on another thread.
                while ((!this.communicationChannel.Connected) &&
                        (DateTime.Now.Subtract(threadStartTime).TotalMilliseconds < 10000))
                {
                    Thread.Sleep(100);
                }

                while (this.communicationChannel.Connected && (!this.cancellationToken.IsCancellationRequested))
                {
                    this.ManageTxQueue();

                    lock (this.packetLock)
                    {
                        this.ManageRxQueue();
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception on XbeeNetwork thread", ex);
            }
        }

        /// <summary>
        /// Process a received <see cref="XBeePacket"/>
        /// </summary>
        /// <param name="packet">The <see cref="XBeePacket"/> to process.</param>
        private void ProcessXbeePacket(XBeePacket packet)
        {
            if (packet != null)
            {
                if (packet is ExplicitRxIndicatorPacket eriPacket)
                {
                    XbeeSerialCommunication deviceForPacket = this.GetDevice(eriPacket.SourceAddress64);
                    deviceForPacket.OnDataReceived(eriPacket.RFData);
                }
                else if (packet is TransmitStatusPacket txsPacket)
                {
                    this.ProcessTransmitStatusPacket(txsPacket);
                }
                else
                {
                    Log.Info($"Got unknown packet of type {packet.GetType().Name}");
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(packet.ToPrettyString());
                    }
                }
            }
        }

        /// <summary>
        /// Process a received <see cref="TransmitStatusPacket"/>.
        /// On success, only debug logging occurs.
        /// When transmission is indicated as not successful, a failure is reported for the device that sent the frame identified in the <see cref="TransmitStatusPacket"/>
        /// </summary>
        /// <param name="txsPacket">The <see cref="TransmitStatusPacket"/> parsed from the byte array.</param>
        private void ProcessTransmitStatusPacket(TransmitStatusPacket txsPacket)
        {
            if (this.devices.Any(dev => dev.FrameID == txsPacket.FrameID))
            {
                XbeeSerialCommunication deviceForPacket = this.devices.First(dev => dev.FrameID == txsPacket.FrameID);

                if (txsPacket.TransmitStatus == XBeeLibrary.Core.Models.XBeeTransmitStatus.SUCCESS)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Transmit succeeded to {deviceForPacket.Xbee64address.ToString()}");
                    }
                }
                else
                {
                    Log.WarnFormat($"Got TX status {txsPacket.TransmitStatus} for device {deviceForPacket.Xbee64address}");
                    deviceForPacket.OnFailure();
                }
            }
            else
            {
                Log.WarnFormat($"Got TX Status {txsPacket.TransmitStatus} for unknown frame ID {txsPacket.FrameID}");
            }
        }

        /// <summary>
        /// Parse packets in the receive queue and distribute them among the devices in the network.
        /// </summary>
        private void ManageRxQueue()
        {
            if (this.currentPacketPosition > 0)
            {
                XBeePacket packet = this.CheckPacketComplete(this.currentPacket, this.currentPacketPosition);
                if (packet != null)
                {
                    this.StartNewPacket();
                }
            }

            while (!this.receiveQueue.IsEmpty)
            {
                byte[] buffer;
                if (this.receiveQueue.TryDequeue(out buffer))
                {
                    XBeePacket packet = this.CheckPacketComplete(buffer, buffer.Length);
                    if (packet != null)
                    {
                        this.ProcessXbeePacket(packet);
                    }
                }
            }
        }

        /// <summary>
        /// Sends the message in front of the queue.
        /// Only sends one message to ensure that the network load remains within reasonable limits.
        /// Called every thread operation cycle, which is at most once every 10 milliseconds, variations depend on scheduling.
        /// </summary>
        private void ManageTxQueue()
        {
            byte[] transmitData;
            if (this.transmitQueue.TryDequeue(out transmitData))
            {
                this.communicationChannel.Write(transmitData);
            }
        }
    }
}

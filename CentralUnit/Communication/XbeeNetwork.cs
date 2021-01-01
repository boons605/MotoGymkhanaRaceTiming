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
    using XBeeLibrary.Core.Models;

    /// <summary>
    /// A network of <c>Xbee</c> devices accessed through a single serial communication channel.
    /// </summary>
    public class XbeeNetwork
    {
        /// <summary>
        /// List of <c>Xbee</c> devices registered with this network.
        /// </summary>
        private List<XbeeSerialCommunication> devices = new List<XbeeSerialCommunication>();

        /// <summary>
        /// The serial communication channel utilized by this network.
        /// </summary>
        private ISerialCommunication communicationChannel;

        /// <summary>
        /// Queue for outgoing messages.
        /// </summary>
        private ConcurrentQueue<byte[]> transmitQueue;

        /// <summary>
        /// Communication thread.
        /// </summary>
        private Thread commThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="XbeeNetwork" /> class.
        /// </summary>
        /// <param name="communication">The serial communication channel utilized by this network</param>
        public XbeeNetwork(ISerialCommunication communication)
        {
            this.communicationChannel = communication;
            this.communicationChannel.ConnectionStateChanged += this.CommunicationChannel_ConnectionStateChanged;
            this.communicationChannel.DataReceived += this.CommunicationChannel_DataReceived;
            this.communicationChannel.Failure += this.CommunicationChannel_Failure;
            this.transmitQueue = new ConcurrentQueue<byte[]>();
            this.commThread = new Thread(this.RunXbeeNetwork) { IsBackground = true };
        }

        /// <summary>
        /// Gets a device from the network or adds this device to the network.
        /// </summary>
        /// <param name="address64bit">The 64-bit address to identify the device, in hex format: (0x)0013A20041BB64A6</param>
        /// <returns>An <see cref="XbeeSerialCommunication"/> communication channel</returns>
        public XbeeSerialCommunication GetDevice(string address64bit)
        {
            XBee64BitAddress address = new XBee64BitAddress(address64bit);
            XbeeSerialCommunication device = null;
            if (this.devices.Any(dev => dev.Xbee64address.Equals(address)))
            {
                device = this.devices.First(dev => dev.Xbee64address.Equals(address));
            }
            else
            {
                device = new XbeeSerialCommunication(address, this);
                this.devices.Add(device);
            }

            return device;
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
        /// <param name="e">EventArgs supplied with the event</param>
        private void CommunicationChannel_DataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles change in communication channel connection state.
        /// Notifies all devices in this network of the new connection state.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">EventArgs supplied with the event</param>
        private void CommunicationChannel_ConnectionStateChanged(object sender, EventArgs e)
        {
            foreach (XbeeSerialCommunication xbee in this.devices)
            {
                xbee.OnConnectionStateChanged(this.communicationChannel.Connected);
            }
        }

        /// <summary>
        /// Thread main method.
        /// </summary>
        private void RunXbeeNetwork()
        {
            DateTime threadStartTime = DateTime.Now;

            // Wait for up to 10 seconds for a connection. This happens on another thread.
            while ((!this.communicationChannel.Connected) &&
                    (DateTime.Now.Subtract(threadStartTime).TotalMilliseconds < 10000))
            {
                Thread.Sleep(100);
            }

            while (this.communicationChannel.Connected)
            {
                this.ManageTxQueue();

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Sends the message in front of the queue.
        /// Only sends one message to ensure that the network load remains within reasonable limits.
        /// Called every thread operation cycle, which is at most once every 10 milliseconds, variations depend on scheduling.
        /// </summary>
        private void ManageTxQueue()
        {
            if (!this.transmitQueue.IsEmpty)
            {
                byte[] transmitData;
                if (this.transmitQueue.TryDequeue(out transmitData))
                {
                    this.communicationChannel.Write(transmitData);
                }
            }
        }
    }
}

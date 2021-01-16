// <copyright file="BLERiderIdUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace RiderIdUnit
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Communication;
    using log4net;
    using Models;

    /// <summary>
    /// Rider ID Unit implementation for the ESP32-based Rider ID unit employing BLE iBeacons for identifying riders.
    /// </summary>
    public class BLERiderIdUnit : IRiderIdUnit, IDisposable
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The protocol handler.
        /// </summary>
        private CommunicationProtocol protocolHandler;

        /// <summary>
        /// A list of known <see cref="Rider"/> objects with <see cref="Beacon"/> object
        /// </summary>
        private List<Rider> knownRiders;

        /// <summary>
        /// The queue of commands.
        /// </summary>
        private ConcurrentQueue<CommandData> commandQueue;

        /// <summary>
        /// The current process state.
        /// </summary>
        private State state = State.Idle;

        /// <summary>
        /// The timer to guard response timeouts.
        /// </summary>
        private System.Timers.Timer timeoutTimer;

        /// <summary>
        /// The retry-sending-command timer.
        /// </summary>
        private System.Timers.Timer retryTimer;

        /// <summary>
        /// The current command being processed.
        /// </summary>
        private CommandData currentCommand;

        /// <summary>
        /// A list of found beacons from either a <see cref="BLERiderIDCommands.DetectAll"/> or <see cref="BLERiderIDCommands.ListAllowed"/>
        /// </summary>
        private List<Beacon> foundBeacons;

        /// <summary>
        /// The maximum detection distance.
        /// </summary>
        private double maxDetectionDistance = 4.0;

        /// <summary>
        /// The closest beacon.
        /// </summary>
        private Beacon closestBeacon;

        /// <summary>
        /// Initializes a new instance of the <see cref="BLERiderIdUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        public BLERiderIdUnit(ISerialCommunication commInterface)
        {
            if (commInterface == null)
            {
                throw new ArgumentNullException("commInterface");
            }

            this.protocolHandler = new CommunicationProtocol(commInterface);
            this.protocolHandler.ConnectionStateChanged += this.ProtocolHandler_ConnectionStateChanged;
            this.protocolHandler.NewDataArrived += this.ProtocolHandler_NewDataArrived;
            this.knownRiders = new List<Rider>();
            this.timeoutTimer = new System.Timers.Timer(500);
            this.retryTimer = new System.Timers.Timer(100);
            this.timeoutTimer.Elapsed += this.TimeoutTimer_Elapsed;
            this.retryTimer.Elapsed += this.RetryTimer_Elapsed;
            this.commandQueue = new ConcurrentQueue<CommandData>();
        }

        /// <inheritdoc/>
        public event EventHandler<RiderIdEventArgs> OnRiderId;

        /// <inheritdoc/>
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        /// <summary>
        /// Process state.
        /// </summary>
        private enum State
        {
            /// <summary>
            /// Idle or running a single step process.
            /// </summary>
            Idle,

            /// <summary>
            /// Adding the list of known riders to the device.
            /// </summary>
            AddingKnownRiders,

            /// <summary>
            /// Clearing the list of known riders.
            /// </summary>
            ClearingKnownRiders
        }

        /// <summary>
        /// BLE rider ID commands.
        /// </summary>
        private enum BLERiderIDCommands
        {
            /// <summary>
            /// Add an allowed beacon.
            /// </summary>
            AddAllowed = 0x0001,

            /// <summary>
            /// Remove an allowed beacon.
            /// </summary>
            DeleteAllowed = 0x0002,

            /// <summary>
            /// List all allowed beacons.
            /// </summary>
            ListAllowed = 0x0003,

            /// <summary>
            /// Detect all beacons
            /// </summary>
            DetectAll = 0x0004,

            /// <summary>
            /// Get the closest beacon.
            /// </summary>
            GetClosest = 0x0005
        }

        /// <summary>
        /// Add a list of known <see cref="Rider"/> object.
        /// </summary>
        /// <param name="riders">The riders to add</param>
        public void AddKnownRiders(List<Rider> riders)
        {
            foreach (Rider rider in riders)
            {
                if (rider.Beacon != null)
                {
                    if (!this.knownRiders.Any(rid => rid.Name == rider.Name))
                    {
                        this.knownRiders.Add(rider);
                        this.commandQueue.Enqueue(this.GenerateAddRiderCommand(rider.Beacon));
                    }
                }
            }

            if (this.state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        /// <summary>
        /// Remove all known riders.
        /// </summary>
        public void ClearKnownRiders()
        {
            this.commandQueue.Enqueue(new CommandData((ushort)BLERiderIDCommands.ListAllowed, 0, new byte[2]));

            if (this.state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        /// <summary>
        /// Remove a known rider.
        /// </summary>
        /// <param name="name">The rider to remove.</param>
        public void RemoveKnownRider(string name)
        {
            if (this.knownRiders.Any(rid => rid.Name == name))
            {
                this.commandQueue.Enqueue(this.GenerateRemoveRiderCommand(this.knownRiders.First(rid => rid.Name == name).Beacon));
            }

            if (this.state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        /// <summary>
        /// Dispose of this object.
        /// </summary>
        public void Dispose()
        {
            this.protocolHandler.Dispose();
        }

        /// <summary>
        /// Generate a 'Add allowed device' command based on a beacon.
        /// </summary>
        /// <param name="b">The <see cref="Beacon"/> to add.</param>
        /// <returns>A <see cref="CommandData"/> object to send to the device via the <see cref="CommunicationProtocol"/></returns>
        private CommandData GenerateAddRiderCommand(Beacon b)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(b.Identifier);
            writer.Write(b.CorrectionFactor);

            return new CommandData((ushort)BLERiderIDCommands.AddAllowed, 0, stream.ToArray());
        }

        /// <summary>
        /// Generate a 'Remove allowed device' command based on a beacon.
        /// </summary>
        /// <param name="b">The <see cref="Beacon"/> to remove.</param>
        /// <returns>A <see cref="CommandData"/> object to send to the device via the <see cref="CommunicationProtocol"/></returns>
        private CommandData GenerateRemoveRiderCommand(Beacon b)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(b.Identifier);
            writer.Write(b.CorrectionFactor);

            return new CommandData((ushort)BLERiderIDCommands.DeleteAllowed, 0, stream.ToArray());
        }

        /// <summary>
        /// Send the next command and set the state of the Rider ID unit.
        /// </summary>
        private void SendNextCommand()
        {
            if (!this.commandQueue.IsEmpty)
            {
                if (this.protocolHandler.ReadyToSend())
                {
                    if (this.commandQueue.TryDequeue(out this.currentCommand))
                    {
                        if (this.currentCommand.CommandType == (ushort)BLERiderIDCommands.ListAllowed)
                        {
                            this.state = State.ClearingKnownRiders;
                            this.foundBeacons = new List<Beacon>();
                        }
                        else if (this.currentCommand.CommandType == (ushort)BLERiderIDCommands.AddAllowed)
                        {
                            this.state = State.AddingKnownRiders;
                        }

                        this.protocolHandler.SendCommand(this.currentCommand);
                        this.timeoutTimer.Start();
                    }
                    else
                    {
                        this.retryTimer.Start();
                    }
                }
                else
                {
                    this.retryTimer.Start();
                }
            }
            else
            {
                this.state = State.Idle;
            }
        }

        /// <summary>
        /// Retry sending a command when the <see cref="CommunicationProtocol"/> wasn't available.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">EventArgs for this timer event.</param>
        private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.retryTimer.Stop();
            this.SendNextCommand();
        }

        /// <summary>
        /// Handle a timeout.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">EventArgs for this timer event.</param>
        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Don't know what to do yet.
            this.timeoutTimer.Stop();
            this.SendNextCommand();
        }

        /// <summary>
        /// Handle new data from the <see cref="CommunicationProtocol"/>
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args, not used.</param>
        private void ProtocolHandler_NewDataArrived(object sender, EventArgs e)
        {
            CommandData packet;
            while ((packet = this.protocolHandler.NextPacket) != null)
            {
                this.timeoutTimer.Stop();
                this.retryTimer.Stop();
                switch (packet.CommandType)
                {
                    case (ushort)BLERiderIDCommands.AddAllowed:
                        this.HandleAddAllowedResponse(packet);
                        break;
                    case (ushort)BLERiderIDCommands.DeleteAllowed:
                        this.HandleRemoveAllowedResponse(packet);
                        break;
                    case (ushort)BLERiderIDCommands.ListAllowed:
                        this.HandleListAllowedDevices(packet);
                        break;
                    case (ushort)BLERiderIDCommands.DetectAll:
                        this.HandleListDetectedDevices(packet);
                        break;
                    case (ushort)BLERiderIDCommands.GetClosest:
                        this.HandleGetClosestDevice(packet);
                        break;
                    default:
                        break;
                }
            }

            this.SendNextCommand();
        }

        /// <summary>
        /// Sets the closest beacon and fires an event if:
        /// - The beacon was not null and in range an the beacon is now null or out of range. <see cref="OnRiderExit"/>
        /// - The beacon was null or out of range and is now not null and in range. <see cref="OnRiderId"/>
        /// </summary>
        /// <param name="b">The <see cref="Beacon"/></param>
        private void SetClosestBeacon(Beacon b)
        {
            if (this.CheckDeviceInRange(b) && (!this.CheckDeviceInRange(this.closestBeacon)))
            {
                // Entered range
                this.OnRiderId?.Invoke(this, null);
            }
            else if ((!this.CheckDeviceInRange(b)) && this.CheckDeviceInRange(this.closestBeacon))
            {
                // Left range
                this.OnRiderExit?.Invoke(this, null);
            }

            this.closestBeacon = b;
        }

        /// <summary>
        /// Checks if a <see cref="Beacon"/> <paramref name="b"/> is at a distance of less than <see cref="maxDetectionDistance"/>.
        /// If <paramref name="b"/> is null, it is considered not in range.
        /// </summary>
        /// <param name="b">The beacon to check.</param>
        /// <returns>true if <paramref name="b"/> is non-null and in range, false otherwise.</returns>
        private bool CheckDeviceInRange(Beacon b)
        {
            bool retVal = false;

            if (b != null)
            {
                if (b.Distance < this.maxDetectionDistance)
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Handle a 'Get closest device' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleGetClosestDevice(CommandData packet)
        {
            if (packet.Status != 0)
            {
                Log.Info($"GetClosestDevice returned {packet.Status}");
                this.SetClosestBeacon(null);
            }
            else
            {
                List<Beacon> beacons = RiderIdUnit.RiderIDCommandDataParser.ParseClosestDeviceResponse(packet.Status, packet.Data);
                foreach (Beacon b in beacons)
                {
                    Log.Info($"Got closest device: {b.ToString()}");
                    this.SetClosestBeacon(b);
                }
            }
        }

        /// <summary>
        /// Handle a 'List all detected devices' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleListDetectedDevices(CommandData packet)
        {
            try
            {
                if ((packet.Status == 1) && (packet.Data.Length > 2))
                {
                    byte[] beaconData = new byte[packet.Data.Length - 2];
                    packet.Data.CopyTo(beaconData, 2);
                    this.foundBeacons.AddRange(RiderIDCommandDataParser.ParseClosestDeviceResponse(0, beaconData));
                    if (packet.Data[0] == (packet.Data[1] - 1))
                    {
                        foreach (Beacon b in this.foundBeacons)
                        {
                            Log.Info($"Found beacon {b}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {this.currentCommand.CommandType} command", ex);
            }
        }

        /// <summary>
        /// Handle a 'List all allowed devices' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleListAllowedDevices(CommandData packet)
        {
            try
            {
                if ((packet.Status == 1) && (packet.Data.Length > 2))
                {
                    byte[] beaconData = new byte[packet.Data.Length - 2];
                    packet.Data.CopyTo(beaconData, 2);
                    this.foundBeacons.AddRange(RiderIDCommandDataParser.ParseClosestDeviceResponse(0, beaconData));
                    if (packet.Data[0] == (packet.Data[1] - 1))
                    {
                        foreach (Beacon b in this.foundBeacons)
                        {
                            this.commandQueue.Enqueue(this.GenerateRemoveRiderCommand(b));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {this.currentCommand.CommandType} command", ex);
            }
        }

        /// <summary>
        /// Handle a 'Remove allowed device' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleRemoveAllowedResponse(CommandData packet)
        {
            try
            {
                Beacon receivedBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(packet.Status, packet.Data);
                Beacon sentBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(this.currentCommand.Status, this.currentCommand.Data);

                if ((packet.Status == 0) && sentBeacon.Equals(receivedBeacon))
                {
                    this.knownRiders.RemoveAll(rid => rid.Beacon.Equals(sentBeacon));
                }
                else
                {
                    Log.Warn("Failure while removing rider");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {this.currentCommand.CommandType} command", ex);
            }
        }

        /// <summary>
        /// Handle a 'Add allowed device' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleAddAllowedResponse(CommandData packet)
        {
            try
            {
                Beacon receivedBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(packet.Status, packet.Data);
                Beacon sentBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(this.currentCommand.Status, this.currentCommand.Data);

                if ((packet.Status == 0) && sentBeacon.Equals(receivedBeacon))
                {
                    if (this.knownRiders.Any(rid => rid.Beacon.Equals(receivedBeacon)))
                    {
                        Log.Info($"Successfully added rider {this.knownRiders.First(rid => rid.Beacon.Equals(receivedBeacon)).Name} with beacon {receivedBeacon}");
                    }                    
                }
                else
                {
                    Log.Warn("Failure while adding rider");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {this.currentCommand.CommandType} command", ex);
            }
        }

        /// <summary>
        /// Handle a connection state change of the protocol handler.
        /// </summary>
        /// <param name="sender">The protocol handler.</param>
        /// <param name="e">Event args containing the data for this event.</param>
        private void ProtocolHandler_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (!e.Connected)
            {
                this.state = State.Idle;
                CommandData cmd;
                while (this.commandQueue.TryDequeue(out cmd))
                {
                    Log.Info($"Clearing command from queue due to disconnect {cmd}");
                }
            }
        }
    }
}

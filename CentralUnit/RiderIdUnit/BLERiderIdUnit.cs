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
    public class BLERiderIdUnit : AbstractCommunicatingUnit, IRiderIdUnit
    {
        /// <summary>
        /// A list of known <see cref="Rider"/> objects with <see cref="Beacon"/> object
        /// </summary>
        private List<Rider> knownRiders;

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
        /// Queue of events.
        /// </summary>
        private ConcurrentQueue<RiderIDQueuedEvent> eventQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BLERiderIdUnit" /> class based with a specific serial channel.
        /// Using a default max detection distance of 4 meter.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        /// <param name="unitId">The unit name</param>
        public BLERiderIdUnit(ISerialCommunication commInterface, string unitId) : this(commInterface, unitId, 4.0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BLERiderIdUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        /// <param name="unitId">The unit name</param>
        /// <param name="distanceLimit">The distance in meter within which a beacon must be to be considered in range.</param>
        public BLERiderIdUnit(ISerialCommunication commInterface, string unitId, double distanceLimit) : base(commInterface, unitId)
        {
            this.knownRiders = new List<Rider>();
            this.eventQueue = new ConcurrentQueue<RiderIDQueuedEvent>();
            this.maxDetectionDistance = distanceLimit;
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
        }

        /// <summary>
        /// Remove all known riders.
        /// </summary>
        public void ClearKnownRiders()
        {
            this.commandQueue.Enqueue(new CommandData((ushort)BLERiderIDCommands.ListAllowed, 0, new byte[2]));
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
        }

        /// <summary>
        /// Event dispatcher and command thread.
        /// </summary>
        protected override void RunEventThread()
        {
            try
            {
                while (this.keepEventThreadAlive)
                {
                    while (this.eventQueue.TryDequeue(out RiderIDQueuedEvent evt))
                    {
                        if (evt.Type == RiderIDQueuedEvent.RiderIdQueuedEventType.Entered)
                        {
                            this.OnRiderId?.Invoke(this, evt.EventArgs);
                        }
                        else if (evt.Type == RiderIDQueuedEvent.RiderIdQueuedEventType.Exit)
                        {
                            this.OnRiderExit?.Invoke(this, evt.EventArgs);
                        }
                        else
                        {
                            Log.Error($"Got illegal type of RiderIDQueuedEvent: {evt.Type}");
                        }
                    }

                    while (this.protocolHandler.ReadyToSend() &&
                             (!this.commandQueue.IsEmpty))
                    {
                        CommandData command;
                        if (this.commandQueue.TryDequeue(out command))
                        {
                            if (command.CommandType == (ushort)BLERiderIDCommands.ListAllowed)
                            {
                                this.foundBeacons = new List<Beacon>();
                            }

                            this.protocolHandler.SendCommand(command);
                        }
                    }

                    Thread.Sleep(20);
                }

                Log.Info($"Event thread ended for unit {this.unitId}");
            }
            catch (Exception ex)
            {
                Log.Error($"Exception on thread for {this.unitId}", ex);
                this.OnThreadException(ex);
            }
        }

        /// <inheritdoc/>
        protected override void ProcessPacket(CommandData packet)
        {
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
                    Log.Error($"Got invalid packet {packet}");
                    break;
            }
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
        /// Sets the closest beacon and fires an event if:
        /// - The beacon was not null and in range an the beacon is now null or out of range. <see cref="OnRiderExit"/>
        /// - The beacon was null or out of range and is now not null and in range. <see cref="OnRiderId"/>
        /// </summary>
        /// <param name="b">The <see cref="Beacon"/></param>
        private void SetClosestBeacon(Beacon b)
        {
            if (knownRiders.Any(rid => rid.Beacon.Equals(b)))
            {
                if (this.CheckDeviceInRange(b) && (!this.CheckDeviceInRange(this.closestBeacon)))
                {
                    // Entered range
                    this.eventQueue.Enqueue(new RiderIDQueuedEvent(
                                                    new RiderIdEventArgs(this.knownRiders.First(rid => rid.Beacon.Equals(b)), DateTime.Now, this.unitId),
                                                    RiderIDQueuedEvent.RiderIdQueuedEventType.Entered));
                }
                else if ((!this.CheckDeviceInRange(b)) && this.CheckDeviceInRange(this.closestBeacon))
                {
                    // Left range
                    this.eventQueue.Enqueue(new RiderIDQueuedEvent(
                                                    new RiderIdEventArgs(this.knownRiders.First(rid => rid.Beacon.Equals(b)), DateTime.Now, this.unitId),
                                                    RiderIDQueuedEvent.RiderIdQueuedEventType.Exit));
                }

                this.closestBeacon = b;
            }
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
                if (this.closestBeacon != null)
                {
                    Log.Info($"GetClosestDevice returned {packet.Status}");
                }

                this.SetClosestBeacon(null);
            }
            else
            {
                List<Beacon> beacons = RiderIdUnit.RiderIDCommandDataParser.ParseClosestDeviceResponse(packet.Status, packet.Data);
                foreach (Beacon b in beacons)
                {
                    if (!b.Equals(this.closestBeacon))
                    {
                        Log.Info($"Got closest device: {b}");
                    }

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
                            Log.Debug($"Found beacon {b}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {packet.CommandType} command", ex);
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
                Log.Error($"Received bad response for {packet.CommandType} command", ex);
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

                if (packet.Status == 0)
                {
                    this.knownRiders.RemoveAll(rid => rid.Beacon.Equals(receivedBeacon));
                }
                else
                {
                    Log.Warn("Failure while removing rider");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {packet.CommandType} command", ex);
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

                if (packet.Status == 0)
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
                Log.Error($"Received bad response for {packet.CommandType} command", ex);
            }
        }
    }
}

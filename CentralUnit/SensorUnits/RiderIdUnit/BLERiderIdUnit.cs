// <copyright file="BLERiderIdUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.RiderIdUnit
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
    using StartLightUnit;

    /// <summary>
    /// Rider ID Unit implementation for the ESP32-based Rider ID unit employing BLE iBeacons for identifying riders.
    /// </summary>
    public class BLERiderIdUnit : AbstractCommunicatingUnit, IRiderIdUnit, IStartLightUnit
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
        /// The closest rider.
        /// </summary>
        private Rider closestRider;

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
        /// <param name="token">The cancellation token for this unit</param>
        public BLERiderIdUnit(ISerialCommunication commInterface, string unitId, CancellationToken token) : this(commInterface, unitId, 4.0, token)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BLERiderIdUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        /// <param name="unitId">The unit name</param>
        /// <param name="distanceLimit">The distance in meter within which a beacon must be to be considered in range.</param>
        /// <param name="token">The cancellation token for this unit</param>
        public BLERiderIdUnit(ISerialCommunication commInterface, string unitId, double distanceLimit, CancellationToken token) : base(commInterface, unitId, token)
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
            GetClosest = 0x0005,

            /// <summary>
            /// Set the start light color.
            /// </summary>
            SetStartColor = 0x0006,

            /// <summary>
            /// Clear all allowed riders.
            /// </summary>
            ClearAllowed = 0x0007
        }

        /// <summary>
        /// The ID for this unit, i.e. startUnit or finishUnit
        /// </summary>
        public string UnitId => this.unitId;

        public Rider Closest => this.closestRider;

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
                        Log.Info($"{this.unitId}: Adding new known rider {rider}");
                        this.knownRiders.Add(rider);
                        this.commandQueue.Enqueue(this.GenerateAddRiderCommand(rider.Beacon));
                    }
                    else
                    {
                        Log.Info($"{this.unitId}: Known rider {rider} already exists");
                    }
                }
                else
                {
                    Log.Warn($"{this.unitId}: Can't add rider {rider} without a beacon");
                }
            }
        }

        /// <summary>
        /// Remove all known riders.
        /// </summary>
        public void ClearKnownRiders()
        {
            Log.Info($"{this.unitId}: Clearing known riders");
            this.commandQueue.Enqueue(new CommandData((ushort)BLERiderIDCommands.ClearAllowed, 0, new byte[2]));
        }

        /// <summary>
        /// Remove a known rider.
        /// </summary>
        /// <param name="name">The rider to remove.</param>
        public void RemoveKnownRider(string name)
        {
            if (this.knownRiders.Any(rid => rid.Name == name))
            {
                Rider r = this.knownRiders.First(rid => rid.Name == name);
                Log.Info($"{this.unitId}: Removing known rider {r}");
                this.commandQueue.Enqueue(this.GenerateRemoveRiderCommand(r.Beacon));
            }
            else
            {
                Log.Warn($"{this.unitId}: Can not remove rider with name {name}, it does not exist");
            }
        }

        /// <inheritdoc/>
        public void SetStartLightColor(StartLightColor color)
        {
            byte colorByte;
            switch (color)
            {
                case StartLightColor.RED:
                    colorByte = 0x04;
                    break;
                case StartLightColor.YELLOW:
                    colorByte = 0x02;
                    break;
                case StartLightColor.GREEN:
                    colorByte = 0x01;
                    break;
                default:
                    colorByte = 0x00;
                    break;
            }

            this.commandQueue.Enqueue(new CommandData((ushort)BLERiderIDCommands.SetStartColor, 0, new byte[] { colorByte }));
        }

        /// <summary>
        /// Event dispatcher and command thread.
        /// </summary>
        protected override void RunEventThread()
        {
            Log.Info($"{this.unitId}:Event thread started");
            try
            {
                while (this.keepEventThreadAlive && (!this.cancellationToken.IsCancellationRequested))
                {
                    while (this.eventQueue.TryDequeue(out RiderIDQueuedEvent evt))
                    {
                        if (evt.EventArgs.IdType == Direction.Enter)
                        {
                            this.OnRiderId?.Invoke(this, evt.EventArgs);
                        }
                        else if (evt.EventArgs.IdType == Direction.Exit)
                        {

                            this.OnRiderExit?.Invoke(this, evt.EventArgs);
                        }
                        else
                        {
                            Log.Error($"{this.unitId}: Got illegal type of RiderIDQueuedEvent: {evt.EventArgs.IdType}");
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
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}:Exception on thread for {this.unitId}", ex);
                this.OnThreadException(ex);
            }

            if (this.cancellationToken.IsCancellationRequested)
            {
                Log.Info($"{this.unitId}:Event thread ended for unit {this.unitId} because cancellation was requested");
            }
            else if (!this.keepEventThreadAlive)
            {
                Log.Info($"{this.unitId}:Event thread ended for unit {this.unitId} because event thread was not kept alive");
            }
            else
            {
                Log.Info($"{this.unitId}:Event thread ended for unit {this.unitId} for other reasons");
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
                case (ushort)BLERiderIDCommands.SetStartColor:
                    this.HandleSetStartColorResponse(packet);
                    break;
                case (ushort)BLERiderIDCommands.ClearAllowed:
                    this.HandleClearAllowed(packet);
                    break;
                default:
                    Log.Error($"{this.unitId}:Got invalid packet {packet}");
                    break;
            }
        }

        /// <summary>
        /// Handle a 'Clear all allowed' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleClearAllowed(CommandData packet)
        {
            try
            {
                if (packet.Status == 0)
                {
                    Log.Info($"{this.unitId}: Cleared allowed");
                    this.knownRiders.Clear();
                }
                else
                {
                    Log.Warn($"{this.unitId}: Failure while clearing known riders, got packet status {packet.Status}");
                }
                
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}:Received bad response for {packet.CommandType} command", ex);
            }
        }

        /// <summary>
        /// Handle a 'Set start light color' response.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        private void HandleSetStartColorResponse(CommandData packet)
        {
            try
            {
                Log.Info($"{this.unitId}:Set color to {packet.Data[0]}");
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}:Received bad response for {packet.CommandType} command", ex);
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
        /// - The rider was not null and in range an the beacon is now null or out of range. <see cref="OnRiderExit"/>
        /// - The beacon is null or not linked to a known rider, and a rider is currently in range. <see cref="OnRiderExit"/>
        /// - The rider was null or out of range and is now not null and in range. <see cref="OnRiderId"/>
        /// </summary>
        /// <param name="b">The <see cref="Beacon"/></param>
        private void SetClosestRider(Beacon b)
        {
            if (this.knownRiders.Any(rid => rid.Beacon.Equals(b)))
            {
                Rider newClosest = this.knownRiders.First(rid => rid.Beacon.Equals(b));

                if (this.CheckBeaconInRange(b) && (!this.CheckBeaconInRange(this.closestRider?.Beacon)))
                {
                    // Entered range
                    newClosest.Beacon.UpdateFromReceivedBeacon(b);
                    Log.Info($"{this.unitId}:Enqueueing Rider ID entered range event for {newClosest}");
                    this.eventQueue.Enqueue(new RiderIDQueuedEvent(new RiderIdEventArgs(newClosest, DateTime.Now, this.unitId ,Direction.Enter)));
                }
                else if ((!this.CheckBeaconInRange(b)) && this.CheckBeaconInRange(this.closestRider?.Beacon))
                {
                    // Left range
                    newClosest.Beacon.UpdateFromReceivedBeacon(b);
                    Log.Info($"{this.unitId}:Enqueueing Rider ID left range event for {newClosest}");
                    this.eventQueue.Enqueue(new RiderIDQueuedEvent(new RiderIdEventArgs(newClosest, DateTime.Now, this.unitId, Direction.Exit)));
                }

                this.closestRider = newClosest;
            }
            else if (this.closestRider != null)
            {
                // Left range
                Log.Info($"{this.unitId}:Enqueueing Rider ID left range event for {this.closestRider}");
                this.eventQueue.Enqueue(new RiderIDQueuedEvent(new RiderIdEventArgs(this.closestRider, DateTime.Now, this.unitId, Direction.Exit)));

                this.closestRider = null;
            }
        }

        /// <summary>
        /// Checks if a <see cref="Rider"/> <paramref name="r"/> is at a distance of less than <see cref="maxDetectionDistance"/>.
        /// If <paramref name="r"/> is null, it is considered not in range.
        /// </summary>
        /// <param name="b">The beacon to check.</param>
        /// <returns>true if <paramref name="b"/> is non-null and in range, false otherwise.</returns>
        private bool CheckBeaconInRange(Beacon b)
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
                if (this.closestRider != null)
                {
                    Log.Info($"{this.unitId}: GetClosestDevice returned {packet.Status}");
                }

                this.SetClosestRider(null);
            }
            else
            {
                List<Beacon> beacons = RiderIdUnit.RiderIDCommandDataParser.ParseClosestDeviceResponse(packet.Status, packet.Data);
                foreach (Beacon b in beacons)
                {
                    if (!b.Equals(this.closestRider?.Beacon))
                    {
                        Log.Info($"{this.unitId}: Got closest device: {b}");
                    }

                    this.SetClosestRider(b);
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
                            Log.Debug($"{this.unitId}:Found beacon {b}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}:Received bad response for {packet.CommandType} command", ex);
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
                Log.Error($"{this.unitId}:Received bad response for {packet.CommandType} command", ex);
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
                    Log.Info($"{this.unitId}: Succesfully removed Beacon {receivedBeacon}");
                    this.knownRiders.RemoveAll(rid => rid.Beacon.Equals(receivedBeacon));
                }
                else
                {
                    switch (packet.Status)
                    {
                        case (ushort)0xFFFFU:
                            Log.Warn($"{this.unitId}: Failure while removing rider with beacon {receivedBeacon}, device not found");
                            break;
                        default:
                            Log.Warn($"{this.unitId}: Failure while removing rider, got packet status {packet.Status}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}: Received bad response for {packet.CommandType} command", ex);
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
                        Log.Info($"{this.unitId}: Successfully added rider {this.knownRiders.First(rid => rid.Beacon.Equals(receivedBeacon)).Name} with beacon {receivedBeacon}");
                    }
                    else
                    {
                        Log.Warn($"{this.unitId}: Successfully added beacon {receivedBeacon}, but no rider was found with that beacon. Sending remove command");
                        this.commandQueue.Enqueue(this.GenerateRemoveRiderCommand(receivedBeacon));
                    }
                }
                else
                {
                    switch (packet.Status)
                    {
                        case (ushort)0xFFFFU:
                            Log.Warn($"{this.unitId}: Failure while adding rider with beacon {receivedBeacon}, device already exists");
                            break;
                        case (ushort)0xFFFEU:
                            Log.Warn($"{this.unitId}: Failure while adding rider with beacon {receivedBeacon}, maximum number of devices reached");
                            break;
                        default:
                            Log.Warn($"{this.unitId}: Failure while adding rider, got packet status {packet.Status}");
                            break;
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{this.unitId}:Received bad response for {packet.CommandType} command", ex);
            }
        }
    }
}

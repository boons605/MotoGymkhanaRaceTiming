// <copyright file="BLERiderIdUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace RiderIdUnit
{
    using Models;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Communication;
    using log4net;

    /// <summary>
    /// Rider ID Unit implementation for the ESP32-based Rider ID unit employing BLE iBeacons for identifying riders.
    /// </summary>
    public class BLERiderIdUnit : IRiderIdUnit, IDisposable
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler OnRiderId;
        public event EventHandler OnRiderExit;

        private AutoResetEvent waitHandle;

        private CommunicationProtocol protocolHandler;

        private List<Rider> knownRiders;

        private ConcurrentQueue<CommandData> commandQueue;

        private State state = State.Idle;

        private System.Timers.Timer timeoutTimer;

        private System.Timers.Timer retryTimer;

        private CommandData currentCommand;

        List<Beacon> foundBeacons;

        public BLERiderIdUnit(ISerialCommunication commInterface)
        {
            if (commInterface == null)
            {
                throw new ArgumentNullException("commInterface");
            }

            waitHandle = new AutoResetEvent(false);
            protocolHandler = new CommunicationProtocol(commInterface);
            protocolHandler.ConnectionStateChanged += ProtocolHandler_ConnectionStateChanged;
            protocolHandler.NewDataArrived += ProtocolHandler_NewDataArrived;
            knownRiders = new List<Rider>();
            this.timeoutTimer = new System.Timers.Timer(500);
            this.retryTimer = new System.Timers.Timer(100);
            this.timeoutTimer.Elapsed += TimeoutTimer_Elapsed;
            this.retryTimer.Elapsed += RetryTimer_Elapsed;
            commandQueue = new ConcurrentQueue<CommandData>();
        }



        private enum State
        {
            Idle,
            AddingKnownRiders,
            ClearingKnownRiders
        };

        private enum BLERiderIDCommands
        {
            AddAllowed = 0x0001,
            DeleteAllowed = 0x0002,
            ListAllowed = 0x0003,
            DetectAll = 0x0004,
            GetClosest = 0x0005
        };

        public void AddKnownRiders(List<Rider> riders)
        {
            foreach (Rider rider in riders)
            {
                if (rider.Beacon != null)
                {
                    if (!knownRiders.Any(rid => rid.Name == rider.Name))
                    {
                        knownRiders.Add(rider);
                        commandQueue.Enqueue(GenerateAddRiderCommand(rider.Beacon));
                    }
                }
            }

            if (state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        public void ClearKnownRiders()
        {
            commandQueue.Enqueue(new CommandData((ushort)BLERiderIDCommands.ListAllowed, 0, new byte[2]));

            if (state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        public void RemoveKnownRider(string name)
        {
            if (knownRiders.Any(rid => rid.Name == name))
            {
                commandQueue.Enqueue(GenerateRemoveRiderCommand(knownRiders.First(rid => rid.Name == name).Beacon));
            }

            if (state == State.Idle)
            {
                this.SendNextCommand();
            }
        }

        public void Dispose()
        {
            this.protocolHandler.Dispose();
        }

        private CommandData GenerateAddRiderCommand(Beacon b)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(b.Identifier);
            writer.Write(b.CorrectionFactor);

            return new CommandData((ushort)BLERiderIDCommands.AddAllowed, 0, stream.ToArray());
        }

        private CommandData GenerateRemoveRiderCommand(Beacon b)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(b.Identifier);
            writer.Write(b.CorrectionFactor);

            return new CommandData((ushort)BLERiderIDCommands.DeleteAllowed, 0, stream.ToArray());
        }

        private void SendNextCommand()
        {
            if (!this.commandQueue.IsEmpty)
            {
                if (this.protocolHandler.ReadyToSend())
                {
                    if (commandQueue.TryDequeue(out currentCommand))
                    {
                        if (currentCommand.CommandType == (ushort)BLERiderIDCommands.ListAllowed)
                        {
                            state = State.ClearingKnownRiders;
                            foundBeacons = new List<Beacon>();
                        }
                        else if (currentCommand.CommandType == (ushort)BLERiderIDCommands.AddAllowed)
                        {
                            state = State.AddingKnownRiders;
                        }
                        protocolHandler.SendCommand(currentCommand);
                        timeoutTimer.Start();
                    }
                    else
                    {
                        retryTimer.Start();
                    }
                }
                else
                {
                    retryTimer.Start();
                }
            }
            else
            {
                this.state = State.Idle;
            }
        }

        private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            retryTimer.Stop();
            SendNextCommand();

        }

        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Don't know what to do yet.
            timeoutTimer.Stop();
            SendNextCommand();
        }

        private void ProtocolHandler_NewDataArrived(object sender, EventArgs e)
        {
            CommandData packet;
            while ((packet = protocolHandler.NextPacket) != null)
            {
                timeoutTimer.Stop();
                retryTimer.Stop();
                switch (packet.CommandType)
                {
                    case (ushort)BLERiderIDCommands.AddAllowed:
                        HandleAddAllowedResponse(packet);
                        break;
                    case (ushort)BLERiderIDCommands.DeleteAllowed:
                        HandleRemoveAllowedResponse(packet);
                        break;
                    case (ushort)BLERiderIDCommands.ListAllowed:
                        HandleListAllowedDevices(packet);
                        break;
                    case (ushort)BLERiderIDCommands.DetectAll:
                        HandleListDetectedDevices(packet);
                        break;
                    case (ushort)BLERiderIDCommands.GetClosest:
                        HandleGetClosestDevice(packet);
                        break;
                    default:
                        break;
                }
            }

            SendNextCommand();
        }

        private void HandleGetClosestDevice(CommandData packet)
        {
            //throw new NotImplementedException();
        }

        private void HandleListDetectedDevices(CommandData packet)
        {
            try
            {
                if ((packet.Status == 1) && (packet.Data.Length > 2))
                {
                    byte[] beaconData = new byte[packet.Data.Length - 2];
                    packet.Data.CopyTo(beaconData, 2);
                    foundBeacons.AddRange(RiderIDCommandDataParser.ParseClosestDeviceResponse(0, beaconData));
                    if (packet.Data[0] == (packet.Data[1] - 1))
                    {
                        foreach (Beacon b in foundBeacons)
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

        private void HandleListAllowedDevices(CommandData packet)
        {
            try
            {
                if ((packet.Status == 1) && (packet.Data.Length > 2))
                {
                    byte[] beaconData = new byte[packet.Data.Length - 2];
                    packet.Data.CopyTo(beaconData, 2);
                    foundBeacons.AddRange(RiderIDCommandDataParser.ParseClosestDeviceResponse(0, beaconData));
                    if (packet.Data[0] == (packet.Data[1] - 1))
                    {
                        foreach (Beacon b in foundBeacons)
                        {
                            commandQueue.Enqueue(GenerateRemoveRiderCommand(b));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Received bad response for {this.currentCommand.CommandType} command", ex);
            }
        }

        private void HandleRemoveAllowedResponse(CommandData packet)
        {
            try
            {
                Beacon receivedBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(packet.Status, packet.Data);
                Beacon sentBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(this.currentCommand.Status, this.currentCommand.Data);

                if ((packet.Status == 0) && (sentBeacon.Equals(receivedBeacon)))
                {
                    knownRiders.RemoveAll(rid => rid.Beacon.Equals(sentBeacon));
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

        private void HandleAddAllowedResponse(CommandData packet)
        {
            try
            {
                Beacon receivedBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(packet.Status, packet.Data);
                Beacon sentBeacon = RiderIDCommandDataParser.ParseAllowedDeviceOperationResponse(this.currentCommand.Status, this.currentCommand.Data);

                if ((packet.Status == 0) && (sentBeacon.Equals(receivedBeacon)))
                {
                    if (knownRiders.Any(rid => rid.Beacon.Equals(receivedBeacon)))
                    {
                        Log.Info($"Successfully added rider {knownRiders.First(rid => rid.Beacon.Equals(receivedBeacon)).Name} with beacon {receivedBeacon}");
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

        private void ProtocolHandler_ConnectionStateChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}

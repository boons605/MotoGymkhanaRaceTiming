// <copyright file="SerialTimingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.TimingUnit
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Communication;
    using DisplayUnit;
    using log4net;
    using Models;

    /// <summary>
    /// Implementation of interface <see cref="ITimingUnit"/> as built by <c>Romke Boonstra for MGNL</c> purposes.
    /// </summary>
    public class SerialTimingUnit : AbstractCommunicatingUnit, ITimingUnit, IDisplayUnit
    {
        /// <summary>
        /// Events waiting to be dispatched.
        /// </summary>
        private ConcurrentQueue<TimingTriggeredEventArgs> timingEvents;

        public int StartId { get; private set; }

        public int EndId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialTimingUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        /// <param name="unitId">The name of the timing unit</param>
        /// <param name="token">The cancellation token for this unit</param>
        public SerialTimingUnit(ISerialCommunication commInterface, string unitId, CancellationToken token, int startId, int endId) : base(commInterface, unitId, token)
        {
            this.timingEvents = new ConcurrentQueue<TimingTriggeredEventArgs>();
            this.StartId = startId;
            this.EndId = endId;
            this.commandQueue.Enqueue(new CommandData((ushort)SerialTimerCommands.UpdateOpMode, 0, new byte[] { 2 }));
        }

        /// <inheritdoc/>
        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        /// <summary>
        /// Serial timer commands.
        /// </summary>
        private enum SerialTimerCommands
        {
            /// <summary>
            /// Latest timestamp
            /// </summary>
            GetLatestTimeStamp = 101,

            /// <summary>
            /// All laps
            /// </summary>
            GetAllLaps = 102,

            /// <summary>
            /// Current Time
            /// </summary>
            GetCurrentTime = 103,

            /// <summary>
            /// Update Display Response
            /// </summary>
            UpdateDisplayedTime = 104,

            /// <summary>
            /// Update timer operation mode.
            /// </summary>
            UpdateOpMode = 105,

            /// <summary>
            /// Get device ID response
            /// </summary>
            GetIdentification = 255
        }

        /// <inheritdoc/>
        public void SetDisplayTime(int milliSeconds)
        {
            byte[] commandData = BitConverter.GetBytes(milliSeconds);
            if (milliSeconds == 0)
            {
                //Display can handle max 9:59.999, so 10:00.000 should overflow into .000 to clear the display
                commandData = BitConverter.GetBytes(600000);
            }
            this.commandQueue.Enqueue(new CommandData((ushort)SerialTimerCommands.UpdateDisplayedTime, 0, commandData));
        }

        /// <inheritdoc/>
        protected override void RunEventThread()
        {
            try
            {
                while (this.keepEventThreadAlive && (!this.cancellationToken.IsCancellationRequested))
                {
                    TimingTriggeredEventArgs timingEvent;
                    while (this.timingEvents.TryDequeue(out timingEvent))
                    {
                        Log.Info($"Triggering timing event for {unitId}");
                        this.OnTrigger?.Invoke(this, timingEvent);
                        Log.Info($"Triggered timing event for {unitId}");
                    }

                    while (this.protocolHandler.ReadyToSend() &&
                             (!this.commandQueue.IsEmpty))
                    {
                        CommandData command;
                        if (this.commandQueue.TryDequeue(out command))
                        {
                            this.protocolHandler.SendCommand(command);
                        }
                    }

                    Thread.Sleep(20);
                }

                Log.Warn($"Event thread ended for unit {unitId}");
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
                case (ushort)SerialTimerCommands.GetLatestTimeStamp:
                    this.HandleGetLatestTimestamp(packet);
                    break;
                case (ushort)SerialTimerCommands.GetCurrentTime:
                    this.HandleGetCurrentTime(packet);
                    break;
                case (ushort)SerialTimerCommands.UpdateDisplayedTime:
                    this.HandleUpdateDisplayedTime(packet);
                    break;
                case (ushort)SerialTimerCommands.UpdateOpMode:
                    this.HandleUpdateOpMode(packet);
                    break;
                case (ushort)SerialTimerCommands.GetAllLaps:
                    this.HandleGetAllLaps(packet);
                    break;
                case (ushort)SerialTimerCommands.GetIdentification:
                    this.HandleGetIdentification(packet);
                    break;
                default:
                    Log.Error($"Got invalid packet {packet}");
                    break;
            }
        }


        /// <summary>
        /// Handles Get Identification responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetIdentification(CommandData packet)
        {
            Log.Debug($"Got ID packet for timer: {packet}");
        }

        /// <summary>
        /// Handles Get All Laps responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetAllLaps(CommandData packet)
        {
            Log.Debug($"Got laps response for timer: {packet}");
        }

        /// <summary>
        /// Handles Update Operation Mode responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleUpdateOpMode(CommandData packet)
        {
            Log.Debug($"Got update op mode response for timer: {packet}");
        }

        /// <summary>
        /// Handles Update Display responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleUpdateDisplayedTime(CommandData packet)
        {
            Log.Debug($"Got update display response for timer: {packet}");
        }

        /// <summary>
        /// Handles Get Current Time responses.
        /// Not relevant, only logging. This message is sent periodically as a means of 'keep alive'
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetCurrentTime(CommandData packet)
        {
            Log.Debug($"Got current time for timer: {packet}");
        }

        /// <summary>
        /// Handles Timestamps from the timer.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Used for communication, exact sizing required.")]
        private void HandleGetLatestTimestamp(CommandData packet)
        {
            Log.Info($"Got timestamp for timer: {packet}");
            try
            {
                if (packet.DataLength >= 5)
                {
                    var reader = new BinaryReader(new MemoryStream(packet.Data));
                    UInt32 micros = reader.ReadUInt32();
                    byte gateId = reader.ReadByte();

                    if ((int)gateId != StartId && gateId != EndId)
                    {
                        throw new ArgumentException($"Timing sensor reported gate id: {gateId}, expeted either {StartId} or {EndId}");
                    }

                    this.timingEvents.Enqueue(new TimingTriggeredEventArgs(micros, gateId));
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception while processing timestamp packet", ex);
            }
        }
    }
}

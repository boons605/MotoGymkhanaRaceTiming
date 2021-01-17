// <copyright file="SerialTimingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace TimingUnit
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
    public class SerialTimingUnit : ITimingUnit, IDisplayUnit, ICommunicatingUnit, IDisposable
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
        /// Thread to dispatch events on, to prevent blocking communication thread.
        /// </summary>
        private Thread eventThread;

        /// <summary>
        /// Events waiting to be dispatched.
        /// </summary>
        private ConcurrentQueue<TimingTriggeredEventArgs> timingEvents;

        /// <summary>
        /// Commands waiting to be sent.
        /// </summary>
        private ConcurrentQueue<CommandData> commandQueue;

        /// <summary>
        /// Timer to monitor the communication timeouts.
        /// </summary>
        private System.Timers.Timer commTimeoutTimer = new System.Timers.Timer(2000);

        /// <summary>
        /// Flag that keeps the thread alive.
        /// </summary>
        private bool keepEventThreadAlive = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialTimingUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>>
        public SerialTimingUnit(ISerialCommunication commInterface)
        {
            if (commInterface == null)
            {
                throw new ArgumentNullException("commInterface");
            }

            this.protocolHandler = new CommunicationProtocol(commInterface);
            this.protocolHandler.ConnectionStateChanged += this.ProtocolHandler_ConnectionStateChanged;
            this.protocolHandler.NewDataArrived += this.ProtocolHandler_NewDataArrived;
            this.eventThread = new Thread(this.RunEventThread);
            this.timingEvents = new ConcurrentQueue<TimingTriggeredEventArgs>();
            this.commandQueue = new ConcurrentQueue<CommandData>();
            this.commTimeoutTimer.Elapsed += this.CommTimeoutTimer_Elapsed;
        }

        /// <inheritdoc/>
        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        /// <inheritdoc/>
        public event EventHandler<CommunicationFailureEventArgs> CommunicationFailure;

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

        /// <summary>
        /// Dispose of this unit.
        /// Sets <see cref="keepEventThreadAlive"/> to false and disposes of <see cref="protocolHandler"/>.
        /// </summary>
        public void Dispose()
        {
            this.keepEventThreadAlive = false;
            this.protocolHandler.Dispose();
        }

        /// <inheritdoc/>
        public void SetDisplayTime(int milliSeconds)
        {
            this.commandQueue.Enqueue(new CommandData((ushort)SerialTimerCommands.UpdateDisplayedTime, 0, BitConverter.GetBytes(milliSeconds)));
        }

        /// <summary>
        /// Called when <see cref="commTimeoutTimer"/> has elapsed.
        /// </summary>
        /// <param name="sender">Reference to the event sender.</param>
        /// <param name="e">The timer event args</param>
        private void CommTimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.commTimeoutTimer.Stop();
            this.keepEventThreadAlive = false;
            this.CommunicationFailure?.Invoke(this, new CommunicationFailureEventArgs(FailureType.Timeout, $"No communication received from timer for {this.commTimeoutTimer.Interval}ms"));
        }

        /// <summary>
        /// Event dispatcher and command thread.
        /// </summary>
        private void RunEventThread()
        {
            this.commTimeoutTimer.Start();
            while (this.keepEventThreadAlive)
            {
                TimingTriggeredEventArgs timingEvent;
                while (this.timingEvents.TryDequeue(out timingEvent))
                {
                    this.OnTrigger?.Invoke(this, timingEvent);
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
        }

        /// <summary>
        /// Protocol handler connection status has changed.
        /// </summary>
        /// <param name="sender">The protocol handler.</param>
        /// <param name="e"><see cref="ConnectionStateChangedEventArgs"/> with information about the connection state change.</param>
        private void ProtocolHandler_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (!e.Connected)
            {
                Log.Info($"Timer connection state");
                this.keepEventThreadAlive = false;
                this.CommunicationFailure?.Invoke(this, new CommunicationFailureEventArgs(FailureType.Disconnect, $"Timer disconnected"));
            }
        }

        /// <summary>
        /// Handles new data that comes in through <see cref="protocolHandler"/>
        /// </summary>
        /// <param name="sender">The protocol handler.</param>
        /// <param name="e">Event args</param>
        private void ProtocolHandler_NewDataArrived(object sender, EventArgs e)
        {
            this.commTimeoutTimer.Stop();
            CommandData packet;
            while ((packet = this.protocolHandler.NextPacket) != null)
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

            this.commTimeoutTimer.Start();
        }

        /// <summary>
        /// Handles Get Identification responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetIdentification(CommandData packet)
        {
            Log.Info($"Got ID packet for timer: {packet}");
        }

        /// <summary>
        /// Handles Get All Laps responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetAllLaps(CommandData packet)
        {
            Log.Info($"Got laps response for timer: {packet}");
        }

        /// <summary>
        /// Handles Update Operation Mode responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleUpdateOpMode(CommandData packet)
        {
            Log.Info($"Got update op mode response for timer: {packet}");
        }

        /// <summary>
        /// Handles Update Display responses.
        /// Not relevant, only logging.
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleUpdateDisplayedTime(CommandData packet)
        {
            Log.Info($"Got update display response for timer: {packet}");
        }

        /// <summary>
        /// Handles Get Current Time responses.
        /// Not relevant, only logging. This message is sent periodically as a means of 'keep alive'
        /// </summary>
        /// <param name="packet">The packet with data.</param>
        private void HandleGetCurrentTime(CommandData packet)
        {
            Log.Info($"Got current time for timer: {packet}");
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

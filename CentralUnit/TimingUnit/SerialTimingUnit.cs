// <copyright file="SerialTimingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

namespace TimingUnit
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
    /// Implementation of interface <see cref="ITimingUnit"/> as built by Romke Boonstra for MGNL purposes.
    /// </summary>
    public class SerialTimingUnit : ITimingUnit
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The protocol handler.
        /// </summary>
        private CommunicationProtocol protocolHandler;

        public SerialTimingUnit(ISerialCommunication commInterface)
        {
            if (commInterface == null)
            {
                throw new ArgumentNullException("commInterface");
            }

            this.protocolHandler = new CommunicationProtocol(commInterface);
            this.protocolHandler.ConnectionStateChanged += this.ProtocolHandler_ConnectionStateChanged;
            this.protocolHandler.NewDataArrived += this.ProtocolHandler_NewDataArrived;
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

        private void ProtocolHandler_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (!e.Connected)
            {
                Log.Info($"Timer connection state");  
            }
        }

        private void ProtocolHandler_NewDataArrived(object sender, EventArgs e)
        {
            CommandData packet;
            while ((packet = this.protocolHandler.NextPacket) != null)
            {
                switch (packet.CommandType)
                {
                    case (ushort)SerialTimerCommands.GetLatestTimeStamp:
                        HandleGetLatestTimestamp(packet);
                        break;
                    case (ushort)SerialTimerCommands.GetCurrentTime:
                        HandleGetCurrentTime(packet);
                        break;
                    case (ushort)SerialTimerCommands.UpdateDisplayedTime:
                        HandleUpdateDisplayedTime(packet);
                        break;
                    case (ushort)SerialTimerCommands.UpdateOpMode:
                        HandleUpdateOpMode(packet);
                        break;
                    case (ushort)SerialTimerCommands.GetAllLaps:
                        HandleGetAllLaps(packet);
                        break;
                    case (ushort)SerialTimerCommands.GetIdentification:
                        HandleGetIdentification(packet);
                        break;
                    default:
                        Log.Error($"Got invalid packet {packet}");
                        break;
                }
            }
        }

        private void HandleGetIdentification(CommandData packet)
        {
            Log.Info($"Got ID packet for timer: {packet}");
        }

        private void HandleGetAllLaps(CommandData packet)
        {
            Log.Info($"Got laps response for timer: {packet}");
        }

        private void HandleUpdateOpMode(CommandData packet)
        {
            Log.Info($"Got update op mode response for timer: {packet}");
        }

        private void HandleUpdateDisplayedTime(CommandData packet)
        {
            Log.Info($"Got update display response for timer: {packet}");
        }

        private void HandleGetCurrentTime(CommandData packet)
        {
            Log.Info($"Got current time for timer: {packet}");
        }

        private void HandleGetLatestTimestamp(CommandData packet)
        {
            Log.Info($"Got timestamp for timer: {packet}");
        }
    }
}

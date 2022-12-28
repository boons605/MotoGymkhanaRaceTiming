// <copyright file="BLERiderIdUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.StartLightUnit
{
    using System;
    using System.Threading;
    using Communication;

    /// <summary>
    /// Rider ID Unit implementation for the ESP32-based Rider ID unit employing BLE iBeacons for identifying riders.
    /// </summary>
    public class BLEStartLightUnit : AbstractCommunicatingUnit, IStartLightUnit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BLEStartLightUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        /// <param name="unitId">The unit name</param>
        /// <param name="token">The cancellation token for this unit</param>
        public BLEStartLightUnit(ISerialCommunication commInterface, string unitId, CancellationToken token) : base(commInterface, unitId, token)
        {
        }

        /// <summary>
        /// BLE start light commands.
        /// </summary>
        private enum BLEStartLightCommands
        {
            /// <summary>
            /// Set the start light color.
            /// </summary>
            SetStartColor = 0x0006
        }

        /// <summary>
        /// The ID for this unit, i.e. startUnit or finishUnit
        /// </summary>
        public string UnitId => this.unitId;

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

            this.commandQueue.Enqueue(new CommandData((ushort)BLEStartLightCommands.SetStartColor, 0, new byte[] { colorByte }));
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
                case (ushort)BLEStartLightCommands.SetStartColor:
                    this.HandleSetStartColorResponse(packet);
                    break;
                default:
                    Log.Error($"{this.unitId}:Got invalid packet {packet}");
                    break;
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
    }
}
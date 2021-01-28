// <copyright file="CommunicationProtocol.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Timers;
    using log4net;

    /// <summary>
    /// Handles the communication protocol for both the timing and the Rider ID devices.
    /// </summary>
    public class CommunicationProtocol : IDisposable
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The communication channel used by this instance.
        /// </summary>
        private ISerialCommunication communicationChannel;

        /// <summary>
        /// The queue for received responses.
        /// </summary>
        private ConcurrentQueue<CommandData> receiveQueue = new ConcurrentQueue<CommandData>();

        /// <summary>
        /// The command to send. Just one, since we need to await the response anyway, which is the responsibility of the user of the protocol.
        /// </summary>
        private CommandData commandToSend = null;

        /// <summary>
        /// The object to lock operations on the command.
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// Monitor receive timeouts.
        /// </summary>
        private System.Timers.Timer timeoutTimer;

        /// <summary>
        /// Monitor response timeouts.
        /// </summary>
        private System.Timers.Timer responseTimeoutTimer;

        /// <summary>
        /// The buffer to store data in until the entire command is received..
        /// </summary>
        private byte[] dataBuffer = new byte[256];

        /// <summary>
        /// Current position in the buffer.
        /// </summary>
        private int bufferPosition = 0;

        /// <summary>
        /// The current state of the protocol.
        /// </summary>
        private State state = State.Idle;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationProtocol" /> class.
        /// </summary>
        /// <param name="channel">The <see cref="ISerialCommunication"/> for this instance to use.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="channel"/> is null</exception>
        public CommunicationProtocol(ISerialCommunication channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            this.communicationChannel = channel;
            this.communicationChannel.DataReceived += this.CommunicationChannel_DataReceived;
            this.communicationChannel.ConnectionStateChanged += this.CommunicationChannel_ConnectionStateChanged;
            this.communicationChannel.Failure += this.CommunicationChannel_Failure;

            this.timeoutTimer = new System.Timers.Timer();
            this.timeoutTimer.Interval = 200;
            this.timeoutTimer.Elapsed += this.TimeoutTimer_Elapsed;
            this.responseTimeoutTimer = new Timer(750);
            this.responseTimeoutTimer.Elapsed += this.ResponseTimeoutTimer_Elapsed;
        }

        /// <summary>
        /// Fires when the underlying connection state has changed.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Fires when a new packet has arrived.
        /// </summary>
        public event EventHandler<EventArgs> NewDataArrived;

        /// <summary>
        /// Possible states of the communication protocol.
        /// </summary>
        private enum State
        {
            /// <summary>
            /// Protocol Idle state.
            /// </summary>
            Idle,

            /// <summary>
            /// State to indicate that the protocol is in receiving mode.
            /// </summary>
            Receiving
        }

        /// <summary>
        /// Gets the packet in front of the queue.
        /// </summary>
        public CommandData NextPacket
        {
            get
            {
                CommandData latestData;
                if (this.receiveQueue.TryDequeue(out latestData))
                {
                    return latestData;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets if the protocol is ready to send the next command.
        /// The protocol is ready to send the next command when no command is pending.
        /// </summary>
        /// <returns>true when no command is pending, false when a command is pending.</returns>
        public bool ReadyToSend()
        {
            bool retVal = false;
            lock (this.lockObj)
            {
                retVal = (this.commandToSend == null) && this.communicationChannel.Connected;
            }

            return retVal;
        }

        /// <summary>
        /// Send the next command.
        /// When the protocol is in the process of receiving data from a unit, sending is deferred.
        /// </summary>
        /// <param name="cmd">The command to send.</param>
        public void SendCommand(CommandData cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            lock (this.lockObj)
            {
                this.commandToSend = cmd;
            }

            this.SendPendingCommand();
        }

        /// <summary>
        /// Closes the <see cref="ISerialCommunication"/> for this instance to allow graceful shutdown.
        /// </summary>
        public void Dispose()
        {
            this.communicationChannel.Close();
        }

        /// <summary>
        /// Handles a failure in the communication channel.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        private void CommunicationChannel_Failure(object sender, EventArgs e)
        {
            Log.Error("Comms failure");
        }

        /// <summary>
        /// Handles a connection state change in the communication channel.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        private void CommunicationChannel_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.ConnectionStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Checks if a complete packet has been received.
        /// </summary>
        /// <param name="data">The data buffer to check.</param>
        /// <param name="position">The position in the buffer.</param>
        /// <returns>true when the position in the buffer is more than the data length plus <see cref="CommandData.CommandHeaderLength"/>. False otherwise.</returns>
        private bool CheckDataBufferContentLength(byte[] data, int position)
        {
            if (position >= (BitConverter.ToUInt16(data, 0) + CommandData.CommandHeaderLength))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes new data presented by the <see cref="communicationChannel"/>
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> containing the data for this event.</param>
        private void CommunicationChannel_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (this.state == State.Idle)
            {
                this.timeoutTimer.Start();
                this.state = State.Receiving;
            }

            if (this.state == State.Receiving)
            {
                if (e.Data.Length > 0)
                {
                    this.AddNewDataToBuffer(e);

                    while (this.CheckDataBufferContentLength(this.dataBuffer, this.bufferPosition))
                    {
                        this.timeoutTimer.Stop();

                        this.ValidateDataBufferContents();
                    }

                    this.SendPendingCommand();
                }
            }
        }

        /// <summary>
        /// Sends a pending command.
        /// Only sends the command when no command reception is in progress.
        /// </summary>
        private void SendPendingCommand()
        {
            if (this.state == State.Idle)
            {
                byte[] data = null;
                lock (this.lockObj)
                {
                    if (this.commandToSend != null)
                    {
                        this.commandToSend.UpdateCRC();
                        data = this.commandToSend.ToArray(false);
                    }
                }

                if (data != null)
                {
                    this.responseTimeoutTimer.Start();
                    this.communicationChannel.Write(data);
                }
            }
        }

        /// <summary>
        /// Adds new data to the reception buffer.
        /// </summary>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> containing the data for this event.</param>
        private void AddNewDataToBuffer(DataReceivedEventArgs e)
        {
            if ((e.Data.Length + this.bufferPosition) < this.dataBuffer.Length)
            {
                e.Data.CopyTo(this.dataBuffer, this.bufferPosition);
                this.bufferPosition += e.Data.Length;
            }
            else
            {
                Array.Copy(e.Data, 0, this.dataBuffer, this.bufferPosition, this.dataBuffer.Length - this.bufferPosition);
                this.bufferPosition += this.dataBuffer.Length - this.bufferPosition;
                Log.Warn($"Data buffer overrun, ignoring {e.Data.Length - (dataBuffer.Length - bufferPosition)} bytes");
            }
        }

        /// <summary>
        /// When a complete packet is present in the data buffer, this methods constructs a new <see cref="CommandData"/> out of it.
        /// If the CRC is valid, the <see cref="CommandData"/> is added to the queue and a <see cref="NewDataArrived"/> event is fired.
        /// The current packet is removed from the buffer and any further packets are moved to the front of the buffer.
        /// </summary>
        private void ValidateDataBufferContents()
        {
            CommandData data = new CommandData(this.dataBuffer);
            if (data.VerifyCRC())
            {
                this.receiveQueue.Enqueue(data);
                if (this.commandToSend != null)
                {
                    if (data.CommandType == this.commandToSend.CommandType)
                    {
                        this.commandToSend = null;
                        this.responseTimeoutTimer.Stop();
                    }
                }

                this.NewDataArrived?.Invoke(this, null);
            }
            else
            {
                Log.Error($"Received data with invalid CRC {data.CRC,4:X} , {BitConverter.ToString(data.ToArray(false))} ");
            }

            if (this.bufferPosition > data.TotalCommandLength)
            {
                this.RemoveLastCommandFromDataBuffer(data);
            }
            else
            {
                this.ClearBufferResetState();
            }
        }

        /// <summary>
        /// Remove the data from the last processed command from the buffer and move the remaining data to the front of the buffer.
        /// Restarts the timeout timer for the command.
        /// </summary>
        /// <param name="data">The last processed command.</param>
        private void RemoveLastCommandFromDataBuffer(CommandData data)
        {
            byte[] newDataBuffer = new byte[this.dataBuffer.Length];
            Array.Copy(this.dataBuffer, data.TotalCommandLength, newDataBuffer, 0, this.dataBuffer.Length - data.TotalCommandLength);
            this.dataBuffer = newDataBuffer;
            this.bufferPosition -= data.TotalCommandLength;
            this.timeoutTimer.Start();
        }

        /// <summary>
        /// Clears the buffer and resets the state to idle.
        /// </summary>
        private void ClearBufferResetState()
        {
            this.timeoutTimer.Stop();
            Array.Clear(this.dataBuffer, 0, this.dataBuffer.Length);
            this.bufferPosition = 0;
            this.state = State.Idle;
        }

        /// <summary>
        /// Handles a timeout condition while receiving a packet.
        /// Clears the buffer, logs the error and sends a potentially pending command.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        private void TimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Error($"Timeout on data reception, clearing buffer, discarding {bufferPosition} bytes");
            this.ClearBufferResetState();
            this.SendPendingCommand();
        }

        /// <summary>
        /// Handles a timeout condition while waiting for a response.
        /// Clears the command that is waiting for a response.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        private void ResponseTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.responseTimeoutTimer.Stop();
            this.commandToSend = null;
        }
    }
}

// <copyright file="AbstractCommunicatingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using log4net;

    /// <summary>
    /// Base class for communicating units.
    /// </summary>
    public abstract class AbstractCommunicatingUnit : ICommunicatingUnit, IDisposable
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The protocol handler.
        /// </summary>
        protected CommunicationProtocol protocolHandler;

        /// <summary>
        /// Thread to dispatch events on, to prevent blocking communication thread.
        /// </summary>
        protected Thread eventThread;

        /// <summary>
        /// Commands waiting to be sent.
        /// </summary>
        protected ConcurrentQueue<CommandData> commandQueue;

        /// <summary>
        /// Timer to monitor the communication timeouts.
        /// </summary>
        protected System.Timers.Timer commTimeoutTimer = new System.Timers.Timer(2000);

        /// <summary>
        /// Flag that keeps the thread alive.
        /// </summary>
        protected bool keepEventThreadAlive = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCommunicatingUnit" /> class based with a specific serial channel.
        /// </summary>
        /// <param name="commInterface">The <see cref="ISerialCommunication"/> used for communicating with this Rider ID unit</param>
        public AbstractCommunicatingUnit(ISerialCommunication commInterface)
        {
            if (commInterface == null)
            {
                throw new ArgumentNullException("commInterface");
            }

            this.protocolHandler = new CommunicationProtocol(commInterface);
            this.protocolHandler.ConnectionStateChanged += this.ProtocolHandler_ConnectionStateChanged;
            this.protocolHandler.NewDataArrived += this.ProtocolHandler_NewDataArrived;
            this.eventThread = new Thread(this.RunEventThread);
            this.commandQueue = new ConcurrentQueue<CommandData>();
            this.commTimeoutTimer.Elapsed += this.CommTimeoutTimer_Elapsed;
        }

        /// <inheritdoc/>
        public event EventHandler<CommunicationFailureEventArgs> CommunicationFailure;

        /// <summary>
        /// Dispose of this unit.
        /// Sets <see cref="keepEventThreadAlive"/> to false and disposes of <see cref="protocolHandler"/>.
        /// </summary>
        public void Dispose()
        {
            this.keepEventThreadAlive = false;
            this.protocolHandler.Dispose();
        }
        
        /// <summary>
        /// Event dispatcher and command thread method.
        /// </summary>
        protected abstract void RunEventThread();

        /// <summary>
        /// Handles new packets that come in through <see cref="protocolHandler"/>
        /// </summary>
        /// <param name="packet">The packet</param>
        protected abstract void ProcessPacket(CommandData packet);

        /// <summary>
        /// Protocol handler connection status has changed.
        /// </summary>
        /// <param name="sender">The protocol handler.</param>
        /// <param name="e"><see cref="ConnectionStateChangedEventArgs"/> with information about the connection state change.</param>
        private void ProtocolHandler_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (!e.Connected)
            {
                Log.Info($"Connection disconnected");
                this.keepEventThreadAlive = false;
                CommandData cmd;
                while (this.commandQueue.TryDequeue(out cmd))
                {
                    Log.Info($"Clearing command from queue due to disconnect {cmd}");
                }

                this.CommunicationFailure?.Invoke(this, new CommunicationFailureEventArgs(FailureType.Disconnect, $"Device disconnected"));
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
                this.ProcessPacket(packet);
            }

            this.commTimeoutTimer.Start();
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
    }
}

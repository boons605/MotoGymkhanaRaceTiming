// <copyright file="ConnectionStateChangedEventArgs.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Event arguments for events indicating a change in connection state.
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new connection state.
        /// </summary>
        private bool connected;

        /// <summary>
        /// Was a disconnect caused by a failure or by a graceful disconnect
        /// </summary>
        private bool connectionFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs" /> class.
        /// </summary>
        /// <param name="connected">The new connection state</param>
        /// <param name="failure">Was a disconnect caused by a failure or by a graceful disconnect</param>
        public ConnectionStateChangedEventArgs(bool connected, bool failure)
        {
            this.connected = connected;
            this.connectionFailure = failure;
        }

        /// <summary>
        /// Gets a value indicating whether the state changed to connected or not.
        /// </summary>
        public bool Connected { get => this.connected; }

        /// <summary>
        /// Gets a value indicating whether a disconnect was caused by a failure (true) or by a graceful disconnect (false)
        /// </summary>
        public bool Failure { get => this.connectionFailure; }
    }
}

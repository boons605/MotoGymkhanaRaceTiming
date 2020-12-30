// <copyright file="DataReceivedEventArgs.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Event args used to provide serial data to listeners.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataReceivedEventArgs" /> class.
        /// </summary>
        /// <param name="data">The data received from the serial port, stripped from any API data (e.g. the <c>Xbee</c> API)</param>
        public DataReceivedEventArgs(byte[] data)
        {
            this.Data = data;
        }

        /// <summary>
        /// Gets the data that has been received.
        /// </summary>
        public byte[] Data { get; }
    }
}

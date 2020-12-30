// <copyright file="CommunicationManager.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Management of communication devices.
    /// </summary>
    public class CommunicationManager
    {
        /// <summary>
        /// Gets the serial communication device with the specified identifier.
        /// Prefix <c>directserial</c> (i.e. <c>directserial:COM1</c> or <c>directserial:/dev/ttyS0</c>) gets a direct serial connection.
        /// Prefix <c>xbee</c> followed by a 64-bit <c>Xbee</c> address gets an <c>Xbee</c> connection.
        /// </summary>
        /// <param name="identifier">The device to get.</param>
        /// <returns>An ISerialCommunication device.</returns>
        public ISerialCommunication GetCommunicationDevice(string identifier)
        {
            return null;
        }
    }
}

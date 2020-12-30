using System;

namespace Communication
{
    public interface ISerialCommunication
    {
        /// <summary>
        /// This event fires when data is received on the serial port.
        /// </summary>
        event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Indicates a communication failure, like closed serial port.
        /// </summary>
        event EventHandler Failure;

        /// <summary>
        /// Indicates the connection state has changed. Check the <c>Connected</c> property to check the state.
        /// </summary>
        event EventHandler ConnectionStateChanged;

        /// <summary>
        /// Indicates if the connection is opened or not.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Close the communication channel
        /// </summary>
        void Close();

        /// <summary>
        /// Write serial data to the device
        /// </summary>
        /// <param name="input">The data to write.</param>
        void Write(byte[] input);
    }
}

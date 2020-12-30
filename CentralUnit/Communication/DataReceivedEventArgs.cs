using System;
using System.Collections.Generic;
using System.Text;

namespace Communication
{
    public class DataReceivedEventArgs : EventArgs
    {

        /// <summary>
        /// Constructor to creat the object and add data to it.
        /// </summary>
        /// <param name="data">The data received from the serial port, stripped from any API data (e.g. the Xbee API)</param>
        public DataReceivedEventArgs(byte[] data)
        {
            this.Data = data;
        }

        /// <summary>
        /// The data that has been received.
        /// </summary>
        public byte[] Data { get; }
    }
}

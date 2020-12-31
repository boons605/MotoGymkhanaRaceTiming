// <copyright file="XbeeSerialCommunication.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using XBeeLibrary.Core.Models;

    public class XbeeSerialCommunication : ISerialCommunication
    {
        public bool Connected => throw new NotImplementedException();

        public XBee64BitAddress Xbee64address { get => xbee64address; }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler Failure;
        public event EventHandler ConnectionStateChanged;

        private XBee64BitAddress xbee64address;

        public XbeeSerialCommunication(XBee64BitAddress address)
        {
            this.xbee64address = address;
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] input)
        {
            throw new NotImplementedException();
        }
    }
}

// <copyright file="XbeeSerialCommunication.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using XBeeLibrary.Core.Models;

    public class XbeeNetwork
    {

        private List<XbeeSerialCommunication> devices = new List<XbeeSerialCommunication>();

        private ISerialCommunication communicationChannel;

        public XbeeNetwork(ISerialCommunication communication)
        {

            this.communicationChannel = communication;
            this.communicationChannel.ConnectionStateChanged += CommunicationChannel_ConnectionStateChanged;
            this.communicationChannel.DataReceived += CommunicationChannel_DataReceived;
            this.communicationChannel.Failure += CommunicationChannel_Failure;

        }

        public XbeeSerialCommunication GetDevice(string address64bit)
        {
            XBee64BitAddress address = new XBee64BitAddress(address64bit);
            XbeeSerialCommunication device = null;
            if (devices.Any(dev => dev.Xbee64address.Equals(address)))
            {
                device = devices.First(dev => dev.Xbee64address.Equals(address));
            }
            else
            {
                device = new XbeeSerialCommunication(address);
                devices.Add(device);
            }

            return device;

        }

        private void CommunicationChannel_Failure(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CommunicationChannel_DataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CommunicationChannel_ConnectionStateChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

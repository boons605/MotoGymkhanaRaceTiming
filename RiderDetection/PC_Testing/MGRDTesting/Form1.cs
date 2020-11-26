using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MGRDTesting
{
    public partial class Form1 : Form
    {
        CommunicationProtocol proto;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string pName in SerialPort.GetPortNames())
            {
                if (!toolStripComboBox1.Items.Contains(pName))
                {
                    toolStripComboBox1.Items.Add(pName);
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (proto == null)
            {
                proto = new CommunicationProtocol(toolStripComboBox1.SelectedItem.ToString());
                proto.ConnectionStateChanged += Proto_ConnectionStateChanged;
                proto.NewDataArrived += Proto_NewDataArrived;
                proto.Start();
            }
        }

        private void Proto_NewDataArrived(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Proto_ConnectionStateChanged(object sender, EventArgs e)
        {
            MessageBox.Show("Connection running: " + proto.IsRunning);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (proto == null)
            {
                return;
            }

            if (proto.ReadyToSend())
            {
                MGBTCommandData data = new MGBTCommandData();
                data.Status = 0x0000;
                data.CommandType = 0x0001;
                data.data = new byte[] { 0x24, 0x6f, 0x28, 0x7c, 0x13, 0x5a };
                proto.SendCommand(data);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (proto == null)
            {
                return;
            }

            if (proto.ReadyToSend())
            {
                MGBTCommandData data = new MGBTCommandData();
                data.Status = 0x0000;
                data.CommandType = 0x0002;
                data.data = new byte[] { 0x24, 0x6f, 0x28, 0x7c, 0x13, 0x5a };
                proto.SendCommand(data);
            }
        }
    }
}

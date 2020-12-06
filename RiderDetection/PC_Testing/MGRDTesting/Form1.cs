using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MGRDTesting
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        CommunicationProtocol proto;
        private string lastMessage;

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
                
            }

            if (proto.IsRunning)
            {
                proto.Stop();
                proto.NewDataArrived -= Proto_NewDataArrived;
                proto.ConnectionStateChanged -= Proto_ConnectionStateChanged;

            }
            else
            {
                proto = new CommunicationProtocol(toolStripComboBox1.SelectedItem.ToString());
                proto.NewDataArrived += Proto_NewDataArrived;
                proto.ConnectionStateChanged += Proto_ConnectionStateChanged;
                proto.Start();
            }
            

        }

        private void Proto_NewDataArrived(object sender, EventArgs e)
        {
            //Not a pretty solution, too lazy to write a good one. This is a tester after all;
           while (proto.HasData)
            {
                MGBTCommandData cmd = proto.GetLatestData();
                if (cmd != null)
                {
                    switch (cmd.CommandType)
                    {
                        case 1:
                            HandleAddAllowedResponse(cmd.data);
                            break;
                        case 2:
                            HandleRemoveAllowedResponse(cmd.data);
                            break;

                    }
                }
            }
        }

        private string MacBytesToString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    builder.AppendFormat("0x{0:X2}", data[i]);
                    if (i < (data.Length - 1))
                    {
                        builder.Append(":");
                    }
                }
            }
            return builder.ToString();
        }

        private void HandleAddAllowedResponse(byte[] data)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() => {
                AddLineToStatus("Added device: " + MacBytesToString(data));
            }));
        }

        private void HandleRemoveAllowedResponse(byte[] data)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() => {
                AddLineToStatus("Removed device: " + MacBytesToString(data));
            }));
        }

        private void Proto_ConnectionStateChanged(object sender, EventArgs e)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() => {
                AddLineToStatus("Connection running: " + proto.IsRunning);
            }));
        }

        private void AddLineToStatus(string line)
        {
            if (line != lastMessage)
            {
                lastMessage = line;
                statusLbx.Items.Add(line);
                statusLbx.SelectedIndex = statusLbx.Items.Count - 1;
                statusLbx.SelectedIndex = -1;
            }
        }

        private string macRegEx = "([0-9a-fA-F]{2})(?:[-:]){0,1}";

        private byte[] TextToMacBytes(string text)
        {
            byte[] macBytes = new byte[6];

            if (!string.IsNullOrEmpty(text))
            {
                if (Regex.IsMatch(text, macRegEx))
                {
                    MatchCollection bytes = Regex.Matches(text, macRegEx);
                    
                    if (bytes.Count == 6)
                    {
                        for (int i = 0; i < bytes.Count; i++)
                        {
                            if (bytes[i].Groups.Count == 2)
                            {
                                macBytes[i] = Convert.ToByte("0x" + bytes[i].Groups[1], 16);
                            }
                        }
                    }
                }
            }

            return macBytes;
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
                data.data = TextToMacBytes(macTbx.Text);
                proto.SendCommand(data);
            }
            else
            {
                AddLineToStatus("Proto not ready to send, try again later");
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
                data.data = TextToMacBytes(macTbx.Text);
                proto.SendCommand(data);
            }
        }
    }
}

using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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

        private bool addingMultiple = false;
        private int multiIndex = 0;
        private string[] multiAddresList;
        private Dictionary<string, string> riderNames = new Dictionary<string, string>();

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
                            HandleAddAllowedResponse(cmd);
                            break;
                        case 2:
                            HandleRemoveAllowedResponse(cmd);
                            break;
                        case 3:
                            HandleListAllowedDevices(cmd);
                            break;
                        case 4:
                            HandleListDetectedDevices(cmd);
                            break;
                        case 5:
                            HandleGetClosestDevice(cmd);
                            break;
                        case 101:
                            HandleGetCurrentTime(cmd);
                            break;
                        case 102:
                            HandleGetLaps(cmd);
                            break;
                        case 103:
                            HandleGetCurrentTime(cmd);
                            break;
                        case 104:
                            break;
                        case 255:
                            HandleGetID(cmd);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void HandleGetLaps(MGBTCommandData cmd)
        {
            try
            {
                var reader = new BinaryReader(new MemoryStream(cmd.data));
                int byteIndex = 0;
                int time = 0;
                int lapIndex = 0;
                StringBuilder lapLines = new StringBuilder();
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    byte aByte = reader.ReadByte();
                    time |= aByte << (8 * byteIndex);

                    byteIndex++;

                    if (byteIndex >= 3)
                    {
                        byteIndex = 0;
                        lapLines.AppendLine($"Got lap {lapIndex} with time {time}");
                        lapIndex++;
                    }
                    
                }
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus(lapLines.ToString());
                }));
            }
            catch (Exception ex)
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus($"Exception: {ex}");
                }));
            }
        }

        private void HandleGetID(MGBTCommandData cmd)
        {
            try
            {
                var reader = new BinaryReader(new MemoryStream(cmd.data));

                byte deviceType = reader.ReadByte();
                byte[] idDate = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus($"Got type: {deviceType} with data: {BitConverter.ToString(idDate)}");
                }));

            }
            catch (Exception ex)
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus($"Exception: {ex}");
                }));
            }
        }

        private void HandleGetCurrentTime(MGBTCommandData cmd)
        {
            try
            {
                var reader = new BinaryReader(new MemoryStream(cmd.data));

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    UInt32 time = reader.ReadUInt32();
                    byte timeType = reader.ReadByte();

                    InvocationHelper.InvokeIfRequired(this, new Action(() =>
                    {
                        AddLineToStatus($"Got time: {time}");
                        AddLineToStatus($"Of type: {timeType}");
                    }));
                }
            }
            catch (Exception ex)
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus($"Exception: {ex}");
                }));
            }
        }

        private void HandleGetClosestDevice(MGBTCommandData cmd)
        {
            if (cmd.Status == 0)
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    List<MGBTDevice> devices = MGBTDevice.FromArray(cmd.data);
                    if (devices.Count > 0)
                    {
                        string riderName = "ILLEGAL";
                        string address = MacBytesToString(devices[0].Address).ToUpperInvariant();
                        if (riderNames.ContainsKey(address))
                        {
                            riderName = riderNames[address];
                        }

                        closestDeviceLbl.Text = devices[0].ToString() + "; " + riderName;
                    }
                }));
            }
            else
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus(String.Format("Error response: {0}", cmd.Status));
                }));
            }
        }

        private void HandleListDetectedDevices(MGBTCommandData cmd)
        {
            switch (cmd.Status)
            {
                case 8:
                    HandleListDetectedDevicesProgress(cmd.data);
                    break;
                case 0:
                    InvocationHelper.InvokeIfRequired(this, new Action(() =>
                    {
                        AddLineToStatus("Started listing all devices");
                    }));
                    break;
                case 1:
                    if (cmd.data.Length > 2)
                    {
                        byte[] devicesData = new byte[cmd.data.Length - 2];
                        Array.Copy(cmd.data, 2, devicesData, 0, devicesData.Length);
                        if (cmd.data[0] == 0)
                        {
                            InvocationHelper.InvokeIfRequired(this, new Action(() =>
                            {
                                detectedDevicesLbx.Items.Clear();
                                HandleListDetectedDevicesDeviceData(devicesData);
                            }));
                        }
                        else
                        {
                            InvocationHelper.InvokeIfRequired(this, new Action(() =>
                            {
                                HandleListDetectedDevicesDeviceData(devicesData);
                            }));
                        }

                    }
                    else
                    {
                        InvocationHelper.InvokeIfRequired(this, new Action(() =>
                        {
                            AddLineToStatus("Data not long enough");
                        }));
                    }
                    break;
                default:
                    InvocationHelper.InvokeIfRequired(this, new Action(() =>
                    {
                        AddLineToStatus(String.Format("Error response: {0}", cmd.Status));
                    }));
                    break;
            }
        }

        private void HandleListDetectedDevicesDeviceData(byte[] devicesData)
        {
            List<MGBTDevice> devices = MGBTDevice.FromArray(devicesData);
            detectedDevicesLbx.Items.AddRange(devices.ToArray());
        }

        private void HandleListDetectedDevicesProgress(byte[] data)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() =>
            {
                AddLineToStatus(String.Format("Detection progress: {0:D}%", data[0]));
            }));
        }

        private void HandleListAllowedDevices(MGBTCommandData cmd)
        {
            if ((cmd.data.Length > 2) && (cmd.Status == 1))
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    if (cmd.data[0] == 0)
                    {
                        AllowedDevicesLbx.Items.Clear();
                    }
                    byte[] devicesData = new byte[cmd.data.Length - 2];
                    Array.Copy(cmd.data, 2, devicesData, 0, devicesData.Length);
                    List<MGBTDevice> devices = MGBTDevice.FromArray(devicesData);
                    AllowedDevicesLbx.Items.AddRange(devices.ToArray());
                }));
            }
            else
            {
                InvocationHelper.InvokeIfRequired(this, new Action(() =>
                {
                    AddLineToStatus(String.Format("Data length or error response: {0}", cmd.Status));
                }));
            }
        }

        private string MacBytesToString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    builder.AppendFormat("{0:X2}", data[i]);
                    if (i < (data.Length - 1))
                    {
                        builder.Append(":");
                    }
                }
            }
            return builder.ToString();
        }

        private void HandleAddAllowedResponse(MGBTCommandData cmd)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() =>
            {

                AddLineToStatus("Added device: " + MacBytesToString(cmd.data) + ", status: " + cmd.Status);
                if (addingMultiple)
                {
                    SendNextMultiAdd();
                }
            }));
        }

        private void HandleRemoveAllowedResponse(MGBTCommandData cmd)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() =>
            {
                AddLineToStatus("Removed device: " + MacBytesToString(cmd.data) + ", status: " + cmd.Status);
            }));
        }

        private void Proto_ConnectionStateChanged(object sender, EventArgs e)
        {
            InvocationHelper.InvokeIfRequired(this, new Action(() =>
            {
                AddLineToStatus("Connection running: " + proto.IsRunning);
            }));
        }

        private void AddLineToStatus(string line)
        {
            if (line != lastMessage)
            {
                lastMessage = line;
                statusLbx.Items.AddRange(line.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                statusLbx.SelectedIndex = statusLbx.Items.Count - 1;
                statusLbx.SelectedIndex = -1;
            }
        }

        private byte[] GetCommandData(string line)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            if (!string.IsNullOrEmpty(line))
            {
                string[] elements = line.Split(';');

                writer.Write(TextToMacBytes(elements[0]));
                if (elements.Length > 1)
                {
                    writer.Write(Convert.ToInt16(elements[1]));
                }
                else
                {
                    writer.Write(Convert.ToInt16(0));
                }

                if (elements.Length > 2)
                {
                    if (riderNames.ContainsKey(elements[0].ToUpperInvariant()))
                    {
                        riderNames.Remove(elements[0].ToUpperInvariant());
                    }

                    riderNames.Add(elements[0].ToUpperInvariant(), elements[2]);
                }
            }
            return stream.ToArray();
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
                else
                {
                    AddLineToStatus("Not a valid address: " + text);
                }
            }

            return macBytes;
        }

        private void SendNextMultiAdd()
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0001;
            data.data = GetCommandData(multiAddresList[multiIndex]);
            multiIndex++;
            if (multiIndex >= multiAddresList.Length)
            {
                addingMultiple = false;
            }
            SendCommand(data);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0001;
            data.data = GetCommandData(macTbx.Text);
            SendCommand(data);

        }

        private void button2_Click(object sender, EventArgs e)
        {

            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0002;
            data.data = TextToMacBytes(macTbx.Text);
            SendCommand(data);
        }

        private void SendCommand(MGBTCommandData data)
        {
            if (proto == null)
            {
                return;
            }

            if (proto.ReadyToSend())
            {

                proto.SendCommand(data);
            }
            else
            {
                AddLineToStatus("Proto not ready to send, try again later");
            }
        }

        private void UpdateAllowedDevicesBtn_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0003;
            data.data = new byte[2];
            SendCommand(data);
        }

        private void updateDetectedDevicesBtn_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0004;
            data.data = new byte[2];
            SendCommand(data);
        }

        private void updateClosestDeviceBtn_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x0005;
            data.data = new byte[2];
            SendCommand(data);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    multiAddresList = File.ReadAllLines(openDialog.FileName);
                    multiIndex = 0;
                    addingMultiple = true;
                    SendNextMultiAdd();
                }
                catch (Exception ex)
                {
                    AddLineToStatus("Can not read lines from file " + openDialog.FileName + Environment.NewLine + ex);
                }
            }



        }

        private void closestDeviceLbl_Click(object sender, EventArgs e)
        {

        }

        private void IdentBtn_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 0x00FF;
            data.data = new byte[2];
            SendCommand(data);
        }

        private void SetTimeBtn_Click(object sender, EventArgs e)
        {
            int millis;
            if (Int32.TryParse(macTbx.Text, out millis))
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                if (millis > 599999)
                {
                    millis = 599999;
                }
                writer.Write(millis);
                MGBTCommandData data = new MGBTCommandData();
                data.Status = 0x0000;
                data.CommandType = 104;
                data.data = stream.ToArray();
                SendCommand(data);
            }    
            else
            {
                AddLineToStatus($"{macTbx.Text} is not an int");
            }
        }

        private void SetOpMode(int opMode)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 105;
            data.data = new byte[2];
            data.data[0] = (byte)opMode;
            SendCommand(data);
        }

        private void connectedRbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (connectedRbtn.Checked)
            {
                SetOpMode(2);
            }
            
        }

        private void singleRunRbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (singleRunRbtn.Checked)
            {
                SetOpMode(3);
            }
            
        }

        private void lapTimerRbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (lapTimerRbtn.Checked)
            {
                SetOpMode(1);
            }
            
        }

        private void getLapsBtn_Click(object sender, EventArgs e)
        {
            MGBTCommandData data = new MGBTCommandData();
            data.Status = 0x0000;
            data.CommandType = 102;
            data.data = new byte[2];
            SendCommand(data);
        }
    }
}

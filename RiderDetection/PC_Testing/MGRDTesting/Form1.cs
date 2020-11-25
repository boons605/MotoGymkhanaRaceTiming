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
                if (toolStripComboBox1.Items.Contains(pName))
                {
                    toolStripComboBox1.Items.Add(pName);
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }
    }
}

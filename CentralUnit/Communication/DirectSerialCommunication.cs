using log4net;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Communication
{
    /// <summary>
    /// Implementation of ISerialCommunication for direct serial communication.
    /// </summary>
    public class DirectSerialCommunication : ISerialCommunication
    {

        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Serial port used for communication.
        /// </summary>
        private readonly SerialPort serialPort;
        
        /// <summary>
        /// Communication thread.
        /// </summary>
        private Thread commThread;

        /// <summary>
        /// Is the port opened?
        /// </summary>
        public bool Connected => serialPort.IsOpen;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler Failure;
        public event EventHandler ConnectionStateChanged;

        /// <summary>
        /// Open the serial port specificed by portName.
        /// Start the serial communication thread.
        /// Port is opened at 115200/8/N/1.
        /// </summary>
        /// <param name="portName">The serial port name, e.g. COM1 or /dev/ttyS0</param>
        public DirectSerialCommunication(string portName)
        {
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            commThread = new Thread(this.run) { IsBackground = true };
            commThread.Start();
        }

        /// <summary>
        /// The thread body. Performs polling.
        /// Reads data from the serial port and throws the DataReceived event.
        /// Throws Failure event when an exception occurs.
        /// </summary>
        /// <param name="obj">Not used</param>
        private void run(object obj)
        {
            try 
            {
                serialPort.Open();

                ConnectionStateChanged?.Invoke(this, new EventArgs());

                while (serialPort.IsOpen)
                {

                    int bytesToRead = serialPort.BytesToRead;

                    if (bytesToRead > 0)
                    {
                        byte[] receivedData = new byte[bytesToRead];
                        serialPort.Read(receivedData, 0, bytesToRead);
                        DataReceived?.Invoke(this, new DataReceivedEventArgs(receivedData));
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Serial port failure: " + serialPort.PortName, ex);
                Failure?.Invoke(this, new EventArgs());
            }
            finally
            {
                ConnectionStateChanged?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Write data to the serial port.
        /// Files a failure event when this fails.
        /// </summary>
        /// <param name="input">The data to write to the serial port. May not be null or empty</param>
        /// <exception cref="ArgumentNullException">When input is null</exception>
        /// <exception cref="ArgumentException">When input has length 0</exception>
        public void Write(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "input data may not be null");
            }

            if (input.Length == 0)
            {
                throw new ArgumentException("Length of byte array to be sent may not be 0", "input");
            }

            try 
            {

                serialPort.Write(input, 0, input.Length);
            }
            catch (Exception ex)
            {
                Log.Error("Unable to write data to the serial port: " + serialPort.PortName, ex);
                Failure?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Close the serial port.
        /// </summary>
        public void Close()
        {
            try
            {
                serialPort.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Unable to close the serial port: " + serialPort.PortName, ex);
                Failure?.Invoke(this, new EventArgs());
            }

        }
    }
}

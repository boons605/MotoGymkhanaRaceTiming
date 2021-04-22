// <copyright file="DirectSerialCommunication.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Text;
    using System.Threading;
    using log4net;

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
        /// The cancellation token for this unit.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSerialCommunication" /> class.
        /// Open the serial port specified by <c>portName</c>.
        /// Start the serial communication thread.
        /// Port is opened at 115200/8/N/1.
        /// </summary>
        /// <param name="portName">The serial port name, e.g. <c>COM1</c> or <c>/dev/ttyS0</c></param>
        /// <param name="token">The cancellation token for this unit</param>
        public DirectSerialCommunication(string portName, CancellationToken token)
        {
            this.cancellationToken = token;
            this.serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            this.commThread = new Thread(this.Run) { IsBackground = true };
            this.commThread.Start();
        }

        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc/>
        public event EventHandler Failure;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Is the port opened?
        /// </summary>
        public bool Connected => this.serialPort.IsOpen;

        /// <inheritdoc/>
        public string Name => this.serialPort.PortName;

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
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Writing serial data {BitConverter.ToString(input)}");
                }

                this.serialPort.Write(input, 0, input.Length);
            }
            catch (Exception ex)
            {
                Log.Error("Unable to write data to the serial port: " + this.serialPort.PortName, ex);
                this.Failure?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Close the serial port.
        /// </summary>
        public void Close()
        {
            try
            {
                this.serialPort.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Unable to close the serial port: " + this.serialPort.PortName, ex);
                this.Failure?.Invoke(this, new EventArgs());
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Direct serial comm channel {this.serialPort.PortName}";
        }

        /// <summary>
        /// The thread body. Performs polling.
        /// Reads data from the serial port and throws the DataReceived event.
        /// Throws Failure event when an exception occurs.
        /// </summary>
        /// <param name="obj">Not used</param>
        private void Run(object obj)
        {
            try 
            {
                this.serialPort.Open();

                this.ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(this.serialPort.IsOpen, false));

                while (this.serialPort.IsOpen && (!this.cancellationToken.IsCancellationRequested))
                {
                    int bytesToRead = this.serialPort.BytesToRead;

                    if (bytesToRead > 0)
                    {
                        byte[] receivedData = new byte[bytesToRead];
                        this.serialPort.Read(receivedData, 0, bytesToRead);
                        this.DataReceived?.Invoke(this, new DataReceivedEventArgs(receivedData));
                    }

                    Thread.Sleep(10);
                }

                this.ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false, false));
            }
            catch (Exception ex)
            {
                Log.Error("Serial port failure: " + this.serialPort.PortName, ex);
                this.ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false, true));
            }

            if (this.serialPort.IsOpen)
            {
                this.Close();
            }
        }
    }
}

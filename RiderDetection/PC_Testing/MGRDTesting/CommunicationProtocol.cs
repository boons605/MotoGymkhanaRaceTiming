using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MGRDTesting
{
    public class CommunicationProtocol
    {

        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ConcurrentQueue<MGBTCommandData> rxQueue = new ConcurrentQueue<MGBTCommandData>();

        MGBTCommandData commandToSend = null;

        object lockObj = new object();

        SerialPort serialPort;
        
        Thread commThread;

        public event EventHandler<EventArgs> ConnectionStateChanged;

        public event EventHandler<EventArgs> NewDataArrived;


        private System.Timers.Timer timeoutTimer;

        private enum State
        {
            Idle,
            Receiving,
            Sending
        }

        State state = State.Idle;

        public bool IsRunning
        {
            get
            {
                return (serialPort.IsOpen && commThread.IsAlive);
            }
        }

        public CommunicationProtocol(string portName)
        {
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            commThread = new Thread(this.run) { IsBackground = true };
        }

        public bool HasData
        {
            get
            {
                return !rxQueue.IsEmpty;
            }
        }

        public MGBTCommandData GetLatestData()
        {
            MGBTCommandData data = null;
            if (rxQueue.TryDequeue(out data))
            {
                return data;
            }
            return data;
        }

        public bool CheckDataBuffer(byte[] data, int position)
        {
            if (position >= (BitConverter.ToUInt16(data, 0) + 8))
            {
                return true;
            }

            return false;
        }

        private void run()
        {
            byte[] dataBuffer = new byte[256];
            int bufferPosition = 0;

            timeoutTimer = new System.Timers.Timer();
            timeoutTimer.Interval = 200;
            timeoutTimer.Elapsed += TimeoutTimer_Elapsed;

            try
            {


                serialPort.Open();

                ConnectionStateChanged?.Invoke(this, null);

                while (serialPort.IsOpen)
                {
                    int bytesToRead = serialPort.BytesToRead;
                    if (state == State.Idle)
                    {
                        
                        if (bytesToRead > 0)
                        {
                           timeoutTimer.Start();
                           state = State.Receiving; 
                        }
                        else if (commandToSend != null)
                        {
                            state = State.Sending;
                        }
                        

                        
                    }

                    switch (state)
                    {
                        case State.Receiving:
                            {
                                if (bytesToRead > 0)
                                {
                                    if ((bytesToRead + bufferPosition) < dataBuffer.Length)
                                    {
                                        serialPort.Read(dataBuffer, bufferPosition, bytesToRead);
                                        bufferPosition += bytesToRead;
                                    }
                                    else
                                    {
                                        serialPort.Read(dataBuffer, bufferPosition, (dataBuffer.Length-bufferPosition));
                                        bufferPosition += (dataBuffer.Length - bufferPosition);
                                    }
                                    
                                        
                                        while (CheckDataBuffer(dataBuffer, bufferPosition))
                                        { 
                                            MGBTCommandData data = MGBTCommandData.FromArray(dataBuffer);
                                            if (data.VerifyCRC())
                                            {
                                                rxQueue.Enqueue(data);
                                                NewDataArrived?.Invoke(this, null);
                                            }
                                            else
                                            {

                                            }
                                            if (bufferPosition > (data.dataLength + 8))
                                            {
                                                byte[] newDataBuffer = new byte[dataBuffer.Length];
                                                Array.Copy(dataBuffer, (data.dataLength + 8), newDataBuffer, 0, dataBuffer.Length - (data.dataLength + 8));
                                                dataBuffer = newDataBuffer;
                                                bufferPosition -= (data.dataLength + 8);
                                            }
                                            else
                                            {
                                                Array.Clear(dataBuffer, 0, dataBuffer.Length);
                                                bufferPosition = 0;
                                                state = State.Idle;
                                            }
                                            
                                        }
                                    
                                }
                                break;
                            }
                        case State.Sending:
                            {
                                byte[] data = null;
                                lock (lockObj)
                                {
                                    commandToSend.UpdateCRC();
                                    data = commandToSend.ToArray(false);
                                    commandToSend = null;
                                    state = State.Idle;
                                }

                                if (data != null)
                                {
                                    serialPort.Write(data, 0, data.Length);
                                }    

                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                    Thread.Sleep(10);
                }


            }
            catch (Exception e)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
            ConnectionStateChanged?.Invoke(this, null);
        }

        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timeoutTimer.Stop();
            state = State.Idle;
        }

        public bool Start()
        {
            commThread.Start();
            return commThread.IsAlive;
        }

        public bool Stop()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }

            return !commThread.IsAlive;       
        }

        public bool ReadyToSend()
        {
            bool retVal = false;
            lock(lockObj)
            {
                retVal = (commandToSend == null);
            }
            return retVal;
        }

        public void SendCommand(MGBTCommandData cmd)
        {
            lock (lockObj)
            {
                commandToSend = cmd;
            }
        }

        

    }
}

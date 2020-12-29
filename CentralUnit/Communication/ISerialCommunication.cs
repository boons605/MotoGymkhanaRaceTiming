using System;

namespace Communication
{
    public interface ISerialCommunication
    {
        void Write(byte[] input);
        byte[] Read();
    }
}

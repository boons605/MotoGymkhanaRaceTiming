
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGRDTesting
{
    public struct MGBTCommandData
    {
        UInt16 dataLength;
        UInt16 CRC;
        UInt16 Status;
        byte[] data;
        public byte[] ToArray()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(this.dataLength);
            writer.Write(this.CRC);
            writer.Write(this.Status);
            writer.Write(this.data);

            return stream.ToArray();
        }

        public static MGBTCommandData FromArray(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));

            var s = default(MGBTCommandData);

            s.dataLength = reader.ReadUInt16();
            s.CRC = reader.ReadUInt16();
            s.Status = reader.ReadUInt16();
            s.data = reader.ReadBytes(s.dataLength);

            return s;
        }
    }
}

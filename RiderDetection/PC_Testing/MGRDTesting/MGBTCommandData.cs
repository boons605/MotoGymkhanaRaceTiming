
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGRDTesting
{
    public class MGBTCommandData
    {
        private byte headerData = 0xFF;
        public UInt16 dataLength;
        public UInt16 CRC;
        public UInt16 Status;
        public UInt16 CommandType;
        public byte[] data;
        public byte[] ToArray(bool forCrc)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            if (!forCrc)
            {
                writer.Write(headerData);
                writer.Write((UInt16)this.data.Length);
                writer.Write(this.CRC);
            }
            writer.Write(this.Status);
            writer.Write(this.CommandType);
            writer.Write(this.data);

            return stream.ToArray();
        }

        public static MGBTCommandData FromArray(byte[] bytes)
        {
            try 
            {
                var reader = new BinaryReader(new MemoryStream(bytes));

                var s = new MGBTCommandData();

                s.dataLength = reader.ReadUInt16();
                s.CRC = reader.ReadUInt16();
                s.Status = reader.ReadUInt16();
                s.CommandType = reader.ReadUInt16();
                s.data = reader.ReadBytes(s.dataLength);


                return s;
            }
            catch (Exception e)
            {
                return null;
            }
            
        }

        public void UpdateCRC()
        {
            CRC = calculateCRC(ToArray(true));
        }

        public bool VerifyCRC()
        {
            return CRC == calculateCRC(ToArray(true));
        }

        private readonly UInt16[] crctable =
        {
            0x0000,
            0x1021,
            0x2042,
            0x3063,
            0x4084,
            0x50a5,
            0x60c6,
            0x70e7,
            0x8108,
            0x9129,
            0xa14a,
            0xb16b,
            0xc18c,
            0xd1ad,
            0xe1ce,
            0xf1ef,
    };

        //Calculates the magical CRC value
        private UInt16 calculateCRC(byte[] u8Buf)
        {
            UInt16 crc = 0xFFFF;

            for (int i = 0; i < u8Buf.Length; i++)
            {
                crc = (UInt16)(((UInt16)(((UInt16)(crc << 4)) | ((UInt16)(u8Buf[i] >> 4)))) ^ crctable[crc >> 12]);
                crc = (UInt16)(((UInt16)(((UInt16)(crc << 4)) | ((UInt16)(u8Buf[i] & (byte)0x0F)))) ^ crctable[crc >> 12]);
            }

            return crc;
        }
    }
}

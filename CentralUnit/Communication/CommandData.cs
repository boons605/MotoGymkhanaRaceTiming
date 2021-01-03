// <copyright file="CommandData.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Command data structure as used by the embedded devices.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Used for communication, exact sizing required.")]
    public class CommandData
    {
        /// <summary>
        /// Number of bytes in the command header.
        /// Length of: Data length (2 bytes) + CRC (2 bytes) + Status (2 bytes) + Command Type (2 bytes)
        /// </summary>
        public const byte CommandHeaderLength = 8;

        /// <summary>
        /// Header byte for the protocol.
        /// </summary>
        private const byte HeaderData = 0xFF;

        /// <summary>
        /// Table of magical CRC values.
        /// </summary>
        private readonly ushort[] crctable =
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
        
        /// <summary>
        /// The number of bytes sent in <see cref="data"/> for this command, as a <see cref="UInt16" />
        /// </summary>
        private UInt16 dataLength;

        /// <summary>
        /// The 16-bit CRC for this command, as a <see cref="UInt16" />
        /// </summary>
        private UInt16 cRC;

        /// <summary>
        /// The status field for this command, as a <see cref="UInt16" />
        /// </summary>
        private UInt16 status;

        /// <summary>
        /// The Command Type for this command, as a <see cref="UInt16" />
        /// </summary>
        private UInt16 commandType;

        /// <summary>
        /// The data contained by this command.
        /// </summary>
        private byte[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandData" /> class.
        /// </summary>
        /// <param name="commandType">The command type for this instance</param>
        /// <param name="status">The status to be sent with this instance</param>
        /// <param name="data">The data to be sent with this instance</param>
        public CommandData(UInt16 commandType, UInt16 status, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.data = data;
            this.dataLength = (UInt16)this.data.Length;
            this.commandType = commandType;
            this.status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandData" /> class based on data received from a serial connection.
        /// </summary>
        /// <param name="data">The data received from a serial connection.</param>
        public CommandData(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length < CommandHeaderLength)
            {
                throw new ArgumentException($"Data length ({data.Length}) less than command header length ({CommandHeaderLength})");
            }

            var reader = new BinaryReader(new MemoryStream(data));
            this.dataLength = reader.ReadUInt16();
            this.cRC = reader.ReadUInt16();
            this.status = reader.ReadUInt16();
            this.commandType = reader.ReadUInt16();
            this.data = reader.ReadBytes(this.dataLength);
        }

        /// <summary>
        /// Gets the status field for this command.
        /// </summary>
        public ushort Status { get => this.status; }

        /// <summary>
        /// Gets the Command Type for this command
        /// </summary>
        public ushort CommandType { get => this.commandType; }

        /// <summary>
        /// Gets the data contained by this command.
        /// </summary>
        public byte[] Data { get => this.data; }

        /// <summary>
        /// Gets the data length sent with this message.
        /// </summary>
        public ushort DataLength { get => this.dataLength; }

        /// <summary>
        /// Gets the total length of the message data as sent over a serial connection.
        /// </summary>
        public ushort TotalCommandLength { get => (ushort)(this.dataLength + 8); }

        /// <summary>
        /// Gets the 16-bit CRC
        /// </summary>
        public ushort CRC { get => this.cRC; }

        /// <summary>
        /// Generate a byte array for either CRC calculation or data transmission over the network.
        /// </summary>
        /// <param name="forCrc">Set to true when resulting byte array is only used for CRC calculation, as for the CRC the fields Header, DataLength and CRC itself are skipped.</param>
        /// <returns>This instance as a byte array.</returns>
        public byte[] ToArray(bool forCrc)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            if (!forCrc)
            {
                writer.Write(HeaderData);
                writer.Write((UInt16)this.data.Length);
                writer.Write(this.cRC);
            }

            writer.Write(this.status);
            writer.Write(this.commandType);
            writer.Write(this.data);

            return stream.ToArray();
        }

        /// <summary>
        /// Calculate the <see cref="CRC"/> for this instance.
        /// </summary>
        public void UpdateCRC()
        {
            this.cRC = this.CalculateCRC(this.ToArray(true));
        }

        /// <summary>
        /// Verifies the CRC of this command.
        /// </summary>
        /// <returns>true when <see cref="CRC"/> matches the CRC calculated based on the data in this instance. false otherwise</returns>
        public bool VerifyCRC()
        {
            return this.cRC == this.CalculateCRC(this.ToArray(true));
        }

        /// <summary>
        /// Calculates the CRC for the given byte buffer.
        /// Non-standard CRC, inspired by SparkFun code and ThingMagic RFID code, written in C++.
        /// </summary>
        /// <param name="buffer">The byte buffer to calculate the CRC for</param>
        /// <returns>Custom CRC16 value, based on <see cref="crctable"/></returns>
        private ushort CalculateCRC(byte[] buffer)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < buffer.Length; i++)
            {
                crc = (ushort)(((ushort)(((ushort)(crc << 4)) | ((ushort)(buffer[i] >> 4)))) ^ this.crctable[crc >> 12]);
                crc = (ushort)(((ushort)(((ushort)(crc << 4)) | ((ushort)(buffer[i] & (byte)0x0F)))) ^ this.crctable[crc >> 12]);
            }

            return crc;
        }
    }
}

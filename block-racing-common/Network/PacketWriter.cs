using System;
using System.Collections.Generic;
using System.Text;

namespace block_racing_common.Network
{
    public class PacketWriter
    {
        private readonly List<byte> _buffer = new();

        public PacketWriter(ushort packetId)
        {
            // Length placeholder
            _buffer.AddRange(new byte[2]);

            // PacketId
            _buffer.AddRange(BitConverter.GetBytes(packetId));
        }

        public void Write(bool value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(byte value)
        {
            _buffer.Add(value);
        }

        public void Write(ushort value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(int value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(long value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(float value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            _buffer.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _buffer.AddRange(bytes);
        }

        public byte[] ToArray()
        {
            ushort length = (ushort)_buffer.Count;

            byte[] lengthBytes = BitConverter.GetBytes(length);

            _buffer[0] = lengthBytes[0];
            _buffer[1] = lengthBytes[1];

            return _buffer.ToArray();
        }
    }
}
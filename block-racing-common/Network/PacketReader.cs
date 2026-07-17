using System;
using System.Text;

namespace block_racing_common.Network
{
    public class PacketReader
    {
        private readonly byte[] _buffer;
        private int _pos;

        public PacketReader(byte[] buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        private void EnsureSize(int size)
        {
            if (_pos + size > _buffer.Length)
                throw new Exception($"PacketReader overflow: pos={_pos}, size={size}, len={_buffer.Length}");
        }

        public bool ReadBool()
        {
            EnsureSize(sizeof(bool));
            bool value = BitConverter.ToBoolean(_buffer, _pos);
            _pos += sizeof(bool);
            return value;
        }

        public byte ReadByte()
        {
            EnsureSize(sizeof(byte));
            byte value = _buffer[_pos];
            _pos += sizeof(byte);
            return value;
        }

        public ushort ReadUInt16()
        {
            EnsureSize(sizeof(ushort));

            ushort value = BitConverter.ToUInt16(_buffer, _pos);
            _pos += sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            EnsureSize(sizeof(int));

            int value = BitConverter.ToInt32(_buffer, _pos);
            _pos += sizeof(int);
            return value;
        }

        public long ReadLong()
        {
            EnsureSize(sizeof(long));
            long value = BitConverter.ToInt64(_buffer, _pos);
            _pos += sizeof(long);
            return value;
        }

        public float ReadFloat()
        {
            EnsureSize(sizeof(float));
            float value = BitConverter.ToSingle(_buffer, _pos);
            _pos += sizeof(float);
            return value;
        }

        public string ReadString()
        {
            ushort len = ReadUInt16();

            EnsureSize(len);

            string value = Encoding.UTF8.GetString(_buffer, _pos, len);
            _pos += len;

            return value;
        }

        public int Remaining => _buffer.Length - _pos;
    }
}
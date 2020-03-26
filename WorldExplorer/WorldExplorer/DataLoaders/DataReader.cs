using System;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    class DataReader
    {
        readonly byte[] _data;
        readonly int _baseOffset;
        readonly int _length;
        int _offset;

        public DataReader(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            _data = data;
            _baseOffset = 0;
            _length = _data.Length;
        }

        public DataReader(byte[] data, int baseOffset, int length)
        {
            _data = data;
            _baseOffset = baseOffset;
            _length = length;

            _offset = 0;
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int RealOffset
        {
            get { return _baseOffset + _offset; }
            set { SetOffset(value-_baseOffset);}
        }

        public int Offset
        {
            get { return _offset; }
            set { SetOffset(value); }
        }

        public int Length
        {
            get { return _length; }
        }

        public void SetOffset(int offset)
        {
            if (offset < 0 || offset > Length)
                throw new ArgumentOutOfRangeException("offset");

            _offset = offset;
        }

        public void Skip(int bytesToSkip)
        {
            if (_offset + bytesToSkip < 0 || _offset + bytesToSkip > Length)
                throw new ArgumentOutOfRangeException("bytesToSkip");

            _offset = _offset + bytesToSkip;
        }

        public void Rewind(int bytesToRewind)
        {
            Skip(-bytesToRewind);
        }

        public byte ReadByte()
        {
            if (_offset + 1 >= Length)
                throw new IndexOutOfRangeException("No more data");

            return _data[_baseOffset + _offset++];
        }

        public byte[] ReadBytes(int count)
        {
            if (_offset + count > Length)
                throw new IndexOutOfRangeException("No more data");

            var value = new byte[count];

            Array.Copy(_data, _baseOffset + _offset, value, 0, count);

            _offset += count;

            return value;
        }

        public int ReadInt32()
        {
            var value = BitConverter.ToInt32(ReadBytes(4), 0);
            return value;
        }

        public short ReadInt16()
        {
            var value = BitConverter.ToInt16(ReadBytes(2), 0);
            return value;
        }

        public float ReadFloat()
        {
            var value = BitConverter.ToSingle(ReadBytes(4), 0);
            return value;
        }

        public double ReadDouble()
        {
            var value = BitConverter.ToDouble(ReadBytes(8), 0);
            return value;
        }

        public string ReadString(int length)
        {
            if (_offset + length >= Length)
                throw new ArgumentOutOfRangeException("length");

            var value = Encoding.ASCII.GetString(_data, _baseOffset + _offset, length);
            _offset += length;
            return value;
        }

        public string ReadZString()
        {
            int stringLength = 0;
            for (int i = _offset; i < Length; i++)
            {
                if (_data[_baseOffset + i] == 0)
                {
                    break;
                }
                stringLength++;
            }
            var value = ReadString(stringLength);
            Skip(1); // Skip the zero
            return value;
        }
    }
}

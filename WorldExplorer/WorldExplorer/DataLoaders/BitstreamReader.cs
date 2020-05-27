using System;

namespace WorldExplorer.DataLoaders
{
    class BitstreamReader
    {
        readonly byte[] _data;
        readonly int _baseOffset;
        readonly int _length;
        int _bitPosition;

        public BitstreamReader(byte[] data) : this(data, 0, data.Length)
        {
        }

        public BitstreamReader(byte[] data, int baseOffset, int length)
        {
            if (data == null) {
                throw new ArgumentNullException("data");
            }

            _data = new byte[length];
            Array.Copy(data, baseOffset, _data, 0, length);

            byte tempByte;
            for (int i = 0; i < length; i+= 2)
            {
                tempByte = _data[i];
                _data[i] = _data[i + 1];
                _data[i + 1] = tempByte;
            }

            _baseOffset = 0;
            _length = length;

            _bitPosition = 0;
        }

        public bool HasData(int requiredBits)
        {
            return _bitPosition+requiredBits <= (_length - _baseOffset) * 8;
        }

        public ushort Read(int numBits)
        {
            if (numBits <= 0 || numBits > 16) {
                throw new ArgumentException("numBits");
            }
            uint value = 0;

            // First read 16 bits from the data starting at the current bit position
            int bytePos = _bitPosition / 8;
            value = _data[_baseOffset + bytePos];
            value <<= 8;

            if (bytePos + 1 < _length) {
                value |= _data[_baseOffset + bytePos + 1];
            }
            value <<= 8;
            if (bytePos + 2 < _length)
            {
                value |= _data[_baseOffset + bytePos + 2];
            }

            value >>= (8 - (_bitPosition & 7));

            // bit 15 now contains the first bit we are interested in.

            // shift so that numBits is in bit position numBits
            value >>= 16 - numBits;

            // Mask out the unused bits
            value &= ((uint)0x0000FFFF >> (16 - numBits));


            _bitPosition += numBits;

            return (ushort)value;
        }

        public int ReadSigned(int numBits)
        {
            int v = Read(numBits);
            int maxVal = 1 << (numBits-1);
            if (v >= maxVal)
            {
                // for 8 bits, 0x80 = -x7f, 0x81 = -x7e
                // 81-80 = 1. 80-1-1
                int x = maxVal - (v - maxVal) - 1;
                v = -x;
            }
            return v;
        }

        // Test routine.
        public static void Main()
        {
            // 12345678
            var reader = new BitstreamReader(new byte[] { 0x34, 0x12, 0x78, 0x56 });
            ushort one = reader.Read(4);        // will return 0x01
            ushort twothree = reader.Read(8);   // will return 0x23
            ushort eight = reader.Read(5);      // will return 0x08
            ushort x15 = reader.Read(5);          // 101 01
        }
    }
}

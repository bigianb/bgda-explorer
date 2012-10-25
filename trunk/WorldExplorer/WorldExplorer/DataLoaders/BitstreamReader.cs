using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            _data = data;
            _baseOffset = baseOffset;
            _length = length;

            _bitPosition = 0;
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

            // shift so that numBits is in bit positino numBits
            value >>= 16 - numBits;

            // Mask out the unused bits
            value &= ((uint)0x0000FFFF >> (16 - numBits));


            _bitPosition += numBits;

            return (ushort)value;
        }

        // Test routine.
        public static void Main()
        {
            var reader = new BitstreamReader(new byte[]{0x12, 0x34, 0x56, 0x78});
            ushort one = reader.Read(4);        // will return 0x01
            ushort twothree = reader.Read(8);   // will return 0x23
            ushort eight = reader.Read(5);      // will return 0x08

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    class BitstreamReader
    {
        readonly byte[] data;
        int bitPosition;

        public BitstreamReader(byte[] data)
        {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            this.data = data;
            bitPosition = 0;
        }

        public ushort read(int numBits)
        {
            if (numBits <= 0 || numBits > 16) {
                throw new ArgumentException("numBits");
            }
            uint value = 0;

            // First read 16 bits from the data starting at the current bit position
            int bytePos = bitPosition / 8;
            value = data[bytePos];
            value <<= 8;

            if (bytePos + 1 < data.Length) {
                value |= data[bytePos + 1];
            }
            value <<= 8;
            if (bytePos + 2 < data.Length) {
                value |= data[bytePos + 2];
            }

            value >>= (8 - (bitPosition & 7));

            // bit 15 now contains the first bit we are interested in.

            // shift so that numBits is in bit positino numBits
            value >>= 16 - numBits;

            // Mask out the unused bits
            value &= ((uint)0x0000FFFF >> (16 - numBits));


            bitPosition += numBits;

            return (ushort)value;
        }

        // Test routine.
        public static void Main()
        {
            BitstreamReader reader = new BitstreamReader(new byte[]{0x12, 0x34, 0x56, 0x78});
            ushort one = reader.read(4);        // will return 0x01
            ushort twothree = reader.read(8);   // will return 0x23
            ushort eight = reader.read(5);      // will return 0x08

        }
    }
}

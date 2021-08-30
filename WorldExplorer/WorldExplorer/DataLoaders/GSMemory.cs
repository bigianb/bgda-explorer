
using System;

namespace WorldExplorer.DataLoaders
{
    class GSMemory
    {
        // GS has 4 meg of DRAM
        public byte[] mem = new byte[1024 * 1024 * 4];

        private int[] block32 =
        {
        0,  1,  4,  5, 16, 17, 20, 21,
        2,  3,  6,  7, 18, 19, 22, 23,
        8,  9, 12, 13, 24, 25, 28, 29,
        10, 11, 14, 15, 26, 27, 30, 31
    };


        private int[] columnWord32 =
        {
        0,  1,  4,  5,  8,  9, 12, 13, 2,  3,  6,  7, 10, 11, 14, 15
    };

        // writes to the memory when destination format is set to PSMCT32
        public void writeTexPSMCT32(int dbp, int dbw, int dsax, int dsay, int rrw, int rrh, ReadOnlySpan<byte> data, int dataIndex)
        {
            var startBlockPos = dbp * 64;
            for (var y = dsay; y < dsay + rrh; y++)
            {
                for (var x = dsax; x < dsax + rrw; x++)
                {
                    var pageX = x / 64;
                    var pageY = y / 32;
                    var page = pageX + pageY * dbw;

                    var px = x - (pageX * 64);
                    var py = y - (pageY * 32);

                    var blockX = px / 8;
                    var blockY = py / 8;
                    var block = block32[blockX + blockY * 8];

                    var bx = px - blockX * 8;
                    var by = py - blockY * 8;

                    var column = by / 2;

                    var cx = bx;
                    var cy = by - column * 2;
                    var cw = columnWord32[cx + cy * 8];

                    var gsIndex = startBlockPos + page * 2048 + block * 64 + column * 16 + cw;
                    gsIndex *= 4;

                    mem[gsIndex++] = data[dataIndex];
                    mem[gsIndex++] = data[dataIndex + 1];
                    mem[gsIndex++] = data[dataIndex + 2];
                    mem[gsIndex] = data[dataIndex + 3];
                    dataIndex += 4;
                }
            }
        }

        int[] block8 =
        {
        0,  1,  4,  5, 16, 17, 20, 21,
        2,  3,  6,  7, 18, 19, 22, 23,
        8,  9, 12, 13, 24, 25, 28, 29,
        10, 11, 14, 15, 26, 27, 30, 31
    };

        int[,] columnWord8 =
        {
        {
            0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
            2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,

            8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7
        },
        {
            8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,

            0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
            2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15
        }
    };

        int[] columnByte8 =
        {
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,

        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,
        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3
    };

        public byte[] readTexPSMT8(int dbp, int dbw, int dsax, int dsay, int rrw, int rrh)
        {
            var data = new byte[rrw * rrh];
            var dataIndex = 0;

            dbw >>= 1;

            var startBlockPos = dbp * 64;

            for (var y = dsay; y < dsay + rrh; y++)
            {
                for (var x = dsax; x < dsax + rrw; x++)
                {
                    var pageX = x / 128;
                    var pageY = y / 64;
                    var page = pageX + pageY * dbw;

                    var px = x - (pageX * 128);
                    var py = y - (pageY * 64);

                    var blockX = px / 16;
                    var blockY = py / 16;
                    var block = block8[blockX + blockY * 8];

                    var bx = px - blockX * 16;
                    var by = py - blockY * 16;

                    var column = by / 4;

                    var cx = bx;
                    var cy = by - column * 4;
                    var cw = columnWord8[column & 1, cx + cy * 16];
                    var cb = columnByte8[cx + cy * 16];

                    var gsIndex = startBlockPos + page * 2048 + block * 64 + column * 16 + cw;
                    gsIndex *= 4;

                    data[dataIndex++] = mem[gsIndex + cb];
                }
            }
            return data;
        }

        private int[] block4 =
        {
        0,  2,  8, 10,
        1,  3,  9, 11,
        4,  6, 12, 14,
        5,  7, 13, 15,
        16, 18, 24, 26,
        17, 19, 25, 27,
        20, 22, 28, 30,
        21, 23, 29, 31
    };

        private int[,] columnWord4 =
        {
        {
            0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
            2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,

            8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7
        },
        {
            8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,

            0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
            2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15
        }
    };

        private int[] columnByte4 =
        {
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,

        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7,
        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7
    };

        public byte[] readTexPSMT4(int dbp, int dbw, int dsax, int dsay, int rrw, int rrh)
        {

            var wBytes = rrw / 2;
            var data = new byte[wBytes * rrh];

            dbw >>= 1;
            var dataIndex = 0;
            var startBlockPos = dbp * 64;

            var odd = false;

            for (var y = dsay; y < dsay + rrh; y++)
            {
                for (var x = dsax; x < dsax + rrw; x++)
                {
                    var pageX = x / 128;
                    var pageY = y / 128;
                    var page = pageX + pageY * dbw;

                    var px = x - (pageX * 128);
                    var py = y - (pageY * 128);

                    var blockX = px / 32;
                    var blockY = py / 16;
                    var block = block4[blockX + blockY * 4];

                    var bx = px - blockX * 32;
                    var by = py - blockY * 16;

                    var column = by / 4;

                    var cx = bx;
                    var cy = by - column * 4;
                    var cw = columnWord4[column & 1, cx + cy * 32];
                    var cb = columnByte4[cx + cy * 32];

                    var gsIndex = 4 * (startBlockPos + page * 2048 + block * 64 + column * 16 + cw);
                    gsIndex += (cb >> 1);

                    int gsVal = mem[gsIndex];
                    int dataVal = data[dataIndex];
                    if ((cb & 1) == 1)
                    {
                        if (odd)
                        {
                            dataVal = (dataVal & 0x0f) | (gsVal & 0xf0);
                        }
                        else
                        {
                            dataVal = (dataVal & 0xf0) | ((gsVal >> 4) & 0x0f);
                        }
                    }
                    else
                    {
                        if (odd)
                        {
                            dataVal = (dataVal & 0x0f) | ((gsVal << 4) & 0xf0);
                        }
                        else
                        {
                            dataVal = (dataVal & 0xf0) | (gsVal & 0x0f);
                        }
                    }
                    data[dataIndex] = (byte)dataVal;
                    if (odd)
                    {
                        ++dataIndex;
                    }
                    odd = !odd;
                }
            }
            return data;
        }
    }
}

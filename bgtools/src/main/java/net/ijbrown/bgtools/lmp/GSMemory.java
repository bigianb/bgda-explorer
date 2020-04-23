package net.ijbrown.bgtools.lmp;

public class GSMemory {

    // GS has 4 meg of DRAM
    public byte[] mem = new byte[1024*1024*4];

    private final int[] block32 =
    {
        0,  1,  4,  5, 16, 17, 20, 21,
        2,  3,  6,  7, 18, 19, 22, 23,
        8,  9, 12, 13, 24, 25, 28, 29,
        10, 11, 14, 15, 26, 27, 30, 31
    };


    private final int[] columnWord32 =
    {
        0,  1,  4,  5,  8,  9, 12, 13, 2,  3,  6,  7, 10, 11, 14, 15
    };

    // writes to the memory when destination format is set to PSMCT32
    void writeTexPSMCT32(int dbp, int dbw, int dsax, int dsay, int rrw, int rrh, byte[] data, int dataIndex)
    {
        int startBlockPos = dbp * 64;
        for(int y = dsay; y < dsay + rrh; y++)
        {
            for(int x = dsax; x < dsax + rrw; x++)
            {
                int pageX = x / 64;
                int pageY = y / 32;
                int page  = pageX + pageY * dbw;

                int px = x - (pageX * 64);
                int py = y - (pageY * 32);

                int blockX = px / 8;
                int blockY = py / 8;
                int block  = block32[blockX + blockY * 8];

                int bx = px - blockX * 8;
                int by = py - blockY * 8;

                int column = by / 2;

                int cx = bx;
                int cy = by - column * 2;
                int cw = columnWord32[cx + cy * 8];

                int gsIndex = startBlockPos + page * 2048 + block * 64 + column * 16 + cw;

                mem[gsIndex++] = data[dataIndex + 3];
                mem[gsIndex++] = data[dataIndex + 2];
                mem[gsIndex++] = data[dataIndex + 1];
                mem[gsIndex] = data[dataIndex];

                dataIndex += 4;
            }
        }
    }

    private final int[] block4 =
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

    private final int[][] columnWord4 =
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

    private final int[] columnByte4 =
    {
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,
        0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,

        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7,
        1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7
    };

    public byte[] readTexPSMT4(int dbp, int dbw, int dsax, int dsay, int rrw, int rrh) {

        int wBytes = (rrw+1)/2;
        byte []data = new byte[wBytes*rrh];

        dbw >>= 1;
        int dataIndex = 0;
        int startBlockPos = dbp * 64;

        boolean odd = false;

        for (int y = dsay; y < dsay + rrh; y++) {
            for (int x = dsax; x < dsax + rrw; x++) {
                int pageX = x / 128;
                int pageY = y / 128;
                int page = pageX + pageY * dbw;

                int px = x - (pageX * 128);
                int py = y - (pageY * 128);

                int blockX = px / 32;
                int blockY = py / 16;
                int block = block4[blockX + blockY * 4];

                int bx = px - blockX * 32;
                int by = py - blockY * 16;

                int column = by / 4;

                int cx = bx;
                int cy = by - column * 4;
                int cw = columnWord4[column & 1][cx + cy * 32];
                int cb = columnByte4[cx + cy * 32];

                int gsIndex = 4 * (startBlockPos + page * 2048 + block * 64 + column * 16 + cw);
                gsIndex += (cb >> 1);

                int gsVal = mem[gsIndex];
                int dataVal = data[dataIndex];
                if ((cb & 1) == 1) {
                    if (odd) {
                        dataVal = (dataVal & 0x0f) | (gsVal & 0xf0);
                    } else {
                        dataVal = (dataVal & 0xf0) | ((gsVal >> 4) & 0x0f);
                    }
                } else {
                    if (odd) {
                        dataVal = (dataVal & 0x0f) | ((gsVal << 4) & 0xf0);
                    } else {
                        dataVal = (dataVal & 0xf0) | (gsVal & 0x0f);
                    }
                }
                data[dataIndex] = (byte) dataVal;
                if (odd) {
                    ++dataIndex;
                }
                odd = !odd;
            }
        }
        return data;
    }
}

namespace JetBlackEngineLib.Data.Textures;

internal class GsMemory
{
    // GS has 4 meg of DRAM
    private const int MemorySize = 1024 * 1024 * 4;
    
    private static readonly int[] Block32 =
    {
        0, 1, 4, 5, 16, 17, 20, 21, 2, 3, 6, 7, 18, 19, 22, 23, 8, 9, 12, 13, 24, 25, 28, 29, 10, 11, 14, 15,
        26, 27, 30, 31
    };

    private static readonly int[] Block4 =
    {
        0, 2, 8, 10, 1, 3, 9, 11, 4, 6, 12, 14, 5, 7, 13, 15, 16, 18, 24, 26, 17, 19, 25, 27, 20, 22, 28, 30,
        21, 23, 29, 31
    };

    private static readonly int[] Block8 =
    {
        0, 1, 4, 5, 16, 17, 20, 21, 2, 3, 6, 7, 18, 19, 22, 23, 8, 9, 12, 13, 24, 25, 28, 29, 10, 11, 14, 15,
        26, 27, 30, 31
    };

    private static readonly int[] ColumnByte4 =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4, 6, 6, 6, 6, 6, 6, 6, 6, 0, 0, 0,
        0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4, 6, 6, 6, 6, 6, 6, 6, 6, 1, 1, 1, 1, 1, 1,
        1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 5, 5, 5, 5, 5, 5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7, 1, 1, 1, 1, 1, 1, 1, 1, 3,
        3, 3, 3, 3, 3, 3, 3, 5, 5, 5, 5, 5, 5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7
    };

    private static readonly int[] ColumnByte8 =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 3, 3, 3
    };


    private static readonly int[] ColumnWord32 = {0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15};

    private static readonly int[,] ColumnWord4 =
    {
        {
            0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13,
            2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11,
            14, 15, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0,
            1, 4, 5, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14,
            15, 2, 3, 6, 7
        },
        {
            8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5,
            10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2,
            3, 6, 7, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9,
            12, 13, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7,
            10, 11, 14, 15
        }
    };

    private static readonly int[,] ColumnWord8 =
    {
        {
            0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14,
            15, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2,
            3, 6, 7
        },
        {
            8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6,
            7, 0, 1, 4, 5, 8, 9, 12, 13, 0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15, 2, 3, 6, 7, 10, 11,
            14, 15
        }
    };
    
    private readonly byte[] _memory = new byte[MemorySize];

    /// <summary>
    /// Writes to the memory when destination format is set to PSMCT32
    /// </summary>
    /// <param name="dbp"></param>
    /// <param name="dbw"></param>
    /// <param name="dsaX"></param>
    /// <param name="dsaY"></param>
    /// <param name="rrW"></param>
    /// <param name="rrH"></param>
    /// <param name="data"></param>
    /// <param name="dataIndex"></param>
    public void WriteTexPSMCT32(int dbp, int dbw, int dsaX, int dsaY, int rrW, int rrH, ReadOnlySpan<byte> data,
        int dataIndex)
    {
        var startBlockPos = dbp * 64;
        for (var y = dsaY; y < dsaY + rrH; y++) {
            for (var x = dsaX; x < dsaX + rrW; x++)
            {
                var pageX = x / 64;
                var pageY = y / 32;
                var page = pageX + (pageY * dbw);

                var px = x - (pageX * 64);
                var py = y - (pageY * 32);

                var blockX = px / 8;
                var blockY = py / 8;
                var block = Block32[blockX + (blockY * 8)];

                var bx = px - (blockX * 8);
                var by = py - (blockY * 8);

                var column = by / 2;

                var cx = bx;
                var cy = by - (column * 2);
                var cw = ColumnWord32[cx + (cy * 8)];

                var gsIndex = startBlockPos + (page * 2048) + (block * 64) + (column * 16) + cw;
                gsIndex *= 4;

                if (dataIndex+3 < data.Length)
                {
                    _memory[gsIndex++] = data[dataIndex];
                    _memory[gsIndex++] = data[dataIndex + 1];
                    _memory[gsIndex++] = data[dataIndex + 2];
                    _memory[gsIndex] = data[dataIndex + 3];
                    dataIndex += 4;
                }
            }
        }
    }

    public byte[] ReadTexPSMT8(int dbp, int dbw, int dsaX, int dsaY, int rrW, int rrH)
    {
        var data = new byte[rrW * rrH];
        var dataIndex = 0;

        dbw >>= 1;

        var startBlockPos = dbp * 64;

        for (var y = dsaY; y < dsaY + rrH; y++)
        for (var x = dsaX; x < dsaX + rrW; x++)
        {
            var pageX = x / 128;
            var pageY = y / 64;
            var page = pageX + (pageY * dbw);

            var px = x - (pageX * 128);
            var py = y - (pageY * 64);

            var blockX = px / 16;
            var blockY = py / 16;
            var block = Block8[blockX + (blockY * 8)];

            var bx = px - (blockX * 16);
            var by = py - (blockY * 16);

            var column = by / 4;

            var cx = bx;
            var cy = by - (column * 4);
            var cw = ColumnWord8[column & 1, cx + (cy * 16)];
            var cb = ColumnByte8[cx + (cy * 16)];

            var gsIndex = startBlockPos + (page * 2048) + (block * 64) + (column * 16) + cw;
            gsIndex *= 4;

            data[dataIndex++] = _memory[gsIndex + cb];
        }

        return data;
    }

    public byte[] ReadTexPSMT4(int dbp, int dbw, int dsaX, int dsaY, int rrW, int rrH)
    {
        var wBytes = rrW / 2;
        var data = new byte[wBytes * rrH];

        dbw >>= 1;
        var dataIndex = 0;
        var startBlockPos = dbp * 64;

        var odd = false;

        for (var y = dsaY; y < dsaY + rrH; y++)
        for (var x = dsaX; x < dsaX + rrW; x++)
        {
            var pageX = x / 128;
            var pageY = y / 128;
            var page = pageX + (pageY * dbw);

            var px = x - (pageX * 128);
            var py = y - (pageY * 128);

            var blockX = px / 32;
            var blockY = py / 16;
            var block = Block4[blockX + (blockY * 4)];

            var bx = px - (blockX * 32);
            var by = py - (blockY * 16);

            var column = by / 4;

            var cx = bx;
            var cy = by - (column * 4);
            var cw = ColumnWord4[column & 1, cx + (cy * 32)];
            var cb = ColumnByte4[cx + (cy * 32)];

            var gsIndex = 4 * (startBlockPos + (page * 2048) + (block * 64) + (column * 16) + cw);
            gsIndex += cb >> 1;

            int gsVal = _memory[gsIndex];
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

        return data;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorldExplorer.DataLoaders
{
    public class WorldTexFile
    {
        public WorldTexFile(EngineVersion engineVersion, string filepath)
        {
            _engineVersion = engineVersion;
            _filepath = filepath;
            fileData = File.ReadAllBytes(filepath);
            Filename = Path.GetFileName(_filepath);

            if (EngineVersion.ReturnToArms == _engineVersion || EngineVersion.JusticeLeagueHeroes == _engineVersion)
            {
                _entries = ReadEntries(fileData);
            }
        }

        private readonly EngineVersion _engineVersion;
        private readonly TexEntry[] _entries;
        private string _filepath;
        public string Filename;
        public byte[] fileData;

        private Dictionary<int, WriteableBitmap> texMap = new Dictionary<int, WriteableBitmap>();

        public WriteableBitmap GetBitmapRTA(int x, int y, int textureNumber)
        {
            foreach (var entry in _entries)
            {
                var _y = entry.CellOffset / 100;
                var _x = entry.CellOffset % 100;
                if (_x == x && _y == y)
                {
                    return GetBitmap(entry.DirectoryOffset, textureNumber);
                }
            }
            return null;
        }

        public WriteableBitmap GetBitmap(int chunkStartOffset, int textureNumber)
        {
            var numTexturesinChunk = DataUtil.getLEInt(fileData, chunkStartOffset);
            if (textureNumber > numTexturesinChunk)
            {
                return null;
            }
            var offset = chunkStartOffset + textureNumber * 0x40;
            if (!texMap.TryGetValue(offset, out var tex))
            {
                tex = Decode(offset, chunkStartOffset);
                texMap.Add(offset, tex);
            }
            return tex;
        }

        public WriteableBitmap Decode(int offset, int chunkStartOffset)
        {
            // Dark Alliance encodes pointers as offsets from the entry in the texture entry table.
            // Return to arms (more sensibly) encodes pointers as offsets from the current chunk loaded from the disc.
            var deltaOffset = EngineVersion.DarkAlliance == _engineVersion ? offset : chunkStartOffset;

            var pixelWidth = DataUtil.getLEUShort(fileData, offset);
            var pixelHeight = DataUtil.getLEUShort(fileData, offset + 2);
            var header10 = DataUtil.getLEInt(fileData, offset + 0x10);
            var compressedDataLen = DataUtil.getLEInt(fileData, offset + 0x14);
            var compressedDataOffset = header10 + deltaOffset;
            if (compressedDataOffset <= 0 || compressedDataOffset >= fileData.Length)
            {
                return null;
            }
            var palOffset = DataUtil.getLEInt(fileData, compressedDataOffset) + deltaOffset;

            var palette = PalEntry.readPalette(fileData, palOffset, 16, 16);
            palette = PalEntry.unswizzlePalette(palette);
            var huffVals = decodeHuff(palOffset + 0xc00);

            var p = compressedDataOffset + 4;

            var width = (pixelWidth + 0x0f) & ~0x0f;
            var height = (pixelHeight + 0x0f) & ~0x0f;

            var image = new WriteableBitmap(
                    width, height,
                    96, 96,
                    PixelFormats.Bgra32,
                    null);
            image.Lock();

            while (fileData[p] != 0xFF)
            {
                int x0 = fileData[p];
                int y0 = fileData[p + 1];
                int x1 = fileData[p + 2];
                int y1 = fileData[p + 3];
                p += 4;
                for (var yblock = y0; yblock <= y1; ++yblock)
                {
                    for (var xblock = x0; xblock <= x1; ++xblock)
                    {
                        var blockDataStart = DataUtil.getLEInt(fileData, p) + deltaOffset;
                        decodeBlock(xblock, yblock, blockDataStart, palOffset + 0x400, image, palette, huffVals);
                        p += 4;
                    }
                }
            }
            // Specify the area of the bitmap that changed.
            image.AddDirtyRect(new Int32Rect(0, 0, width, height));

            // Release the back buffer and make it available for display.
            image.Unlock();

            return image;
        }

        private int[] backJumpTable = new int[] { -1, -16, -17, -15, -2 };

        private void decodeBlock(int xblock, int yblock, int blockDataStart, int table0Start, WriteableBitmap image, PalEntry[] palette, HuffVal[] huffVals)
        {
            var tableOffset = table0Start + 0x800;
            var table1Len = DataUtil.getLEInt(fileData, tableOffset) * 2;
            var table1Start = tableOffset + 4;
            var table2Start = table1Start + table1Len;
            var table3Start = table2Start + 0x48;

            var pix8s = new int[16 * 16];
            var curpix8 = 0;
            var startBit = 0;
            var prevPixel = 0;
            for (var y = 0; y < 16; ++y)
            {
                for (var x = 0; x < 16; ++x)
                {
                    var startWordIdx = startBit / 16;
                    int word1 = DataUtil.getLEUShort(fileData, blockDataStart + startWordIdx * 2);
                    int word2 = DataUtil.getLEUShort(fileData, blockDataStart + startWordIdx * 2 + 2);
                    // if startBit is 0, word == word1
                    // if startBit is 1, word is 15 bits of word1 and 1 bit of word2
                    var word = ((word1 << 16 | word2) >> (16 - (startBit & 0x0f))) & 0xFFFF;

                    var byte1 = (word >> 8) & 0xff;
                    var hv = huffVals[byte1];
                    int pixCmd;
                    if (hv.numBits != 0)
                    {
                        pixCmd = hv.val;
                        startBit += hv.numBits;
                    }
                    else
                    {
                        // Must be more than an 8 bit code
                        var bit = 9;
                        var a = word >> (16 - bit);
                        var v = DataUtil.getLEInt(fileData, table3Start + bit * 4);
                        while (v < a)
                        {
                            ++bit;
                            if (bit > 16)
                            {
                                throw new Exception("A decoding error occured");
                            }
                            a = word >> (16 - bit);
                            v = DataUtil.getLEInt(fileData, table3Start + bit * 4);
                        }
                        startBit += bit;
                        var val = DataUtil.getLEInt(fileData, table2Start + bit * 4);
                        var table1Index = a + val;

                        pixCmd = DataUtil.getLEShort(fileData, table1Start + table1Index * 2);
                    }
                    var pix8 = 0;
                    if (pixCmd < 0x100)
                    {
                        pix8 = pixCmd;
                    }
                    else if (pixCmd < 0x105)
                    {
                        var backjump = backJumpTable[pixCmd - 0x100];
                        if ((curpix8 + backjump) >= 0)
                        {
                            pix8 = pix8s[curpix8 + backjump];
                        }
                        else
                        {
                            throw new Exception("Something went wrong");
                        }
                    }
                    else
                    {
                        var table0Index = (pixCmd - 0x105) + prevPixel * 8;
                        pix8 = fileData[table0Start + table0Index] & 0xFF;
                    }

                    pix8s[curpix8++] = pix8;

                    prevPixel = pix8 & 0xFF;
                    var pixel = palette[pix8 & 0xFF];
                    var pBackBuffer = image.BackBuffer;
                    var xpos = xblock * 16 + x;
                    var ypos = yblock * 16 + y;
                    var p = pBackBuffer + ypos * image.BackBufferStride + xpos * 4;
                    unsafe
                    {
                        *((int*)p) = pixel.argb();
                    }
                }
            }
        }

        private HuffVal[] decodeHuff(int tableOffset)
        {
            var huffOut = new HuffVal[256];

            var table1Len = DataUtil.getLEInt(fileData, tableOffset) * 2;
            var table1Start = tableOffset + 4;
            var table2Start = table1Start + table1Len;
            var table3Start = table2Start + 0x48;

            for (var i = 0; i < 256; ++i)
            {
                var bit = 1;
                var a = i >> (8 - bit);
                var v = DataUtil.getLEInt(fileData, table3Start + bit * 4);
                while (v < a)
                {
                    ++bit;
                    if (bit > 8)
                    {
                        break;
                    }
                    a = i >> (8 - bit);
                    v = DataUtil.getLEInt(fileData, table3Start + bit * 4);
                }
                huffOut[i] = new HuffVal();
                if (bit <= 8)
                {
                    var val = DataUtil.getLEInt(fileData, table2Start + bit * 4);
                    var table1Index = a + val;
                    huffOut[i].val = DataUtil.getLEShort(fileData, table1Start + table1Index * 2);
                    huffOut[i].numBits = (short)bit;
                }
            }

            return huffOut;
        }

        public static TexEntry[] ReadEntries(byte[] fileData)
        {
            var entries = new List<TexEntry>();

            var reader = new DataReader(fileData);

            // Unknown
            reader.ReadInt32();

            while (true)
            {
                var entry = new TexEntry
                {
                    CellOffset = reader.ReadInt32(),
                    DirectoryOffset = reader.ReadInt32(),
                    Size = reader.ReadInt32()
                };

                if (entry.CellOffset < 0)
                {
                    break;
                }

                entries.Add(entry);
            }

            return entries.ToArray();
        }
        class HuffVal
        {
            public short val;
            public short numBits;
        }
        public class TexEntry
        {
            public int CellOffset;
            public int DirectoryOffset;
            public int Size;
        }
    }
}

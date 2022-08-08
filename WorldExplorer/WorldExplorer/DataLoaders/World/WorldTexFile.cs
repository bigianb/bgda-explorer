using JetBlackEngineLib;
using JetBlackEngineLib.Io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders.World
{
    public class WorldTexFile
    {
        private readonly TexEntry[] _entries;

        public EngineVersion EngineVersion { get; }
        public byte[] FileData { get; }
        public string FileName { get; }
        
        private readonly int[] _backJumpTable = {-1, -16, -17, -15, -2};
        private readonly Dictionary<int, WriteableBitmap> _texMap = new();
        
        public WorldTexFile(EngineVersion engineVersion, string filepath)
        {
            EngineVersion = engineVersion;
            FileData = File.ReadAllBytes(filepath);
            FileName = Path.GetFileName(filepath);

            if (EngineVersion is EngineVersion.ReturnToArms or EngineVersion.JusticeLeagueHeroes)
            {
                _entries = ReadEntries(FileData);
            }
            else
            {
                _entries = Array.Empty<TexEntry>();
            }
        }
        
        public WriteableBitmap? GetBitmapRTA(WorldElementDataInfo dataInfo)
        {
            foreach (var entry in _entries)
            {
                var _div = entry.CellOffset / 100;
                var _mod = entry.CellOffset % 100;
                if (_mod == dataInfo.TextureMod && _div == dataInfo.TextureDiv)
                {
                    return GetBitmap(entry.DirectoryOffset, dataInfo.TextureNumber);
                }
            }
            return null;
        }
        
        public WriteableBitmap? GetBitmapBGDA(WorldElementDataInfo dataInfo, WorldData worldData)
        {
            if (worldData.TextureChunkOffsets == null) return null;
            return GetBitmap(
                worldData.TextureChunkOffsets[dataInfo.TextureDiv, dataInfo.TextureMod],
                dataInfo.TextureNumber
            );
        }

        public WriteableBitmap? GetBitmapRTA(int mod, int div, int textureNumber)
        {
            foreach (var entry in _entries)
            {
                var _div = entry.CellOffset / 100;
                var _mod = entry.CellOffset % 100;
                if (_mod == mod && _div == div)
                {
                    return GetBitmap(entry.DirectoryOffset, textureNumber);
                }
            }

            return null;
        }

        public WriteableBitmap? GetBitmap(int chunkStartOffset, int textureNumber)
        {
            var numTexturesInChunk = DataUtil.getLEInt(FileData, chunkStartOffset);
            if (textureNumber > numTexturesInChunk)
            {
                return null;
            }

            var offset = chunkStartOffset + (textureNumber * 0x40);
            if (_texMap.TryGetValue(offset, out var tex))
            {
                return tex;
            }

            tex = Decode(offset, chunkStartOffset);
            if (tex != null)
                _texMap.Add(offset, tex);

            return tex;
        }

        public WriteableBitmap? Decode(int offset, int chunkStartOffset)
        {
            // Dark Alliance encodes pointers as offsets from the entry in the texture entry table.
            // Return to arms (more sensibly) encodes pointers as offsets from the current chunk loaded from the disc.
            var deltaOffset = EngineVersion.DarkAlliance == EngineVersion ? offset : chunkStartOffset;

            var pixelWidth = DataUtil.getLEUShort(FileData, offset);
            var pixelHeight = DataUtil.getLEUShort(FileData, offset + 2);
            var header10 = DataUtil.getLEInt(FileData, offset + 0x10);
            // Unused for now
            // var compressedDataLen = DataUtil.getLEInt(FileData, offset + 0x14);
            var compressedDataOffset = header10 + deltaOffset;
            if (compressedDataOffset <= 0 || compressedDataOffset >= FileData.Length)
            {
                return null;
            }

            var palOffset = DataUtil.getLEInt(FileData, compressedDataOffset) + deltaOffset;

            var palette = PalEntry.readPalette(FileData, palOffset, 16, 16);
            palette = PalEntry.unswizzlePalette(palette);
            var huffVals = DecodeHuff(palOffset + 0xc00);

            var p = compressedDataOffset + 4;

            var width = (pixelWidth + 0x0f) & ~0x0f;
            var height = (pixelHeight + 0x0f) & ~0x0f;

            WriteableBitmap image = new(
                width, height,
                96, 96,
                PixelFormats.Bgra32,
                null);
            image.Lock();

            while (FileData[p] != 0xFF)
            {
                int x0 = FileData[p];
                int y0 = FileData[p + 1];
                int x1 = FileData[p + 2];
                int y1 = FileData[p + 3];
                p += 4;
                for (var yBlock = y0; yBlock <= y1; ++yBlock)
                for (var xBlock = x0; xBlock <= x1; ++xBlock)
                {
                    var blockDataStart = DataUtil.getLEInt(FileData, p) + deltaOffset;
                    DecodeBlock(xBlock, yBlock, blockDataStart, palOffset + 0x400, image, palette, huffVals);
                    p += 4;
                }
            }

            // Specify the area of the bitmap that changed.
            image.AddDirtyRect(new Int32Rect(0, 0, width, height));

            // Release the back buffer and make it available for display.
            image.Unlock();

            return image;
        }

        private void DecodeBlock(int xBlock, int yBlock, int blockDataStart, int table0Start, WriteableBitmap image,
            PalEntry[] palette, HuffVal[] huffVals)
        {
            var tableOffset = table0Start + 0x800;
            var table1Len = DataUtil.getLEInt(FileData, tableOffset) * 2;
            var table1Start = tableOffset + 4;
            var table2Start = table1Start + table1Len;
            var table3Start = table2Start + 0x48;

            var pix8s = new int[16 * 16];
            var curpix8 = 0;
            var startBit = 0;
            var prevPixel = 0;
            for (var y = 0; y < 16; ++y)
            for (var x = 0; x < 16; ++x)
            {
                var startWordIdx = startBit / 16;
                int word1 = DataUtil.getLEUShort(FileData, blockDataStart + (startWordIdx * 2));
                int word2 = DataUtil.getLEUShort(FileData, blockDataStart + (startWordIdx * 2) + 2);
                // if startBit is 0, word == word1
                // if startBit is 1, word is 15 bits of word1 and 1 bit of word2
                var word = (((word1 << 16) | word2) >> (16 - (startBit & 0x0f))) & 0xFFFF;

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
                    var v = DataUtil.getLEInt(FileData, table3Start + (bit * 4));
                    while (v < a)
                    {
                        ++bit;
                        if (bit > 16)
                        {
                            throw new Exception("A decoding error occured");
                        }

                        a = word >> (16 - bit);
                        v = DataUtil.getLEInt(FileData, table3Start + (bit * 4));
                    }

                    startBit += bit;
                    var val = DataUtil.getLEInt(FileData, table2Start + (bit * 4));
                    var table1Index = a + val;

                    pixCmd = DataUtil.getLEShort(FileData, table1Start + (table1Index * 2));
                }

                int pix8;
                if (pixCmd < 0x100)
                {
                    pix8 = pixCmd;
                }
                else if (pixCmd < 0x105)
                {
                    var backjump = _backJumpTable[pixCmd - 0x100];
                    if (curpix8 + backjump >= 0)
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
                    var table0Index = pixCmd - 0x105 + (prevPixel * 8);
                    pix8 = FileData[table0Start + table0Index] & 0xFF;
                }

                pix8s[curpix8++] = pix8;

                prevPixel = pix8 & 0xFF;
                var pixel = palette[pix8 & 0xFF];
                var pBackBuffer = image.BackBuffer;
                var xpos = (xBlock * 16) + x;
                var ypos = (yBlock * 16) + y;
                var p = pBackBuffer + (ypos * image.BackBufferStride) + (xpos * 4);
                Marshal.WriteInt32(p, pixel.argb());
            }
        }

        private HuffVal[] DecodeHuff(int tableOffset)
        {
            var huffOut = new HuffVal[256];

            var table1Len = DataUtil.getLEInt(FileData, tableOffset) * 2;
            var table1Start = tableOffset + 4;
            var table2Start = table1Start + table1Len;
            var table3Start = table2Start + 0x48;

            for (var i = 0; i < 256; ++i)
            {
                var bit = 1;
                var a = i >> (8 - bit);
                var v = DataUtil.getLEInt(FileData, table3Start + (bit * 4));
                while (v < a)
                {
                    ++bit;
                    if (bit > 8)
                    {
                        break;
                    }

                    a = i >> (8 - bit);
                    v = DataUtil.getLEInt(FileData, table3Start + (bit * 4));
                }

                huffOut[i] = new HuffVal();
                if (bit <= 8)
                {
                    var val = DataUtil.getLEInt(FileData, table2Start + (bit * 4));
                    var table1Index = a + val;
                    huffOut[i].val = DataUtil.getLEShort(FileData, table1Start + (table1Index * 2));
                    huffOut[i].numBits = (short)bit;
                }
            }

            return huffOut;
        }

        public static TexEntry[] ReadEntries(byte[] fileData)
        {
            List<TexEntry> entries = new();

            DataReader reader = new(fileData);

            // Unknown
            reader.ReadInt32();

            while (true)
            {
                TexEntry entry = new()
                {
                    CellOffset = reader.ReadInt32(), DirectoryOffset = reader.ReadInt32(), Size = reader.ReadInt32()
                };

                if (entry.CellOffset < 0)
                {
                    break;
                }

                entries.Add(entry);
            }

            return entries.ToArray();
        }

        private class HuffVal
        {
            public short numBits;
            public short val;
        }

        public class TexEntry
        {
            public int CellOffset;
            public int DirectoryOffset;
            public int Size;
        }
    }
}
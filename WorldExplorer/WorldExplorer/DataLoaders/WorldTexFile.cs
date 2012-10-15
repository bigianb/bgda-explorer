using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;

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
        }

        private readonly EngineVersion _engineVersion;

        private string _filepath;

        public String Filename;

        public byte[] fileData;


        private Dictionary<int, WriteableBitmap> texMap = new Dictionary<int, WriteableBitmap>();

        public WriteableBitmap GetBitmap(int chunkStartOffset, int textureNumber)
        {
            int numTexturesinChunk = DataUtil.getLEInt(fileData, chunkStartOffset);
            if (textureNumber > numTexturesinChunk)
            {
                return null;
            }
            int offset = chunkStartOffset + textureNumber * 0x40;
            WriteableBitmap tex = null;
            if (!texMap.TryGetValue(offset, out tex)){
                tex = Decode(offset, chunkStartOffset);
                texMap.Add(offset, tex);
            }
            return tex;
        }

        public WriteableBitmap Decode(int offset, int chunkStartOffset)
        {
            // Dark Alliance encodes pointers as offsets from the entry in the texture entry table.
            // Return to arms (more sensibly) encodes pointers as offsets from the current chunk loaded from the disc.
            int deltaOffset = EngineVersion.DarkAlliance == _engineVersion ? offset : chunkStartOffset;

            int pixelWidth = DataUtil.getLEUShort(fileData, offset);
            int pixelHeight = DataUtil.getLEUShort(fileData, offset+2);
            int header10 =  DataUtil.getLEInt(fileData, offset + 0x10);
            int compressedDataLen = DataUtil.getLEInt(fileData, offset + 0x14);
            int compressedDataOffset = header10 + deltaOffset;
            if (compressedDataOffset <= 0 || compressedDataOffset >= fileData.Length)
            {
                return null;
            }
            int palOffset = DataUtil.getLEInt(fileData, compressedDataOffset) + deltaOffset;

            PalEntry[] palette = PalEntry.readPalette(fileData, palOffset, 16, 16);
            palette = PalEntry.unswizzlePalette(palette);
            HuffVal[] huffVals = decodeHuff(palOffset+0xc00);

            int p = compressedDataOffset+4;

            int width = (pixelWidth + 0x0f) & ~0x0f;
            int height = (pixelHeight + 0x0f) & ~0x0f;

            WriteableBitmap image = new WriteableBitmap(
                    width, height,
                    96, 96,
                    PixelFormats.Bgr32,
                    null);
            image.Lock();

            while (fileData[p] != 0xFF) {
                int x0 = fileData[p];
                int y0 = fileData[p + 1];
                int x1 = fileData[p + 2];
                int y1 = fileData[p + 3];
                p += 4;
                for (int yblock = y0; yblock <= y1; ++yblock) {
                    for (int xblock = x0; xblock <= x1; ++xblock) {
                        int blockDataStart = DataUtil.getLEInt(fileData, p) + deltaOffset;
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

        private int[] backJumpTable = new int[] {-1, -16, -17, -15, -2};

        private void decodeBlock(int xblock, int yblock, int blockDataStart, int table0Start, WriteableBitmap image, PalEntry[] palette, HuffVal[] huffVals)
        {
            int tableOffset = table0Start + 0x800;
            int table1Len = DataUtil.getLEInt(fileData, tableOffset) * 2;
            int table1Start = tableOffset + 4;
            int table2Start = table1Start + table1Len;
            int table3Start = table2Start + 0x48;

            int[] pix8s = new int[16*16];
            int curpix8=0;
            int startBit=0;
            int prevPixel=0;
            for (int y=0; y<16; ++y){
                for (int x=0; x < 16; ++x){
                    int startWordIdx = startBit / 16;
                    int word1 = DataUtil.getLEUShort(fileData, blockDataStart + startWordIdx * 2);
                    int word2 = DataUtil.getLEUShort(fileData, blockDataStart + startWordIdx * 2 + 2);
                    // if startBit is 0, word == word1
                    // if startBit is 1, word is 15 bits of word1 and 1 bit of word2
                    int word = ((word1 << 16 | word2) >> (16 - (startBit & 0x0f))) & 0xFFFF;

                    int byte1 = (word >> 8) & 0xff;
                    HuffVal hv = huffVals[byte1];
                    int pixCmd;
                    if (hv.numBits != 0){
                        pixCmd = hv.val;
                        startBit += hv.numBits;
                    } else {
                        // Must be more than an 8 bit code
                        int bit=9;
                        int a = word >> (16-bit);
                        int v = DataUtil.getLEInt(fileData, table3Start + bit*4);
                        while (v < a){
                            ++bit;
                            if (bit > 16){
                                throw new Exception("A decoding error occured");
                            }
                            a = word >> (16-bit);
                            v = DataUtil.getLEInt(fileData, table3Start + bit*4);
                        }
                        startBit += bit;
                        int val = DataUtil.getLEInt(fileData, table2Start + bit*4);
                        int table1Index = a + val;

                        pixCmd = DataUtil.getLEShort(fileData, table1Start + table1Index*2);
                    }
                    int pix8 = 0;
                    if (pixCmd < 0x100){
                        pix8 = pixCmd;
                    } else if (pixCmd < 0x105){
                        int backjump = backJumpTable[pixCmd - 0x100];
                        if ((curpix8 + backjump) >= 0){
                            pix8 = pix8s[curpix8 + backjump];
                        } else {
                            throw new Exception("Something went wrong");
                        }
                    } else {
                        int table0Index = (pixCmd - 0x105) + prevPixel * 8;
                        pix8 = fileData[table0Start + table0Index] & 0xFF;
                    }

                    pix8s[curpix8++] = pix8;

                    prevPixel = pix8 & 0xFF;
                    PalEntry pixel = palette[pix8  & 0xFF];
                    var pBackBuffer = image.BackBuffer;
                    int xpos = xblock * 16 + x;
                    int ypos = yblock * 16 + y;
                    var p = pBackBuffer + ypos * image.BackBufferStride + xpos * 4;
                    unsafe
                    {
                        *((int*)p) = pixel.argb();
                    }
                }
            }
        }

        class HuffVal
        {
            public short val;
            public short numBits;
        }

        private HuffVal[] decodeHuff(int tableOffset)
        {
            HuffVal[] huffOut = new HuffVal[256];

            int table1Len = DataUtil.getLEInt(fileData, tableOffset) * 2;
            int table1Start = tableOffset + 4;
            int table2Start = table1Start + table1Len;
            int table3Start = table2Start + 0x48;

            for (int i=0; i<256; ++i){
                int bit=1;
                int a = i >> (8-bit);
                int v = DataUtil.getLEInt(fileData, table3Start + bit*4);
                while (v < a){
                    ++bit;
                    if (bit > 8){
                        break;
                    }
                    a = i >> (8-bit);
                    v = DataUtil.getLEInt(fileData, table3Start + bit*4);
                }
                huffOut[i] = new HuffVal();
                if (bit <= 8){
                    int val = DataUtil.getLEInt(fileData, table2Start + bit*4);
                    int table1Index = a + val;
                    huffOut[i].val = DataUtil.getLEShort(fileData, table1Start + table1Index * 2 );
                    huffOut[i].numBits = (short)bit;
                }
            }

            return huffOut;
        }
    }
}

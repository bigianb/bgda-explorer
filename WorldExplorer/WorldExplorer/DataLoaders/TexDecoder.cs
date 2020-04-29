/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WorldExplorer.DataLoaders
{
    public class TexDecoder
    {
        private const int BITBLTBUF = 0x50;
        private const int TRXPOS = 0x51;
        private const int TRXREG = 0x52;

        private const int PSMCT32 = 0x00;
        private const int PSMT4 = 0x14;

        public static WriteableBitmap Decode(byte[] data, int startOffset)
        {
            GSMemory gsMem = new GSMemory();

            int length = DataUtil.getLEShort(data, startOffset + 6) * 16;

            int finalw = BitConverter.ToInt16(data, startOffset);
            int finalh = BitConverter.ToInt16(data, startOffset + 2);
            int offsetToGIF = DataUtil.getLEInt(data, startOffset + 16);

            int sourcew = finalw;
            int sourceh = finalh;
            PalEntry[] pixels = null;
            byte[] bytes = null;

            int curIdx = offsetToGIF + startOffset;
            int endIndex = curIdx + length;

            GIFTag gifTag = new GIFTag();
            gifTag.parse(data, curIdx);

            // This is basically heuristics. Writing a full GIF parser is complex and as the texture files are written by a tool,
            // we can safely make some assumptions about their structure.
            if (gifTag.nloop == 4) {

                int palw = DataUtil.getLEShort(data, curIdx + 0x30);
                int palh = DataUtil.getLEShort(data, curIdx + 0x34);

                curIdx += gifTag.Length;
                GIFTag gifTag2 = new GIFTag();
                gifTag2.parse(data, curIdx);

                // 8 bit palletised
                PalEntry[] palette = PalEntry.readPalette(data, curIdx + 0x10, palw, palh);

                palette = PalEntry.unswizzlePalette(palette);

                curIdx += gifTag2.Length;
                int destWBytes = (finalw + 0x0f) & ~0x0f;
                int destHBytes = (finalh + 0x0f) & ~0x0f;

                int dpsm = PSMCT32;
                int dbw = 0;
                int dbp = 0;
                int rrw = 0;
                int rrh = 0;
                int startx = 0;
                int starty = 0;

                while (curIdx < endIndex - 0x10) {
                    GIFTag gifTag3 = new GIFTag();
                    gifTag3.parse(data, curIdx);
                    while (!gifTag3.isImage())
                    {
                        int trxregOffset = findADEntry(data, curIdx + 0x10, gifTag3.nloop, TRXREG);
                        if (trxregOffset != 0)
                        {
                            rrw = DataUtil.getLEShort(data, trxregOffset);
                            rrh = DataUtil.getLEShort(data, trxregOffset + 4);
                        }
                        int trxposOffset = findADEntry(data, curIdx + 0x10, gifTag3.nloop, TRXPOS);
                        if (trxposOffset != 0)
                        {
                            startx = DataUtil.getLEShort(data, trxposOffset + 0x04) & 0x07FF;
                            starty = DataUtil.getLEShort(data, trxposOffset + 0x06) & 0x07FF;
                        }
                        int bitbltOffset = findADEntry(data, curIdx + 0x10, gifTag3.nloop, BITBLTBUF);
                        if (bitbltOffset != 0)
                        {
                            //int sbw = fileData[bitbltOffset + 0x02] & 0x3F;
                            dbp = data[bitbltOffset + 0x04] & 0x3FFF;
                            dbw = data[bitbltOffset + 0x06] & 0x3F;
                            dpsm = data[bitbltOffset + 0x07] & 0x3F;
                        }

                        curIdx += gifTag3.Length;
                        gifTag3.parse(data, curIdx);
                    }
                    curIdx += 0x10;     // image gif tag
                    int bytesToTransfer = gifTag3.nloop * 16;

                    if (palette.Length == 16)
                    {
                        // source is PSMT4. Dest can be PSMT4 or PSMCT32
                        if (dpsm == PSMCT32)
                        {
                            byte[] imageData = data;
                            int imageDataIdx = curIdx;
                            // check for multiple IMAGE entries.
                            int nextTagInd = bytesToTransfer + curIdx;
                            if (nextTagInd < endIndex - 0x10)
                            {
                                GIFTag imageTag2 = new GIFTag();
                                imageTag2.parse(data, nextTagInd);
                                if (imageTag2.flg == 2)
                                {
                                    // IMAGE
                                    int bytesToTransfer2 = imageTag2.nloop * 16;
                                    imageDataIdx = 0;
                                    imageData = new byte[bytesToTransfer + bytesToTransfer2];
                                    int j = curIdx;
                                    for (int i = 0; i < bytesToTransfer; ++i)
                                    {
                                        imageData[i] = data[j];
                                    }
                                    j = nextTagInd + 0x10;
                                    for (int i = bytesToTransfer; i < bytesToTransfer + bytesToTransfer2; ++i)
                                    {
                                        imageData[i] = data[j];
                                    }
                                    bytesToTransfer += imageTag2.Length;
                                }
                            }

                            gsMem.writeTexPSMCT32(dbp, dbw, startx, starty, rrw, rrh, imageData, imageDataIdx);

                            destWBytes = (finalw + 0x3f) & ~0x3f;
                            bytes = gsMem.readTexPSMT4(dbp, destWBytes / 0x40, startx, starty, destWBytes, destHBytes);
                            bytes = expand4bit(bytes);

                        }
                        else
                        {
                            // dest and source are the same and so image isn't swizzled
                            bytes = transferPSMT4(bytes, data, curIdx, startx, starty, rrw, rrh, destWBytes, destHBytes);
                        }
                    }
                    else
                    {
                        // source is PSMT8. Dest is always PSMCT32.
                        gsMem.writeTexPSMCT32(dbp, dbw, startx, starty, rrw, rrh, data, curIdx);
                    }
                    curIdx += bytesToTransfer;
                }
                if (palette.Length == 256)
                {
                    destWBytes = (finalw + 0x3f) & ~0x3f;
                    dbw = destWBytes / 0x40;
                    bytes = gsMem.readTexPSMT8(dbp, dbw, 0, 0, destWBytes, finalh);
                }
                pixels = applyPalette(palette, bytes);
                sourcew = destWBytes;
                sourceh = destHBytes;
            }
            else if (gifTag.nloop == 3) {
                GIFTag gifTag2 = new GIFTag();
                gifTag2.parse(data, startOffset + 0xC0);

                if (gifTag2.flg == 2) {
                    // image mode
                    pixels = readPixels32(data, startOffset + 0xD0, finalw, finalh);
                }
            }
            WriteableBitmap image = null;
            if (finalw != 0) {
                image = new WriteableBitmap(
                    finalw, finalh,
                    96, 96,
                    PixelFormats.Bgr32,
                    null);
                image.Lock();
                if (pixels != null) { 
                unsafe
                    {
                        IntPtr pBackBuffer = image.BackBuffer;
                        for (int y = 0; y < sourceh && y < finalh; ++y)
                        {
                            for (int x = 0; x < sourcew && x < finalw; ++x)
                            {
                                PalEntry pixel = pixels[y * sourcew + x];
                                if (pixel != null)
                                {
                                    if (x < finalw && y < finalh)
                                    {
                                        var p = pBackBuffer + y * image.BackBufferStride + x * 4;
                                        *((int*)p) = pixel.argb();
                                    }
                                }
                            }
                        }
                    }
                }
                // Specify the area of the bitmap that changed.
                image.AddDirtyRect(new Int32Rect(0, 0, finalw, finalh));

                // Release the back buffer and make it available for display.
                image.Unlock();
            }
            return image;
        }


    // Take an image where the pixels are packed and expand them to one byte per pixel.
    private static byte[] expand4bit(byte[] bytes)
    {
        byte[] outbytes = new byte[bytes.Length * 2];
        int j = 0;
        foreach (int val in bytes)
        {
            outbytes[j++] = (byte) (val & 0x0f);
            outbytes[j++] = (byte) ((val >> 4) & 0x0f);
        }
        return outbytes;
    }

    private static PalEntry[] applyPalette(PalEntry[] palette, byte[] bytes)
    {
        PalEntry[] pixels = new PalEntry[bytes.Length];
        for (int i = 0; i < bytes.Length; ++i)
        {
            pixels[i] = palette[bytes[i] & 0xFF];
        }
        return pixels;
    }

    private static int findADEntry(byte[] fileData, int dataStartIdx, int nloop, int registerId)
        {
            int retval = 0;
            for (int i = 0; i < nloop; ++i)
            {
                int reg = DataUtil.getLEInt(fileData, dataStartIdx + i * 0x10 + 0x08);
                if (reg == registerId)
                {
                    retval = dataStartIdx + i * 0x10;
                    break;
                }
            }
            return retval;
        }

        private static byte[] transferPSMT4(byte[] pixels, byte[] fileData, int startOffset, int startx, int starty,
                                 int rrw, int rrh, int destWBytes, int destHBytes)
        {
            if (pixels == null)
            {
                pixels = new byte[destWBytes * destHBytes];
            }

            int nybble = 2;
            byte[] nybbles = new byte[2];
            int idx = startOffset;
            for (int y = 0; y < rrh && (y + starty) < destHBytes; ++y)
            {
                for (int x = 0; x < rrw; ++x)
                {
                    if (nybble > 1)
                    {
                        byte twoPix = fileData[idx++];
                        nybbles[0] = (byte)((twoPix) & 0x0f);
                        nybbles[1] = (byte)((twoPix >> 4) & 0x0f);
                        nybble = 0;
                    }
                    int destIdx = (y + starty) * destWBytes + (x + startx);
                    pixels[destIdx] = nybbles[nybble];
                    ++nybble;
                }
            }
            return pixels;
        }

        private static PalEntry[] readPixels32(byte[] fileData, int startOffset, int w, int h)
        {
            int numPixels = w * h;
            PalEntry[] pixels = new PalEntry[numPixels];
            int destIdx = 0;
            int endOffset = startOffset + numPixels * 4;
            for (int idx = startOffset; idx < endOffset; ) {
                PalEntry pe = new PalEntry();
                pe.r = fileData[idx++];
                pe.g = fileData[idx++];
                pe.b = fileData[idx++];
                pe.a = fileData[idx++];

                pixels[destIdx++] = pe;
            }

            return pixels;
        }

    }
}

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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorldExplorer.DataLoaders
{
    public class TexDecoder
    {
        private const int BITBLTBUF = 0x50;
        private const int TRXPOS = 0x51;
        private const int TRXREG = 0x52;

        private const int PSMCT32 = 0x00;
        private const int PSMT4 = 0x14;

        public static WriteableBitmap Decode(ReadOnlySpan<byte> data)
        {
            var gsMem = new GSMemory();

            var length = DataUtil.getLEShort(data, 6) * 16;

            int finalw = BitConverter.ToInt16(data[..2].ToArray());
            int finalh = BitConverter.ToInt16(data.Slice(2, 2).ToArray());
            var offsetToGIF = DataUtil.getLEInt(data, 16);

            var sourcew = finalw;
            var sourceh = finalh;
            PalEntry[] pixels = null;
            byte[] bytes = null;

            var curIdx = offsetToGIF;
            var endIndex = curIdx + length;

            var gifTag = new GIFTag();
            gifTag.parse(data, curIdx);

            // This is basically heuristics. Writing a full GIF parser is complex and as the texture files are written by a tool,
            // we can safely make some assumptions about their structure.
            if (gifTag.nloop == 4)
            {

                int palw = DataUtil.getLEShort(data, curIdx + 0x30);
                int palh = DataUtil.getLEShort(data, curIdx + 0x34);

                curIdx += gifTag.Length;
                var gifTag2 = new GIFTag();
                gifTag2.parse(data, curIdx);

                // 8 bit palletised
                var palette = PalEntry.readPalette(data.Slice(curIdx + GIFTag.Size), palw, palh);

                palette = PalEntry.unswizzlePalette(palette);

                curIdx += gifTag2.Length;
                var destWBytes = (finalw + 0x0f) & ~0x0f;
                var destHBytes = (finalh + 0x0f) & ~0x0f;

                var dpsm = PSMCT32;
                var dbw = 0;
                var dbp = 0;
                var rrw = 0;
                var rrh = 0;
                var startx = 0;
                var starty = 0;

                while (curIdx < endIndex - GIFTag.Size)
                {
                    var gifTag3 = new GIFTag();
                    gifTag3.parse(data, curIdx);
                    while (!gifTag3.IsImage)
                    {
                        var trxregOffset = findADEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, TRXREG);
                        if (trxregOffset != 0)
                        {
                            rrw = DataUtil.getLEShort(data, trxregOffset);
                            rrh = DataUtil.getLEShort(data, trxregOffset + 4);
                        }
                        var trxposOffset = findADEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, TRXPOS);
                        if (trxposOffset != 0)
                        {
                            startx = DataUtil.getLEShort(data, trxposOffset + 0x04) & 0x07FF;
                            starty = DataUtil.getLEShort(data, trxposOffset + 0x06) & 0x07FF;
                        }
                        var bitbltOffset = findADEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, BITBLTBUF);
                        if (bitbltOffset != 0)
                        {
                            //int sbw = fileData[bitbltOffset + 0x02] & 0x3F;
                            dbp = data[bitbltOffset + 0x04] & 0x3FFF;
                            dbw = data[bitbltOffset + 0x06] & 0x3F;
                            dpsm = data[bitbltOffset + 0x07] & 0x3F;
                        }

                        curIdx += gifTag3.Length;
                        if (curIdx + GIFTag.Size >= endIndex)
                        {
                            break;
                        }

                        gifTag3.parse(data, curIdx);
                    }
                    curIdx += GIFTag.Size;     // image gif tag
                    var bytesToTransfer = gifTag3.nloop * 16;

                    if (palette.Length == 16)
                    {
                        // source is PSMT4. Dest can be PSMT4 or PSMCT32
                        if (dpsm == PSMCT32)
                        {
                            var imageData = data.ToArray();
                            var imageDataIdx = curIdx;
                            // check for multiple IMAGE entries.
                            var nextTagInd = bytesToTransfer + curIdx;
                            if (nextTagInd < endIndex - GIFTag.Size)
                            {
                                var imageTag2 = new GIFTag();
                                imageTag2.parse(data, nextTagInd);
                                if (imageTag2.flg == 2)
                                {
                                    // IMAGE
                                    var bytesToTransfer2 = imageTag2.nloop * 16;
                                    imageDataIdx = 0;
                                    imageData = new byte[bytesToTransfer + bytesToTransfer2];
                                    var j = curIdx;
                                    for (var i = 0; i < bytesToTransfer; ++i)
                                    {
                                        imageData[i] = data[j];
                                    }
                                    j = nextTagInd + GIFTag.Size;
                                    for (var i = bytesToTransfer; i < bytesToTransfer + bytesToTransfer2; ++i)
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
                // THIS IS A HACK
                if (palette.Length == 1024)
                {
                    destWBytes = (finalw + 0x3f) & ~0x3f;
                    dbw = destWBytes / 0x40;
                    bytes = gsMem.readTexPSMT8(dbp, dbw, 0, 0, destWBytes, finalh);
                }
                pixels = applyPalette(palette, bytes);
                sourcew = destWBytes;
                sourceh = destHBytes;
            }
            else if (gifTag.nloop == 3)
            {
                var gifTag2 = new GIFTag();
                gifTag2.parse(data, 0xC0);

                if (gifTag2.flg == 2)
                {
                    // image mode
                    pixels = readPixels32(data.Slice(0xD0), finalw, finalh);
                }
            }
            WriteableBitmap image = null;
            if (finalw != 0)
            {
                image = new WriteableBitmap(
                    finalw, finalh,
                    96, 96,
                    PixelFormats.Bgr32,
                    null);
                image.Lock();
                if (pixels != null)
                {
                    unsafe
                    {
                        var pBackBuffer = image.BackBuffer;
                        for (var y = 0; y < sourceh && y < finalh; ++y)
                        {
                            for (var x = 0; x < sourcew && x < finalw; ++x)
                            {
                                var pixel = pixels[y * sourcew + x];
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
            var outbytes = new byte[bytes.Length * 2];
            var j = 0;
            foreach (int val in bytes)
            {
                outbytes[j++] = (byte)(val & 0x0f);
                outbytes[j++] = (byte)((val >> 4) & 0x0f);
            }
            return outbytes;
        }

        private static PalEntry[] applyPalette(PalEntry[] palette, byte[] bytes)
        {
            var pixels = new PalEntry[bytes.Length];
            for (var i = 0; i < bytes.Length; ++i)
            {
                pixels[i] = palette[bytes[i] & 0xFF];
            }
            return pixels;
        }

        private static int findADEntry(ReadOnlySpan<byte> fileData, int dataStartIdx, int nloop, int registerId)
        {
            var retval = 0;
            for (var i = 0; i < nloop; ++i)
            {
                var reg = DataUtil.getLEInt(fileData, dataStartIdx + i * 0x10 + 0x08);
                if (reg == registerId)
                {
                    retval = dataStartIdx + i * 0x10;
                    break;
                }
            }
            return retval;
        }

        private static byte[] transferPSMT4(byte[] pixels, ReadOnlySpan<byte> fileData, int startOffset, int startx, int starty,
                                 int rrw, int rrh, int destWBytes, int destHBytes)
        {
            if (pixels == null)
            {
                pixels = new byte[destWBytes * destHBytes];
            }

            var nybble = 2;
            var nybbles = new byte[2];
            var idx = startOffset;
            for (var y = 0; y < rrh && (y + starty) < destHBytes; ++y)
            {
                for (var x = 0; x < rrw; ++x)
                {
                    if (nybble > 1)
                    {
                        var twoPix = fileData[idx++];
                        nybbles[0] = (byte)((twoPix) & 0x0f);
                        nybbles[1] = (byte)((twoPix >> 4) & 0x0f);
                        nybble = 0;
                    }
                    var destIdx = (y + starty) * destWBytes + (x + startx);
                    pixels[destIdx] = nybbles[nybble];
                    ++nybble;
                }
            }
            return pixels;
        }
        
        private static PalEntry[] readPixels32(ReadOnlySpan<byte> fileData, int w, int h)
        {
            var numPixels = w * h;
            var pixels = new PalEntry[numPixels];
            var destIdx = 0;
            var endOffset = numPixels * 4;
            for (var idx = 0; idx < endOffset;)
            {
                var pe = new PalEntry
                {
                    r = fileData[idx++],
                    g = fileData[idx++],
                    b = fileData[idx++],
                    a = fileData[idx++]
                };

                pixels[destIdx++] = pe;
            }

            return pixels;
        }

        private static PalEntry[] readPixels32(byte[] fileData, int startOffset, int w, int h)
        {
            var numPixels = w * h;
            var pixels = new PalEntry[numPixels];
            var destIdx = 0;
            var endOffset = startOffset + numPixels * 4;
            for (var idx = startOffset; idx < endOffset;)
            {
                var pe = new PalEntry
                {
                    r = fileData[idx++],
                    g = fileData[idx++],
                    b = fileData[idx++],
                    a = fileData[idx++]
                };

                pixels[destIdx++] = pe;
            }

            return pixels;
        }

    }
}

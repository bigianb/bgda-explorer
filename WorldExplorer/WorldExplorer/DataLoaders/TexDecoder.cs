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
        public static WriteableBitmap Decode(byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            int finalw = BitConverter.ToInt16(data, startOffset);
            int finalh = BitConverter.ToInt16(data, startOffset + 2);

            int sourcew = finalw;
            int sourceh = finalh;
            PalEntry[] pixels = null;

            int curIdx = 0x80 + startOffset;
            GIFTag gifTag = new GIFTag();
            gifTag.parse(data, curIdx);

            // This is basically heuristics. Writing a full GIF parser is complex and as the texture files are written by a tool,
            // we can safely make some assumptions about their structure.
            if (gifTag.nloop == 4) {

                int palw = DataUtil.getLEShort(data, curIdx + 0x30);
                int palh = DataUtil.getLEShort(data, curIdx + 0x34);

                curIdx += 0x50;
                GIFTag gifTag2 = new GIFTag();
                gifTag2.parse(data, curIdx);

                // 8 bit palletised
                PalEntry[] palette = PalEntry.readPalette(data, curIdx + 0x10, palw, palh);

                palette = PalEntry.unswizzlePalette(palette);

                int palLen = palw * palh * 4;
                curIdx += (palLen + 0x10);

                GIFTag gifTag50 = new GIFTag();
                gifTag50.parse(data, curIdx);
                curIdx += 0x20;

                int dbw = (sourcew / 2 + 0x07) & ~0x07;
                int dbh = (sourceh / 2 + 0x07) & ~0x07;

                // The following should be a loop, there are repeating sections
                while (curIdx < endIndex - 0x10) {
                    GIFTag gifTag3 = new GIFTag();
                    gifTag3.parse(data, curIdx);

                    int dimOffset = 0x10;

                    int thisRrw = DataUtil.getLEShort(data, curIdx + dimOffset);
                    int thisRrh = DataUtil.getLEShort(data, curIdx + dimOffset + 4);

                    int startx = DataUtil.getLEShort(data, curIdx + dimOffset + 20);
                    int starty = DataUtil.getLEShort(data, curIdx + dimOffset + 22);

                    curIdx += gifTag.nloop * 0x10 + 0x10;
                    pixels = readPixels32(pixels, data, palette, curIdx, startx, starty, thisRrw, thisRrh, dbw, dbh);
                    curIdx += thisRrw * thisRrh * 4;
                }
                if (palLen != 64) {
                    pixels = unswizzle8bpp(pixels, dbw * 2, dbh * 2);
                    sourcew = dbw * 2;
                    sourceh = dbh * 2;
                } else {
                    sourcew = dbw;
                    sourceh = dbh;
                }
                

            } else if (gifTag.nloop == 3) {
                GIFTag gifTag2 = new GIFTag();
                gifTag2.parse(data, startOffset + 0xC0);

                if (gifTag2.flg == 2) {
                    // image mode
                    pixels = readPixels32(data, startOffset + 0xD0, finalw, finalh);
                }
            }
            WriteableBitmap image = null;
            if (finalw != 0 && pixels != null) {
                image = new WriteableBitmap(
                    finalw, finalh,
                    96, 96,
                    PixelFormats.Bgr32,
                    null);
                image.Lock();
                unsafe {
                    IntPtr pBackBuffer = image.BackBuffer;
                    for (int y = 0; y < sourceh; ++y) {
                        for (int x = 0; x < sourcew; ++x) {
                            PalEntry pixel = pixels[y * sourcew + x];
                            if (pixel != null) {
                                if (x < finalw && y < finalh) {
                                    var p = pBackBuffer + y * image.BackBufferStride + x * 4;
                                    *((int*)p) = pixel.argb();
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

        private static PalEntry[] unswizzle8bpp(PalEntry[] pixels, int w, int h)
        {
            PalEntry[] unswizzled = new PalEntry[pixels.Length];

            for (int y = 0; y < h; ++y) {
                for (int x = 0; x < w; ++x) {

                    int block_location = (y & (~0xf)) * w + (x & (~0xf)) * 2;
                    int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                    int column_location = posY * w * 2 + ((x + swap_selector) & 0x7) * 4;

                    int byte_num = ((y >> 1) & 1) + ((x >> 2) & 2);     // 0,1,2,3

                    int idx = block_location + column_location + byte_num;
                    if (idx < pixels.Length) {
                        unswizzled[(y * w) + x] = pixels[idx];
                    }
                }
            }

            return unswizzled;
        }


        private static PalEntry[] readPixels32(PalEntry[] pixels, byte[] fileData, PalEntry[] palette, int startOffset, int startx, int starty, int rrw, int rrh, int dbw, int dbh)
        {
            if (palette.Length == 256) {
                int numDestBytes = dbh * dbw * 4;
                int widthBytes = dbw * 4;
                if (pixels == null) {
                    pixels = new PalEntry[numDestBytes];
                }
                int idx = startOffset;
                for (int y = 0; y < rrh && (y+starty) < dbh; ++y) {
                    for (int x = 0; x < rrw; ++x) {
                        int destIdx = (y+starty) * widthBytes + (x + startx) * 4;
                        pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                        pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                        pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                        pixels[destIdx] = palette[fileData[idx++] & 0xFF];
                    }
                }
                return pixels;
            } else {
                int numDestBytes = rrh * dbw;
                if (numDestBytes <= 0)
                {
                    return pixels;
                }
                if (pixels == null) {
                    pixels = new PalEntry[numDestBytes];
                }
                int idx = startOffset;
                bool lowbit = false;
                for (int y = 0; y < rrh; ++y) {
                    for (int x = 0; x < rrw; ++x) {
                        int destIdx = (y + starty) * dbw + x + startx;
                        if (!lowbit) {
                            pixels[destIdx] = palette[fileData[idx] >> 4 & 0x0F];
                        } else {
                            pixels[destIdx] = palette[fileData[idx++] & 0x0F];
                        }
                        lowbit = !lowbit;
                    }
                }
                return pixels;
            }
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

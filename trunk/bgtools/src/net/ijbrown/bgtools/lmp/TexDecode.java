/*  Copyright (C) 2011 Ian Brown

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
package net.ijbrown.bgtools.lmp;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;

/**
 * Decodes a Texture.
 */
public class TexDecode
{
    public static void main(String[] args) throws IOException
    {
        String filename = "/emu/bgda/BG/DATA_extracted/lever.tex";

        String outDir = "/emu/bgda/BG/DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        TexDecode obj = new TexDecode();
        obj.extract(filename, outDirFile);
    }

    // Texture format is something like as follows:
    // 16 byte header.
    //    short width
    //    short height

    // Then starting at address 0x80
    // GS Packet

    // Currently it is assumed that all the image data is in one GIFTag segment.
    // This is not the case for some textures (e.g. chest_large), so the code will
    // need extending to read the GIFTags properly.

    private void extract(String filename, File outDirFile) throws IOException
    {
        File file = new File(filename);
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        int fileLength = (int) file.length();
        byte[] fileData = new byte[fileLength];

        int offset = 0;
        int remaining = fileLength;
        while (remaining > 0) {
            int read = is.read(fileData, offset, remaining);
            if (read == -1) {
                throw new IOException("Read less bytes then expected when reading file");
            }
            remaining -= read;
            offset += read;
        }

        int startIdx = 0x80;
        GIFTag gifTag = new GIFTag();
        gifTag.parse(fileData, startIdx);

        PalEntry[] palette = readPalette(fileData, 0xE0);

        palette = unswizzlePalette(palette);

        int finalw = DataUtil.getLEShort(fileData, 0);
        int finalh = DataUtil.getLEShort(fileData, 2);

        int rrw = DataUtil.getLEShort(fileData, 0x510);
        int rrh = DataUtil.getLEShort(fileData, 0x514);

        byte dbw = fileData[0x4f6];

        PalEntry[] pixels = readPixels32(fileData, palette, 0x550, rrw, rrh, rrw);

        pixels = unswizzle8bpp(pixels, rrw*2, rrh*2);

        int sourcew = rrw*2;

        BufferedImage image = new BufferedImage(finalw, finalh, BufferedImage.TYPE_INT_ARGB);
        for (int y = 0; y < finalh; ++y) {
            for (int x = 0; x < finalw; ++x) {
                PalEntry pixel = pixels[y * sourcew + x];
                if (pixel != null) {
                    image.setRGB(x, y, pixel.argb());
                }
            }
        }
        File outputfile = new File(filename + ".png");
        ImageIO.write(image, "png", outputfile);

    }

    private PalEntry[] unswizzlePalette(PalEntry[] palette)
    {
        PalEntry[] unswizzled = new PalEntry[256];

        int j = 0;
        for (int i = 0; i < 256; i += 32, j += 32) {
            copy(unswizzled, i, palette, j, 8);
            copy(unswizzled, i + 16, palette, j + 8, 8);
            copy(unswizzled, i + 8, palette, j + 16, 8);
            copy(unswizzled, i + 24, palette, j + 24, 8);
        }
        return unswizzled;
    }

    private void copy(PalEntry[] unswizzled, int i, PalEntry[] swizzled, int j, int num)
    {
        for (int x = 0; x < num; ++x) {
            unswizzled[i + x] = swizzled[j + x];
        }
    }

    private PalEntry[] unswizzle8bpp(PalEntry[] pixels, int w, int h)
    {
        PalEntry[] unswizzled = new PalEntry[pixels.length];

        for (int y = 0; y < h; ++y) {
            for (int x = 0; x < w; ++x) {

                int block_location = (y & (~0xf)) * w + (x & (~0xf)) * 2;
                int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                int column_location = posY * w * 2 + ((x + swap_selector) & 0x7) * 4;

                int byte_num = ((y >> 1) & 1) + ((x >> 2) & 2);     // 0,1,2,3

                int idx= block_location + column_location + byte_num;
                if (idx >= pixels.length){
                    System.out.println("x");
                } else {
                    unswizzled[(y * w) + x] = pixels[idx];
                }
            }
        }

        return unswizzled;
    }


    private PalEntry[] readPixels32(byte[] fileData, PalEntry[] palette, int startOffset, int rrw, int rrh, int dbw)
    {
        int numDestBytes = rrh * dbw * 4;
        int widthBytes = dbw*4;
        PalEntry[] pixels = new PalEntry[numDestBytes];
        int idx = startOffset;
        for (int y=0; y<rrh; ++y){
            for (int x=0; x<rrw; ++x){
                int destIdx = y*widthBytes + x*4;
                pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                pixels[destIdx] = palette[fileData[idx++] & 0xFF];
            }
        }
        return pixels;
    }

    private PalEntry[] readPalette(byte[] fileData, int startOffset)
    {
        PalEntry[] palette = new PalEntry[256];
        for (int i = 0; i < 256; ++i) {
            PalEntry pe = new PalEntry();
            pe.r = fileData[startOffset + i * 4];
            pe.g = fileData[startOffset + i * 4 + 1];
            pe.b = fileData[startOffset + i * 4 + 2];
            pe.a = fileData[startOffset + i * 4 + 3];

            palette[i] = pe;
        }
        return palette;
    }

    class PalEntry
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public int argb()
        {
            // in ps2 0x80 is fully transparent and 0 is opaque.
            // in java 0 is transparent and 0xFF is opaque.

            byte java_a = a == 0 ? 0 : (byte) ((a << 1) - 1);

            java_a = (byte) 0xFF;

            int argb = (java_a << 24) |
                    ((r << 16) & 0xFF0000) |
                    ((g << 8) & 0xFF00) |
                    (b & 0xFF);
            return argb;
        }
    }
}

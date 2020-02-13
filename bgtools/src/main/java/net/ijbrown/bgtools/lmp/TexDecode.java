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
        GameType gameType = GameType.JUSTICE_LEAGUE_HEROES;

        Config config = new Config(gameType);
        String inDir = config.getDataDir();
        String outDir = inDir+"../DATA_extracted/";

        TexDecode obj = new TexDecode();
        obj.extract("ammobox.tex", new File(outDir + "AMMOBOX_LMP"));

    }

    // Texture format is something like as follows:
    // 16 byte header.
    //    short width
    //    short height

    // Then starting offset stored at address 0x10
    // GS Packet

    // Currently it is assumed that all the image data is in one GIFTag segment.
    // This is not the case for some textures (e.g. chest_large), so the code will
    // need extending to read the GIFTags properly.

    public void extract(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
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

        extract(outDirFile, fileData, 0, filename, fileLength);
    }

    public void extract(File outDirFile, byte[] fileData, int startOffset, String filename, int length) throws IOException
    {
        int endIndex = startOffset + length;
        int finalw = DataUtil.getLEShort(fileData, startOffset);
        int finalh = DataUtil.getLEShort(fileData, startOffset + 2);
        int sourcew = finalw;
        int sourceh = finalh;
        PalEntry[] pixels = null;

        int offsetToGIF = DataUtil.getLEInt(fileData, startOffset + 16);

        int curIdx = offsetToGIF + startOffset;
        GIFTag gifTag = new GIFTag();
        gifTag.parse(fileData, curIdx);

        // This is basically heuristics
        if (gifTag.nloop == 4) {

            int palw = DataUtil.getLEShort(fileData, curIdx + 0x30);
            int palh = DataUtil.getLEShort(fileData, curIdx + 0x34);

            curIdx += gifTag.getLength();
            GIFTag gifTag2 = new GIFTag();
            gifTag2.parse(fileData, curIdx);

            // 8 bit palletised
            PalEntry[] palette = PalEntry.readPalette(fileData, curIdx + 0x10, palw, palh);

            palette = PalEntry.unswizzlePalette(palette);

            int palLen = palw * palh * 4;
            curIdx += gifTag2.getLength();

            GIFTag gifTag50 = new GIFTag();
            gifTag50.parse(fileData, curIdx);
            curIdx += gifTag50.getLength();

            int dbw = (sourcew / 2 + 0x07) & ~0x07;
            int dbh = (sourceh / 2 + 0x07) & ~0x07;

            while (curIdx < endIndex - 0x10) {
                GIFTag gifTag3 = new GIFTag();
                gifTag3.parse(fileData, curIdx);

                int trxregOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, 0x52);

                if (trxregOffset == 0) {
                    curIdx += gifTag3.getLength();
                    gifTag3.parse(fileData, curIdx);
                    trxregOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, 0x52);
                    if (trxregOffset == 0) {
                        throw new RuntimeException("Failed to find TRXREG register");
                    }
                }

                int rrw = DataUtil.getLEShort(fileData, trxregOffset);
                int rrh = DataUtil.getLEShort(fileData, trxregOffset + 4);

                int startx = DataUtil.getLEShort(fileData, trxregOffset + 0x20);
                int starty = DataUtil.getLEShort(fileData, trxregOffset + 0x22);

                curIdx += gifTag3.getLength();
                GIFTag imageTag = new GIFTag();
                imageTag.parse(fileData, curIdx);
                curIdx += 0x10;     // image gif tag
                pixels = readPixels32(pixels, fileData, palette, curIdx, startx, starty, rrw, rrh, dbw, dbh);
                curIdx += rrw * rrh * 4;
            }
            if (palLen != 64){
                pixels = unswizzle8bpp(pixels, dbw * 2, dbh * 2);
                sourcew = dbw * 2;
                sourceh = dbh * 2;
            } else {
                sourcew = dbw;
                sourceh = dbh;
            }

        } else if (gifTag.nloop == 3) {
            GIFTag gifTag2 = new GIFTag();
            gifTag2.parse(fileData, startOffset + 0xC0);
            System.out.println(gifTag2.toString());

            if (gifTag2.flg == 2) {
                // image mode
                pixels = readPixels32(fileData, startOffset + 0xD0, finalw, finalh);
            }
        }
        if (finalw != 0 && pixels != null) {
            BufferedImage image = new BufferedImage(finalw, finalh, BufferedImage.TYPE_INT_ARGB);
            for (int y = 0; y < sourceh && y < finalh; ++y) {
                for (int x = 0; x < sourcew && x < finalw; ++x) {
                    PalEntry pixel = pixels[y * sourcew + x];
                    if (pixel != null) {
                        image.setRGB(x, y, pixel.argb());
                    }
                }
            }
            File outputfile = new File(outDirFile, filename + ".png");
            ImageIO.write(image, "png", outputfile);
        }
    }

    private int findADEntry(byte[] fileData, int dataStartIdx, int nloop, int registerId)
    {
        int retval = 0;
        for (int i=0; i<nloop; ++i){
            int reg = DataUtil.getLEInt(fileData, dataStartIdx + i * 0x10 + 0x08);
            if (reg == registerId){
                retval = dataStartIdx + i*0x10;
                break;
            }
        }
        return retval;
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

                int idx = block_location + column_location + byte_num;
                if (idx >= pixels.length) {
                    System.out.println("x");
                } else {
                    unswizzled[(y * w) + x] = pixels[idx];
                }
            }
        }

        return unswizzled;
    }


    private PalEntry[] readPixels32(PalEntry[] pixels, byte[] fileData, PalEntry[] palette, int startOffset, int startx, int starty, int rrw, int rrh, int dbw, int dbh)
    {
        if (palette.length == 256){
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
            if (pixels == null) {
                pixels = new PalEntry[numDestBytes];
            }
            int idx = startOffset;
            boolean lowbit=false;
            for (int y = 0; y < rrh; ++y) {
                for (int x = 0; x < rrw; ++x) {
                    int destIdx = (y + starty) * dbw + x + startx;
                    if (lowbit){
                        pixels[destIdx] = palette[fileData[idx] >> 4 & 0x0F];
                        idx++;
                    } else {
                        pixels[destIdx] = palette[fileData[idx] & 0x0F];
                    }
                    lowbit = !lowbit;
                }
            }
            return pixels;
        }
    }

    private PalEntry[] readPixels32(byte[] fileData, int startOffset, int w, int h)
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

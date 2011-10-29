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
        String filename = "/emu/bgda/BG/DATA_extracted/player1.tex";

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
    //    int pad, pad, pad

    // Then starting at address 0x80
    // GS Packet

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
        int dbwPixels = dbw * 64;

        PalEntry[] pixels = readPixels32(fileData, palette, 0x550, rrw, rrh, rrw);

//        pixels = unswizzle8bpp(pixels, finalw, finalh);
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

    private PalEntry[] unswizzle32bpp(PalEntry[] pixels, int w, int h)
    {
        PalEntry[] unswizzled = new PalEntry[pixels.length];

        for (int y = 0; y < h; ++y) {
            for (int xx = 0; xx < w; ++xx) {

                int x = xx * 4;   // byte position

                int block_location = (y & (~0xf)) * w + (x & (~0xf)) * 2;
                int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                int column_location = posY * w * 2 + ((x + swap_selector) & 0x7) * 4;

                int byte_num = ((y >> 1) & 1) + ((x >> 2) & 2);     // 0,1,2,3

                unswizzled[(y * w) + xx] = pixels[(block_location + column_location + byte_num) / 4];
            }
        }
        return unswizzled;
    }


    int block32[] = new int[]
            {
                    0, 1, 4, 5, 16, 17, 20, 21,
                    2, 3, 6, 7, 18, 19, 22, 23,
                    8, 9, 12, 13, 24, 25, 28, 29,
                    10, 11, 14, 15, 26, 27, 30, 31
            };


    int columnWord32[] = new int[]
            {
                    0, 1, 4, 5, 8, 9, 12, 13,
                    2, 3, 6, 7, 10, 11, 14, 15
            };

    PalEntry[] readTexPSMCT32(int rrw, int rrh, PalEntry[] pixels)
    {
        PalEntry[] unswizzled = new PalEntry[pixels.length];

        int dbw = (rrw + 31)/32;

        int idx=0;
        for (int y = 0; y < rrh; y++) {
            for (int x = 0; x < rrw; x++) {
                // quarterpage is 16 pixels wide and 32 pixels high.
                int quarterpageX = x / 16;
                int pageY = y / 32;
                int quarterwidthpage = quarterpageX + pageY * dbw;

                int px = x - (x & ~63);
                int py = y - (pageY * 32);

                int blockX = px / 8;
                int blockY = py / 8;
                int block = block32[blockX + blockY * 8];

                int bx = px - blockX * 8;
                int by = py - blockY * 8;

                int column = by / 2;

                int cx = bx;
                int cy = by - column * 2;
                int cw = columnWord32[cx + cy * 8];

                unswizzled[idx] = pixels[quarterwidthpage * 512 + block * 64 + column * 16 + cw];
                idx++;
            }
        }
        return unswizzled;
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

    /*

    Internally, the GE processes textures as 16 bytes by 8 rows blocks (independent of actual pixelformat,
    so a 32×32 32-bit texture is a 128×32 texture from the swizzlings point of view).
    When you are not swizzling, this means it will have to do scattered reads from the texture as it moves
    the block into its texture-cache, which has a big impact on performance.
    To improve on this, you can re-order your textures into these blocks so that it can
    fetch one entire block by reading sequentially.


    The swizzle function is fairly simple. If you look at the offset into texture like this (bit 0 on the right):

    31                           v lg2(width)        0
    by...by by by by by  my my my bx...bx  mx mx mx mx

    bx,by are block coords of the 16×8 block within the texture.
    bx has log2(width)-4 bits, and by has 31-log2(width)-3 bits (ie, all the MSBs).
    mx,my are the coords within the block.

    The swizzle function rotates the my-bx group left by 3 bits, giving:

    by...by by by by by  bx...bx my my my  mx mx mx mx
    leaving by and mx unchanged in the offset.

    Unswizzling is identical, except you rotate the my-bx group right by 3 bits.


    unsigned swizzle(unsigned offset, unsigned log2_w)
    {
        if (log2_w <= 4)
            return offset;

        unsigned w_mask = (1 << log2_w) - 1;

        unsigned mx = offset & 0xf;
        unsigned by = offset & (~7 << log2_w);
        unsigned bx = offset & w_mask & ~0xf;
        unsigned my = offset & (7 << log2_w);

        return by | (bx << 3) | (my >> (log2_w - 4)) | mx;
    }

    unsigned unswizzle32bpp(unsigned offset, unsigned log2_w)
    {
        if (log2_w <= 4)
            return offset;

        unsigned w_mask = (1 << log2_w) - 1;

        unsigned mx = offset & 0xf;
        unsigned by = offset & (~7 << log2_w);
        unsigned bx = offset & ((w_mask & 0xf) << 7);
        unsigned my = offset & 0x70;

        return by | (bx >> 3) | (my << (log2_w - 4)) | mx;
    }
    */
}

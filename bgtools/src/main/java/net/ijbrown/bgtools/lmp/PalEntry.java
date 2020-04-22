package net.ijbrown.bgtools.lmp;

public class PalEntry
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

        //java_a = (byte) 0xFF;

        int argb = (java_a << 24) |
                ((r << 16) & 0xFF0000) |
                ((g << 8) & 0xFF00) |
                (b & 0xFF);
        return argb;
    }

    public static PalEntry[] readPalette(byte[] fileData, int startOffset, int palw, int palh)
    {
        int numEntries = palw * palh;
        PalEntry[] palette = new PalEntry[numEntries];
        for (int i = 0; i < numEntries; ++i) {
            PalEntry pe = new PalEntry();
            pe.r = fileData[startOffset + i * 4];
            pe.g = fileData[startOffset + i * 4 + 1];
            pe.b = fileData[startOffset + i * 4 + 2];
            pe.a = fileData[startOffset + i * 4 + 3];

            palette[i] = pe;
        }
        return palette;
    }

    public static PalEntry[] unswizzlePalette(PalEntry[] palette)
    {
        if (palette.length == 256) {
            PalEntry[] unswizzled = new PalEntry[palette.length];

            int j = 0;
            for (int i = 0; i < 256; i += 32, j += 32) {
                copy(unswizzled, i, palette, j, 8);
                copy(unswizzled, i + 16, palette, j + 8, 8);
                copy(unswizzled, i + 8, palette, j + 16, 8);
                copy(unswizzled, i + 24, palette, j + 24, 8);
            }
            return unswizzled;
        } else {
            return palette;
        }
    }

    private static void copy(PalEntry[] unswizzled, int i, PalEntry[] swizzled, int j, int num)
    {
        for (int x = 0; x < num; ++x) {
            unswizzled[i + x] = swizzled[j + x];
        }
    }
}
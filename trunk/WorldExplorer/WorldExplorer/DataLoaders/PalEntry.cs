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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
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

            byte java_a = (byte)(256 - (byte)(a*2));

            //java_a = (byte)0xFF;

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
            if (palette.Length == 256) {
                PalEntry[] unswizzled = new PalEntry[palette.Length];

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
}

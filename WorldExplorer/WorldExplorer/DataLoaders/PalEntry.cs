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

namespace WorldExplorer.DataLoaders
{
    public class PalEntry
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        const int TransparentThreshold = 0x80;

        public int argb()
        {
            // Note to reviewers: I have no memory of this place

            // in ps2 0x80 is fully transparent and 0 is opaque.
            // in java 0 is transparent and 0xFF is opaque.

            var convertedAlpha = (byte)(256 - (byte)(a * 2));

            var java_a = a == 0 ? 255 :
                (byte)(256 - (byte)(a * 2));

            //if (convertedAlpha < TransparentThreshold)
            {
                convertedAlpha = (byte)(255 - a);
            }


            //java_a = (byte)0xFF;

            var argb = (convertedAlpha << 24) |
                    ((r << 16) & 0xFF0000) |
                    ((g << 8) & 0xFF00) |
                    (b & 0xFF);
            return argb;
        }

        public static PalEntry[] readPalette(byte[] fileData, int startOffset, int palw, int palh)
        {
            var numEntries = palw * palh;
            var palette = new PalEntry[numEntries];
            for (var i = 0; i < numEntries; ++i)
            {
                var pe = new PalEntry
                {
                    r = fileData[startOffset + i * 4],
                    g = fileData[startOffset + i * 4 + 1],
                    b = fileData[startOffset + i * 4 + 2],
                    a = fileData[startOffset + i * 4 + 3]
                };

                palette[i] = pe;
            }
            return palette;
        }

        public static PalEntry[] unswizzlePalette(PalEntry[] palette)
        {
            if (palette.Length == 256)
            {
                var unswizzled = new PalEntry[palette.Length];

                var j = 0;
                for (var i = 0; i < 256; i += 32, j += 32)
                {
                    copy(unswizzled, i, palette, j, 8);
                    copy(unswizzled, i + 16, palette, j + 8, 8);
                    copy(unswizzled, i + 8, palette, j + 16, 8);
                    copy(unswizzled, i + 24, palette, j + 24, 8);
                }
                return unswizzled;
            }
            else
            {
                return palette;
            }
        }

        private static void copy(PalEntry[] unswizzled, int i, PalEntry[] swizzled, int j, int num)
        {
            for (var x = 0; x < num; ++x)
            {
                unswizzled[i + x] = swizzled[j + x];
            }
        }
    }
}

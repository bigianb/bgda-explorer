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

namespace WorldExplorer.DataLoaders
{
    class HexUtil
    {
        public static string formatHexUShort(int num)
        {
            return "0x" + num.ToString("x4");
        }

        public static string formatHexByte(int num)
        {
            return "0x" + num.ToString("x2");
        }

        public static string formatHexShort(int num)
        {
            short signed = (short)num;
            string s = signed < 0 ? "-0x" : "0x";
            short posval = (short)(signed < 0 ? -signed : signed);
            return s + posval.ToString("x4");
        }

        public static string formatHexClean(byte num)
        {
            return num.ToString("x2");
        }

        public static string formatHex(int num)
        {
            return "0x" + num.ToString("x8");
        }
    }
}

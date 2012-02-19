using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    class HexUtil
    {
        public static String formatHexUShort(int num)
        {
            return "0x" + num.ToString("x4");
        }

        public static String formatHexByte(int num)
        {
            return "0x" + num.ToString("x2");
        }

        public static String formatHexShort(int num)
        {
            short signed = (short)num;
            String s = signed < 0 ? "-0x" : "0x";
            short posval = (short)(signed < 0 ? -signed : signed);
            return s + posval.ToString("x4");
        }

        public static String formatHexClean(byte num)
        {
            return num.ToString("x2");
        }

        public static String formatHex(int num)
        {
            return "0x" + num.ToString("x8");
        }
    }
}

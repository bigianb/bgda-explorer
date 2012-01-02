package net.ijbrown.bgtools.lmp;

/**
 * Utility for formatting hex numbers.
 */
public class HexUtil
{
    public static String formatHexUShort(int num)
    {
        String s = "0x";
        String s2 = Integer.toHexString(num & 0xFFFF);
        int pad = 4 - s2.length();
        while (pad > 0){
            s += '0';
            --pad;
        }
        return s+s2;
    }

    public static String formatHexByte(int num)
    {
        String s = "0x";
        String s2 = Integer.toHexString(num & 0xFF);
        int pad = 2 - s2.length();
        while (pad > 0){
            s += '0';
            --pad;
        }
        return s+s2;
    }

    public static String formatHexShort(int num)
    {
        short signed = (short)num;
        String s = signed < 0 ? "-0x" : "0x";
        String s2 = Integer.toHexString(signed < 0 ? -signed : signed);
        int pad = 4 - s2.length();
        while (pad > 0){
            s += '0';
            --pad;
        }
        return s+s2;
    }

    public static String formatHexClean(byte num)
    {
        String s = "";
        String s2 = Integer.toHexString((int)num & 0xFF);
        int pad = 2 - s2.length();
        while (pad > 0){
            s += '0';
            --pad;
        }
        return s+s2;
    }

    public static String formatHex(int num)
    {
        String s = "0x";
        String s2 = Integer.toHexString(num);
        int pad = 8 - s2.length();
        while (pad > 0){
            s += '0';
            --pad;
        }
        return s+s2;
    }

}

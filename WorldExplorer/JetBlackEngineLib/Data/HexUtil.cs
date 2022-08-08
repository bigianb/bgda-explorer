namespace JetBlackEngineLib.Data;

internal class HexUtil
{
    public static string FormatHexUShort(int num)
    {
        return "0x" + num.ToString("x4");
    }

    public static string FormatHexByte(int num)
    {
        return "0x" + num.ToString("x2");
    }

    public static string FormatHexShort(int num)
    {
        var signed = (short)num;
        var s = signed < 0 ? "-0x" : "0x";
        var posVal = (short)(signed < 0 ? -signed : signed);
        return s + posVal.ToString("x4");
    }

    public static string FormatHexClean(byte num)
    {
        return num.ToString("x2");
    }

    public static string FormatHex(int num)
    {
        return "0x" + num.ToString("x8");
    }
}
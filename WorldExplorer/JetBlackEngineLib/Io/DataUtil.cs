using System.IO;
using System.Text;

namespace JetBlackEngineLib.Io
{
    public static class DataUtil
    {
        public static string GetString(byte[] data, int index)
        {
            var length = 0;

            for (var i = index; i < data.Length && data[i] != 0; i++)
            {
                length++;
            }
            
            return Encoding.UTF8.GetString(data, index, length);
        }

        public static string GetString(ReadOnlySpan<byte> data, int offset = 0)
        {
            if (data.Length == 0)
                throw new EndOfStreamException();
            
            var length = 0;

            for (var i = offset; i < data.Length && data[i] != 0; i++)
            {
                length++;
            }

            return Encoding.UTF8.GetString(data.Slice(offset, length));
        }

        public static int getLEInt(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Slice(offset, 4).ToArray());
        }

        public static int getLEInt(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static short getLEShort(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Slice(offset, 2).ToArray());
        }

        public static short getLEShort(byte[] data, int offset)
        {
            return BitConverter.ToInt16(data, offset);
        }

        public static float getLEFloat(byte[] data, int offset)
        {
            return BitConverter.ToSingle(data, offset);
        }

        public static ushort getLEUShort(byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset);
        }

        public static ushort getLEUShort(ReadOnlySpan<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Slice(offset, 2).ToArray());
        }
    }
}
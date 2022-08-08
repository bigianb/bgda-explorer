using System.IO;
using System.Text;

namespace JetBlackEngineLib.Io
{
    internal class DataUtil
    {
        public static string GetString(byte[] data, int index)
        {
            StringBuilder sb = new();
            var i = index;

            // TODO: Check array length before access
            while (data[i] != 0)
            {
                sb.Append((char)data[i]);
                ++i;
            }

            return sb.ToString();
        }

        public static string GetString(ReadOnlySpan<byte> data, int offset = 0)
        {
            if (data.Length == 0)
                throw new EndOfStreamException();

            StringBuilder sb = new();
            for (var i = offset; i < data.Length; i++)
            {
                var character = data[i];
                if (character == 0) break;
                sb.Append((char)character);
            }

            return sb.ToString();
        }

        public static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
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
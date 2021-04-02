using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public static class DialogDecoder
    {
        public static List<DialogData> Decode(byte[] data, int startOffset, int length)
        {
            var reader = new DataReader(data, startOffset, length);
            var numEntries = length / 0x44;
            var objects = new List<DialogData>(numEntries);

            for (var entry = 0; entry < numEntries; ++entry)
            {
                var dataObj = new DialogData();
                var entryOffset = entry * 0x44;
                dataObj.Name = GetString(reader, entryOffset);
                reader.SetOffset(entryOffset + 0x40);
                dataObj.StartOffsetInVAFile = reader.ReadInt32();

                if (entry != numEntries - 1)
                {
                    reader.SetOffset(entryOffset + 0x84);
                    var nextStart = reader.ReadInt32();
                    dataObj.Length = nextStart - dataObj.StartOffsetInVAFile;
                }
                else
                {
                    // Means to the end of the VA file
                    dataObj.Length = 0;
                }
                objects.Add(dataObj);
            }
            return objects;
        }

        private static string GetString(DataReader reader, int offset)
        {
            var tempOffset = reader.Offset;
            reader.SetOffset(offset);
            var value = reader.ReadZString();
            reader.SetOffset(tempOffset);
            return value;
        }
    }

    public class DialogData
    {
        public string Name;
        public int StartOffsetInVAFile;
        public int Length;
    }
}

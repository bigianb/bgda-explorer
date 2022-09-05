namespace JetBlackEngineLib.Data.CutScenes;

public static class DialogDecoder
{
    public static List<DialogData> Decode(byte[] data, int startOffset, int length)
    {
        DataReader reader = new(data, startOffset, length);
        var numEntries = length / 0x44;
        List<DialogData> objects = new(numEntries);

        for (var entry = 0; entry < numEntries; ++entry)
        {
            var entryOffset = entry * 0x44;
            var name = GetString(reader, entryOffset);
            reader.SetOffset(entryOffset + 0x40);
            var startOffsetInVAFile = reader.ReadInt32();
            int dialogLength;

            if (entry != numEntries - 1)
            {
                reader.SetOffset(entryOffset + 0x84);
                var nextStart = reader.ReadInt32();
                dialogLength = nextStart - startOffsetInVAFile;
            }
            else
            {
                // Means to the end of the VA file
                dialogLength = 0;
            }

            objects.Add(new DialogData(name, startOffsetInVAFile, dialogLength));
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

    public DialogData(string name, int startOffsetInVaFile, int length)
    {
        Length = length;
        Name = name;
        StartOffsetInVAFile = startOffsetInVaFile;
    }
}
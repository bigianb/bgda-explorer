using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public static class ObDecoder
    {
        public static List<ObjectData> Decode(byte[] data, int startOffset, int length)
        {
            DataReader reader = new(data, startOffset, length);

            var count = reader.ReadInt16();
            // Skip the flags value since we don't use it right now
            reader.Skip(2); // var flags = reader.ReadInt16();
            var stringOffset = reader.ReadInt32();

            List<ObjectData> objects = new(count);

            for (var i = 0; i < count; i++)
            {
                var nameStringOffset = reader.ReadInt32();
                var name = GetString(reader, stringOffset + nameStringOffset);
                var structSize =
                    reader.ReadInt16(); // Size would be 20 if there isn't a stringoffsetarray, but usually 24 with an empty array
                var i6 = reader.ReadInt16();
                var floats = new float[3];
                floats[0] = reader.ReadFloat();
                floats[1] = reader.ReadFloat();
                floats[2] = reader.ReadFloat();
                
                List<string> props = new();
                
                if (structSize > 20)
                {
                    for (var o = 0; o < (structSize - 20) / 4; o++)
                    {
                        var propStringOffset = reader.ReadInt32();

                        if (propStringOffset == 0 && o == ((structSize - 20) / 4) - 1)
                            // There is always a null at the end of the array
                        {
                            break;
                        }

                        props.Add(GetString(reader, stringOffset + propStringOffset));
                    }
                }

                objects.Add(new(name, i6, floats, props));
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

    public class ObjectData
    {
        public readonly float[] Floats;
        public short I6;
        public string Name;
        public readonly List<string> Properties;

        public static ObjectData Empty => new ObjectData(
            "", 0, new float[3], new List<string>()
        );

        public ObjectData(string name, short i6, float[] floats, List<string> properties)
        {
            Floats = floats;
            I6 = i6;
            Name = name;
            Properties = properties;
        }

        public string? GetProperty(string name)
        {
            foreach (var prop in Properties)
            {
                var signIdx = prop.IndexOf('=');
                if (signIdx != -1 && prop.Substring(0, signIdx) == name)
                {
                    return prop.Substring(signIdx + 1);
                }
            }

            return null;
        }
    }
}
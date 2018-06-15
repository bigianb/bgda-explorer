using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public static class ObDecoder
    {
        public static List<ObjectData> Decode(byte[] data, int startOffset, int length)
        {
            var reader = new DataReader(data, startOffset, length);

            var count = reader.ReadInt16();
            var flags = reader.ReadInt16();
            var stringOffset = reader.ReadInt32();

            var objects = new List<ObjectData>(count);

            for (int i = 0; i < count; i++)
            {
                var obj = new ObjectData();

                int nameStringOffset = reader.ReadInt32();
                obj.Name = GetString(reader, stringOffset + nameStringOffset);
                short structSize = reader.ReadInt16(); // Size would be 20 if there isn't a stringoffsetarray, but usually 24 with an empty array
                obj.I6 = reader.ReadInt16();
                obj.Floats = new float[3];
                obj.Floats[0] = reader.ReadFloat();
                obj.Floats[1] = reader.ReadFloat();
                obj.Floats[2] = reader.ReadFloat();


                if (structSize > 20)
                {
                    var props = new List<string>();
                    for (int o =0; o < (structSize-20)/4; o++)
                    {
                        int propStringOffset = reader.ReadInt32();

                        if (propStringOffset == 0 && o == (structSize - 20) / 4 - 1)
                        {
                            // There is always a null at the end of the array
                            break;
                        }

                        props.Add(GetString(reader, stringOffset + propStringOffset));
                    }
                    obj.Properties = props;
                }

                objects.Add(obj);
            }

            return objects;
        }

        private static string GetString(DataReader reader, int offset)
        {
            int tempOffset = reader.Offset;
            reader.SetOffset(offset);
            var value = reader.ReadZString();
            reader.SetOffset(tempOffset);
            return value;
        }
    }

    public class ObjectData
    {
        public string Name;
        public short I6;
        public float[] Floats;
        public List<string> Properties;

        public string GetProperty(string name)
        {
            foreach (var prop in Properties)
            {
                int signIdx = prop.IndexOf('=');
                if (signIdx != -1 && prop.Substring(0, signIdx) == name)
                {
                    return prop.Substring(signIdx + 1);
                }
            }
            return null;
        }
    }
}

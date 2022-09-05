namespace JetBlackEngineLib.Data.World;

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
            if (signIdx != -1 && prop[..signIdx] == name)
            {
                return prop[(signIdx + 1)..];
            }
        }

        return null;
    }
}
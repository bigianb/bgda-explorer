namespace JetBlackEngineLib.Data.Textures;

public class YakEntry
{
    public readonly YakEntryChild?[] Children;

    public YakEntry(YakEntryChild?[] children)
    {
        Children = children;
    }
}
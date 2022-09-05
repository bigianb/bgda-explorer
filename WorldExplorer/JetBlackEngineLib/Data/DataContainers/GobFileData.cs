namespace JetBlackEngineLib.Data.DataContainers;

public struct GobFileData
{
    public Dictionary<string, GobFileEntry> Entries { get; set; }
}
    
public struct GobFileEntry
{
    public GobFileEntry(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }

    public int Offset { get; }
    public int Length { get; }
}
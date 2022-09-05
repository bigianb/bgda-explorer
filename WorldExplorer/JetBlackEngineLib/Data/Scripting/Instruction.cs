namespace JetBlackEngineLib.Data.Scripting;

public class Instruction
{
    public int Address;
    public readonly List<int> Args = new();
    public string? Label;
    public int OpCode;
}
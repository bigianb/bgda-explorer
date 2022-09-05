using JetBlackEngineLib.Core;
using System.Runtime.InteropServices;

namespace JetBlackEngineLib.Data.World;

/// <summary>
/// <see cref="WorldFileV2Decoder"/>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WorldV2Element
{
    /// <summary>
    /// The size in bytes of the struct.
    /// </summary>
    public const int Size = 0x3C;
        
    public int VifDataOffset { get; set; }
    public int VifLength { get; set; }
    public Vector3F Bounds1 { get; set; }
    public Vector3F Bounds2 { get; set; }
    public int TextureNum { get; set; }
    public int UnknownFlag36 { get; set; }
    public Vector3Int Pos { get; set; }
    public short TexCellXY { get; set; }
    public int RotFlags { get; set; }
    public short Unknown58 { get; set; }
}
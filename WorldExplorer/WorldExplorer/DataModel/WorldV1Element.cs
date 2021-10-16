using System.Runtime.InteropServices;
using WorldExplorer.Core;
using WorldExplorer.DataLoaders.World;

namespace WorldExplorer.DataModel
{
    /// <summary>
    /// <see cref="WorldFileV1Decoder"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldV1Element
    {
        /// <summary>
        /// The size in bytes of the struct.
        /// </summary>
        public const int Size = 0x38;
        
        public int VifDataOffset { get; set; }
        public int Tex2 { get; set; }
        public int VifLength { get; set; }
        public Vector3F Bounds1 { get; set; }
        public Vector3F Bounds2 { get; set; }
        public int TextureNum { get; set; }
        public short TexCellXY { get; set; }
        public Vector3Short Pos { get; set; }
        public int Flags { get; set; }
        public short SinAlpha { get; set; }
    }
}
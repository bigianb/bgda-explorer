using System.Runtime.InteropServices;

namespace WorldExplorer.DataModel
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldFileHeader
    {
        public int NumberOfElements { get; set; }
        public int Unknown04 { get; set; }
        public int Unknown08 { get; set; }
        public int Unknown12 { get; set; }
        public int NumberOfColumns { get; set; }
        public int NumberOfRows { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public int Unknown32 { get; set; }
        public int ElementArrayStart { get; set; }
        public int Unknown40 { get; set; }
        public int Unknown44 { get; set; }
        public int Offset56Columns { get; set; }
        public int Offset56Rows { get; set; }
        public int Offset56 { get; set; }
        public int Unknown60 { get; set; }
        public int Unknown64 { get; set; }
        public int Unknown68 { get; set; }
        public int Unknown72 { get; set; }
        public int Unknown76 { get; set; }
        public int Unknown80 { get; set; }
        public int Unknown84 { get; set; }
        public int Texll { get; set; }
        public int Texur { get; set; }
        public int Unknown96 { get; set; }
        public int WorldTexOffsetsOffset { get; set; }
    }
}
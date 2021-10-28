using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    public class WorldElement
    {
        public Rect3D BoundingBox { get; set; }
        public Model? Model { get; set; }
        public WriteableBitmap? Texture { get; set; }
        /// <summary>
        /// The position before rotation.
        /// </summary>
        public Vector3D Position { get; set; }
        /// <summary>
        /// Indicates if the y axis should be flipped. Does not apply when <see cref="UsesRotFlags" /> is true.
        /// </summary>
        public bool NegYaxis { get; set; }
        public double SinAlpha { get; set; }
        public double CosAlpha { get; set; }
        public bool UsesRotFlags { get; set; }
        public int XyzRotFlags { get; set; }
        public int ElementIndex { get; set; }
        
        /// <summary>
        /// Contains info on data this element references.
        /// </summary>
        public WorldElementDataInfo? DataInfo { get; set; }
    }
}
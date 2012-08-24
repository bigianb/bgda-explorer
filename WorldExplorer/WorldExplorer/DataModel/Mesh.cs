using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    public class Mesh
    {
        public Int32Collection TriangleIndices;
        public Point3DCollection Positions;
        public PointCollection TextureCoordinates;
        public Vector3DCollection Normals;
        public List<VertexWeight> vertexWeights;
    }
}

using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    public class Mesh
    {
        public readonly Vector3DCollection Normals;
        public readonly Point3DCollection Positions;
        public readonly PointCollection TextureCoordinates;
        public readonly Int32Collection TriangleIndices;
        public readonly List<VertexWeight> VertexWeights;

        public Mesh(Vector3DCollection normals, Point3DCollection positions, PointCollection textureCoordinates,
            Int32Collection triangleIndices, List<VertexWeight> vertexWeights)
        {
            Normals = normals;
            Positions = positions;
            TextureCoordinates = textureCoordinates;
            TriangleIndices = triangleIndices;
            VertexWeights = vertexWeights;
        }
    }
}
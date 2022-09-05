using JetBlackEngineLib.Data.Animation;
using System.Windows.Media.Media3D;
using System.Windows;

namespace JetBlackEngineLib.Data.Models;

public class Mesh
{
    public readonly IList<Vector3D> Normals;
    public readonly IList<Point3D> Positions;
    public readonly IList<Point> TextureCoordinates;
    public readonly IList<int> TriangleIndices;
    public readonly IList<VertexWeight> VertexWeights;

    public Mesh(IList<Vector3D> normals, IList<Point3D> positions, IList<Point> textureCoordinates,
        IList<int> triangleIndices, IList<VertexWeight> vertexWeights)
    {
        Normals = normals;
        Positions = positions;
        TextureCoordinates = textureCoordinates;
        TriangleIndices = triangleIndices;
        VertexWeights = vertexWeights;
    }
}
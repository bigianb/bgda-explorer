/*  Copyright (C) 2012-2018 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;

namespace WorldExplorer.Win3D
{
    public class Conversions
    {
        public static Model3D CreateModel3D(List<Mesh> meshGroups, BitmapSource texture, AnimData pose, int frame)
        {
            var model = new GeometryModel3D();
            var mesh3D = new MeshGeometry3D();

            var numVertices = 0;
            foreach (var meshGroup in meshGroups)
            {
                numVertices += meshGroup.Positions.Count;
            }
            var triangleIndices = new Int32Collection();
            var positions = new Point3DCollection(numVertices);
            var normals = new Vector3DCollection(numVertices);
            var uvCoords = new PointCollection(numVertices);
            var vstart = 0;

            foreach (var meshGroup in meshGroups)
            {
                var hasVertexWeights = meshGroup.vertexWeights.Count > 0;
                var vwNum = 0;
                var vw = new VertexWeight();
                if (meshGroup.vertexWeights.Count > 0)
                {
                    vw = meshGroup.vertexWeights[vwNum];
                }
                var vnum = 0;
                foreach (var vertex in meshGroup.Positions)
                {
                    var point = vertex;
                    if (frame >= 0 && pose != null)
                    {
                        if (vw.endVertex < vnum)
                        {
                            ++vwNum;
                            vw = meshGroup.vertexWeights[vwNum];
                            if (vnum < vw.startVertex || vnum > vw.endVertex)
                            {
                                Debug.Fail("Vertex " + vnum + " out of range of bone weights " + vw.startVertex + " -> " + vw.endVertex);
                            }
                        }
                        var bone1No = vw.bone1;
                        var bindingPos1 = pose.bindingPose[bone1No];
                        var bone1Pose = pose.perFrameFKPoses[frame, bone1No];
                        var joint1Pos = bone1Pose.Position;
                        if (vw.bone2 == 0xFF)
                        {
                            if (bone1No == 1)
                            {
                                bone1No = 1;
                            }
                            var m = Matrix3D.Identity;
                            m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y, -bindingPos1.Z));   // Inverse binding matrix
                            m.Rotate(bone1Pose.Rotation);
                            m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                            point = m.Transform(point);
                        }
                        else
                        {
                            // multi-bone
                            var bone2No = vw.bone2;
                            var bindingPos2 = pose.bindingPose[bone2No];
                            var bone2Pose = pose.perFrameFKPoses[frame, bone2No];
                            double boneSum = vw.boneWeight1 + vw.boneWeight2;
                            var bone1Coeff = vw.boneWeight1 / boneSum;
                            var bone2Coeff = vw.boneWeight2 / boneSum;

                            var m = Matrix3D.Identity;
                            m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y, -bindingPos1.Z));   // Inverse binding matrix
                            m.Rotate(bone1Pose.Rotation);
                            m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                            var point1 = m.Transform(point);

                            // Now rotate
                            var m2 = Matrix3D.Identity;
                            m2.Translate(new Vector3D(-bindingPos2.X, -bindingPos2.Y, -bindingPos2.Z));   // Inverse binding matrix
                            m2.Rotate(bone2Pose.Rotation);
                            m2.Translate(new Vector3D(bone2Pose.Position.X, bone2Pose.Position.Y, bone2Pose.Position.Z));
                            var point2 = m2.Transform(point);

                            point = new Point3D(point1.X * bone1Coeff + point2.X * bone2Coeff, point1.Y * bone1Coeff + point2.Y * bone2Coeff, point1.Z * bone1Coeff + point2.Z * bone2Coeff);
                        }
                    }
                    positions.Add(point);
                    ++vnum;
                }
                foreach (var normal in meshGroup.Normals)
                {
                    normals.Add(normal);
                }
                foreach (var ti in meshGroup.TriangleIndices)
                {
                    triangleIndices.Add(ti + vstart);
                }
                foreach (var uv in meshGroup.TextureCoordinates)
                {
                    uvCoords.Add(uv);
                }
                vstart += meshGroup.Positions.Count;
            }
            mesh3D.TriangleIndices = triangleIndices;
            mesh3D.Positions = positions;
            mesh3D.TextureCoordinates = uvCoords;
            mesh3D.Normals = normals;
            model.Geometry = mesh3D;
            var dm = new DiffuseMaterial();
            if (texture != null && texture.Width > 0 && texture.Height > 0)
            {
                var ib = new ImageBrush(texture)
                {
                    ViewportUnits = BrushMappingMode.Absolute
                };
                // May be needed at a later point
                //ib.TileMode = TileMode.Tile;
                dm.Brush = ib;
            }
            else
            {
                var dg = new DrawingGroup();
                // Background
                dg.Children.Add(new GeometryDrawing()
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Geometry = new RectangleGeometry(new Rect(0, 0, 2, 2))
                });

                // Tiles
                dg.Children.Add(new GeometryDrawing()
                {
                    Brush = new SolidColorBrush(Colors.Violet),
                    Geometry = new RectangleGeometry(new Rect(0, 0, 1, 1))
                });
                dg.Children.Add(new GeometryDrawing()
                {
                    Brush = new SolidColorBrush(Colors.Violet),
                    Geometry = new RectangleGeometry(new Rect(1, 1, 1, 1))
                });

                dm.Brush = new DrawingBrush(dg) { TileMode = TileMode.Tile, Transform = new ScaleTransform(0.1, 0.1) };
            }
            model.Material = dm;
            return model;
        }
    }
}

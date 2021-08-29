﻿using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataExporters
{
    public class VifObjExporter : IVifExporter
    {
        static void WriteMtlFile(string mtlFile, string name)
        {
            using (var stream = new FileStream(mtlFile, FileMode.Create))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("newmtl " + name);
                writer.WriteLine("map_Kd .\\" + name + ".png");

                writer.Flush();
            }
        }

        public void SaveToFile(string savePath, Model model, WriteableBitmap texture, AnimData pose, int frame, double scale)
        {
            var dir = Path.GetDirectoryName(savePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(savePath);

            // Save the texture to a .png file
            using (var stream = new FileStream(Path.Combine(dir, name + ".png"), FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(texture));
                encoder.Save(stream);

                stream.Flush();
                stream.Close();
            }

            WriteMtlFile(Path.Combine(dir, name + ".mtl"), name);

            var objFile = File.Open(Path.Combine(dir, name + ".obj"), FileMode.Create);
            var writer = new StreamWriter(objFile);

            writer.WriteLine("mtllib " + name + ".mtl");
            writer.WriteLine("");

            var vStart = 0;
            var meshCount = 1;
            foreach (var mesh in model.meshList)
            {
                writer.WriteLine("g Mesh_" + meshCount);
                writer.WriteLine("usemtl " + name);

                var hasVertexWeights = mesh.vertexWeights.Count > 0;
                var vwNum = 0;
                var vw = new VertexWeight();
                if (mesh.vertexWeights.Count > 0)
                {
                    vw = mesh.vertexWeights[vwNum];
                }
                var vnum = 0;
                foreach (var vertex in mesh.Positions)
                {
                    var point = vertex;
                    if (frame >= 0 && pose != null)
                    {
                        if (vw.endVertex < vnum)
                        {
                            ++vwNum;
                            vw = mesh.vertexWeights[vwNum];
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
                    ++vnum;
                    writer.WriteLine("v {0} {1} {2}",
                        FormatDouble(point.X / scale),
                        FormatDouble(point.Y / scale),
                        FormatDouble(point.Z / scale));
                }
                writer.WriteLine("");

                foreach (var uv in mesh.TextureCoordinates)
                {
                    writer.WriteLine("vt {0} {1}",
                        FormatDouble(uv.X),
                        FormatDouble(1 - uv.Y)); // Flip uv's vertically
                }
                writer.WriteLine("");

                foreach (var vec in mesh.Normals)
                {
                    // TODO: If the mesh is posed, the normals needs modifying too.
                    writer.WriteLine("vn {0} {1} {2}",
                        FormatDouble(vec.X),
                        FormatDouble(vec.Y),
                        FormatDouble(vec.Z));
                }
                writer.WriteLine("");

                for (var i = 0; i < mesh.TriangleIndices.Count - 3; i += 6)
                {
                    writer.WriteLine("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
                        mesh.TriangleIndices[i] + 1 + vStart,
                        mesh.TriangleIndices[i] + 1 + vStart,
                        mesh.TriangleIndices[i] + 1 + vStart,

                        mesh.TriangleIndices[i + 1] + 1 + vStart,
                        mesh.TriangleIndices[i + 1] + 1 + vStart,
                        mesh.TriangleIndices[i + 1] + 1 + vStart,

                        mesh.TriangleIndices[i + 2] + 1 + vStart,
                        mesh.TriangleIndices[i + 2] + 1 + vStart,
                        mesh.TriangleIndices[i + 2] + 1 + vStart);
                }
                writer.WriteLine("");

                vStart += mesh.Positions.Count;
                meshCount++;
            }

            writer.Flush();
            writer.Close();
        }

        

        private static string FormatDouble(double d)
        {
            return d.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;


namespace WorldExplorer.DataExporters
{
    class VifExporter
    {
        static void WriteMtlFile(string mtlFile, String name)
        {
            using (var stream = new FileStream(mtlFile, FileMode.Create))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("newmtl " + name);
                writer.WriteLine("map_Kd .\\" + name + ".png");

                writer.Flush();
            }
        }

        public static void WriteObj(String savePath, Model model, WriteableBitmap texture, double scale)
        {
            WritePosedObj(savePath, model, texture, null, 1, scale);
        }

        public static void WritePosedObj(string savePath, Model model, WriteableBitmap texture, AnimData pose, int frame, double scale)
        {
            string dir = Path.GetDirectoryName(savePath) ?? "";
            string name = Path.GetFileNameWithoutExtension(savePath);

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

            int vStart = 0;
            int meshCount = 1;
            foreach (var mesh in model.meshList)
            {
                writer.WriteLine("g Mesh_" + meshCount);
                writer.WriteLine("usemtl " + name);

                Boolean hasVertexWeights = mesh.vertexWeights.Count > 0;
                int vwNum = 0;
                VertexWeight vw = new VertexWeight();
                if (mesh.vertexWeights.Count > 0)
                {
                    vw = mesh.vertexWeights[vwNum];
                }
                int vnum = 0;
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
                        int bone1No = vw.bone1;
                        Point3D bindingPos1 = pose.bindingPose[bone1No];
                        AnimMeshPose bone1Pose = pose.perFrameFKPoses[frame, bone1No];
                        var joint1Pos = bone1Pose.Position;
                        if (vw.bone2 == 0xFF)
                        {
                            if (bone1No == 1)
                            {
                                bone1No = 1;
                            }
                            Matrix3D m = Matrix3D.Identity;
                            m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y, -bindingPos1.Z));   // Inverse binding matrix
                            m.Rotate(bone1Pose.Rotation);
                            m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                            point = m.Transform(point);
                        }
                        else
                        {
                            // multi-bone
                            int bone2No = vw.bone2;
                            Point3D bindingPos2 = pose.bindingPose[bone2No];
                            AnimMeshPose bone2Pose = pose.perFrameFKPoses[frame, bone2No];
                            double boneSum = vw.boneWeight1 + vw.boneWeight2;
                            double bone1Coeff = vw.boneWeight1 / boneSum;
                            double bone2Coeff = vw.boneWeight2 / boneSum;

                            Matrix3D m = Matrix3D.Identity;
                            m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y, -bindingPos1.Z));   // Inverse binding matrix
                            m.Rotate(bone1Pose.Rotation);
                            m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                            var point1 = m.Transform(point);

                            // Now rotate
                            Matrix3D m2 = Matrix3D.Identity;
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

                for (int i = 0; i < mesh.TriangleIndices.Count-3; i += 6)
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

        public static void WriteChunks(string savePath, List<VifDecoder.Chunk> chunks)
        {
            using (var objFile = File.Open(savePath, FileMode.Create))
            using (var writer = new StreamWriter(objFile))
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];

                    writer.WriteLine("Chunk {0}", (i + 1));

                    writer.WriteLine("MSCAL: {0}", chunk.mscalID);
                    if (chunk.gifTag0 != null)
                        writer.WriteLine("GifTag0: {0}", chunk.gifTag0.ToString());
                    if (chunk.gifTag1 != null)
                        writer.WriteLine("GifTag1: {0}", chunk.gifTag1.ToString());

                    writer.WriteLine("");
                    writer.WriteLine("==== Vertices ====");
                    foreach (var vert in chunk.vertices)
                    {
                        writer.WriteLine("X: {0}, Y: {1}, Z: {2}", vert.x, vert.y, vert.z);
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== VLocs ====");
                    foreach (var vloc in chunk.vlocs)
                    {
                        writer.WriteLine(
                            "V1: {0}, V2: {1}, V3: {2}",
                            FormatFlagShort(vloc.v1),
                            FormatFlagShort(vloc.v2),
                            FormatFlagShort(vloc.v3));
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== Extra VLocs ====");
                    for (int o = 0; o + 3 < chunk.extraVlocs.Length; o += 4)
                    {
                        writer.WriteLine(
                            "V1: {0}, V2: {1}, V3: {2}, V4: {3}",
                            FormatFlagShort(chunk.extraVlocs[o]),
                            FormatFlagShort(chunk.extraVlocs[o + 1]),
                            FormatFlagShort(chunk.extraVlocs[o + 2]),
                            FormatFlagShort(chunk.extraVlocs[o + 3]));
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== UVs ====");
                    foreach (var uv in chunk.uvs)
                    {
                        writer.WriteLine(
                            "U: {0}, V: {1}", 
                            FormatDouble(uv.u/16.0), 
                            FormatDouble(uv.v/16.0));
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== Normals ====");
                    foreach (var normal in chunk.normals)
                    {
                        writer.WriteLine("X: {0}, Y: {1}, Z: {2}", normal.x, normal.y, normal.z);
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== Vertex Weights ====");
                    foreach (var weight in chunk.vertexWeights)
                    {
                        writer.WriteLine(
                            "StartVertex: {0}, EndVertex: {1}",
                            weight.startVertex,
                            weight.endVertex);
                        writer.WriteLine(
                            "Bone1: {0}, Bone2: {1}, Bone3: {2}, Bone4: {3}",
                            weight.bone1,
                            weight.bone2,
                            weight.bone3,
                            weight.bone4);
                        writer.WriteLine(
                            "BoneWeight1: {0}, BoneWeight2: {1}, BoneWeight3: {2}, BoneWeight4: {3}",
                            weight.boneWeight1,
                            weight.boneWeight2,
                            weight.boneWeight3,
                            weight.boneWeight4);
                        writer.WriteLine("");
                    }

                    writer.WriteLine("");
                    writer.WriteLine("==== Direct Gif Tags ====");
                    foreach (var tag in chunk.DIRECTGifTags)
                    {
                        writer.WriteLine(tag.ToString());
                    }

                    writer.WriteLine("");
                }
            }
        }

        private static string FormatDouble(double d)
        {
            return d.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
        }
        private static string FormatFlagShort(int i)
        {
            return "0x" + i.ToString("X4");
        }
        private static string FormatFlag(int i)
        {
            return "0x"+i.ToString("X8");
        }
    }
}

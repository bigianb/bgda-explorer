using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataExporters
{
    class VifExporter
    {
        void WriteMtlFile(string mtlFile, String name)
        {
            using (var stream = new FileStream(mtlFile, FileMode.Create))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("newmtl " + name);
                writer.WriteLine("map_Kd .\\" + name + ".png");

                writer.Flush();
            }
        }

        public void WriteObj(String savePath, Model model, WriteableBitmap texture, double scale)
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

                foreach (var vertex in mesh.Positions)
                {
                    writer.WriteLine("v {0} {1} {2}", 
                        FormatDouble(vertex.X / scale), 
                        FormatDouble(vertex.Y / scale), 
                        FormatDouble(vertex.Z / scale));
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

        public void WriteChunks(string savePath, List<VifDecoder.Chunk> chunks)
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
                    writer.WriteLine("==== Direct Bytes ====");
                    foreach (var bytes in chunk.DIRECTBytes)
                    {
                        foreach(var b in bytes)
                        {
                            writer.Write("{0:X2} ", b);
                        }
                        writer.WriteLine("");
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

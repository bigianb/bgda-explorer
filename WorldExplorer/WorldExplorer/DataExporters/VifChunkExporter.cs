using System.Collections.Generic;
using System.IO;
using WorldExplorer.DataLoaders;

namespace WorldExplorer.DataExporters
{
    public class VifChunkExporter
    {
        public static void WriteChunks(string savePath, List<VifDecoder.Chunk> chunks)
        {
            using (var objFile = File.Open(savePath, FileMode.Create))
            using (var writer = new StreamWriter(objFile))
            {
                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];

                    writer.WriteLine("Chunk {0}", (i + 1));

                    writer.WriteLine("MSCAL: {0}", chunk.mscalID);
                    if (chunk.gifTag0 != null)
                    {
                        writer.WriteLine("GifTag0: {0}", chunk.gifTag0.ToString());
                    }

                    if (chunk.gifTag1 != null)
                    {
                        writer.WriteLine("GifTag1: {0}", chunk.gifTag1.ToString());
                    }

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
                    for (var o = 0; o + 3 < chunk.extraVlocs.Length; o += 4)
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
                            FormatDouble(uv.u / 16.0),
                            FormatDouble(uv.v / 16.0));
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
    }
}

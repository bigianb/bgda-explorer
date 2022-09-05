using JetBlackEngineLib.Data.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WorldExplorer.DataExporters;

public class VifChunkExporter
{
    public static void WriteChunks(string savePath, List<VifDecoder.Chunk> chunks)
    {
        using (var objFile = File.Open(savePath, FileMode.Create))
        using (StreamWriter writer = new(objFile))
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];

                writer.WriteLine("Chunk {0}", i + 1);

                writer.WriteLine("MSCAL: {0}", chunk.mscalID);
                if (chunk.gifTag0 != null)
                {
                    writer.WriteLine("GifTag0: {0}", chunk.gifTag0);
                }

                if (chunk.gifTag1 != null)
                {
                    writer.WriteLine("GifTag1: {0}", chunk.gifTag1);
                }

                writer.WriteLine("");
                writer.WriteLine("==== Vertices ====");
                foreach (var vert in chunk.Vertices)
                {
                    writer.WriteLine("X: {0}, Y: {1}, Z: {2}", vert.X, vert.Y, vert.Z);
                }

                writer.WriteLine("");
                writer.WriteLine("==== VLocs ====");
                foreach (var vloc in chunk.VLocs)
                {
                    writer.WriteLine(
                        "V1: {0}, V2: {1}, V3: {2}",
                        FormatFlagShort(vloc.Value1),
                        FormatFlagShort(vloc.Value2),
                        FormatFlagShort(vloc.Value3));
                }

                if (chunk.ExtraVlocs != null)
                {
                    writer.WriteLine("");
                    writer.WriteLine("==== Extra VLocs ====");
                    for (var o = 0; o + 3 < chunk.ExtraVlocs.Length; o += 4)
                    {
                        writer.WriteLine(
                            "V1: {0}, V2: {1}, V3: {2}, V4: {3}",
                            FormatFlagShort(chunk.ExtraVlocs[o]),
                            FormatFlagShort(chunk.ExtraVlocs[o + 1]),
                            FormatFlagShort(chunk.ExtraVlocs[o + 2]),
                            FormatFlagShort(chunk.ExtraVlocs[o + 3]));
                    }
                }

                writer.WriteLine("");
                writer.WriteLine("==== UVs ====");
                foreach (var uv in chunk.UVs)
                {
                    writer.WriteLine(
                        "U: {0}, V: {1}",
                        FormatDouble(uv.U / 16.0),
                        FormatDouble(uv.V / 16.0));
                }

                writer.WriteLine("");
                writer.WriteLine("==== Normals ====");
                foreach (var normal in chunk.Normals)
                {
                    writer.WriteLine("X: {0}, Y: {1}, Z: {2}", normal.X, normal.Y, normal.Z);
                }

                writer.WriteLine("");
                writer.WriteLine("==== Vertex Weights ====");
                foreach (var weight in chunk.VertexWeights)
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
                foreach (var tag in chunk.DirectGifTags)
                {
                    writer.WriteLine(tag.ToString());
                }

                writer.WriteLine("");
            }
        }
    }

    private static string FormatDouble(double d)
    {
        return d.ToString("0.0000", CultureInfo.InvariantCulture);
    }

    private static string FormatFlagShort(int i)
    {
        return "0x" + i.ToString("X4");
    }
}
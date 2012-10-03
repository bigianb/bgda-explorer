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
            using (var stream = File.Open(mtlFile, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("newmtl " + name);
                writer.WriteLine("map_Kd " + name + ".png");

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

            var objFile = File.OpenWrite(Path.Combine(dir, name + ".obj"));
            var writer = new StreamWriter(objFile);

            writer.WriteLine("g " + name);
            writer.WriteLine("mtllib " + name + ".mtl");
            writer.WriteLine("usemtl " + name);

            foreach (var mesh in model.meshList)
            {
                foreach (var vertex in mesh.Positions)
                {
                    writer.WriteLine("v {0} {1} {2}", 
                        FormatDouble(vertex.X / scale), 
                        FormatDouble(vertex.Y / scale), 
                        FormatDouble(vertex.Z / scale));
                }

                foreach (var uv in mesh.TextureCoordinates)
                {
                    writer.WriteLine("vt {0} {1}", 
                        FormatDouble(uv.X), 
                        FormatDouble(uv.Y));
                }

                foreach (var vec in mesh.Normals)
                {
                    writer.WriteLine("vn {0} {1} {2}", 
                        FormatDouble(vec.X), 
                        FormatDouble(vec.Y), 
                        FormatDouble(vec.Z));
                }

                // TODO: Skip the duplicate face, but also skip adding duplicate positions, uv's etc
                for (int i = 0; i < mesh.TriangleIndices.Count-3; i += 3)
                {
                    writer.WriteLine("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
                        mesh.TriangleIndices[i] + 1,
                        mesh.TriangleIndices[i + 1] + 1,
                        mesh.TriangleIndices[i + 2] + 1,

                        mesh.TriangleIndices[i] + 1,
                        mesh.TriangleIndices[i + 1] + 1,
                        mesh.TriangleIndices[i + 2] + 1,

                        mesh.TriangleIndices[i] + 1,
                        mesh.TriangleIndices[i + 1] + 1,
                        mesh.TriangleIndices[i + 2] + 1);
                }
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

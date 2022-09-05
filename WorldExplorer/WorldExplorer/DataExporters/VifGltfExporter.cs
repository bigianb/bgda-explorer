using JetBlackEngineLib.Data.Animation;
using JetBlackEngineLib.Data.Models;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using VERTEX = SharpGLTF.Geometry.VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPosition,
    SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>;

namespace WorldExplorer.DataExporters;

public class VifGltfExporter : IVifExporter
{
    public void SaveToFile(string savePath, Model model, WriteableBitmap? texture, AnimData? pose, int frame,
        double scale)
    {
        var dir = Path.GetDirectoryName(savePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(savePath);

        var diffuseTexturePath = Path.Combine(dir, name + ".png");
            
        if (texture != null)
        {
            // Save the texture to a .png file
            using FileStream stream = new(diffuseTexturePath, FileMode.Create);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(texture));
            encoder.Save(stream);

            stream.Flush();
            stream.Close();
        }

        var material1 = new MaterialBuilder()
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader()
            .WithChannelImage(KnownChannel.BaseColor, diffuseTexturePath);
        MeshBuilder<VertexPosition, VertexTexture1, VertexJoints4> meshB =
            new("mesh");

        var vStart = 0;
        var meshCount = 1;
        foreach (var mesh in model.MeshList)
        {
            var prim =
                meshB.UsePrimitive(material1);
            var verts = new VERTEX[mesh.Positions.Count];

            var hasVertexWeights = mesh.VertexWeights.Count > 0;
            var vwNum = 0;
            var vw = hasVertexWeights ? mesh.VertexWeights[vwNum] : new VertexWeight();
            var vNum = 0;

            for (var i = 0; i < mesh.Positions.Count; i++)
            {
                var pos = mesh.Positions[i];
                var uv = mesh.TextureCoordinates[i];
                // var normal = mesh.Normals[i];

                if (vw.endVertex < vNum)
                {
                    ++vwNum;
                    vw = mesh.VertexWeights[vwNum];
                    if (i < vw.startVertex || i > vw.endVertex)
                    {
                        Debug.Fail("Vertex " + i + " out of range of bone weights " + vw.startVertex + " -> " +
                                   vw.endVertex);
                    }
                }

                VertexPosition pos1 = new((float)(pos.X / scale), (float)(pos.Y / scale),
                    (float)(pos.Z / scale));
                VertexTexture1 tex1 = new(new Vector2((float)uv.X, (float)uv.Y));
                List<(int JointIndex, float Weight)> weight = new();

                weight.Add((vw.bone1, vw.boneWeight1 / 255.0f));
                if (vw.bone2 != 0)
                {
                    weight.Add((vw.bone2, vw.boneWeight2 / 255.0f));
                }

                if (vw.bone3 != 0)
                {
                    weight.Add((vw.bone3, vw.boneWeight3 / 255.0f));
                }

                if (vw.bone4 != 0)
                {
                    weight.Add((vw.bone4, vw.boneWeight4 / 255.0f));
                }

                verts[i] = new VERTEX(pos1, tex1, weight.ToArray());
            }

            vwNum = 0;
            vw = hasVertexWeights ? mesh.VertexWeights[vwNum] : new VertexWeight();
            vNum = 0;
            foreach (var vertex in mesh.Positions)
            {
                var point = vertex;
                if (frame >= 0 && pose != null)
                {
                    if (vw.endVertex < vNum)
                    {
                        ++vwNum;
                        vw = mesh.VertexWeights[vwNum];
                        if (vNum < vw.startVertex || vNum > vw.endVertex)
                        {
                            Debug.Fail("Vertex " + vNum + " out of range of bone weights " + vw.startVertex +
                                       " -> " + vw.endVertex);
                        }
                    }

                    var bone1No = vw.bone1;
                    var bindingPos1 = pose.BindingPose[bone1No];
                    var bone1Pose = pose.PerFrameFkPoses?[frame, bone1No]
                                    ?? throw new InvalidDataException("Invalid frame/bone pair encountered!");
                    // var joint1Pos = bone1Pose.Position;
                    if (vw.bone2 == 0xFF)
                    {
                        var m = Matrix3D.Identity;
                        m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y,
                            -bindingPos1.Z)); // Inverse binding matrix
                        m.Rotate(bone1Pose.Rotation);
                        m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                        // point = m.Transform(point);
                    }
                    else
                    {
                        // multi-bone
                        var bone2No = vw.bone2;
                        var bindingPos2 = pose.BindingPose[bone2No];
                        var bone2Pose = pose.PerFrameFkPoses[frame, bone2No];
                        double boneSum = vw.boneWeight1 + vw.boneWeight2;
                        var bone1Coeff = vw.boneWeight1 / boneSum;
                        var bone2Coeff = vw.boneWeight2 / boneSum;

                        var m = Matrix3D.Identity;
                        m.Translate(new Vector3D(-bindingPos1.X, -bindingPos1.Y,
                            -bindingPos1.Z)); // Inverse binding matrix
                        m.Rotate(bone1Pose.Rotation);
                        m.Translate(new Vector3D(bone1Pose.Position.X, bone1Pose.Position.Y, bone1Pose.Position.Z));
                        var point1 = m.Transform(point);

                        // Now rotate
                        var m2 = Matrix3D.Identity;
                        m2.Translate(new Vector3D(-bindingPos2.X, -bindingPos2.Y,
                            -bindingPos2.Z)); // Inverse binding matrix
                        m2.Rotate(bone2Pose.Rotation);
                        m2.Translate(new Vector3D(bone2Pose.Position.X, bone2Pose.Position.Y,
                            bone2Pose.Position.Z));
                        var point2 = m2.Transform(point);

                        point = new Point3D((point1.X * bone1Coeff) + (point2.X * bone2Coeff),
                            (point1.Y * bone1Coeff) + (point2.Y * bone2Coeff),
                            (point1.Z * bone1Coeff) + (point2.Z * bone2Coeff));
                    }
                }

                ++vNum;
            }

            for (var i = 0; i < mesh.TriangleIndices.Count - 3; i += 6)
            {
                prim.AddTriangle(verts[mesh.TriangleIndices[i]], verts[mesh.TriangleIndices[i + 1]],
                    verts[mesh.TriangleIndices[i + 2]]);
            }

            vStart += mesh.Positions.Count;
            meshCount++;
        }

        // create a scene
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("Default");

        var skelet = scene.CreateNode("Skeleton");
        var joint0 = skelet.CreateNode("Joint 0").WithLocalTranslation(new Vector3(0, 0, 0));
        var joint1 =
            joint0.CreateNode("Joint 1")
                .WithLocalTranslation(new Vector3(0, 40, 0)); //.WithRotationAnimation("Base Track", keyframes);
        var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0));

        var snode = scene.CreateNode("Skeleton Node");
        snode.Skin = modelRoot.CreateSkin();
        snode.Skin.BindJoints(joint0, joint1, joint2);
        snode.WithMesh(modelRoot.CreateMesh(meshB));

        // save the model in different formats
        modelRoot.SaveGLTF(Path.Combine(dir, name + ".gltf"));
    }
}
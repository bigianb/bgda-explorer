using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders.Animation
{
    public class RtaAnmDecoder : AnmDecoder
    {
        public override AnimData Decode(ReadOnlySpan<byte> data)
        {
            var numBones = DataUtil.getLEInt(data, 0);
            var offset4Val = DataUtil.getLEInt(data, 4); // max frame
            var offset14Val = DataUtil.getLEInt(data, 0x14);
            var offset18Val = DataUtil.getLEInt(data, 0x18);
            var offset8Val = DataUtil.getLEInt(data, 8);

            var bindingPoseOffset = DataUtil.getLEInt(data, 0x0C);
            var bindingPose = new Point3D[numBones];
            for (var i = 0; i < numBones; ++i)
            {
                bindingPose[i] = new Point3D(
                    -DataUtil.getLEShort(data, bindingPoseOffset + (i * 8) + 0) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + (i * 8) + 2) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + (i * 8) + 4) / 64.0
                );
            }

            // Skeleton structure
            var offset10Val = DataUtil.getLEInt(data, 0x10);
            var skeletonDef = new int[numBones];
            for (var i = 0; i < numBones; ++i)
            {
                skeletonDef[i] = data[offset10Val + i];
            }

            var curPose = new AnimMeshPose[numBones];
            var meshPoses = new List<AnimMeshPose?>();
            AnimMeshPose? pose;
            BitstreamReader bitReader = new(data.ToArray(), offset8Val, data.Length - offset8Val);
            for (var boneNum = 0; boneNum < numBones; ++boneNum)
            {
                pose = new AnimMeshPose {BoneNum = boneNum, FrameNum = 0};

                var posLen = bitReader.Read(4) + 1;
                pose.Position = new Point3D(
                    bitReader.ReadSigned(posLen) / 64.0,
                    bitReader.ReadSigned(posLen) / 64.0,
                    bitReader.ReadSigned(posLen) / 64.0);

                var rotLen = bitReader.Read(4) + 1;
                var a = bitReader.ReadSigned(rotLen) / 4096.0;
                var b = bitReader.ReadSigned(rotLen) / 4096.0;
                var c = bitReader.ReadSigned(rotLen) / 4096.0;
                var d = bitReader.ReadSigned(rotLen) / 4096.0;

                pose.Rotation = new Quaternion(b, c, d, a);

                pose.Velocity = new Point3D(0, 0, 0);
                pose.AngularVelocity = new Quaternion(0, 0, 0, 0);

                // This may give us duplicate frame zero poses, but that's ok.
                meshPoses.Add(pose);
                curPose[boneNum] = new AnimMeshPose(pose);
            }

            var curAngVelFrame = new int[numBones];
            var curVelFrame = new int[numBones];

            var totalFrame = 0;
            pose = null;
            while (bitReader.HasData(22) && totalFrame < offset4Val)
            {
                int count = bitReader.Read(8);
                if (count == 0xFF)
                {
                    break;
                }

                int flag = bitReader.Read(1);
                int boneNum = bitReader.Read(6);

                if (boneNum >= numBones)
                {
                    break;
                }

                totalFrame += count;

                if (pose == null || pose.FrameNum != totalFrame || pose.BoneNum != boneNum)
                {
                    if (pose != null)
                    {
                        meshPoses.Add(pose);
                    }

                    pose = new AnimMeshPose
                    {
                        FrameNum = totalFrame,
                        BoneNum = boneNum,
                        Position = curPose[boneNum].Position,
                        Rotation = curPose[boneNum].Rotation,
                        AngularVelocity = curPose[boneNum].AngularVelocity,
                        Velocity = curPose[boneNum].Velocity
                    };
                }

                if (flag == 1)
                {
                    // xyz
                    var posLen = bitReader.Read(4) + 1;

                    var x = bitReader.ReadSigned(posLen);
                    var y = bitReader.ReadSigned(posLen);
                    var z = bitReader.ReadSigned(posLen);

                    Point3D vel = new(x, y, z);
                    var prevVel = pose.Velocity;
                    var coeff = (totalFrame - curVelFrame[boneNum]) / 256.0;
                    Point3D posDelta = new(prevVel.X * coeff, prevVel.Y * coeff, prevVel.Z * coeff);
                    pose.Position = new Point3D(pose.Position.X + posDelta.X, pose.Position.Y + posDelta.Y,
                        pose.Position.Z + posDelta.Z);
                    pose.FrameNum = totalFrame;
                    pose.Velocity = vel;

                    curPose[boneNum].Position = pose.Position;
                    curPose[boneNum].Velocity = pose.Velocity;
                    curVelFrame[boneNum] = totalFrame;
                }
                else
                {
                    // rot
                    var rotLen = bitReader.Read(4) + 1;
                    var a = bitReader.ReadSigned(rotLen);
                    var b = bitReader.ReadSigned(rotLen);
                    var c = bitReader.ReadSigned(rotLen);
                    var d = bitReader.ReadSigned(rotLen);

                    Quaternion angVel = new(b, c, d, a);

                    var prevAngVel = pose.AngularVelocity;
                    var coeff = (totalFrame - curAngVelFrame[boneNum]) / 131072.0;
                    Quaternion angDelta = new(prevAngVel.X * coeff, prevAngVel.Y * coeff,
                        prevAngVel.Z * coeff,
                        prevAngVel.W * coeff);
                    pose.Rotation = new Quaternion(pose.Rotation.X + angDelta.X, pose.Rotation.Y + angDelta.Y,
                        pose.Rotation.Z + angDelta.Z, pose.Rotation.W + angDelta.W);

                    pose.FrameNum = totalFrame;
                    pose.AngularVelocity = angVel;

                    curPose[boneNum].Rotation = pose.Rotation;
                    curPose[boneNum].AngularVelocity = pose.AngularVelocity;
                    curAngVelFrame[boneNum] = totalFrame;
                }
            }

            if (pose != null)
            {
                meshPoses.Add(pose);
            }
            
            var numFrames = offset4Val + 1;

            var animData = new AnimData(bindingPose, numBones, numFrames, offset4Val, offset14Val, offset18Val,
                skeletonDef, meshPoses);
            animData.BuildPerFramePoses();
            animData.BuildPerFrameFKPoses();
            return animData;
        }
    }
}
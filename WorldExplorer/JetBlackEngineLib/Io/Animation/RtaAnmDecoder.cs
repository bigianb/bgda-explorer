using JetBlackEngineLib.Data;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Io.Animation;

public class RtaAnmDecoder : AnmDecoder
{
    private static readonly EngineVersion[] StaticSupportedVersions =
    {
        EngineVersion.ReturnToArms, EngineVersion.JusticeLeagueHeroes
    };
    
    public override IReadOnlyList<EngineVersion> SupportedVersions => StaticSupportedVersions;
    
    public override AnimData Decode(ReadOnlySpan<byte> data)
    {
        var numBones = DataUtil.GetLeInt(data, 0);
        var maxFrames = DataUtil.GetLeInt(data, 4); // max frame
        var offset14Val = DataUtil.GetLeInt(data, 0x14);
        var offset18Val = DataUtil.GetLeInt(data, 0x18);
        var offset8Val = DataUtil.GetLeInt(data, 8);

        var bindingPoseOffset = DataUtil.GetLeInt(data, 0x0C);
        var bindingPose = new Point3D[numBones];
        for (var i = 0; i < numBones; ++i)
        {
            bindingPose[i] = new Point3D(
                -DataUtil.GetLeShort(data, bindingPoseOffset + i * 8 + 0) / 64.0,
                -DataUtil.GetLeShort(data, bindingPoseOffset + i * 8 + 2) / 64.0,
                -DataUtil.GetLeShort(data, bindingPoseOffset + i * 8 + 4) / 64.0
            );
        }

        // Skeleton structure
        var offset10Val = DataUtil.GetLeInt(data, 0x10);
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
            pose = new AnimMeshPose { BoneNum = boneNum, FrameNum = 0 };

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

        var frameNumber = 0;
        pose = null;
        while (bitReader.HasData(22) && frameNumber < maxFrames)
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

            frameNumber += count;

            if (pose == null || pose.FrameNum != frameNumber || pose.BoneNum != boneNum)
            {
                if (pose != null)
                {
                    meshPoses.Add(pose);
                }

                pose = new AnimMeshPose
                {
                    FrameNum = frameNumber,
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
                var coEff = (frameNumber - curVelFrame[boneNum]) / 256.0;
                Point3D posDelta = new(prevVel.X * coEff, prevVel.Y * coEff, prevVel.Z * coEff);
                pose.Position = new Point3D(pose.Position.X + posDelta.X, pose.Position.Y + posDelta.Y,
                    pose.Position.Z + posDelta.Z);
                pose.FrameNum = frameNumber;
                pose.Velocity = vel;

                curPose[boneNum].Position = pose.Position;
                curPose[boneNum].Velocity = pose.Velocity;
                curVelFrame[boneNum] = frameNumber;
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
                var coEff = (frameNumber - curAngVelFrame[boneNum]) / 131072.0;
                Quaternion angDelta = new(prevAngVel.X * coEff, prevAngVel.Y * coEff,
                    prevAngVel.Z * coEff,
                    prevAngVel.W * coEff);
                pose.Rotation = new Quaternion(pose.Rotation.X + angDelta.X, pose.Rotation.Y + angDelta.Y,
                    pose.Rotation.Z + angDelta.Z, pose.Rotation.W + angDelta.W);

                pose.FrameNum = frameNumber;
                pose.AngularVelocity = angVel;

                curPose[boneNum].Rotation = pose.Rotation;
                curPose[boneNum].AngularVelocity = pose.AngularVelocity;
                curAngVelFrame[boneNum] = frameNumber;
            }
        }

        if (pose != null)
        {
            meshPoses.Add(pose);
        }

        var numFrames = maxFrames + 1;

        var animData = new AnimData(bindingPose, numBones, numFrames, maxFrames, offset14Val, offset18Val,
            skeletonDef, meshPoses);
        return animData;
    }
}
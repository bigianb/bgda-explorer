/*  Copyright (C) 2012 Ian Brown

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataLoaders
{
    public class AnmDecoder
    {
        public static AnimData Decode(EngineVersion engineVersion, byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            AnimData animData = new AnimData();
            animData.NumBones = DataUtil.getLEInt(data, startOffset);
            animData.Offset4Val = DataUtil.getLEInt(data, startOffset + 4);
            animData.Offset14Val = DataUtil.getLEInt(data, startOffset + 0x14);
            animData.Offset18Val = DataUtil.getLEInt(data, startOffset + 0x18);
            int offset8Val = startOffset + DataUtil.getLEInt(data, startOffset + 8);

            int bindingPoseOffset = startOffset + DataUtil.getLEInt(data, startOffset + 0x0C);
            animData.bindingPose = new Point3D[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i)
            {
                animData.bindingPose[i] = new Point3D(
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 0) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 2) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 4) / 64.0
                );
            }

            if (engineVersion == EngineVersion.ReturnToArms)
            {
                var bitReader = new BitstreamReader(data, offset8Val, length - (offset8Val-startOffset));

                for (int i = 0; i < animData.NumBones; i++)
                {
                    int length1 = bitReader.Read(4) + 1;
                    int[] values3 = new int[3];
                    for (int o = 0; o < 3; o++)
                    {
                        values3[o] = bitReader.Read(length1);
                    }
                    int length2 = bitReader.Read(4) + 1;
                    int[] values4 = new int[4];
                    for (int o = 0; o < 4; o++)
                    {
                        values4[o] = bitReader.Read(length2);
                    }
                }
            }

            // Skeleton structure
            int offset10Val = startOffset + DataUtil.getLEInt(data, startOffset + 0x10);
            animData.skeletonDef = new int[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i)
            {
                animData.skeletonDef[i] = data[offset10Val + i];
            }

            AnimMeshPose[] curPose = new AnimMeshPose[animData.NumBones];

            AnimMeshPose pose = null;
            for (int boneNum = 0; boneNum < animData.NumBones; ++boneNum)
            {
                pose = new AnimMeshPose();
                pose.BoneNum = boneNum;
                pose.FrameNum = 0;
                int frameOff = offset8Val + boneNum * 0x0e;

                pose.Position = new Point3D(
                    DataUtil.getLEShort(data, frameOff) / 64.0,
                    DataUtil.getLEShort(data, frameOff + 2) / 64.0,
                    DataUtil.getLEShort(data, frameOff + 4) / 64.0);

                double a = DataUtil.getLEShort(data, frameOff + 6) / 4096.0;
                double b = DataUtil.getLEShort(data, frameOff + 8) / 4096.0;
                double c = DataUtil.getLEShort(data, frameOff + 0x0A) / 4096.0;
                double d = DataUtil.getLEShort(data, frameOff + 0x0C) / 4096.0;

                pose.Rotation = new Quaternion(b, c, d, a);

                pose.Velocity = new Point3D(0, 0, 0);
                pose.AngularVelocity = new Quaternion(0, 0, 0, 0);

                // This may give us duplicate frame zero poses, but that's ok.
                animData.MeshPoses.Add(pose);
                curPose[boneNum] = new AnimMeshPose(pose);
            }
            int[] curAngVelFrame = new int[animData.NumBones];
            int[] curVelFrame = new int[animData.NumBones];

            animData.NumFrames = 1;

            int totalFrame = 0;
            int otherOff = offset8Val + animData.NumBones * 0x0e;

            pose = null;
            while (otherOff < endIndex) {
                int count = data[otherOff++];
                byte byte2 = data[otherOff++];
                int boneNum = byte2 & 0x3f;
                if (boneNum == 0x3f) break;

                totalFrame += count;

                if (pose == null || pose.FrameNum != totalFrame || pose.BoneNum != boneNum)
                {
                    if (pose != null)
                    {
                        animData.MeshPoses.Add(pose);
                    }
                    pose = new AnimMeshPose();
                    pose.FrameNum = totalFrame;
                    pose.BoneNum = boneNum;
                    pose.Position = curPose[boneNum].Position;
                    pose.Rotation = curPose[boneNum].Rotation;
                    pose.AngularVelocity = curPose[boneNum].AngularVelocity;
                    pose.Velocity = curPose[boneNum].Velocity;
                }

                // bit 7 specifies whether to read 4 (set) or 3 elements following
                // bit 6 specifies whether they are shorts or bytes (set).
                if ((byte2 & 0x80) == 0x80) {
                    int a, b, c, d;
                    if ((byte2 & 0x40) == 0x40) {
                        a = (sbyte)data[otherOff++];
                        b = (sbyte)data[otherOff++];
                        c = (sbyte)data[otherOff++];
                        d = (sbyte)data[otherOff++];
                    } else {
                        a = DataUtil.getLEShort(data, otherOff);
                        b = DataUtil.getLEShort(data, otherOff+2);
                        c = DataUtil.getLEShort(data, otherOff+4);
                        d = DataUtil.getLEShort(data, otherOff+6);
                        otherOff += 8;
                    }
                    Quaternion angVel = new Quaternion(b, c, d, a);

                    Quaternion prevAngVel = pose.AngularVelocity;
                    double coeff = (totalFrame - curAngVelFrame[boneNum]) / 131072.0;
                    Quaternion angDelta = new Quaternion(prevAngVel.X * coeff, prevAngVel.Y * coeff, prevAngVel.Z * coeff, prevAngVel.W * coeff);
                    pose.Rotation = new Quaternion(pose.Rotation.X + angDelta.X, pose.Rotation.Y + angDelta.Y, pose.Rotation.Z + angDelta.Z, pose.Rotation.W + angDelta.W);
                    
                    pose.FrameNum = totalFrame;
                    pose.AngularVelocity = angVel;

                    curPose[boneNum].Rotation = pose.Rotation;
                    curPose[boneNum].AngularVelocity = pose.AngularVelocity;
                    curAngVelFrame[boneNum] = totalFrame;
                }
                else
                {
                    int x, y, z;
                    if ((byte2 & 0x40) == 0x40) {
                        x = (sbyte)data[otherOff++];
                        y = (sbyte)data[otherOff++];
                        z = (sbyte)data[otherOff++];
                    } else {
                        x = DataUtil.getLEShort(data, otherOff);
                        y = DataUtil.getLEShort(data, otherOff + 2);
                        z = DataUtil.getLEShort(data, otherOff + 4);
                        otherOff += 6;
                    }
                    Point3D vel = new Point3D(x, y, z);
                    Point3D prevVel = pose.Velocity;
                    double coeff = (totalFrame - curVelFrame[boneNum]) / 512.0;
                    Point3D posDelta = new Point3D(prevVel.X * coeff, prevVel.Y * coeff, prevVel.Z * coeff);
                    pose.Position = new Point3D(pose.Position.X + posDelta.X, pose.Position.Y + posDelta.Y, pose.Position.Z + posDelta.Z);                   
                    pose.FrameNum = totalFrame;
                    pose.Velocity = vel;

                    curPose[boneNum].Position = pose.Position;
                    curPose[boneNum].Velocity = pose.Velocity;
                    curVelFrame[boneNum] = totalFrame;
                }
            }
            animData.MeshPoses.Add(pose);
            animData.NumFrames = totalFrame+1;
            animData.BuildPerFramePoses();
            animData.BuildPerFrameFKPoses();
            return animData;
        }
    }

    public class AnimMeshPose
    {
        public Point3D Position;
        public Quaternion Rotation;

        public Quaternion AngularVelocity;
        public Point3D Velocity;

        public int BoneNum;
        public int FrameNum;

        public AnimMeshPose()
        { }

        public AnimMeshPose(AnimMeshPose copyFrom)
        {
            Position = copyFrom.Position;
            Rotation = copyFrom.Rotation;
            AngularVelocity = copyFrom.AngularVelocity;
            Velocity = copyFrom.Velocity;
            BoneNum = copyFrom.BoneNum;
            FrameNum = copyFrom.FrameNum;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AnimMeshPose: BoneNum=").Append(BoneNum);
            sb.Append(", FrameNum=").Append(FrameNum);
            sb.Append(", Pos=(").Append(Position.ToString());
            sb.Append(") Rot=(").Append(Rotation.ToString());
            if (Rotation.IsNormalized)
            {
                sb.Append("{Normalised}");
            }
            sb.Append(") Vel=(").Append(Velocity.ToString());
            sb.Append(") AngVel=(").Append(AngularVelocity.ToString());
            sb.Append(")");
           
            return sb.ToString();
        }
    }

    public class AnimData
    {
        public int NumBones;
        public int NumFrames;
        public int Offset4Val;
        public int Offset14Val;
        public int Offset18Val;     // These are 4 bytes which are all ored together

        public int[] skeletonDef;

        public Point3D[] bindingPose;

        public string Other;

        public List<AnimMeshPose> MeshPoses = new List<AnimMeshPose>();

        public AnimMeshPose[,] perFramePoses;

        // With forward kinematics applied
        public AnimMeshPose[,] perFrameFKPoses;

        public void BuildPerFrameFKPoses()
        {
            perFrameFKPoses = new AnimMeshPose[NumFrames, NumBones];
            Point3D[] parentPoints = new Point3D[64];
            Quaternion[] parentRotations = new Quaternion[64];
            parentPoints[0] = new Point3D(0, 0, 0);
            parentRotations[0] = new Quaternion(0, 0, 0, 1);
            for (int frame = 0; frame < NumFrames; ++frame) {
                for (int jointNum = 0; jointNum < skeletonDef.GetLength(0); ++jointNum) {

                    int parentIndex = skeletonDef[jointNum];
                    Point3D parentPos = parentPoints[parentIndex];
                    Quaternion parentRot = parentRotations[parentIndex];

                    // The world position of the child joint is the local position of the child joint rotated by the
                    // world rotation of the parent and then offset by the world position of the parent.
                    AnimMeshPose pose = perFramePoses[frame, jointNum];

                    Matrix3D m = Matrix3D.Identity;
                    m.Rotate(parentRot);
                    var thisPos = m.Transform(pose.Position);
                    thisPos.Offset(parentPos.X, parentPos.Y, parentPos.Z);

                    // The world rotation of the child joint is the world rotation of the parent rotated by the local rotation of the child.
                    var poseRot = pose.Rotation;
                    poseRot.Normalize();
                    Quaternion thisRot = Quaternion.Multiply(parentRot, poseRot);
                    thisRot.Normalize();
                    
                    AnimMeshPose fkPose = new AnimMeshPose();
                    fkPose.Position = thisPos;
                    fkPose.Rotation = thisRot;
                    perFrameFKPoses[frame, jointNum] = fkPose;

                    parentPoints[parentIndex + 1] = fkPose.Position;
                    parentRotations[parentIndex + 1] = fkPose.Rotation;
                }
            }
        }

        public void BuildPerFramePoses()
        {
            perFramePoses = new AnimMeshPose[NumFrames, NumBones];
            foreach (AnimMeshPose pose in MeshPoses)
            {
                if (pose != null)
                {
                    perFramePoses[pose.FrameNum, pose.BoneNum] = pose;
                }
            }
            for (int bone = 0; bone < NumBones; ++bone)
            {
                AnimMeshPose prevPose = null;
                for (int frame = 0; frame < NumFrames; ++frame)
                {
                    if (perFramePoses[frame, bone] == null)
                    {
                        int frameDiff = frame - prevPose.FrameNum;
                        double avCoeff = frameDiff / 131072.0;
                        Quaternion rotDelta = new Quaternion(prevPose.AngularVelocity.X * avCoeff, prevPose.AngularVelocity.Y * avCoeff, prevPose.AngularVelocity.Z * avCoeff, prevPose.AngularVelocity.W * avCoeff);

                        double velCoeff = frameDiff / 512.0;
                        Point3D posDelta = new Point3D(prevPose.Velocity.X * velCoeff, prevPose.Velocity.Y * velCoeff, prevPose.Velocity.Z * velCoeff);

                        AnimMeshPose pose = new AnimMeshPose();
                        pose.BoneNum = bone;
                        pose.FrameNum = frame;
                        pose.Position = new Point3D(prevPose.Position.X + posDelta.X, prevPose.Position.Y + posDelta.Y, prevPose.Position.Z + posDelta.Z);
                        pose.Rotation = new Quaternion(prevPose.Rotation.X + rotDelta.X, prevPose.Rotation.Y + rotDelta.Y, prevPose.Rotation.Z + rotDelta.Z, prevPose.Rotation.W + rotDelta.W);
                        pose.AngularVelocity = prevPose.AngularVelocity;
                        pose.Velocity = prevPose.Velocity;
                        perFramePoses[frame, bone] = pose;
                    }
                    prevPose = perFramePoses[frame, bone];
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Num Bones = ").Append(NumBones).Append("\n");
            sb.Append("Num Frames = ").Append(NumFrames).Append("\n");
            sb.Append("Offset 4 val = ").Append(Offset4Val).Append("\n");
            sb.Append("Offset 0x14 val = ").Append(Offset14Val).Append("\n");
            sb.Append("Offset 0x18 val = ").Append(Offset18Val).Append("\n");

            sb.Append("skeleton def = ");
            for (int b=0; b<NumBones; ++b){
                if (b != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(skeletonDef[b]);
            }
            sb.Append("\n");

            sb.Append("Joint positions:\n");
            for (int b = 0; b < NumBones; ++b)
            {
                sb.Append("Joint: ").Append(b).Append(" ... ");
                sb.Append(bindingPose[b].ToString()).Append("\n");
            }

            foreach (var pose in MeshPoses)
            {
                if (pose != null)
                {
                    sb.Append(pose.ToString()).Append("\n");
                }
            }
            if (Other != null)
            {
                sb.Append(Other);
            }
            return sb.ToString();
        }
    }

}

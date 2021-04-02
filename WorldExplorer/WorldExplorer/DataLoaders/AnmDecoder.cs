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
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders
{
    public class AnmDecoder
    {
        public static AnimData Decode(EngineVersion engineVersion, byte[] data, int startOffset, int length)
        {
            if (engineVersion == EngineVersion.ReturnToArms) {
                return DecodeCRTA(engineVersion, data, startOffset, length);
            } else {
                return DecodeBGDA(engineVersion, data, startOffset, length);
            }
        }

        public static AnimData DecodeCRTA(EngineVersion engineVersion, byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            AnimData animData = new AnimData
            {
                NumBones = DataUtil.getLEInt(data, startOffset),
                Offset4Val = DataUtil.getLEInt(data, startOffset + 4),     // max frame
                Offset14Val = DataUtil.getLEInt(data, startOffset + 0x14),
                Offset18Val = DataUtil.getLEInt(data, startOffset + 0x18)
            };
            int offset8Val = DataUtil.getLEInt(data, startOffset + 8);

            int bindingPoseOffset = startOffset + DataUtil.getLEInt(data, startOffset + 0x0C);
            animData.bindingPose = new Point3D[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i) {
                animData.bindingPose[i] = new Point3D(
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 0) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 2) / 64.0,
                    -DataUtil.getLEShort(data, bindingPoseOffset + i * 8 + 4) / 64.0
                );
            }           

            // Skeleton structure
            int offset10Val = startOffset + DataUtil.getLEInt(data, startOffset + 0x10);
            animData.skeletonDef = new int[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i) {
                animData.skeletonDef[i] = data[offset10Val + i];
            }

            AnimMeshPose[] curPose = new AnimMeshPose[animData.NumBones];

            AnimMeshPose pose = null;
            var bitReader = new BitstreamReader(data, startOffset + offset8Val, length - offset8Val);
            for (int boneNum = 0; boneNum < animData.NumBones; ++boneNum) {
                pose = new AnimMeshPose
                {
                    BoneNum = boneNum,
                    FrameNum = 0
                };

                int posLen = bitReader.Read(4) + 1;
                pose.Position = new Point3D(
                    bitReader.ReadSigned(posLen) / 64.0,
                    bitReader.ReadSigned(posLen) / 64.0,
                    bitReader.ReadSigned(posLen) / 64.0);

                int rotLen = bitReader.Read(4) + 1;
                double a = bitReader.ReadSigned(rotLen) / 4096.0;
                double b = bitReader.ReadSigned(rotLen) / 4096.0;
                double c = bitReader.ReadSigned(rotLen) / 4096.0;
                double d = bitReader.ReadSigned(rotLen) / 4096.0;

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

            pose = null;
            while (bitReader.HasData(22) && totalFrame < animData.Offset4Val) {
                int count = bitReader.Read(8);
                if (count == 0xFF)
                {
                    break;
                }
                int flag = bitReader.Read(1);
                int boneNum = bitReader.Read(6);
                
                if (boneNum >= animData.NumBones)
                {
                    break;
                }

                totalFrame += count;
         
                if (pose == null || pose.FrameNum != totalFrame || pose.BoneNum != boneNum) {
                    if (pose != null) {
                        animData.MeshPoses.Add(pose);
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
                if (flag == 1) {
                    // xyz
                    int posLen = bitReader.Read(4) + 1;
                    
                    int x = bitReader.ReadSigned(posLen);
                    int y = bitReader.ReadSigned(posLen);
                    int z = bitReader.ReadSigned(posLen);

                    Point3D vel = new Point3D(x, y, z);
                    Point3D prevVel = pose.Velocity;
                    double coeff = (totalFrame - curVelFrame[boneNum]) / 256.0;
                    Point3D posDelta = new Point3D(prevVel.X * coeff, prevVel.Y * coeff, prevVel.Z * coeff);
                    pose.Position = new Point3D(pose.Position.X + posDelta.X, pose.Position.Y + posDelta.Y, pose.Position.Z + posDelta.Z);
                    pose.FrameNum = totalFrame;
                    pose.Velocity = vel;

                    curPose[boneNum].Position = pose.Position;
                    curPose[boneNum].Velocity = pose.Velocity;
                    curVelFrame[boneNum] = totalFrame;
                } else {
                    // rot
                    int rotLen = bitReader.Read(4) + 1;
                    int a = bitReader.ReadSigned(rotLen);
                    int b = bitReader.ReadSigned(rotLen);
                    int c = bitReader.ReadSigned(rotLen);
                    int d = bitReader.ReadSigned(rotLen);

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
                
            }
            animData.MeshPoses.Add(pose);
            animData.NumFrames = animData.Offset4Val+1; // totalFrame + 1;
            animData.BuildPerFramePoses();
            animData.BuildPerFrameFKPoses();
            return animData;
        }

        public static AnimData DecodeBGDA(EngineVersion engineVersion, byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            AnimData animData = new AnimData
            {
                NumBones = DataUtil.getLEInt(data, startOffset),
                Offset4Val = DataUtil.getLEInt(data, startOffset + 4),
                Offset14Val = DataUtil.getLEInt(data, startOffset + 0x14),
                Offset18Val = DataUtil.getLEInt(data, startOffset + 0x18)
            };
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
                pose = new AnimMeshPose
                {
                    BoneNum = boneNum,
                    FrameNum = 0
                };
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

}

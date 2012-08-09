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
        public static AnimData Decode(byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            AnimData animData = new AnimData();
            animData.NumBones = DataUtil.getLEInt(data, startOffset);
            animData.Offset4Val = DataUtil.getLEInt(data, startOffset + 4);
            animData.Offset14Val = DataUtil.getLEInt(data, startOffset + 0x14);
            animData.Offset18Val = DataUtil.getLEInt(data, startOffset + 0x18);
            int offset8Val = startOffset + DataUtil.getLEInt(data, startOffset + 8);

            // 4 shorts per bone
            int offset0CVal = startOffset + DataUtil.getLEInt(data, startOffset + 0x0C);
            animData.boneOffsets = new Vector3D[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i)
            {
                animData.boneOffsets[i] = new Vector3D(
                    DataUtil.getLEShort(data, offset0CVal + i * 8) / 64.0,
                    DataUtil.getLEShort(data, offset0CVal + i * 8 + 2) / 64.0,
                    DataUtil.getLEShort(data, offset0CVal + i * 8 + 4) / 64.0
                );
            }

            // Mapping table, one byte per meshGroup
            int offset10Val = startOffset + DataUtil.getLEInt(data, startOffset + 0x10);
            animData.boneToMeshMapping = new int[animData.NumBones];
            for (int i = 0; i < animData.NumBones; ++i)
            {
                animData.boneToMeshMapping[i] = data[offset10Val + i];
            }

            AnimMeshPose[] previousPoses = new AnimMeshPose[animData.NumBones];

            AnimMeshPose pose = new AnimMeshPose();
            for (int meshNum = 0; meshNum < animData.NumBones; ++meshNum)
            {
                pose = new AnimMeshPose();
                pose.MeshNum = meshNum;
                pose.FrameNum = 0;
                int frameOff = offset8Val + meshNum * 0x0e;

                pose.Position = new Vector3D(
                    DataUtil.getLEShort(data, frameOff) / 64.0,
                    DataUtil.getLEShort(data, frameOff + 2) / 64.0,
                    DataUtil.getLEShort(data, frameOff + 4) / 64.0);

                pose.Rotation = new Quaternion(
                    DataUtil.getLEShort(data, frameOff + 6) / 4096.0,
                    DataUtil.getLEShort(data, frameOff + 8) / 4096.0,
                    DataUtil.getLEShort(data, frameOff + 0xA) / 4096.0,
                    DataUtil.getLEShort(data, frameOff + 0xC) / 4096.0);

                previousPoses[meshNum] = pose;
                animData.MeshPoses.Add(pose);
                pose = null;
            }
            int totalFrame = 1;
            int otherOff = offset8Val + animData.NumBones * 0x0e;
 
            while (otherOff < endIndex) {
                int count = data[otherOff++];
                byte byte2 = data[otherOff++];
                int meshNum = byte2 & 0x3f;
                if (meshNum == 0x3f) break;

                totalFrame += count;

                if (pose == null || pose.FrameNum != totalFrame || pose.MeshNum != meshNum)
                {
                    if (pose != null)
                    {
                        animData.MeshPoses.Add(pose);
                    }
                    pose = new AnimMeshPose();
                    pose.FrameNum = totalFrame;
                    pose.MeshNum = meshNum;
                    pose.Position = previousPoses[meshNum].Position;
                    pose.Rotation = previousPoses[meshNum].Rotation;
                }

                // bit 7 specifies whether to read 4 (set) or 3 elements following
                // bit 6 specifies whether they are shorts or bytes (set).
                if ((byte2 & 0x80) == 0x80) {
                    int a, b, c, d;
                    if ((byte2 & 0x40) == 0x40) {
                        a = data[otherOff++];
                        b = data[otherOff++];
                        c = data[otherOff++];
                        d = data[otherOff++];
                    } else {
                        a = DataUtil.getLEShort(data, otherOff);
                        b = DataUtil.getLEShort(data, otherOff+2);
                        c = DataUtil.getLEShort(data, otherOff+4);
                        d = DataUtil.getLEShort(data, otherOff+6);
                        otherOff += 8;
                    }
                    pose.Rotation = new Quaternion(a / 131072.0, b / 131072.0, c / 131072.0, d / 131072.0);
                } else {
                    int x, y, z;
                    if ((byte2 & 0x40) == 0x40) {
                        x = data[otherOff++];
                        y = data[otherOff++];
                        z = data[otherOff++];
                    } else {
                        x = DataUtil.getLEShort(data, otherOff);
                        y = DataUtil.getLEShort(data, otherOff + 2);
                        z = DataUtil.getLEShort(data, otherOff + 4);
                        otherOff += 6;
                    }
                    pose.Position = new Vector3D(x / 512.0, y / 512.0, z / 512.0);
                }
            }
            animData.MeshPoses.Add(pose);
            animData.NumFrames = totalFrame+1;
            animData.BuildPerFramePoses();
            return animData;
        }
    }

    public class AnimMeshPose
    {
        public Vector3D Position;
        public Quaternion Rotation;
        public int MeshNum;
        public int FrameNum;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AnimMeshPose: MeshNum=").Append(MeshNum);
            sb.Append(", FrameNum=").Append(FrameNum);
            sb.Append(", Pos=(").Append(Position.ToString()).Append(") Rot=(");
            sb.Append(Rotation.ToString()).Append(")");
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

        public int[] boneToMeshMapping;

        public Vector3D[] boneOffsets;

        public string Other;

        public List<AnimMeshPose> MeshPoses = new List<AnimMeshPose>();

        public AnimMeshPose[,] perFramePoses;

        public void BuildPerFramePoses()
        {
            perFramePoses = new AnimMeshPose[NumFrames, NumBones];
            foreach (AnimMeshPose pose in MeshPoses)
            {
                perFramePoses[pose.FrameNum, pose.MeshNum] = pose;
            }
            for (int mesh = 0; mesh < NumBones; ++mesh)
            {
                AnimMeshPose prevPose = null;
                for (int frame = 0; frame < NumFrames; ++frame)
                {
                    if (perFramePoses[frame, mesh] == null)
                    {
                        // TODO: Interpolate between previous frame values and here.
                        AnimMeshPose pose = new AnimMeshPose();
                        pose.MeshNum = mesh;
                        pose.FrameNum = frame;
                        pose.Position = prevPose.Position;
                        pose.Rotation = prevPose.Rotation;
                        perFramePoses[frame, mesh] = pose;
                    }
                    prevPose = perFramePoses[frame, mesh];
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

            sb.Append("bone mappings = ");
            for (int b=0; b<NumBones; ++b){
                if (b != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(boneToMeshMapping[b]);
            }
            sb.Append("\n");

            sb.Append("bone Offsets:\n");
            for (int b = 0; b < NumBones; ++b)
            {
                sb.Append("Bone: ").Append(b).Append(" ... ");
                sb.Append(boneOffsets[b].ToString()).Append("\n");
            }

            foreach (var pose in MeshPoses)
            {
                sb.Append(pose.ToString()).Append("\n");
            }
            if (Other != null)
            {
                sb.Append(Other);
            }
            return sb.ToString();
        }
    }

}

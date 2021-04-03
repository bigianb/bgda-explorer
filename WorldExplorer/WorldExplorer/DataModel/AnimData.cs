/*  Copyright (C) 2012-2018 Ian Brown

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

using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    public class AnimMeshPose
    {
        public Point3D Position;
        public Quaternion Rotation;

        public Quaternion AngularVelocity;
        public Point3D Velocity;

        public int BoneNum;
        public int FrameNum;

        public AnimMeshPose()
        {
        }

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
            var sb = new StringBuilder();
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
            var parentPoints = new Point3D[64];
            var parentRotations = new Quaternion[64];
            parentPoints[0] = new Point3D(0, 0, 0);
            parentRotations[0] = new Quaternion(0, 0, 0, 1);
            for (var frame = 0; frame < NumFrames; ++frame)
            {
                for (var jointNum = 0; jointNum < skeletonDef.GetLength(0); ++jointNum)
                {

                    var parentIndex = skeletonDef[jointNum];
                    var parentPos = parentPoints[parentIndex];
                    var parentRot = parentRotations[parentIndex];

                    // The world position of the child joint is the local position of the child joint rotated by the
                    // world rotation of the parent and then offset by the world position of the parent.
                    var pose = perFramePoses[frame, jointNum];

                    var m = Matrix3D.Identity;
                    m.Rotate(parentRot);
                    var thisPos = m.Transform(pose.Position);
                    thisPos.Offset(parentPos.X, parentPos.Y, parentPos.Z);

                    // The world rotation of the child joint is the world rotation of the parent rotated by the local rotation of the child.
                    var poseRot = pose.Rotation;
                    poseRot.Normalize();
                    var thisRot = Quaternion.Multiply(parentRot, poseRot);
                    thisRot.Normalize();

                    var fkPose = new AnimMeshPose
                    {
                        Position = thisPos,
                        Rotation = thisRot
                    };
                    perFrameFKPoses[frame, jointNum] = fkPose;

                    parentPoints[parentIndex + 1] = fkPose.Position;
                    parentRotations[parentIndex + 1] = fkPose.Rotation;
                }
            }
        }

        public void BuildPerFramePoses()
        {
            perFramePoses = new AnimMeshPose[NumFrames, NumBones];
            foreach (var pose in MeshPoses)
            {
                if (pose != null && pose.FrameNum <= NumFrames)
                {
                    perFramePoses[pose.FrameNum, pose.BoneNum] = pose;
                }
            }
            for (var bone = 0; bone < NumBones; ++bone)
            {
                AnimMeshPose prevPose = null;
                for (var frame = 0; frame < NumFrames; ++frame)
                {
                    if (perFramePoses[frame, bone] == null)
                    {
                        var frameDiff = frame - prevPose.FrameNum;
                        var avCoeff = frameDiff / 131072.0;
                        var rotDelta = new Quaternion(prevPose.AngularVelocity.X * avCoeff, prevPose.AngularVelocity.Y * avCoeff, prevPose.AngularVelocity.Z * avCoeff, prevPose.AngularVelocity.W * avCoeff);

                        var velCoeff = frameDiff / 512.0;
                        var posDelta = new Point3D(prevPose.Velocity.X * velCoeff, prevPose.Velocity.Y * velCoeff, prevPose.Velocity.Z * velCoeff);

                        var pose = new AnimMeshPose
                        {
                            BoneNum = bone,
                            FrameNum = frame,
                            Position = new Point3D(prevPose.Position.X + posDelta.X, prevPose.Position.Y + posDelta.Y, prevPose.Position.Z + posDelta.Z),
                            Rotation = new Quaternion(prevPose.Rotation.X + rotDelta.X, prevPose.Rotation.Y + rotDelta.Y, prevPose.Rotation.Z + rotDelta.Z, prevPose.Rotation.W + rotDelta.W),
                            AngularVelocity = prevPose.AngularVelocity,
                            Velocity = prevPose.Velocity
                        };
                        perFramePoses[frame, bone] = pose;
                    }
                    prevPose = perFramePoses[frame, bone];
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Num Bones = ").Append(NumBones).Append("\n");
            sb.Append("Num Frames = ").Append(NumFrames).Append("\n");
            sb.Append("Offset 4 val = ").Append(Offset4Val).Append("\n");
            sb.Append("Offset 0x14 val = ").Append(Offset14Val).Append("\n");
            sb.Append("Offset 0x18 val = ").Append(Offset18Val).Append("\n");

            sb.Append("skeleton def = ");
            for (var b = 0; b < NumBones; ++b)
            {
                if (b != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(skeletonDef[b]);
            }
            sb.Append("\n");

            sb.Append("Joint positions:\n");
            for (var b = 0; b < NumBones; ++b)
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


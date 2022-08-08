using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Data.Animation;

public class AnimData
{
    public readonly Point3D[] BindingPose;

    private readonly List<AnimMeshPose?> MeshPoses;
    public readonly int NumBones;
    public readonly int NumFrames;
    private readonly int Offset14Val;
    private readonly int Offset18Val; // These are 4 bytes which are all OR-ed together
    private readonly int Offset4Val;

    // With forward kinematics applied
    public AnimMeshPose[,]? PerFrameFkPoses;

    public AnimMeshPose?[,]? PerFramePoses;

    public readonly int[] SkeletonDef;

    public AnimData(Point3D[] bindingPose, int numBones, int numFrames, int offset4Val, int offset14Val,
        int offset18Val, int[] skeletonDef, List<AnimMeshPose?> meshPoses)
    {
        BindingPose = bindingPose;
        NumBones = numBones;
        NumFrames = numFrames;
        Offset4Val = offset4Val;
        Offset14Val = offset14Val;
        Offset18Val = offset18Val;
        SkeletonDef = skeletonDef;
        MeshPoses = meshPoses;
        
        BuildPerFramePoses();
        BuildPerFrameFkPoses();
    }

    [MemberNotNull(nameof(PerFrameFkPoses))]
    private void BuildPerFrameFkPoses()
    {
        if (PerFramePoses == null)
            throw new InvalidOperationException("Can't build FK poses until frame poses have been built.");
        var pendingPerFrameFkPoses = new AnimMeshPose[NumFrames, NumBones];
        var parentPoints = new Point3D[64];
        var parentRotations = new Quaternion[64];
        parentPoints[0] = new Point3D(0, 0, 0);
        parentRotations[0] = new Quaternion(0, 0, 0, 1);
        for (var frame = 0; frame < NumFrames; ++frame)
        {
            for (var jointNum = 0; jointNum < SkeletonDef.GetLength(0); ++jointNum)
            {
                var parentIndex = SkeletonDef[jointNum];
                var parentPos = parentPoints[parentIndex];
                var parentRot = parentRotations[parentIndex];

                // The world position of the child joint is the local position of the child joint rotated by the
                // world rotation of the parent and then offset by the world position of the parent.
                var pose = PerFramePoses[frame, jointNum];

                var m = Matrix3D.Identity;
                m.Rotate(parentRot);
                var thisPos = m.Transform(pose?.Position ?? new Point3D(0, 0, 0));
                thisPos.Offset(parentPos.X, parentPos.Y, parentPos.Z);

                // The world rotation of the child joint is the world rotation of the parent rotated by the local rotation of the child.
                var poseRot = pose?.Rotation ?? Quaternion.Identity;
                poseRot.Normalize();
                var thisRot = Quaternion.Multiply(parentRot, poseRot);
                thisRot.Normalize();

                AnimMeshPose fkPose = new() {Position = thisPos, Rotation = thisRot};
                pendingPerFrameFkPoses[frame, jointNum] = fkPose;

                parentPoints[parentIndex + 1] = fkPose.Position;
                parentRotations[parentIndex + 1] = fkPose.Rotation;
            }
        }

        PerFrameFkPoses = pendingPerFrameFkPoses;
    }

    [MemberNotNull(nameof(PerFramePoses))]
    private void BuildPerFramePoses()
    {
        var pendingFramePoses = new AnimMeshPose?[NumFrames, NumBones];
        foreach (var pose in MeshPoses)
        {
            if (pose != null && pose.FrameNum <= NumFrames)
            {
                pendingFramePoses[pose.FrameNum, pose.BoneNum] = pose;
            }
        }

        for (var bone = 0; bone < NumBones; ++bone)
        {
            AnimMeshPose? prevPose = null;
            for (var frame = 0; frame < NumFrames; ++frame)
            {
                if (pendingFramePoses[frame, bone] == null && prevPose != null)
                {
                    var frameDiff = frame - prevPose.FrameNum;
                    var avCoEff = frameDiff / 131072.0;
                    Quaternion rotDelta = new(prevPose.AngularVelocity.X * avCoEff,
                        prevPose.AngularVelocity.Y * avCoEff, prevPose.AngularVelocity.Z * avCoEff,
                        prevPose.AngularVelocity.W * avCoEff);

                    var velCoEff = frameDiff / 512.0;
                    Point3D posDelta = new(prevPose.Velocity.X * velCoEff, prevPose.Velocity.Y * velCoEff,
                        prevPose.Velocity.Z * velCoEff);

                    AnimMeshPose pose = new()
                    {
                        BoneNum = bone,
                        FrameNum = frame,
                        Position = new Point3D(prevPose.Position.X + posDelta.X, prevPose.Position.Y + posDelta.Y,
                            prevPose.Position.Z + posDelta.Z),
                        Rotation = new Quaternion(prevPose.Rotation.X + rotDelta.X,
                            prevPose.Rotation.Y + rotDelta.Y, prevPose.Rotation.Z + rotDelta.Z,
                            prevPose.Rotation.W + rotDelta.W),
                        AngularVelocity = prevPose.AngularVelocity,
                        Velocity = prevPose.Velocity
                    };
                    pendingFramePoses[frame, bone] = pose;
                }

                prevPose = pendingFramePoses[frame, bone];
            }
        }

        PerFramePoses = pendingFramePoses;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("Num Bones = ").Append(NumBones).Append('\n');
        sb.Append("Num Frames = ").Append(NumFrames).Append('\n');
        sb.Append("Offset 4 val = ").Append(Offset4Val).Append('\n');
        sb.Append("Offset 0x14 val = ").Append(Offset14Val).Append('\n');
        sb.Append("Offset 0x18 val = ").Append(Offset18Val).Append('\n');

        sb.Append("skeleton def = ");
        for (var b = 0; b < NumBones; ++b)
        {
            if (b != 0)
            {
                sb.Append(", ");
            }

            sb.Append(SkeletonDef[b]);
        }

        sb.Append('\n');

        sb.Append("Joint positions:\n");
        for (var b = 0; b < NumBones; ++b)
        {
            sb.Append("Joint: ").Append(b).Append(" ... ");
            sb.Append(BindingPose[b].ToString()).Append('\n');
        }

        foreach (var pose in MeshPoses)
        {
            if (pose != null)
            {
                sb.Append(pose).Append('\n');
            }
        }
            
        return sb.ToString();
    }
}
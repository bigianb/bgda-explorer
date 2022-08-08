using System.Text;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Data;

public class AnimMeshPose
{
    public Quaternion AngularVelocity;

    public int BoneNum;
    public int FrameNum;
    public Point3D Position;
    public Quaternion Rotation;
    public Point3D Velocity;

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
        StringBuilder sb = new();
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
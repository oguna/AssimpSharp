using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    public struct VectorKey
    {
        public double Time;
        public Vector3 Value;
        public VectorKey(double time, Vector3 value)
        {
            this.Time = time;
            this.Value = value;
        }
    }
    public struct QuatKey
    {
        public double Time;
        public Quaternion Value;
        public QuatKey(double time, Quaternion value)
        {
            this.Time = time;
            this.Value = value;
        }
    }
    public struct MeshKey
    {
        public double Time;
        public int Value;
        public MeshKey(double time, int value)
        {
            this.Time = time;
            this.Value = value;
        }
    }
    public enum AnimBehaviour
    {
        Default = 0x0,
        Constant = 0x1,
        Linear = 0x2,
        Repeat = 0x3
    }

    public class NodeAnim
    {
        public string NodeName;
        public VectorKey[] PositionKeys;
        public QuatKey[] RotationKeys;
        public VectorKey[] ScalingKeys;
        public AnimBehaviour PreState;
        public AnimBehaviour PostState;
    }

    public struct MeshAnim
    {
        public string Name;
        public MeshKey[] Keys;
    }

    public class Animation
    {
        public string Name;
        public double Duration = -1;
        public double TicksPreSecond;
        public NodeAnim[] Channels;
        public MeshAnim[] MeshChannels;
    }
}

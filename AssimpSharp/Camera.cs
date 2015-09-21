using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    public class Camera
    {
        public string Name;
        public Vector3 Position;
        public Vector3 Up;
        public Vector3 LookAt;
        public float HorizontalFOV;
        public float ClipPlaneNear;
        public float ClipPlaneFar;
        public float Aspect;
        public Camera()
        {
            this.Up = new Vector3(0, 1, 0);
            this.LookAt = new Vector3(0, 0, 1);
            this.HorizontalFOV = MathUtil.PiOverFour;
            this.ClipPlaneNear = 0.1f;
            this.ClipPlaneFar = 1000f;
            this.Aspect = 0f;
        }
        public Matrix CameraMatrix
        {
            get
            {
                var proj = Matrix.PerspectiveFovRH(HorizontalFOV, Aspect, ClipPlaneNear, ClipPlaneFar);
                var view = Matrix.LookAtRH(Position, LookAt, Up);
                return proj * view;
            }
        }
    }
}

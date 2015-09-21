using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    public enum SceneFlags :uint
    {
        Incomplete = 0x1,
        Validated = 0x2,
        ValidationWarning = 0x4,
        NonVerboseFormat = 0x8,
        Terrain = 0x10
    }

    public class Scene
    {
        public SceneFlags Flags;
        public Node RootNode;
        public readonly List<Mesh> Meshes = new List<Mesh>();
        public readonly List<Material> Materials = new List<Material>();
        public readonly List<Animation> Animations = new List<Animation>();
        public readonly List<Texture> Textures = new List<Texture>();
        public readonly List<Light> Lights = new List<Light>();
        public readonly List<Camera> Cameras = new List<Camera>();
        public object Private;
    }
}

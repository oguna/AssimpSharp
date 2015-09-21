using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;

namespace AssimpView
{
    class FbxMesh
    {
        private BasicEffect basicEffect;
        private Buffer<VertexPositionNormalTexture> vertices;
        private Buffer<int> indices;
        private VertexInputLayout inputLayout;
        private GraphicsDevice GraphicsDevice;
        private string path;
        private BoundingSphere boundingSphere;
        private BoundingBox boundingBox;

        public FbxMesh(AssimpSharp.Mesh mesh, AssimpSharp.Material mat, GraphicsDevice device, string path)
        {
            this.GraphicsDevice = device;
            this.path = path;
            LoadMesh(mesh);
            LoadMaterial(mat);
        }
        public FbxMesh(GraphicsDevice device)
        {
            this.GraphicsDevice = device;
            LoadCube();
        }

        public BoundingSphere BoundingSphere
        {
            get
            { 
                return BoundingSphere;
            }
        }

        public BoundingBox BoundingBox
        { 
            get
            {
                return boundingBox;
            }
        }

        public void LoadMaterial(AssimpSharp.Material material)
        {
            // Creates a basic effect
            basicEffect = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = false,
                View = Matrix.LookAtRH(new Vector3(0, 0, 500), new Vector3(0, 0, 0), Vector3.UnitY),
                Projection = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f),
                World = Matrix.Identity,
                PreferPerPixelLighting = true
            };
            if (material.TextureDiffuse != null)
            {
                basicEffect.Texture = Texture2D.Load(GraphicsDevice, Path.Combine(Path.GetDirectoryName(path), material.TextureDiffuse.TextureBase));
                basicEffect.TextureEnabled = true;
            }
            if (material.ColorDiffuse.HasValue)
            {
                basicEffect.DiffuseColor = material.ColorDiffuse.Value.ToVector4();
            }
        }

        public void LoadMesh(AssimpSharp.Mesh mesh)
        {
            var vertexSource = new VertexPositionNormalTexture[mesh.Vertices.Length];
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                vertexSource[i].Position = mesh.Vertices[i];
                vertexSource[i].Normal = mesh.Normals[i];
                vertexSource[i].TextureCoordinate = Vector2.Zero;
            }
            boundingSphere = BoundingSphere.FromPoints(mesh.Vertices);
            boundingBox = BoundingBox.FromPoints(mesh.Vertices);
            if (mesh.HasTextureCoords(0))
            {
                var channel = mesh.TextureCoords[0];
                for(int i=0; i<channel.Length; i++)
                {
                    vertexSource[i].TextureCoordinate.X = channel[i].X;
                    vertexSource[i].TextureCoordinate.Y = 1 - channel[i].Y;
                }
            }
            vertices = Buffer.Vertex.New(GraphicsDevice, vertexSource);

            var indexSource = new List<int>();
            for(int i=0; i<mesh.Faces.Length; i++)
            {
                var face = mesh.Faces[i];
                if (face.Indices.Length == 3)
                {
                    indexSource.AddRange(face.Indices.Reverse());
                }
                else if (face.Indices.Length == 4)
                {
                    indexSource.Add(face.Indices[2]);
                    indexSource.Add(face.Indices[1]);
                    indexSource.Add(face.Indices[0]);
                    indexSource.Add(face.Indices[0]);
                    indexSource.Add(face.Indices[3]);
                    indexSource.Add(face.Indices[2]);
                }
                else if (face.Indices.Length == 5)
                {
                    indexSource.Add(face.Indices[2]);
                    indexSource.Add(face.Indices[1]);
                    indexSource.Add(face.Indices[0]);
                    indexSource.Add(face.Indices[0]);
                    indexSource.Add(face.Indices[3]);
                    indexSource.Add(face.Indices[2]);
                    indexSource.Add(face.Indices[0]);
                    indexSource.Add(face.Indices[4]);
                    indexSource.Add(face.Indices[3]);
                }
                else
                {
                    throw (new Exception("invalid vertex count of polygon"));
                }
            }
            indices = Buffer.Index.New(GraphicsDevice, indexSource.ToArray());

            inputLayout = VertexInputLayout.FromBuffer(0, vertices);

        }

        public void LoadCube()
        {
            //// Creates a basic effect
            //basicEffect = new BasicEffect(GraphicsDevice)
            //{
            //    VertexColorEnabled = true,
            //    View = Matrix.LookAtRH(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY),
            //    Projection = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f),
            //    World = Matrix.Identity
            //};

            //// Creates vertices for the cube
            //vertices = Buffer.Vertex.New(
            //    GraphicsDevice,
            //    new[]
            //        {
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.Orange), // Back
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, 1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, 1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, 1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, 1.0f), Color.Orange),

            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, -1.0f), Color.Orange), // Front
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, -1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, -1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, -1.0f), Color.Orange),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.Orange),

            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, 1.0f), Color.OrangeRed), // Top
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, -1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, 1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, 1.0f), Color.OrangeRed),

            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.OrangeRed), // Bottom
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, -1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, -1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, 1.0f), Color.OrangeRed),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, -1.0f), Color.OrangeRed),

            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.DarkOrange), // Left
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, -1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, -1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(-1.0f, -1.0f, 1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, -1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(-1.0f, 1.0f, 1.0f), Color.DarkOrange),

            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, 1.0f), Color.DarkOrange), // Right
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, -1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(1.0f, -1.0f, 1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, 1.0f), Color.DarkOrange),
            //            new VertexPositionColor(new Vector3(1.0f, 1.0f, -1.0f), Color.DarkOrange),
            //        });
            //// Create an input layout from the vertices
            //inputLayout = VertexInputLayout.FromBuffer(0, vertices);
        }

        public void Draw(GameTime gameTime, Matrix transform)
        {
            // Rotate the cube.
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            basicEffect.World = transform;
            //basicEffect.World = Matrix.RotationX(time) * Matrix.RotationY(time * 2.0f) * Matrix.RotationZ(time * .7f);
            basicEffect.Projection = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 1000.0f);
            basicEffect.EnableDefaultLighting();

            // Setup the vertices
            GraphicsDevice.SetVertexBuffer(vertices);
            GraphicsDevice.SetVertexInputLayout(inputLayout);
            GraphicsDevice.SetIndexBuffer(indices, true);

            // Apply the basic effect technique and draw the rotating cube
            basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawIndexed(PrimitiveType.TriangleList, indices.ElementCount);
        }
    }
}

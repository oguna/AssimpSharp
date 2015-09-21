// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Toolkit;

namespace AssimpView
{
    // Use this namespace here in case we need to use Direct3D11 namespace as well, as this
    // namespace will override the Direct3D11.
    using SharpDX.Toolkit.Graphics;
    using System.Linq;

    /// <summary>
    /// Simple MiniCube application using SharpDX.Toolkit.
    /// The purpose of this application is to show a rotating cube using <see cref="BasicEffect"/>.
    /// </summary>
    public class AssimpViewGame : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private BasicEffect basicEffect;
        private Buffer<VertexPositionColor> vertices;
        private VertexInputLayout inputLayout;
        private List<FbxMesh> meshes;
        private AssimpSharp.Scene scene;
        private string file;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssimpViewGame" /> class.
        /// </summary>
        public AssimpViewGame(string file)
        {
            this.file = file;
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            meshes = FbxMeshLoader.Load(file, GraphicsDevice, out scene);
            base.LoadContent();
        }

        protected override void Initialize()
        {
            Window.Title = "Assimp View";

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            // Handle base.Update
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            var trans = Matrix.RotationX(time) * Matrix.RotationY(time * 2.0f) * Matrix.RotationZ(time * .7f);
            DrawSceneNode(scene.RootNode, trans, gameTime);
            // Handle base.Draw
            base.Draw(gameTime);
        }

        void DrawSceneNode(AssimpSharp.Node node, Matrix transform, GameTime gameTime)
        {
            transform = node.Transformation * transform;
            foreach(var meshId in node.Meshes ?? Enumerable.Empty<int>())
            {
                meshes[meshId].Draw(gameTime, transform);
            }
            foreach(var child in node.Children ?? Enumerable.Empty<AssimpSharp.Node>())
            {
                DrawSceneNode(child, transform, gameTime);
            }
        }
    }
}
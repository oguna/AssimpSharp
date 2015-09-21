using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using NUnit.Framework;
using AssimpSharp;
using AssimpSharp.FBX;
using SharpDX;

namespace Test
{
    public class FileTest
    {
        public FileTest(string filename)
        {
            this.filename = filename;
        }

        protected string filename;
        protected AssimpSharp.Scene assimpSharpScene;
        protected Assimp.Scene assimpNetScene;

        [Test]
        public void TestMeshes()
        {
            var netMeshes = assimpNetScene.Meshes;
            var sharpMeshes = assimpSharpScene.Meshes;
            Assert.AreEqual(netMeshes.Count, sharpMeshes.Count);
            for (int i = 0; i < netMeshes.Count; i++)
            {
                TestMesh(netMeshes[i], sharpMeshes[i]);
            }
        }

        void TestMesh(Assimp.Mesh netMesh, AssimpSharp.Mesh sharpMesh)
        {
            // Vertices
            Assert.AreEqual(netMesh.HasVertices, sharpMesh.Vertices != null);
            if (netMesh.HasVertices)
            {
                Assert.AreEqual(netMesh.VertexCount, sharpMesh.Vertices.Length);
                MathAssert.AreEqual(netMesh.Vertices, sharpMesh.Vertices);
            }
            // Faces
            Assert.AreEqual(netMesh.HasFaces, sharpMesh.Faces != null);
            if (netMesh.HasFaces)
            {
                Assert.AreEqual(netMesh.FaceCount, sharpMesh.Faces.Length);
                for (int i = 0; i < netMesh.FaceCount; i++)
                {
                    Assert.AreEqual(netMesh.Faces[i].HasIndices, sharpMesh.Faces[i].Indices != null);
                    Assert.AreEqual(netMesh.Faces[i].IndexCount, sharpMesh.Faces[i].Indices.Length);
                    Assert.AreEqual(netMesh.Faces[i].Indices, sharpMesh.Faces[i].Indices);
                }
            }
            // Normals
            Assert.AreEqual(netMesh.HasNormals, sharpMesh.Normals != null);
            if (netMesh.HasNormals)
            {
                MathAssert.AreEqual(netMesh.Normals, sharpMesh.Normals);
            }

            // BiTangents
            Assert.AreEqual(netMesh.HasTangentBasis, sharpMesh.Bitangents != null);
            if (netMesh.HasTangentBasis)
            {
                MathAssert.AreEqual(netMesh.BiTangents, sharpMesh.Bitangents);
            }
            // PrimitiveType
            Assert.AreEqual(netMesh.PrimitiveType.ToString(), Enum.GetName(typeof(AssimpSharp.PrimitiveType), sharpMesh.PrimitiveTypes));
            // TexureCoord
            for (int i = 0; i < netMesh.TextureCoordinateChannelCount; i++)
            {
                Assert.AreEqual(netMesh.HasTextureCoords(i), sharpMesh.HasTextureCoords(i));
                Assert.AreEqual(netMesh.TextureCoordinateChannels[i], sharpMesh.TextureCoords[i]);
            }
            // VertexColorChannels
            for (int i = 0; i < netMesh.VertexColorChannelCount; i++)
            {
                Assert.AreEqual(netMesh.HasVertexColors(i), sharpMesh.HasVertexColors(i));
                if (netMesh.HasVertexColors(i))
                {
                    Assert.AreEqual(netMesh.VertexColorChannels[i], sharpMesh.Colors[i]);
                }
            }
            // MaterialIndex
            Assert.AreEqual(netMesh.MaterialIndex, sharpMesh.MaterialIndex);
            // Name
            Assert.AreEqual(netMesh.Name, sharpMesh.Name);
            // UVComponentCount
            Assert.AreEqual(netMesh.UVComponentCount, sharpMesh.NumUVComponents);
            // MeshAnimationAttachments
            Assert.AreEqual(netMesh.HasMeshAnimationAttachments, sharpMesh.AnimMeshes != null);
            if (netMesh.HasMeshAnimationAttachments)
            {
                for (int i = 0; i < netMesh.MeshAnimationAttachmentCount; i++)
                {
                    TestAnimationAttachment(netMesh.MeshAnimationAttachments[i], sharpMesh.AnimMeshes[i]);
                }
            }
        }

        void TestAnimationAttachment(Assimp.MeshAnimationAttachment netAnimMesh, AssimpSharp.AnimMesh sharpAnimMesh)
        {
            Assert.AreEqual(netAnimMesh.HasVertices, sharpAnimMesh.HasPosition);
            if (netAnimMesh.HasVertices)
            {
                Assert.AreEqual(netAnimMesh.Vertices, sharpAnimMesh.Vertices);
            }
            Assert.AreEqual(netAnimMesh.HasNormals, sharpAnimMesh.HasNormals);
            if (netAnimMesh.HasNormals)
            {
                Assert.AreEqual(netAnimMesh.Normals, sharpAnimMesh.Normals);
            }
            Assert.AreEqual(netAnimMesh.HasTangentBasis, sharpAnimMesh.HasTangentsAndBitangets);
            if (netAnimMesh.HasTangentBasis)
            {
                Assert.AreEqual(netAnimMesh.Tangents, sharpAnimMesh.Tangents);
                Assert.AreEqual(netAnimMesh.BiTangents, sharpAnimMesh.Bitangents);
            }
            for (int i=0; i < netAnimMesh.TextureCoordinateChannelCount; i++)
            {
                Assert.AreEqual(netAnimMesh.HasTextureCoords(i), sharpAnimMesh.HasTextureCoords(i));
                if (netAnimMesh.HasTextureCoords(i))
                {
                    Assert.AreEqual(netAnimMesh.TextureCoordinateChannels[i], sharpAnimMesh.TextureCoords[i]);
                }
            }
            for (int i = 0; i < netAnimMesh.VertexColorChannelCount; i++)
            {
                Assert.AreEqual(netAnimMesh.HasVertexColors(i), sharpAnimMesh.HasVertexColors(i));
                if (netAnimMesh.HasVertexColors(i))
                {
                    Assert.AreEqual(netAnimMesh.VertexColorChannels[i], sharpAnimMesh.Colors[i]);
                }
            }
        }
        
        [Test]
        public void TestTextures()
        {
            var netTextures = assimpNetScene.Textures;
            var sharpTextures = assimpSharpScene.Textures;
            Assert.AreEqual(assimpNetScene.TextureCount, assimpSharpScene.Textures.Count);
            for(int textureIndex = 0; textureIndex < assimpNetScene.TextureCount; textureIndex++)
            {
                var netTexture = netTextures[textureIndex];
                var sharpTexture = sharpTextures[textureIndex];
                TestTexture(netTexture, sharpTexture);
            }
        }

        void TestTexture(EmbeddedTexture netTexture, AssimpSharp.Texture sharpTexture)
        {
            throw (new NotImplementedException());
        }

        [Test]
        public void TestNodes()
        {
            var netRootNodes = assimpNetScene.RootNode;
            var sharpRootNode = assimpSharpScene.RootNode;
            TestEachNode(netRootNodes, sharpRootNode);
        }

        public void TestEachNode(Assimp.Node netNode, AssimpSharp.Node sharpNode)
        {
            Assert.AreEqual(netNode.Name, sharpNode.Name);
            MathAssert.AreEqual(netNode.Transform, sharpNode.Transformation);
            Assert.AreEqual(netNode.HasMeshes, sharpNode.Meshes != null);
            if (netNode.HasMeshes)
            {
                Assert.AreEqual(netNode.MeshCount, sharpNode.Meshes.Count);
                for (int i = 0; i<netNode.MeshCount; i++)
                {
                    Assert.AreEqual(netNode.MeshIndices, sharpNode.Meshes);
                }
            }
            Assert.AreEqual(netNode.HasChildren, sharpNode.Children != null);
            if (netNode.HasChildren)
            {
                Assert.AreEqual(netNode.ChildCount, sharpNode.Children.Count);
                for(int nodeIndex = 0; nodeIndex < netNode.ChildCount; nodeIndex++)
                {
                    TestEachNode(netNode.Children[nodeIndex], sharpNode.Children[nodeIndex]);
                }
            }
        }

        [Test]
        public void TestMaterials()
        {
            var netMaterials = assimpNetScene.Materials;
            var sharpMaterials = assimpSharpScene.Materials;
            Assert.AreEqual(assimpNetScene.MaterialCount, assimpSharpScene.Materials.Count);
            for(int materialIndex = 0; materialIndex < netMaterials.Count; materialIndex++)
            {
                var netMaterial = netMaterials[materialIndex];
                var sharpMaterial = sharpMaterials[materialIndex];
                Console.WriteLine("--------");
                Console.WriteLine("Index : " + materialIndex);
                Assert.AreEqual(netMaterial.HasName, !string.IsNullOrEmpty(sharpMaterial.Name));
                if (netMaterial.HasName)
                {
                    Assert.AreEqual(netMaterial.Name, sharpMaterial.Name);
                    Console.WriteLine("Name : " + netMaterial.Name);
                }
                Assert.AreEqual(netMaterial.HasColorDiffuse, sharpMaterial.ColorDiffuse.HasValue);
                if (netMaterial.HasColorDiffuse)
                {
                    MathAssert.AreEqual(netMaterial.ColorDiffuse, sharpMaterial.ColorDiffuse.Value);
                    Console.WriteLine("ColorDiffuse : " + sharpMaterial.ColorDiffuse.Value);
                }
                Assert.AreEqual(netMaterial.HasColorAmbient, sharpMaterial.ColorAmbient.HasValue);
                if (netMaterial.HasColorAmbient)
                {
                    MathAssert.AreEqual(netMaterial.ColorAmbient, sharpMaterial.ColorAmbient.Value);
                    Console.WriteLine("ColorAmbient : " + sharpMaterial.ColorAmbient.Value);
                }
                Assert.AreEqual(netMaterial.HasColorEmissive, sharpMaterial.ColorEmissive.HasValue);
                if (netMaterial.HasColorEmissive)
                {
                    MathAssert.AreEqual(netMaterial.ColorEmissive, sharpMaterial.ColorEmissive.Value);
                    Console.WriteLine("ColorEmissive : " + sharpMaterial.ColorEmissive.Value);
                }
                Assert.AreEqual(netMaterial.HasColorReflective, sharpMaterial.ColorReflective.HasValue);
                if (netMaterial.HasColorReflective) 
                {
                    MathAssert.AreEqual(netMaterial.ColorReflective, sharpMaterial.ColorReflective.Value);
                    Console.WriteLine("ColorReflective : " + sharpMaterial.ColorReflective.Value);
                }
                Assert.AreEqual(netMaterial.HasColorSpecular, sharpMaterial.ColorSpecular.HasValue);
                if (netMaterial.HasColorSpecular)
                {
                    MathAssert.AreEqual(netMaterial.ColorSpecular, sharpMaterial.ColorSpecular.Value);
                    Console.WriteLine("ColorSpecular : " + sharpMaterial.ColorSpecular.Value);
                }
                Assert.AreEqual(netMaterial.HasColorTransparent, sharpMaterial.ColorTransparent.HasValue);
                if (netMaterial.HasColorTransparent)
                {
                    MathAssert.AreEqual(netMaterial.ColorTransparent, sharpMaterial.ColorTransparent.Value);
                    Console.WriteLine("ColorTransparent : " + sharpMaterial.ColorTransparent.Value);
                }
                Assert.AreEqual(netMaterial.HasOpacity, sharpMaterial.Opacity.HasValue);
                if (netMaterial.HasOpacity)
                {
                    MathAssert.AreEqual(netMaterial.Opacity, sharpMaterial.Opacity.Value);
                    Console.WriteLine("Opacity : " + sharpMaterial.Opacity.Value);
                }
                Assert.AreEqual(netMaterial.HasReflectivity, sharpMaterial.Reflectivity.HasValue);
                if (netMaterial.HasReflectivity)
                {
                    MathAssert.AreEqual(netMaterial.Reflectivity, sharpMaterial.Reflectivity.Value);
                    Console.WriteLine("Reflectivity : " + sharpMaterial.Reflectivity.Value);
                }
                Assert.AreEqual(netMaterial.HasShininess, sharpMaterial.Shininess.HasValue);
                if (netMaterial.HasShininess)
                {
                    MathAssert.AreEqual(netMaterial.Shininess, sharpMaterial.Shininess.Value);
                    Console.WriteLine("Shininess : " + sharpMaterial.Shininess.Value);
                }
                Assert.AreEqual(netMaterial.HasShininessStrength, sharpMaterial.ShininessStrength.HasValue);
                if (netMaterial.HasShininessStrength)
                {
                    MathAssert.AreEqual(netMaterial.ShininessStrength, sharpMaterial.ShininessStrength.Value);
                    Console.WriteLine("ShininessStrength : " + sharpMaterial.ShininessStrength.Value);
                }
                Assert.AreEqual(netMaterial.HasShadingMode, sharpMaterial.ShadingMode.HasValue);
                if (netMaterial.HasShadingMode)
                {
                    Assert.AreEqual(netMaterial.ShadingMode, sharpMaterial.ShadingMode.Value);
                    Console.WriteLine("ShadingMode : " + sharpMaterial.ShadingMode.Value);
                }
                Assert.AreEqual(netMaterial.HasBlendMode, sharpMaterial.BlendMode.HasValue);
                if (netMaterial.HasBlendMode)
                {
                    Assert.AreEqual(netMaterial.BlendMode, sharpMaterial.BlendMode.Value);
                    Console.WriteLine("BlendMode : " + sharpMaterial.BlendMode.Value);
                }
                Assert.AreEqual(netMaterial.HasBumpScaling, sharpMaterial.BumpScaling.HasValue);
                if (netMaterial.HasBumpScaling)
                {
                    Assert.AreEqual(netMaterial.BumpScaling, sharpMaterial.BumpScaling.Value);
                    Console.WriteLine("BumpScaling : " + sharpMaterial.BumpScaling.Value);
                }
                Assert.AreEqual(netMaterial.HasTextureDiffuse, sharpMaterial.TextureDiffuse != null);
                if (netMaterial.HasTextureDiffuse)
                {
                    Assert.AreEqual(netMaterial.TextureDiffuse.FilePath, sharpMaterial.TextureDiffuse.TextureBase);
                    Console.WriteLine("TextureDiffuse : " + netMaterial.TextureDiffuse.FilePath);
                }
            }
        }

        [Test]
        public void TestAnimation()
        {
            var netAnimation = assimpNetScene.Animations;
            var sharpAnimations = assimpSharpScene.Animations;
            Assert.AreEqual(assimpNetScene.HasAnimations, assimpSharpScene.Animations.Count > 0);
            if (!assimpNetScene.HasAnimations)
            {
                return;
            }
            for(int i=0; i < netAnimation.Count; i++)
            {
                var netAnim = netAnimation[i];
                var sharpAnim = sharpAnimations[i];
                Assert.AreEqual(netAnim.Name, sharpAnim.Name);
                Assert.AreEqual(netAnim.DurationInTicks, sharpAnim.Duration);
                Assert.AreEqual(netAnim.TicksPerSecond, sharpAnim.TicksPreSecond);

                Assert.AreEqual(netAnim.MeshAnimationChannelCount, sharpAnim.MeshChannels.Length);
                if (netAnim.MeshAnimationChannelCount > 0)
                {
                    for(int j=0; j<netAnim.MeshAnimationChannelCount; j++)
                    {
                        TestAnimationChannel(netAnim.MeshAnimationChannels[i], sharpAnim.MeshChannels[i]);
                    }
                }

                Assert.AreEqual(netAnim.NodeAnimationChannelCount, sharpAnim.Channels.Length);
                if (netAnim.NodeAnimationChannelCount > 0)
                {
                    for(int j=0; j<netAnim.NodeAnimationChannelCount; j++)
                    {
                        TestAnimationChannel(netAnim.NodeAnimationChannels[i], sharpAnim.Channels[i]);
                    }
                }
            }
        }

        void TestAnimationChannel(NodeAnimationChannel netChannel, NodeAnim sharpChannel)
        {
            Assert.AreEqual(netChannel.NodeName, sharpChannel.NodeName);
            Assert.AreEqual(netChannel.PositionKeyCount, sharpChannel.PositionKeys.Length);
            for(int i=0; i<netChannel.PositionKeyCount; i++)
            {
                Assert.AreEqual(netChannel.PositionKeys[i].Time, sharpChannel.PositionKeys[i].Time);
                MathAssert.AreEqual(netChannel.PositionKeys[i].Value, sharpChannel.PositionKeys[i].Value);
            }
            Assert.AreEqual(netChannel.RotationKeyCount, sharpChannel.RotationKeys.Length);
            for (int i = 0; i < netChannel.RotationKeyCount; i++)
            {
                Assert.AreEqual(netChannel.RotationKeys[i].Time, sharpChannel.RotationKeys[i].Time);
                MathAssert.AreEqual(netChannel.RotationKeys[i].Value, sharpChannel.RotationKeys[i].Value);
            }
            Assert.AreEqual(netChannel.ScalingKeyCount, sharpChannel.ScalingKeys.Length);
            for (int i = 0; i < netChannel.ScalingKeyCount; i++)
            {
                Assert.AreEqual(netChannel.ScalingKeys[i].Time, sharpChannel.ScalingKeys[i].Time);
                MathAssert.AreEqual(netChannel.ScalingKeys[i].Value, sharpChannel.ScalingKeys[i].Value);
            }
        }

        void TestAnimationChannel(MeshAnimationChannel netChannel, MeshAnim sharpChannel)
        {
            Assert.AreEqual(netChannel.MeshName, sharpChannel.Name);
            Assert.AreEqual(netChannel.MeshKeyCount, sharpChannel.Keys.Length);
            for(int i=0; i<netChannel.MeshKeyCount; i++)
            {
                Assert.AreEqual(netChannel.MeshKeys[i].Time, sharpChannel.Keys[i].Time);
                Assert.AreEqual(netChannel.MeshKeys[i].Value, sharpChannel.Keys[i].Value);
            }
        }

        [Test]
        public void TestCameras()
        {
            var netCameras = assimpNetScene.Cameras;
            var sharpCameras = assimpSharpScene.Cameras;
            Assert.AreEqual(assimpNetScene.CameraCount, assimpSharpScene.Cameras.Count);
            for (int i = 0; i < netCameras.Count; i++)
            {
                var netCamera = netCameras[i];
                var sharpCamera = sharpCameras[i];
                Assert.AreEqual(netCamera.AspectRatio, sharpCamera.Aspect);
                Assert.AreEqual(netCamera.ClipPlaneFar, sharpCamera.ClipPlaneFar);
                Assert.AreEqual(netCamera.ClipPlaneNear, sharpCamera.ClipPlaneNear);
                MathAssert.AreEqual(netCamera.Direction, sharpCamera.LookAt);
                // Assimp.net bug?
                //Assert.AreEqual(netCamera.FieldOfview, sharpCamera.HorizontalFOV);
                Assert.AreEqual(netCamera.Name, sharpCamera.Name);
                MathAssert.AreEqual(netCamera.Position, sharpCamera.Position);
                MathAssert.AreEqual(netCamera.Up, sharpCamera.Up);
                MathAssert.AreEqual(netCamera.ViewMatrix, sharpCamera.CameraMatrix);
            }
        }

        [Test]
        public void TestLights()
        {
            var netLights = assimpNetScene.Lights;
            var sharpLights = assimpSharpScene.Lights;
            Assert.AreEqual(assimpNetScene.LightCount, assimpSharpScene.Lights.Count);
            for (int i=0; i<netLights.Count; i++)
            {
                var netLight = netLights[i];
                var sharpLight = sharpLights[i];
                Assert.AreEqual(netLight.AngleInnerCone, sharpLight.AngleInnerCone);
                Assert.AreEqual(netLight.AngleOuterCone, sharpLight.AngleOuterCone);
                Assert.AreEqual(netLight.AttenuationConstant, sharpLight.AttenuationConstant);
                Assert.AreEqual(netLight.AttenuationLinear, sharpLight.AttenuationLinear);
                Assert.AreEqual(netLight.AttenuationQuadratic, sharpLight.AttenuationQuadratic);
                MathAssert.AreEqual(netLight.ColorAmbient, sharpLight.ColorAmbient);
                MathAssert.AreEqual(netLight.ColorDiffuse, sharpLight.ColorDiffuse);
                MathAssert.AreEqual(netLight.ColorSpecular, sharpLight.ColorSpecular);
                MathAssert.AreEqual(netLight.Direction, sharpLight.Direction);
                Assert.AreEqual((int)netLight.LightType, (int)sharpLight.Type);
                Assert.AreEqual(netLight.Name, sharpLight.Name);
                MathAssert.AreEqual(netLight.Position, sharpLight.Position);
            }
        }

    }
}

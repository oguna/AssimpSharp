using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using aiMaterial = AssimpSharp.Material;
using SharpDX;

namespace AssimpSharp.XFile
{
    /// <summary>
    /// The XFileImporter is a worker class capable of importing a scene from a
    /// DirectX file.x
    /// </summary>
    public class XFileImporter : BaseImporter
    {
        const int AI_MAX_NUMBER_OF_COLOR_SETS = 8;
        const int AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;

        /// <summary>
        /// Buffer to hold the loaded file
        /// </summary>
        protected byte[] Buffer;

        protected static readonly ImporterDesc Desc = new ImporterDesc()
        {
            Name = "Direct3D XFile Importer",
            Author = "",
            Maintainer = "",
            Comment = "",
            Flags = ImporterFlags.SupportTextFlavour | ImporterFlags.SupportBinaryFlavour | ImporterFlags.SupportCompressedFlavour,
            MinMajor = 1,
            MinMinor = 3,
            MaxMajor = 1,
            MaxMinor = 5,
            FileExtensions = "x"
        };

        /// <summary>
        /// Returns whether the class can handle the format of the given file.
        /// See BaseImporter::CanRead() for details.
        /// </summary>
        public override bool CanRead(string file, IOSystem ioHandler, bool checkSig)
        {
            var extension = GetExtension(file);
            if (extension == "x")
            {
                return true;
            }
            if (string.IsNullOrEmpty(extension) || checkSig)
            {
                var token = new[] { "xof" };
                return CheckMagicToken(ioHandler, file, token, 1, 0);
            }
            return false;
        }

        /// <summary>
        /// Return importer meta information.
        /// See #BaseImporter::GetInfo for the details
        /// </summary>
        public override ImporterDesc GetInfo()
        {
            return Desc;
        }

        /// <summary>
        /// Imports the given file into the given scene structure.
        /// See BaseImporter::InternReadFile() for details
        /// </summary>
        public void InternReadFile(string filename, AssimpSharp.Scene scene)
        {
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                Buffer = new byte[stream.Length];
                stream.Read(Buffer, 0, (int)stream.Length);
            }

            var parser = new XFileParser(Buffer);

            CreateDataRepresentationFromImport(scene, parser.GetImportedData());

            if (scene.RootNode== null)
            {
                throw (new NotImplementedException("XFile is ill-formatted - no content imported."));
            }
        }

        /// <summary>
        /// Constructs the return data structure out of the imported data.
        /// </summary>
        /// <param name="scene">The scene to construct the return data in.</param>
        /// <param name="sdata">The imported data in the internal temporary representation.</param>
        protected void CreateDataRepresentationFromImport(AssimpSharp.Scene scene, Scene data)
        {
            // Read the global materials first so that meshes referring to them can find them later
            ConvertMaterials(scene, data.GlobalMaterial);

            // copy nodes, extracting meshes and materials on the way
            scene.RootNode = CreateNodes(scene, null, data.RootNode);

            // extract animations
            CreateAnimations(scene, data);

            // read the global meshes that were stored outside of any node
            if (data.GlobalMeshes.Count > 0)
            {
                // create a root node to hold them if there isn't any, yet
                if (scene.RootNode == null)
                {
                    scene.RootNode = new AssimpSharp.Node();
                    scene.RootNode.Name = "$dummy_node";
                }

                // convert all global meshes and store them in the root node.
                // If there was one before, the global meshes now suddenly have its transformation matrix...
                // Don't know what to do there, I don't want to insert another node under the present root node
                // just to avoid this.
                CreateMeshes(scene, scene.RootNode, data.GlobalMeshes);
            }

            if (scene.RootNode == null)
            {
                throw (new DeadlyImportError("No root node"));
            }

            // Convert everything to OpenGL space... it's the same operation as the conversion back, so we can reuse the step directly
            var convertProcess = new MakeLeftHandedProcess();
            convertProcess.Execute(scene);

            var flipper = new FlipWindingOrderProcess();
            flipper.Execute(scene);

            // finally: create a dummy material if not material was imported
            if (scene.Materials.Count == 0)
            {
                var mat = new aiMaterial();
                mat.ShadingMode = ShadingMode.Gouraud;
                mat.ColorEmissive = new Color4(0, 0, 0, 1);
                mat.ColorSpecular = new Color4(0, 0, 0, 1);
                mat.ColorDiffuse = new Color4(0.5f, 0.5f, 0.5f, 1);
                mat.Shininess = 1.401298e-45f;
                scene.Materials.Add(mat);
            }
        }

        /// <summary>
        /// Recursively creates scene nodes from the imported hierarchy.
        /// The meshes and materials of the nodes will be extracted on the way.
        /// </summary>
        /// <param name="scene">The scene to construct the return data in.</param>
        /// <param name="parent">The parent node where to create new child nodes</param>
        /// <param name="node">The temporary node to copy.</param>
        /// <returns>The created node</returns>
        protected AssimpSharp.Node CreateNodes(AssimpSharp.Scene scene, AssimpSharp.Node parent, AssimpSharp.XFile.Node node)
        {
            if (node == null)
            {
                return null;
            }

            // crate node
            var result = new AssimpSharp.Node();
            result.Name = node.Name;
            result.Transformation = node.TrafoMatrix;
            result.Parent = parent;

            // convert meshes from the source node
            CreateMeshes(scene, result, node.Meshes);

            // handle childs
            if (node.Children.Count > 0)
            {
                result.Children.Clear();
                foreach(var i in node.Children)
                {
                    result.Children.Add(CreateNodes(scene, result, i));
                }
            }

            return result;
        }

        /// <summary>
        /// Converts all meshes in the given mesh array. Each mesh is split
        /// up per material, the indices of the generated meshes are stored in
        /// the node structure.
        /// </summary>
        /// <param name="scene">The scene to construct the return data in.</param>
        /// <param name="node">The target node structure that references the constructed meshes.</param>
        /// <param name="meshes">The array of meshes to convert</param>
        protected void CreateMeshes(AssimpSharp.Scene scene, AssimpSharp.Node node, List<AssimpSharp.XFile.Mesh> meshes)
        {
            if (meshes.Count == 0)
            {
                return;
            }

            // create a mesh for each mesh-material combination in the source node
            var result = new List<AssimpSharp.Mesh>();
            for (int a = 0; a < meshes.Count; a++)
            {
                var sourceMesh = meshes[a];
                // first convert its materials so that we can find them with their index afterwards
                ConvertMaterials(scene, sourceMesh.Materials);

                int numMaterials = Math.Max(sourceMesh.Materials.Count, 1);
                for (int b = 0; b < numMaterials; b++)
                {
                    // collect the faces belonging to this material
                    var faces = new List<int>();
                    var numVertices = 0;
                    if (sourceMesh.FaceMaterials.Count > 0)
                    {
                        // if there is a per-face material defined, select the faces with the corresponding material
                        for (int c = 0; c < sourceMesh.FaceMaterials.Count; c++)
                        {
                            if (sourceMesh.FaceMaterials[c] == b)
                            {
                                faces.Add(c);
                                numVertices += sourceMesh.PosFaces[c].Indices.Count;
                            }
                        }
                    }
                    else
                    {
                        // if there is no per-face material, place everything into one mesh
                        for (int c = 0; c < sourceMesh.PosFaces.Count; c++)
                        {
                            faces.Add(c);
                            numVertices += sourceMesh.PosFaces[c].Indices.Count;
                        }
                    }

                    // no faces/vertices using this material? strange...
                    if (numVertices == 0)
                    {
                        continue;
                    }

                    // create a submesh using this material
                    var mesh = new AssimpSharp.Mesh();
                    result.Add(mesh);

                    // find the material in the scene's material list. Either own material
                    // or referenced material, it should already have a valid index
                    if (sourceMesh.FaceMaterials.Count > 0)
                    {
                        mesh.MaterialIndex = sourceMesh.Materials[b].SceneIndex;
                    }
                    else
                    {
                        mesh.MaterialIndex = 0;
                    }

                    // Create properly sized data arrays in the mesh. We store unique vertices per face,
                    // as specified
                    mesh.NumVertices = numVertices;
                    mesh.Vertices = new Vector3[numVertices];
                    mesh.NumFaces = faces.Count;
                    mesh.Faces = new AssimpSharp.Face[mesh.NumFaces];

                    // normals?
                    if (sourceMesh.Normals.Count > 0)
                    {
                        mesh.Normals = new Vector3[numVertices];
                    }
                    // texture coords
                    for (int c = 0; c < AI_MAX_NUMBER_OF_TEXTURECOORDS; c++)
                    {
                        if (sourceMesh.TexCoords[c] != null && sourceMesh.TexCoords[c].Count > 0)
                        {
                            mesh.TextureCoords[c] = new Vector3[numVertices];
                        }
                    }
                    // vertex colors
                    mesh.Colors = new Color4[AI_MAX_NUMBER_OF_COLOR_SETS][];
                    for (int c = 0; c < AI_MAX_NUMBER_OF_COLOR_SETS; c++)
                    {
                        if (sourceMesh.Colors[c] != null && sourceMesh.Colors[c].Count > 0)
                        {
                            mesh.Colors[c] = new Color4[numVertices];
                        }
                    }

                    // now collect the vertex data of all data streams present in the imported mesh
                    int newIndex = 0;
                    var orgPoints = new int[numVertices];

                    for (int c = 0; c < faces.Count; c++)
                    {
                        int f = faces[c];
                        var pf = sourceMesh.PosFaces[f];

                        // create face. either triangle or triangle fan depending on the index count
                        var df = mesh.Faces[c] = new AssimpSharp.Face();
                        df.Indices = new int[pf.Indices.Count];

                        // collect vertex data for indices of this face
                        for (int d = 0; d < df.Indices.Length; d++)
                        {
                            df.Indices[d] = newIndex;
                            orgPoints[newIndex] = (int)pf.Indices[d];

                            // Position
                            mesh.Vertices[newIndex] = sourceMesh.Positions[(int)pf.Indices[d]];

                            // Normal, if present
                            if (mesh.HasNormals)
                            {
                                mesh.Normals[newIndex] = sourceMesh.Normals[(int)sourceMesh.NormalFaces[f].Indices[d]];
                            }

                            // texture coord sets
                            for (int e = 0; e < AI_MAX_NUMBER_OF_TEXTURECOORDS; e++)
                            {
                                if (mesh.HasTextureCoords(e))
                                {
                                    var tex = sourceMesh.TexCoords[e][(int)pf.Indices[d]];
                                    mesh.TextureCoords[e][newIndex] = new Vector3(tex.X, 1.0f - tex.Y, 0.0f);
                                }
                            }

                            // vertex color sets
                            for (int e = 0; e < AI_MAX_NUMBER_OF_COLOR_SETS; e++)
                            {
                                if (mesh.HasVertexColors(e))
                                {
                                    mesh.Colors[e][newIndex] = sourceMesh.Colors[e][(int)pf.Indices[d]];
                                }
                            }
                            newIndex++;
                        }
                    }

                    Debug.Assert(newIndex == numVertices);

                    var bones = sourceMesh.Bones;
                    var newBones = new List<AssimpSharp.Bone>();
                    for (int c = 0; c < bones.Count; c++)
                    {
                        var obone = bones[c];

                        var oldWeights = new float[sourceMesh.Positions.Count];
                        for (int d = 0; d < obone.Weights.Count; d++)
                        {
                            oldWeights[(int)obone.Weights[d].Vertex] = obone.Weights[d].Weight;
                        }

                        var newWeights = new List<AssimpSharp.VertexWeight>(numVertices);
                        for (int d = 0; d < orgPoints.Length; d++)
                        {
                            float w = oldWeights[orgPoints[d]];
                            if (w > 0.0f)
                            {
                                newWeights.Add(new VertexWeight(d, w));
                            }
                        }

                        if (newWeights.Count == 0)
                        {
                            continue;
                        }

                        // create
                        var nbone = new AssimpSharp.Bone();
                        newBones.Add(nbone);
                        // copy name end matrix
                        nbone.Name = obone.Name;
                        nbone.OffsetMatrix = obone.OffsetMatrix;
                        nbone.NumWeights = newWeights.Count;
                        nbone.Weights = new VertexWeight[nbone.NumWeights];
                        for (int d = 0; d < newWeights.Count; d++)
                        {
                            nbone.Weights[d] = newWeights[d];
                        }
                    }

                    mesh.NumBones = newBones.Count;
                    if (newBones.Count > 0)
                    {
                        mesh.Bones = mesh.Bones.ToArray();
                    }
                }
            }
            
            // allocate mesh index array in the node
            node.Meshes.Capacity = meshes.Count;

            // store all meshes in the mesh library of the scene and store their indices in the node
            for (int a = 0; a < result.Count; a++)
            {
                scene.Meshes.Add(result[a]);
                node.Meshes.Add(scene.Meshes.Count - 1);
            }
        }

        /// <summary>
        /// Converts the animations from the given imported data and creates
        ///  them in the scene.
        /// </summary>
        /// <param name="scene">The scene to hold to converted animations</param>
        /// <param name="data">The data to read the animations from</param>
        protected void CreateAnimations(AssimpSharp.Scene scene, AssimpSharp.XFile.Scene data)
        {
            var newAnims = new List<AssimpSharp.Animation>();

            for (int a = 0; a < data.Anims.Count; a++)
            {
                var anim = data.Anims[a];
                if (anim.Anims.Count == 0)
                {
                    continue;
                }

                var nanim = new AssimpSharp.Animation();
                newAnims.Add(nanim);
                nanim.Name = anim.Name;
                nanim.Duration = 0;
                nanim.TicksPreSecond = data.AnimTicksPerSecond;
                nanim.Channels = new AssimpSharp.NodeAnim[anim.Anims.Count];

                for (int b = 0; b < anim.Anims.Count; b++)
                {
                    var bone = anim.Anims[b];
                    var nbone = new AssimpSharp.NodeAnim();
                    nbone.NodeName = bone.BoneName;
                    nanim.Channels[b] = nbone;

                    if (bone.TrafoKeys.Count > 0)
                    {
                        nbone.PositionKeys = new VectorKey[bone.TrafoKeys.Count];
                        nbone.RotationKeys = new QuatKey[bone.TrafoKeys.Count];
                        nbone.ScalingKeys = new VectorKey[bone.TrafoKeys.Count];

                        for (int c = 0; c < bone.TrafoKeys.Count; c++)
                        {
                            var time = bone.TrafoKeys[c].Key;
                            var trafo = bone.TrafoKeys[c].Value;

                            var pos = trafo.TranslationVector;

                            nbone.PositionKeys[c].Time = time;
                            nbone.PositionKeys[c].Value = pos;

                            Vector3 scale;
                            scale.X = new Vector3(trafo[0, 0], trafo[0, 1], trafo[0, 2]).Length();
                            scale.Y = new Vector3(trafo[1, 0], trafo[1, 1], trafo[1, 2]).Length();
                            scale.Z = new Vector3(trafo[2, 0], trafo[2, 1], trafo[2, 2]).Length();
                            nbone.ScalingKeys[c].Time = time;
                            nbone.ScalingKeys[c].Value = scale;

                            var rotmat = new Matrix3x3(
                        trafo[0, 0] / scale.X, trafo[0, 1] / scale.Y, trafo[0, 2] / scale.Z,
                        trafo[1, 0] / scale.X, trafo[1, 1] / scale.Y, trafo[1, 2] / scale.Z,
                        trafo[2, 0] / scale.X, trafo[2, 1] / scale.Y, trafo[2, 2] / scale.Z);

                            nbone.RotationKeys[c].Time = time;
                            throw (new NotImplementedException());
                            //nbone.RotationKeys[c].Value = ;
                        }
                        nanim.Duration = Math.Max(nanim.Duration, bone.TrafoKeys[bone.TrafoKeys.Count].Key);
                    }
                    else
                    {
                        // separate key sequences for position, rotation, scaling
                        nbone.PositionKeys = new VectorKey[bone.PosKeys.Count];
                        for (int c = 0; c < nbone.PositionKeys.Length; c++)
                        {
                            var pos = bone.PosKeys[c].Value;
                            nbone.PositionKeys[c].Time = bone.PosKeys[c].Time;
                            nbone.PositionKeys[c].Value = pos;
                        }

                        // rotation
                        nbone.RotationKeys = new QuatKey[bone.RotKeys.Count];
                        for (int c = 0; c < nbone.RotationKeys.Length; c++)
                        {
                            var rotmat = Matrix3x3.RotationQuaternion(bone.RotKeys[c].Value);
                            nbone.RotationKeys[c].Time = bone.RotKeys[c].Time;
                            Quaternion.RotationMatrix(ref rotmat, out nbone.RotationKeys[c].Value);
                            nbone.RotationKeys[c].Value.W *= -1.0f;
                        }

                        // longest lasting key sequence determines duration
                        nbone.ScalingKeys = new VectorKey[bone.ScaleKeys.Count];
                        for (int c = 0; c < nbone.ScalingKeys.Length; c++)
                        {
                            nbone.ScalingKeys[c] = bone.ScaleKeys[c];
                        }

                        // longest lasting key sequence determines duration
                        if (bone.PosKeys.Count > 0)
                        {
                            nanim.Duration = Math.Max(nanim.Duration, bone.PosKeys[bone.PosKeys.Count - 1].Time);
                        }
                        if (bone.RotKeys.Count > 0)
                        {
                            nanim.Duration = Math.Max(nanim.Duration, bone.RotKeys[bone.RotKeys.Count - 1].Time);
                        }
                        if (bone.ScaleKeys.Count > 0)
                        {
                            nanim.Duration = Math.Max(nanim.Duration, bone.ScaleKeys[bone.ScaleKeys.Count - 1].Time);
                        }
                    }
                }
            }

            // store all converted animations in the scene
            if (newAnims.Count > 0)
            {
                scene.Animations.Clear();
                scene.Animations.AddRange(newAnims);
            }
        }

        /// <summary>
        /// Converts all materials in the given array and stores them in the
        ///  scene's material list.
        /// </summary>
        /// <param name="scene">The scene to hold the converted materials.</param>
        /// <param name="materials">The material array to convert.</param>
        protected void ConvertMaterials(AssimpSharp.Scene scene, List<AssimpSharp.XFile.Material> materials)
        {
            // count the non-referrer materials in the array
            var numNewMaterials = materials.Count(e => { return !e.IsReference; });

            // resize the scene's material list to offer enough space for the new materials
            if (numNewMaterials > 0)
            {
                scene.Materials.Capacity = scene.Materials.Count + numNewMaterials;

            }

            // convert all the materials given in the array
            for (int a = 0; a < materials.Count; a++)
            {
                XFile.Material oldMat = materials[a];
                if (oldMat.IsReference)
                {
                    // find the material it refers to by name, and store its index
                    for (int b = 0; b < scene.Materials.Count; b++)
                    {
                        var name = scene.Materials[b].Name;
                        if (name == oldMat.Name)
                        {
                            oldMat.SceneIndex = a;
                            break;
                        }
                    }

                    if (oldMat.SceneIndex == int.MaxValue)
                    {
                        throw (new Exception());
                        oldMat.SceneIndex = 0;
                    }
                    continue;
                }

                var mat = new aiMaterial();
                mat.Name = oldMat.Name;

                var shadeMode = (oldMat.SpecularExponent == 0.0f)
                    ? AssimpSharp.ShadingMode.Gouraud : ShadingMode.Phong;
                mat.ShadingMode = shadeMode;

                mat.ColorEmissive = new Color4(oldMat.Emissive, 1);
                mat.ColorDiffuse = oldMat.Diffuse;
                mat.ColorSpecular = new Color4(oldMat.Specular, 1);
                mat.Shininess = oldMat.SpecularExponent;

                if (1 == oldMat.Textures.Count)
                {
                    var otex = oldMat.Textures[0];
                    if (!string.IsNullOrEmpty(otex.Name))
                    {
                        string tex = otex.Name;
                        if (otex.IsNormalMap)
                        {
                            mat.TextureNormals = new TextureSlot() { TextureBase = tex };
                        }
                        else
                        {
                            mat.TextureDiffuse = new TextureSlot() { TextureBase = tex };
                        }
                    }
                }
                else
                {
                    int iHM = 0, iNM = 0, iDM = 0, iSM = 0, iAM = 0, iEM = 0;
                    for (int b = 0; b < oldMat.Textures.Count; b++)
                    {
                        var otex = oldMat.Textures[b];
                        var sz = otex.Name;
                        if (string.IsNullOrEmpty(sz))
                        {
                            continue;
                        }

                        int s = sz.LastIndexOf("\\/");
                        if (s == -1)
                        {
                            s = 0;
                        }

                        int sExt = sz.LastIndexOf('.');
                        if (sExt > 0)
                        {
                            sz = sz.Substring(0, sExt - 1) + '\0' + sz.Substring(sExt);
                        }

                        sz = sz.ToLower();

                        var tex = new TextureSlot() { TextureBase = oldMat.Textures[b].Name };

                        if (sz.Substring(s).Contains("bump") || sz.Substring(s).Contains("height"))
                        {
                            mat.TextureHeight = tex;
                        }
                        else if (otex.IsNormalMap || sz.Substring(s).Contains("normal") || sz.Substring(s).Contains("nm"))
                        {
                            mat.TextureNormals = tex;
                        }
                        else if (sz.Substring(s).Contains("spec") || sz.Substring(s).Contains("glanz"))
                        {
                            mat.TextureSpecular = tex;
                        }
                        else if (sz.Substring(s).Contains("ambi") || sz.Substring(s).Contains("env"))
                        {
                            mat.TextureAmbient = tex;
                        }
                        else if (sz.Substring(s).Contains("emissive") || sz.Substring(s).Contains("self"))
                        {
                            mat.TextureEmmisive = tex;
                        }
                        else
                        {
                            mat.TextureDiffuse = tex;
                        }
                    }
                }
                scene.Materials.Add(mat);
                //scene.Materials[scene.Materials.Count] = mat;
                oldMat.SceneIndex = scene.Materials.Count - 1;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Imports a waveform obj file
    /// </summary>
    public class ObjFileImporter : BaseImporter
    {
        const int ObjMinSize = 16;

        readonly ImporterDesc Desc = new ImporterDesc()
        {
            Name = "Wavefront Object Importer",
            Author = "",
            Maintainer = "",
            Comment = "surfaces not supported",
            Flags = ImporterFlags.SupportTextFlavour,
            MinMajor = 0,
            MinMinor = 0,
            MaxMajor = 0,
            MaxMinor = 0,
            FileExtensions = "obj"
        };

        /// <summary>
        /// Data buffer
        /// </summary>
        TextReader Buffer;

        /// <summary>
        /// Root object
        /// </summary>
        Object RootObject;

        /// <summary>
        /// Absolute pathname of model in file system
        /// </summary>
        string AbsPath;

        public ObjFileImporter()
        {
        }

        public override bool CanRead(string file, IOSystem ioHandler, bool checkSig)
        {
            if (!checkSig)
            {
                return SimpleExtensionCheck(file, "obj");
            }
            else
            {
                var tokens = new[] { "mtllib", "usemtl", "v ", "vt ", "vn ", "o ", "g ", "s ", "f " };
                return SearchFileHeaderForToken(ioHandler, file, tokens);
            }
        }

        public override ImporterDesc GetInfo()
        {
            return Desc;
        }

        void InternalReadFile(string filename, Scene scene, IOSystem ioHandler)
        {
            var file = ioHandler.Open(filename, FileMode.Open);
            if (file == null)
            {
                throw (new DeadlyImportError("Failed to open file " + filename + "."));
            }

            var fileSize = file.Length;
            if (fileSize < ObjMinSize)
            {
                throw new DeadlyImportError("OBJ-file is too small.");
            }

            Buffer = new StreamReader(file);

            string modelName;
            int pos = filename.LastIndexOf("\\/");
            if (pos >= 0)
            {
                modelName = filename.Substring(pos + 1, filename.Length - pos - 1);
            }
            else
            {
                modelName = filename;
            }

            var parser = new ObjFileParser(Buffer, modelName, filename);

            CreateDataFromImport(parser.GetModel(), scene);
        }

        void CreateDataFromImport(Model model, Scene scene)
        {
            if (null == model)
            {
                return;
            }

            scene.RootNode = new Node();
            if (string.IsNullOrEmpty(model.ModelName))
            {
                scene.RootNode.Name = model.ModelName;
            }
            else
            {
                Debug.Assert(false);
            }

            var meshArray = new List<AssimpSharp.Mesh>();
            for (int index = 0; index < model.Objects.Count; index++)
            {
                CreateNodes(model, model.Objects[index], scene.RootNode, scene, meshArray);
            }

            if (scene.Meshes.Count > 0)
            {
                scene.Meshes.AddRange(meshArray);
            }

            CreateMaterilas(model, scene);
        }

        Node CreateNodes(Model model, Object data, Node parent, Scene scene, List<AssimpSharp.Mesh> meshArray)
        {
            Debug.Assert(null != model);
            if (null == data)
            {
                return null;
            }

            var olsMeshSize = meshArray.Count;
            var node = new AssimpSharp.Node();
            node.Name = data.ObjName;

            if (parent != null)
            {
                AppendChildToParentNode(parent, node);
            }

            foreach (var meshId in data.Meshes)
            {
                var mesh = CreateTopology(model, data, (int)meshId);
                if (mesh != null && mesh.NumFaces > 0)
                {
                    meshArray.Add(mesh);
                }
            }

            if (data.SubObjects.Count > 0)
            {
                var numChilds = data.SubObjects.Count;

            }

            var meshSizeDiff = meshArray.Count - olsMeshSize;
            if (meshSizeDiff > 0)
            {
                int index = 0;
                for (int i = olsMeshSize; i < meshArray.Count; i++)
                {
                    throw (new NotImplementedException());
                }
            }

            return node;
        }

        AssimpSharp.Mesh CreateTopology(Model model, Object data, int meshIndex)
        {
            var objMesh = model.Meshes[meshIndex];
            if (objMesh == null)
            {
                return null;
            }
            var mesh = new AssimpSharp.Mesh();
            if (!string.IsNullOrEmpty(objMesh.Name))
            {
                mesh.Name = objMesh.Name;
            }

            for (int index = 0; index < objMesh.Faces.Count; index++)
            {
                var inp = objMesh.Faces[index];
                if (inp.PrimitiveType == PrimitiveType.Line)
                {
                    mesh.NumFaces += inp.Vertices.Count;
                    mesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Line;
                }
                else if (inp.PrimitiveType == PrimitiveType.Point)
                {
                    mesh.NumFaces += inp.Vertices.Count;
                    mesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Point;
                }
                else
                {
                    mesh.NumFaces++;
                    if (inp.Vertices.Count > 3)
                    {
                        mesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Polygon;
                    }
                    else
                    {
                        mesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Triangle;
                    }
                }
            }

            int indexCount = 0;
            if (mesh.NumFaces > 0)
            {
                mesh.Faces = new AssimpSharp.Face[mesh.NumFaces];
                if (objMesh.MaterialIndex != Mesh.NoMaterial)
                {
                    mesh.MaterialIndex = (int)objMesh.MaterialIndex;
                }

                int outIndex = 0;

                // Copy all data from all stored meshes
                for (int index = 0; index < objMesh.Faces.Count; index++)
                {
                    var inp = objMesh.Faces[index];
                    if (inp.PrimitiveType == PrimitiveType.Line)
                    {
                        for (int i = 0; i < inp.Vertices.Count - 1; i++)
                        {
                            var f = mesh.Faces[outIndex++];
                            indexCount += 2;
                            f.Indices = new int[2];
                        }
                        continue;
                    }
                    else if (inp.PrimitiveType == PrimitiveType.Point)
                    {
                        for (int i = 0; i < inp.Vertices.Count; i++)
                        {
                            var f = mesh.Faces[outIndex++];
                            indexCount += 1;
                            f.Indices = new int[1];
                        }
                        continue;
                    }

                    var face = mesh.Faces[outIndex++];
                    var numIndices = objMesh.Faces[index].Vertices.Count;
                    indexCount += numIndices;
                    if (face.Indices.Length > 0)
                    {
                        face.Indices = new int[numIndices];
                    }
                }
            }

            // Create mesh vertices
            CreateVertexArray(model, data, meshIndex, mesh, indexCount);

            return mesh;
        }

        void CreateVertexArray(Model model, Object currentObject, int meshIndex, AssimpSharp.Mesh mesh, int numIndices)
        {
            Debug.Assert(null != currentObject);

            if (currentObject.Meshes.Count == 0)
            {
                return;
            }

            var objMesh = model.Meshes[meshIndex];
            if (null == objMesh || objMesh.NumIndices < 1)
            {
                return;
            }

            mesh.NumVertices = numIndices;
            if (mesh.NumVertices == 0)
            {
                throw (new DeadlyImportError("OBJ: no vertices"));
            }
            else if (mesh.NumVertices > int.MaxValue)
            {
                throw (new DeadlyImportError("OBJ: Too many vertices, would run out of memory"));
            }
            mesh.Vertices = new Vector3[mesh.NumVertices];

            if (model.Normals.Count != 0 && objMesh.HasNormals)
            {
                mesh.Normals = new Vector3[mesh.NumVertices];
            }

            if (model.TextureCoord.Count > 0 && objMesh.UVCoordinates[0] > 0)
            {
                mesh.NumUVComponents[0] = 2;
                mesh.TextureCoords[0] = new Vector3[mesh.NumVertices];
            }

            int newIndex = 0;
            int outIndex = 0;
            for (int index = 0; index < objMesh.Faces.Count; index++)
            {
                var sourceFace = objMesh.Faces[index];

                for (int vertexIndex = 0, outVertexIndex = 0; vertexIndex < sourceFace.Vertices.Count; vertexIndex++)
                {
                    int vertex = (int)sourceFace.Vertices[vertexIndex];
                    if (vertexIndex >= model.Vertices.Count)
                    {
                        throw (new DeadlyImportError("OBJ: vertex index out of range"));
                    }
                    mesh.Vertices[newIndex] = model.Vertices[vertex];


                }
            }
        }

        void CountObjects(List<Object> objects, out int numMeshes)
        {
            numMeshes = 0;
            if (objects.Count == 0)
            {
                return;
            }

            numMeshes += objects.Count;
            foreach (var it in objects)
            {
                if (it.SubObjects.Count > 0)
                {
                    CountObjects(it.SubObjects, out numMeshes);
                }
            }
        }

        /// <summary>
        /// Creates the material
        /// </summary>
        void CreateMaterilas(Model model, Scene scene)
        {
            Debug.Assert(null != scene);
            {
                return;
            }

            int numMaterials = model.MaterialLib.Count;
            scene.Materials.Clear();
            if (model.MaterialLib.Count == 0)
            {
                Debug.WriteLine("OBJ: no materials specified");
                return;
            }

            for (int matIndex = 0; matIndex < numMaterials; matIndex++)
            {
                // Store material name
                // No material found, use the default material
                Material currentMaterial;
                if (!model.MaterialMap.TryGetValue(model.MaterialLib[matIndex], out currentMaterial))
                {
                    continue;
                }

                var mat = new AssimpSharp.Material();
                mat.Name = currentMaterial.MaterialName;

                // convert illumination model
                ShadingMode sm = 0;
                switch (currentMaterial.IlluminationModel)
                {
                    case 0:
                        sm = ShadingMode.NoShading;
                        break;
                    case 1:
                        sm = ShadingMode.Gouraud;
                        break;
                    case 2:
                        sm = ShadingMode.Phong;
                        break;
                    default:
                        sm = ShadingMode.Gouraud;
                        Debug.WriteLine("OBJ: unexpected illumination model (0-2 recognized)");
                        break;
                }
                mat.ShadingMode = sm;

                // multiplying the specular exponent with 2 seems to yield better results
                currentMaterial.Shineness *= 4f;

                // Adding material colors
                mat.ColorAmbient = new Color4(currentMaterial.Ambient);
                mat.ColorDiffuse = new Color4(currentMaterial.Diffuse);
                mat.ColorSpecular = new Color4(currentMaterial.Specular);
                mat.ColorEmissive = new Color4(currentMaterial.Emissive);
                mat.Shininess = currentMaterial.Shineness;
                mat.Opacity = currentMaterial.Alpha;

                // Adding refraction index
                mat.Reflectivity = currentMaterial.IOR;

                // Adding textures
                if (!string.IsNullOrEmpty(currentMaterial.Texture))
                {
                    mat.TextureDiffuse = new TextureSlot()
                    {
                        TextureBase = currentMaterial.Texture
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.DiffuseType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Diffuse);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureAmbient))
                {
                    mat.TextureAmbient = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureAmbient
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.AmbientType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Ambient);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureEmissive))
                {
                    mat.TextureAmbient = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureEmissive
                    };
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureSpecular))
                {
                    mat.TextureSpecular = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureSpecular
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.SpecularType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Specular);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureBump))
                {
                    mat.TextureHeight = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureBump
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.BumpType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Height);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureNormal))
                {
                    mat.TextureNormals = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureNormal
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.NormalType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Normals);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureDisp))
                {
                    mat.TextureDisplacement = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureDisp
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.DispType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Displacement);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureOpacity))
                {
                    mat.TextureOpacity = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureOpacity
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.OpacityType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Opacity);
                    }
                }

                if (!string.IsNullOrEmpty(currentMaterial.TextureSpecularity))
                {
                    mat.TextureShininess = new TextureSlot()
                    {
                        TextureBase = currentMaterial.TextureSpecularity
                    };
                    if (currentMaterial.Clamp[(int)Material.TextureType.SpecularityType])
                    {
                        AddTextureMappingModeProperty(mat, TextureType.Shininess);
                    }
                }

                // Store material property info in material array in scene
                scene.Materials.Add(mat);
            }

            // Test number of created materials.
            Debug.Assert(scene.Materials.Count == numMaterials);
        }

        /// <summary>
        /// Add clamp mode property to material if necessary
        /// </summary>
        void AddTextureMappingModeProperty(AssimpSharp.Material mat, AssimpSharp.TextureType type, int clampMode = 1)
        {
            Debug.Assert(null != mat);
            mat.TextureSlotCollection[type].MappingModeU = (TextureMapMode)clampMode;
            mat.TextureSlotCollection[type].MappingModeV = (TextureMapMode)clampMode;
        }

        /// <summary>
        /// Appends this node to the parent node
        /// </summary>
        void AppendChildToParentNode(AssimpSharp.Node parent, AssimpSharp.Node child)
        {
            // Checking preconditions
            Debug.Assert(null != parent);
            Debug.Assert(null != child);

            // Assign parent to child
            child.Parent = parent;

            parent.Children.Add(child);
        }
    }
}

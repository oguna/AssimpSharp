using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System;
using System.Diagnostics;
using Color4D = SharpDX.Color4;
using Color3D = SharpDX.Color3;
using Vector2D = SharpDX.Vector2;
using Vector3D = SharpDX.Vector3;
using Matrix4x4 = SharpDX.Matrix;
using Quaternion = SharpDX.Quaternion;
using VectorKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Vector3>;
using QuatKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Quaternion>;
using MatrixKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Matrix>;


namespace AssimpSharp.XFile
{
    public class XFileParser
    {
        public const uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;
        public const uint AI_MAX_NUMBER_OF_COLOR_SETS = 4;
        const uint MSZIP_MAGIC = 0x4B43;
        const uint MSZIP_BLOCK = 32786;

        /// <summary>
        /// Constructor. Creates a data structure out of the XFile given in the memory block.
        /// </summary>
        /// <param name="buffer"></param>
        public XFileParser(byte[] buffer)
        {
            this.buffer = buffer;
            majorVersion = minorVersion = 0;
            isBinaryFormat = false;
            binaryNumCount = 0;
            p = end = -1;
            lineNumber = 0;
            scene = null;

            // vector to store uncompressed file for INFLATE'd X files
            byte[] uncompressed;

            // set up memory pointers
            p = 0;
            end = buffer.Length;

            string header = Encoding.Default.GetString(buffer, 0, 16);
            // check header
            if (header.Substring(0, 4) != "xof ")
                throw new Exception("Header mismatch, file is not an XFile.");

            // read version. It comes in a four byte format such as "0302"
            majorVersion = uint.Parse(header.Substring(4, 2));
            minorVersion = uint.Parse(header.Substring(6, 2));

            bool compressed = false;

            // txt - pure ASCII text format
            if (header.Substring(8, 4) == "txt ")
            {
                isBinaryFormat = false;
            }// bin - Binary format
            else if (header.Substring(8, 4) == "bin ")
            {
                isBinaryFormat = true;
            }
            // tzip - Inflate compressed text format
            else if (header.Substring(8, 4) == "tzip")
            {
                isBinaryFormat = false;
                compressed = true;
            }
            // bzip - Inflate compressed binary format
            else if (header.Substring(8, 4) == "bzip")
            {
                isBinaryFormat = true;
                compressed = true;
            }
            else ThrowException(string.Format("Unsupported xfile format '{0}'", header.Substring(8, 4)));

            // float size
            binaryFloatSize = uint.Parse(header.Substring(12, 4));

            if (binaryFloatSize != 32 && binaryFloatSize != 64)
                ThrowException(string.Format("Unknown float size {0} specified in xfile header.", binaryFloatSize));

            // The x format specifies size in bits, but we work in bytes
            binaryFloatSize /= 8;

            p += 16;

            // If this is a compressed X file, apply the inflate algorithm to it
            if (compressed)
            {
                //throw (new Exception("Assimp was built without compressed X support"));
                MemoryStream stream = new MemoryStream(buffer);
                stream.Position += 16;
                stream.Position += 6;
                long p1 = stream.Position;
                uint estOut = 0;
                while (p1 + 3 < end)
                {
                    ushort ofs = BitConverter.ToUInt16(buffer, (int)p1);
                    p1 += 2;
                    if (ofs >= MSZIP_BLOCK)
                    {
                        throw (new Exception("X: Invalid offset to next MSZIP compressed block"));
                    }
                    ushort magic = BitConverter.ToUInt16(buffer, (int)p1);
                    p1 += 2;
                    if (magic != MSZIP_MAGIC)
                    {
                        throw (new Exception("X: Unsupported compressed format, expected MSZIP header"));
                    }
                    p1 += ofs;
                    estOut += MSZIP_BLOCK;
                }

                uncompressed = new byte[estOut + 1];
                int uncompressedEnd = 0;
                while (p + 3 < end)
                {
                    ushort ofs = BitConverter.ToUInt16(buffer, (int)p);
                    p += 4;
                    if (p + ofs > end + 2)
                    {
                        throw (new Exception("X: Unexpected EOF in compressed chunk"));
                    }
                    stream.Position = p;
                    DeflateStream uncomp = new DeflateStream(stream, CompressionMode.Decompress);
                    int readLnegth = uncomp.Read(uncompressed, 0, (int)MSZIP_BLOCK);
                    uncompressedEnd += readLnegth;
                    p += ofs;
                }
                this.buffer = uncompressed;
                this.end = uncompressedEnd;
                this.p = 0;
            }
            else
            {
                // start reading here
                ReadUntilEndOfLine();
            }

            scene = new Scene();
            ParseFile();

            // filter the imported hierarchy for some degenerated cases
            if (scene.RootNode != null)
            {
                FilterHierarchy(scene.RootNode);
            }
        }

        ~XFileParser()
        {
        }

        public Scene GetImportedData()
        {
            return scene;
        }

        protected void ParseFile()
        {
            bool runnning = true;
            while (runnning)
            {
                // read name of next object
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                {
                    break;
                }

                // parse specific object
                if (objectName == "template")
                {
                    ParseDataObjectTemplate();
                }
                else if (objectName == "Frame")
                {
                    ParseDataObjectFrame(null);
                }
                else if (objectName == "Mesh")
                {
                    // some meshes have no frames at all
                    Mesh mesh;
                    ParseDataObjectMesh(out mesh);
                    scene.GlobalMeshes.Add(mesh);
                }
                else if (objectName == "AnimTicksPerSecond")
                {
                    ParseDataObjectAnimTicksPerSecond();
                }
                else if (objectName == "AnimationSet")
                {
                    ParseDataObjectAnimationSet();
                }
                else if (objectName == "Material")
                {
                    // Material outside of a mesh or node
                    Material material;
                    ParseDataObjectMaterial(out material);
                    scene.GlobalMaterial.Add(material);
                }
                else if (objectName == "}")
                {
                    // whatever?
                    Debug.WriteLine("} found in dataObject");
                }
                else
                {
                    // unknown format
                    Debug.WriteLine("Unknown data object in animation of .x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectTemplate()
        {
            // parse a template data object. Currently not stored.
            string name;
            ReadHeadOfDataObject(out name);

            // read GUID
            string guid = GetNextToken();

            // read and ignore data members
            bool running = true;
            while (running)
            {
                string s = GetNextToken();
                if (s == "}")
                    break;
                if (s.Length == 0)
                    ThrowException("Unexpected end of file reached while parsing template definition");
            }
        }

        protected void ParseDataObjectFrame(Node parent)
        {
            string name;
            ReadHeadOfDataObject(out name);

            // create a named node and place it at its parent, if given
            Node node = new Node(parent);
            node.Name = name;
            if (parent != null)
            {
                parent.Children.Add(node);
            }
            else
            {
                // there might be multiple root nodes
                if (scene.RootNode != null)
                {
                    if (scene.RootNode.Name != "$dummy_root")
                    {
                        // place a dummy root if not there
                        Node exroot = scene.RootNode;
                        scene.RootNode = new Node(null);
                        scene.RootNode.Name = "$dummy_root";
                        scene.RootNode.Children.Add(exroot);
                        exroot.Parent = scene.RootNode;
                    }
                    // put the new node as its child instead
                    scene.RootNode.Children.Add(node);
                    node.Parent = scene.RootNode;
                }
                else
                {
                    // it's the first node imported. place it as root
                    scene.RootNode = node;
                }
            }

            // Now inside a frame.
            // read tokens until closing brace is reached.
            bool running = true;
            while (running)
            {
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                {
                    ThrowException("Unexpected end of file reached while parsing frame");
                }

                if (objectName == "}")
                {
                    break; // frame finished
                }
                else if (objectName == "Frame")
                {
                    ParseDataObjectFrame(node); // child frame
                }
                else if (objectName == "FrameTransformMatrix")
                {
                    ParseDataObjectTransformationMatrix(out node.TrafoMatrix);
                }
                else if (objectName == "Mesh")
                {
                    Mesh mesh;
                    ParseDataObjectMesh(out mesh);
                    node.Meshes.Add(mesh);
                }
                else
                {
                    Debug.WriteLine("Unknown data object in frame in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectTransformationMatrix(out Matrix4x4 matrix)
        {
            // read header, we're not interested if it has a name
            ReadHeadOfDataObject();

            // read its components
            matrix = new Matrix4x4();
            matrix.M11 = ReadFloat(); matrix.M21 = ReadFloat();
            matrix.M31 = ReadFloat(); matrix.M41 = ReadFloat();
            matrix.M12 = ReadFloat(); matrix.M22 = ReadFloat();
            matrix.M32 = ReadFloat(); matrix.M42 = ReadFloat();
            matrix.M13 = ReadFloat(); matrix.M23 = ReadFloat();
            matrix.M33 = ReadFloat(); matrix.M43 = ReadFloat();
            matrix.M14 = ReadFloat(); matrix.M24 = ReadFloat();
            matrix.M34 = ReadFloat(); matrix.M44 = ReadFloat();

            // trailing symbols
            CheckForSemicolon();
            CheckForClosingBrace();
        }

        protected void ParseDataObjectMesh(out Mesh mesh)
        {
            mesh = new Mesh();
            string name;
            ReadHeadOfDataObject(out name);

            // read vertex count
            uint numVertices = ReadInt();
            mesh.Positions = new List<Vector3D>((int)numVertices);

            // read vertices
            for (int a = 0; a < numVertices; a++)
                mesh.Positions.Add(ReadVector3());

            // read position faces
            uint numPosFaces = ReadInt();
            mesh.PosFaces = new List<Face>((int)numPosFaces);
            for (uint a = 0; a < numPosFaces; a++)
            {
                uint numIndices = ReadInt();
                if (numIndices < 3)
                    ThrowException(string.Format("Invalid index count {0} for face {1}.", numIndices, a));

                // read indices
                Face face = new Face();
                face.Indices = new List<uint>();
                for (uint b = 0; b < numIndices; b++)
                    face.Indices.Add(ReadInt());
                mesh.PosFaces.Add(face);
                TestForSeparator();
            }

            // here, other data objects may follow
            bool running = true;
            while (running)
            {
                string objectName = GetNextToken();

                if (objectName.Length == 0)
                    ThrowException("Unexpected end of file while parsing mesh structure");
                else if (objectName == "}")
                    break; // mesh finished
                else if (objectName == "MeshNormals")
                    ParseDataObjectMeshNormals(ref mesh);
                else if (objectName == "MeshTextureCoords")
                    ParseDataObjectMeshTextureCoords(ref mesh);
                else if (objectName == "MeshVertexColors")
                    ParseDataObjectMeshVertexColors(ref mesh);
                else if (objectName == "MeshMaterialList")
                    ParseDataObjectMeshMaterialList(ref mesh);
                else if (objectName == "VertexDuplicationIndices")
                    ParseUnknownDataObject(); // we'll ignore vertex duplication indices
                else if (objectName == "XSkinMeshHeader")
                    ParseDataObjectSkinMeshHeader(ref mesh);
                else if (objectName == "SkinWeights")
                    ParseDataObjectSkinWeights(ref mesh);
                else
                {
                    Debug.WriteLine("Unknown data object in mesh in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectSkinWeights(ref Mesh mesh)
        {
            ReadHeadOfDataObject();

            string transformNodeName;
            GetNextTokenAsString(out transformNodeName);

            Bone bone = new Bone();
            mesh.Bones.Add(bone);
            bone.Name = transformNodeName;

            // read vertex weights
            uint numWeights = ReadInt();

            for (uint a = 0; a < numWeights; a++)
            {
                BoneWeight weight = new BoneWeight();
                weight.Vertex = ReadInt();
                bone.Weights.Add(weight);
            }

            // read vertex weights
            for (int a = 0; a < numWeights; a++)
            {
                BoneWeight bw = bone.Weights[a];
                bw.Weight = ReadFloat();
                bone.Weights[a] = bw;
            }

            // read matrix offset
            bone.OffsetMatrix = new Matrix4x4();
            bone.OffsetMatrix.M11 = ReadFloat(); bone.OffsetMatrix.M21 = ReadFloat();
            bone.OffsetMatrix.M31 = ReadFloat(); bone.OffsetMatrix.M41 = ReadFloat();
            bone.OffsetMatrix.M12 = ReadFloat(); bone.OffsetMatrix.M22 = ReadFloat();
            bone.OffsetMatrix.M33 = ReadFloat(); bone.OffsetMatrix.M42 = ReadFloat();
            bone.OffsetMatrix.M13 = ReadFloat(); bone.OffsetMatrix.M23 = ReadFloat();
            bone.OffsetMatrix.M33 = ReadFloat(); bone.OffsetMatrix.M43 = ReadFloat();
            bone.OffsetMatrix.M14 = ReadFloat(); bone.OffsetMatrix.M24 = ReadFloat();
            bone.OffsetMatrix.M33 = ReadFloat(); bone.OffsetMatrix.M44 = ReadFloat();

            CheckForSemicolon();
            CheckForClosingBrace();

        }

        protected void ParseDataObjectSkinMeshHeader(ref Mesh mesh)
        {
            ReadHeadOfDataObject();
            ReadInt();
            ReadInt();
            ReadInt();
            CheckForClosingBrace();
        }

        protected void ParseDataObjectMeshNormals(ref Mesh mesh)
        {
            ReadHeadOfDataObject();

            // read count
            uint numNormals = ReadInt();
            mesh.Normals = new List<Vector3D>((int)numNormals);

            // read normal vectors
            for (uint a = 0; a < numNormals; a++)
                mesh.Normals.Add(ReadVector3());

            // read normal indices
            uint numFaces = ReadInt();
            if (numFaces != mesh.PosFaces.Count)
                ThrowException("Normal face count does not match vertex face count.");

            for (uint a = 0; a < numFaces; a++)
            {
                uint numIndices = ReadInt();
                Face face = new Face();
                face.Indices = new List<uint>();
                for (uint b = 0; b < numIndices; b++)
                    face.Indices.Add(ReadInt());
                mesh.NormalFaces.Add(face);

                TestForSeparator();
            }

            CheckForClosingBrace();
        }

        protected void ParseDataObjectMeshTextureCoords(ref Mesh mesh)
        {
            ReadHeadOfDataObject();
            if (mesh.NumTextures + 1 > AI_MAX_NUMBER_OF_TEXTURECOORDS)
            {
                ThrowException("Too many sets of texture coordinates");
            }

            uint numCoords = ReadInt();
            if (numCoords != mesh.Positions.Count)
            {
                ThrowException("Texture coord count does not match vertex count");
            }

            List<Vector2D> coords = new List<Vector2D>((int)numCoords);
            for (int a = 0; a < numCoords; a++)
            {
                coords.Add(ReadVector2());
            }
            mesh.TexCoords[mesh.NumTextures++] = coords;

            CheckForClosingBrace();
        }

        protected void ParseDataObjectMeshVertexColors(ref Mesh mesh)
        {
            ReadHeadOfDataObject();
            if (mesh.NumColorSets + 1 > AI_MAX_NUMBER_OF_COLOR_SETS)
                ThrowException("Too many colorsets");
            Color4D[] colors;

            uint numColors = ReadInt();
            if (numColors != mesh.Positions.Count)
                ThrowException("Vertex color count does not match vertex count");

            //colors.resize( numColors, aiColor4D( 0, 0, 0, 1));
            colors = new Color4D[numColors];

            for (uint a = 0; a < numColors; a++)
            {
                uint index = ReadInt();
                if (index >= mesh.Positions.Count)
                    ThrowException("Vertex color index out of bounds");

                colors[(int)index] = ReadRGBA();
                // HACK: (thom) Maxon Cinema XPort plugin puts a third separator here, kwxPort puts a comma.
                // Ignore gracefully.
                if (!isBinaryFormat)
                {
                    FindNextNoneWhiteSpace();
                    if (buffer[p] == ';' || buffer[p] == ',')
                        p++;
                }
            }
            mesh.Colors[(int)mesh.NumColorSets] = new List<Color4D>(colors);
            CheckForClosingBrace();
        }

        protected void ParseDataObjectMeshMaterialList(ref Mesh mesh)
        {
            ReadHeadOfDataObject();

            // read material count
            /*unsigned int numMaterials =*/
            ReadInt();
            // read non triangulated face material index count
            uint numMatIndices = ReadInt();

            // some models have a material index count of 1... to be able to read them we
            // replicate this single material index on every face
            if (numMatIndices != mesh.PosFaces.Count && numMatIndices != 1)
                ThrowException("Per-Face material index count does not match face count.");

            // read per-face material indices
            for (uint a = 0; a < numMatIndices; a++)
                mesh.FaceMaterials.Add(ReadInt());

            // in version 03.02, the face indices end with two semicolons.
            // commented out version check, as version 03.03 exported from blender also has 2 semicolons
            if (!isBinaryFormat) // && MajorVersion == 3 && MinorVersion <= 2)
            {
                if (p < end && buffer[p] == ';')
                    ++p;
            }

            // if there was only a single material index, replicate it on all faces
            while (mesh.FaceMaterials.Count < mesh.PosFaces.Count)
                mesh.FaceMaterials.Add(mesh.FaceMaterials[0]);

            // read following data objects
            bool running = true;
            while (running)
            {
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                    ThrowException("Unexpected end of file while parsing mesh material list.");
                else if (objectName == "}")
                    break; // material list finished
                else if (objectName == "{")
                {
                    // template materials 
                    string matName = GetNextToken();
                    Material material = new Material();
                    material.IsReference = true;
                    material.Name = matName;
                    mesh.Materials.Add(material);

                    CheckForClosingBrace(); // skip }
                }
                else if (objectName == "Material")
                {
                    Material material;
                    ParseDataObjectMaterial(out material);
                    mesh.Materials.Add(material);
                }
                else if (objectName == ";")
                {
                    // ignore
                }
                else
                {
                    Debug.WriteLine("Unknown data object in material list in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectMaterial(out Material material)
        {
            string matName;
            ReadHeadOfDataObject(out matName);
            if (matName.Length == 0)
            {
                matName = "material" + lineNumber;
            }
            material = new Material();
            material.Name = matName;
            material.IsReference = false;

            // read material values
            material.Diffuse = ReadRGBA();
            material.SpecularExponent = ReadFloat();
            material.Specular = ReadRGB();
            material.Emissive = ReadRGB();

            // read other data objects
            bool running = true;
            material.Textures = new List<TexEntry>();
            while (running)
            {
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                    ThrowException("Unexpected end of file while parsing mesh material");
                else if (objectName == "}")
                {
                    break; // material finished
                }
                else if (objectName == "TextureFilename" || objectName == "TextureFileName")
                {
                    // some exporters write "TextureFileName" instead.
                    string texname = string.Empty;
                    ParseDataObjectTextureFilename(ref texname);
                    material.Textures.Add(new TexEntry(texname));
                }
                else if (objectName == "NormalmapFilename" || objectName == "NormalmapFileName")
                {
                    // one exporter writes out the normal map in a separate filename tag
                    string texname = string.Empty;
                    ParseDataObjectTextureFilename(ref texname);
                    material.Textures.Add(new TexEntry(texname, true));
                }
                else
                {
                    Debug.WriteLine("Unknown data object in material in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectAnimTicksPerSecond()
        {
            ReadHeadOfDataObject();
            scene.AnimTicksPerSecond = ReadInt();
            CheckForClosingBrace();
        }

        protected void ParseDataObjectAnimationSet()
        {
            string animName;
            ReadHeadOfDataObject(out animName);

            Animation anim = new Animation();
            scene.Anims.Add(anim);
            anim.Name = animName;

            bool running = true;
            while (running)
            {
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                {
                    ThrowException("Unexpected end of file while parsing animation set.");
                }
                else if (objectName == "}")
                {
                    break; // animation set finished
                }
                else if (objectName == "Animation")
                {
                    ParseDataObjectAnimation(anim);
                }
                else
                {
                    Debug.WriteLine("Unknown data object in animation set in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectAnimation(Animation pAnim)
        {
            ReadHeadOfDataObject();
            AnimBone banim = new AnimBone();
            pAnim.Anims.Add(banim);

            bool running = true;
            while (running)
            {
                string objectName = GetNextToken();
                if (objectName.Length == 0)
                {
                    ThrowException("Unexpected end of file while parsing animation.");
                }
                else if (objectName == "}")
                {
                    break; // animation finished
                }
                else if (objectName == "AnimationKey")
                {
                    ParseDataObjectAnimationKey(banim);
                }
                else if (objectName == "AnimationOptions")
                {
                    ParseUnknownDataObject(); // not interested
                }
                else if (objectName == "{")
                {
                    // read frame name
                    banim.BoneName = GetNextToken();
                    CheckForClosingBrace();
                }
                else
                {
                    Debug.WriteLine("Unknown data object in animation in x file");
                    ParseUnknownDataObject();
                }
            }
        }

        protected void ParseDataObjectAnimationKey(AnimBone animBone)
        {
            ReadHeadOfDataObject();

            // read key type
            uint keyType = ReadInt();

            // read number of keys
            uint numKeys = ReadInt();

            for (uint a = 0; a < numKeys; a++)
            {
                // read time
                uint time = ReadInt();

                // read keys
                switch (keyType)
                {
                    case 0:
                        {
                            if (ReadInt() != 4)
                                ThrowException("Invalid number of arguments for quaternion key in animation");

                            var keyValue = new Quaternion();
                            keyValue.W = ReadFloat();
                            keyValue.X = ReadFloat();
                            keyValue.Y = ReadFloat();
                            keyValue.Z = ReadFloat();
                            QuatKey key = new QuatKey((double)time, keyValue);
                            animBone.RotKeys.Add(key);

                            CheckForSemicolon();
                            break;
                        }
                    case 1: // scale vector
                    case 2: // position vector
                        {
                            // read count
                            if (ReadInt() != 3)
                            {
                                ThrowException("Invalid number of arguments for vector key in animation");
                            }
                            VectorKey key = new VectorKey((double)time, ReadVector3());

                            if (keyType == 2)
                            {
                                animBone.PosKeys.Add(key);
                            }
                            else
                            {
                                animBone.ScaleKeys.Add(key);
                            }
                            break;
                        }
                    case 3:
                    case 4:
                        {
                            // read count
                            if (ReadInt() != 16)
                                ThrowException("Invalid number of arguments for matrix key in animation");

                            // read matrix
                            double key = (double)time;
                            Matrix4x4 value = new Matrix4x4();
                            value.M11 = ReadFloat(); value.M21 = ReadFloat();
                            value.M31 = ReadFloat(); value.M41 = ReadFloat();
                            value.M12 = ReadFloat(); value.M22 = ReadFloat();
                            value.M32 = ReadFloat(); value.M42 = ReadFloat();
                            value.M13 = ReadFloat(); value.M23 = ReadFloat();
                            value.M33 = ReadFloat(); value.M43 = ReadFloat();
                            value.M14 = ReadFloat(); value.M24 = ReadFloat();
                            value.M34 = ReadFloat(); value.M44 = ReadFloat();
                            animBone.TrafoKeys.Add(new MatrixKey(key, value));
                            CheckForSemicolon();
                            break;
                        }
                    default:
                        ThrowException(string.Format("Unknown key type {0} in animation.", keyType));
                        break;
                }
                CheckForSeparator();
            }
            CheckForClosingBrace();
        }

        protected void ParseDataObjectTextureFilename(ref string name)
        {
            ReadHeadOfDataObject();
            GetNextTokenAsString(out name);
            CheckForClosingBrace();

            // FIX: some files (e.g. AnimationTest.x) have "" as texture file name
            if (name.Length == 0)
            {
                Debug.WriteLine("Length of texture file name is zero. Skipping this texture.");
            }

            // some exporters write double backslash paths out. We simply replace them if we find them
            while (name.Contains("\\\\"))
            {
                name = name.Remove(name.IndexOf("\\\\"), 1);
            }
        }

        protected void ParseUnknownDataObject()
        {
            // find opening delimiter
            bool running = true;
            while (running)
            {
                string t = GetNextToken();
                if (t.Length == 0)
                    ThrowException("Unexpected end of file while parsing unknown segment.");

                if (t == "{")
                    break;
            }

            uint counter = 1;

            // parse until closing delimiter
            while (counter > 0)
            {
                string t = GetNextToken();

                if (t.Length == 0)
                    ThrowException("Unexpected end of file while parsing unknown segment.");

                if (t == "{")
                    ++counter;
                else if (t == "}")
                    --counter;
            }
        }

        /// <summary>
        /// places pointer to next begin of a token, and ignores comments
        /// </summary>
        protected void FindNextNoneWhiteSpace()
        {
            if (isBinaryFormat)
                return;

            bool running = true;
            while (running)
            {
                while ((p < end) && (char.IsSeparator((char)buffer[p]) || char.IsControl((char)buffer[p])))
                {
                    if (buffer[p] == '\n')
                        lineNumber++;
                    ++p;
                }

                if (p >= end)
                    return;

                // check if this is a comment
                if ((buffer[p] == '/' && buffer[p + 1] == '/') || buffer[p] == '#')
                    ReadUntilEndOfLine();
                else
                    break;
            }
        }

        protected string GetNextToken()
        {
            string s = string.Empty;

            // process binary-formatted file
            if (isBinaryFormat)
            {
                // in binary mode it will only return NAME and STRING token
                // and (correctly) skip over other tokens.

                if (end - p < 2) return s;
                uint tok = ReadBinWord();
                uint len;

                // standalone tokens
                switch (tok)
                {
                    case 1:
                        // name token
                        if (end - p < 4) return s;
                        len = ReadBinDWord();
                        if (end - p < (int)len) return s;
                        s = Encoding.Default.GetString(buffer, p, (int)len);
                        p += (int)len;
                        return s;
                    case 2:
                        // string token
                        if (end - p < 4) return s;
                        len = ReadBinDWord();
                        if (end - p < (int)len) return s;
                        s = Encoding.Default.GetString(buffer, p, (int)len);
                        p += ((int)len + 2);
                        return s;
                    case 3:
                        // integer token
                        p += 4;
                        return "<integer>";
                    case 5:
                        // GUID token
                        p += 16;
                        return "<guid>";
                    case 6:
                        if (end - p < 4) return s;
                        len = ReadBinDWord();
                        p += ((int)len * 4);
                        return "<int_list>";
                    case 7:
                        if (end - p < 4) return s;
                        len = ReadBinDWord();
                        p += (int)(len * binaryFloatSize);
                        return "<flt_list>";
                    case 0x0a:
                        return "{";
                    case 0x0b:
                        return "}";
                    case 0x0c:
                        return "(";
                    case 0x0d:
                        return ")";
                    case 0x0e:
                        return "[";
                    case 0x0f:
                        return "]";
                    case 0x10:
                        return "<";
                    case 0x11:
                        return ">";
                    case 0x12:
                        return ".";
                    case 0x13:
                        return ",";
                    case 0x14:
                        return ";";
                    case 0x1f:
                        return "template";
                    case 0x28:
                        return "WORD";
                    case 0x29:
                        return "DWORD";
                    case 0x2a:
                        return "FLOAT";
                    case 0x2b:
                        return "DOUBLE";
                    case 0x2c:
                        return "CHAR";
                    case 0x2d:
                        return "UCHAR";
                    case 0x2e:
                        return "SWORD";
                    case 0x2f:
                        return "SDWORD";
                    case 0x30:
                        return "void";
                    case 0x31:
                        return "string";
                    case 0x32:
                        return "unicode";
                    case 0x33:
                        return "cstring";
                    case 0x34:
                        return "array";
                }
            }
            // process text-formatted file
            else
            {
                FindNextNoneWhiteSpace();
                if (p >= end)
                    return s;

                while ((p < end) && !(char.IsSeparator((char)buffer[p]) || char.IsControl((char)buffer[p])))
                {
                    // either keep token delimiters when already holding a token, or return if first valid char
                    if (buffer[p] == ';' || buffer[p] == '}' || buffer[p] == '{' || buffer[p] == ',')
                    {
                        if (s.Length == 0)
                            s += (char)buffer[p++];
                        break; // stop for delimiter
                    }
                    s += (char)buffer[p++];
                }
            }
            return s;
        }

        protected void ReadHeadOfDataObject(out string name)
        {
            name = string.Empty;
            string nameOrBrace = GetNextToken();
            if (nameOrBrace != "{")
            {
                name = nameOrBrace;

                if (GetNextToken() != "{")
                    ThrowException("Opening brace expected.");
            }
        }


        protected void ReadHeadOfDataObject()
        {
            string nameOrBrace = GetNextToken();
            if (nameOrBrace != "{")
            {
                if (GetNextToken() != "{")
                    ThrowException("Opening brace expected.");
            }
        }

        /// <summary>
        /// checks for closing curly brace, throws exception if not there
        /// </summary>
        protected void CheckForClosingBrace()
        {
            if (GetNextToken() != "}")
                ThrowException("Closing brace expected.");
        }

        /// <summary>
        /// checks for one following semicolon, throws exception if not there
        /// </summary>
        protected void CheckForSemicolon()
        {
            if (isBinaryFormat)
                return;

            if (GetNextToken() != ";")
                ThrowException("Semicolon expected.");
        }

        /// <summary>
        /// checks for a separator char, either a ',' or a ';'
        /// </summary>
        protected void CheckForSeparator()
        {
            if (isBinaryFormat)
                return;

            string token = GetNextToken();
            if (token != "," && token != ";")
                ThrowException("Separator character (';' or ',') expected.");
        }

        /// <summary>
        /// tests and possibly consumes a separator char, but does nothing if there was no separator
        /// </summary>
        protected void TestForSeparator()
        {
            if (isBinaryFormat)
                return;

            FindNextNoneWhiteSpace();
            if (p >= end)
                return;

            // test and skip
            if (buffer[p] == ';' || buffer[p] == ',')
                p++;
        }

        /// <summary>
        ///  reads a x file style string
        /// </summary>
        protected void GetNextTokenAsString(out string poString)
        {
            if (isBinaryFormat)
            {
                poString = GetNextToken();
                return;
            }

            FindNextNoneWhiteSpace();
            if (p >= end)
                ThrowException("Unexpected end of file while parsing string");

            if (buffer[p] != '"')
                ThrowException("Expected quotation mark.");
            ++p;
            int startPos = p;

            while (p < end && buffer[p] != '"')
                p++;
            poString = Encoding.Default.GetString(buffer, startPos, p - startPos);

            if (p >= end - 1)
                ThrowException("Unexpected end of file while parsing string");

            if (buffer[p + 1] != ';' || buffer[p] != '"')
                ThrowException("Expected quotation mark and semicolon at the end of a string.");
            p += 2;
        }

        protected void ReadUntilEndOfLine()
        {
            if (isBinaryFormat)
                return;

            while (p < end)
            {
                if (buffer[p] == '\n' || buffer[p] == '\r')
                {
                    ++p;
                    lineNumber++;
                    return;
                }
                ++p;
            }
        }

        protected ushort ReadBinWord()
        {
            Debug.Assert(end - p >= 2);
            ushort tmp = BitConverter.ToUInt16(buffer, p);
            p += 2;
            return tmp;
        }

        protected uint ReadBinDWord()
        {
            Debug.Assert(end - p >= 4);
            uint tmp = BitConverter.ToUInt32(buffer, p);
            p += 4;
            return tmp;
        }

        protected uint ReadInt()
        {
            if (isBinaryFormat)
            {
                if (binaryNumCount == 0 && end - p >= 2)
                {
                    ushort tmp = ReadBinWord(); // 0x06 or 0x03
                    if (tmp == 0x06 && end - p >= 4) // array of ints follows
                        binaryNumCount = ReadBinDWord();
                    else // single int follows
                        binaryNumCount = 1;
                }

                --binaryNumCount;
                if (end - p >= 4)
                {
                    return ReadBinDWord();
                }
                else
                {
                    p = end;
                    return 0;
                }
            }
            else
            {
                FindNextNoneWhiteSpace();

                // TODO: consider using strtol10 instead???

                // check preceeding minus sign
                bool isNegative = false;
                if (buffer[p] == '-')
                {
                    isNegative = true;
                    p++;
                }

                // at least one digit expected
                if (!char.IsDigit((char)buffer[p]))
                    ThrowException("Number expected.");

                // read digits
                uint number = 0;
                while (p < end)
                {
                    if (!char.IsDigit((char)buffer[p]))
                        break;
                    number = number * 10 + ((uint)buffer[p] - 48);
                    p++;
                }

                CheckForSeparator();
                return isNegative ? (uint)(-1 * (int)number) : number;
            }

        }

        protected float ReadFloat()
        {
            if (isBinaryFormat)
            {
                if (binaryNumCount == 0 && end - p >= 2)
                {
                    ushort tmp = ReadBinWord();
                    if (tmp == 0x07 && end - p >= 4)
                    {
                        binaryNumCount = ReadBinDWord();
                    }
                    else
                    {
                        binaryNumCount = 1;
                    }
                }

                --binaryNumCount;
                if (binaryFloatSize == 8)
                {
                    if (end - p >= 8)
                    {
                        float result = (float)BitConverter.ToDouble(buffer, p);
                        p += 8;
                        return result;
                    }
                    else
                    {
                        p = end;
                        return 0;
                    }
                }
                else
                {
                    if (end - p >= 4)
                    {
                        float result = BitConverter.ToSingle(buffer, p);
                        p += 4;
                        return result;
                    }
                    else
                    {
                        p = end;
                        return 0;
                    }
                }
            }

            // text version
            FindNextNoneWhiteSpace();
            // check for various special strings to allow reading files from faulty exporters
            // I mean you, Blender!
            // Reading is safe because of the terminating zero
            if (Encoding.Default.GetString(buffer, p, 9) == "-1.#IND00" || Encoding.Default.GetString(buffer, p, 8) == "1.#IND00")
            {
                p += 9;
                CheckForSeparator();
                return 0.0f;
            }
            else if (Encoding.Default.GetString(buffer, p, 8) == "1.#QNAN0")
            {
                p += 8;
                CheckForSeparator();
                return 0.0f;
            }

            float result_ = 0.0f;

            string tmp_ = string.Empty;
            while (p < end)
            {
                byte c = buffer[p];
                if (char.IsDigit((char)c) || c == '+' || c == '.' || c == '-' || c == 'e' || c == 'E')
                {
                    tmp_ += (char)c;
                    p++;
                }
                else
                {
                    break;
                }
            }
            float.TryParse(tmp_, out result_);

            CheckForSeparator();

            return result_;
        }

        protected Vector2D ReadVector2()
        {
            Vector2D vector;
            vector.X = ReadFloat();
            vector.Y = ReadFloat();
            TestForSeparator();

            return vector;
        }

        protected Vector3D ReadVector3()
        {
            Vector3D vector;
            vector.X = ReadFloat();
            vector.Y = ReadFloat();
            vector.Z = ReadFloat();
            TestForSeparator();

            return vector;
        }

        protected Color3D ReadRGB()
        {
            Color3D color = new Color3D();
            color.Red = ReadFloat();
            color.Green = ReadFloat();
            color.Blue = ReadFloat();
            TestForSeparator();

            return color;
        }

        protected Color4D ReadRGBA()
        {
            Color4D color;
            color.Red = ReadFloat();
            color.Green = ReadFloat();
            color.Blue = ReadFloat();
            color.Alpha = ReadFloat();
            TestForSeparator();

            return color;
        }

        /// <summary>
        /// Throws an exception with a line number and the given text.
        /// </summary>
        protected void ThrowException(string text)
        {
            if (isBinaryFormat)
                throw (new Exception(text));
            else
                throw (new Exception(string.Format("Line {0}: {1}", lineNumber, text)));
        }

        protected void FilterHierarchy(Node node)
        {
            // if the node has just a single unnamed child containing a mesh, remove
            // the anonymous node inbetween. The 3DSMax kwXport plugin seems to produce this
            // mess in some cases
            if (node.Children.Count == 1 && node.Meshes.Count == 0)
            {
                Node child = node.Children[0];
                if (child.Name.Length == 0 && (child.Meshes.Count > 0))
                {
                    // transfer its meshes to us
                    for (uint a = 0; a < child.Meshes.Count; a++)
                        node.Meshes.Add(child.Meshes[(int)a]);
                    child.Meshes.Clear();

                    // transfer the transform as well
                    node.TrafoMatrix = node.TrafoMatrix * child.TrafoMatrix;
                    // then kill it
                    node.Children.Clear();
                }
            }

            // recurse
            for (uint a = 0; a < node.Children.Count; a++)
                FilterHierarchy(node.Children[(int)a]);
        }

        protected uint majorVersion, minorVersion;

        protected bool isBinaryFormat;

        protected uint binaryFloatSize;

        protected uint binaryNumCount;

        protected int p;

        protected byte[] buffer;

        /// <summary>
        /// counter for number arrays in binary format
        /// </summary>
        protected int end;

        /// <summary>
        /// Line number when reading in text format
        /// </summary>
        protected uint lineNumber;

        /// <summary>
        /// Imported data
        /// </summary>
        protected Scene scene;
    }
}

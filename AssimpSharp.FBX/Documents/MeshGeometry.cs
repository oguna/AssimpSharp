using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for FBX geometry of type "Mesh"
    /// </summary>
    public class MeshGeometry : Geometry
    {
        public const int AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;
        public const int AI_MAX_NUMBER_OF_COLOR_SETS = 4;

        #region メンバ変数
        private List<int> materials = new List<int>();
        private List<Vector3> vertices = new List<Vector3>();
        private List<uint> faces = new List<uint>();
        private List<uint> facesVertexSttartIndices = new List<uint>();
        private List<Vector3> tangents = new List<Vector3>();
        private List<Vector3> binormals = new List<Vector3>();
        private List<Vector3> normals = new List<Vector3>();
        private string[] uvNames = new string[AI_MAX_NUMBER_OF_TEXTURECOORDS];
        private List<Vector2>[] uvs = new List<Vector2>[AI_MAX_NUMBER_OF_TEXTURECOORDS];
        private List<Color4>[] colors = new List<Color4>[AI_MAX_NUMBER_OF_COLOR_SETS];
        private uint[] mappingCounts;
        private uint[] mappingOffsets;
        private uint[] mappings;
        #endregion

        #region ゲッター
        public List<Vector3> Vertices
        {
            get
            {
                return vertices;
            }
        }
        public List<Vector3> Normals
        {
            get
            {
                return normals;
            }
        }
        public List<Vector3> Tangents
        {
            get
            {
                return tangents;
            }
        }
        public List<Vector3> Binormals
        {
            get
            {
                return binormals;
            }
        }
        public List<uint> FaceIndexCounts
        {
            get
            {
                return faces;
            }
        }
        public List<Vector2> GetTextureCoords(int index)
        {
            if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS)
            {
                return new List<Vector2>();
            }
            else
            {
                return uvs[index];
            }
        }
        public string GetTextureCoordChannelName(int index)
        {
            if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS)
            {
                return string.Empty;
            }
            else
            {
                return uvNames[index];
            }
        }

        public List<Color4> GetVertexColors(int index)
        {
            if (index >= AI_MAX_NUMBER_OF_COLOR_SETS)
            {
                return new List<Color4>();
            }
            else
            {
                return colors[index];
            }
        }

        public List<int> MaterialIndices
        {
            get
            {
                return materials;
            }
        }

        /// <summary>
        /// Convert from a fbx file vertex index (for example from a #Cluster weight) or NULL
        /// if the vertex index is not valid.
        /// </summary>
        public ArraySegment<uint> ToOutputVertexIndex(uint inIndex)
        {
            if (inIndex >= mappingCounts.Length)
            {
                return new ArraySegment<uint>();
            }
            Debug.Assert(mappingCounts.Length == mappingOffsets.Length);
            var count = (int)mappingCounts[inIndex];
            Debug.Assert(count != 0);
            Debug.Assert(mappingOffsets[(int)inIndex] + count <= mappings.Length);
            return new ArraySegment<uint>(mappings, (int)mappingOffsets[inIndex], count);
        }

        /// <summary>
        /// Determine the face to which a particular output vertex index belongs.
        /// This mapping is always unique.
        /// </summary>
        public uint FaceForVertexIndex(uint inIndex)
        {
            Debug.Assert(inIndex < vertices.Count);

            // in the current conversion pattern this will only be needed if
            // weights are present, so no need to always pre-compute this table
            if (facesVertexSttartIndices.Count == 0)
            {
                facesVertexSttartIndices = new List<uint>(faces.Count + 1);
                facesVertexSttartIndices.Add(0);
                uint sum = 0;
                for (int i = 0; i < faces.Count; i++)
                {
                    sum += faces[i];
                    facesVertexSttartIndices.Add(sum);
                }
            }

            Debug.Assert(facesVertexSttartIndices.Count == faces.Count);
            int it;
            for (it = 0; it < facesVertexSttartIndices.Count; it++)
            {
                if (facesVertexSttartIndices[it] > inIndex)
                {
                    break;
                }
            }
            return (uint)it;
        }

        #endregion


        public MeshGeometry(ulong id, Element element, string name, Document doc)
            : base(id, element, name, doc)
        {
            for(int i=0; i<AI_MAX_NUMBER_OF_COLOR_SETS; i++)
            {
                colors[i] = new List<Color4>();
            }
            for (int i=0; i<AI_MAX_NUMBER_OF_TEXTURECOORDS; i++)
            {
                uvs[i] = new List<Vector2>();
            }

            var sc = element.Compound;
            if (sc == null)
            {
                throw (new DomException("failed to read Geometry object (class: Mesh), no data scope found"));
            }

            // must have Mesh elements:
            var Vertices = Parser.GetRequiredElement(sc, "Vertices", element);
            var PolygonVertexIndex = Parser.GetRequiredElement(sc, "PolygonVertexIndex", element);

            // optional Mesh elements:
            var Layer = sc.GetCollection("Layer");

            List<Vector3> tempVerts;
            Parser.ParseVectorDataArray(out tempVerts, Vertices);
            if (tempVerts.Count == 0)
            {
                throw (new Exception());
            }

            List<int> tempFaces;
            Parser.ParseVectorDataArray(out tempFaces, PolygonVertexIndex);
            if (tempFaces.Count == 0)
            {
                throw (new Exception());
            }

            vertices = new List<Vector3>(tempFaces.Count);
            faces = new List<uint>(tempFaces.Count / 3);

            mappingOffsets = new uint[tempVerts.Count];
            mappingCounts = new uint[tempVerts.Count];
            mappings = new uint[tempFaces.Count];

            int vertexCount = tempVerts.Count;

            // generate output vertices, computing an adjacency table to
            // preserve the mapping from fbx indices to *this* indexing.
            int count = 0;
            foreach (var index in tempFaces)
            {
                int absi = index < 0 ? (-index - 1) : index;
                if (absi >= vertexCount)
                {
                    throw (new DomException("polygon vertex index out of range", PolygonVertexIndex));
                }
                vertices.Add(tempVerts[absi]);
                ++count;

                ++mappingCounts[absi];
                if (index < 0)
                {
                    faces.Add((uint)count);
                    count = 0;
                }
            }

            uint cursor = 0;
            for (int i = 0, e = tempVerts.Count; i < e; ++i)
            {
                mappingOffsets[i] = cursor;
                cursor += mappingCounts[i];
                mappingCounts[i] = 0;
            }

            cursor = 0;
            foreach (var index in tempFaces)
            {
                int absi = index < 0 ? (-index - 1) : index;
                mappings[(int)mappingOffsets[absi] + (int)mappingCounts[absi]++] = cursor++;
            }

            // if settings.readAllLayers is true:
            //  * read all layers, try to load as many vertex channels as possible
            // if settings.readAllLayers is false:
            //  * read only the layer with index 0, but warn about any further layers
            foreach (var it in Layer)
            {
                var tokens = it.Tokens;
                string err;
                int index = Parser.ParseTokenAsInt(tokens[0], out err);
                if (!string.IsNullOrEmpty(err))
                {
                    throw (new DomException(err, element));
                }
                if (doc.Settings.ReadAllLayers || index == 0)
                {
                    var layer = Parser.GetRequiredScope(it);
                    ReadLayer(layer);
                }
                else
                {
                    throw (new Exception("ignoring additional geometry layers"));
                }
            }
        }

        private void ReadLayer(Scope layer)
        {
            var LayerElemenet = layer.GetCollection("LayerElement");
            foreach (var eit in LayerElemenet)
            {
                var elayer = Parser.GetRequiredScope(eit);
                ReadLayerElement(elayer);
            }
        }

        private void ReadLayerElement(Scope layerElement)
        {
            var Type = Parser.GetRequiredElement(layerElement, "Type");
            var TypedIndex = Parser.GetRequiredElement(layerElement, "TypedIndex");

            var type = Parser.ParseTokenAsString(Parser.GetRequiredToken(Type, 0));
            var typedIndex = Parser.ParseTokenAsInt(Parser.GetRequiredToken(TypedIndex, 0));

            var top = Parser.GetRequiredScope(this.SourceElement);
            var canditates = top.GetCollection(type);

            foreach (var it in canditates)
            {
                int index = Parser.ParseTokenAsInt(Parser.GetRequiredToken(it, 0));
                if (index == typedIndex)
                {
                    ReadVertexData(type, typedIndex, Parser.GetRequiredScope(it));
                    return;
                }
            }
            throw (new Exception()); // LogError
        }

        private void ReadVertexData(string type, int index, Scope source)
        {
            string MappingInformationType = Parser.ParseTokenAsString(Parser.GetRequiredToken(Parser.GetRequiredElement(source, "MappingInformationType"), 0));
            string ReferenceInformationType = Parser.ParseTokenAsString(Parser.GetRequiredToken(Parser.GetRequiredElement(source, "ReferenceInformationType"), 0));

            if (type == "LayerElementUV")
            {
                if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS)
                {
                    throw (new Exception()); // logerror
                }

                Element Name = source["Name"];
                uvNames[index] = "";
                if (Name != null)
                {
                    uvNames[index] = Parser.ParseTokenAsString(Parser.GetRequiredToken(Name, 0));
                }

                ReadVertexDataUV(out uvs[index], source, MappingInformationType, ReferenceInformationType);
            }
            else if (type == "LayerElementMaterial")
            {
                if (materials.Count > 0)
                {
                    throw (new Exception()); // logerror
                }

                List<int> tempMaterials;
                ReadVertexDataMaterials(out tempMaterials, source, MappingInformationType, ReferenceInformationType);

                int countNeg = 0;
                foreach (var i in tempMaterials)
                {
                    if (i < 0)
                    {
                        countNeg++;
                    }
                }
                if (countNeg == tempMaterials.Count)
                {
                    throw (new Exception()); // LogWarn
                }
                materials = tempMaterials;
            }
            else if (type == "LayerElementNormal")
            {
                if (normals.Count > 0)
                {
                    throw (new Exception());
                }
                ReadVertexDataNormals(out normals, source, MappingInformationType, ReferenceInformationType);
            }
            else if (type == "LayerElementTangent")
            {
                if (tangents.Count > 0)
                {
                    throw (new Exception());
                }
                ReadVertexDataTangents(out tangents, source, MappingInformationType, ReferenceInformationType);
            }
            else if (type == "LayerElementBinormal")
            {
                if (binormals.Count > 0)
                {
                    throw (new Exception());
                }
                ReadVertexDataBinormals(out binormals, source, MappingInformationType, ReferenceInformationType);
            }
            else if (type == "LayerElementColor")
            {
                if (index >= AI_MAX_NUMBER_OF_COLOR_SETS)
                {
                    throw (new Exception());
                }
                ReadVertexDataBinormals(out binormals, source, MappingInformationType, ReferenceInformationType);
            }
        }

        private static void ResolveVertexDataArray(
           out Vector2[] dataOut,
           Scope source,
           string MappingInformationType,
           string ReferenceInformationType,
           string dataElementName,
           string indexDataElementName,
           int vertexCount,
           uint[] mappingCounts,
           uint[] mappingOffset,
           uint[] mappings)
        {
            List<Vector2> tempUV;
            Parser.ParseVectorDataArray(out tempUV, Parser.GetRequiredElement(source, dataElementName));
            ResolveVertexDataArrayImpl(out dataOut, tempUV, source, MappingInformationType, ReferenceInformationType, dataElementName, indexDataElementName, vertexCount, mappingCounts, mappingOffset, mappings);
        }

        private static void ResolveVertexDataArray(
           out Vector3[] dataOut,
           Scope source,
           string MappingInformationType,
           string ReferenceInformationType,
           string dataElementName,
           string indexDataElementName,
           int vertexCount,
           uint[] mappingCounts,
           uint[] mappingOffset,
           uint[] mappings)
        {
            List<Vector3> tempUV;
            Parser.ParseVectorDataArray(out tempUV, Parser.GetRequiredElement(source, dataElementName));
            ResolveVertexDataArrayImpl(out dataOut, tempUV, source, MappingInformationType, ReferenceInformationType, dataElementName, indexDataElementName, vertexCount, mappingCounts, mappingOffset, mappings);
        }

        private static void ResolveVertexDataArray(
           out Vector4[] dataOut,
           Scope source,
           string MappingInformationType,
           string ReferenceInformationType,
           string dataElementName,
           string indexDataElementName,
           int vertexCount,
           uint[] mappingCounts,
           uint[] mappingOffset,
           uint[] mappings)
        {
            List<Vector4> tempUV;
            Parser.ParseVectorDataArray(out tempUV, Parser.GetRequiredElement(source, dataElementName));
            ResolveVertexDataArrayImpl(out dataOut, tempUV, source, MappingInformationType, ReferenceInformationType, dataElementName, indexDataElementName, vertexCount, mappingCounts, mappingOffset, mappings);
        }

        private static void ResolveVertexDataArrayImpl<T>(
            out T[] dataOut,
            List<T> tempUV,
            Scope source,
            string MappingInformationType,
            string ReferenceInformationType,
            string dataElementName,
            string indexDataElementName,
            int vertexCount,
            uint[] mappingCounts,
            uint[] mappingOffset,
            uint[] mappings)
            where T : struct
        {
            // handle permutations of Mapping and Reference type - it would be nice to
            // deal with this more elegantly and with less redundancy, but right
            // now it seems unavoidable.
            if (MappingInformationType == "ByVertice" && ReferenceInformationType == "Direct")
            {
                dataOut = new T[vertexCount];
                for (int i = 0, e = tempUV.Count; i < e; i++)
                {
                    int istart = (int)mappingOffset[i];
                    int iend = (int)istart + (int)mappingCounts[i];
                    for (int j = istart; j < iend; j++)
                    {
                        dataOut[(int)mappings[j]] = tempUV[i];
                    }
                }
            }
            else if (MappingInformationType == "ByVertice" && ReferenceInformationType == "IndexToDirect")
            {
                dataOut = new T[vertexCount];
                List<int> uvIndices;
                Parser.ParseVectorDataArray(out uvIndices, Parser.GetRequiredElement(source, indexDataElementName));
                for (int i = 0, e = uvIndices.Count; i < e; i++)
                {
                    int istart = (int)mappingOffset[i];
                    int iend = (int)istart + (int)mappingCounts[i];
                    for (int j = istart; j < iend; j++)
                    {
                        if (uvIndices[i] >= tempUV.Count)
                        {
                            throw (new DomException("index out of range", Parser.GetRequiredElement(source, indexDataElementName)));
                        }
                        dataOut[(int)mappings[j]] = tempUV[uvIndices[i]];
                    }
                }
            }
            else if (MappingInformationType == "ByPolygonVertex" && ReferenceInformationType == "Direct")
            {
                if (tempUV.Count != vertexCount)
                {
                    throw (new Exception());
                }
                dataOut = tempUV.ToArray();
            }
            else if (MappingInformationType == "ByPolygonVertex" && ReferenceInformationType == "IndexToDirect")
            {
                dataOut = new T[vertexCount];
                List<int> uvIndices;
                Parser.ParseVectorDataArray(out uvIndices, Parser.GetRequiredElement(source, indexDataElementName));
                if (uvIndices.Count != vertexCount)
                {
                    throw (new Exception());
                }
                int next = 0;
                foreach (var i in uvIndices)
                {
                    if (i >= tempUV.Count)
                    {
                        throw (new DomException("index out of range", Parser.GetRequiredElement(source, indexDataElementName)));
                    }
                    dataOut[next++] = tempUV[i];
                }
            }
            else
            {
                throw (new Exception());
            }
        }

        private void ReadVertexDataUV(out List<Vector2> uvOut, Scope source, string MappingInformationType, string ReferenceInformationType)
        {
            Vector2[] tmp;
            ResolveVertexDataArray(out tmp, source, MappingInformationType, ReferenceInformationType,
                "UV", "UVIndex", vertices.Count, mappingCounts, mappingOffsets, mappings);
            uvOut = tmp.ToList();
        }

        private void ReadVertexDataNormals(out List<Vector3> normalsOut, Scope source, string mappingInformationType, string referenceInformationType)
        {
            Vector3[] tmp;
            ResolveVertexDataArray(out tmp, source, mappingInformationType, referenceInformationType,
                "Normals", "NormalsIndex", vertices.Count, mappingCounts, mappingOffsets, mappings);
            normalsOut = tmp.ToList();
        }

        private void ReadVertexDataColor(out List<Vector4> colorsOut, Scope source, string MappingInformationType, string ReferenceInformationType)
        {
            Vector4[] tmp;
            ResolveVertexDataArray(out tmp, source, MappingInformationType, ReferenceInformationType,
                  "Colors", "ColorIndex", vertices.Count, mappingCounts, mappingOffsets, mappings);
            colorsOut = tmp.ToList();
        }

        private void ReadVertexDataTangents(out List<Vector3> tangentsOut, Scope source, string MappingInformationType, string ReferenceInformationType)
        {
            Vector3[] tmp;
            ResolveVertexDataArray(out tmp, source, MappingInformationType, ReferenceInformationType,
                   "Tangent", "TangentIndex", vertices.Count, mappingCounts, mappingOffsets, mappings);
            tangentsOut = tmp.ToList();
        }

        private void ReadVertexDataBinormals(out List<Vector3> binormalsOut, Scope source, string MappingInformationType, string ReferenceInformationType)
        {
            Vector3[] tmp;
            ResolveVertexDataArray(out tmp, source, MappingInformationType, ReferenceInformationType,
                "Binormal", "BinormalIndex", vertices.Count, mappingCounts, mappingOffsets, mappings);
            binormalsOut = tmp.ToList();
        }

        private void ReadVertexDataMaterials(out List<int> materialsOut, Scope source, string MappingInformationType, string ReferenceInformationType)
        {
            int faceCount = faces.Count;
            Debug.Assert(faceCount != 0);

            // materials are handled separately. First of all, they are assigned per-face
            // and not per polyvert. Secondly, ReferenceInformationType=IndexToDirect
            // has a slightly different meaning for materials.
            Parser.ParseVectorDataArray(out materialsOut, Parser.GetRequiredElement(source, "Materials"));

            if (MappingInformationType == "AllSame")
            {
                // easy - same material for all faces
                if (materialsOut.Count == 0)
                {
                    throw (new Exception("expected material index, ignoring"));
                }
                else if (materialsOut.Count > 1)
                {
                    throw (new Exception("expected only a single material index, ignoring all except the first one"));
                    materialsOut.Clear();
                }
                materials = new List<int>(vertices.Count);
                for (int i = 0; i < vertices.Count; i++)
                {
                    materials.Add(materialsOut[0]);
                }
            }
            else if (MappingInformationType == "ByPolygon" && ReferenceInformationType == "IndexToDirect")
            {
                materials = new List<int>(faceCount);
                if (materialsOut.Count != faceCount)
                {
                    throw (new Exception());
                }
            }
            else
            {
                throw (new Exception());
            }
        }
    }
}

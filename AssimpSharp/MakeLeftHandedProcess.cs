using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp
{
    public class MakeLeftHandedProcess
    {
        public bool IsActive(int flags)
        {
            throw (new NotImplementedException());
        }

        /// <summary>
        /// Executes the post processing step on the given imported data.
        /// </summary>
        public void Execute(Scene scene)
        {
            // Check for an existent root node to proceed
            Debug.Assert(scene.RootNode != null);

            // recursively convert all the nodes
            ProcessNode(scene.RootNode, Matrix.Identity);

            // process the meshes accordingly
            foreach (var mesh in scene.Meshes)
            {
                ProcessMesh(mesh);
            }

            // process the materials accordingly
            foreach (var mat in scene.Materials)
            {
                ProcessMaterial(mat);
            }

            // transform all animation channels as well
            foreach (var anim in scene.Animations)
            {
                foreach (var nodeAnim in anim.Channels)
                {
                    ProcessAnimation(nodeAnim);
                }
            }
        }

        /// <summary>
        /// Recursively converts a node, all of its children and all of its meshes
        /// </summary>
        void ProcessNode(Node node, Matrix parentGlobalRotation)
        {
            // mirror all base vectors at the local Z axis
            node.Transformation.M31 = -node.Transformation.M31;
            node.Transformation.M32 = -node.Transformation.M32;
            node.Transformation.M33 = -node.Transformation.M33;
            node.Transformation.M34 = -node.Transformation.M34;

            // now invert the Z axis again to keep the matrix determinant positive.
            // The local meshes will be inverted accordingly so that the result should look just fine again.
            node.Transformation.M13 = -node.Transformation.M13;
            node.Transformation.M23 = -node.Transformation.M23;
            node.Transformation.M33 = -node.Transformation.M33;
            node.Transformation.M43 = -node.Transformation.M43;

            // continue for all children
            foreach (var child in node.Children)
            {
                ProcessNode(child, parentGlobalRotation * node.Transformation);
            }
        }

        /// <summary>
        /// Converts a single mesh to left handed coordinates.
        /// </summary>
        void ProcessMesh(Mesh mesh)
        {

            // mirror positions, normals and stuff along the Z axis
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                mesh.Vertices[i].Z *= -1;
                if (mesh.HasNormals)
                {
                    mesh.Normals[i].Z *= -1;
                }
                if (mesh.HasTangentsAndBitangets)
                {
                    mesh.Tangents[i].Z *= -1;
                    mesh.Bitangents[i].Z *= -1;
                }
            }

            // mirror offset matrices of all bones
            for (int i = 0; i < mesh.Bones.Length; i++)
            {
                var bone = mesh.Bones[i];
                bone.OffsetMatrix.M13 = -bone.OffsetMatrix.M13;
                bone.OffsetMatrix.M23 = -bone.OffsetMatrix.M23;
                bone.OffsetMatrix.M43 = -bone.OffsetMatrix.M43;
                bone.OffsetMatrix.M31 = -bone.OffsetMatrix.M31;
                bone.OffsetMatrix.M32 = -bone.OffsetMatrix.M32;
                bone.OffsetMatrix.M34 = -bone.OffsetMatrix.M34;
            }

            // mirror bitangents as well as they're derived from the texture coords
            if (mesh.HasTangentsAndBitangets)
            {
                for(int i=0; i<mesh.Vertices.Length; i++)
                {
                    mesh.Bitangents[i] *= -1;
                }
            }
        }

        /// <summary>
        /// Converts a single material to left handed coordinates.
        /// </summary>
        void ProcessMaterial(Material mat)
        {
            //throw (new NotImplementedException());
        }

        void ProcessAnimation(NodeAnim anim)
        {
            // position keys
            for (int a = 0; a < anim.PositionKeys.Length; a++)
            {
                anim.PositionKeys[a].Value.Z *= 1;
            }

            // rotation keys
            for (int a = 0; a < anim.RotationKeys.Length; a++)
            {
                /* That's the safe version, but the float errors add up. So we try the short version instead
                aiMatrix3x3 rotmat = pAnim->mRotationKeys[a].mValue.GetMatrix();
                rotmat.a3 = -rotmat.a3; rotmat.b3 = -rotmat.b3;
                rotmat.c1 = -rotmat.c1; rotmat.c2 = -rotmat.c2;
                aiQuaternion rotquat( rotmat);
                pAnim->mRotationKeys[a].mValue = rotquat;
                */
                anim.RotationKeys[a].Value.X *= -1;
                anim.RotationKeys[a].Value.Y *= -1;
            }
        }
    }
}

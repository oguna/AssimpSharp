using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    // FBXDocument.cpp

    /// <summary>
    /// Represents a delay-parsed FBX objects. Many objects in the scene
    /// are not needed by assimp, so it makes no sense to parse them
    /// upfront.
    /// </summary>
    public class LazyObject
    {
        private Document doc;
        private Element element;
        private Object obj;
        private ulong id;
        [Flags]
        private enum Flags
        {
            BeingConstructured = 0x1,
            FailedToConstruct = 0x2
        };
        private Flags flags;

        public LazyObject(ulong id, Element element, Document doc)
        {
            this.doc = doc;
            this.element = element;
            this.id = id;
            this.flags = 0;
        }

        public Object Get(bool dieOnError = false)
        {
            if (IsBeginConstructed || IsFailToConstruct)
            {
                return null;
            }

            if (obj != null)
            {
                return obj;
            }

            // if this is the root object, we return a dummy since there
            // is no root object int he fbx file - it is just referenced
            // with id 0.
            if (id == 0L)
            {
                obj = new Object(id, element, "Model::RootNode");
                return obj;
            }

            var key = element.KeyToken;
            var tokens = element.Tokens;

            if (tokens.Count < 3)
            {
                DocumentUtil.DOMError("expected at least 3 tokens: id, name and class tag");
            }

            string err;
            var name = Parser.ParseTokenAsString(tokens[1], out err);
            if (!string.IsNullOrEmpty(err))
            {
                DocumentUtil.DOMError(err);
            }

            // small fix for binary reading: binary fbx files don't use
            // prefixes such as Model:: in front of their names. The
            // loading code expects this at many places, though!
            // so convert the binary representation (a 0x0001) to the
            // double colon notation.
            if (tokens[1].IsBinary)
            {
                for (int i = 0; i < name.Length; i++)
                {
                    if (name[i] == 0x0 && name[i + 1] == 0x1)
                    {
                        name = name.Substring(i + 2) + "::" + name.Substring(0, i);
                    }
                }
                throw (new NotImplementedException());
            }

            var classtag = Parser.ParseTokenAsString(tokens[2], out err);
            if (!string.IsNullOrEmpty(err))
            {
                DocumentUtil.DOMError(err);
            }

            // prevent recursive calls
            flags |= Flags.BeingConstructured;
            try
            {
                // this needs to be relatively fast since it happens a lot,
                // so avoid constructing strings all the time.
                var obtype = key.StringContents;
                if (obtype == "Geometry")
                {
                    if (classtag == "Mesh")
                    {
                        obj = new MeshGeometry(id, element, name, doc);
                    }
                }
                else if (obtype == "NodeAttribute")
                {
                    if (classtag == "Camera")
                    {
                        obj = new Camera(id, element, doc, name);
                    }
                    else if (classtag == "CameraSwitcher")
                    {
                        obj = new CameraSwicher(id, element, doc, name);
                    }
                    else if (classtag == "Light")
                    {
                        obj = new Light(id, element, doc, name);
                    }
                    else if (classtag == "Null")
                    {
                        obj = new Null(id, element, doc, name);
                    }
                    else if (classtag == "LimbNode")
                    {
                        obj = new LimbNode(id, element, doc, name);
                    }
                }
                else if (obtype == "Deformer")
                {
                    if (classtag == "Cluster")
                    {
                        obj = new Cluster(id, element, doc, name);
                    }
                    else if (classtag == "Skin")
                    {
                        obj = new Skin(id, element, doc, name);
                    }
                }
                else if (obtype == "Model")
                {
                    // FK and IK effectors are not supported
                    if (classtag != "IKEffector" && classtag != "FKEffector")
                    {
                        obj = new Model(id, element, doc, name);
                    }
                }

                else if (obtype == "Material")
                {
                    obj = new Material(id, element, doc, name);
                }
                else if (obtype == "Texture")
                {
                    obj = new Texture(id, element, doc, name);
                }
                else if (obtype == "LayeredTexture")
                {
                    obj = new LayeredTexture(id, element, doc, name);
                }
                else if (obtype == "AnimationStack")
                {
                    obj = new AnimationStack(id, element, name, doc);
                }
                else if (obtype == "AnimationLayer")
                {
                    obj = new AnimationLayer(id, element, name, doc);
                }
                else if (obtype == "AnimationCurve")
                {
                    obj = new AnimationCurve(id, element, name, doc);
                }
                else if (obtype == "AnimationCurveNode")
                {
                    obj = new AnimationCurveNode(id, element, name, doc);
                }
            }
            catch (Exception ex)
            {
                flags &= ~Flags.BeingConstructured;
                flags |= Flags.FailedToConstruct;
                if (dieOnError || doc.Settings.StrictMode)
                {
                    throw (new Exception());
                }

                Console.Error.WriteLine(ex.Message);
                return null;
            }
            if (obj == null)
            {
                //DOMError("failed to convert element to DOM object, class: " + classtag + ", name: " + name,&element);
            }
            flags &= ~Flags.BeingConstructured;
            return obj;
        }

        public T Get<T>(bool dieOnError = false) where T : Object
        {
            var ob = Get(dieOnError);
            return ob as T;
        }

        public ulong ID
        {
            get
            {
                return id;
            }
        }

        public bool IsBeginConstructed
        {
            get
            {
                return flags.HasFlag(Flags.BeingConstructured);
            }
        }

        public bool IsFailToConstruct
        {
            get
            {
                return flags.HasFlag(Flags.FailedToConstruct);
            }
        }

        public Element Element
        {
            get
            {
                return element;
            }
        }

        public Document Document
        {
            get
            {
                return doc;
            }
        }

    }
}

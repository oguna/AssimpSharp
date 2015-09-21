using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for generic FBX materials
    /// </summary>
    public class Material : Object
    {
        private string shading;

        private bool multilayer;

        private PropertyTable props;

        private Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

        private Dictionary<string, LayeredTexture> layeredTextures = new Dictionary<string, LayeredTexture>();

        public string ShadingModel
        {
            get
            {
                return shading;
            }
        }

        public bool IsMultilayer
        {
            get
            {
                return multilayer;
            }
        }

        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        public Dictionary<string, Texture> Textures
        {
            get
            {
                return textures;
            }
        }

        public Dictionary<string, LayeredTexture> LayeredTextures
        {
            get
            {
                return layeredTextures;
            }
        }

        public Material(ulong id, Element element, Document doc, string name)
            : base(id, element, name)
        {
            var sc = Parser.GetRequiredScope(element);

            var ShadingModel = sc["ShadingModel"];
            var MultiLayer = sc["MultiLayer"];

            if (MultiLayer != null)
            {
                this.multilayer = (Parser.ParseTokenAsID(Parser.GetRequiredToken(MultiLayer, 0)) != 0);
            }

            if (ShadingModel != null)
            {
                this.shading = Parser.ParseTokenAsString(Parser.GetRequiredToken(ShadingModel, 0));
            }
            else
            {
                DocumentUtil.DOMWarning("shading mode not specified, assuming phong", element);
                shading = "phong";
            }

            var templateName = string.Empty;

            var sh = shading;
            if (sh == "phong")
            {
                templateName = "Material.FbxSurfacePhong";
            }
            else if (sh == "lambert")
            {
                templateName = "Material.FbxSurfaceLambert";
            }
            else
            {
                DocumentUtil.DOMWarning("shading mode not recognized: " + shading, element);
            }

            props = DocumentUtil.GetPropertyTable(doc, templateName, element, sc);

            // resolve texture links
            var conns = doc.GetConnectionsByDestinationSequenced(ID);
            foreach (var con in conns)
            {
                // texture link to properties, not objects
                if (string.IsNullOrEmpty(con.PropertyName))
                {
                    continue;
                }

                var ob = con.SourceObject;
                if (ob == null)
                {
                    DocumentUtil.DOMWarning("failed to read source object for texture link, ignoring", element);
                    continue;
                }

                var tex = ob as Texture;
                if (tex == null)
                {
                    var layeredTexture = ob as LayeredTexture;
                    if (layeredTexture == null)
                    {
                        DocumentUtil.DOMWarning("source object for texture link is not a texture or layered texture, ignoring", element);
                        continue;
                    }
                    var prop = con.PropertyName;
                    if (layeredTextures.ContainsKey(prop))
                    {
                        DocumentUtil.DOMWarning("duplicate layered texture link: " + prop, element);
                    }

                    layeredTextures[prop] = layeredTexture;
                    layeredTexture.FillTexture(doc);
                }
                else
                {
                    var prop = con.PropertyName;
                    if (textures.ContainsKey(prop))
                    {
                        DocumentUtil.DOMWarning("duplicate texture link: " + prop, element);
                    }
                    textures[prop] = tex;
                }
            }
        }
    }
}

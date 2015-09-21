using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpDX;

namespace AssimpSharp.ObjFile
{
    public class ObjFileMtlImporter
    {
        public ObjFileMtlImporter(TextReader reader, string absPath, Model model)
        {
            this.Reader = reader;
            this.Model = model;
            this.Line = 0;
            if (model.DefaultMaterial == null)
            {
                Model.DefaultMaterial = new Material();
                model.DefaultMaterial.MaterialName = "default";
            }
            Load();
        }

        private void Load()
        {
            string line;
            while ((line = Reader.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                string[] items = line.Split(' ', '\t');
                if (items[0].StartsWith("#"))
                {
                    continue;
                }
                switch (items[0])
                {
                    case "Ka":
                        {
                            var color = new Color3();
                            for (int i = 0; i < 3; i++)
                            {
                                color[i] = float.Parse(items[i + 1]);
                            }
                            Model.CurrentMaterial.Ambient = color;
                            break;
                        }
                    case "Kd":
                        {
                            var color = new Color3();
                            for (int i = 0; i < 3; i++)
                            {
                                color[i] = float.Parse(items[i + 1]);
                            }
                            Model.CurrentMaterial.Diffuse = color;
                            break;
                        }
                    case "Ks":
                        {
                            var color = new Color3();
                            for (int i = 0; i < 3; i++)
                            {
                                color[i] = float.Parse(items[i + 1]);
                            }
                            Model.CurrentMaterial.Specular = color;
                            break;
                        }
                    case "d":
                        Model.CurrentMaterial.Alpha = float.Parse(items[1]);
                        break;
                    case "Ns":
                        Model.CurrentMaterial.Shineness = float.Parse(items[1]);
                        break;
                    case "Ni":
                        Model.CurrentMaterial.IOR = float.Parse(items[1]);
                        break;
                    case "map_Kd":
                        Model.CurrentMaterial.Texture = items[1];
                        break;
                    case "map_Ka":
                        Model.CurrentMaterial.TextureAmbient = items[1];
                        break;
                    case "map_Ks":
                        Model.CurrentMaterial.TextureSpecular = items[1];
                        break;
                    case "map_d":
                        Model.CurrentMaterial.TextureOpacity = items[1];
                        break;
                    case "map_bump":
                    case "bump":
                        Model.CurrentMaterial.TextureBump = items[1];
                        break;
                    case "map_ns":
                        Model.CurrentMaterial.TextureSpecularity = items[1];
                        break;
                    case "newmtl":
                        CreateMaterial(items);
                        break;
                    case "illum":
                        Model.CurrentMaterial.IlluminationModel = int.Parse(items[1]);
                        break;
                    default:
                        Console.Error.WriteLine("OBJ/MTL: Encountered unknown texture type");
                        break;
                }
            }
        }

        private void GetColorRGBA(out float[] color)
        {
            color = new float[3];
        }

        private void GetIlluminationModel(int illum_model)
        { }

        private void GetFloatValue(float value)
        { }

        private void CreateMaterial(string[] tokens)
        {
            string name;
            if (tokens.Length == 1)
            {
                name = "material";
            }
            else
            {
                name = tokens[1];
            }
            Model.CurrentMaterial = new Material();
            Model.CurrentMaterial.MaterialName = name;
            Model.MaterialLib.Add(name);
            Model.MaterialMap[name] = Model.CurrentMaterial;
        }

        private void GetTexture()
        { }

        private void GetTextureOption(bool clamp)
        { }

        private string AbsPath;
        private Model Model;
        private TextReader Reader;
        private uint Line;
    }
}
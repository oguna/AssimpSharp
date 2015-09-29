using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.XFile
{
    /// <summary>
    /// Helper class to export a given scene to a X-file.
    /// </summary>
    /// <remarks>
    /// an xFile uses a left hand system. Assimp used a right hand system (OpenGL), therefore we have to transform everything
    /// </remarks>
    public class XFileExporter
    {
        public static void ExportSceneXFile(string file, IOSystem iosystem, AssimpSharp.Scene scene, ExportProperties propoerties)
        {
            throw (new NotImplementedException());
        }

        /// <summary>
        /// Constructor for a specific scene to export
        /// </summary>
        public XFileExporter(AssimpSharp.Scene scene, IOSystem iosystem, string path, string file)
        {
            //Properties = Properties;
            IOSystem = iosystem;
            Path = path;
            File = file;
            Scene = scene;
            SceneOwned = false;
            End = "\n";
            WriteFile();
        }

        /// <summary>
        /// Starts writing the contents
        /// </summary>
        protected void WriteFile()
        {
            Output.NewLine = End;
            WriteHeader();
            Output.WriteLine("Frame DXCC_ROOT {");
            PushTag();
            WriteFrameTransform(Matrix.Identity);
            WriteNode(Scene.RootNode);
            PopTag();
            Output.WriteLine("}");
        }

        /// <summary>
        /// Writes the asset header
        /// </summary>
        protected void WriteHeader()
        { }

        /// <summary>
        /// write a frame transform
        /// </summary>
        protected void WriteFrameTransform(Matrix m)
        { }

        /// <summary>
        /// Recursively writes the given node
        /// </summary>
        protected void WriteNode(AssimpSharp.Node node)
        {
        }

        /// <summary>
        /// write a mesh entry of the scene
        /// </summary>
        protected void WriteMesh(Mesh mesh)
        {

        }

        /// <summary>
        /// Enters a new xml element, which increases the indentation
        /// </summary>
        protected void PushTag()
        {
            Start += "  ";
        }

        /// <summary>
        /// Leaves an element, decreasing the indentation
        /// </summary>
        protected void PopTag()
        {
            Debug.Assert(Start.Length > 1);
            Start = Start.Remove(Start.Length - 2);
        }

        /// <summary>
        /// Stringstream to write all output into
        /// </summary>
        public StringWriter Output;

        /// <summary>
        /// normalize the name to be accepted by xfile readers
        /// </summary>
        protected string ToXFileString(string name)
        {
            var str = new StringBuilder(name);
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsDigit(str[i]) || char.IsUpper(str[i]) || char.IsLower(str[i]))
                {
                    continue;
                }
                str[i] = '_';
            }
            return str.ToString();
        }

        /// <summary>
        /// hold the properties pointer
        /// </summary>
        //protected ExportPropoerties Properties;

        /// <summary>
        /// write a path
        /// </summary>
        protected void WritePath(string path)
        {
            var str = path;

        }

        /// <summary>
        /// The IOSystem for output
        /// </summary>
        protected IOSystem IOSystem;

        /// <summary>
        /// Path of the directory where the scene will be exported
        /// </summary>
        protected string Path;

        /// <summary>
        /// Name of the file (without extension) where the scene will be exported
        /// </summary>
        protected string File;

        /// <summary>
        /// The scene to be written
        /// </summary>
        protected AssimpSharp.Scene Scene;

        protected bool SceneOwned;

        /// <summary>
        /// current line start string, contains the current indentation for simple stream insertion
        /// </summary>
        protected string Start;

        /// <summary>
        /// current line end string for simple stream insertion
        /// </summary>
        protected string End;
    }
}

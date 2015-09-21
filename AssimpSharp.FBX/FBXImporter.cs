using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AssimpSharp;
using AssimpSharp.FBX;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public class FBXImporter : BaseImporter
    {
        protected static readonly ImporterDesc Desc = new ImporterDesc()
        {
            Name = "Autodesk FBX Importer",
            Author = "",
            Maintainer = "",
            Comment = "",
            Flags = ImporterFlags.SupportTextFlavour,
            MinMajor = 0,
            MinMinor = 0,
            MaxMajor = 0,
            MaxMinor = 0,
            FileExtensions = "fbx"
        };

        public override ImporterDesc GetInfo()
        {
            return Desc;
        }

        public override bool CanRead(string file, IOSystem ioHandler, bool checkSig)
        {
            string extension = GetExtension(file);
            if (extension == "fbx")
            {
                return true;
            }
            else if ((extension.Length == 0 || checkSig) && ioHandler != null)
            {
                var tokens = new string[] {"FBX"};
                return SearchFileHeaderForToken(ioHandler, file, tokens);
            }
            return false;
        }

        public Scene ReadFile(string file)
        {
            byte[] input;
            using (var stream = new FileStream(file, FileMode.Open))
            {
                input = new byte[stream.Length];
                stream.Read(input, 0, (int)stream.Length);
            }
            bool isBinary = false;
            List<Token> tokens;
            if (Encoding.ASCII.GetString(input, 0, 18) == "Kaydara FBX Binary")
            {
                isBinary = true;
                BinaryTokenizer.TokenizeBinary(out tokens, input, input.Length);
            }
            else
            {
                Tokenizer.Tokenize(out tokens, input);
            }
            Parser parser = new Parser(tokens, isBinary);
            Document doc = new Document(parser, settings);
            Scene scene;
            FbxConverter.ConvertToScene(out scene, doc);
            return scene;
        }

        public ImporterSettings settings = ImporterSettings.Default;

        public static void LogError(string message)
        {
            throw (new Exception(message));
        }
        public static void LogWarn(string message)
        {
            throw (new Exception(message));
        }
        public static void LogInfo(string message)
        {
            Debug.WriteLine(message);
        }
    }
}

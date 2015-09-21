using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace AssimpSharp
{
    public abstract class BaseImporter
    {
        public abstract bool CanRead(string file, IOSystem ioHandler, bool checkSig);

        public abstract ImporterDesc GetInfo();

        /// <summary>
        /// Extract file extension from a string
        /// </summary>
        /// <param name="file">Input file</param>s
        /// <returns>Extension without trailing dot, all lowercase</returns>
        protected string GetExtension(string file)
        {
            int pos = file.LastIndexOf('.');

            // no file extension at all
            if (pos == -1)
            {
                return "";
            }

            string ret = file.Substring(pos + 1);
            ret = ret.ToLower();
            return ret;
        }

        protected static bool CheckMagicToken(IOSystem ioHandler, string file, string[] magic, int num, int offset = 0, int size = 4)
        {
            throw (new NotImplementedException());
        }

        public virtual Scene ReadFile()
        {
            throw (new NotImplementedException());
        }

        /// <summary>
        /// Simple check for file extension
        /// </summary>
        protected static bool SimpleExtensionCheck(string file, params string[] exts)
        {
            foreach (var ext in exts)
            {
                if (file.EndsWith('.' + ext))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// A utility for CanRead().
        /// </summary>
        /// <remarks>
        /// The function searches the header of a file for a specific token
	    /// and returns true if this token is found. This works for text
	    /// files only. There is a rudimentary handling of UNICODE files.
	    /// The comparison is case independent.
        /// </remarks>
        /// <param name="iosystem">IO System to work with</param>
        /// <param name="file">File name of the file</param>
        /// <param name="tokens">List of tokens to search for</param>
        /// <param name="searchBytes">Size of the token array</param>
        /// <param name="tokensSol">Number of bytes to be searched for the tokens.</param>
        public static bool SearchFileHeaderForToken(IOSystem iosystem, string file, string[] tokens, int searchBytes = 200, bool tokensSol = false)
        {
            Debug.Assert(tokens != null && tokens.Length > 0 && searchBytes != 0);
            if (iosystem == null)
            {
                return false;
            }

            var stream = iosystem.Open(file);
            if (stream != null)
            {
                var buffer = new byte[searchBytes];
                var read = stream.Read(buffer, 0, searchBytes);
                if (read == 0)
                {
                    return false;
                }

                throw (new NotImplementedException());

            }
            return true;
        }
    }
}

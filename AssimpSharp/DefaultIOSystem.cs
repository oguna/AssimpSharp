using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace AssimpSharp
{
    /// <summary>
    /// Default implementation of IOSystem
    /// </summary>
    public class DefaultIOSystem : IOSystem
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DefaultIOSystem()
        { }

        /// <summary>
        /// Tests for the existence of a file at the given path.
        /// </summary>
        public override bool Exists(string file)
        {
            return File.Exists(file);
        }

        /// <summary>
        /// Returns the directory separator.
        /// </summary>
        public char GetOsSeparator()
        {
            return Path.PathSeparator;
        }

        /// <summary>
        /// Open a new file with a given path.
        /// </summary>
        public override Stream Open(string strfile, FileMode mode = FileMode.Open)
        {
            Debug.Assert(strfile != null);
            Debug.Assert(
                mode != null);

            var file = new FileStream(strfile, mode);
            return file;
        }

        /// <summary>
        /// Closes the given file and releases all resources associated with it.
        /// </summary>
        public override void Close(Stream file)
        {
            if (file != null)
            {
                file.Dispose();
            }
        }

        /// <summary>
        /// Compare two paths
        /// </summary>
        public override bool ComparePaths(string one, string second)
        {
            if (string.Compare(one, second) == 0)
            {
                return true;
            }

            string temp1;
            string temp2;
            MakeAbsolutePath(one, out temp1);
            MakeAbsolutePath(second, out temp2);
            return string.Compare(temp1, temp2) == 0;
        }

        /// <summary>
        /// Convert a relative path into an absolute path
        /// </summary>
        private void MakeAbsolutePath(string input, out string output)
        {
            output = Path.GetFullPath(input);
        }
    }
}

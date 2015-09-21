using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    [Flags]
    public enum ImporterFlags
    {
        SupportTextFlavour = 0x1,
        SupportBinaryFlavour = 0x2,
        SupportCompressedFlavour = 0x4,
        LimitedSupport = 0x8,
        Experimental = 0x10,
    }

    public class ImporterDesc
    {
        /// <summary>
        /// Full name of the importer (i.e. Blender3D importer)
        /// </summary>
        public string Name;
        /// <summary>
        /// Original author (left blank if unknown or whole assimp team)
        /// </summary>
        public string Author;
        /// <summary>
        /// Current maintainer, left blank if the author maintains
        /// </summary>
        public string Maintainer;
        /// <summary>
        /// Implementation comments, i.e. unimplemented features
        /// </summary>
        public string Comment;
        /// <summary>
        /// Any combination of the #aiLoaderFlags enumerated values.
	    /// These flags indicate some characteristics common to many
		/// importers.
        /// </summary>
        public ImporterFlags Flags;
        /// <summary>
        /// Minimum format version that can be loaded im major.minor format,
	    /// both are set to 0 if there is either no version scheme 
		/// or if the loader doesn't care.
        /// </summary>
        public int MinMajor;
        public int MinMinor;
        /// <summary>
        /// Maximum format version that can be loaded im major.minor format,
	    /// both are set to 0 if there is either no version scheme 
		/// or if the loader doesn't care. Loaders that expect to be
		/// forward-compatible to potential future format versions should 
		/// indicate  zero, otherwise they should specify the current
		/// maximum version.
        /// </summary>
        public int MaxMajor;
        public int MaxMinor;
        /// <summary>
        /// List of file extensions this importer can handle.
	    /// List entries are separated by space characters.
		/// All entries are lower case without a leading dot (i.e.
		/// "xml dae" would be a valid value. Note that multiple
		/// importers may respond to the same file extension -
		/// assimp calls all importers in the order in which they
		/// are registered and each importer gets the opportunity
		/// to load the file until one importer "claims" the file. Apart
		/// from file extension checks, importers typically use
		/// other methods to quickly reject files (i.e. magic
		/// words) so this does not mean that common or generic
		/// file extensions such as XML would be tediously slow.
        /// </summary>
        public string FileExtensions;
    }
}

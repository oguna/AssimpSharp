using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public class Document
    {
        private ImporterSettings settings;
        private Dictionary<ulong, LazyObject> objects = new Dictionary<ulong, LazyObject>();
        private Parser parser;
        private Dictionary<string, PropertyTable> templates = new Dictionary<string, PropertyTable>();
        private Dictionary<ulong, List<Connection>> srcConnections = new Dictionary<ulong, List<Connection>>();
        private Dictionary<ulong, List<Connection>> destConnections = new Dictionary<ulong, List<Connection>>();
        private uint fbxVersion;
        private string creator;
        private uint[] creationTimeStamp = new uint[7];
        private List<ulong> animationStacks = new List<ulong>();
        private List<AnimationStack> animationStacksResolved = new List<AnimationStack>();
        private FileGlobalSettings globals;



        public Document(Parser parser, ImporterSettings settings)
        {
            this.settings = settings;
            this.parser = parser;

            for(int i=0; i<7 ; i++)
            {
                creationTimeStamp[i] = 0;
            }

            ReadHeader();
            ReadPropertyTemplates();
            ReadGlobalSettings();
            ReadObjects();
            ReadConnections();
        }

        private void ReadHeader()
        {
            Scope sc = parser.RootScope;
            Element ehead = sc["FBXHeaderExtension"];
            if (ehead == null || ehead.Compound == null)
            {
                throw (new Exception());
            }

            Scope shead = ehead.Compound;
            fbxVersion = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(shead, "FBXVersion", ehead), 0));

            if (fbxVersion < 7100)
            {
                throw (new Exception("unsupported, old format version, supported are only FBX 2011, FBX 2012 and FBX 2013"));
            }
            if (fbxVersion > 7300)
            {
                if (settings.StrictMode)
                {
                    throw (new Exception("unsupported, newer format version, supported are only FBX 2011, FBX 2012 and FBX 2013 (turn off strict mode to try anyhow)"));
                }
                else
                {
                    throw (new Exception("unsupported, newer format version, supported are only FBX 2011, FBX 2012 and FBX 2013, trying to read it nevertheless"));
                }
            }

            Element ecreator = shead["Creator"];
            if (ecreator!= null)
            {
                creator = Parser.ParseTokenAsString(Parser.GetRequiredToken(ecreator, 0));
            }

            Element etimestamp = shead["CreationTimeStamp"];
            if (etimestamp != null && etimestamp.Compound != null)
            {
                Scope stimestamp = etimestamp.Compound;
                creationTimeStamp[0] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Year"), 0));
                creationTimeStamp[1] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Month"), 0));
                creationTimeStamp[2] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Day"), 0));
                creationTimeStamp[3] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Hour"), 0));
                creationTimeStamp[4] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Minute"), 0));
                creationTimeStamp[5] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Second"), 0));
                creationTimeStamp[6] = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Parser.GetRequiredElement(stimestamp, "Millisecond"), 0));
            }
        }

        private void ReadGlobalSettings()
        {
            Scope sc = parser.RootScope;
            Element ehead = sc["GlobalSettings"];
            if (ehead == null || ehead.Compound == null)
            {
                Console.Error.WriteLine("no GlobalSettings dictionary found");
                globals = new FileGlobalSettings(this, new PropertyTable());
                return;
            }
            PropertyTable props = DocumentUtil.GetPropertyTable(this, "", ehead, ehead.Compound, true);
            if (props == null)
            {
                throw(new Exception("GlobalSettings dictionary contains no property table"));
            }
            globals = new FileGlobalSettings(this, props);
            var test = globals.TimeSpanStop.Value;
        }

        private void ReadObjects()
        {
            Scope sc = parser.RootScope;
            Element eobjects = sc["Objects"];
            if (eobjects == null || eobjects.Compound == null)
            {
                throw (new Exception("no Objects dictionary found"));
            }
            objects[0] = new LazyObject(0, eobjects, this);
            Scope sobjects = eobjects.Compound;
            foreach(var el_ in sobjects.Elements)
            {
                foreach (var el in el_.Value)
                {
                    // extra ID
                    List<Token> tok = el.Tokens;
                    if (tok.Count == 0)
                    {
                        throw (new Exception("expected ID after object key"));
                    }
                    string err;
                    var id = Parser.ParseTokenAsID(tok[0], out err);
                    if (!string.IsNullOrEmpty(err))
                    {
                        throw (new Exception(err));
                    }

                    // id=0 is normally implicit
                    if (id == 0)
                    {
                        throw (new Exception("encountered object with implicitly defined id 0"));
                    }
                    if (objects.ContainsKey(id))
                    {
                        Console.Error.WriteLine("encountered duplicate object id, ignoring first occurrence");
                    }
                    objects[id] = new LazyObject(id, el, this);

                    // grab all animation stacks upfront since there is no listing of them
                    if (el_.Key == "AnimationStack")
                    {
                        animationStacks.Add(id);
                    }
                }
            }
        }

        private void ReadPropertyTemplates()
        {
            Scope sc = parser.RootScope;
            Element edefs = sc["Definitions"];
            if (edefs == null || edefs.Compound == null)
            {
                Console.Error.WriteLine("no Definitions dictionary found");
                return;
            }

            Scope sdefs = edefs.Compound;
            List<Element> otypes = sdefs.GetCollection("ObjectType");
            foreach(Element el in otypes)
            {
                Scope sc2 = el.Compound;
                if (sc2 == null)
                {
                    Console.Error.WriteLine("expected nested scope in ObjectType, ignoring");
                    continue;
                }
                List<Token> tok = el.Tokens;
                if (tok.Count == 0)
                {
                    Console.Error.WriteLine("expected name for ObjectType element, ignoring");
                    continue;
                }
                string oname = Parser.ParseTokenAsString(tok[0]);
                List<Element> templs = sc2.GetCollection("PropertyTemplate");
                if (templs != null)
                {
                    foreach (Element el3 in templs)
                    {
                        Scope sc3 = el3.Compound;
                        if (sc3 == null)
                        {
                            Console.Error.WriteLine("expected nested scope in PropertyTemplate, ignoring");
                            continue;
                        }
                        List<Token> tok2 = el3.Tokens;
                        if (tok2.Count == 0)
                        {
                            Console.Error.WriteLine("expected name for PropertyTemplate element, ignoring");
                            continue;
                        }
                        string pname = Parser.ParseTokenAsString(tok2[0]);
                        Element properties70 = sc3["Properties70"];
                        if (properties70 != null)
                        {
                            PropertyTable props = new PropertyTable(properties70, new PropertyTable());
                            templates[oname + "." + pname] = props;
                        }
                    }
                }
            }
        }

        private void ReadConnections()
        {
            Scope sc = parser.RootScope;
            Element econns = sc["Connections"];
            if (econns == null || econns.Compound == null)
            {
                throw (new Exception("no Connections dictionary found"));
            }

            ulong insertionOrder = 01;
            Scope sconns = econns.Compound;
            List<Element> conns = sconns.GetCollection("C");
            foreach (var el in conns)
            {
                string type = Parser.ParseTokenAsString(Parser.GetRequiredToken(el, 0));
                ulong src = Parser.ParseTokenAsID(Parser.GetRequiredToken(el, 1));
                ulong dest = Parser.ParseTokenAsID(Parser.GetRequiredToken(el, 2));

                string prop = (type == "OP" ? Parser.ParseTokenAsString(Parser.GetRequiredToken(el,3)) : "");
                if (!objects.ContainsKey(src))
                {
                    Console.Error.WriteLine("source object for connection does not exist");
                    continue;
                }
                if (!objects.ContainsKey(dest))
                {
                    Console.Error.WriteLine("destination object for connection does not exist");
                    continue;
                }
                // add new connection
                Connection c = new Connection(insertionOrder++, src, dest, prop, this);
                if (srcConnections.ContainsKey(src))
                {
                    srcConnections[src].Add(c);
                }
                else
                {
                    srcConnections[src] = new List<Connection>() { c };
                }
                if (destConnections.ContainsKey(dest))
                {
                    destConnections[dest].Add(c);
                }
                else
                {
                    destConnections[dest] = new List<Connection>() { c };
                }
            }
        }

        public LazyObject GetObject(UInt64 id)
        {
            LazyObject it;
            if (objects.TryGetValue(id, out it))
            {
                return it;
            }
            else
            {
                return null;
            }
        }

        public bool IsBinary
        {
            get
            {
                return parser.IsBinary;
            }
        }

        public uint FBXVersion
        {
            get
            {
                return fbxVersion;
            }
        }

        public string Creator
        {
            get
            {
                return creator;
            }
        }

        public uint[] CreationTimeStamp
        {
            get
            {
                return creationTimeStamp;
            }
        }

        public FileGlobalSettings GlobalSettings
        {
            get
            {
                return globals;
            }
        }

        public Dictionary<string, PropertyTable> Templates()
        {
            return templates;
        }
        
        public Dictionary<ulong, LazyObject> Objects
        {
            get
            {
                return objects;
            }
        }

        public ImporterSettings Settings
        {
            get
            {
                return settings;
            }
        }

        public Dictionary<ulong, List<Connection>> ConnectionsBySource
        {
            get
            {
                return srcConnections;
            }
        }

        public Dictionary<ulong, List<Connection>> ConnectionsByDestination
        {
            get
            {
                return destConnections;
            }
        }

        public List<AnimationStack> AnimationStacks
        {
            get
            {
                if (animationStacksResolved.Count != 0 || animationStacks.Count == 0)
                {
                    return animationStacksResolved;
                }

                animationStacksResolved = new List<AnimationStack>(animationStacks.Count);
                foreach(var id in animationStacks)
                {
                    LazyObject lazy = GetObject(id);
                    AnimationStack stack = null;
                    if (lazy == null || (stack = lazy.Get() as AnimationStack) == null)
                    {
                        Debug.WriteLine("failed to read AnimationStack object");
                        continue;
                    }
                    animationStacksResolved.Add(stack);
                }
                return animationStacksResolved;
            }
        }

        public List<Connection> GetConnectionsBySourceSequenced(ulong source)
        {
            return GetConnectionsSequenced(source, ConnectionsBySource);
        }

        public List<Connection> GetConnectionsByDestinationSequenced(ulong dest)
        {
            return GetConnectionsSequenced(dest, ConnectionsByDestination);
        }


        public List<Connection> GetConnectionsBySourceSequenced(ulong dest, string classname)
        {
            return GetConnectionsBySourceSequenced(dest,new []{ classname});
        }

        public List<Connection> GetConnectionsByDestinationSequenced(ulong dest, string classname)
        {
            return GetConnectionsByDestinationSequenced(dest, new[] { classname });
        }

        public List<Connection> GetConnectionsBySourceSequenced(ulong source, string[] classnames)
        {
            return GetConnectionsSequenced(source, true, ConnectionsBySource, classnames);
        }

        public List<Connection> GetConnectionsByDestinationSequenced(ulong dest, string[] classnames)
        {
            return GetConnectionsSequenced(dest, false, ConnectionsByDestination , classnames);
        }


        // public List<AnimationStack> AnimationStaks { get; }

        private List<Connection> GetConnectionsSequenced(ulong id, Dictionary<ulong, List<Connection>> conns)
        {
            List<Connection> temp;
            List<Connection> range;
            if (!conns.TryGetValue(id, out range))
            {
                range = new List<Connection>();
            }
            temp = new List<Connection>(range.Count);
            foreach (Connection it in range)
            {
                temp.Add(it);
            }
            temp.Sort();
            return temp;
        }

        private List<Connection> GetConnectionsSequenced(ulong id, bool isSrc, Dictionary<ulong, List<Connection>> conns, string[] classnames)
        {
            Debug.Assert(classnames != null);
            Debug.Assert(classnames.Length > 0);

            int[] length = new int[classnames.Length];
            for (int i=0; i<classnames.Length; i++)
            {
                length[i] = classnames[i].Length;
            }
            List<Connection> temp;
            List<Connection> range;
            if (!conns.TryGetValue(id, out range))
            {
                range = new List<Connection>();
            }
            temp = new List<Connection>(range.Count);
            foreach(var it in range)
            {
                Token key = (isSrc ? it.LazyDestinationObject : it.LazySourceObject).Element.KeyToken;
                string obtype = key.StringContents;
                for(int i=0; i<classnames.Length; i++)
                {
                    Debug.Assert(classnames[i]!=null);
                    if (key.End - key.Begin == length[i] && classnames[i] == obtype)
                    {
                        obtype = null;
                        break;
                    }
                }
                if (obtype != null)
                {
                    continue;
                }
                temp.Add(it);
            }
            temp.Sort();
            return temp;            
        }
    }
}

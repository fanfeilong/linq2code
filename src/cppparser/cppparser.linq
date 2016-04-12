<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <IncludePredicateBuilder>true</IncludePredicateBuilder>
</Query>

void Main() {
    var pwd = Path.GetDirectoryName(Util.CurrentQueryPath);
    var root = Path.Combine(pwd, @"src\P2PFramework").Dump();
    var codebase = new CodeBase(root);

    var cpp = codebase.AllCppFiles.Where(f => f.Contains("Impl")).Where(f=>f.Contains("ServiceName"));
    foreach (var c in cpp) {
        var blocks = c.FileBlocks().ToList();
        blocks.Dump();
    }
}

// Define other methods and classes here
public class ControlBlock {
    public ControlBlock Parent { get; set; }
    public List<ControlBlock> Children { get; private set; }
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public string HeadBody { get; set; }
    public string Owner { get; set; }
    public string Tail { get; set;}

    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    public List<string> Codes { get; set; }
    public int AddRefCount { get; set; }
    public int GetRefCount { get; set; }
    public int ReleaseCount { get; set; }
    public int PersistCount { get; set; }
    public List<string> LeakVariables { get; set; }

    public bool Close { get; set; }

    public void AddLine(string line) {

        this.Codes.Add(line);
        if (this.Parent != null) {
            this.Parent.AddLine(line);
        }
    }

    public ControlBlock() {
        Codes = new List<string>();
        LeakVariables = new List<string>();
        Children = new List<ControlBlock>();
        Close = false;
        ReturnType = "void";
        Tail="";
    }
}

public class ClassBlock {
    public List<ControlBlock> ConstructBlocks { get; set; }
    public ControlBlock DestructBlock { get; set; }
    public List<ControlBlock> PublicBlocks { get; set; }
    public List<ControlBlock> ProtectedBlocks { get; set; }
    public List<ControlBlock> PrivateBlocks { get; set; }
}

public class CodeBase {
    private List<string> allCppFileNames;
    private List<string> allHFileNames;

    public string Root {
        get;
        set;
    }

    public List<string> AllCppFiles {
        get {
            return allCppFileNames;
        }
    }

    public List<string> AllHFiles {
        get {
            return allHFileNames;
        }
    }

    public string GetBuddyFile(string src) {
        var dir = Path.GetDirectoryName(src);
        var ext = Path.GetExtension(src);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(src);
        if (ext == ".cpp") {
            var buddyFileName = Path.Combine(dir, fileNameWithoutExt + ".h");
            return allHFileNames.FirstOrDefault(h => h == buddyFileName);
        } else if (ext == ".h") {
            var buddyFileName = Path.Combine(dir, fileNameWithoutExt + ".cpp");
            return allCppFileNames.FirstOrDefault(h => h == buddyFileName);
        } else {
            Debug.Assert(false);
            return null;
        }
    }

    public CodeBase(string root) {
        Root = root;
        Init();
    }

    public void Init() {
        allCppFileNames = Root.GetAllFileNames("*.cpp").ToList();
        allHFileNames = Root.GetAllFileNames("*.h").ToList();
    }
}

public class LexerState {
    public List<string> Lines { get; set; }
    public string File { get; set; }
    public LexerState(string file) {
        File = file;
    }
}

public class ParserState {
    public List<string> Buffers { get; set; }

    public int LineNumber { get; set; }
    public string Last { get; set; }
    public string Current { get; set; }

    public Stack<ControlBlock> BlockStack { get; set; }
    public ControlBlock CurrentBlock { get; set; }


    public LexerState Lexer {
        get;
        private set;
    }

    public ParserState(LexerState l) {
        Lexer = l;
        Buffers = new List<string>();
        BlockStack = new Stack<ControlBlock>();
        LineNumber = 1;
    }
}

public static class CodeBaseExtension {
    public static IEnumerable<ControlBlock> FileBlocks(this string file) {
        var lexer = new LexerState(file);
        var parser = new ParserState(lexer);
        var blocks = parser.Parse();
        return blocks;
    }
}

public static class LexerExtension {
    public static IEnumerable<string> Next(this LexerState l) {
        var allText = File.ReadAllText(l.File);
        var lines = new List<string>();
        var level = 0;
        int index = 0;
        int count = allText.Length;
        var line = new StringBuilder();
        char lastc = char.MinValue;
        var enterfor=false;
        var semicount = 0;

        while (index < count) {
            if (index >= 1) {
                lastc = allText[index - 1];
            } 
            char c = allText[index++];
            
            if (c == '\r' || c == '\n') {
                var nc = allText[index];
                if (nc == '\r' || nc == '\n') {
                    index++;
                }
                var tmp = line.ToString();
                if (tmp.Contains("include")) {
                    var space = new string('\t', level);
                    lines.Add(string.Format("{0}{1}", space, line.ToString().Trim()));
                    line.Clear();
                }
                continue;
            } else if (c == '{') {
                var space = new string('\t', level);
                lines.Add(string.Format("{0}{1}", space, line.ToString().Trim()));
                lines.Add(string.Format("{0}{{", space));
                line.Clear();
                level++;
                continue;
            } else if (c == '}') {
                var preLine = line.ToString().Trim();
                if (!string.IsNullOrEmpty(preLine)) {
                    var space1 = new string('\t', level);
                    lines.Add(string.Format("{0}{1}", space1, preLine));
                }

                level--;
                var space = new string('\t', level);

                int j = index;
                bool f = false;
                while (index < count) {
                    c = allText[j++];
                    if (c == ' ') {
                        continue;
                    }
                    if (c == ';') {
                        f = true;
                        index = j;
                        break;
                    }
                    break;
                }
                lines.Add(string.Format("{0}}}{1}\r\n", space, f ? ";" : ""));
                line.Clear();
                continue;
            } else if (c == ';') {

                line.Append(c);
                if (enterfor) {
                    semicount++;
                    if (semicount <=2) {
                        continue;
                    } else {
                        enterfor = false;
                        semicount=0;
                    }
                    
                }
                
                var space = new string('\t', level);
                var newLine = string.Format("{0}{1}", space, line.ToString().Trim());
                lines.Add(newLine);
                line.Clear();
                continue;
            } else if (c == '/') {
                int j = index;
                line.Append(c);

                var nc = allText[index];
                if (nc == '/') {
                    line.Append(nc);
                    index++;

                    while (index < count) {
                        c = allText[j++];
                        if (c == '\r' || c == '\n') {
                            nc = allText[j];
                            if (nc == '\r' || nc == '\n') {
                                j++;
                            }
                            break;
                        }
                        line.Append(c);
                    }
                    var space = new string('\t', level);
                    lines.Add(string.Format("{0}{1}", space, line.ToString().Trim()));
                    line.Clear();
                    index = j;
                    continue;
                }
            } else if (c == ' ' || c == '\t') {
                if (lastc == ' ' || lastc == '\t'||lastc=='\r'||lastc=='\n') {
                    // Do Nothing    
                } else {
                    line.Append(c);
                }
            } else {
                var skip = false;
                if (c == 'f') {
                    if (lastc == ' ' || lastc == '\t' || lastc == '\r' || lastc == '\n') {
                        // Do Nothing    
                        var j = index;
                        var nc = allText[j++];
                        if (nc == 'o') {
                            nc = allText[j++];
                            if (nc == 'r') {
                                nc = allText[j++];
                                if (nc == ' ' || nc == '\t' || nc == '\r' || nc == '\n' || nc == '(') {
                                    line.Append("for");
                                    index += 3;
                                    enterfor = true;
                                    skip = true;
                                }
                            }
                        }
                    }
                }
                
                if (!skip) {
                    line.Append(c);
                }
            }
        }
        l.Lines = lines.Where(ll => !string.IsNullOrWhiteSpace(ll)).ToList();
        return l.Lines;
    }
}

public static class ParserExtension {
    public static IEnumerable<ControlBlock> Parse(this ParserState p) {
        var next = p.Lexer.Next();
        var yieldable = false;
        foreach (var line in next) {
            p.Current = line;
            p.LineNumber++;

            if (p.CurrentBlock != null && p.CurrentBlock.Close == false) {
                p.CurrentBlock.AddLine(line);
            }

            do {
                // Block Begin
                if (p.PassBeginBlock()) {
                    break;
                }

                // Pass NonBlock
                if (p.CurrentBlock == null) {
                    break;
                }

                // Block End
                if (p.PassEndBlock()) {
                    if (p.BlockStack.Count == 0) {
                        yieldable = true;
                    }
                    break;
                }

                // AddRef
                if (p.PassAddRef()) {
                    break;
                }

                // GetRef
                if (p.PassGetRef()) {
                    break;
                }

                // Release
                if (p.PassRelease()) {
                    break;
                }

                // Persist
                if (p.PassPersit()) {
                    break;
                }
            } while (false);

            if (yieldable) {
                yield return p.CurrentBlock;
                p.CurrentBlock = null;
                yieldable = false;
            }

            p.Last = line;
        }
    }
    private static bool PassBeginBlock(this ParserState p) {
        if (p.Current.Trim() == "{") {

            // Create new block
            var b = new ControlBlock();
            b.LineStart = p.LineNumber - 1;
            b.AddLine(p.Last);
            b.AddLine(p.Current);


            // Stack Control Blocks
            if (p.BlockStack.Count > 0) {
                var top = p.BlockStack.Peek();
                b.Parent = top;
                b.Owner = top.Name;
                top.Children.Add(b);
            }

            p.CurrentBlock = b;
            p.BlockStack.Push(b);

            bool pass = true;
            do {
                // xxx_API(r) function(...){}
                if (p.PassAPIHeader()) {
                    break;
                }

                // r o::m(...){}
                if (p.PassMethod()) {
                    break;
                }

                // o::c(...){}
                // ~o::c(...){}
                if (p.PassCtorAndDtor()) {
                    break;
                }

                // if(...){} 
                // else if(){}
                // else{}
                // for(...){}
                // while(...){}
                // do{ }while(...);
                // switch(...){}
                if (p.PassControl()) {
                    break;
                }

                // {}  
                pass = false;
            } while (false);

            // anonymous block
            if (!pass) {
                b.Codes.RemoveAt(0);
            }

            return pass;
        } else {
            return false;
        }
    }
    private static bool PassEndBlock(this ParserState p) {
        if (p.Current.Trim() == "}") {
            p.CurrentBlock.LineEnd = p.LineNumber;
            p.CurrentBlock.Close = true;
            p.BlockStack.Pop();

            if (p.BlockStack.Count > 0) {
                p.CurrentBlock = p.BlockStack.Peek();
            } else {
                
            }

            return true;
        } else {
            return false;
        }
    }
    private static bool PassAddRef(this ParserState p) {
        if (p.Current.Contains("AddRef")) {
            string v = null;

            do {
                var m = @"AddRef\(\s*(\w+)\s*,";
                v = p.Current.ValueAt(m, 1);
                if (v != null) break;

                m = @"\s*(\w+)\s*->AddRef\(\s*\)";
                v = p.Current.ValueAt(m, 1);
                if (v != null) break;

            } while (false);

            if (v != null) {
                p.CurrentBlock.LeakVariables.Add(v);
                p.CurrentBlock.AddRefCount++;
            } else {
                Debug.Assert(false);
            }

            return true;
        } else {
            return false;
        }
    }
    private static bool PassGetRef(this ParserState p) {
        if (p.Current.Contains("Get")) {
            var sub = p.Current.Substring(p.Current.IndexOf("Get") + 3);
            if (!sub.StartsWith("Instance") &&
                !sub.StartsWith("TypeInfo") &&
                !sub.StartsWith("ClassData") &&
                !sub.StartsWith("Capacity") &&
                !sub.StartsWith("Count")) {

                var m = @".*\s+(\w+)\s+=";
                var v = p.Current.ValueAt(m, 1);
                if (v != null) {
                    p.CurrentBlock.LeakVariables.Add(v);
                    p.CurrentBlock.GetRefCount++;
                } else {
                    Debug.Assert(false);
                }
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }
    private static bool PassRelease(this ParserState p) {
        if (p.Current.Contains("Release")) {

            string v = null;
            do {
                var m = @"Release\(\s*(\w+)\s*,";
                v = p.Current.ValueAt(m, 1);
                if (v != null) break;

                m = @"\s*(\w+)\s*->Release\(\s*\)";
                v = p.Current.ValueAt(m, 1);
                if (v != null) break;

            } while (false);

            if (v != null) {
                p.CurrentBlock.LeakVariables.Remove(v);
                p.CurrentBlock.ReleaseCount++;
            } else {
                Debug.Assert(false);
            }
            return true;
        } else {
            return false;
        }
    }
    private static bool PassPersit(this ParserState p) {
        string v = null;
        do {
            // m_xxx = y
            var m = @"\s+m_\w+\s+=\s*(\w+)\s+";
            v = p.Current.ValueAt(m, 1);
            if (v != null) {
                break;
            }

            // m_xxxs.push_back(y)
            m = @"\s+m_\w+.push_back(\s*(\w+)\s*)";
            v = p.Current.ValueAt(m, 1);
            if (v != null) {
                break;
            }

            // m_xxxs.insert(std::make_pair(k,v))
            m = @"\s+m_\w+.insert\(\s*std::make_pair\(\s*\w+\s*,\s*(\w+)\s*\)\s*\)";
            v = p.Current.ValueAt(m, 1);
            if (v != null) {
                break;
            }
        } while (false);

        if (v != null) {
            p.CurrentBlock.LeakVariables.Remove(v);
            p.CurrentBlock.PersistCount++;
            return true;
        } else {
            return false;
        }
    }

    private static bool PassAPIHeader(this ParserState p) {
        var sb = new StringBuilder();
        sb.Char('+').Str("_API")    // API 
          .L('(')
            .Space('*')
                .B().Any('*').E()    // return type
            .Space('*')
          .R(')')

          .Space('+')
            .B().Word('*', '*').E()   // function name
          .Space('*')

          .L('(')
            .B().Any('*').E()        // function arguments
          .R(')');

        var m = sb.ToString();
        var vs = p.Last.ValuesAt(m, 1, 2, 3);
        if (vs.Count() == 3) {
            p.CurrentBlock.ReturnType = vs.ElementAt(0);
            p.CurrentBlock.Name = vs.ElementAt(1);
            p.CurrentBlock.HeadBody = vs.ElementAt(2);
            return true;
        } else {
            return false;
        }
    }
    private static bool PassMethod(this ParserState p) {
        var sb = new StringBuilder();
        sb.Space('*')

          .B().Any('*').E()         // return type
          .Space('+')

          .B().Char('+').E()          // class name
          .Space('*')

          .Str("::")
          .Space('*')

          .B().Char('+').E()          // method name
          .Space('*')

          .L('(')
            .B().Any('*').E()         // method arguments
          .R(')')
          .Space('*')
          
          .B().Any('*').E()           // tail
          
          ;

        var m = sb.ToString();
        var vs = p.Last.ValuesAt(m, 1, 2, 3, 4,5);
        if (vs.Count() >=4) {
            p.CurrentBlock.ReturnType = vs.ElementAt(0);
            p.CurrentBlock.Name = vs.ElementAt(2);
            p.CurrentBlock.HeadBody = vs.ElementAt(3);
            p.CurrentBlock.Owner = vs.ElementAt(1);

            if (vs.Count() >= 5) {
                p.CurrentBlock.Tail = vs.ElementAt(4);
            }
            
            return true;
        } else {
            return false;
        }
    }
    private static bool PassCtorAndDtor(this ParserState p) {
        var sb = new StringBuilder();
        sb.Space('*')

          .B().Char('+').E()            // class name
          .Space('*')

          .Str("::")
          .Space('*')

          .B().Char('~', '?').E()       // dtor 
          .Space('*')

          .B().Char('+').E()            // method name
          .Space('*')

          .L('(')
            .B().Any(@"[^\)]",'*').E()  // method arguments
          .R(')')
          .Space('*')

          .B().Any('*').E()             // tail

          ;

        var m = sb.ToString();
        var vs = p.Last.ValuesAt(m, 1, 2, 3, 4, 5);

        if (vs.Count() == 5&&vs.Where(v=>!string.IsNullOrWhiteSpace(v)).Count()>=2) {
            var lookup = vs.ElementAt(1);

            if (lookup == "~") {
                p.CurrentBlock.ReturnType = vs.ElementAt(1);
                p.CurrentBlock.Owner = vs.ElementAt(0);
                p.CurrentBlock.Name = vs.ElementAt(2);
                p.CurrentBlock.HeadBody = vs.ElementAt(3);
                return true;
            } else {
                p.CurrentBlock.Owner = vs.ElementAt(1);
                p.CurrentBlock.Name = vs.ElementAt(2);
                p.CurrentBlock.HeadBody = vs.ElementAt(3);
                p.CurrentBlock.Tail = vs.ElementAt(4);
                return true;
            }
        } else {
            return false;
        }
        
    }
    private static bool PassControl(this ParserState p) {
        var last = p.Last.Trim();

        var keywords = new[]{
            "if","else if","else","for","do","while","switch"
        };
        
        string keyword = null;
        foreach (var k in keywords) {
            if (last.StartsWith(k)) {
                keyword = k;
                break;
            }
        }

        if (keyword == "else" || keyword == "do") {
            p.CurrentBlock.Name = keyword;
            return true;
        }

        if (keyword!=null) {

            var sb = new StringBuilder();
            sb.B()
              .Str(@"\w+\s*\w*")          // if else if for do while
              .E()
              .Space('*')

              .L('(')
                .Space('*')
                    .B().Any('*').E()      // conditions
                .Space('*')
              .R(')');

            var m = sb.ToString();
            var vs = p.Last.ValuesAt(m, 1,2);

            if (vs.Count()==2) {
                p.CurrentBlock.Name = keyword;
                Debug.Assert(keyword==vs.ElementAt(0));
                p.CurrentBlock.HeadBody = vs.ElementAt(1);
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }
}

public static class PathExtension {
    public static IEnumerable<string> GetAllFileNames(this string path, string searchpattern) {
        if (Directory.Exists(path)) {
            foreach (var f in Directory.GetFiles(path, searchpattern)) {
                yield return f;
            }
        } else if (File.Exists(path)) {
            yield return path;
        } else {
            yield break;
        }
    }
}

public static class RegexExtension {
    public static string ValueAt(this string src, string pattern, int i) {
        var m = Regex.Match(src, pattern);
        if (m.Captures.Count > 0) {
            if (m.Groups.Count > i && i > 0) {
                return m.Groups[i].Value;
            }
        } else {
            int j=0;
        }
        return null;
    }

    public static IEnumerable<string> ValuesAt(this string src, string pattern, params int[] ia) {
        var m = Regex.Match(src, pattern);
        if (m.Captures.Count > 0) {
            foreach (var i in ia) {
                if (m.Groups.Count > i && i > 0) {
                    yield return m.Groups[i].Value;
                }
            }
        }
        yield break;
    }
    public static StringBuilder Word(this StringBuilder p, char left, char right) {
        p.AppendFormat(@"\s{0}\w+\s{1}", left, right);
        return p;
    }
    public static StringBuilder Space(this StringBuilder p, char c) {
        p.AppendFormat(@"\s{0}", c);
        return p;
    }
    public static StringBuilder Tab(this StringBuilder p, char c) {
        p.AppendFormat(@"\t{0}", c);
        return p;
    }
    public static StringBuilder Any(this StringBuilder p, char c) {
        p.AppendFormat(@".{0}", c);
        return p;
    }
    public static StringBuilder Any(this StringBuilder p, string w,char c) {
        p.AppendFormat(@"{0}{1}", w,c);
        return p;
    }
    public static StringBuilder Char(this StringBuilder p, char c) {
        p.AppendFormat(@"\w{0}", c);
        return p;
    }
    public static StringBuilder Char(this StringBuilder p, char w,char c) {
        p.AppendFormat(@"{0}{1}",w, c);
        return p;
    }
    public static StringBuilder B(this StringBuilder p) {
        p.Append(@"(");
        return p;
    }
    public static StringBuilder E(this StringBuilder p) {
        p.Append(@")");
        return p;
    }
    public static StringBuilder L(this StringBuilder p, char c) {
        switch (c) {
            case '{': p.Append(@"{{"); break;
            case '(': p.Append(@"\("); break;
            case '}': Debug.Assert(false); break;
            case ')': Debug.Assert(false); break;
            default: p.Append(c); break;
        }
        return p;
    }
    public static StringBuilder R(this StringBuilder p, char c) {
        switch (c) {
            case '{': Debug.Assert(false); break;
            case '(': Debug.Assert(false); break;
            case '}': p.Append(@"}}"); break;
            case ')': p.Append(@"\)"); break;
            default: p.Append(c); break;
        }
        return p;
    }
    public static StringBuilder Str(this StringBuilder p, string str) {
        p.Append(str);
        return p;
    }
}

// Define other methods and classes here

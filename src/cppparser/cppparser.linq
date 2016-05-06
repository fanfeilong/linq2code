<Query Kind="Program">
  <Output>DataGrids</Output>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <IncludePredicateBuilder>true</IncludePredicateBuilder>
</Query>

void Main() {
	//u.TestBuffer();
	//u.TestEatBlanceGroup();
	u.TestLexer();
	//u.TestParser();
	//u.TestCodeBase();
}

public static class u {
	public static void TestCodeBase() {
		var root = @"c:\src\codebase\";
		var codebase = new CodeBase(root);

		var blocks = codebase.Compile();
		blocks.Dump("blocks");

		var classes = blocks.Link();
		classes.Dump("classes");
	}
	public static void TestLexer() {
		var root = @"c:\src\testcase\";

		var files = root.GetAllFileNames("*.h");
		var step = 30;
		var start = step * 0;
		var length = Math.Min(files.Count().Dump("totollength") - start, step);

		foreach (var file in files.Skip(start).Take(length)) {
			var lexer = new LexerState(file);
			var tokens = lexer.Next();
			tokens.Dump(Path.GetFileName(file));
		}
	}
	public static void TestParser() {
		var root = @"c:\src\testcase\";
		var codebase = new CodeBase(root);

		var blocks = codebase.Compile();
		blocks.Dump("blocks");
	}
	public static void TestBuffer() {
		var l = new LexerState("");
		var strs = new[] { 
			"123+-*/(){}[(<>)]++++---<<<< <>>>> >====== = \r\n&&&&&& & %~ ; ;;;; ,,, ^^^^^^ ^",
			" ++c ++",
			"<<<< <>>>> >"
		};
		foreach (var str in strs) {
			l.ResetBuffer();
			foreach (var c in str) {
				l.BufferNew(c);
			}
			l.ShowLine();
		}
	}
	public static void TestEatBlanceGroup() {
		var str = "\"    fadsf   \' \'  \' \r\n    sa\"";
		int i=0;
		var r= str.EatEspaceChar(ref i,'"',true);
		r.Dump();
	}
}

// Define other methods and classes here
public enum BasicType {
	Void,
	Int8,UInt8,Int16,UInt16,Int32,UInt32,Int64,UInt64,
	Char,WChar,
	Bool,Boolean,
	Float,Double
}

public enum IdentifierType {
	FunctionName,
	ArrayName,
	VariableName,
	ClassName,
	StructName,
	UnionName,
	NamespaceName,
}

public enum KeywordType {
	Asm,Auto,
	If, ElseIf, Else,
	Switch, Case, Default, Break,
	Do, Whilie, For,
	Struct, Union, Enum, Class, Namespace,
	Include,
	Define, MacroNewLine,
	Ifdef, Ifndef, Def, UnDef, EndIf,
	TypeDef,
	Static,Public,Private,Protected,
	This,Friend,Virtual,Override,Final,
	New,Delete,
	Try,Catch,
	Return,
	BasicType,
	Const,Volatile,Register,
}

public enum OperatorType {
	Plus, Minus, Multiply, Div, Mod,
	BitOr, BitAnd, BitNot, BitXor,
	Or, And, Not,
	Less, Equals, Greater,
	Assign, LeftShift, RightShift,
	TemplateLeftBracket, TemplateRightBracket,
	QuestionMark,
	Inherent, DoubleColons,
	LeftBlock, RightBlock, LeftParenthesis, RightParenthesis, LeftBracket, RightBracket,
	Semicolons,
}

public enum TokenType {
	Identifier, // Name of functions, arrays, variables,classes
	Keyword,
	Constant,
	Operator
}

public class Token {
	public string Value { get; set; }
	public TokenType Type { get; set;}
}

public class ControlBlock {
    private ControlBlock parent = null;
    public ControlBlock Parent {
        get {
            return parent;
        }
        set {
            parent = value;
        }
    }
    public List<ControlBlock> Children { get; private set; }

    public List<string> Codes { get; set; }
    public int LineStart { get; set; }
    public int LineEnd { get; set; }

    private string kind = "";
    public string Kind {
        get {
            return kind;
        }
        set {
            kind = value;
            if (kind == "struct") {
                lastScopeAccessor = "public :";
            } else if (kind == "class") {
                lastScopeAccessor = "private :";
            } else if (kind == "enum") {
                lastScopeAccessor = "public :";
            }
        }
    }
    public string Accessor { get; set;}
    public string Prefix { get; set; }

    private string name;
    public string Name {
        get {
            return name;
        }
        set {
            name = value;
        }
    }
    public string Fields { get; set; }
    public string Postfix { get; set;}
    public bool Close { get; set; }
    public bool IsDeclare {get;set;}
    
    public List<string> LeakVariables { get; set; }
    public int AddRefCount { get; set; }
    public int GetRefCount { get; set; }
    public int ReleaseCount { get; set; }
    public int PersistCount { get; set; }

    private string lastScopeAccessor;
    public string LastScopeAccessor {
        get {
            if(lastScopeAccessor==null) return Accessor;
            return lastScopeAccessor;
        }
    }

    public void AddLine(string line) {
        if(line==null) return;

        this.Codes.Add(line);
        if (this.Parent != null) {
            this.Parent.AddLine(line);
        }

        var accessor = new[] { "public :", "protected :", "private :" };
        foreach (var a in accessor) {
            if (line.Trim() == a) {
                this.lastScopeAccessor = a;
                break;
            }
        }
    }

    public bool IsNamespace() {
        return Kind=="namespace";
    }

    public bool IsType() {
        var types = new[] { "class", "struct", "enum", "union"};
        foreach (var t in types) {
            if (t == Kind) return true;
        }
        return false;
    }

    private string fullName = "";
    public string FullName {
        get {
            return fullName;
        }
    }

    public string OwnerFullName {
        get {
            var pos = FullName.LastIndexOf("::");
            if (pos > 0) {
                return FullName.Substring(0, pos);
            } else {
                return FullName;
            }
        }
    }

    public bool IsMemeber() {
        return Kind=="method"||Kind=="constructor"||Kind=="destructor";
    }

    public bool IsMethod() {
        return Kind == "method" ;
    }

    public void Update() {
        if (IsMemeber()) {
            if (IsMethod()) {
                var signName = name + Fields + Postfix.TrimEnd(new char[] { ' ', ';' });
                updateFullName(signName);
            } else {
                var pos = Postfix.IndexOf(":");
                var signName = name + Fields + (pos>=0?Postfix.Substring(0,pos):Postfix).TrimEnd(new char[] { ' ', ';' });
                updateFullName(signName);
            }
        } else if (IsType()||IsNamespace()) {
            updateFullName(name);
        } else {
            updateFullName("");
        }

        foreach (var c in Children) {
            c.Update();
        }
    }

    private void updateFullName(string localName) {
        var compress = localName.Replace(" ", "");
        if (Parent != null) {
            fullName = Parent.FullName + "::" + compress;
        } else {
            fullName = compress;
        }
    }

    public ControlBlock() {
        Codes = new List<string>();
        LeakVariables = new List<string>();
        Children = new List<ControlBlock>();
        Close = false;
        Prefix = "void";
        Postfix="";
        Accessor = "public :";
        IsDeclare = false;
        Name = "";
    }
}

public class ClassBlock {
    public string FullName { get; private set;}
    public List<ControlBlock> ConstructBlocks { get; set; }
    public ControlBlock DestructBlock { get; set; }
    public List<ControlBlock> PublicBlocks { get; set; }
    public List<ControlBlock> ProtectedBlocks { get; set; }
    public List<ControlBlock> PrivateBlocks { get; set; }
    public ClassBlock(string fullName) {
        FullName = fullName;
        ConstructBlocks = new List<ControlBlock>();
        PublicBlocks = new List<ControlBlock>();
        ProtectedBlocks = new List<ControlBlock>();
        PrivateBlocks = new List<ControlBlock>();
    }
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
	public int next = 0;
	public int level = 0;
	
	private string[] spaces;

	public bool EnterTailBlock {get;set;}

	public int TabSize { get; private set;}
	public List<string> Lines { get; set; }
	public StringBuilder Word { get; set; }
	public StringBuilder Line { get; set; }
	public string Indent {
		get {
			Debug.Assert(level<spaces.Length,"ERROR: indent overflow.");
			return spaces[level];
		}
	}
	public int Count {
		get {
			return Code.Length;
		}
	}
	public string Code { get; set; }
	public char Current { get; set; }
	public char Last { get; set; }
	public char LastNonWhiteSpace { get; set;}

	public string LastLine { get; set; }
	public Stack<int> LastClassLevels { get; set;}
	
	public bool HasKeyword = false;


	public Char PeekChar() {
		if (next < Count) {
			return Code[next];
		} else {
			return Char.MinValue;
		}
	}
	public string Peek(int number) {
		int beg = 0;
		int end = 0;
		if (number >= 0) {
			beg = next;
			end = next + number;
		} else {
			beg = next-number;
			end = next;
		}

		if (beg < 0) {
			beg = 0;
		}
		if (end >= Count) {
			end = Count - 1;
		}
		
		if (beg<Count && end<Count && beg>=0 && end>=0) {
			return Code.Substring(beg, end - beg);
		} else {
			Debug.Assert(false,string.Format("ERROR: Peek overflow. {0}",FullFileName));
			return "";
		}
	}
	public string PeekMatch(params string[] words) {
		var maxlen = words.Max(k => k.Length);
		var minlen = words.Min(k => k.Length);
		var matches = words.ToList();
		
		if(minlen>Count-next) return null;
		var hint = false;
		string result=null;
		for (int i = 0; i < maxlen; i++) {
			var ii = next+i;
			if(ii>=Count) break;

			
			var cc = Code[ii];
			var remove = new List<int>();
			for (int j = 0; j < matches.Count; j++) {
				var m = matches[j];
				if (i == m.Length - 1) {
					if (m[i] != cc) {
						remove.Add(j);
					} else {
						hint = true;
						result = m;
						break;
					}
				} else {
					if (m[i] != cc) {
						remove.Add(j);
					}
				}
			}

			if (hint) {
				break;
			}
			
			var k = remove.Count-1;
			while (k >= 0) {
				matches.RemoveAt(remove[k]);
				k--;
			}
		}
		return result;
	}
	
    public string FullFileName { get; private set; }
    public LexerState(string file) {
		Line = new StringBuilder();
		Word = new StringBuilder();
		Lines = new List<string>();
		FullFileName = file;
		TabSize = 4;
		levels = new Stack<int>();
		spaces = new string[64];
		for (int i = 0; i < 64; i++) {
			spaces[i] = new string(' ', i*TabSize);
		}
    }

	public LexerState Prepare() {
		this.LastClassLevels = new Stack<int>();
		this.Code = File.ReadAllText(FullFileName);
		return this;
	}

	public Stack<int> levels;
	// can be split
	public readonly char[] spaceTokens = new[]{
		'(', ')', '[', ']', '{', '}',
		';', ','
	};

	// maybe continue
	// ----------------------------------------
	// TODO: 
	// i-->5;
	// p->test();
	// p.->test();
	public readonly char[] spaceTokens2 = new[]{
		'+','-','/', '*', '^', '%','~',
		'>','<',
		'|','&',
		'=',
		':','!',
	};

	public char last = char.MinValue;
	public bool lastescape = false;
	public bool enterstr = false;
	
	public char SemicolonsChar = ';';
	public char LastSemicolonsChar = ';';
	
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
    public static IEnumerable<ControlBlock> Compile(this CodeBase code) {
        return code.AllHFiles.SelectMany(h=>h.FileBlocks())
               .Union(
               code.AllCppFiles.SelectMany(cpp=>cpp.FileBlocks()));
    }
}

public static class LexerBufferExtension {
	public static LexerState PushLevel(this LexerState l, int n) {
		l.levels.Push(l.level);
		l.level = n;
		return l;
	}
	public static LexerState PopLevel(this LexerState l) {
		l.level = l.levels.Pop();
		return l;
	}

	private static LexerState AddLine(this LexerState l, string newLine) {
		if (!string.IsNullOrWhiteSpace(newLine)) {
			//newLine = newLine.TrimEnd().Replace(' ', '@');
			l.Lines.Add(newLine);
		}
		return l;
	}	
	public static LexerState Save(this LexerState l) {
		if (l.Lines.Count > 0) {
			l.LastLine = l.Lines.Last();
		}
		l.BufferWord();
		if (l.Line.Length>0) {
			var newLine = string.Format("{0}{1}", l.Indent, l.Line.ToString());
			l.AddLine(newLine);
		}
		l.Line.Clear();
		return l;
	}
	public static LexerState SaveNew(this LexerState l, string format, params object[] args) {
		if (l.Lines.Count > 0) {
			l.LastLine = l.Lines.Last();
			Debug.Assert(l.Line.Length==0,"ERROR: SaveNew iff L.Line is empty.");
		}
		
		string newLine = null;
		if (args.Length > 0) {
			newLine = string.Format("{0}{1}", l.Indent, string.Format(format, args));
		} else {
			newLine = string.Format("{0}{1}", l.Indent, format);
		}

		l.AddLine(newLine);

		return l;
	}

	public static LexerState ResetBuffer(this LexerState l) {
		l.Word.Clear();
		l.Line.Clear();
		return l;
	}
	public static LexerState ShowLine(this LexerState l) {
		l.BufferWord();
		("'"+l.Line.ToString().Replace(' ','@')+"'").Dump();
		return l;
	}

	private static bool CanBufferSpace(this StringBuilder b){
		if (b.Length == 0) {
			return false;
		} else {
			var last = b[b.Length - 1];
			if (last.IsSpace()||last=='.'||last=='~') {
				return false;
			} else {
				return true;
			}
		}
	}

	private static void BufferSpace(this LexerState l) {
		if (l.Line.CanBufferSpace()||l.enterstr) {
			l.Line.Append(' ');
		}
	}
	private static void BufferWord(this LexerState l) {
		if (l.Word.Length > 0) {
			l.BufferSpace();
			for (int i = 0; i < l.Word.Length; i++) {
				var cc = l.Word[i];
				l.Line.Append(cc);
			}
			l.BufferSpace();
			l.Word.Clear();
		}
	}
	private static bool change(this char left, char right) {
		if (left != right) {
			if (left == '-' && right == '>') {
				return false;
			} else if(left=='+'&&right=='='){
				return false;
			} else if (left == '-' && right == '=') {
				return false;
			} else if (left == '*' && right == '=') {
				return false;
			} else if (left == '/' && right == '=') {
				return false;
			} else if (left == '%' && right == '=') {
				return false;
			} else if (left == '~' && right == '=') {
				return false;
			} else if (left == '|' && right == '=') {
				return false;
			} else if (left == '&' && right == '=') {
				return false;
			} else if (left == '!' && right == '=') {
				return false;
			} else {
				return true;
			}
		} else {
			return false;
		}
	}
	private static void BufferNonSpace(this LexerState l, char c) {
		if (c == '\\') {
			l.lastescape = true;
		} else if (c == '"') {
			if (!l.lastescape) {
				l.enterstr = !l.enterstr;
			}
		}
		
		if (l.enterstr) {
			l.Line.Append(c);
		} else {
			if (l.last == '\\'&&c=='\\') {
				return ;
			}
		
			if (l.last.change(c)) {
				l.BufferWord();
			} 

			if (l.spaceTokens.Contains(c)) {
				l.BufferSpace();
				l.Line.Append(c);
				l.BufferSpace();
			} else if (!l.spaceTokens2.Contains(c)) {
				l.Line.Append(c);
			} else {
				l.Word.Append(c);
			}
		}
		
		l.last = c;
	}

	private static void BufferInternal(this LexerState l, char c) {
		Debug.Assert(!c.IsNewLine(),"WARNING: SHOULD NOT buffer a newline.");
		if (c.IsSpace()) {
			l.lastescape = false;
			l.BufferWord();
			l.BufferSpace();
		} else {
			l.BufferNonSpace(c);
		}
	}

	public static LexerState Buffer(this LexerState l) {
		l.BufferInternal(l.Current);
		return l;
	}
	public static LexerState BufferNew(this LexerState l, char c) {
		l.BufferInternal(c);
		return l;
	}
	public static LexerState BufferNew(this LexerState l, string format, params object[] args) {
		string str;
		if (args.Length > 0) {
			str = string.Format(format, args);
		} else {
			str = format;
		}

		foreach (var c in str) {
			l.BufferNew(c);
		}
		return l;
	}
	public static LexerState BufferKeyword(this LexerState l, string keyword) {
		l.HasKeyword = true;
		l.BufferNew(' ');
		l.Line.Append(keyword);
		l.BufferNew(' ');
		return l;
	}

	public static bool Step(this LexerState l,int i=1) {
		if (l.next < l.Count) {
			l.next += i;
			if (l.next == 0) {
				l.Current = ' ';
			} else {
				l.Current = l.Code[l.next - 1];
			}
			return true;
		} else {
			return false;
		}
	}
}

public static class LexerTokenExtension {
	public static bool CheckSemicolons(this LexerState l) {
		l.LastSemicolonsChar = l.SemicolonsChar;
		if (l.SemicolonsChar != ';') {
			var k = l.next;
			var cc = char.MinValue;
			var newLineCount = 0;
			var s = l.Code.Skip(ref k, c => {
				if (c.IsNewLine() && !cc.IsNewLine()) {
					newLineCount++;
					if (newLineCount == 2) {
						return false;
					}
				}
				cc = c;
				return true;
			});

			if (cc == ';') {
				l.SemicolonsChar = ';';
				return true;
			}
		}
		return false;
	}
	public static bool PassComment(this LexerState l) {
		Debug.Assert(l.Current == '/', "ERROR: l.Currnet SHOULD BE '/' .");
		if (l.next < l.Count) {
			var nc = l.Code[l.next];
			if (nc == '/') {
				int i = l.next + 1;
				int skipCount = l.Code.SkipSingleLineComment(ref i);
				l.next += skipCount + 1;
				return true;
			} else if (nc == '*') {
				int i = l.next + 1;
				int skipCount = l.Code.SkipMultilineComment(ref i);
				l.next += skipCount + 1;
				return true;
			} else {
				return false;
			}
		}
		return false;
	}
	public static bool PassBlankLines(this LexerState l) {
		if (!l.Current.IsNewLine() && l.next != 1 || (l.Current != l.Code[l.next - 1])) {
			Debug.Assert(l.Current.IsNewLine(), "ERROR: l.Currnet SHOULD BE '\r' or '\n' .");
		}
		int i = l.next;
		int skipCount = l.Code.SkipBlankLines(ref i);
		l.next += skipCount;
		return skipCount > 0;
	}
	public static bool PassSemiColons(this LexerState l) {
		Debug.Assert(l.Current == l.SemicolonsChar, "ERROR: l.Currnet SHOULD BE ';' .");
		l.HasKeyword = false;

		bool skip = false;
		do {
			int i = l.next;
			int skipCount = l.Code.SkipSpace(ref i);
			l.next += skipCount;

			var v = l.PeekChar();
			if (v == l.SemicolonsChar) {
				l.next++;
				skip = true;
			} else {
				var j = l.next;
				var nskipCount = l.Code.SkipSpaceOrNewLine(ref j);
				if (nskipCount > 0) {
					var vv = l.Code.PeekChar(j);
					if (vv == l.SemicolonsChar) {
						l.next += nskipCount + 1;
						skip = true;
					} else {
						skip = false;
					}
				} else {
					skip = false;
				}
			}

		} while (skip);
		
		l.BufferNew(l.SemicolonsChar);
		l.Save();
		l.CheckSemicolons();

		return true;
	}
	public static bool PassLeftBrace(this LexerState l) {
		Debug.Assert(l.Current == '{', "ERROR: l.Currnet SHOULD BE '{' .");

		l.EnterTailBlock = false;
		l.HasKeyword = false;

		var lastLine = l.Line.ToString();
		if (lastLine != null) {
			if (lastLine.Contains("typedef")) {
				l.EnterTailBlock = true;
			}
		}

		if (l.LastSemicolonsChar == '\\' || l.SemicolonsChar == '\\') {
			l.BufferNew('\\');
		}
		l.Save();
		l.BufferNew("{");
		
		if (l.LastSemicolonsChar == '\\' || l.SemicolonsChar == '\\') {
			l.BufferNew('\\');
		}
		l.Save();

		l.LastClassLevels.Push(l.level);
		l.level++;
		return true;
	}
	public static bool PassRightBrace(this LexerState l) {
		Debug.Assert(l.Current == '}', "ERROR: l.Currnet SHOULD BE '}' .");
		if (l.LastSemicolonsChar == '\\' || l.SemicolonsChar == '\\') {
			l.BufferNew('\\');
		}
		l.Save();
		l.level--;
		l.HasKeyword = false;
		if (l.LastClassLevels.Count > 0) {
			if (l.level == l.LastClassLevels.Peek()) {
				l.LastClassLevels.Pop();
			}
		}

		l.BufferNew("}");
		
		var i = l.next;
		var skipCount = 0;
		if (l.EnterTailBlock) {
			skipCount += l.Code.Skip(ref i, cc => {
				var valid = cc != l.SemicolonsChar;
				if (valid) {
					l.BufferNew(cc);
				}
				return valid;
			});
		}
		
		
		skipCount += l.Code.SkipSpaceOrNewLine(ref i);
		var nc = l.Code.PeekChar(i);
		if (nc == l.SemicolonsChar) {
			l.BufferNew(nc);
			skipCount += 1;
		} 
		l.next+=skipCount;

		if ((l.LastSemicolonsChar == '\\') || (l.SemicolonsChar == '\\'&&l.LastSemicolonsChar!='\\')) {
			l.BufferNew('\\');
		}

		l.Save();

		return true;
	}
	public static bool PassFor(this LexerState l) {
		//Debug.Assert(l.Current.IsSpaceOrNewLine(),"ERROR: prefix of for should be space or newline");
		var lookup = l.Peek(4);
		if (lookup.Length == 4 && lookup.Substring(0, 3) == "for" && (lookup[3].IsSpaceOrNewLine() || lookup[3] == '(')) {
			var headcount = (lookup[3] == '(' ? 3 : 4);
			int i = l.next + headcount;
			var compress = true;
			var r = l.Code.EatBlanceGroup(ref i,'(',')', compress);
			if (r != null) {
				var skipValue = r.Item2;
				var skipCount = r.Item1 + headcount;
				
				l.Save();
				l.BufferNew("for{0}", skipValue);
				l.Save();
				l.next += skipCount;
				return true;
			}
		}
		return false;
	}
	public static bool PassKeyWords(this LexerState l, params string[] keywords) {
		Debug.Assert(keywords.Count() > 0, "ERROR: Keywords is empty.");
		var r = l.PeekMatch(keywords);
		if (r != null) {
			if (!l.HasKeyword) {
				l.Save();
			}
			l.BufferKeyword(r).BufferNew(' ');
			l.next += r.Length;
			return true;
		} else {
			return false;
		}
	}
	public static bool PassLineByHeader(this LexerState l, params string[] keywords) {
		Debug.Assert(keywords.Count() > 0, "ERROR: Keywords is empty.");
		var r = l.PeekMatch(keywords);
		if (r != null) {
			l.Save();
			l.BufferNew(r);
			l.next += r.Length;
			
			l.enterstr = true;

			// eat until newline
			var lastdiv = false;
			var comment = false;
			var last = char.MinValue;
			var eatCount = l.Code.Skip(ref l.next, c => {
				var newLine = c.IsNewLine();
				if (!newLine) {
					if (c == '/') {
						if (lastdiv) {
							comment = true;
						}
						lastdiv = true;
					} else if (c == '*') {
						if (lastdiv) {
							comment = true;
						}
						lastdiv = false;
					} else {
						lastdiv = false;
					}
				}
				var valid = !newLine && !comment;
				if (valid) {
					if (lastdiv) {
						last = c;
					} else {
						if (last != char.MinValue) {
							l.BufferNew(last);
							last = char.MinValue;
						}
						l.BufferNew(c);
						//sb.Append(c);
					}
				}
				return valid;
			});

			if (comment) {
				// skip comments
				l.Current = '/';
				var skipCount = l.PassComment();
			}
			
			l.enterstr = false;
			
			// save the line
			l.Save();

			return true;
		} else {
			return false;
		}
	}
	public static bool PassAccessors(this LexerState l) {
		int i = l.next;
		int skipCount = l.Code.SkipSpace(ref i);

		var keywords = new[] { "public:", "protected:", "private:" };
		var r = l.PeekMatch(keywords);
		if (r != null) {
			l.Save();

			l.PushLevel(l.LastClassLevels.Peek());
			l.BufferNew(r);
			if (l.LastSemicolonsChar == '\\' || l.SemicolonsChar == '\\') {
				l.BufferNew('\\');
			}
			l.Save();
			l.PopLevel();


			l.next += r.Length + skipCount;
			l.Current = l.Code[l.next - 1];

			return true;
		} else {
			return false;
		}
	}
	public static bool PassChar(this LexerState l) {
		l.Buffer();
		l.next += 1;
		return true;
	}
	public static bool PassPackage(this LexerState l) {

		// TODO: opt

		if (l.PassFor()) {
			return true;
		}

		if (l.PassLineByHeader("#define ")) {
			if (l.Lines.Last().Trim().EndsWith("\\")) {
				l.LastSemicolonsChar = l.SemicolonsChar;
				l.SemicolonsChar = '\\';
			}
			return true;
		}

		if (l.PassLineByHeader(
			"#if ", "#ifdef ", "#ifndef ",
			"#error ", "#include ", "#import ", "case ", "default ", "default ")) {
			return true;
		}

		if (l.PassLineByHeader(
			"#endif", "default:")) {
			return true;
		}

		if (l.PassKeyWords(
			"typedef ", "template ",
			"class ", "struct ", "union ", "enum ", "friend class ")) {
			// patch a space
			l.next--;
			l.Current = l.Code[l.next - 1];
			return true;
		}

		if (l.PassKeyWords(
			"template<")) {
			return true;
		}

		if (l.PassAccessors()) {
			return true;
		}

		return false;
	}
	public static bool PassString(this LexerState l) {
		Debug.Assert(l.Current == '"', "ERROR: l.Current SHOULD be '\"'");
		var compress = false;
		int i = l.next-1;
		var r = l.Code.EatEspaceChar(ref i,'"',compress);
		if (r != null) {
			var skipValue = r.Item2;
			var skipCount = r.Item1-1;
			
			l.BufferNew(skipValue);
			l.next += skipCount;
			return true;
		}
		return false;
	}
}

public static class LexerExtension {
	public static IEnumerable<string> Next(this LexerState l) {
		l.Prepare();
		while (l.Step()) {

			if (!l.Current.IsSpaceOrNewLine()) {
				l.LastNonWhiteSpace = l.Current;
			}
			//l.ShowLine();
			foreach (var lt in l.Lines) {
				yield return lt;
			}
			l.Lines.Clear();

			if (l.Current == '"') {
				if(!l.PassString()) {
					Debug.Assert(false, "ERROR: PassString Failed.");
					break;
				} else {
					continue;
				}
			}

			if (l.Current == '{') {
				if (!l.PassLeftBrace()) {
					Debug.Assert(false, "ERROR: PassLeftBrace Failed.");
					break;
				} else {
					continue;
				}
			}

			if (l.Current == '}') {
				if (!l.PassRightBrace()) {
					Debug.Assert(false, "ERROR: PassRightBrace Failed.");
					break;
				} else {
					continue;
				}
			}

			if (l.Current == '/') {
				if (l.PassComment()) {
					continue;
				}
			}

			if (l.Current == l.SemicolonsChar) {
				if (!l.PassSemiColons()) {
					Debug.Assert(false, "ERROR: PassSemiColons Failed.");
					break;
				} else {
					continue;
				}
			}

			if (l.Current.IsNewLine() || l.next == 1) {
				l.CheckSemicolons();
				if (l.PassBlankLines()) {
					continue;
				} else {
					while (l.PeekChar().IsNewLine()) {
						l.Step();
					}
				}
			}

			if (l.Current.IsSpace()) {
				while (l.PeekChar().IsSpace()) {
					l.Step();
				}
			}
			
			if (l.Current.IsSpaceOrNewLine()) {

				if (l.PassPackage()) {
					continue;
				}

				if (l.Current.IsNewLine()) {
					l.PassBlankLines();
					l.BufferNew(' ');
					continue;
				}

				if (l.Current.IsSpace()) {
					l.Buffer();
					continue;
				}
				
				Debug.Assert(false,"Space or NewLine SHOULD be eaten.");
			}

			l.Step(-1);
			if (l.PassPackage()) {
				continue;
			}
			l.Step();
			
			l.Buffer();
		}
		foreach (var lt in l.Lines) {
			yield return lt;
		}
		yield break;
	}
}

public static class EatExtension {
	public static Tuple<int, string> EatEspaceChar(this string str, ref int index, char e, bool compress) {
		int i=index;
		var count = str.Length;
		var skipCount = 0;
		
		if (i > count) return null;

		var sb = new StringBuilder();
		var lastescape = false;
		var hint = false;

		Action eatSpace = () => {
			int j = i;
			int space = str.SkipSpaceOrNewLine(ref i);
			if (space > 0) {
				lastescape = false;
				if (compress) {
					sb.Append(' ');
				} else {
					sb.Append(str.Substring(j, space));
				}
				skipCount += space;
			}
		};
		eatSpace();
		if(i>=count) return null;

		var c = str[i++];
		sb.Append(c);
		skipCount++;
		Debug.Assert(c==e,string.Format("First NonSpace Char SHOULD be escape char: {0} .",e));

		while (true) {
			if(i>=count)break;
			eatSpace();
			if(i>=count)break;
			
			c = str[i++];
			skipCount++;
			
			sb.Append(c);
			if (c == '\\') {
				lastescape = true;
			} else if (c == e) {
				if (!lastescape) {
					hint = true;
					break;
				} 
				lastescape = false;
			} else {
				lastescape = false;
			}
		}

		if (hint) {
			var eat = sb.ToString();
			index += skipCount;
			return Tuple.Create(skipCount, eat);
		} else {
			return null;
		}
	}
	public static Tuple<int, string> EatBlanceGroup(this string str, ref int index,char left, char right, bool compress) {
		var keepspaceornewline = !compress;
		int skipCount = 0;
		int i = index;
		int count = str.Length;
		skipCount += str.SkipSpaceOrNewLine(ref i);
		bool hint = false;
		var sb = new StringBuilder();
		var lastspace = false;
		if (i >= count) {
			return null;
		}
		var stack = new Stack<char>();

		var skipNonParentheses = new Action(() => {
			skipCount += str.Skip(ref i, cc => {
				var valid = cc != left && cc != right;
				if (valid) {
					if (cc.IsSpaceOrNewLine()) {
						if (keepspaceornewline) {
							sb.Append(cc);
						} else {
							if (lastspace == false) {
								sb.Append(' ');
								lastspace = true;
							}
						}
					} else {
						sb.Append(cc); lastspace = false;
					}
				}
				return valid;
			});
		});

		while (true) {
			//sb.ToString().Dump();
			if (i >= count) break;
			var c = str[i++];
			skipCount++;
			bool error = false;

            if (c == left) {
				sb.Append(c); lastspace = false;
				stack.Push(c);
				skipNonParentheses();
			} else if (c == right) {
				sb.Append(c); lastspace = false;
				if (stack.Count > 0) {
					stack.Pop();
					if (stack.Count > 0) {
						skipNonParentheses();
					} else {
						hint = true;
					}
				} else {
					error = true;
				}
			}else{
				error = true;
			}

			if (error || hint) {
				break;
			}
		}

		if (hint) {
			var eat = sb.ToString();
			index += skipCount;
			return Tuple.Create(skipCount, eat);
		} else {
			return null;
		}
	}
}

public static class SkipExtension {
	public static Char PeekChar(this string str,int pos) {
		if (pos < str.Length) {
			return str[pos];
		} else {
			return Char.MinValue;
		}
	}
	
	public static bool IsSpace(this char c) {
		return c == ' ' || c == '\t';
	}
	public static bool IsNewLine(this char c) {
		return c == '\r' || c == '\n';
	}
	public static bool IsSpace(this string str, int pos) {
		return pos<str.Length&&str[pos].IsSpace();
	}
	public static bool IsNewLine(this string str, int pos) {
		return pos < str.Length && str[pos].IsNewLine();
	}
	public static bool IsSpaceOrNewLine(this char c) {
		return Char.IsWhiteSpace(c)||c==' '||c=='\t'||c=='\r'||c=='\n';
	}
	
	public static int SkipBlankLines(this string allText, ref int index) {
		if (index > 1) {
			Debug.Assert(allText[index - 1].IsNewLine(), "ERROR: BlankLines's Head must be '\r' or '\n' .");
		}

		if (!allText[index - 1].IsNewLine()) {

		}
		
		int skipCount = 0;
		int inc = 0;
		int count = allText.Length;
		int i = index;

		inc = allText.SkipBlankLine(ref i);
		while (inc > 0) {
			skipCount += inc;
			inc=0;
			if (allText.IsNewLine(i)) {
				int j=i+1;
				inc = allText.SkipBlankLine(ref j);
				if (inc > 0) {
					skipCount+=1;
				}
			}
		}

		index += skipCount;
		return skipCount;
	}
	public static int SkipBlankLine(this string allText, ref int index) {
		if (index > 1) {
			Debug.Assert(allText[index - 1].IsNewLine(), "ERROR: BlankLine's Head must be '\r' or '\n' .");
		}
        int i = index;
        bool blankline = false;
		int skipCount = allText.Skip(ref i, c => {
			if (c.IsSpace()) {
				return true;
			} else {
				if (c.IsNewLine()) {
					blankline = true;
				} else {
					blankline = false;
				}
				return false;
			}
        });

		if (blankline&&skipCount>0) {
			index+=skipCount;
			return skipCount;
		} else {
			return 0;
		}
    }
    public static int SkipSingleLineComment(this string allText, ref int index) {
		Debug.Assert(allText[index-1]=='/'&&allText[index-2]=='/',string.Format("ERROR: SingleLineComment's Head must be '//' Get:{0} .",allText.Substring(index-2,2)));
        int i = index;
        int count = allText.Length;

        int skipCount = allText.Skip(ref i, c => {
			return !c.IsNewLine();
        });
		
        index += skipCount;
        return skipCount;
    }
    public static int SkipMultilineComment(this string allText, ref int index) {
		Debug.Assert(allText[index-1]=='*'&&allText[index-2]=='/',string.Format("ERROR: MultilineComment's Head must be '//' Get:{0} .",allText.Substring(index-2,2)));
        int skipCount = 0;
        int i = index;
        int count = allText.Length;

        while (true) {
			// skip non star
			var hintstar = false;
			do {
				skipCount += allText.SkipInlineNonStar(ref i);
				if (allText.PeekChar(i) != '*') {
					hintstar = false;
					// if not end in line
					skipCount += allText.SkipNewLine(ref i);
				} else {
					hintstar = true;
				}
			}while(!hintstar);
			
            if (i >= count) {
                break;
			}
			
			char c = allText[i++];
			Debug.Assert(c == '*', "must be star!!!");
			skipCount++;

			// skip continues star
			skipCount += allText.SkipStar(ref i);

			if (i >= count) {
				break;
			}

			c = allText[i++];
			skipCount++;

			if (c == '/') {
				// hint "*/"
				skipCount += allText.SkipSpace(ref i);

				break;
			}
		}


		index += skipCount;
		
        return skipCount;
    }
    
	public static int SkipSpaceOrNewLine(this string allText, ref int index) {
        return allText.Skip(ref index, c => {
            var skips = new char[] { ' ', '\t', '\r', '\n' };
            return skips.Contains(c);
        });
	}
	public static int SkipSpace(this string allText, ref int index) {
		return allText.Skip(ref index, c => {
			var skips = new char[] { ' ', '\t'};
			return skips.Contains(c);
		});
	}
	public static int SkipNewLine(this string allText, ref int index) {
		return allText.Skip(ref index, c => {
			var skips = new char[] { '\r', '\n' };
			return skips.Contains(c);
		});
	}
	public static int SkipInlineNonStar(this string allText, ref int index) {
        return allText.Skip(ref index, c => {
            return c != '*'&&!c.IsNewLine();
        });
    }
    public static int SkipStar(this string allText, ref int index) {
        return allText.Skip(ref index, c => {
            return c == '*';
        });
    }
    public static int Skip(this string allText, ref int index, Func<char, bool> skip) {
        int skipCount = 0;
        int i = index;
        int count = allText.Length;

        if (i < count) {
            char c = allText[i++];
            while (skip(c)) {
                skipCount++;
                if (i < count) {
                    c = allText[i++];
                } else {
                    break;
                }
            }

        }
        index += skipCount;
        return skipCount;
    }
}

public static class ParserExtension {
	public static IEnumerable<ControlBlock> Parse(this ParserState p) {
		yield break;
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

public static class BlockExtension {
    public static IEnumerable<ClassBlock> Link(this IEnumerable<ControlBlock> blocks) {
        var functions = blocks.Where(b=>b.Kind=="function");
        var methods = blocks.Where(b=>b.IsMemeber());
        blocks.Where(b => b.IsType()).Dump("....");
        var classes = blocks.Where(b => b.IsType()).ToDictionary(b => b.FullName).Dump("types");
        var newClasses = new Dictionary<string,ClassBlock>();
        foreach (var cc in classes) {
            var c = cc.Value;
            if (!newClasses.ContainsKey(c.FullName)) {
                var nc = new ClassBlock(c.FullName);
                foreach (var sb in c.Children) {
                    nc.Add(sb);
                }
                newClasses.Add(c.FullName, nc);
            }
        }
        
        var freeClass = new ClassBlock("");
        freeClass.PublicBlocks.AddRange(functions);
        
        
        foreach (var b in methods) {
            if (classes.ContainsKey(b.OwnerFullName)){
                var c = classes[b.OwnerFullName];
                if (newClasses.ContainsKey(c.FullName)) {
                    var nc = newClasses[c.FullName];
                    nc.Add(b);
                } else {
                    var nc = new ClassBlock(c.FullName);
                    foreach (var sb in c.Children) {
                        nc.Add(sb);
                    }
                    nc.Add(b);
                    newClasses.Add(c.FullName,nc);
                }
            } else {
                freeClass.PublicBlocks.Add(b);
            }
        }

        yield return freeClass;
        foreach (var nc in newClasses) {
            yield return nc.Value;
        }
    }

    public static void Add(this ClassBlock cb, ControlBlock b) {
        switch (b.Kind) {
            case "constructor":
                if (!cb.ConstructBlocks.TryMerge(b)) {
                   cb.ConstructBlocks.Add(b);
                } 
                return;
            case "destructor":
                var buddy = cb.DestructBlock.Buddy(b);
                cb.DestructBlock = buddy;
                return;
            case "method":
                if (cb.PublicBlocks.TryMerge(b)) {
                    return;
                } else if (cb.ProtectedBlocks.TryMerge(b)) {
                    return;
                } else if (cb.PrivateBlocks.TryMerge(b)) {
                    return;
                } else {
                    switch (b.Accessor) {
                        case "public :": cb.PublicBlocks.Add(b); return;
                        case "protected :": cb.ProtectedBlocks.Add(b); return;
                        case "private :": cb.PrivateBlocks.Add(b); return;
                        default: b.Accessor = "public :"; cb.PublicBlocks.Add(b); return;
                    }
                }
            default:
                return;
        }
    }

    public static bool TryMerge(this List<ControlBlock> blocks, ControlBlock b) {
        int index= 0;
        int hint = -1;
        ControlBlock save = null;
        
        foreach (var cb in blocks) {
            var buddy = cb.Buddy(b);
            if ( buddy!= null) {
                hint = index;
                save = buddy;
            }
            index++;
        }

        if (save != null) {
            blocks.RemoveAt(hint);
            blocks.Insert(hint, save);
            return true;
        } else {
            return false;
        }
    }

    public static ControlBlock Buddy(this ControlBlock left, ControlBlock right) {
        if(left==null) return right;
        if(right==null) return left;
    
        if (left.Kind != right.Kind) {
            return null;
        }

        if (left.FullName == right.FullName) {
            if (left.IsDeclare) {
                Debug.Assert(right.IsDeclare==false,"Buddy Block");
                right.Accessor = left.Accessor;
                return right;
            } else {
                Debug.Assert(right.IsDeclare==true,"Buddy Block");
                left.Accessor = right.Accessor;
                return left;
            }
        } else {
            return null;
        }
    }
}

public static class EnumeratorExtension {
	public static IEnumerator<T> Skip<T>(this IEnumerator<T> e, int c) {
		while (c > 0 && e.MoveNext()) {
			c -= 1;
		}
		return e;
	}
	public static IEnumerable<T> Take<T>(this IEnumerator<T> e, int c) {
		while (c > 0 && e.MoveNext()) {
			yield return e.Current;
			c -= 1;
		}
	}
}


// Define other methods and classes here
<Query Kind="Program">
  <Output>DataGrids</Output>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <IncludePredicateBuilder>true</IncludePredicateBuilder>
</Query>

void Main() {
	u.TestCodeBase();
}

public static class u {
	public static void TestCodeBase() {
		var root = @"c:\src\";
		var codebase = new CodeBase(root);

		var blocks = codebase.Compile();
		blocks.Dump("blocks");

		var classes = blocks.Link();
		classes.Dump("classes");
	}
}

// Define other methods and classes here
public enum LineTokenType {
	BlankLine,
	Comment,
	For,
	If,
	ElseIf,
	Else,
	Switch,
	Case,
	Do,
	Whilie,
	Struct,
	Union,
	Enum,
	Class,
	Namespace,
	Initializer,
	Macro,
	Include,
	Ifdef,
	Ifndef,
	Def,
	UnDef,
	EndIf
}

public class LineToken {
	public string Value { get; set; }
	public LineTokenType Type { get; set;}
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
                lastScopeAccessor = "public:";
            } else if (kind == "class") {
                lastScopeAccessor = "private:";
            } else if (kind == "enum") {
                lastScopeAccessor = "public:";
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

        var accessor = new[] { "public:", "protected:", "private:" };
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
        Accessor = "public:";
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
	public bool KeepNewLineOnce { get; set; }
	public bool KeepMacroLines { get; set; }
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
			Debug.Assert(false,"ERROR: Peek overflow.");
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
		KeepNewLineOnce = false;
		Line = new StringBuilder();
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

public static class LexerExtension {
	public static LexerState PushLevel(this LexerState l,int n) {
		l.levels.Push(l.level);
		l.level = n;
		return l;
	}
	public static LexerState PopLevel(this LexerState l) {
		l.level = l.levels.Pop();
		return l;
	}
	public static LexerState Save(this LexerState l) {
		if (l.Lines.Count > 0) {
			l.LastLine = l.Lines.Last();
		}

		var str = l.Line.ToString();
		if (!string.IsNullOrWhiteSpace(str)) {
			var newLine = string.Format("{0}{1}", l.Indent,str);
			l.Lines.Add(newLine);
		}

		l.Line.Clear();
		return l;
	}
	public static LexerState SaveNew(this LexerState l,string format,params object[] args) {
		if (l.Lines.Count > 0) {
			l.LastLine = l.Lines.Last();
		}

		var newLine = string.Format("{0}{1}", l.Indent, format, args);
		l.Lines.Add(newLine);

		return l;
	}
	public static LexerState Buffer(this LexerState l) {
		l.Line.Append(l.Current);
		return l;
	}
	public static LexerState BufferNew(this LexerState l, string format, params object[] args) {
		if (args.Length > 0) {
			l.Line.AppendFormat(format, args);
		} else {
			l.Line.Append(format);
		}
		return l;
	}
	public static LexerState BufferKeyword(this LexerState l, string keyword) {
		l.HasKeyword = true;
		l.Line.Append(keyword);
		return l;
	}
	public static LexerState BufferNew(this LexerState l, char c) {
		l.Line.Append(c);
		return l;
	}
	public static bool Step(this LexerState l) {
		if (l.next < l.Count) {
			l.Current = l.Code[l.next++];
			return true;
		} else {
			return false;
		}
	}
	public static bool PassCommentOrDiv(this LexerState l) {
		Debug.Assert(l.Current=='/',"ERROR: l.Currnet SHOULD BE '/' .");
		if (l.next < l.Count) {
			var nc = l.Code[l.next];
			if (nc == '/') {
				l.Save();
				int skipCount = l.Code.SkipSingleLineComment(ref l.next);
			} else if (nc == '*') {
				l.Save();
				int skipCount = l.Code.SkipMultilineComment(ref l.next);
			} else {
				l.Buffer();
			}
		}
		return true;
	}
	public static bool PassSpaceOrNewLine(this LexerState l) {
		Debug.Assert(l.Current.IsSpaceOrNewLine(), "ERROR: l.Currnet SHOULD BE '\r' or '\n' .");
		if (l.KeepNewLineOnce) {
			l.Save();
			l.KeepNewLineOnce = false;
			if (l.LastNonWhiteSpace == '\\'){
				l.KeepMacroLines = true;
			}
		}
		int skipCount = l.Code.SkipSpaceOrNewLine(ref l.next);
		return true;
	}
	public static bool PassFor(this LexerState l) {
		Debug.Assert(l.Current.IsSpaceOrNewLine(),"ERROR: prefix of for should be space or newline");
		var lookup = l.Peek(4);
		if (lookup.Length==4&&lookup.Substring(0, 3) == "for" && lookup[3].IsSpaceOrNewLine()) {
			int i = l.next+4;
			var compress = true;
			var r = l.Code.EatParentheses(ref i,compress);
			if (r != null) {
				var skipValue = r.Item2;
				var skipCount = r.Item1+4;
				
				l.SaveNew("for{0}",skipValue);
				l.next+=skipCount;
				return true;
			}
		}
		return false;
	}
	public static bool PassSemiColons(this LexerState l) {
		Debug.Assert(l.Current==';',"ERROR: l.Currnet SHOULD BE ';' .");
		l.Buffer();
		l.HasKeyword = false;
		l.KeepNewLineOnce = false;
		if (!l.KeepMacroLines) {
			l.Save();
			l.next += 1;
		} else {
			int skipCount = l.Code.SkipSpace(ref l.next);
			var lookup = l.Peek(1);
			if (!string.IsNullOrWhiteSpace(lookup)) {
				if (lookup[0] == '\\') {
					l.BufferNew('\\');
					l.next+=1;
				} 
				l.Save();
				l.next += 1;
			}
		}
		return true;
	}
	public static bool PassLeftBrace(this LexerState l) {
		Debug.Assert(l.Current == '{', "ERROR: l.Currnet SHOULD BE '{' .");

		l.EnterTailBlock = false;
		l.HasKeyword = false;
		l.KeepNewLineOnce = false;
		
		var lastLine = l.Line.ToString();
		if (lastLine != null) {
			if (lastLine.Contains("typedef")) {
				l.EnterTailBlock = true;
			}

			if (lastLine.Contains("class ") || lastLine.Contains("struct ")) {
				l.LastClassLevels.Push(l.level);
			}
		}

		l.Save();
		l.SaveNew("{");
		
		
		l.level++;
		return true;
	}
	public static bool PassRightBrace(this LexerState l) {
		Debug.Assert(l.Current == '}', "ERROR: l.Currnet SHOULD BE '}' .");
		l.Save();
		l.level--;
		l.HasKeyword = false;
		l.KeepNewLineOnce = false;
		if (l.LastClassLevels.Count > 0) {
			if (l.level == l.LastClassLevels.Peek()) {
				l.LastClassLevels.Pop();
			}
		}

		Action peekSemicolons = () => {
			int old = l.next;
			int skipCount = l.Code.SkipSpaceOrNewLine(ref l.next);
			var nc = l.PeekChar();
			if (nc == ';') {
				l.BufferNew(nc);
				l.next += 1;
			} else {
				l.next = old;
			}
		};

		l.BufferNew("}");

		if (!l.EnterTailBlock) {
			peekSemicolons();
			l.Save();
        	
		} else {
			int i = l.next;
			var lastspace = false;
			var skipCount = l.Code.Skip(ref i, cc => {
				var valid = cc != ';';
				if (valid) {
					if (cc.IsSpaceOrNewLine()) {
						if (!lastspace) {
							l.BufferNew(' ');
							lastspace = true;
						}
					} else {
						l.BufferNew(cc);
						lastspace = false;
					}
				}
				return valid;
			});
			
			l.next+=skipCount;
			peekSemicolons();
			
			l.Save();
			
		}
		
		return true;
	}
	public static bool PassKeyWords(this LexerState l,params string[] keywords) {
		Debug.Assert(keywords.Count() > 0, "ERROR: Keywords is empty.");
		var r = l.PeekMatch(keywords);
		if (r != null) {
			if (!l.HasKeyword) {
				l.Save();
			}
			l.BufferKeyword(r);
			l.next+=r.Length;
			l.KeepNewLineOnce = false;
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
			l.KeepNewLineOnce = true;
			return true;
		} else {
			return false;
		}
	}
	public static bool PassMacro(this LexerState l, params string[] keywords) {
		Debug.Assert(keywords.Count() > 0, "ERROR: Keywords is empty.");
		var r = l.PeekMatch(keywords);
		if (r != null) {
			l.Save();
			l.BufferNew(r);
			l.next += r.Length;
			l.KeepNewLineOnce = true;
			return true;
		} else {
			return false;
		}
	}
	public static bool PassAccessors(this LexerState l) {
		l.Code.SkipSpace(ref l.next);
		var keywords = new[] {"public:","protected:","private:"};
		var r = l.PeekMatch(keywords);
		if (r != null) {
			l.PushLevel(l.LastClassLevels.Peek());
			l.Save();
			l.SaveNew(r);
			l.PopLevel();
			l.next+=r.Length;
			l.KeepNewLineOnce = true;
			return true;
		} else {
			return false;
		}
	}
	public static bool PassChar(this LexerState l) {
		l.Buffer();
		l.next +=1;
		return true;
	}

	public static IEnumerable<string> Next(this LexerState l) {
		l.Prepare();
		var lastIsNewLine = false;
		while (l.Step()) {
			var keepNewLineOnce = l.KeepNewLineOnce;
			
			if (!l.Current.IsSpaceOrNewLine()) {
				l.LastNonWhiteSpace = l.Current;
			}
		
			foreach (var lt in l.Lines) {
				yield return lt;
			}
			l.Lines.Clear();
			
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
				if (!l.PassCommentOrDiv()) {
					Debug.Assert(false, "ERROR: PassCommentOrDiv Failed.");
					break;
				} else {
					lastIsNewLine = true;
					continue;
				}
			}

			if (l.Current.IsNewLine()) {
				lastIsNewLine = true;
				if (!l.PassSpaceOrNewLine()) {
					Debug.Assert(false, "ERROR: PassSpaceOrNewLine Failed.");
					break;
				} else {
					// Ignore
				}
			}

			if (l.Current == ';') {
				if (!l.PassSemiColons()) {
					Debug.Assert(false, "ERROR: PassSemiColons Failed.");
					break;
				} else {
					continue;
				}
			}

			if (l.Current.IsSpaceOrNewLine() || lastIsNewLine) {
				var old = l.Current;
				var oldIndex = l.next;
				if (lastIsNewLine&&!l.Current.IsSpaceOrNewLine()) {
					l.Current = ' ';
					l.next--;
				}
				lastIsNewLine = false;
				if (l.PassFor()) {
					continue;
				} else if (l.PassLineByHeader(
					"#if", "#ifdef", "#ifndef", "#define", "#endif",
					"#error", "#include", "#import","case ","default ","default:")) {
					continue;
				} else if (l.PassKeyWords(
					"typedef ", "template<", "template ",
					"class ","struct ","union ","enum ","friend class ")) {
					continue;
				} else if (l.PassAccessors()) {
					continue;
				} else if (old.IsSpaceOrNewLine()){
					l.BufferNew(' ');

					var lookup = l.Peek(1);
					if (lookup.Length>0&&lookup[0].IsSpaceOrNewLine()) {
						if (!l.PassSpaceOrNewLine()) {
							Debug.Assert(false, "ERROR: Final PassSpaceOrNewLine Failed.");
							break;
						} else {
							// Ignore
						}
					} else {

					}
				} else {
					l.Current = old;
					l.next = oldIndex;
				}
			}
			
			l.Buffer();
		}
		foreach (var lt in l.Lines) {
			yield return lt;
		}
		yield break;
    }
}

public static class EatExtension {
	public static Tuple<int, string> EatParentheses(this string str, ref int index, bool compress) {
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
				var valid = cc != '(' && cc != ')';
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

			if (i >= count) break;
			var c = str[i++];
			skipCount++;
			bool error = false;
			switch (c) {
				case '(':
					sb.Append(c); lastspace = false;
					stack.Push(c);
					skipNonParentheses();
					break;
				case ')':
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
					break;
				default:
					error = true;
					break;
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
	public static bool IsSpaceOrNewLine(this char c) {
		return Char.IsWhiteSpace(c)||c==' '||c=='\t'||c=='\r'||c=='\n';
	}
	public static bool IsNewLine(this char c) {
		return c == '\r' || c == '\n';
	}
	public static int SkipBlankLines(this string allText, ref int index) {
		int skipCount = 0;
		int inc = 0;
		int i = index;

		inc = allText.SkipBlankLine(ref i);
		while (inc > 0) {
			skipCount += inc;
			inc = allText.SkipBlankLine(ref i);
		}

		index += skipCount;
		return skipCount;
	}
	public static int SkipBlankLine(this string allText, ref int index) {
        int i = index;
        int count = allText.Length;
        bool blankline = false;

        int skipCount = allText.Skip(ref i, c => {
            return c == ' ' || c == '\t';
        });

        if (i < count) {
            char cc = allText[i++];
            if (cc == '\r' || cc == '\n') {
                skipCount++;
                skipCount += allText.Skip(ref i, c => {
                    return c == '\r' || c == '\n';
                });
                blankline = true;
            }
        }
        if (blankline) {
            index += skipCount;
            return skipCount;
        } else {
            return 0;
        }
    }
    public static int SkipSingleLineComment(this string allText, ref int index) {
        int i = index;
        int count = allText.Length;

        int skipCount = allText.Skip(ref i, c => {
            return c != '\r' && c != '\n';
        });

        skipCount += allText.Skip(ref i, c => {
            return c == '\r' || c == '\n';
        });

        skipCount += allText.SkipBlankLine(ref i);

        index += skipCount;
        return skipCount;
    }
    public static int SkipMultilineComment(this string allText, ref int index) {
        int skipCount = 0;
        int i = index;
        int count = allText.Length;

        while (true) {
            // skip non star
            skipCount += allText.SkipNonStar(ref i);
            if (i < count) {
                char c = allText[i++];
                Debug.Assert(c == '*');
                skipCount++;

                // skip continues start
                skipCount += allText.SkipStar(ref i);

                if (i < count) {
                    c = allText[i++];
                    skipCount++;

                    if (c == '/') {
                        // hint "*/"
                        break;
                    }
                } else {
                    break;
                }
            } else {
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
	public static int SkipNonStar(this string allText, ref int index) {
        return allText.Skip(ref index, c => {
            return c != '*';
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
        var next = p.Lexer.Next();
        var yieldable = false;
        foreach (var ll in next) {
            var line = ll.Replace("XPF_BEGIN_EXTERN_C","");
            line = line.Replace("XPF_END_EXTERN_C","");
            
            p.Current = line;
            p.LineNumber++;

            if (p.CurrentBlock != null && p.CurrentBlock.Close == false) {
                p.CurrentBlock.AddLine(line);
            }

            do {
                // Block Begin
                if (p.PassBeginBlock()) {
                    if (p.BlockStack.Count == 0) {
                        yieldable = true;
                    }
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
            } while (false);

            if (yieldable) {
                p.CurrentBlock.Update();
                yield return p.CurrentBlock;
                p.CurrentBlock = null;
                yieldable = false;
            }

            p.Last = line;
        }
    }
    private static bool PassBeginBlock(this ParserState p) {
        var isBlock = p.Current.Trim() == "{";
        var isInlineBlock = false;
        
        var last = p.Last;
        var current = p.Current;
        
        if (!isBlock) {
            if (p.CurrentBlock==null||(p.CurrentBlock.IsType()||p.CurrentBlock.IsNamespace())) {
                var l = p.Current.Trim();
                if (l.Contains("(")) {
                    isInlineBlock = l.EndsWith(";");
                    if (isInlineBlock) {
                        p.Last = p.Current;
                        p.Current = null;
                    }
                }
            }
        }
        if (isBlock||isInlineBlock) {

            // Create new block
            var b = new ControlBlock();
            b.LineStart = p.LineNumber - 1;
            b.AddLine(p.Last);
            b.AddLine(p.Current);


            // Stack Control Blocks
            if (p.BlockStack.Count > 0) {
                var top = p.BlockStack.Peek();
                b.Parent = top;
                b.Accessor = b.Parent.LastScopeAccessor;
                top.Children.Add(b);
            }

            p.CurrentBlock = b;
            p.BlockStack.Push(b);

            bool pass = true;
            do {
                // xxx_API(r) function(...){}
                if (p.PassAPI()) {
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

                // class c:...{}
                if (p.PassClass()) {
                    break;
                }

                // o::c(...){}
                // ~o::c(...){}
                if (p.PassCtorAndDtor()) {
                    break;
                }

                // r o::m(...){}
                if (p.PassMethod()) {
                    break;
                }

                // {}  
                pass = false;
            } while (false);

            // anonymous block
            if (!pass&&!isInlineBlock) {
                b.Codes.RemoveAt(0);
            }

            if (isInlineBlock) {
                p.CurrentBlock.IsDeclare = true;
                p.PassEndBlock(true);
            }
            p.Last = last;
            p.Current = current;

            return pass;
        } else {
            return false;
        }
    }
    private static bool PassEndBlock(this ParserState p,bool inline=false) {
        var isEnd = inline;
        
        if (!isEnd) {
            isEnd = p.Current.Contains("}");
        }
        
        if (isEnd) {
            p.CurrentBlock.LineEnd = p.LineNumber;
            p.CurrentBlock.Close = true;
            if ((p.CurrentBlock.Parent == null||p.CurrentBlock.Parent.IsNamespace())&&!p.CurrentBlock.Name.Contains("::")) {
                if (p.CurrentBlock.Kind == "method") {
                    p.CurrentBlock.Kind = "function";
                }
            }

            if (p.Current!=null&&string.IsNullOrWhiteSpace(p.CurrentBlock.Postfix)) {
                p.CurrentBlock.Postfix = p.Current.Trim().TrimStart('}');
            }
            
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
    private static bool PassAPI(this ParserState p) {
        var m = new StringBuilder();
        m
        .Home()
        .Space('*')
        
        .Char('+').Str("_API")       // API 
        .Space('*')
        
        .L('(')
        .Space('*')
        
        .B().Any('*').E()             // 0. return type
        .Space('*')
        
        .R(')')
        .Space('+')
        
        .B().Char('+').E()             // 1. function name
        .Space('*')
        
        .L('(')
        .Space('*')
        
        .B().Any(@"[^\)\(]",'*').E()   // 2. function arguments
        .Space('*')
        
        .R(')')
        .Space('*')
        ;

        var vs = p.Last.ValuesAt(m, 0, 1, 2);
        if (vs.Count() == 3) {
            p.CurrentBlock.Kind   = "api";
            p.CurrentBlock.Prefix = vs.ElementAt(0);
            p.CurrentBlock.Name   = vs.ElementAt(1);
            p.CurrentBlock.Fields = vs.ElementAt(2);
            return true;
        } else {
            return false;
        }
    }
    private static bool PassMethod(this ParserState p) {
        var m = new StringBuilder();

        // split
        m.Clear();
        m
        .BlanceGroup("(", ")", 1, 2);
        
        var vs = Regex.Match(p.Last,m.ToString()).Groups.Captures().ToList();
        if(vs.Count<3) return false;
        
        var pre    = vs[1];
        var middle = vs[2];
        var post   = vs[3];

        var first = pre.Value;
        var second = middle.Value.Replace("(","").Replace(")","");
        var third = post.Value;
        if (middle.Captures.Count ==2) {
            first = first+" "+middle.Captures[0].Value.Replace("(","").Replace(")","");
        }

        // split
        m.Clear();
        m
        .Home()
        .Space('*')

        .B().Words().E()                       // 0. return type
        .Space('+')

        .B().Str(@"\S+\s*(::\s*\S+\s*)*").E()   // 1.2 method name
        .Space('*');
        
        var sv = Regex.Match(first,m.ToString()).Groups.Captures().ToList();
        if(sv.Count<3) return false;
        
        p.CurrentBlock.Kind    = "method";
        p.CurrentBlock.Fields  = second;
        p.CurrentBlock.Postfix = third;
        p.CurrentBlock.Prefix = sv[1].Value;
        p.CurrentBlock.Name   = sv[2].Value;

        return true;
    }
    private static bool PassCtorAndDtor(this ParserState p) {
        var m = new StringBuilder();
        m
        .Home()
        .Space('*')
        
        .B().Str(@"~?\w+\s*(::\s*(\w|~)+\s*)*").E()  // 0.1.2 method name
        .Space('*')
        
        .L('(')
        .Space('*')
        
        .B().Any(@"[^\)\(]",'*').E()                // 3. method arguments
        .Space('*')
        
        .R(')')
        .Space('*')
        
        .B().Any('*').E()                           // 4. tail
        ;

        var vs = p.Last.ValuesAt(m, 0, 1,2, 3, 4);

        if (vs.Count() == 5) {
            var lookup = vs.ElementAt(0);
            if (p.CurrentBlock.Parent != null) {
                if (!lookup.Contains(p.CurrentBlock.Parent.Name)) {
                    return false;
                }
            }

            if (lookup.Contains("~")) {
                p.CurrentBlock.Kind     = "destructor";
            } else {
                p.CurrentBlock.Kind     = "constructor";
            }
            p.CurrentBlock.Name = vs.ElementAt(0);
            p.CurrentBlock.Fields = vs.ElementAt(3);
            p.CurrentBlock.Postfix = vs.ElementAt(4);
            
            return true;
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


        if (keyword != null) {

            p.CurrentBlock.Kind = "control";
            p.CurrentBlock.Name = keyword;
            
            if (keyword == "else" || keyword == "do") {
                return true;
            }

            var m = new StringBuilder();
            m
            .Words()           // if else if for do while
            .Space('*')

            .L('(')
            .Space('*')
            
            .B().Words().E()   // 0. conditions
            .Space('*')
            
            .R(')')
            .Space('*')
            ;

            var vs = p.Last.ValuesAt(m, 0);

            if (vs.Count()==1) {
                p.CurrentBlock.Fields = vs.ElementAt(0);
                return true;
            } else {
                // error
                return false;
            }
        } else {
            return false;
        }
    }

    private static bool PassClass(this ParserState p) {

        if (p.Last.Contains("tagXPFAddressCacheTypeInfo")) {
            int j=0;
        }
        
        var keywords = new[]{
            "union","structs","enums","namespace"
        };

        string keyword = null;
        foreach (var k in keywords) {
            if (p.Last.Trim()==k) {
                keyword = k;
                break;
            }
        }

        if (keyword!=null) {
            p.CurrentBlock.Kind  =keyword;
            return true;
        }

        var tempalte = new[]{
            "class","struct"
        };

        var pre = "";
        var last = p.Last;
        if (p.Last.Contains("template")) {
            var second = @"(^\s*template\s*<[^<>]*(((?'Open'<)[^<>]*)+((?'-Open'>)[^<>]*)+)*(?(Open)(?!))>\s*)";
            var r = Regex.Match(p.Last, second);
            if (r.Captures.Count > 0) {
                pre = r.Value;
                last=p.Last.Substring(pre.Length);
            }
        }
        
        var m = new StringBuilder();
        m
        .Home()
        .Space('*')
        
        .B().Str(@"typedef|\s*").E()                                   // 0. prefix
        .Space('*')
        
        .B().Str("class|struct|enum|namespace|union").E()   // 1. class,struct,enum 
        .Str(@"[\s\t\r\n]+")
        
        .B().Str(@"\S[^:]*").E()                  // 2. name
        .Space('*')
        
        .B().Str(@":?").Any('*').E()                   // 3. postfix: inherit     
        .Space('*')
        ;

        var vs = last.ValuesAt(m, 0, 1, 2,3);

        if (last.Contains("namespace")) {
            int i=0;
        }

        if (vs.Count() == 4) {
            if (vs.ElementAt(0).Contains("template")) {
                "debug".Dump();
            }
            p.CurrentBlock.Kind     = vs.ElementAt(1);
            p.CurrentBlock.Prefix   = pre+vs.ElementAt(0);
            p.CurrentBlock.Name     = vs.ElementAt(2);
            p.CurrentBlock.Postfix  = vs.ElementAt(3);

            if (p.CurrentBlock.Name.Contains("ServiceRandomRule")) {
                int z=0;
            }
            
            return true;
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
    public static string ValueAt(this string src, StringBuilder pattern, int i) {
        return src.ValueAt(pattern.ToString(), i);
    }
    public static string ValueAt(this string src, string pattern, int i) {
        Match m = null;
		try {
			m = Regex.Match(src, pattern);
		} catch (Exception e) {
			var sb = string.Format("{0}/{1}", src, pattern);
			string.Format("ValueAt Error:{0},\r\nsrc:{1}\r\npattern:{2}", e.Message, src, pattern).Dump("error");
		}
		if (m.Captures.Count > 0) {
			if (m.Groups.Count > i && i > 0) {
                return m.Groups[i].Value;
            }
        } else {
            int j = 0;
        }
        return null;
    }
    public static IEnumerable<string> ValuesAt(this string src, StringBuilder pattern, params int[] ia) {
        return src.ValuesAt(pattern.ToString(), ia);
    }
    public static IEnumerable<string> ValuesAt(this string src, string pattern, params int[] ia) {
        Match m = null;
        try {
            m = Regex.Match(src, pattern);
        } catch(Exception e){
            var sb = string.Format("{0}/{1}", src, pattern);
            string.Format("ValuesAt Error:{0},\r\nsrc:{1}\r\npattern:{2}",e.Message,src,pattern).Dump("error");
        }
        if (m.Captures.Count > 0) {
            foreach (var i in ia) {
                if (m.Groups.Count > i && i >= 0) {
                    yield return m.Groups[i + 1].Value;
                }
            }
        }
        yield break;
    }
    public static StringBuilder Words(this StringBuilder p) {
        p.Append(@"\S.*\S");
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
    public static StringBuilder Any(this StringBuilder p, string w, char c) {
        p.AppendFormat(@"{0}{1}", w, c);
        return p;
    }
    public static StringBuilder Char(this StringBuilder p) {
        p.Append(@"\w");
        return p;
    }
    public static StringBuilder Char(this StringBuilder p, char c) {
        p.AppendFormat(@"\w{0}", c);
        return p;
    }
    public static StringBuilder Char(this StringBuilder p, char w, char c) {
        p.AppendFormat(@"{0}{1}", w, c);
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
            case '}':
            case ']':
            case ')':
                Debug.Assert(false); break;
            case '(': p.Append(@"\("); break;
            default: p.Append(c); break;
        }
        return p;
    }
    public static StringBuilder R(this StringBuilder p, char c) {
        switch (c) {
            case '{':
            case '[':
            case '(':
                Debug.Assert(false); break;
            case ')': p.Append(@"\)"); break;
            default: p.Append(c); break;
        }
        return p;
    }
    public static StringBuilder Str(this StringBuilder p, string str) {
        p.Append(str);
        return p;
    }
    public static StringBuilder Or(this StringBuilder p) {
        p.Append("|");
        return p;
    }
    public static StringBuilder Home(this StringBuilder p) {
        p.Append("^");
        return p;
    }
    public static StringBuilder End(this StringBuilder p) {
        p.Append("$");
        return p;
    }
    public static StringBuilder BlanceGroup(this StringBuilder p, string l, string r, int min, int max) {
        if (l == "(") l = @"\(";
        if (r == ")") r = @"\)";
        var first = @"([^<>]*)";
        var second = @"(<[^<>]*(((?'Open'<)[^<>]*)+((?'-Open'>)[^<>]*)+)*(?(Open)(?!))>\s*)";
        var third = @"([^<>]*)";
        var pp = string.Format(@"{0}\s*{1}{{{2},{3}}}{4}", first, second, min, max, third);
        p.Append(pp.Replace("<", l).Replace(">", r));
        return p;
    }
    public static IEnumerable<System.Text.RegularExpressions.Group> Captures(this System.Text.RegularExpressions.GroupCollection groups) {
        for (int i = 0; i < groups.Count; i++) {
            System.Text.RegularExpressions.Group g = groups[i];
            if (g.Success) yield return g;
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
                        case "public:": cb.PublicBlocks.Add(b); return;
                        case "protected:": cb.ProtectedBlocks.Add(b); return;
                        case "private:": cb.PrivateBlocks.Add(b); return;
                        default: b.Accessor = "public:"; cb.PublicBlocks.Add(b); return;
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

// Define other methods and classes here
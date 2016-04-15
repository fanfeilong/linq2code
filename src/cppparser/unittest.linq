<Query Kind="Program" />

void Main() {
    //u.TestSkipMultilineCommment();
    //u.TestSkipSingleLineComment();
	//u.TestSkipBlankLines();
	u.TestEatParentheses();
}

// Define other methods and classes here
public static class u {
    private static void split() {
        "-------------".Dump();
    }
    public static void TestSkipMultilineCommment() {
        split();
        
        var continueComment = "////\r\n  **/";
        var str = string.Format("{0} /**/ss",continueComment);
        
        int index = 0;
        int skipCount = str.SkipMultilineComment(ref index);
        Debug.Assert(skipCount == continueComment.Length,"skipcount should equals to continue skip length");
        Debug.Assert(index == continueComment.Length,"pos should equals to continue skip length");
        
        str.Length.Dump("total length");
        continueComment.Length.Dump("continue comment length");
        index.Dump("pos");
        skipCount.Dump("skip count");
        str.Substring(0,index).Dump("skip");
        str.Substring(index).Dump("rest");
    }
    public static void TestSkipSingleLineComment() {
        split();
        var continueComment = "adfjaskfjlakfjdlasf j\r\n\r\n\r\n";
        var str = string.Format("{0}  adfa",continueComment);

        int index = 0;
        int skipCount = str.SkipSingleLineComment(ref index);
        Debug.Assert(skipCount == continueComment.Length, "skipcount should equals to continue skip length");
        Debug.Assert(index == continueComment.Length, "pos should equals to continue skip length");

        str.Length.Dump("total length");
        continueComment.Length.Dump("continue comment length");
        index.Dump("pos");
        skipCount.Dump("skip count");
        str.Substring(0, index).Dump("skip");
        str.Substring(index).Dump("rest");
	}
	public static void TestSkipBlankLines() {
		split();
		var shouldSkip = "\r\n\r\n  \r\n\r\n  \r\n  \r   \r\n";
		var str = string.Format("{0}  adfa\r\n", shouldSkip);

		int index = 0;
		int skipCount = str.SkipBlankLines(ref index);
		Debug.Assert(skipCount == shouldSkip.Length, "skipcount should equals to continue skip length");
		Debug.Assert(index == shouldSkip.Length, "pos should equals to continue skip length");

		str.Length.Dump("total length");
		shouldSkip.Length.Dump("continue comment length");
		index.Dump("pos");
		skipCount.Dump("skip count");
		str.Substring(0, index).Dump("skip");
		str.Substring(index).Dump("rest");
	}
	public static void TestEatParentheses() {
		split();
		var shouldSkip = "  \r\n(\r\nadjflakf       \r\n     jlajk(asfd  \r\naf\r\n(adfafa)a))";
		var str = string.Format("{0}  adfa\r\n", shouldSkip);

		int index = 0;
		var r = str.EatParentheses(ref index,false);
		if (r != null) {
			var skipCount = r.Item1;
			var skipValue = r.Item2;
			Debug.Assert(skipCount== shouldSkip.Length, "skipcount should equals to continue skip length");
			Debug.Assert(index == shouldSkip.Length, "pos should equals to continue skip length");
				
			skipValue.Dump("SkipValue");
			str.Length.Dump("total length");
			shouldSkip.Length.Dump("continue comment length");
			index.Dump("pos");
			skipCount.Dump("skip count");
			str.Substring(0, index).Dump("skip");
			str.Substring(index).Dump("rest");
		}
	}
}

public static class EatExtension {
	public static Tuple<int, string> EatParentheses(this string str, ref int index, bool keepspaceornewline) {
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
		return c == ' ' || c == '\t' || c == '\r' || c == '\n';
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
			var skips = new char[] { ' ', '\t' };
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
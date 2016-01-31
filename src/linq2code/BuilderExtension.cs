using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Linq.Code {
    public static class BuilderExtension {
        private static int indent=0;
        private static int lastIndent=0;
        private static int tabSize = 4;

        private static void ResetIndent() {
            if (lastIndent>0) {
                indent=lastIndent;
                lastIndent=0;
            }
        }

        public static StringBuilder ConfigTabSize(this StringBuilder sb,int count){
            tabSize = count;
            return sb;
        }

        public static StringBuilder Head(this StringBuilder sb, string text) {
            lastIndent=indent;
            indent=0;
            var space=new string(' ', lastIndent*tabSize);
            sb.AppendFormat("{0}{1}", space, text);
            return sb;
        }

        public static StringBuilder Push(this StringBuilder sb) {
            if (lastIndent>0) {
                indent=++lastIndent;
                lastIndent=0;
            } else {
                indent++;
            }
            return sb;
        }
        public static StringBuilder Pop(this StringBuilder sb) {
            if (lastIndent>0) {
                indent=--lastIndent;
                lastIndent=0;
                Debug.Assert(indent>=0);
            } else {
                indent--;
                Debug.Assert(indent>=0);
            }


            return sb;
        }
        public static StringBuilder Line(this StringBuilder sb, string text, bool ignoreIndent=false) {
            if (ignoreIndent) {
                sb.AppendLine(text);
            } else {
                var space=new string(' ', indent*tabSize);
                sb.AppendFormat("{0}{1}\n", space, text);
            }
            ResetIndent();
            return sb;
        }

        public static StringBuilder Line(this StringBuilder sb) {
            sb.AppendLine();
            ResetIndent();
            return sb;
        }
        public static StringBuilder FormatLine(this StringBuilder sb, string format, params object[] args) {
            var space=new string(' ', indent*tabSize);
            var text=string.Format(format, args);
            sb.AppendFormat("{0}{1}\n", space, text);
            ResetIndent();
            return sb;
        }
        public static StringBuilder PushLine(this StringBuilder sb, string text) {
            return sb.Line(text).Push();
        }
        public static StringBuilder PopLine(this StringBuilder sb, string text) {
            return sb.Pop().Line(text);
        }
        public static StringBuilder PopLine(this StringBuilder sb) {
            return sb.Pop().Line();
        }

        public class EachBuilder<T> {
            public IEnumerable<T> List;
            public StringBuilder Builder;
            public List<Action<T>> Actions;
            public EachBuilder() {
                Actions=new List<Action<T>>();
            }
        }
        public static EachBuilder<T> BeginEach<T>(this StringBuilder sb, IEnumerable<T> list) {
            return new EachBuilder<T>() {
                List=list,
                Builder=sb
            };
        }
        public static EachBuilder<T> Push<T>(this EachBuilder<T> sbe) {
            var action=new Action<T>(t => sbe.Builder.Push());
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> Line<T>(this EachBuilder<T> sbe, string text, bool ignoreIndent=false) {
            var action=new Action<T>(t => sbe.Builder.Line(text, ignoreIndent));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> Line<T>(this EachBuilder<T> sbe) {
            var action=new Action<T>(t => sbe.Builder.Line());
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> PushLine<T>(this EachBuilder<T> sbe, string text) {
            var action=new Action<T>(t => sbe.Builder.PushLine(text));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> PopLine<T>(this EachBuilder<T> sbe, string text) {
            var action=new Action<T>(t => sbe.Builder.PopLine(text));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> PopLine<T>(this EachBuilder<T> sbe) {
            var action=new Action<T>(t => sbe.Builder.PopLine());
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> Pop<T>(this EachBuilder<T> sbe) {
            var action=new Action<T>(t => sbe.Builder.Pop());
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> FormatLine<T>(this EachBuilder<T> sbe, string format, Func<T, string> args) {
            var action=new Action<T>(t => sbe.Builder.FormatLine(format, args(t)));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> FormatLine<T>(this EachBuilder<T> sbe, string format, Func<T, string> args1, Func<T, string> args2) {
            var action=new Action<T>(t => sbe.Builder.FormatLine(format, args1(t), args2(t)));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static EachBuilder<T> FormatLine<T>(this EachBuilder<T> sbe, string format, Func<T, string> args1, Func<T, string> args2, Func<T, string> args3) {
            var action=new Action<T>(t => sbe.Builder.FormatLine(format, args1(t), args2(t), args3(t)));
            sbe.Actions.Add(action);
            return sbe;
        }
        public static StringBuilder EndEach<T>(this EachBuilder<T> sbe) {
            foreach (var item in sbe.List) {
                foreach (var action in sbe.Actions) {
                    action(item);
                }
            }
            return sbe.Builder;
        }
    }
}

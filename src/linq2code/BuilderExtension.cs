using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Code;
using System.Text;

public static class BuilderExtension {
    private static int indent=0;
    private static int tabSize=4;

    public static Builder<P, PP, PPP> ConfigTab<P, PP, PPP>(this Builder<P, PP, PPP> b, int size) where PPP : class {
        tabSize=size;
        return b;
    }

    public static Builder<P, PP, PPP> Push<P, PP, PPP>(this Builder<P, PP, PPP> b) where PPP : class {
        Debug.Assert(indent>=0);
        indent++;
        return b;
    }
    public static Builder<P, PP, PPP> Pop<P, PP, PPP>(this Builder<P, PP, PPP> b) where PPP : class {
        indent--;
        Debug.Assert(indent>=0);
        return b;
    }

    public static Builder<P, PP, PPP> TopLine<P, PP, PPP>(this Builder<P, PP, PPP> b, string format, params object[] objs) where PPP : class {
        int currentIndent=indent;
        indent=0;
        var r=Line<int, P, PP, PPP>(b, format, null, objs);
        indent=currentIndent;
        return r;
    }
    public static Builder<P, PP, PPP> PushLine<P, PP, PPP>(this Builder<P, PP, PPP> b, string format) where PPP : class {
        return Line(b, format).Push();
    }
    public static Builder<P, PP, PPP> PopLine<P, PP, PPP>(this Builder<P, PP, PPP> b, string format, params object[] objs) where PPP : class {
        return b.Pop().Line(format, objs);
    }

    public static Builder<P, PP, PPP> Line<P, PP, PPP>(this Builder<P, PP, PPP> b) where PPP : class {
        var lb=new LineBuilder<int, P, Builder<P, PP, PPP>>(b, null);
        lb.AddConcat((sb, v) => {
            sb.AppendLine();
        });
        return lb.End();
    }

    public static Builder<P, PP, PPP> Line<P, PP, PPP>(this Builder<P, PP, PPP> b, string format,
        params object[] objs)
        where PPP : class {

        var lb=new LineBuilder<int, P, Builder<P, PP, PPP>>(b, null);
        var currentIndent=indent;
        

        var c=new Action<StringBuilder, int>((sb, p) => {
            var space=new string(' ', currentIndent*tabSize);
            var currentFormat=format;
            if (objs.Length>0) {
                sb.Append(space).AppendFormat(currentFormat, objs).AppendLine();
            } else {
                sb.Append(space).Append(currentFormat).AppendLine();
            }
        });

        lb.AddConcat(c);
        return lb.End();
    }

    public static Builder<P, PP, PPP> Line<T, P, PP, PPP>(this Builder<P, PP, PPP> b, string format,
        Func<P, T> selector,
        params object[] objs)
        where PPP : class {

        var lb=new LineBuilder<T, P, Builder<P, PP, PPP>>(b, selector);
        var currentIndent=indent;
        

        lb.AddConcat((sb, v) => {
            var space=new string(' ', currentIndent*tabSize);
            var currentFormat=format;
            currentFormat=currentFormat.Replace("{0}", v.ToString());
            for (int i=0; i<objs.Length; i++) {
                currentFormat=currentFormat.Replace(string.Format("{{{0}}}", i+1), string.Format("{{{0}}}", i));
            }

            if (objs.Length>0) {
                sb.Append(space).AppendFormat(currentFormat, objs).AppendLine();
            } else {
                sb.Append(space).Append(currentFormat).AppendLine();
            }
        });
        return lb.End();
    }

    public static Builder<P, PP, PPP> Line<T, T2, P, PP, PPP>(this Builder<P, PP, PPP> b, string format,
        Func<P, T> selector1,
        Func<P, T2> selector2,
        params object[] objs)
        where PPP : class {

        Func<P, P> passtor=p => p;

        //
        var lb=new LineBuilder<P, P, Builder<P, PP, PPP>>(b, passtor);
        var currentIndent=indent;
        

        lb.AddConcat((sb, p) => {
            var space=new string(' ', currentIndent*tabSize);

            var currentFormat=format;
            currentFormat=currentFormat.Replace("{0}", selector1(p).ToString());
            currentFormat=currentFormat.Replace("{1}", selector2(p).ToString());

            for (int i=1; i<objs.Length; i++) {
                currentFormat=currentFormat.Replace(string.Format("{{{0}}}", i+1), string.Format("{{{0}}}", i-1));
            }

            if (objs.Length>0) {
                sb.Append(space).AppendFormat(currentFormat, objs).AppendLine();
            } else {
                sb.Append(space).Append(currentFormat).AppendLine();
            }
        });
        return lb.End();
    }

    public static Builder<P, PP, PPP> Line<T, T2, P, PP, PPP>(this Builder<P, PP, PPP> b, string format,
        Func<P, T> selector1,
        Func<P, T2> selector2,
        Func<P, T2> selector3,
        params object[] objs)
        where PPP : class {

        Func<P, P> passtor=p => p;
        

        //
        var lb=new LineBuilder<P, P, Builder<P, PP, PPP>>(b, passtor);
        var currentIndent=indent;
        lb.AddConcat((sb, p) => {
            var space=new string(' ', currentIndent*tabSize);
            var currentFormat=format;
            currentFormat=currentFormat.Replace("{0}", selector1(p).ToString());
            currentFormat=currentFormat.Replace("{1}", selector2(p).ToString());
            currentFormat=currentFormat.Replace("{2}", selector3(p).ToString());

            for (int i=2; i<objs.Length; i++) {
                currentFormat=currentFormat.Replace(string.Format("{{{0}}}", i+1), string.Format("{{{0}}}", i-2));
            }

            if (objs.Length>0) {
                sb.Append(space).AppendFormat(currentFormat, objs).AppendLine();
            } else {
                sb.Append(space).Append(currentFormat).AppendLine();
            }
        });
        return lb.End();
    }


    public static EachBuilder<T, P, Builder<P, PP, PPP>> Each<T, P, PP, PPP>(this Builder<P, PP, PPP> builder, IEnumerable<T> list) where PPP : class {
        return new EachBuilder<T, P, Builder<P, PP, PPP>>(builder, list);
    }
    public static SwitchBuilder<T, P, Builder<P, PP, PPP>> Switch<T, P, PP, PPP>(this Builder<P, PP, PPP> builder, Func<P, T> selector) where PPP : class {
        return new SwitchBuilder<T, P, Builder<P, PP, PPP>>(builder, selector);
    }
    public static SwitchBuilder<P, PP, PPP> Case<P, PP, PPP>(this Builder<P, PP, PPP> b, P v) where PPP : class {
        var swb=b as SwitchBuilder<P, PP, PPP>;
        Debug.Assert(swb!=null);
        swb.Case(v);
        return swb;
    }
    public static SwitchBuilder<P, PP, PPP> Default<P, PP, PPP>(this Builder<P, PP, PPP> b) where PPP : class {
        var swb=b as SwitchBuilder<P, PP, PPP>;
        Debug.Assert(swb!=null);
        swb.Default();
        return swb;
    }
    public static IfBuilder<T, P, Builder<P, PP, PPP>> If<T, P, PP, PPP>(this Builder<P, PP, PPP> builder, Func<P, T> selector, T v) where PPP : class {
        return new IfBuilder<T, P, Builder<P, PP, PPP>>(builder, selector, v);
    }
    public static IfBuilder<P, PP, PPP> Else<P, PP, PPP>(this Builder<P, PP, PPP> b) where PPP : class {
        var ifb=b as IfBuilder<P, PP, PPP>;
        Debug.Assert(ifb!=null);
        ifb.Else();
        return ifb;
    }

    public static PP End<T, P, PP>(this Builder<T, P, PP> b) where PP : class {
        var lb=b as LineBuilder<T, P, PP>;
        if (lb!=null) {
            return lb.End();
        }

        var eb=b as EachBuilder<T, P, PP>;
        if (eb!=null) {
            return eb.End();
        }

        var swb=b as SwitchBuilder<T, P, PP>;
        if (swb!=null) {
            return swb.End();
        }

        var ifb=b as IfBuilder<T, P, PP>;
        if (ifb!=null) {
            return ifb.End();
        }

        Debug.Assert(false);
        return null;
    }
}

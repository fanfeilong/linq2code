using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Linq.Code {
    public interface IBuilder<T> {
        void AddConcat(Action<StringBuilder, T> concat);
        void AddSubConcat(Action<StringBuilder, T> concat);
    }

    public interface IPrivoisBuilder<P> {
        IBuilder<P> GetPrivousBuilder();
    }

    public abstract class Builder<T, P, PP> : IBuilder<T>, IPrivoisBuilder<P> where PP : class {
        protected List<Action<StringBuilder, T>> Concats {
            get;
            set;
        }
        protected IBuilder<P> Privous;

        public Builder(IBuilder<P> privous) {
            Concats=new List<System.Action<System.Text.StringBuilder, T>>();
            Privous=privous;
        }

        public virtual void AddConcat(Action<StringBuilder, T> concat) {
            Concats.Add(concat);
        }
        public virtual void AddSubConcat(Action<StringBuilder, T> concat) {
            AddConcat(concat);
        }
        public virtual IBuilder<P> GetPrivousBuilder() {
            return Privous;
        }
    }

    public class TextBuilder : Builder<string, string, string> {
        public TextBuilder()
            : base(null) {
            //
        }
        public override string ToString() {
            var sb=new StringBuilder();
            foreach (var c in Concats) {
                c(sb, "");
            }
            return sb.ToString();
        }
    }

    public class EachBuilder<T, P, PP> : Builder<T, P, PP> where PP : class {
        public IEnumerable<T> List {
            get;
            private set;
        }

        public PP End() {
            Privous.AddSubConcat((sb, p) => {
                foreach (var item in List) {
                    foreach (var concat in Concats) {
                        concat(sb, item);
                    }
                }
            });
            return Privous as PP;
        }

        public EachBuilder(IBuilder<P> privous, IEnumerable<T> list)
            : base(privous) {
            List=list;
        }
    }

    public class SwitchBuilder<T, P, PP> : Builder<T, P, PP> where PP : class {
        private Func<P, T> Selector;

        private Dictionary<T, List<Action<StringBuilder, T>>> Cases {
            get;
            set;
        }

        bool isDefault=false;
        private List<Action<StringBuilder, T>> Defaults;

        private T CurrentCase {
            get;
            set;
        }
        public SwitchBuilder<T, P, PP> Case(T v) {
            isDefault=false;
            CurrentCase=v;
            Debug.Assert(!Cases.ContainsKey(v));
            Cases.Add(v, new List<Action<StringBuilder, T>>());
            return this;
        }
        public SwitchBuilder<T, P, PP> Default() {
            isDefault=true;
            return this;
        }
        public PP End() {
            var keys=new List<T>();
            Privous.AddSubConcat((sb, p) => {
                foreach (var pair in Cases) {
                    foreach (var concat in pair.Value) {
                        if (Selector(p).Equals(pair.Key)) {
                            keys.Add(pair.Key);
                            concat(sb, pair.Key);
                        }
                    }
                }
            });
            

            if (Defaults.Any()) {
                Privous.AddSubConcat((sb, p) => {
                    if (keys.Count==Cases.Sum(pair => pair.Value.Count())) {
                        foreach (var concat in Defaults) {
                            concat(sb, default(T));
                        }
                    }
                });
            }
            return Privous as PP;
        }

        public override void AddConcat(Action<StringBuilder, T> concat) {
            if (isDefault) {
                Defaults.Add(concat);
            } else {
                Cases[CurrentCase].Add(concat);
            }
        }

        public SwitchBuilder(IBuilder<P> privous, Func<P, T> selector)
            : base(privous) {
            Selector=selector;
            Cases=new Dictionary<T, List<Action<StringBuilder, T>>>();
            Defaults=new List<System.Action<System.Text.StringBuilder, T>>();
        }
    }

    public class LineBuilder<T, P, PP> : Builder<T, P, PP> where PP : class {
        private Func<P, T> Selector;
        public PP End() {
            Privous.AddSubConcat((sb, p) => {
                foreach (var concat in Concats) {
                    if (Selector!=null) {
                        concat(sb, Selector(p));
                    } else {
                        concat(sb, default(T));
                    }
                }
            });
            
            return Privous as PP;
        }
        public LineBuilder(IBuilder<P> privous, Func<P, T> selector)
            : base(privous) {
            Selector=selector;
        }
    }

    public class IfBuilder<T, P, PP> : Builder<T, P, PP> where PP : class {
        private Func<P, T> Selector;
        private bool isElse;
        private List<Action<StringBuilder, T>> Ifs;
        private List<Action<StringBuilder, T>> Elses;
        private T Value;

        public IfBuilder<T, P, PP> Else() {
            isElse=true;
            return this;
        }
        public override void AddConcat(Action<StringBuilder, T> concat) {
            if (isElse) {
                Elses.Add(concat);
            } else {
                Ifs.Add(concat);
            }
        }
        public PP End() {
            Privous.AddSubConcat((sb, p) => {
                var v=Selector(p);
                if (v.Equals(Value)) {
                    foreach (var concat in Ifs) {
                        concat(sb, v);
                    }
                } else {
                    foreach (var concat in Elses) {
                        concat(sb, v);
                    }
                }
            });
            return Privous as PP;
        }
        public IfBuilder(IBuilder<P> privous, Func<P, T> selector, T value)
            : base(privous) {
            Value=value;
            Selector=selector;
            Ifs=new List<System.Action<System.Text.StringBuilder, T>>();
            Elses=new List<System.Action<System.Text.StringBuilder, T>>();
        }
    }
}

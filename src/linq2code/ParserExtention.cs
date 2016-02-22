using System.Collections.Generic;
using System.Text;

namespace System.Linq.Code {
    public static class ParserExtention {
        // see: https://github.com/kklingenberg/parsec/blob/master/parsec/parse.py

        // Returns a parser that attempts the parser **p** at least zero
        // times.It consumes as much as possible, and returns a list of
        // results.The resulting parser cannot fail with a ParseError.
        public static Func<string, Tuple<List<T>, string>> Many<T>(Func<string, Tuple<T, string>> p) {
            var parser=new Func<string, Tuple<List<T>, string>>(str => {
                var result=new List<T>();
                while (true) {
                    try {
                        var r=p(str);
                        result.Add(r.Item1);
                        str=r.Item2;
                    } catch {
                        return Tuple.Create(result, str);
                    }
                }
            });
            return parser;
        }

        // Returns a parser that attempts the parser **p** at least one
        // time.It consumes as much as possible, and returns a list of
        // results.
        public static Func<string, Tuple<List<T>, string>> ManyOne<T>(Func<string, Tuple<T, string>> p) {
            var parser=new Func<string, Tuple<List<T>, string>>(str => {
                var rfirst=p(str);
                var rrest=Many(p)(rfirst.Item2);

                var rstr=rrest.Item2;
                var rlist=rrest.Item1;
                rlist.Insert(0, rfirst.Item1);

                return Tuple.Create(rlist, rstr);
            });
            return parser;
        }

        // Makes a parser that sequences others, accumulating the results in a list.
        public static Func<string, Tuple<List<T>, string>> Sequence<T>(IEnumerable<Func<string, Tuple<T, string>>> ps) {
            var parser=new Func<string, Tuple<List<T>, string>>(str => {
                var list=new List<T>();
                foreach (var p in ps) {
                    var r=p(str);
                    str=r.Item2;
                    list.Add(r.Item1);
                }
                return Tuple.Create(list, str);
            });
            return parser;
        }

        // Sequences parsers that return strings and concatenates the result.
        public static Func<string, Tuple<R, string>> Concat<T, R>(
            IEnumerable<Func<string, Tuple<T, string>>> ps,
            Func<IEnumerable<T>, R> c) {
            var parser=new Func<string, Tuple<R, string>>(str => {
                var r=Sequence(ps)(str);
                if (r.Item1.Equals(default(List<T>))) {
                    return Tuple.Create(default(R), r.Item2);
                } else {
                    return Tuple.Create(c(r.Item1.Where(i => !i.Equals(default(T)))), r.Item2);
                }
            });
            return parser;
        }

        // Maps a function to the result of parser *p*. Returns a parser.
        public static Func<string, Tuple<R, string>> Map<T, R>(Func<string, Tuple<T, string>> p, Func<T, R> map) {
            var parser=new Func<string, Tuple<R, string>>(str => {
                var r=p(str);
                var v=map(r.Item1);
                return Tuple.Create(v, r.Item2);
            });
            return parser;
        }

        // Return
        public static Func<string, Tuple<T, string>> Return<T>(T value) {
            var parser=new Func<string, Tuple<T, string>>(str => {
                return Tuple.Create(value, str);
            });
            return parser;
        }

        // Or
        public static Func<string, Tuple<T, string>> Or<T>(IEnumerable<Func<string, Tuple<T, string>>> ps) {
            var parser=new Func<string, Tuple<T, string>>(str => {
                var message=new StringBuilder();
                foreach (var p in ps) {
                    try {
                        var r=p(str);
                        return Tuple.Create(r.Item1, r.Item2);
                    } catch (Exception e) {
                        message.AppendLine(e.Message);
                    }
                }
                throw new Exception(message.ToString());
            });
            return parser;
        }

        // Try
        public static Func<string, Tuple<T, string>> Try<T>(Func<string, Tuple<T, string>> p) {
            var parser=new Func<string, Tuple<T, string>>(str => {
                try {
                    var r=p(str);
                    return Tuple.Create(r.Item1, r.Item2);
                } catch {
                    return Tuple.Create(default(T), str);
                }
            });
            return parser;
        }

        // Drop Value
        // Returns a parser that acts like *p* but that returns ``None``
        // if it succeeds."
        public static Func<string, Tuple<T, string>> DropValue<T, R>(Func<string, Tuple<T, string>> p) {
            return Map(p, t => default(T));
        }

        // Returns a parser that acts like it's argument but that fails
        // with the error message *msg*, including also the first few
        // characters in the input.
        public static Func<string, Tuple<T, string>> ErrorMessage<T>(Func<string, Tuple<T, string>> p, string message) {
            var parser=new Func<string, Tuple<T, string>>(str => {
                try {
                    return p(str);
                } catch (Exception e) {
                    var sb=new StringBuilder();
                    sb.Append(e.Message);
                    sb.Append(message);
                    throw new Exception(sb.ToString());
                }
            });
            return parser;
        }

        // Matches any character except those in *letters*.
        public static Func<string, Tuple<char, string>> Except(char[] letters) {
            var parser=new Func<string, Tuple<char, string>>(str => {
                if (string.IsNullOrEmpty(str)) {
                    throw new Exception("Empty string.");
                } else {
                    var c=str[0];
                    if (letters.Contains(c)) {
                        throw new Exception(string.Format("Input matches one of {0}", c));
                    } else {
                        return Tuple.Create(c, str.Substring(1));
                    }
                }
            });
            return parser;
        }
    }
}

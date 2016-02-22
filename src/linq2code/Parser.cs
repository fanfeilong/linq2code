using System.Text;

namespace System.Linq.Code{
    public static class Parser {
        public static int FirstAsc(this string s) {
            if (string.IsNullOrEmpty(s)) {
                throw new Exception("Character is not valid.");
            }
            var asciiEncoding=new ASCIIEncoding();
            var intAsciiCode=(int)asciiEncoding.GetBytes(s.Substring(0, 1))[0];
            return (intAsciiCode);
        }
        public static Tuple<int, string> Digit(string str) {
            if (string.IsNullOrEmpty(str)) {
                throw new Exception("Emptry string.");
            }

            var d=str.FirstAsc();
            if (d<=57&&d>=48) {
                return Tuple.Create(d, str.Substring(1));
            } else {
                throw new Exception("Not a digit.");
            }
        }
        public static Tuple<char, string> Letter(string str) {
            if (string.IsNullOrEmpty(str)) {
                throw new Exception("Emptry string.");
            }

            var d=str.FirstAsc();
            if ((d<=90&&d>=65)||(d<=122&&d>=97)) {
                return Tuple.Create(str[0], str.Substring(1));
            } else {
                throw new Exception("Not a digit.");
            }
        }
        public static Tuple<char, string> Space(string str) {
            if (string.IsNullOrEmpty(str)) {
                throw new Exception("Emptry string.");
            }

            var d=str[0];
            if (d==' '||d=='\t'||d=='\n') {
                return Tuple.Create(d, str.Substring(1));
            } else {
                throw new Exception("Not a digit.");
            }
        }
        public static Tuple<string, string> End(string str) {
            if (!string.IsNullOrEmpty(str)) {
                throw new Exception("Not the end of input.");
            }
            return Tuple.Create<string, string>(null, "");
        }
    }
}
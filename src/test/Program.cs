using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Code;

namespace test {
    class Program {
        static void Main(string[] args){

            // Prepare meta data
            const string className = "Example";
            var members = new Dictionary<string,string>{
                {"id","int"},
                {"name","string"}
            };

            // Generate CPP class
            var sb = new StringBuilder();
            sb.ConfigTabSize(4)
              .FormatLine("class {0} ", className)
              .PushLine("{")
                .Line("private:",true)
                .BeginEach(members)
                    .FormatLine("{1} m_{0};", m => m.Key, m => m.Value)
                .EndEach()
                .Line()

                .Line("public:", true)
                .FormatLine("{0}()", className)
                .PushLine("{")
                .Line("// TODO")
                .PopLine("}")
                .Line()

                .FormatLine("~{0}()", className)
                .PushLine("{")
                .Line("// TODO")
                .PopLine("}")
                .Line()

                .Line("public:", true)
                .BeginEach(members)
                    .FormatLine("const {1}& get_{0}()const", m => m.Key, m => m.Value)
                    .PushLine("{")
                        .FormatLine("return m_{0};", m => m.Key)
                    .PopLine("}")
                    .Line()

                    .FormatLine("void set_{0}(const {1}& v)", m => m.Key, m => m.Value)
                    .PushLine("{")
                        .FormatLine("m_{0} = v;", m => m.Key)
                    .PopLine("}")
                    .Line()
                .EndEach()

              .PopLine("}")
              .Line();

            // Print Code
            Console.WriteLine(sb.ToString());
            Console.Read();
        }
    }
}

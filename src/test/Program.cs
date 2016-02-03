using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Code;

namespace test {
    class Program {
        static void Main(string[] args){
            TestEach();
            TestSwitchCase();
            TestIfElse();
            Console.Read();
        }

        private static void TestEach(){
            // Prepare meta data
            const string className = "Example";
            var members = new Dictionary<string, string>{
                {"id", "int"},
                {"name", "string"}
            };

            // Generate CPP class
            var code = new TextBuilder();
            code.ConfigTab(4)
                .Line("//Begin Test Each")
                .Line("class {0} ", className)
                .PushLine("{")
                .TopLine("private:")
                    .Each(members)
                        .Line("{1} m_{0};", m => m.Key, m => m.Value)
                    .End()
                    .Line()
                .TopLine("public:")
                    .Line("{0}()", className)
                    .PushLine("{")
                        .Line("// TODO")
                    .PopLine("}")
                    .Line()

                    .Line("~{0}()", className)
                    .PushLine("{")
                        .Line("// TODO")
                    .PopLine("}")
                    .Line()

                .TopLine("public:")
                    .Each(members)
                        .Line("const {1}& get_{0}()const", m => m.Key, m => m.Value)
                        .PushLine("{")
                            .Line("return m_{0};", m => m.Key)
                        .PopLine("}")
                        .Line()

                        .Line("void set_{0}(const {1}& v)", m => m.Key, m => m.Value)
                        .PushLine("{")
                            .Line("m_{0} = v;", m => m.Key)
                        .PopLine("}")
                        .Line()
                    .End()
                .PopLine("}")
                .Line("// End")
                .Line();

            // Print Code
            Console.WriteLine(code.ToString());
        }

        static void TestSwitchCase(){
            var code=new TextBuilder();

            var numbers=new[]{
                1, 2
            };

            code.ConfigTab(4)
                .Line("// Begin Test SwitchCase")
                .Line("int test(int i)")
                .Line("{")
                .Push()
                    .Line("switch(i)")
                    .Line("{")
                    .Push()
                        .Each(numbers)
                            .Line("case {0}:", i => i)
                            .Switch(i => i)

                                .Case(1)
                                .Push()
                                    .Line("int i={0};", s => s)
                                    .Line("break;")
                                .Pop()

                                .Case(2)
                                .Push()
                                    .Line("int x={0};", s => s)
                                    .Line("break;")
                                .Pop()

                                .Default()
                                .Line("default:")
                                .Push()
                                    .Line("assert(false);")
                                    .Line("break;")
                                .Pop()

                            .End()
                        .End()
                    .Pop()
                    .Line("}")
                .Pop()
                .Line("}")
                .Line("// End")
                .Line();

            Console.WriteLine(code.ToString());
        }

        static void TestIfElse(){

            var code=new TextBuilder();

            code.ConfigTab(4)
                .Line("// Begin Test IfElse")
                .If(s => s, "x")
                    .Line("if")
                    .Line("if")
                .Else()
                    .Line("else")
                    .Line("else")
                    .If(s => s, "")
                        .Line("    else if:{0}", v => v)
                        .Line("    else if")
                    .Else()
                        .Line("    else if else:{0}", v => v)
                        .Line("    else if else")
                    .End()
                .End()
                .Line("// End")
                .Line();
            Console.WriteLine(code.ToString());
        }
    }
}
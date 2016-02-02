linq2code

read and write code on the fly

Example
=======

<<<<<<< HEAD
1. Prepare meta data, or DSL 
```
const string className = "Example";
var members = new Dictionary<string,string>{
    {"id","int"},
    {"name","string"}
};
```
=======
## Test Each
#### Source Code:
>>>>>>> add switch-case and if-esle struct

```
private static void TestEach(){
    // Prepare meta data
    const string className = "Example";
    var members = new Dictionary<string, string>{
            {
                    "id", "int"
            },{
                    "name", "string"
            }
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
```

#### Dest Code:

```
//Begin Test Each
class Example
{
private:
    int m_id;
    int m_id;

public:
    Example()
    {
        // TODO
    }

    ~Example()
    {
        // TODO
    }

public:
    const int& get_id()const
    {
        return m_id;
    }

    void set_id(const int& v)
    {
        m_id = v;
    }

    const int& get_id()const
    {
        return m_id;
    }

    void set_id(const int& v)
    {
        m_id = v;
    }

}
// End
// End
```

## Test Switch-Case

#### Source Code:
```
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
```

#### Dest Code:

```
// Begin Test SwitchCase
int test(int i)
{
    switch(i)
    {
        case 1:
            int i=1;
            break;
        case 1:
            int x=2;
            break;
        default:
            assert(false);
            break;
    }
}
// End
```

## Test If-Else

#### Source Code:

```
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
```
<<<<<<< HEAD
=======

#### Dest Code:

```
// Begin Test IfElse
else
else
    else if:
    else if
// End
```
>>>>>>> add switch-case and if-esle struct

linq2code

read and write code on the fly

Example
=======

1. Prepare meta data, or DSL 
```
const string className = "Example";
var members = new Dictionary<string,string>{
    {"id","int"},
    {"name","string"}
};
```

2. Generate CPP class
```
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
```

3. Print Code
```
Console.WriteLine(sb.ToString());
```

4. Output is:
```
class Example
{
private:
    int m_id;
    string m_name;

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

    const string& get_name()const
    {
        return m_name;
    }

    void set_name(const string& v)
    {
        m_name = v;
    }

}
```

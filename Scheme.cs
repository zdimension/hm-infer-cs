namespace hm_infer_cs;

public abstract record SExpr
{
    public static SExpr Parse(string s)
    {
        var pos = 0;

        void SkipWhile(Predicate<char> p)
        {
            while (p(s[pos]))
                pos++;
        }

        SList ReadList(char closing)
        {
            pos++;
            var list = new List<SExpr>();
            while (s[pos] != closing)
            {
                list.Add(Read());
                SkipWhile(char.IsWhiteSpace);
            }

            pos++;
            return new SList(list);
        }

        SAtom<int> ReadNumber()
        {
            var start = pos;
            SkipWhile(char.IsDigit);
            return new SAtom<int>(int.Parse(s[start..pos]));
        }

        SAtom<bool> ReadBoolean()
        {
            pos++;
            return new SAtom<bool>(s[pos++] switch
            {
                't' => true,
                'f' => false,
                _ => throw new Exception("Invalid boolean literal")
            });
        }

        SSymbol ReadSymbol()
        {
            var start = pos;
            SkipWhile(c => c is not ('(' or ')' or '[' or ']') && !char.IsWhiteSpace(c));
            return new SSymbol(s[start..pos]);
        }

        SAtom<string> ReadString()
        {
            pos++;
            var start = pos;
            SkipWhile(c => c != '"');
            return new SAtom<string>(s[start..pos++]);
        }

        SExpr Read()
        {
            SkipWhile(char.IsWhiteSpace);

            return s[pos] switch
            {
                '(' => ReadList(')'),
                '[' => ReadList(']'),
                '#' => ReadBoolean(),
                '"' => ReadString(),
                var c when char.IsDigit(c) => ReadNumber(),
                _ => ReadSymbol()
            };
        }

        return Read();
    }

    public T Expect<T>(string message)
    {
        if (this is T t)
            return t;
        throw new Exception(message);
    }
}

public record SAtom<T>(T Value) : SExpr
    where T : notnull
{
    public override string ToString()
    {
		if (typeof(T) == typeof(string))
			return $"\"{Value}\"";
        return Value.ToString()!;
    }
}

public record SSymbol(string Value) : SExpr
{
    public override string ToString()
    {
        return Value;
    }
}

public record SList(List<SExpr> Values) : SExpr
{
    public override string ToString()
    {
        return $"({string.Join(" ", Values)})";
    }
}
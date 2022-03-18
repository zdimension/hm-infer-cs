namespace hm_infer_cs;

public static class Utilities
{
    public static readonly TypeOperator Int = new("int");
    public static readonly TypeOperator Bool = new("bool");
    public static readonly TypeOperator Str = new("str");
    private static readonly TypeOperator Unit = new("unit");
    private static readonly TypeVariable Bottom = new();

    private static readonly TypeVariable T1 = new();
    private static readonly TypeVariable T2 = new();
    private static readonly TypeVariable T3 = new();
    private static readonly TypeVariable T4 = new();

    public static readonly Dictionary<string, BaseType> Env = new()
    {
        ["+"] = Ft(Int, Int, Int),
        ["-"] = Ft(Int, Int, Int),
        ["*"] = Ft(Int, Int, Int),
        ["/"] = Ft(Int, Int, Int),
        ["modulo"] = Ft(Int, Int, Int),
        ["="] = Ft(Int, Int, Bool),
        ["zero"] = Ft(Int, Bool),
        ["succ"] = Ft(Int, Int),
        ["pred"] = Ft(Int, Int),

        ["and"] = Ft(Bool, Bool, Bool),
        ["or"] = Ft(Bool, Bool, Bool),

        ["error"] = Ft(Str, Bottom),

        ["if"] = Ft(Bool, T1, T1, T1),

        ["pair"] = Ft(T1, T2, Op("*", T1, T2)),
        ["car"] = Ft(Op("*", T1, T2), T1),
        ["cdr"] = Ft(Op("*", T1, T2), T2),

        ["nil"] = Op("list", T1),
        ["cons"] = Ft(T1, Op("list", T1), Op("list", T1)),
        ["hd"] = Ft(Op("list", T1), T1),
        ["tl"] = Ft(Op("list", T1), Op("list", T1)),
        ["null?"] = Ft(Op("list", T1), Bool),
        ["map"] = Ft(Ft(T1, T2), Op("list", T1), Op("list", T2)),
        ["for-each"] = Ft(Ft(T1, Unit), Op("list", T1), Unit),

        ["left"] = Ft(T1, Op("either", T1, T2)),
        ["right"] = Ft(T2, Op("either", T1, T2)),
        ["either"] = Ft(Op("either", T1, T2), Ft(T1, T3), Ft(T2, T4), Op("either", T3, T4)),

        ["just"] = Ft(T1, Op("option", T1)),
        ["nothing"] = Op("option", T1),
        ["maybe"] = Ft(Op("option", T1), Ft(T1, T2), Op("option", T2))
    };

    private static TypeOperator Op(string name, params BaseType[] args)
    {
        return new TypeOperator(name, args);
    }

    public static BaseType Ft(params BaseType[] args)
    {
        return args.Length == 1
            ? args[0]
            : new TypeOperator("->", args[0], Ft(args[1..]));
    }
}

public abstract class BaseType
{
    public virtual BaseType Resolve()
    {
        return this;
    }

    public virtual bool Contains(BaseType other)
    {
        return Resolve() == other.Resolve();
    }

    public abstract void Unify(BaseType other);

    public abstract BaseType Duplicate(Dictionary<BaseType, BaseType> map, List<BaseType> ngen);
}

public class TypeVariable : BaseType
{
    private static char _current;
    private char? _assigned;

    static TypeVariable()
    {
        ResetChar();
    }

    private BaseType? Inner { get; set; }

    public override BaseType Resolve()
    {
        return Inner == null ? this : Inner = Inner.Resolve();
    }

    public static void ResetChar()
    {
        _current = 'a';
    }

    public override string ToString()
    {
        var self = Resolve();
        return self is TypeVariable v
            ? $"{v._assigned ??= _current++}"
            : self.ToString()!;
    }

    public override void Unify(BaseType other)
    {
        (var self, other) = (Resolve(), other.Resolve());

        if (self != this)
        {
            self.Unify(other);
            return;
        }

        if (this == other)
            return;

        if (other.Contains(this))
            throw new InvalidOperationException($"recursive unification between '{this}' and '{other}'");

        Inner = other;
    }

    public override BaseType Duplicate(Dictionary<BaseType, BaseType> map, List<BaseType> ngen)
    {
        var self = Resolve();

        if (self != this)
            return self.Duplicate(map, ngen);

        if (ngen.Contains(self) || ngen.Any(n => n.Resolve().Contains(self)))
            return self;

        if (map.TryGetValue(self, out var existing))
            return existing;

        return map[self] = new TypeVariable();
    }
}

public class TypeOperator : BaseType
{
    public TypeOperator(string name) : this(name, Array.Empty<BaseType>())
    {
    }

    public TypeOperator(string name, params BaseType[] parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public string Name { get; }
    public BaseType[] Parameters { get; }

    public override bool Contains(BaseType other)
    {
        other = other.Resolve();
        return base.Contains(other) || Parameters.Any(p => p.Contains(other));
    }

    public override void Unify(BaseType other)
    {
        other = other.Resolve();
        switch (other)
        {
            case TypeVariable:
                other.Unify(this);
                return;
            case TypeOperator(var name, var baseTypes) when name == Name:
            {
                if (Parameters.Length != baseTypes.Length)
                    throw new InvalidOperationException("type operator arity mismatch");

                foreach (var (a, b) in Parameters.Zip(baseTypes))
                    a.Unify(b);

                break;
            }
            default:
                throw new InvalidOperationException($"can't unify different types '{this}' and '{other}'");
        }
    }

    public override BaseType Duplicate(Dictionary<BaseType, BaseType> map, List<BaseType> ngen)
    {
        return new TypeOperator(Name, Parameters.Select(x => x.Duplicate(map, ngen)).ToArray());
    }

    public void Deconstruct(out string name, out BaseType[] parameters)
    {
        name = Name;
        parameters = Parameters;
    }

    public override string ToString()
    {
        return Name switch
        {
            "->" => $"({Parameters[0]} -> {Parameters[1]})",
            "*" => $"({string.Join(" * ", Parameters.Select(x => x.ToString()))})",
            _ => Parameters.Length == 0
                ? Name
                : $"({Name} {string.Join(" ", Parameters.Select(x => x.ToString()))})"
        };
    }
}
namespace hm_infer_cs;

using static Utilities;

public static class Inference
{
    public static BaseType Analyze(this SExpr e)
    {
        TypeVariable.ResetChar();

        BaseType Aux(SExpr expr, Dictionary<string, BaseType> env, List<BaseType> ngen)
        {
            BaseType FindSymbol(string s)
            {
                return env.TryGetValue(s, out var res)
                    ? res
                    : throw new Exception($"Unknown symbol '{s}', available are {string.Join(", ", env.Keys)}");
            }

            BaseType Self(SExpr node)
            {
                return Aux(node, env, ngen);
            }

            switch (expr)
            {
                case SList(var vals):
                    switch (vals[0])
                    {
                        case SSymbol("let"):
                        {
                            var newenv = new Dictionary<string, BaseType>(env);
                            foreach (var binding in vals[1].Expect<SList>("let: expected list of bindings").Values
                                         .Select(v => v.Expect<SList>("let: expected binding").Values))
                            {
                                if (binding.Count != 2)
                                    throw new Exception("let binding: expected pair");

                                newenv[binding[0].Expect<SSymbol>("let binding: expected identifier").Value] =
                                    Self(binding[1]);
                            }

                            return Aux(vals[2], newenv, ngen);
                        }
                        case SSymbol("let*"):
                        {
                            var bindings = vals[1].Expect<SList>("let*: expected list of bindings").Values
                                .ToArray();
                            var first = bindings[0];
                            var body = vals[2];
                            if (bindings.Length > 1)
                                return Self(
                                    new SList(new List<SExpr>
                                    {
                                        new SSymbol("let*"), new SList(new List<SExpr> { first }),
                                        new SList(new List<SExpr>
                                        {
                                            new SSymbol("let*"),
                                            new SList(bindings[1..].ToList()), body
                                        })
                                    }));

                            var binding = first.Expect<SList>("let*: expected binding").Values;
                            var newenv = new Dictionary<string, BaseType>(env)
                            {
                                [binding[0].Expect<SSymbol>("let* binding: expected identifier").Value] =
                                    Self(binding[1])
                            };
                            return Aux(vals[2], newenv, ngen);
                        }
                        case SSymbol("letrec"):
                        {
                            var newenv = new Dictionary<string, BaseType>(env);
                            var ftypes = new List<(SExpr, BaseType)>();
                            foreach (var binding in vals[1].Expect<SList>("letrec: expected list of bindings").Values
                                         .Select(v => v.Expect<SList>("letrec: expected binding").Values))
                            {
                                if (binding.Count != 2)
                                    throw new Exception("letrec binding: expected pair");

                                ftypes.Add((binding[1],
                                    newenv[binding[0].Expect<SSymbol>("letrec binding: expected identifier").Value] =
                                        new TypeVariable()));
                            }

                            foreach (var (val, type) in ftypes)
                                type.Unify(Aux(val, newenv, ftypes.Select(f => f.Item2).ToList()));

                            return Aux(vals[2], newenv, ngen);
                        }
                        case SSymbol("lambda"):
                        {
                            var pnames = vals[1].Expect<SList>("lambda: expected list of parameters").Values
                                .ToArray();
                            var param = pnames[0];
                            var body = vals[2];
                            if (pnames.Length > 1)
                                return Self(
                                    new SList(new List<SExpr>
                                    {
                                        new SSymbol("lambda"), new SList(new List<SExpr> { param }),
                                        new SList(new List<SExpr>
                                        {
                                            new SSymbol("lambda"),
                                            new SList(pnames[1..].ToList()), body
                                        })
                                    }));

                            var ptype = new TypeVariable();
                            var newenv = new Dictionary<string, BaseType>(env)
                            {
                                [param.Expect<SSymbol>("lambda: expected identifier").Value] = ptype
                            };
                            var newngen = new List<BaseType>(ngen) { ptype };
                            return Ft(ptype, Aux(body, newenv, newngen));
                        }
                        default:
                        {
                            var f = vals[0];
                            var arg = vals[1];
                            if (vals.Count > 2)
                                return Self(new SList(new[]
                                {
                                    new SList(new List<SExpr> { f, arg })
                                }.Concat(vals.Skip(2)).ToList()));

                            var val = Self(f);
                            var rettype = new TypeVariable();
                            var argtype = Self(arg);
                            var functype = Ft(argtype, rettype);
                            functype.Unify(val);
                            return rettype;
                        }
                    }
                case SAtom<int>(_):
                    return Int;
                case SAtom<bool>(_):
                    return Bool;
                case SAtom<string>(_):
                    return Str;
                case SSymbol(var name):
                    return FindSymbol(name).Duplicate(new Dictionary<BaseType, BaseType>(), ngen);
                default:
                    throw new Exception($"Unknown type for Scheme object '{expr}'");
            }
        }

        return Aux(e, Env, new List<BaseType>());
    }
}
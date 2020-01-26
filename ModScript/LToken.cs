using System;
using System.Collections.Generic;

namespace ModScript
{
    enum TokenType //: long
    {
        ADD = (1 << 0),
        ADDW = ADD | EQUAL,
        SUB = (1 << 1),
        SUBW = SUB | EQUAL,
        MULT = (1 << 2),
        MULTW = MULT | EQUAL,
        DIV = (1 << 3),
        DIVW = DIV | EQUAL,
        POW = (1 << 8),
        POWW = POW | EQUAL,
        MOD = (1 << 26),
        MODW = MOD | EQUAL,

        EQUAL = (1 << 9),

        ARROW = (1 << 20),
        RPAR = (1 << 4),
        LPAR = (1 << 5),
        RBRACK = (1 << 21),
        LBRACK = (1 << 22),
        LSQBR = (1 << 24),
        RSQBR = (1 << 25), //25

        VALUE = (1 << 6),

        KEYWORD = (1 << 10),
        IDENTIFIER = (1 << 11),

        COMMA = (1 << 23),  
        NLINE = (1 << 12),
        EOF = (1 << 7),

        EE = (1 << 13),
        NE = (1 << 14),
        GT = (1 << 15),
        LT = (1 << 16),
        GTE = GT | EE,
        LTE = LT | EE,

        AND = (1 << 17),
        OR = (1 << 18),
        NOT = (1 << 19),
        
    }



    class LToken
    {
        public TokenType type;
        public Value value;
        public TextPosition position { get; set; }
        public Context context;
        public LToken(TokenType _type, TextPosition pos)
        {
            type = _type;
            position = pos.Copy();
        }
        public LToken(TokenType _type, Value _value, TextPosition pos)
        {
            type = _type;
            value = _value;
            position = pos.Copy();
        }
        public LToken(TokenType _type)
        {
            type = _type;
        }
        public LToken(TokenType _type, Value _value)
        {
            type = _type;
            value = _value;
        }
        public LToken SetContext(Context _context)
        {
            context = _context;
            return this;
        }
        public override string ToString()
        {
            if (value == null)
                return type.ToString();
            return $"{type}:{value.type}:{value}";
        }
    }

    class Function
    {
        LToken name;
        PNode body;
        List<string> argNames;
        public Function(LToken _name, PNode node, List<string> args)
        {
            if (_name.value == null)
                name = new LToken(TokenType.VALUE, new Value("<anonymous>"), _name.position);
            else
                name = _name;
            body = node;
            argNames = args;
        }

        public RTResult Execute(List<LToken> args, Context context, TextPosition pos)
        {
            RTResult res = new RTResult();
            Context ctx = new Context(name.value.text, context, pos);
            ctx.varlist = new VarList(context.varlist);
            if (args.Count != argNames.Count)
                return res.Failure(new RuntimeError(pos, $"This function reqires {argNames.Count} arguments, insted of {args.Count}.", context));
            for (int n = 0; n < args.Count; n++)
                ctx.varlist[argNames[n]] = args[n].SetContext(ctx);
            LToken t = res.Register(Interpreter.Visit(body, ctx));
            if (res.error != null)
                return res;
            return res.Succes(t);
        }

        public Function Copy()
        {
            return new Function(name, body, argNames);
        }

        public override string ToString()
        {
            return $"<function {name}>";
        }
    }


    class Value
    {
        public static double GetNumber(string s) => double.Parse(s.Replace('.', ','));
        public string text, type;
        public bool isNumber { get { return type == "INT" || type == "FLOAT"; } }
        public double Float;
        public List<Value> values;
        public Function function;
        public double number
        {
            get
            {
                if (type == "INT")
                    return integer;
                else
                    return Float;
            }
            set
            {
                if (value == (int)value)
                {
                    integer = (int)value;
                    type = "INT";
                }
                else
                {
                    Float = value;
                    type = "FLOAT";
                }
            }
        }
        public int integer;
        public bool boolean;
        public Value(string s)
        {
            text = s;
            type = "STRING";
        }

        public Value(double d)
        {
            if (d == (int)d)
            {
                integer = (int)d;
                type = "INT";
            }
            else
            {
                Float = d;
                type = "FLOAT";
            }
        }
        public Value(bool b)
        {
            boolean = b;
            type = "BOOLEAN";
        }
        public Value(List<string> vals)
        {
            type = "LIST";
            values = vals.ConvertAll<Value>(x => new Value(x));
        }
        public Value(List<Value> vals)
        {
            type = "LIST";
            values = vals;
        }
        public Value(List<double> vals)
        {
            type = "LIST";
            values = vals.ConvertAll<Value>(x => new Value(x));
        }
        public Value(List<bool> vals)
        {
            type = "LIST";
            values = vals.ConvertAll<Value>(x => new Value(x));
        }
        public Value(Function f)
        {
            type = "FUNC";
            function = f;
        }
        public override string ToString()
        {
            switch (type)
            {
                case "STRING":
                    return text;
                case "FLOAT":
                    return Float.ToString();
                case "INT":
                    return integer.ToString();
                case "BOOLEAN":
                    return boolean.ToString();
                case "LIST":
                    string s = "[";
                    if (values.Count > 0)
                    {
                        s += values[0].ToString();
                        for (int n = 1; n < values.Count; n++)
                            s += ", " + values[n].ToString();
                    }
                    return s + "]";
                case "FUNC":
                    return function.ToString();
                default:
                    return "null";
            }
        }


        public static bool operator ==(Value a, Value b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.isNumber && b.isNumber)
                return a.number == b.number;
            if (a.type != b.type)
                return false;
            switch (a.type)
            {
                case "STRING":
                    return a.text == b.text;
                case "BOOLEAN":
                    return a.boolean == b.boolean;
                case "LIST":
                    if (a.values.Count != b.values.Count)
                        return false;
                    for (int n = 0; n < a.values.Count; n++)
                        if (a.values[n] != b.values[n])
                            return false;
                    return true;
                case "FUNC":
                    return a.function == b.function;
                default:
                    return false;
            }
        }
        public static bool operator !=(Value a, Value b)
        {
            return !(a == b);
        }

    }
}

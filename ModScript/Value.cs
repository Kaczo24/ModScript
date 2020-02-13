using System.Collections.Generic;
using System;
namespace ModScript
{ 
    class Value : IEquatable<Value>
    {
        public static double GetNumber(string s) => double.Parse(s.Replace('.', ','));
        public string text, type;
        public bool isNumber { get { return type == "INT" || type == "FLOAT"; } }
        public double Float;
        public List<Value> values;
        public Function function;
        public Context context;
        public int integer;
        public bool boolean = false;


        public static Value NULL = new Value();
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
        public Value()
        {
            type = "NULL";
        }
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
            function = f.SetParent(this);
        }
        public Value SetContext(Context _context)
        {
            context = _context;
            return this;
        }


        public Value GetProperty(string prop)
        {
            switch (prop)
            {
                case "length":
                    return InnerValue.length(this);
                default:
                    if (type == "FUNC")
                        if (function.InnerValues.ContainsKey(prop))
                            return function.InnerValues[prop].value;
                    return NULL;
            }
        }

        public Value SetProperty(string prop, LToken val)
        {
            switch (prop)
            {
                default:
                    if (type == "FUNC")
                    {
                        if (function.InnerValues.ContainsKey(prop))
                        if (val.value.type == "FUNC")
                            val.value.function.InnerValues.parent = function.InnerValues;
                        return (function.InnerValues[prop] = val).value;
                    }
                    return NULL;
            }
        }

        public RTResult CallProperty(string prop, List<LToken> args, Context _context, TextPosition pos)
        {
            switch (prop)
            {
                case "Contains":
                    return InnerValue.Contains(this, args, _context, pos);
                default:
                    if (type == "FUNC")
                        if (function.InnerValues.ContainsKey(prop))
                            return function.InnerValues[prop].value.function.Copy().Execute(args, _context, pos);
                    return new RTResult().Failure(new RuntimeError(pos, prop + " in " + type + " is not a function.", _context));
            }
        }

        public Value Copy()
        {
            Value v = new Value();
            v.boolean = boolean;
            v.Float = Float;
            if (type == "FUNC")
                v.function = function.Copy();
            v.integer = integer;
            v.text = text;
            v.type = type;
            v.SetContext(context.Copy());
            return v;
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

        public bool Equals(Value other)
        {
            return this == other;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModScript
{ 
    class Value
    {
        public static double GetNumber(string s) => double.Parse(s.Replace('.', ','));
        public string text, type;
        public bool isNumber { get { return type == "INT" || type == "FLOAT"; } }
        public double Float;
        public List<Value> values;
        public Function function;
        public Context context;

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
        public int integer;
        public bool boolean = false;
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

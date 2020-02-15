using System;
using System.Collections.Generic;
using System.IO;


namespace ModScript
{
    delegate RTResult Predef(Context _context);
    class Function
    {
        public LToken name;
        Value parent;
        PNode body;
        List<string> argNames;
        Context DeffCon;
        public VarList InnerValues = new VarList(null);
        public Function(LToken _name, PNode node, List<string> args, Context _context)
        {
            if (_name.value == null)
                name = new LToken(TokenType.VALUE, new Value("<anonymous>"), _name.position);
            else
                name = _name;
            body = node;
            argNames = args;
            DeffCon = _context;
        }

        public Function SetParent(Value v)
        {
            parent = v;
            return this;
        }

        public RTResult Execute(List<LToken> args, Context _context, TextPosition pos)
        {
            RTResult res = new RTResult();
            Context ctx = parent.context.Copy();
            ctx.name = name.value.text;
            ctx.parentEntry = pos;
            ctx.parent = _context;
            ctx.lastIn = this;
            ctx.varlist.parent = DeffCon.varlist;
            ctx.varlist["this"] = new LToken(TokenType.VALUE, new Value(this).SetContext(parent.context), pos);
            if (args.Count != argNames.Count)
                return res.Failure(new RuntimeError(pos, $"This function reqires {argNames.Count} arguments, insted of {args.Count}.", parent.context));
            for (int n = 0; n < args.Count; n++)
                ctx.varlist.Add(argNames[n], args[n].SetContext(ctx));
            LToken t;
            if (Predefs.ContainsKey(name.value.text))
                t = res.Register(Predefs[name.value.text].Item1(ctx));
            else if (Special.ContainsKey(name.value.text))
                t = res.Register(Special[name.value.text].Item1(ctx));
            else
                t = res.Register(Interpreter.Visit(body, ctx));
            if (res.error != null)
                return res;
            return res.Succes(t.SetContext(ctx));
        }

        public Function Copy()
        {
            Function f = new Function(name, body, argNames, DeffCon);
            f.parent = parent;
            f.InnerValues = InnerValues.Copy();
            if (InnerValues.parent != null)
                foreach (LToken t in f.InnerValues.parent.Values)
                    if (t.value.type == "FUNC")
                        t.value.function.InnerValues.parent = f.InnerValues;
            return f;
        }

        public override string ToString()
        {
            return $"<function {name}>";
        }
        public static Dictionary<string, Tuple<Predef, int>> Predefs = new Dictionary<string, Tuple<Predef, int>>()
        {
            { "Print",  new Tuple<Predef, int>(PredefFunc.Print, 1)},
            { "Printl", new Tuple<Predef, int>(PredefFunc.Printl, 1)},
            { "PrintAscii", new Tuple<Predef, int>(PredefFunc.PrintAscii, 1)},
            { "Input", new Tuple<Predef, int>(PredefFunc.Input, 0)},
            { "InputN", new Tuple<Predef, int>(PredefFunc.InputN, 0)},

            { "Sqrt", new Tuple<Predef, int>(PredefFunc.sqrt, 1)},

            { "File", new Tuple<Predef, int>(PredefFunc.FileF, 0) },
            { "String", new Tuple<Predef, int>(PredefFunc.String, 1) },
            { "List", new Tuple<Predef, int>(PredefFunc.List, 0) },

            { "GetType", new Tuple<Predef, int>(PredefFunc.GetType, 1)},
            { "ParseNumber", new Tuple<Predef, int>(PredefFunc.ParseNumber, 1)},

        };
        public static Dictionary<string, Tuple<Predef, int>> Special = new Dictionary<string, Tuple<Predef, int>>();
    }

    static class PredefFunc
    {
        #region META
        //static string FileProg = 
        //    "return _ARG0;";
        //static PNode FileNode = new Parser(new Lexer("<base>", FileProg).tokens).node;




        public static void Insert(Function f, Dictionary<string, Tuple<Predef, int>> funcs)
        {
            foreach (string s in funcs.Keys)
            {
                List<string> args = new List<string>();
                for (int n = 0; n < funcs[s].Item2; n++)
                    args.Add("_ARG" + n);
                f.InnerValues[s] = new LToken(TokenType.VALUE, new Value(new Function(new LToken(TokenType.VALUE, new Value(s).SetContext(Compiler.root), new TextPosition(0, 0, 0, "CONST", "")), null, args, Compiler.root)).SetContext(Compiler.root), new TextPosition(0, 0, 0, "CONST", ""));
                Function.Special.Add(s, funcs[s]);
            }
        }

        #endregion


        #region Interaction
        public static RTResult Print(Context ctx)
        {
            Console.Write(ctx.varlist["_ARG0"].value);
            return new RTResult().Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult Printl(Context ctx)
        {
            Console.WriteLine(ctx.varlist["_ARG0"].value);
            return new RTResult().Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult PrintAscii(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "INT")
                return res.Failure(new RuntimeError(ctx.varlist["_ARG0"].position, "PrintAscii requies an integer.", ctx));
            if (ctx.varlist["_ARG0"].value.integer < 0 || ctx.varlist["_ARG0"].value.integer > 255)
                return res.Failure(new RuntimeError(ctx.varlist["_ARG0"].position, "PrintAscii requies argument to be between 0 and 256 (IE).", ctx));
            Console.Write(System.Text.ASCIIEncoding.UTF8.GetChars(new byte[] { (byte)ctx.varlist["_ARG0"].value.integer })[0]);
            return res.Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult Input(Context ctx)
        {
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(Console.ReadLine()), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult InputN(Context ctx)
        {
            double d;
            if (!double.TryParse(Console.ReadLine().Replace('.', ','), out d))
                d = 0;
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(d), ctx.parentEntry).SetContext(ctx));
        }
        #endregion
        #region Math
        public static RTResult sqrt(Context ctx)
        {
            RTResult res = new RTResult();
            if (!ctx.varlist["_ARG0"].value.isNumber)
                return res.Failure(new RuntimeError(ctx.parentEntry, "Sqrt can be applied only to numbers", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(Math.Sqrt(ctx.varlist["_ARG0"].value.number)), ctx.parentEntry).SetContext(ctx));
        }

        #endregion
        #region File

        public static RTResult FileF(Context ctx)
        {
            return new RTResult().Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
            //return Interpreter.Visit(runtimes["File"], ctx);
        }

        public static RTResult ReadLines(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if(!File.Exists(ctx.varlist["_ARG0"].value.text))
                return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.varlist["_ARG0"].value.text}' does not exist.", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(new List<string>(File.ReadAllLines(ctx.varlist["_ARG0"].value.text)).ConvertAll(x => new Value(x))), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult ReadText(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if (!File.Exists(ctx.varlist["_ARG0"].value.text))
                return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.varlist["_ARG0"].value.text}' does not exist.", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(File.ReadAllText(ctx.varlist["_ARG0"].value.text)), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult WriteText(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));

            File.WriteAllText(ctx.varlist["_ARG0"].value.text, ctx.varlist["_ARG1"].value.ToString());
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult WriteLines(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if (ctx.varlist["_ARG1"].value.type != "LIST")
                return res.Failure(new RuntimeError(ctx.parentEntry, "Data to write has to be a list", ctx));

            File.WriteAllLines(ctx.varlist["_ARG0"].value.text, ctx.varlist["_ARG1"].value.values.ConvertAll(x => x.ToString()));
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
        }
        #endregion
        #region Primitive
        public static RTResult String(Context ctx) =>
            new RTResult().Succes(new LToken(TokenType.VALUE, new Value(ctx.varlist["_ARG0"].value.ToString()), ctx.parentEntry).SetContext(ctx));

        public static RTResult List(Context ctx) =>
            new RTResult().Succes(new LToken(TokenType.VALUE, new Value(new List<Value>()), ctx.parentEntry).SetContext(ctx));

        #endregion
        #region Miscellaneous
        public static RTResult GetType(Context ctx)
        {
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(ctx.varlist["_ARG0"].value.type), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult ParseNumber(Context ctx)
        {
            RTResult res = new RTResult();
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            double d;
            if (!double.TryParse(ctx.varlist["_ARG0"].value.text.Replace('.', ','), out d))
                d = 0;
            return res.Succes(new LToken(TokenType.VALUE, new Value(d), ctx.parentEntry).SetContext(ctx));
        }
        #endregion



    }
}

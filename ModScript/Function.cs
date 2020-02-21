using System;
using System.Collections.Generic;
using System.IO;


namespace ModScript
{
    delegate RTResult Predef(Context _context, int argN);
    class Function
    {
        public LToken name;
        Value parent;
        public PNode body;
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
            InnerValues["this"] = new LToken(TokenType.VALUE, new Value(this).SetContext(name.value.context), name.position);
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
            if (Predefs.ContainsKey(name.value.text) || Special.ContainsKey(name.value.text))
            {
                for (int n = 0; n < args.Count; n++)
                    ctx.varlist.Add("_ARG" + n, args[n].SetContext(ctx));
            }
            else for (int n = 0; n < argNames.Count; n++)
                    if (n < args.Count)
                        ctx.varlist.Add(argNames[n], args[n].SetContext(ctx));
                    else
                        ctx.varlist.Add(argNames[n], new LToken(TokenType.VALUE, Value.NULL, pos).SetContext(ctx));

            LToken t;
            if (Predefs.ContainsKey(name.value.text))
                t = res.Register(Predefs[name.value.text](ctx, args.Count));
            else if (Special.ContainsKey(name.value.text))
                t = res.Register(Special[name.value.text](ctx, args.Count));
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
            f.InnerValues["this"] = new LToken(TokenType.VALUE, new Value(f).SetContext(name.value.context), name.position);
            VarList F = f.InnerValues;
            while (F.parent != null)
            {
                List<string> Keys = new List<string>();
                foreach (string s in F.parent.Keys)
                    Keys.Add(s);
                for (int n = 0; n < Keys.Count; n++)
                {
                    string s = Keys[n];
                    if (F.parent[s].value.type == "FUNC")
                    {
                        F.parent[s] = F.parent[s].Copy(true);
                        F.parent[s].value.function.InnerValues.parent = F;
                    }
                }
                F = F.parent;
            }
            return f;
        }

        public Function Copy(bool b)
        {
            Function f = new Function(name, body, argNames, DeffCon);
            f.parent = parent;
            f.InnerValues = InnerValues.Copy();
            f.InnerValues["this"] = new LToken(TokenType.VALUE, new Value(f).SetContext(name.value.context), name.position);
            return f;
        }
        public override string ToString()
        {
            return $"<function {name}>";
        }
        public static Dictionary<string, Predef> Predefs = new Dictionary<string, Predef>()
        {
            { "Print",  PredefFunc.Print},
            { "Printl", PredefFunc.Printl},
            { "PrintAscii", PredefFunc.PrintAscii},
            { "Input", PredefFunc.Input},
            { "InputN",PredefFunc.InputN},

            { "Sqrt",PredefFunc.sqrt},

            { "File",PredefFunc.FileF },
            { "String",PredefFunc.String },
            { "List",PredefFunc.List },

            { "GetType",PredefFunc.GetType},
            { "ParseNumber",PredefFunc.ParseNumber},

        };
        public static Dictionary<string, Predef> Special = new Dictionary<string, Predef>();
    }

    static class PredefFunc
    {
        #region META
        //static string FileProg = 
        //    "return _ARG0;";
        //static PNode FileNode = new Parser(new Lexer("<base>", FileProg).tokens).node;




        public static void Insert(Function f, Dictionary<string, Predef> funcs)
        {
            if (f.InnerValues.parent == null)
                f.InnerValues.parent = new VarList(null);
            foreach (string s in funcs.Keys)
            {
                f.InnerValues.parent[s] = 
                    new LToken(TokenType.VALUE, new Value(
                        new Function(
                            new LToken(TokenType.VALUE, new Value(s).SetContext(Compiler.root), new TextPosition(0, 0, 0, "CONST", "")),
                            null, null, Compiler.root)).SetContext(Compiler.root),
                    new TextPosition(0, 0, 0, "CONST", "")).SetPM();

                f.InnerValues[s].value.function.InnerValues.parent = f.InnerValues;
                Function.Special.Add(s, funcs[s]);
            }
        }
        static RTResult MinArgError(Context context, int n) =>
            new RTResult().Failure(new RuntimeError(context.parentEntry, "Minimal agrument requirement fo this function is " + n, context));
        #endregion


        #region Interaction
        public static RTResult Print(Context ctx, int argN)
        {
            if (argN < 1)
                return MinArgError(ctx, 1);
            Console.Write(ctx.varlist["_ARG0"].value);
            return new RTResult().Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult Printl(Context ctx, int argN)
        {
            if (argN < 1)
                return MinArgError(ctx, 1);
            Console.WriteLine(ctx.varlist["_ARG0"].value);
            return new RTResult().Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult PrintAscii(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN < 1)
                return MinArgError(ctx, 1);
            if (ctx.varlist["_ARG0"].value.type != "INT")
                return res.Failure(new RuntimeError(ctx.varlist["_ARG0"].position, "PrintAscii requies an integer.", ctx));
            if (ctx.varlist["_ARG0"].value.integer < 0 || ctx.varlist["_ARG0"].value.integer > 255)
                return res.Failure(new RuntimeError(ctx.varlist["_ARG0"].position, "PrintAscii requies argument to be between 0 and 256 (IE).", ctx));
            Console.Write(System.Text.ASCIIEncoding.UTF8.GetChars(new byte[] { (byte)ctx.varlist["_ARG0"].value.integer })[0]);
            return res.Succes(ctx.varlist["_ARG0"]);
        }
        public static RTResult Input(Context ctx, int argN)
        {
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(Console.ReadLine()), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult InputN(Context ctx, int argN)
        {
            double d;
            if (!double.TryParse(Console.ReadLine().Replace('.', ','), out d))
                d = 0;
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(d), ctx.parentEntry).SetContext(ctx));
        }
        #endregion
        #region Math
        public static RTResult sqrt(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN < 1)
                return MinArgError(ctx, 1);
            if (!ctx.varlist["_ARG0"].value.isNumber)
                return res.Failure(new RuntimeError(ctx.parentEntry, "Sqrt can be applied only to numbers", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(Math.Sqrt(ctx.varlist["_ARG0"].value.number)), ctx.parentEntry).SetContext(ctx));
        }

        #endregion
        #region File

        public static RTResult FileF(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN > 0)
            {
                if (ctx.varlist["_ARG0"].value.type != "STRING")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
                ctx.lastIn.InnerValues["path"] = ctx.varlist["_ARG0"];
                return res.Succes(new LToken(TokenType.VALUE, new Value(ctx.lastIn), ctx.parentEntry).SetContext(ctx));
            }
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
        }

        public static RTResult ReadLines(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if(argN == 0)
            {
                if(!ctx.lastIn.InnerValues.ContainsKey("path"))
                    return res.Failure(new RuntimeError(ctx.parentEntry, "Zero parameter version of this function can only be run from an already defined file.", ctx));
                if (ctx.lastIn.InnerValues["path"].value.type != "STRING")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
                if (!File.Exists(ctx.lastIn.InnerValues["path"].value.text))
                    return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.lastIn.InnerValues["path"].value.text}' does not exist.", ctx));
                return res.Succes(new LToken(TokenType.VALUE, new Value(new List<string>(File.ReadAllLines(ctx.lastIn.InnerValues["path"].value.text)).ConvertAll(x => new Value(x))), ctx.parentEntry).SetContext(ctx));
            }
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if(!File.Exists(ctx.varlist["_ARG0"].value.text))
                return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.varlist["_ARG0"].value.text}' does not exist.", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(new List<string>(File.ReadAllLines(ctx.varlist["_ARG0"].value.text)).ConvertAll(x => new Value(x))), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult ReadText(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN == 0)
            {
                if (!ctx.lastIn.InnerValues.ContainsKey("path"))
                    return res.Failure(new RuntimeError(ctx.parentEntry, "Zero parameter version of this function can only be run from an already defined file.", ctx));
                if (ctx.lastIn.InnerValues["path"].value.type != "STRING")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
                if (!File.Exists(ctx.lastIn.InnerValues["path"].value.text))
                    return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.lastIn.InnerValues["path"].value.text}' does not exist.", ctx));
                return res.Succes(new LToken(TokenType.VALUE, new Value(File.ReadAllText(ctx.lastIn.InnerValues["path"].value.text)), ctx.parentEntry).SetContext(ctx));
            }
            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if (!File.Exists(ctx.varlist["_ARG0"].value.text))
                return res.Failure(new RuntimeError(ctx.parentEntry, $"File '{ctx.varlist["_ARG0"].value.text}' does not exist.", ctx));
            return res.Succes(new LToken(TokenType.VALUE, new Value(File.ReadAllText(ctx.varlist["_ARG0"].value.text)), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult WriteText(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN < 1)
                return MinArgError(ctx, 1);
            if (argN == 1)
            {
                if (!ctx.lastIn.InnerValues.ContainsKey("path"))
                    return res.Failure(new RuntimeError(ctx.parentEntry, "Zero parameter version of this function can only be run from an already defined file.", ctx));
                if (ctx.lastIn.InnerValues["path"].value.type != "STRING")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
                File.WriteAllText(ctx.lastIn.InnerValues["path"].value.text, ctx.varlist["_ARG0"].value.ToString());
                return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
            }

            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));

            File.WriteAllText(ctx.varlist["_ARG0"].value.text, ctx.varlist["_ARG1"].value.ToString());
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult WriteLines(Context ctx, int argN)
        {
            RTResult res = new RTResult();
            if (argN < 1)
                return MinArgError(ctx, 1);
            if (argN == 1)
            {
                if (!ctx.lastIn.InnerValues.ContainsKey("path"))
                    return res.Failure(new RuntimeError(ctx.parentEntry, "Zero parameter version of this function can only be run from an already defined file.", ctx));
                if (ctx.lastIn.InnerValues["path"].value.type != "STRING")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
                if (ctx.varlist["_ARG0"].value.type != "LIST")
                    return res.Failure(new RuntimeError(ctx.parentEntry, "Data to write has to be a list", ctx));
                File.WriteAllLines(ctx.lastIn.InnerValues["path"].value.text, ctx.varlist["_ARG1"].value.values.ConvertAll(x => x.ToString()));
                return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
            }

            if (ctx.varlist["_ARG0"].value.type != "STRING")
                return res.Failure(new RuntimeError(ctx.parentEntry, "File path has to be a string", ctx));
            if (ctx.varlist["_ARG1"].value.type != "LIST")
                return res.Failure(new RuntimeError(ctx.parentEntry, "Data to write has to be a list", ctx));

            File.WriteAllLines(ctx.varlist["_ARG0"].value.text, ctx.varlist["_ARG1"].value.values.ConvertAll(x => x.ToString()));
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, ctx.parentEntry).SetContext(ctx));
        }
        #endregion
        #region Primitive
        public static RTResult String(Context ctx, int argN)
        {
            if (argN < 1)
                return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(""), ctx.parentEntry).SetContext(ctx));
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(ctx.varlist["_ARG0"].value.ToString()), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult List(Context ctx, int argN)
        {
            List<Value> vls = new List<Value>();
            for (int n = 0; n < argN; n++)
                vls.Add(ctx.varlist["_ARG" + n].value);
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(vls), ctx.parentEntry).SetContext(ctx));
        }

        #endregion
        #region Miscellaneous
        public static RTResult GetType(Context ctx, int argN)
        {
            if (argN < 1)
                return MinArgError(ctx, 1);
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(ctx.varlist["_ARG0"].value.type), ctx.parentEntry).SetContext(ctx));
        }
        public static RTResult ParseNumber(Context ctx, int argN)
        {
            RTResult res = new RTResult(); 
            if (argN < 1)
                return MinArgError(ctx, 1);
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

using System;
using System.Collections.Generic;
using System.IO;


namespace ModScript
{
    class Function
    {
        public LToken name;
        Value parent;
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
            ctx.varlist.parent = _context.varlist;
            ctx.varlist["this"] = new LToken(TokenType.VALUE, parent, pos);
            if (args.Count != argNames.Count)
                return res.Failure(new RuntimeError(pos, $"This function reqires {argNames.Count} arguments, insted of {args.Count}.", parent.context));
            for (int n = 0; n < args.Count; n++)
                ctx.varlist[argNames[n]] = args[n].SetContext(ctx);
            LToken t;
            if (Predefs.ContainsKey(name.value.text))
                t = res.Register(Predefs[name.value.text].Item1(ctx));
            else
                t = res.Register(Interpreter.Visit(body, ctx));
            if (res.error != null)
                return res;
            return res.Succes(t.SetContext(ctx));
        }

        public Function Copy()
        {
            return new Function(name, body, argNames);
        }

        public override string ToString()
        {
            return $"<function {name}>";
        }
        public delegate RTResult Predef(Context _context);
        public static Dictionary<string, Tuple<Predef, int>> Predefs = new Dictionary<string, Tuple<Predef, int>>()
        {
            { "Print",  new Tuple<Predef, int>(PredefFunc.Print, 1)},
            { "Printl", new Tuple<Predef, int>(PredefFunc.Printl, 1)},
            { "PrintAscii", new Tuple<Predef, int>(PredefFunc.PrintAscii, 1)},
            { "Input", new Tuple<Predef, int>(PredefFunc.Input, 0)},
            { "InputN", new Tuple<Predef, int>(PredefFunc.InputN, 0)},

            { "Sqrt", new Tuple<Predef, int>(PredefFunc.sqrt, 1)},
            
            { "ReadText", new Tuple<Predef, int>(PredefFunc.ReadText, 1)},
            { "ReadLines", new Tuple<Predef, int>(PredefFunc.ReadLines, 1)},
            { "WriteText", new Tuple<Predef, int>(PredefFunc.WriteText, 2)},
            { "WriteLines", new Tuple<Predef, int>(PredefFunc.WriteLines, 2)},

            { "GetType", new Tuple<Predef, int>(PredefFunc.GetType, 1)},
            { "ParseNumber", new Tuple<Predef, int>(PredefFunc.ParseNumber, 1)},

        };
    }

    static class PredefFunc
    {
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

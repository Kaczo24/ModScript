using System;
using System.Collections.Generic;

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
            { "Input", new Tuple<Predef, int>(PredefFunc.Input, 0)},
            { "InputN", new Tuple<Predef, int>(PredefFunc.InputN, 0)},


            { "Sqrt", new Tuple<Predef, int>(PredefFunc.sqrt, 1)},


            { "GetType", new Tuple<Predef, int>(PredefFunc.GetType, 1)},

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

        #endregion
        #region Miscellaneous
        public static RTResult GetType(Context ctx)
        {
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(ctx.varlist["_ARG0"].value.type), ctx.parentEntry).SetContext(ctx));
        }
        #endregion
    }
}

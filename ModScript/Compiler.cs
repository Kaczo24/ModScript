using System;
using System.Collections.Generic;

namespace ModScript
{
    static class Compiler
    {
        public static Context root = new Context("<base>");
        public static List<string> forbidden = new List<string>();
        public static VarList globalVar = new VarList(null)
        {
            {"true", new LToken(TokenType.VALUE, new Value(true), new TextPosition(0,0,0,"CONST", "")).SetContext(root)},
            {"false", new LToken(TokenType.VALUE, new Value(false), new TextPosition(0,0,0,"CONST", "")).SetContext(root)},
            {"null", new LToken(TokenType.VALUE, Value.NULL, new TextPosition(0,0,0,"CONST", "")).SetContext(root)},
        };

        public static void Prepare()
        {
            foreach (string k in Function.Predefs.Keys)
            {
                List<string> args = new List<string>();
                int m = Function.Predefs[k].Item2;
                for (int n = 0; n < m; n++)
                    args.Add("_ARG" + n);
                globalVar[k] = new LToken(TokenType.VALUE, new Value(new Function(new LToken(TokenType.VALUE, new Value(k).SetContext(root), new TextPosition(0, 0, 0, "CONST", "")), null, args, root)).SetContext(root), new TextPosition(0, 0, 0, "CONST", ""));
            }
            root.varlist = globalVar;
            foreach (string s in globalVar.Keys)
                forbidden.Add(s);
            foreach (string s in Function.Special.Keys)
                forbidden.Add(s);
            PredefFunc.Insert(globalVar["File"].value.function, new Dictionary<string, Tuple<Predef, int>>()
            {
                { "ReadText", new Tuple<Predef, int>(PredefFunc.ReadText, 1)},
                { "ReadLines", new Tuple<Predef, int>(PredefFunc.ReadLines, 1)},
                { "WriteText", new Tuple<Predef, int>(PredefFunc.WriteText, 2)},
                { "WriteLines", new Tuple<Predef, int>(PredefFunc.WriteLines, 2)},
            });
            forbidden.Add("File");
        }

        public static RTResult Run(string line, string fName)
        {
            Lexer lexer = new Lexer(fName, line);
            if (lexer.error != null)
            {
                Console.WriteLine(lexer.error);
                return new RTResult().Failure(lexer.error);
            }
            
            Parser parser = new Parser(lexer.tokens);
            if (parser.error != null)
            {
                Console.WriteLine(parser.error);
                return new RTResult().Failure(parser.error);
            }

            return Interpreter.Visit(parser.node, root); ;
        }
    }
}

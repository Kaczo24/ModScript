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
                globalVar[k] = new LToken(TokenType.VALUE, new Value(new Function(new LToken(TokenType.VALUE, new Value(k).SetContext(root), new TextPosition(0, 0, 0, "CONST", "")), null, null, root)).SetContext(root), new TextPosition(0, 0, 0, "CONST", ""));
            }
            root.varlist = globalVar;
            foreach (string s in globalVar.Keys)
                forbidden.Add(s);
            PredefFunc.Insert(globalVar["File"].value.function, new Dictionary<string, Predef>()
            {
                { "ReadText", PredefFunc.ReadText},
                { "ReadLines", PredefFunc.ReadLines},
                { "WriteText", PredefFunc.WriteText},
                { "WriteLines", PredefFunc.WriteLines},
            });

            foreach (string s in Function.Special.Keys)
                forbidden.Add(s);
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

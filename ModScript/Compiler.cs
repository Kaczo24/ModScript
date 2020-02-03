using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModScript
{
    static class Compiler
    {
        static Context root = new Context("<base>");
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
                globalVar[k] = new LToken(TokenType.VALUE, new Value(new Function(new LToken(TokenType.VALUE, new Value(k).SetContext(root), new TextPosition(0, 0, 0, "CONST", "")), null, args)).SetContext(root), new TextPosition(0, 0, 0, "CONST", ""));
            }
            root.varlist = globalVar;
            foreach (string s in globalVar.Keys)
                forbidden.Add(s);
        }

        public static void Run(string line)
        {
            Lexer lexer = new Lexer(".mod", line);
            if (lexer.error != null)
            {
                Console.WriteLine(lexer.error);
                return;
            }
            
            Parser parser = new Parser(lexer.tokens);
            if (parser.error != null)
            {
                Console.WriteLine(parser.error);
                return;
            }

            RTResult res = Interpreter.Visit(parser.node, root);
            if (res.error != null)
            {
                Console.WriteLine(res.error);
                return;
            }
        }
    }
}

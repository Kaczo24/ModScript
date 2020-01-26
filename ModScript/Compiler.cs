using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModScript
{
    static class Compiler
    {
        static Context root = new Context("<prompt>");
        static VarList globalVar = new VarList(null)
        {
            {"true", new LToken(TokenType.VALUE, new Value(true), new TextPosition(0,0,0,"CONST", "")).SetContext(root)},
            {"false", new LToken(TokenType.VALUE, new Value(false), new TextPosition(0,0,0,"CONST", "")).SetContext(root)}
        };


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
            //Console.WriteLine(parser.node);

            
            root.varlist = globalVar;
            RTResult res = Interpreter.Visit(parser.node, root);
            if (res.error != null)
            {
                Console.WriteLine(res.error);
                return;
            }
            Console.WriteLine(res.value.value);
        }
    }
}

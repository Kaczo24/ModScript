using System;
using System.IO;
using System.Collections.Generic;

namespace ModScript
{
    class Program
    {
        static void Main(string[] args)
        {
            RTResult res = new RTResult();
            Compiler.Prepare();
            if (args.Length == 1)
            {
                res = Compiler.Run(File.ReadAllText(args[0]), new FileInfo(args[0]).Name);
                if (res.error != null)
                    Console.WriteLine(res.error);
                Console.Read();
                return;
            }
            while(true)
            {
                Console.Write("ModScript> ");
                string inp = Console.ReadLine();
                res = Compiler.Run(inp, "<console>");
                if (res.error != null)
                    Console.WriteLine(res.error);
                Console.WriteLine();
            }
        }
    }

}

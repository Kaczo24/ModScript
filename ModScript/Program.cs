using System;
using System.IO;
using System.Collections.Generic;

namespace ModScript
{
    class Program
    {
        static void Main(string[] args)
        {
            Compiler.Prepare();
            //if (args.Length == 1)
            {
                Compiler.Run(File.ReadAllText("Brainfuck.ms"));//args[0]));
                Console.Read();
                return;
            }
            while(true)
            {
                Console.Write("ModScript> ");
                string inp = Console.ReadLine();
                Compiler.Run(inp);
                Console.WriteLine();
            }
        }
    }

}

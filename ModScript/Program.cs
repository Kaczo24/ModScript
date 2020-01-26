﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModScript
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                foreach (string s in File.ReadAllLines(args[0]))
                    Compiler.Run(s);
                Console.Read();
                return;
            }
            while(true)
            {
                Console.Write("ModScript> ");
                string inp = Console.ReadLine();
                Compiler.Run(inp);
                //Console.WriteLine(Compiler.Run(inp));
                Console.WriteLine();
            }
        }
    }

}

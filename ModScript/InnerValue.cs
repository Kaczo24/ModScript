using System;
using System.Collections.Generic;

namespace ModScript
{
    static class InnerValue
    {
        public static Value length(Value v)
        {
            if (v.type == "STRING")
                return new Value(v.text.Length);
            if (v.type == "LIST")
                return new Value(v.values.Count);
            return Value.NULL;
        }

        //----------------------------------------------------------------

        public static RTResult Contains(Value v, List<LToken> args, Context _context, TextPosition pos)
        {
            if (args.Count != 1)
                return new RTResult().Failure(new RuntimeError(pos, "Function 'Contains' requres 1 argument.", _context));
            if (v.type == "STRING")
            {
                if (args[0].value.type != "STRING")
                    return new RTResult().Failure(new RuntimeError(pos, "Function 'Contains' for strings a string argument.", _context));
                return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(v.text.Contains(args[0].value.text)), pos).SetContext(_context));
            }
            if(v.type == "LIST")
                return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(v.values.Contains(args[0].value)), pos).SetContext(_context));
            return new RTResult().Failure(new RuntimeError(pos, "Function 'Contains' can be used only for string and list types.", _context));
        }

        public static RTResult toString(Value v, List<LToken> args, Context _context, TextPosition pos)
        {
            if (args.Count != 0)
                return new RTResult().Failure(new RuntimeError(pos, "Function 'Contains' requres 0 arguments.", _context));
            return new RTResult().Succes(new LToken(TokenType.VALUE, new Value(v.ToString()), pos).SetContext(_context));
        }

        public static RTResult Insert(Value v, List<LToken> args, Context _context, TextPosition pos)
        {
            RTResult res = new RTResult();
            if (args.Count != 2)
                return res.Failure(new RuntimeError(pos, "Function 'Insert' requres 2 arguments.", _context));
            if (args[0].value.type != "INT")
                return res.Failure(new RuntimeError(pos, "Function 'Insert' a int argument1.", _context));
            if (v.type == "STRING")
            {
                if (args[0].value.integer > v.text.Length)
                    return res.Failure(new RuntimeError(pos, "Insertion position cannot be greater then string length", _context));
                v.text.Insert(args[0].value.integer, args[1].value.ToString());
                return res.Succes(new LToken(TokenType.VALUE, v, pos).SetContext(_context));
            }
            if (v.type == "LIST")
            {
                if (args[0].value.integer > v.values.Count)
                    return res.Failure(new RuntimeError(pos, "Insertion position cannot be greater then list length", _context));
                v.values.Insert(args[0].value.integer, args[1].value);
                return res.Succes(new LToken(TokenType.VALUE, v, pos).SetContext(_context));
            }
            return res.Failure(new RuntimeError(pos, "Function 'Insert' can be used only for string and list types.", _context));

        }
    }

}

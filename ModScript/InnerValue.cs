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
    }
}

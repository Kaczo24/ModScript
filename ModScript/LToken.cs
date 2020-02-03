﻿using System;
using System.Collections.Generic;

namespace ModScript
{
    enum TokenType //: long
    {
        ADD = (1 << 0),
        ADDW = ADD | EQUAL,
        SUB = (1 << 1),
        SUBW = SUB | EQUAL,
        MULT = (1 << 2),
        MULTW = MULT | EQUAL,
        DIV = (1 << 3),
        DIVW = DIV | EQUAL,
        POW = (1 << 8),
        POWW = POW | EQUAL,
        MOD = (1 << 26),
        MODW = MOD | EQUAL,

        EQUAL = (1 << 9),

        ARROW = (1 << 20),
        RPAR = (1 << 4),
        LPAR = (1 << 5),
        RBRACK = (1 << 21),
        LBRACK = (1 << 22),
        LSQBR = (1 << 24),
        RSQBR = (1 << 25), //25

        VALUE = (1 << 6),

        KEYWORD = (1 << 10),
        IDENTIFIER = (1 << 11),

        COMMA = (1 << 23),  
        NLINE = (1 << 12),
        EOF = (1 << 7),

        EE = (1 << 13),
        NE = (1 << 14),
        GT = (1 << 15),
        LT = (1 << 16),
        GTE = GT | EE,
        LTE = LT | EE,

        AND = (1 << 17),
        OR = (1 << 18),
        NOT = (1 << 19),
        
    }



    class LToken
    {
        public TokenType type;
        public Value value;
        public TextPosition position { get; set; }

        public LToken(TokenType _type, TextPosition pos)
        {
            type = _type;
            position = pos.Copy();
        }
        public LToken(TokenType _type, Value _value, TextPosition pos)
        {
            type = _type;
            value = _value;
            position = pos.Copy();
        }
        public LToken(TokenType _type)
        {
            type = _type;
        }
        public LToken(TokenType _type, Value _value)
        {
            type = _type;
            value = _value;
        }
        public LToken SetContext(Context _context)
        {
            Context c = new Context(_context.name, _context.parent, _context.parentEntry);
            if (value.context == null)
                c.varlist = _context.varlist;
            else
                c.varlist = value.context.varlist;
            value.SetContext(c);
            return this;
        }

        public LToken Copy()
        {
            return new LToken(type, value.Copy(), position.Copy());
        }

        public override string ToString()
        {
            if (value == null)
                return type.ToString();
            return $"{type}:{value.type}:{value}";
        }
    }

    
}

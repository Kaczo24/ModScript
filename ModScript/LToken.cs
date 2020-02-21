
namespace ModScript
{
    enum TokenType : long
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

        INC = (1L << 31),
        DEC = (1L << 32),

        EQUAL = (1 << 9),
        MOVL = (1L << 33), // 33

        ARROW = (1 << 20),
        RPAR = (1 << 4),
        LPAR = (1 << 5),
        RBRACK = (1 << 21),
        LBRACK = (1 << 22),
        LSQBR = (1 << 24),
        RSQBR = (1 << 25),

        VALUE = (1 << 6),

        KEYWORD = (1 << 10),
        IDENTIFIER = (1 << 11),

        RETURN = (1 << 28),
        BREAK = (1 << 29),
        CONTINUE = (1 << 30),

        COMMA = (1 << 23),  
        DOT = (1 << 27),
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
        public bool protomove = false;
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
        public LToken SetPM()
        {
            protomove = true;
            return this;
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

        public LToken Copy(bool b)
        {
            TextPosition p = null;
            Value v = null;
            if (position != null)
                p = position.Copy();
            if (value != null)
                if(b)
                    v = value.Copy(b);
                else
                    v = value.Copy(false);
            return new LToken(type, v, p);
        }

        public override string ToString()
        {
            if (value == null)
                return type.ToString();
            return $"{type}:{value.type}:{value}";
        }
    }

    
}

using System.Collections.Generic;

namespace ModScript
{
    class Parser
    {
        List<LToken> tokens;
        LToken current;
        public Error error;
        int index = -1;
        public PNode node;
        public Parser(List<LToken> _tokens)
        {
            tokens = _tokens;
            Next();
            ParseResult res = Statements();
            if (res.error != null)
                error = res.error;
            else
            {
                if (current.type != TokenType.EOF)
                    error = res.Failure(new InvalidSyntaxError(current.position)).error;
                node = res.node;
            }
        }

        LToken Next()
        {
            index++;
            if (index < tokens.Count)
                current = tokens[index];
            return current;
        }

        LToken Back(int n = 1)
        {
            index -= n;
            if (index >= 0)
                current = tokens[index];
            return current;
        }

        ParseResult Func_Def()
        {
            ParseResult res = new ParseResult();
            if (current.type != TokenType.KEYWORD || current.value.text != "function")
                return res.Failure(new InvalidSyntaxError(current.position, "Expected function keyword"));
            LToken FName = new LToken(TokenType.VALUE, null, current.position);
            res.Register(Next());
            if (current.type == TokenType.IDENTIFIER)
            {
                FName = new LToken(TokenType.VALUE, new Value(current.value.text), current.position);
                res.Register(Next());
                if (current.type != TokenType.LPAR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected '('"));
            }
            else
            {
                if (current.type != TokenType.LPAR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected '(' or an identifier"));
            }
            res.Register(Next());
            List<LToken> args = new List<LToken>();
            if (current.type == TokenType.IDENTIFIER)
            {
                args.Add(new LToken(TokenType.VALUE, new Value(current.value.text), current.position));
                res.Register(Next());
                while (current.type == TokenType.COMMA)
                {
                    res.Register(Next());
                    if (current.type != TokenType.IDENTIFIER)
                        return res.Failure(new InvalidSyntaxError(current.position, "Expected an identifier"));
                    args.Add(new LToken(TokenType.VALUE, new Value(current.value.text), current.position));
                    res.Register(Next());
                }
                if (current.type != TokenType.RPAR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ')' or ','"));
            }
            else if (current.type != TokenType.RPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));

            res.Register(Next());
            PNode ret = null;
            if (current.type == TokenType.ARROW)
            {
                res.Register(Next());
                ret = res.Register(expr());
                if (res.error != null)
                    return res;
            }
            else if (current.type == TokenType.LBRACK)
            {
                Next();
                ret = res.Register(Statements());
                if (res.error != null)
                    return res;
                if (current.type != TokenType.RBRACK)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected '}'"));
            }
            else return res.Failure(new InvalidSyntaxError(current.position, "Expected '=>' or '{'"));
            return res.Succes(PNode.GetFuncDef(FName, args, ret));
        }

        ParseResult list_expr()
        {
            ParseResult res = new ParseResult();
            LToken str = current;
            res.Register(Next());
            List<PNode> args = new List<PNode>();
            if (current.type == TokenType.RSQBR)
                res.Register(Next());
            else
            {
                args.Add(res.Register(expr()));
                if (res.error != null)
                    return res.Failure(new InvalidSyntaxError(res.error.position, res.error.message + " or expected ']'"));
                while (current.type == TokenType.COMMA)
                {
                    res.Register(Next());
                    args.Add(res.Register(expr()));
                    if (res.error != null)
                        return res;
                }
                if (current.type != TokenType.RSQBR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ',' or ']'"));
                res.Register(Next());
            }
            return res.Succes(new PNode("MakeList", args, str));
        }


        ParseResult atom()
        {
            ParseResult res = new ParseResult();
            LToken t = current;
            if (t.type == TokenType.LPAR)
            {
                res.Register(Next());
                PNode e = res.Register(expr());
                if (res.error != null)
                    return res;
                if (current.type == TokenType.RPAR)
                {
                    res.Register(Next());
                    return res.Succes(e);
                }
                else
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));

            }
            else if (t.type == TokenType.LSQBR)
            {
                PNode n = res.Register(list_expr());
                if (res.error != null)
                    return res;
                return res.Succes(n);
            }
            else if (t.type == TokenType.IDENTIFIER)
            {
                res.Register(Next());
                return res.Succes(new PNode("VarGet", t));
            }
            else if (t.type == TokenType.VALUE)
            {
                res.Register(Next());
                return res.Succes(new PNode(t));
            }
            else if (t.type == TokenType.KEYWORD && t.value.text == "function")
            {
                PNode fd = res.Register(Func_Def());
                if (res.error != null)
                    return res;
                return res.Succes(fd);
            }
            return res.Failure(new InvalidSyntaxError(current.position, "Expected number, identifier, plus, minus or parenthesis"));
        }

        ParseResult call(ParseResult toCall)
        {
            ParseResult res = new ParseResult();
            PNode at = res.Register(toCall);
            if (res.error != null)
                return res;

            if (current.type == TokenType.LPAR)
            {
                res.Register(Next());
                List<PNode> args = new List<PNode>();
                if (current.type == TokenType.RPAR)
                    res.Register(Next());
                else
                {
                    args.Add(res.Register(expr()));
                    if (res.error != null)
                        return res.Failure(new InvalidSyntaxError(res.error.position, res.error.message + " or expected ')'"));
                    while (current.type == TokenType.COMMA)
                    {
                        res.Register(Next());
                        args.Add(res.Register(expr()));
                        if (res.error != null)
                            return res;
                    }
                    if (current.type != TokenType.RPAR)
                        return res.Failure(new InvalidSyntaxError(current.position, "Expected ',' or ')'"));
                    res.Register(Next());
                }
                return call(res.Succes(PNode.GetCall("CallFunc", at, args)));
            }
            else if (current.type == TokenType.LSQBR)
            {
                res.Register(Next());
                if (current.type == TokenType.RSQBR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected int"));
                PNode n = res.Register(expr());
                if (current.type != TokenType.RSQBR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ']'"));
                res.Register(Next());
                res.isInnnerCall = true;
                return call(res.Succes(PNode.GetCall("GetInner", at, new List<PNode>() { n }), true));
            }
            else if(current.type == TokenType.DOT)
            {
                res.Register(Next());
                if (current.type != TokenType.IDENTIFIER)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected identifier"));
                LToken id = current;
                res.Register(Next());
                return res.Succes(new PNode("GetProperty", id, at));
            }
            return res.Succes(at, true);
        }

        ParseResult Power()
        {
            ParseResult res = new ParseResult();
            PNode left = res.Register(call(atom()));
            if (res.error != null)
                return res;
            bool b = true;
            while (TokenType.POW == current.type)
            {
                b = false;
                LToken opT = current;
                res.Register(Next());
                PNode r = res.Register(factor());
                if (res.error != null)
                    return res;
                left = PNode.GetBinOP(left, opT, r);
            }
            if (b)
                return res.Succes(left, true);
            return res.Succes(left);
        }

        ParseResult factor()
        {
            ParseResult res = new ParseResult();
            LToken t = current;
            if ((t.type & (TokenType.ADD | TokenType.SUB)) != 0)
            {
                res.Register(Next());
                PNode f = res.Register(factor());
                if (res.error != null)
                    return res;
                return res.Succes(new PNode("UnarOp", t, f));
            }
            return Power();
        }

        ParseResult term() => BinOP(factor, TokenType.MULT | TokenType.DIV | TokenType.MOD);

        ParseResult aryth_expr() => BinOP(term, TokenType.ADD | TokenType.SUB);

        ParseResult comp_expr()
        {
            ParseResult res = new ParseResult();
            LToken t = current;
            if (current.type == TokenType.NOT)
            {
                res.Register(Next());
                PNode f = res.Register(comp_expr());
                if (res.error != null)
                    return res;
                return res.Succes(new PNode("UnarOp", t, f));
            }
            return BinOP(aryth_expr,
                TokenType.EE |
                TokenType.NE |
                TokenType.GT |
                TokenType.GTE |
                TokenType.LT |
                TokenType.LTE);
        }

        ParseResult expr()
        {
            ParseResult res = new ParseResult();
            if (current.type == TokenType.KEYWORD && current.value.text == "let")
            {
                res.Register(Next());
                if (current.type != TokenType.IDENTIFIER)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected identifier"));

                LToken Vname = current;
                PNode exp = null;
                res.Register(Next());
                if (current.type == TokenType.EQUAL)
                {
                    res.Register(Next());
                    exp = res.Register(expr());
                    if (res.error != null)
                        return res;
                }
                else if (current.type == TokenType.NLINE)
                {
                    Back();
                    exp = new PNode(new LToken(TokenType.VALUE, Value.NULL, current.position));
                    Next();
                }
                else return res.Failure(new InvalidSyntaxError(current.position, "Expected '=' or ';'"));
                return res.Succes(new PNode("VarMake", Vname, exp));
            }
            if (current.type == TokenType.IDENTIFIER)
            {
                LToken Vname = current;
                res.Register(Next());
                if ((current.type & TokenType.EQUAL) != 0)
                {
                    TokenType t = current.type;
                    res.Register(Next());
                    PNode exp = res.Register(expr());
                    if (res.error != null)
                        return res;
                    if ((t & (TokenType.ADD | TokenType.SUB | TokenType.MULT | TokenType.DIV | TokenType.POW)) != 0)
                        exp = PNode.GetBinOP(new PNode("VarGet", Vname), new LToken(t ^ TokenType.EQUAL), exp);
                    return res.Succes(new PNode("VarAsign", Vname, exp));
                }
                res.Register(Back());
            }
            PNode node = res.Register(BinOP(comp_expr, TokenType.AND | TokenType.OR));
            if (res.error != null)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected, let, number, identifier, plus, minus or parenthesis"));
            if (res.isInnnerCall)
            {
                if ((current.type & TokenType.EQUAL) != 0)
                {
                    TokenType t = current.type;
                    res.Register(Next());
                    PNode exp = res.Register(expr());
                    if (res.error != null)
                        return res;
                    if ((t & (TokenType.ADD | TokenType.SUB | TokenType.MULT | TokenType.DIV | TokenType.POW)) != 0)
                        exp = PNode.GetBinOP(node, new LToken(t ^ TokenType.EQUAL), exp);

                    List<PNode> pns = new List<PNode>(node.PNodes);
                    pns.RemoveAt(0);
                    pns.Add(exp);
                    return res.Succes(PNode.GetCall("InnerAsign", node.PNodes[0], pns));
                }
            }
            return res.Succes(node);
        }

        public ParseResult body_statement()
        {
            ParseResult res = new ParseResult();
            if (current.type == TokenType.LBRACK)
            {
                Next();
                PNode stm = res.Register(Statements());
                if (res.error != null)
                    return res;
                if (current.type != TokenType.RBRACK)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected '}'"));
                return res.Succes(stm);
            }
            PNode st = res.Register(statement());
            if (res.error != null)
                return res;
            if (st.TYPE != "IF" && st.TYPE != "WHILE" && st.TYPE != "FOR")
            {
                if (current.type != TokenType.NLINE)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ';'"));
                Next();
            }
            return res.Succes(st);
        }
        public ParseResult if_expr()
        {
            ParseResult res = new ParseResult();
            LToken str = current;
            Next();
            if (current.type != TokenType.LPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '('"));
            Next();
            PNode test = res.Register(expr());
            if (res.error != null)
                return res;
            if (current.type != TokenType.RPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));
            Next();
            bool bracketed = current.type == TokenType.LBRACK;
            PNode body = res.Register(body_statement());
            if (res.error != null)
                return res;
            if (bracketed && current.type != TokenType.RBRACK)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '}'"));
            if (bracketed)
                Next();
            if (current.type == TokenType.KEYWORD && current.value.text == "else")
            {
                Next();
                PNode Ebody = res.Register(body_statement());
                if (res.error != null)
                    return res;
                return res.Succes(new PNode("IF", new List<PNode>() { test, body, Ebody }, str));
            }
            return res.Succes(new PNode("IF", new List<PNode>() { test, body }, str));
        }
        public ParseResult while_expr()
        {
            ParseResult res = new ParseResult();
            LToken str = current;
            Next();
            if (current.type != TokenType.LPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '('"));
            Next();
            PNode test = res.Register(expr());
            if (res.error != null)
                return res;
            if (current.type != TokenType.RPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));
            Next();
            bool bracketed = current.type == TokenType.LBRACK;
            PNode body = res.Register(body_statement());
            if (res.error != null)
                return res;
            if (bracketed && current.type != TokenType.RBRACK)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '}'"));
            if (bracketed)
                Next();
            return res.Succes(new PNode("WHILE", new List<PNode>() { test, body }, str));
        }

        public ParseResult for_expr()
        {
            ParseResult res = new ParseResult();
            LToken str = current;
            Next();
            if (current.type != TokenType.LPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '('"));
            Next();
            PNode sing = res.Register(expr());
            if (res.error != null)
                return res;
            if (current.type != TokenType.NLINE)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ';'"));
            Next();
            PNode test = res.Register(expr());
            if (res.error != null)
                return res;
            if (current.type != TokenType.NLINE)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ';'"));
            Next();
            PNode mult = res.Register(expr());
            if (res.error != null)
                return res;
            if (current.type != TokenType.RPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));
            Next();
            bool bracketed = current.type == TokenType.LBRACK;
            PNode body = res.Register(body_statement());
            if (res.error != null)
                return res;
            if (bracketed && current.type != TokenType.RBRACK)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '}'"));
            if (bracketed)
                Next();
            return res.Succes(new PNode("FOR", new List<PNode>() { sing, test, mult, body }, str));
        }

        public ParseResult statement()
        {
            ParseResult res = new ParseResult();
            if (current.type == TokenType.KEYWORD && current.value.text == "while")
            {
                PNode n = res.Register(while_expr());
                if (res.error != null)
                    return res;
                return res.Succes(n);
            }
            if (current.type == TokenType.KEYWORD && current.value.text == "if")
            {
                PNode n = res.Register(if_expr());
                if (res.error != null)
                    return res;
                return res.Succes(n);
            }
            if (current.type == TokenType.KEYWORD && current.value.text == "for")
            {
                PNode n = res.Register(for_expr());
                if (res.error != null)
                    return res;
                return res.Succes(n);
            }
            PNode exp = res.Register(expr());
            if (res.error != null)
                return res;
            return res.Succes(exp);
        }


        public ParseResult Statements()
        {
            ParseResult res = new ParseResult();
            List<PNode> PNodes = new List<PNode>();
            LToken str = current;
            while (true)
            {
                int b = index;
                PNode exp = res.TryRegister(statement());
                if(exp == null)
                {
                    Back(index - b);
                    break;
                }
                if (exp.TYPE != "IF" && exp.TYPE != "WHILE" && exp.TYPE != "FOR")
                {
                    if (current.type != TokenType.NLINE)
                        return res.Failure(new InvalidSyntaxError(current.position, "Expected ';'"));
                    Next();
                }
                PNodes.Add(exp);
            }
            return res.Succes(new PNode("Body", PNodes, str));
        }



        delegate ParseResult PRes();
        ParseResult BinOP(PRes func, TokenType type, PRes rop = null)
        {
            if (rop == null)
                rop = func;

            ParseResult res = new ParseResult();
            PNode left = res.Register(func());
            bool b = true;
            if (res.error != null)
                return res;
            while ((type & current.type) != 0 && (current.type & TokenType.EQUAL) == 0)
            {
                b = false;
                LToken opT = current;
                res.Register(Next());
                PNode r = res.Register(rop());
                if (res.error != null)
                    return res;
                left = PNode.GetBinOP(left, opT, r);
            }
            if(b)
                return res.Succes(left, true);
            return res.Succes(left);
        }
    }

    class ParseResult
    {
        public PNode node;
        public Error error;
        int advanceC = 0;
        public int toRevert = 0;
        public bool isInnnerCall = false;

        public PNode Register(ParseResult res)
        {
            advanceC += res.advanceC;
            isInnnerCall = res.isInnnerCall;
            if (res.error != null)
                error = res.error;
            return res.node;
        }

        public PNode TryRegister(ParseResult res)
        {
            if(res.error != null)
            {
                toRevert = advanceC;
                return null;
            }
            return Register(res);
        }

        public PNode Register(PNode node)
        { 
            return node;
        }
        public PNode Register(LToken t)
        {
            advanceC++;
            return null;
        }
        public ParseResult Succes(PNode _node)
        {
            isInnnerCall = false;
            node = _node;
            return this;
        }
        public ParseResult Succes(PNode _node, bool b)
        {
            node = _node;
            return this;
        }
        public ParseResult Failure(Error _error)
        {
            if (error == null || advanceC == 0)
                error = _error;
            return this;
        }
    }


    
}

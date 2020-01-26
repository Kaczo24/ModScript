using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            ParseResult res = expr();
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

        LToken Back()
        {
            index--;
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
            if(current.type == TokenType.IDENTIFIER)
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
            if(current.type == TokenType.IDENTIFIER)
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
            else if(current.type != TokenType.RPAR)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected ')'"));

            res.Register(Next());
           
            if (current.type != TokenType.ARROW)
                return res.Failure(new InvalidSyntaxError(current.position, "Expected '=>'"));
            res.Register(Next());
            PNode ret = res.Register(expr());
            if (res.error != null)
                return res;
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
            else if(t.type == TokenType.LSQBR)
            {
                PNode n = res.Register(list_expr());
                if (res.error != null)
                    return res;
                return res.Succes(n);
            }
            else if(t.type == TokenType.IDENTIFIER)
            {
                res.Register(Next());
                return res.Succes(new PNode("VarGet", t));
            }
            else if (t.type == TokenType.VALUE) //  ------------------------------------ ACHTUNG
            {
                res.Register(Next());
                return res.Succes(new PNode(t));
            }
            else if(t.type == TokenType.KEYWORD && t.value.text == "function")
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
            else if(current.type == TokenType.LSQBR)
            {
                res.Register(Next());
                if (current.type == TokenType.RSQBR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected int"));
                PNode n = res.Register(expr());
                if (current.type != TokenType.RSQBR)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected ']'"));
                res.Register(Next());
                res.isInnnerCall = true;
                return call(res.Succes(PNode.GetCall("GetInner", at, new List<PNode>() {n}), true));
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
            if(b)
                return res.Succes(left, true);
            return res.Succes(left);
        }

        ParseResult factor()
        {
            ParseResult res = new ParseResult();
            LToken t = current;
            if((t.type & (TokenType.ADD | TokenType.SUB)) != 0)
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
                TokenType.EE  |
                TokenType.NE  | 
                TokenType.GT  |
                TokenType.GTE |
                TokenType.LT  |
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
                res.Register(Next());
                if (current.type != TokenType.EQUAL)
                    return res.Failure(new InvalidSyntaxError(current.position, "Expected '='"));

                res.Register(Next());
                PNode exp = res.Register(expr());
                if (res.error != null) 
                    return res;

                return res.Succes(new PNode("VarMake", Vname, exp));
            }
            if(current.type == TokenType.IDENTIFIER)
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
        public bool isInnnerCall = false;

        public PNode Register(ParseResult res)
        {
            advanceC += res.advanceC;
            isInnnerCall = res.isInnnerCall;
            if (res.error != null)
                error = res.error;
            return res.node;
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

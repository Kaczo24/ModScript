using System;
using System.Collections.Generic;

namespace ModScript
{
    static class Interpreter
    {
        
        static public RTResult Visit(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken Ot;
            Function f;
            Value v;
            switch (node.TYPE)
            {
                case "VALUE":
                    return res.Succes(node.val.SetContext(context));
                case "VarAsign":
                case "PublicVarMake":
                case "PublicPrototype":
                case "VarMake":
                    Ot = res.Register(VisitVarAsign(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "VarGet":
                    f = context.GetFunction();
                    if (f != null)
                        if (f.InnerValues.ContainsKey(node.val.value.text))
                            return res.Succes(f.InnerValues[node.val.value.text]);
                    if (!context.varlist.ContainsKey(node.val.value.text))
                        return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is not defined", context));
                    return res.Succes(context.varlist[node.val.value.text]);
                case "BinOp":
                    Ot = res.Register(VisitBinOp(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "UnarOp":
                    Ot = res.Register(VisitUnarOp(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "PublicFuncDeff":
                    f = new Function(node.val, node.right, node.LTokens.ConvertAll(x => x.value.text), context);
                    Function CF = context.GetFunction();
                    f.InnerValues.parent = CF.InnerValues;
                    CF.InnerValues[node.val.value.text] = new LToken(TokenType.VALUE, new Value(f), node.val.position).SetContext(context); ;
                    return res.Succes(new LToken(TokenType.VALUE, new Value(f), node.val.position).SetContext(context));
                case "FuncDef":
                    f = new Function(node.val, node.right, node.LTokens.ConvertAll(x => x.value.text), context);
                    if (node.val.value != null)
                        context.varlist[node.val.value.text] = new LToken(TokenType.VALUE, new Value(f), node.val.position).SetContext(context);
                    return res.Succes(new LToken(TokenType.VALUE, new Value(f), node.val.position).SetContext(context));
                case "CallFunc":
                    Ot = res.Register(VisitCall(node, context));
                    if (res.error != null)
                        return res;
                    Ot.type = TokenType.VALUE;
                    return res.Succes(Ot);
                case "MakeList":
                    List<Value> values = new List<Value>();
                    foreach (PNode n in node.PNodes)
                    {
                        values.Add(res.Register(Visit(n, context)).value);
                        if (res.error != null)
                            return res;
                    }
                    return res.Succes(new LToken(TokenType.VALUE, new Value(values), node.val.position).SetContext(context));
                case "Prototype":
                    Ot = res.Register(Visit(node.PNodes[0], context));
                    if (res.error != null)
                        return res;
                    if (Ot.value.type != "FUNC")
                        return res.Failure(new RuntimeError(Ot.position, "Value to prototype into has to be a function.", context));
                    if (Compiler.forbidden.Contains(Ot.value.function.name.value.text))
                        return res.Failure(new RuntimeError(node.val.position, $"{Ot.value.function.name.value.text} is a predefined, unmutable variable", context));
                    Ot.value.function.body.PNodes.Add(new PNode("PublicPrototype", node.val, node.PNodes[1]));
                    return res.Succes(Ot);
                case "GetProperty":
                    Ot = res.Register(Visit(node.right, context));
                    if (res.error != null)
                        return res;
                    v = Ot.value.GetProperty(node.val.value.text);
                    Ot = new LToken(TokenType.VALUE, v, Ot.position);
                    return res.Succes(Ot.SetContext(context));
                case "PropertyAsign":
                    Ot = res.Register(Visit(node.PNodes[1], context));
                    v = res.Register(Visit(node.PNodes[0].right, context)).value.SetProperty(node.PNodes[0].val.value.text, Ot);
                    Ot = new LToken(TokenType.VALUE, v, Ot.position);
                    return res.Succes(Ot.SetContext(context));
                case "CallProperty":
                    LToken toCall = res.Register(Visit(node.PNodes[0], context));
                    if (res.error != null)
                        return res;
                    List<LToken> args = new List<LToken>();
                    for (int n = 1; n < node.PNodes.Count; n++)
                    {
                        args.Add(res.Register(Visit(node.PNodes[n], context)));
                        if (res.error != null)
                            return res;
                    }
                    LToken t = res.Register(toCall.value.CallProperty(node.val.value.text, args, context, toCall.position));
                    if (res.error != null)
                        return res;
                    return res.Succes(t.SetContext(context));
                case "GetInner":
                    Ot = res.Register(VisitGetInner(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "InnerAsign":
                    Ot = res.Register(VisitSetInner(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "IF":
                    {
                        Context ctx = BracketContext(context);
                        Ot = res.Register(Visit(node.PNodes[0], ctx));
                        if (res.error != null)
                            return res;
                        if (Ot.value.boolean)
                        {
                            Ot = res.Register(Visit(node.PNodes[1], ctx));
                            if (res.error != null)
                                return res;
                            if ((Ot.type & (TokenType.BREAK | TokenType.CONTINUE | TokenType.RETURN)) != 0)
                                return res.Succes(Ot);
                        }
                        else if (node.PNodes.Count == 3)
                        {
                            Ot = res.Register(Visit(node.PNodes[2], ctx));
                            if (res.error != null)
                                return res;
                            if ((Ot.type & (TokenType.BREAK | TokenType.CONTINUE | TokenType.RETURN)) != 0)
                                return res.Succes(Ot);
                        }
                    }
                    return res.Succes(new LToken(TokenType.VALUE, Value.NULL, node.val.position));
                case "WHILE":
                    {
                        Context ctx = BracketContext(context);
                        Ot = res.Register(Visit(node.PNodes[0], ctx));
                        if (res.error != null)
                            return res;
                        while (Ot.value.boolean)
                        {
                            Ot = res.Register(Visit(node.PNodes[1], ctx));
                            if (res.error != null)
                                return res;
                            if (Ot.type == TokenType.BREAK)
                                break;
                            if (Ot.type == TokenType.RETURN)
                                return res.Succes(Ot);
                            ctx = BracketContext(context);
                            Ot = res.Register(Visit(node.PNodes[0], ctx));
                            if (res.error != null)
                                return res;
                        }
                    }
                    return res.Succes(new LToken(TokenType.VALUE, Value.NULL, node.val.position));
                case "FOR":
                    {
                        Context CON = BracketContext(context);
                        res.Register(Visit(node.PNodes[0], CON));
                        Ot = res.Register(Visit(node.PNodes[1], CON));
                        if (res.error != null)
                            return res;
                        Context ctx = BracketContext(CON);
                        while (Ot.value.boolean)
                        {
                            Ot = res.Register(Visit(node.PNodes[3], ctx));
                            if (res.error != null)
                                return res;
                            if (Ot.type == TokenType.BREAK)
                                break;
                            if (Ot.type == TokenType.RETURN)
                                return res.Succes(Ot);
                            ctx = BracketContext(CON);
                            res.Register(Visit(node.PNodes[2], ctx));
                            if (res.error != null)
                                return res;
                            Ot = res.Register(Visit(node.PNodes[1], ctx));
                            if (res.error != null)
                                return res;
                        }
                        return res.Succes(new LToken(TokenType.VALUE, Value.NULL, node.val.position));
                    }
                case "MultExpr":
                    return VisitBody(node, BracketContext(context));
                case "Body":
                    return VisitBody(node, context);
                case "NEW":
                    Ot = res.Register(Visit(node.right, context));
                    if (res.error != null)
                        return res;
                    if (Ot.value.type != "FUNC")
                        return res.Failure(new RuntimeError(node.val.position, "'new' copy can be only aplied to functions.", context));
                    return res.Succes(Ot.Copy(false));
                case "RETURN":
                    Ot = res.Register(Visit(node.right, context));
                    if (res.error != null)
                        return res;
                    Ot.type = TokenType.RETURN;
                    return res.Succes(Ot);
                case "BREAK":
                    return res.Succes(new LToken(TokenType.BREAK, Value.NULL, node.val.position));
                case "CONTINUE":
                    return res.Succes(new LToken(TokenType.CONTINUE, Value.NULL, node.val.position));
                case "SUPER":
                    f = context.GetFunction();
                    if (f == null)
                        return res.Failure(new RuntimeError(node.val.position, "Super can only be called inside a function.", context));
                    Ot = res.Register(Visit(node.right, context));
                    if (res.error != null)
                        return res;
                    if (Ot.value.type != "FUNC")
                        return res.Failure(new RuntimeError(Ot.position, "Function can only accept another function as a Super.", context));
                    f.InnerValues.Merge(Ot.value.function.InnerValues, f);
                    return res.Succes(new LToken(TokenType.VALUE, Value.NULL, node.val.position));
                case "RUN":
                    Ot = res.Register(Visit(node.right, context));
                    if (res.error != null)
                        return res;
                    if (Ot.value.type != "STRING")
                        return res.Failure(new RuntimeError(node.val.position, "Run statement requires a string argument.", context));
                    if (!System.IO.File.Exists(Ot.value.text))
                        return res.Failure(new RuntimeError(Ot.position, $"File {Ot.value.text} does not exist.", context));
                    Ot = res.Register(Compiler.Run(System.IO.File.ReadAllText(Ot.value.text), new System.IO.FileInfo(Ot.value.text).Name));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                default:
                    throw new Exception("Visit not defined");
            }
        }

        static RTResult VisitBody(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken Ot;
            List<PNode> PNodes = new List<PNode>(node.PNodes);
            foreach (PNode n in PNodes.FindAll(x => 
                x.TYPE == "FuncDef" ||
                x.TYPE == "PublicPrototype" ||
                x.TYPE == "PublicFuncDeff" ||
                x.TYPE == "SUPER" ||
                x.TYPE == "RUN" ||
                x.TYPE == "Prototype" ||
                x.isMakeValid()))
            {
                Ot = res.Register(Visit(n, context));
                if (res.error != null)
                    return res;
                if ((Ot.type & (TokenType.BREAK | TokenType.CONTINUE | TokenType.RETURN)) != 0)
                    return res.Succes(Ot);
                PNodes.Remove(n);
            }
            foreach (PNode n in PNodes)
            {
                Ot = res.Register(Visit(n, context));
                if (res.error != null)
                    return res;
                if ((Ot.type & (TokenType.BREAK | TokenType.CONTINUE | TokenType.RETURN)) != 0)
                    return res.Succes(Ot);
            }
            return res.Succes(new LToken(TokenType.VALUE, Value.NULL, node.val.position));
        }

        static RTResult VisitSetInner(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken toCall = res.Register(Visit(node.PNodes[0], context));
            if (res.error != null)
                return res;
            if (toCall.value.type == "LIST")
            {
                LToken v = res.Register(Visit(node.PNodes[1], context));
                if (res.error != null)
                    return res;
                if (v.value.type != "INT")
                    return res.Failure(new RuntimeError(v.position, "Element argument has to be an integer.", context));
                LToken exp = res.Register(Visit(node.PNodes[2], context));
                if (res.error != null)
                    return res;
                if(v.value.integer < 0 || v.value.integer >= toCall.value.values.Count)
                    return res.Failure(new RuntimeError(v.position, $"Element argument has to be between 0 and the length of the list ({toCall.value.values.Count}).", context));

                toCall.value.values[v.value.integer] = exp.value;
                return res.Succes(exp.SetContext(context));
            }
            return res.Failure(new RuntimeError(toCall.position, "List expected", context));
        }

        static RTResult VisitGetInner(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken toCall = res.Register(Visit(node.PNodes[0], context));
            if (res.error != null)
                return res;
            if (toCall.value.type == "LIST")
            {
                LToken v = res.Register(Visit(node.PNodes[1], context));
                if (res.error != null)
                    return res;
                if(v.value.type != "INT")
                    return res.Failure(new RuntimeError(v.position, "Element argument has to be an integer.", context));
                if(v.value.integer < 0)
                    return res.Failure(new RuntimeError(v.position, "Element argument cannot be less then 0.", context));
                if (v.value.integer < toCall.value.values.Count && v.value.integer >= 0)
                    return res.Succes(new LToken(TokenType.VALUE, toCall.value.values[v.value.integer], toCall.position).SetContext(context));
                return res.Succes(new LToken(TokenType.VALUE, Value.NULL, toCall.position).SetContext(context));
            }
            else if(toCall.value.type == "STRING")
            {
                LToken v = res.Register(Visit(node.PNodes[1], context));
                if (res.error != null)
                    return res;
                if (v.value.type != "INT")
                    return res.Failure(new RuntimeError(v.position, "Element argument has to be an integer.", context));
                if (v.value.integer < 0)
                    return res.Failure(new RuntimeError(v.position, "Element argument cannot be less then 0.", context));
                if (v.value.integer < toCall.value.text.Length && v.value.integer >= 0)
                    return res.Succes(new LToken(TokenType.VALUE, new Value(toCall.value.text[v.value.integer].ToString()), toCall.position).SetContext(context));
                return res.Succes(new LToken(TokenType.VALUE, Value.NULL, toCall.position).SetContext(context));
            }
            return res.Failure(new RuntimeError(toCall.position, "List expected", context));
        }

        static RTResult VisitCall(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken toCall = res.Register(Visit(node.PNodes[0], context));
            if (res.error != null)
                return res;
            if (toCall.value.function == null)
                return  res.Failure(new RuntimeError(node.PNodes[0].val.position, $"{node.PNodes[0].val.value.text} is not a function.", context));
            List<LToken> args = new List<LToken>();
            for (int n = 1; n < node.PNodes.Count; n++)
            {
                args.Add(res.Register(Visit(node.PNodes[n], context)));
                if (res.error != null)
                    return res;
            }
            LToken t = res.Register(toCall.value.function.Execute(args, context, toCall.position));
            if (res.error != null)
                return res;
            return res.Succes(t.SetContext(context));  
        }

        static RTResult VisitVarAsign(PNode node, Context context)
        {
            RTResult res = new RTResult();
            if (Compiler.forbidden.Contains(node.val.value.text))
                return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is a predefined, unmutable variable", context));
            if (node.TYPE == "PublicVarMake" || node.TYPE == "PublicPrototype")
                if (context.GetFunction() == null)
                    return res.Failure(new RuntimeError(node.val.position, "Public variables have to be declared in an object.", context));
            if (node.TYPE == "VarAsign")
            {
                if (context.GetFunction() == null)
                {
                    if (!context.varlist.ContainsKey(node.val.value.text))
                        return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is not Defined", context));
                }
                else if (!context.varlist.ContainsKey(node.val.value.text) && !context.GetFunction().InnerValues.ContainsKey(node.val.value.text))
                    return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is not Defined", context));
            }
            else if (node.TYPE == "PublicVarMake")
            {
                if (context.GetFunction().InnerValues.ContainsKey(node.val.value.text))
                    return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is already Defined", context));
            }
            else if (node.TYPE != "PublicPrototype")
                if (context.varlist.ContainsKey(node.val.value.text))
                    return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is already Defined", context));

            LToken n = res.Register(Visit(node.right, context));
            if (res.error != null)
                return res;
            LToken Val = new LToken(TokenType.VALUE, n.value, node.val.position).SetContext(n.value.context);
            Function f = context.GetFunction();
            if (node.TYPE == "PublicVarMake" || node.TYPE == "PublicPrototype")
            {
                if (Val.value.type == "FUNC")
                    Val.value.function.InnerValues.parent = f.InnerValues;
                f.InnerValues[node.val.value.text] = Val;
            }
            else if (f != null)
            {
                if (f.InnerValues.ContainsKey(node.val.value.text))
                {
                    if (Val.value.type == "FUNC")
                        Val.value.function.InnerValues.parent = f.InnerValues;
                    f.InnerValues[node.val.value.text] = Val;
                }
                else
                    context.varlist[node.val.value.text] = Val;
            }
            else
                context.varlist[node.val.value.text] = Val;
            return res.Succes(Val.SetContext(context));   
        }

        static RTResult VisitBinOp(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken r = res.Register(Visit(node.right, context));
            if (res.error != null)
                return res;
            LToken l = res.Register(Visit(node.left, context));
            if (res.error != null)
                return res;

            switch(node.val.type)
            {
                case TokenType.EE:
                    return res.Succes(new LToken(TokenType.VALUE, new Value(l.value == r.value), l.position).SetContext(context));
                case TokenType.NE:
                    return res.Succes(new LToken(TokenType.VALUE, new Value(l.value != r.value), l.position).SetContext(context));
            }

            if (l.value.isNumber && r.value.isNumber)
                switch (node.val.type)
                {
                    case TokenType.ADD:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number + r.value.number), l.position).SetContext(l.value.context));
                    case TokenType.SUB:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number - r.value.number), l.position).SetContext(l.value.context));
                    case TokenType.MULT:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number * r.value.number), l.position).SetContext(l.value.context));
                    case TokenType.DIV:
                        if (r.value.number == 0)
                            res.Failure(new RuntimeError(node.val.position, "Division by zero error", context));

                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number / r.value.number), l.position).SetContext(l.value.context));
                    case TokenType.POW:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(Math.Pow(l.value.number, r.value.number)), l.position).SetContext(l.value.context));
                    case TokenType.MOD:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number % r.value.number), l.position).SetContext(l.value.context));
                    case TokenType.GT:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number > r.value.number), l.position).SetContext(context));
                    case TokenType.LT:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number < r.value.number), l.position).SetContext(context));
                    case TokenType.GTE:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number >= r.value.number), l.position).SetContext(context));
                    case TokenType.LTE:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number <= r.value.number), l.position).SetContext(context));

                }
            else if (l.value.type == "BOOLEAN" && r.value.type == "BOOLEAN")
                switch (node.val.type)
                {
                    case TokenType.AND:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.boolean && r.value.boolean), l.position).SetContext(l.value.context));
                    case TokenType.OR:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.boolean || r.value.boolean), l.position).SetContext(l.value.context));
                }
            else if (l.value.type == "LIST" && r.value.type == "LIST")
                switch (node.val.type)
                {
                    case TokenType.ADD:
                        List<Value> Vs = new List<Value>(l.value.values.ToArray());
                        Vs.AddRange(r.value.values);
                        return res.Succes(new LToken(TokenType.VALUE, new Value(Vs), l.position).SetContext(l.value.context));
                }
            else if(l.value.type == "STRING" || r.value.type == "STRING")
                switch (node.val.type)
                {
                    case TokenType.ADD:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.ToString() + r.value.ToString()), l.position).SetContext(l.value.context));
                }


            return res.Failure(new RuntimeError(node.val.position, $"Invalid operation for {l.value.type} and {r.value.type}.", context));
        }

        static RTResult VisitUnarOp(PNode node, Context context)
        {
            RTResult res = new RTResult();
            if ((node.val.type & (TokenType.INC | TokenType.DEC)) != 0)
            {
                LToken Out = res.Register(Visit(node.PNodes[0], context));
                if (res.error != null)
                    return res;
                Out = new LToken(TokenType.VALUE, Out.value, Out.position);
                res.Register(Visit(node.PNodes[1], context));
                if (res.error != null)
                    return res;
                return res.Succes(Out);
            }
            LToken n = res.Register(Visit(node.right, context));
            if (res.error != null)
                return res;

            if (node.val.type == TokenType.SUB)
                if (n.value.isNumber)
                    return res.Succes(new LToken(TokenType.VALUE, new Value(-n.value.number), n.position).SetContext(n.value.context));
            if (node.val.type == TokenType.NOT)
                if (n.value.type == "BOOLEAN")
                    return res.Succes(new LToken(TokenType.VALUE, new Value(!n.value.boolean), n.position).SetContext(n.value.context));
                else
                    return res.Failure(new RuntimeError(n.position, "Expected boolean", context));
            return res.Succes(n);
        }

        static Context BracketContext(Context context)
        {
            Context ctx = context.Copy();
            ctx.varlist = new VarList(context.varlist);
            return ctx;
        }
    }

    class RTResult
    {
        public LToken value;
        public Error error;

        public LToken Register(RTResult res)
        {
            if (res.error != null)
                error = res.error;
            return res.value;
        }

        public RTResult Succes(LToken _value)
        {
            value = _value;
            return this;
        }

        public RTResult Failure(Error _error)
        {
            error = _error;
            return this;
        }
    }

    class Context
    {
        public string name;
        public Context parent;
        public TextPosition parentEntry;
        public VarList varlist = null;
        public Function lastIn = null;
        public Context(string _name, Context _parent = null, TextPosition _parentEntry = null)
        {
            name = _name;
            parent = _parent;
            parentEntry = _parentEntry;
        }

        public Function GetFunction()
        {
            if (parent != null && lastIn == null)
                return parent.GetFunction();
            return lastIn;
        }

        public Context Copy()
        {
            Context c;
            if (parent != null)
                c = new Context(name, parent, parentEntry);
            else
                c = new Context(name);
            if (varlist != null)
                c.varlist = varlist.Copy();
            return c;
        }
    }

    class VarList : Dictionary<string, LToken>
    {
        public VarList parent;
        public VarList(VarList _parent) : base()
        {
            parent = _parent;
        }

        public void Merge(VarList vl, Function f)
        {
            AddRange(vl, f);
            if(vl.parent != null)
            {
                if (parent == null)
                    parent = new VarList(null);
               parent.Merge(vl.parent, f);
            }
        }

        public void AddRange(VarList vl, Function f)
        {
            foreach (string k in vl.Keys)
                if(!ContainsKey(k))
                {
                    if (vl[k].value.type == "FUNC")
                        vl[k].value.function.InnerValues.parent = f.InnerValues;
                    base[k] = vl[k];
                }
        }

        public new bool ContainsKey(string s)
        {
            if (base.ContainsKey(s))
                return true;
            if (parent != null)
                return parent.ContainsKey(s);
            return false;
        }

        public new LToken this[string s]
        {
            get
            {
                if (base.ContainsKey(s))
                    return base[s];
                else if (parent != null)
                    return parent[s];
                return null;
            }
            set
            {
                if (parent != null)
                {
                    if (base.ContainsKey(s))
                        base[s] = value;
                    else if (parent.ContainsKey(s))
                        parent[s] = value;
                    else
                        base[s] = value;
                }
                else
                    base[s] = value;
            }
        }

        public VarList Copy()
        {
            VarList v;
            if (parent != null)
                v = new VarList(parent.Copy());
            else

                v = new VarList(parent);
            foreach (string s in Keys)
                v[s] = this[s];
            return v;
        }
    }



}

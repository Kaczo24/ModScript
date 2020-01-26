﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModScript
{
    static class Interpreter
    {

        static public RTResult Visit(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken Ot;
            switch (node.TYPE)
            {
                case "VALUE":
                    return res.Succes(node.val.SetContext(context));
                case "VarAsign":
                case "VarMake":
                    Ot = res.Register(VisitVarAsign(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "VarGet":
                    if (!context.varlist.ContainsKey(node.val.value.text))
                        return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is not defined", context));
                    return res.Succes(context.varlist[node.val.value.text].SetContext(context));
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
                case "FuncDef":
                    {
                        Function f = new Function(node.val, node.right, node.LTokens.ConvertAll(x => x.value.text));
                        if (node.val.value != null)
                            context.varlist[node.val.value.text] = new LToken(TokenType.VALUE, new Value(f)).SetContext(context);
                        return res.Succes(new LToken(TokenType.VALUE, new Value(f)).SetContext(context));
                    }
                case "CallFunc":
                    Ot = res.Register(VisitCall(node, context));
                    if (res.error != null)
                        return res;
                    return res.Succes(Ot);
                case "MakeList":
                    List<Value> values = new List<Value>();
                    foreach (PNode n in node.PNodes)
                    {
                        values.Add(res.Register(Visit(n, context)).value);
                        if (res.error != null)
                            return res;
                    }
                    return res.Succes(new LToken(TokenType.VALUE, new Value(values), node.val.position));
                default:
                    throw new Exception("Visit not defined");
            }
        }

        static RTResult VisitCall(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken toCall = res.Register(Visit(node.PNodes[0], context));
            if (res.error != null)
                return res;
            if (toCall.value.function == null)
                return res.Failure(new RuntimeError(node.PNodes[0].val.position, $"{node.PNodes[0].val.value.text} is not a function."));
            List<LToken> args = new List<LToken>();
            for (int n = 1; n < node.PNodes.Count; n++)
            {
                args.Add(res.Register(Visit(node.PNodes[n], context)));
                if (res.error != null)
                    return res;
            }
            LToken t = res.Register(toCall.value.function.Execute(args, context, node.PNodes[0].val.position));
            if (res.error != null)
                return res;
            return res.Succes(t);
        }

        static RTResult VisitVarAsign(PNode node, Context context)
        {
            RTResult res = new RTResult();
            if (node.TYPE == "VarAsign")
            {
                if (!context.varlist.ContainsKey(node.val.value.text))
                    return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is not Defined", context));
            }
            else if (context.varlist.ContainsKey(node.val.value.text))
                return res.Failure(new RuntimeError(node.val.position, $"{node.val.value.text} is already Defined", context));

            LToken n = res.Register(Visit(node.right, context));
            if (res.error != null)
                return res;
            LToken Val = new LToken(TokenType.VALUE, n.value, node.val.position).SetContext(context);
            context.varlist[node.val.value.text] = Val;
            return res.Succes(Val);
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
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number + r.value.number), l.position).SetContext(l.context));
                    case TokenType.SUB:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number - r.value.number), l.position).SetContext(l.context));
                    case TokenType.MULT:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number * r.value.number), l.position).SetContext(l.context));
                    case TokenType.DIV:
                        if (r.value.number == 0)
                            res.Failure(new RuntimeError(node.val.position, "Division by zero error"));

                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number / r.value.number), l.position).SetContext(l.context));
                    case TokenType.POW:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(Math.Pow(l.value.number, r.value.number)), l.position).SetContext(l.context));
                    case TokenType.MOD:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.number % r.value.number), l.position).SetContext(l.context));
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
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.boolean && r.value.boolean), l.position).SetContext(l.context));
                    case TokenType.OR:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.boolean || r.value.boolean), l.position).SetContext(l.context));
                }
            else if (l.value.type == "LIST" && r.value.type == "LIST")
                switch (node.val.type)
                {
                    case TokenType.ADD:
                        List<Value> Vs = new List<Value>(l.value.values.ToArray());
                        Vs.AddRange(r.value.values);
                        return res.Succes(new LToken(TokenType.VALUE, new Value(Vs), l.position).SetContext(l.context));
                }
            else if(l.value.type == "STRING" || r.value.type == "STRING")
                switch (node.val.type)
                {
                    case TokenType.ADD:
                        return res.Succes(new LToken(TokenType.VALUE, new Value(l.value.ToString() + r.value.ToString()), l.position).SetContext(l.context));
                }


            return res.Failure(new RuntimeError(node.val.position, $"Invalid operation for {l.value.type} and {r.value.type}.", context));
        }

        static RTResult VisitUnarOp(PNode node, Context context)
        {
            RTResult res = new RTResult();
            LToken n = res.Register(Visit(node.right, context));
            if (res.error != null)
                return res;

            if (node.val.type == TokenType.SUB)
                if (n.value.isNumber)
                    return res.Succes(new LToken(TokenType.VALUE, new Value(-n.value.number)).SetContext(n.context));
            if (node.val.type == TokenType.NOT)
                if (n.value.type == "BOOLEAN")
                    return res.Succes(new LToken(TokenType.VALUE, new Value(!n.value.boolean)).SetContext(n.context));
                else
                    return res.Failure(new RuntimeError(n.position, "Expected boolean", context));
            return res.Succes(n);
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

        public Context(string _name, Context _parent = null, TextPosition _parentEntry = null)
        {
            name = _name;
            parent = _parent;
            parentEntry = _parentEntry;
        }
    }

    class VarList : Dictionary<string, LToken>
    {
        public VarList parent;
        public VarList(VarList _parent) : base()
        {
            parent = _parent;
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
                if (ContainsKey(s))
                    return base[s];
                else if (parent != null)
                    return parent[s];
                return null;
            }
            set
            {
                base[s] = value;
            }
        }
    }



}
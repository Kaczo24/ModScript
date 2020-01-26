using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModScript
{
    class PNode
    {
        public LToken val;
        public PNode left, right;
        public string TYPE;
        public List<PNode> PNodes;
        public List<LToken> LTokens;
        public PNode(LToken _val)
        {
            TYPE = _val.type.ToString();
            val = _val;
        }
        public PNode(PNode _left, LToken _opToken, PNode _right)
        {
            TYPE = "BinOp";
            left = _left;
            val = _opToken;
            right = _right;
        }
        public PNode(string _TYPE, LToken _opToken, PNode node)
        {
            TYPE = _TYPE;
            right = node;
            val = _opToken;
        }
        public PNode(string _TYPE, LToken _opToken)
        {
            TYPE = _TYPE;
            val = _opToken;
        }
        public PNode(string _TYPE, List<PNode> _PNodes, LToken pos)
        {
            TYPE = _TYPE;
            PNodes = _PNodes;
            val = pos;
        }
        public PNode(LToken fName, List<LToken> args, PNode _node)
        {
            TYPE = "FuncDef";
            val = fName;
            LTokens = args;
            right = _node;
        }
        public PNode(PNode fName, List<PNode> args)
        {
            TYPE = "CallFunc";
            PNodes = args;
            PNodes.Insert(0, fName);
        }
        public override string ToString()
        {
            if (TYPE == "BinOp")
                return $"({left}, {val.type}, {right})";
            if (TYPE == "UnarOp")
                return $"({val.type}, {right})";
            if (TYPE == "VarAsign")
                return $"({val.value}, EQUALS, {right})";
            if (TYPE == "VarGet")
                return $"(GET:{val.value}";
            return $"({TYPE}:{val})";
        }
    }
}

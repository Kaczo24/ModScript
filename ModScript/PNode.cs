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
        PNode() { }
        public PNode(LToken _val)
        {
            TYPE = _val.type.ToString();
            val = _val;
        }
        public static PNode GetBinOP(PNode _left, LToken _opToken, PNode _right)
        {
            PNode n = new PNode();
            n.TYPE = "BinOp";
            n.left = _left;
            n.val = _opToken;
            n.right = _right;
            return n;
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
        public static PNode GetFuncDef(LToken fName, List<LToken> args, PNode _node)
        {
            PNode n = new PNode();
            n.TYPE = "FuncDef";
            n.val = fName;
            n.LTokens = args;
            n.right = _node;
            return n;
        }
        public static PNode GetCall(string _TYPE, PNode fName, List<PNode> args)
        {
            PNode n = new PNode();
            n.TYPE = _TYPE;
            n.PNodes = args;
            n.PNodes.Insert(0, fName);
            return n;
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

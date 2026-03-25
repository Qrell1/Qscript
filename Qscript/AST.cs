using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public class Instruct
    {
        public Node root;
        public Instruct(Node root)
        {
            this.root = root;
        }

        public Node getRootNode()
        {
            return root;
        }
    }

    public class ProgramNode : CommonNode
    {
        public List<CommonNode> Constans = new List<CommonNode>();
        public List<string> inlineNames = new List<string>();

        public Dictionary<string, CommonNode> resualtFunc = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> typesArgsFunc = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();
        public List<string> qsFunction = new List<string>();
        public Dictionary<string, CommonNode> consts = new Dictionary<string, CommonNode>();

        public Dictionary<string, CommonNode> declarotivePatternsFunctions = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> declarotivePatternsStruct = new Dictionary<string, CommonNode>();

        public Dictionary<string, List<CommonNode>> classMethods = new Dictionary<string, List<CommonNode>>();
        public Dictionary<string, List<CommonNode>> classVars = new Dictionary<string, List<CommonNode>>();
        public Dictionary<string, List<CommonNode>> classConstructors = new Dictionary<string, List<CommonNode>>();

        public Dictionary<string, List<CommonNode>> declarotiveClassMethods = new Dictionary<string, List<CommonNode>>();
        public Dictionary<string, List<CommonNode>> declarotiveClassVars = new Dictionary<string, List<CommonNode>>();
        public Dictionary<string, List<CommonNode>> declarotiveClassConstructors = new Dictionary<string, List<CommonNode>>();

        public List<string> declarativeClassNames = new List<string>();
        public List<string> declarotiveNames = new List<string>();

        public Dictionary<string, Dictionary<string, CommonNode>> structs = new Dictionary<string, Dictionary<string, CommonNode>>();

        public Dictionary<string, List<CommonNode>> functionOver = new Dictionary<string, List<CommonNode>>();

        public List<CommonNode> sectionNodes = new List<CommonNode>();

        public Dictionary<string, string> externLibrarys = new Dictionary<string, string>();
        public Dictionary<string, List<string>> externFuncs = new Dictionary<string, List<string>>();
        public ProgramNode(string type, Token token) : base(type, token)
        {}
    }

    /// <summary>
    /// Простой класс для выражения всех узлов AST
    /// </summary>
    public class CommonNode
    {
        public string type;
        public Token token;
        public List<CommonNode> childs = new List<CommonNode>();//Dictionary<string, CommonNode> childs;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public CommonNode(string type, Token token)
        {
            this.type = type;
            this.token = token;
        }
    }

    // НУ типо ноды
    public abstract class Node { }
    class AsmInsert : Node
    {
        public Token token;
        public AsmInsert(Token _token)
        {
            token = _token;
        }
    }

    class NumberNode : Node
    {
        public Token token;

        public NumberNode(Token token)
        {
            this.token = token;
        }
    }

    class VarNode : Node
    {
        public Token token;

        public VarNode(Token token)
        {
            this.token = token;
        }
    }

    class BinOpNode : Node
    {
        public Token op;
        public Node left;
        public Node right;

        public BinOpNode(Token op, Node left, Node right)
        {
            this.op = op;
            this.left = left;
            this.right = right;
        }
    }

    class UnarOpNode : Node
    {
        public Token op;
        public Node operand;

        public UnarOpNode(Token op, Node operand)
        {
            this.op = op;
            this.operand = operand;
        }
    }

}

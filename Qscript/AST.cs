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
        public List<string> asmInlineNames = new List<string>();

        public Dictionary<string, CommonNode> resualtFunc = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> typesArgsFunc = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();
        public List<string> qsFunction = new List<string>();
        public Dictionary<string, CommonNode> consts = new Dictionary<string, CommonNode>();

        public List<CommonNode> lambdaQueue = new List<CommonNode>();

        public Dictionary<string, CommonNode> parentsStructs = new Dictionary<string, CommonNode>();

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

        public Dictionary<string, string> ClassesInheritances = new Dictionary<string, string>();

        public Dictionary<string, Dictionary<string, CommonNode>> structs = new Dictionary<string, Dictionary<string, CommonNode>>();
        public List<string> externStructs = new List<string>();

        public Dictionary<string, List<CommonNode>> functionOver = new Dictionary<string, List<CommonNode>>();
        public Dictionary<(string, CommonNode, CommonNode), string> operatorFunctions = new Dictionary<(string, CommonNode, CommonNode), string>();
        public List<CommonNode> sectionNodes = new List<CommonNode>();

        public Dictionary<string, string> externLibrarys = new Dictionary<string, string>();
        public Dictionary<string, List<string>> externFuncs = new Dictionary<string, List<string>>();
        public List<string> functionFromPtr = new List<string>();
        public ProgramNode(NT type, Token token) : base(type, token)
        {}
    }

    /// <summary>
    /// Простой класс для выражения всех узлов AST
    /// </summary>
    public class CommonNode
    {
        public NT type;
        public Token token;
        public List<CommonNode> childs = new List<CommonNode>();//Dictionary<string, CommonNode> childs;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public CommonNode(NT type, Token token)
        {
            this.type = type;
            this.token = token;
        }
        public CommonNode(TT type, Token token)
        {
            this.type = (NT)type;
            this.token = token;
        }
        public CommonNode(Token token)
        {
            this.type = NT.NULL;
            this.token = token;
        }

        public bool _equals(CommonNode right)
        {
            if (ReferenceEquals(right, null)) return false;
            if (token.value == right.token.value && type == right.type) return true;
            return false;
        }
        /*public static bool operator ==(CommonNode left, CommonNode right)
        {
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
            if (left.token.value == right.token.value && left.type == right.type) return true;
            return false;
        }
        public static bool operator !=(CommonNode left, CommonNode right)
        {
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
            if (left.token.value != right.token.value || left.type != right.type) return true;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is CommonNode other)
                return this == other;
            return false;
        }*/
    }


    public enum NT
    {
        // TT чтобы удобно переводить из TT -> NT (по порядковому номеру)
        // TODO: Все изменения TT вносить и сюда!
        COMMENT,
        ELSEIF, IF, ELSE, DEFIF,
        FLOAT, NUMBER, HEX,
        PREFIX, REGDECL, OPER, VARDECL,
        ENDINCLUDE, ASMINCLUDE, INCLUDE, USING,
        NAMESPACE, EXTERNFUNC, EXTERNLIBRARY, EXTERN, FROM,
        ASM, SEM,
        RETURN, BREAK, CONTINUE, JMP,
        LOOP, ITER, FOR, WHILE, ENUMERATOR, REPT,
        LAMBDA, STRUCT, CLASS, ENUM, VIRTUAL, OVERRIDE,
        DEFINE, TYPEDEF, TYPEIF,
        CONST, SECTION, NATIVE, INLINE, ASMINLINE, OPERATOR,
        SIZEOF, TYPEOF, IN, MODIFIER,
        BOOL, VAR, CHAR, ASTRING, STRING,
        LPAR, RPAR, PARS, LFIG, RFIG, LK, RK, LKN, RKN,
        SPACE, TAB, N, PS, TS, TSS,
        NULL,
        // NT чисто NT без перевода обратно в TT
        ROOT, 
        ALLOCMEMSTATICOBJECT, REFVAR, BODY, OFFSETBODY,
        BINOPER, FLOATBINOPER,
        FUNC,  CALL,
        CMP,
        ADDRESS,
        PREUNAROPER, POSTUNAROPER,
        REGUSE, TYPEOPER, SIGNATURE, OFFSET,
        TAG, FUNCTEMPLETE, CONSTRUCTOR, DESTRUCTOR, DECLARATOR, ELSES, NAME, AIR,
        TYPE, INDICATOR, PTR,
        STACK, USEADDRESSVAR,
        STEP,
        FLOATOPER, CALLADDRESS
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

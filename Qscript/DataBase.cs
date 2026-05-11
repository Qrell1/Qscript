using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public static class DataBase
    {
        public static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int64", "dq"},
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "du"},
            {"char", "db"},
            {"wchar", "dw"},
            {"float", "dd"},
            {"double", "dq"},
            {"int32_a", "dd"},
            {"bool", "db"},
            {"long", "dd"},
            {"half", "dw"},
            {"function", "dd"},
            {"dq", "dq"},
            {"dd", "dd"},
            {"dw", "dw"},
            {"db", "db"}
        };
        public static Dictionary<string, string> typesarg = new Dictionary<string, string>()
        {
            {"int64", "QWORD"},
            {"int32", "DWORD"},
            {"int16", "WORD"},
            {"int8", "BYTE"},
            {"byte", "BYTE"},
            {"string", "DWORD"},
            {"char", "BYTE"},
            {"wchar", "WORD"},
            {"float", "DWORD"},
            {"double", "QWORD"},
            {"int32_a", "DWORD"},
            {"bool", "BYTE"},
            {"long", "DWORD"},
            {"half", "WORD"},
            {"function", "DWORD"},
            {"dq", "QWORD"},
            {"dd", "DWORD"},
            {"dw", "WORD"},
            {"db", "BYTE"}
        };
        public static Dictionary<string, int> aligns = new Dictionary<string, int>()
        {
            {"int64",   8},
            {"int32",   4},
            {"int16",   2},
            {"int8",    1},
            {"byte",    1},
            {"string",  2},
            {"char",    1},
            {"wchar",   2},
            {"float",   4},
            {"double",  8},
            {"int32_a", 4},
            {"bool",    1},
            {"long",    4},
            {"half",    2},
            {"function",4},
            {"dq",      8},
            {"dd",      4},
            {"dw",      2},
            {"db",      1}
        };
        public static Dictionary<string, string> typesregs = new Dictionary<string, string>()
        {
            {"int64",   "rax"},
            {"int32",   "eax"},
            {"int16",   "ax"},
            {"int8",    "al"},
            {"byte",    "al"},
            {"string",  "ax"},
            {"char",    "al"},
            {"wchar",   "ax"},
            {"float",   "eax"},
            {"double",  "rax"},
            {"int32_a", "eax"},
            {"bool",    "al"},
            {"long",    "eax"},
            {"half",    "ax"},
            {"function", "eax"},
            {"dq",      "rax"},
            {"dd",      "eax"},
            {"dw",      "ax"},
            {"db",      "al"}
        };
        public static Dictionary<string, string[]> regs = new Dictionary<string, string[]>()
        {
            {"eax", new string[] {"rax","eax","ax","al"}},
            {"ecx", new string[] {"rcx","ecx","cx","cl"}},
            {"edx", new string[] {"rdx","edx","dx","dl"}},
            {"ebx", new string[] {"rbx","ebx","bx","bl"}},
            {"edi", new string[] {"rdi","edi","di","ah"}},
            {"esi", new string[] {"rsi","esi","si","ch"}}
        };
        public static Dictionary<string, string> regschars = new Dictionary<string, string>()
        {
            {"eax", "a"},
            {"ebx", "b"},
            {"edx", "d"},
            {"ecx", "c"}
        };

        public static int isRightOperator (string operation, CommonNode leftType, CommonNode rightType, ref ProgramNode ast)
        {
            int index = 0;
            foreach (var oper in ast.operatorFunctions)
            {
                if (oper.Key.Item1 == operation && 
                    (
                    (oper.Key.Item2._equals(leftType) && oper.Key.Item3._equals(rightType))
                    || (getTypeSize(oper.Key.Item2, ref ast) == getTypeSize(leftType, ref ast)
                        && getTypeSize(oper.Key.Item3, ref ast) == getTypeSize(rightType, ref ast)
                        )
                    ))
                    return index;
                index++;
            }
            return -1;
        }
        //public static int isRightSizeOperator(int index, ref ProgramNode ast)
        //{
        //   
        //}
        public static bool isTypeNodeEquels (CommonNode left,  CommonNode right, ref VarSpace varSpace, ref ProgramNode ast)
        {
            if (left._equals(right)) return true;
            (CommonNode leftType, int leftSize) = getFormulaNodeInfo(left, ref varSpace, ref ast);
            (CommonNode rightType, int rightSize) = getFormulaNodeInfo(right, ref varSpace, ref ast);

            if (leftType._equals(rightType)) return true;
            if (leftSize == rightSize) return true;
            return false;
        }

        public static (CommonNode, int) getFormulaNodeInfo(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            int size = 0;
            CommonNode type = node;
            try
            {
                switch (node.type)
                {
                    case NT.LAMBDA:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "function", node.token.pos));
                        size = 4;
                        break;
                    case NT.VAR:
                    case NT.POSTUNAROPER:
                    case NT.PREUNAROPER:
                        if (node.token.value.Contains('.')) type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", 0));
                        else type = varSpace.GetType(node.token.value);
                        size = getTypeSize(type, ref ast);
                        break;
                    case NT.SIZEOF:
                    case NT.TYPEOF:
                    case NT.NUMBER:
                    case NT.ADDRESS:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
                        size = 4;
                        break;
                    case NT.BINOPER:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
                        size = 4;

                        (CommonNode _type1, int _size1) = getFormulaNodeInfo(node.childs[0], ref varSpace, ref ast);
                        (CommonNode _type2, int _size2) = getFormulaNodeInfo(node.childs[1], ref varSpace, ref ast);

                        if (_size1 == _size2) size = _size1;

                        int temp = isRightOperator(node.token.value, _type1, _type2, ref ast);
                        if (temp != -1) {
                            string OperatorName = ast.operatorFunctions.ElementAt(temp).Value;
                            type = ast.resualtFunc[OperatorName];
                            size = getTypeSize(ast.resualtFunc[OperatorName], ref ast);
                        }
                        break;
                    case NT.FLOAT:
                    case NT.FLOATOPER:
                        type = new CommonNode(NT.TYPE, new Token(TT.FLOAT, "float", node.token.pos));
                        size = 4;
                        break;
                    case NT.STRING:
                        type = new CommonNode(NT.TYPE, new Token(TT.STRING, "string", node.token.pos));
                        size = 2;
                        break;
                    case NT.CHAR:
                        type = new CommonNode(NT.TYPE, new Token(TT.CHAR, "char", node.token.pos));
                        size = 2;
                        break;
                    case NT.BOOL:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 1;
                        break;
                    case NT.TYPEOPER:
                        type = node;
                        size = getTypeSize(type, ref ast);
                        break;
                    case NT.CALL:
                        if (varSpace.ContainsKey(node.token.value)) type = varSpace.GetType(node.token.value);
                        else type = ast.resualtFunc[node.token.value];
                        size = getTypeSize(type, ref ast);
                        break;
                    default:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 4;
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine($"\n{e.Message}\n{e.Data}\n{e.StackTrace}"); Console.ReadKey();
                type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
                size = 4;
            }
            return (type, size);
        }
        public static int getFormulaNodeSize(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item2;
        }
        public static CommonNode getFormulaNodeType(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item1;
        }
        public static CommonNode getFormulaNodeType(CommonNode node, ref Dictionary<string, CommonNode> vars, ref ProgramNode ast)
        {
            VarSpace varSpace = new VarSpace();
            varSpace.VarsSpaces.Push(vars);
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item1;
        }

        /*public static string getFormulaType(CommonNode node, VarSpace varSpace, ProgramNode ast)
        {
            string type;
            try
            {
                switch (node.type)
                {
                    case NT.LAMBDA:
                        type = "function";
                        break;
                    case NT.VAR:
                    case NT.POSTUNAROPER:
                    case NT.PREUNAROPER:
                        type = varSpace.GetType(node.token.value).token.value;
                        break;
                    case NT.SIZEOF:
                    case NT.TYPEOF:
                    case NT.NUMBER:
                    case NT.ADDRESS:
                        type = "int";
                        break;
                    case NT.BINOPER:
                        type = "BINOPER";

                        string _type1 = getFormulaType(node.childs[0]);
                        string _type2 = getFormulaType(node.childs[0]);

                        if (_type1 == _type2) type = _type1;

                        string leftType = (_type1 == "STRING") ? "string" : _type1;
                        string rightType = (_type2 == "STRING") ? "string" : _type2;

                        if (ast.operatorFunctions.ContainsKey((node.token.value, leftType, rightType)))
                        {
                            string OperatorName = ast.operatorFunctions[(node.token.value, leftType, rightType)];

                            type = ast.resualtFunc[OperatorName].token.value;
                        }
                        break;
                    case NT.FLOAT:
                    case NT.FLOATOPER:
                        type = "float";
                        break;
                    case NT.STRING:
                        type = "string";
                        break;
                    case NT.CHAR:
                        type = "char";
                        break;
                    case NT.BOOL:
                        type = "bool";
                        break;
                    case NT.TYPEOPER:
                        type = node.token.value;
                        break;
                    case NT.CALL:
                        type = varSpace.GetType(node.token.value).token.value;
                        break;
                    default:
                        type = "int";
                        break;
                }
            }
            catch { type = node.ToString(); }
            return type;
        }*/
        public static int getStructSize(string type, ref ProgramNode ast)
        {
            int size = 0;
            
            //if (types.ContainsKey())

            foreach (var _var in ast.structs[type].Values)
            {
                if (_var.type == NT.INDICATOR) size += 4;
                else if (DataBase.types.ContainsKey(_var.token.value))
                {
                    string classsize = DataBase.types[_var.token.value];
                    if (_var.token.value == "dq") size += 8;
                    else if (_var.token.value == "dd") size += 4;
                    else if (_var.token.value == "dw") size += 2;
                    else if (_var.token.value == "db") size += 1;
                }
                else if (_var.token.value == "dq") size += 8;
                else if (_var.token.value == "dd") size += 4;
                else if (_var.token.value == "dw") size += 2;
                else if (_var.token.value == "db") size += 1;
                else size += getStructSize(_var.token.value, ref ast);
            }

            return size;
        }
        public static int getTypeSize(CommonNode type, ref ProgramNode ast)
        {
            if (type.type == null) return 4;
            if (type.type == NT.INDICATOR) return 4;
            //Console.WriteLine($"{type.token.value} | {type.type}");
            if (aligns.ContainsKey(type.token.value)) return aligns[type.token.value];
            else return getStructSize(type.token.value, ref ast);
        }
    }
}

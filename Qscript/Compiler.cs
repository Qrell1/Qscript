using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Qscript
{
    public enum CodeData
    { 
        codeData, procData, macroData
    }
    public enum TypeApp
    {
        dll, program32, program64, asmmodule
    }
    public class objProgram
    {
        //public List<CommonNode> functions;
        public StringBuilder data = new StringBuilder();
        public StringBuilder code;

        public StringBuilder codeData = new StringBuilder();
        public StringBuilder procData = new StringBuilder();
        public StringBuilder macroData = new StringBuilder();

        public StringBuilder includes = new StringBuilder();

        //public StringBuilder localsData = new StringBuilder();

        public bool local;
    }
    public class Compiler
    {
        public objProgram _objProg = new objProgram();
        public string fasmCompilerPath;
        private ProgramNode ProgramAst;
        private string refVarStr = string.Empty;

        private List<string> vars = new List<string>();

        private Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "db"},
            {"char", "db"},
            {"float", "dd"},
            {"int32_a", "dd"}
        };
        private Dictionary<string, string> typesarg = new Dictionary<string, string>()
        {
            {"int32", "DWORD"},
            {"int16", "WORD"},
            {"int8", "BYTE"},
            {"byte", "BYTE"},
            {"string", "BYTE"},
            {"char", "BYTE"},
            { "float", "DWORD" },
            {"int32_a", "DWORD"}
        };
        public Dictionary<string, string> stringConsts = new Dictionary<string, string>();
        public int stringConstsIndex;

        private Dictionary<string, string> consts = new Dictionary<string, string>();
        private int constsIndex;

        public Dictionary<string, string> floatConsts = new Dictionary<string, string>();
        public int floatConstsIndex;

        public CommonNode returnType;
        public string funcName;

        private int trueTagIndex;
        private int falseTagIndex;
        private int elsesTagIndex;

        private int iterTagIndex;

        public Compiler(string _fasmCompilerPath, ProgramNode ast) { fasmCompilerPath = _fasmCompilerPath; ProgramAst = ast; }


        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count>=i+1)
            {
                return node.childs[i];
            }
            return null;
        }

        public void setWriteData (CodeData data)
        {
            if (data == CodeData.codeData)
            {
                _objProg.code = _objProg.codeData;
            }
            if (data == CodeData.procData)
            {
                _objProg.code = _objProg.procData;
            }
            if (data == CodeData.macroData)
            {
                _objProg.code = _objProg.macroData;
            }
        }
        public void local()
        {
            _objProg.local = !_objProg.local;
        }

        public void Translation (CommonNode root, int z_buffer)
        {
            //Console.WriteLine($"[DEBUG]--PrintAST>{GenSpaces(z_buffer)} |Тип:{root.type} [{root.token.value}] Дочерних узлов:{root.childs.Count}");
            //if (z_buffer != 0)
                //_objProg.code.Append("  ");
            switch (root.type)
            {
                case "ROOT":
                    setWriteData(CodeData.codeData);
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        CommonNode child = root.childs[i];
                        Translation(child, z_buffer + 1);
                    }
                    break;
                case "VAR":
                    translationVar(root, z_buffer);
                    break;
                case "BINOPER":
                    translationBinOper(root, z_buffer);
                    break;
                case "FLOATBINOPER":
                    translationFloatBinOper(root, z_buffer);
                    break;
                case "NUMBER":
                    _objProg.code.Append($"mov eax, {root.token.value}\n");
                    break;
                case "CONST":
                    translationConst(root, z_buffer);
                    break;
                case "STRING":
                    translationString(root, z_buffer);
                    break;
                case "FUNC":
                    translationFunc(root, z_buffer);
                    break;
                case "INLINE":
                    translationInline(root, z_buffer);
                    break;
                case "STRUCT":
                    translationStruct(root, z_buffer);
                    break;
                case "ALLOCMEMSTATICOBJECT":
                    Translation(take(root, 0), z_buffer + 1);
                    break;
                case "CALL":
                    translationCall(root, z_buffer);
                    break;
                case "CMP":
                    translationCmp(root, z_buffer);
                    break;
                case "IF":
                    translationIf(root, z_buffer);
                    break;
                case "ELSEIF":
                    translationIf(root, z_buffer);
                    break;
                case "ELSE":
                    translationElse(root, z_buffer);
                    break;
                case "ITER":
                    translationIter(root, z_buffer);
                    break;
                case "RETURN":
                    translationReturn(root, z_buffer);
                    break;
                case "BODY":
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        Translation(root.childs[i], z_buffer + 1);
                    }
                    break;
                case "ASM":
                    //_objProg.code.Append(root.token.value);
                    //string[] strings = root.token.value.Split(new char[] { ';' });
                    parseAsm(root);

                    break;
                case "USING":
                    _objProg.includes.Append($"include '{take(root,0).token.value}'\n");
                    break;
            }
        }
        public void translationReturn (CommonNode root, int z_buffer)
        {
            CommonNode returnValue = take(root, 0);

            if (!typesarg.Keys.Contains(returnType.token.value))
            {
                Translation(returnValue, z_buffer + 1);
                _objProg.code.Append($"mov esi, eax\n");
            } else if (returnType.type == "VAR")
            {
                Translation(returnValue, z_buffer + 1);
                _objProg.code.Append($"mov eax, [{returnValue.token.value}]\n");
                //_objProg.code.Append($"mov esi, eax\n");
            } else
            {
                Translation(returnValue, z_buffer + 1);
                //_objProg.code.Append($"mov esi, eax\n");
            }
            _objProg.code.Append($"jmp {funcName}.return\n");
        }
        public void translationIter (CommonNode root, int z_buffer)
        {
            CommonNode countNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            if (countNode.type == "NUMBER")
            {
                _objProg.code.Append($"xor ecx, ecx\n");
                _objProg.code.Append($"iter{iterTagIndex}:\n");

                _objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                _objProg.code.Append($"pop ecx\n");

                _objProg.code.Append($"inc ecx\n");
                _objProg.code.Append($"cmp ecx, {countNode.token.value}\n");
                _objProg.code.Append($"jne iter{iterTagIndex}\n");
            } else
            {
                Translation(countNode, z_buffer + 1);
                _objProg.code.Append($"xor ecx, ecx\n");
                _objProg.code.Append($"iter{iterTagIndex}:\n");

                _objProg.code.Append($"push eax\n");
                _objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                _objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"pop eax\n");

                _objProg.code.Append($"inc ecx\n");
                _objProg.code.Append($"cmp ecx, eax\n");
                _objProg.code.Append($"jne iter{iterTagIndex}\n");
            }
            iterTagIndex++;
        }
        public void translationIf (CommonNode root, int z_buffer)
        {
            CommonNode cmp = take(root, 0);
            CommonNode body = take(root, 1);
            CommonNode elses = take(root, 2);
            translationCmp(cmp, z_buffer + 1);
            Translation(body, z_buffer + 1);
            if (elses != null)
            {
                _objProg.code.Append($"jmp elses{elsesTagIndex}\n");
                _objProg.code.Append($"false{falseTagIndex}:\n");
                falseTagIndex++;
                Translation(elses.childs[0], z_buffer + 2);
                _objProg.code.Append($"elses{elsesTagIndex}:\n");
                elsesTagIndex++;
            }
            else
            {
                _objProg.code.Append($"false{falseTagIndex}:\n");
                falseTagIndex++;
            }
        }
        public void translationElse(CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 0);
            Translation(body, z_buffer + 1);
        }
        public void translationCmp (CommonNode root, int z_buffer, bool cmp=false)
        {
            if ("&&" == root.token.value)
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                _objProg.code.Append($"; CMP {root.token.value}\n");
                translationCmp(leftChild, z_buffer + 1);
                translationCmp(rightChild, z_buffer + 1);
                
            }
            if ("||" == root.token.value)
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                _objProg.code.Append($"; CMP {root.token.value}\n");
                translationCmp(leftChild, z_buffer + 1, true);
                translationCmp(rightChild, z_buffer + 1, true);

                _objProg.code.Append($"jmp false{falseTagIndex}\n");
                _objProg.code.Append($"true{trueTagIndex}:\n");
                trueTagIndex++;
            }
            if (new string[] { "==", "!=", ">=", "<=", "<", ">" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);
                string floatVar;
                string floatVar2;

                _objProg.code.Append($"; CMP\n");

                if (leftChild.type == "NUMBER" && rightChild.type == "NUMBER")
                {
                    _objProg.code.Append($"mov eax, {leftChild.token.value}\n");
                    _objProg.code.Append($"cmp eax, {rightChild.token.value}\n");
                }
                if (leftChild.type == "FLOAT" && rightChild.type == "FLOAT")
                {
                    floatVar = getFloatConst(leftChild);
                    floatVar2 = getFloatConst(rightChild);
                    _objProg.code.Append($"mov eax, [{floatVar}]\n");
                    _objProg.code.Append($"cmp eax, [{floatVar2}]\n");
                }
                else if (rightChild.type == "NUMBER")
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"cmp eax, {rightChild.token.value}\n");
                }
                else if (rightChild.type == "FLOAT")
                {
                    Translation(leftChild, z_buffer + 1);
                    floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"cmp eax, [{floatVar}]\n");
                }
                else
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"push eax\n");
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov ebx, eax\n");
                    _objProg.code.Append($"pop eax\n");
                    _objProg.code.Append($"cmp eax, ebx\n");
                }
                //==  - je
                //!= -jne
                //!0 = -jnz
                //= 0 = -jz
                //> -jg
                //>= -jge
                //< -jl
                //<= -jle
                if (!cmp)
                {
                    switch (root.token.value)
                    {
                        case "==":
                            _objProg.code.Append($"jne false{falseTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"je false{falseTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jl false{falseTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jg false{falseTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jle false{falseTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jge false{falseTagIndex}\n");
                            break;
                    }
                } else
                {
                    switch (root.token.value)
                    {
                        case "==":
                            _objProg.code.Append($"je true{trueTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"jne true{trueTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jge true{trueTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jle true{trueTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jg true{trueTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jl true{trueTagIndex}\n");
                            break;
                    }
                }

                //_objProg.code.Append($"false{falseTagIndex}:");
                //falseTagIndex++;
                //if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                //_objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
            }
        }
        public void translationLeftRightNodes (CommonNode leftChild, CommonNode rightChild, int z_buffer)
        {
            if (leftChild.type == "NUMBER" && rightChild.type == "NUMBER")
            {
                _objProg.code.Append($"mov eax, {leftChild.token.value}\n");
                _objProg.code.Append($"mov ebx, {rightChild.token.value}\n");
            }
            else if (rightChild.type == "NUMBER")
            {
                Translation(leftChild, z_buffer + 1);
                _objProg.code.Append($"mov ebx, {rightChild.token.value}\n");
            }
            else
            {
                Translation(leftChild, z_buffer + 1);
                _objProg.code.Append($"push eax\n");
                Translation(rightChild, z_buffer + 1);
                _objProg.code.Append($"mov ebx, eax\n");
                _objProg.code.Append($"pop eax\n");
            }
        }
        public void translationVar (CommonNode root,  int z_buffer)
        {
            if (root.childs.Count == 0 && !types.Keys.Contains(root.token.value))
            {
                _objProg.code.Append($"mov eax, [{root.token.value}]\n");
                return;
            }
            if (root.childs.Count == 0 && typesarg.Keys.Contains(root.token.value))
            {
                _objProg.code.Append($"lea eax, [{root.token.value}]\n");
                return;
            }
            CommonNode type = take(root, 0);
            string classes = "";
            if (types.Keys.Contains(type.token.value))
                classes = types[type.token.value];
            else
                classes = type.token.value;
            if (!vars.Contains(root.token.value) && type != null && _objProg.local == false)
            {
                _objProg.data.Append($"{root.token.value} {classes} 0\n");
            }
            if (!vars.Contains(root.token.value) && type != null && _objProg.local == true)
            {
                _objProg.code.Append($"local {root.token.value} {classes} 0\n");
            }
        }
        public void translationBinOper (CommonNode root, int z_buffer)
        {
            if (root.token.value == "=")
            {
                // child
                CommonNode varChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);
                //_objProg.code.Append("xor eax, eax\n");
                if (rightChild.type == "FLOAT")
                {
                    _objProg.data.Append($"{varChild.token.value} dd {rightChild.token.value.Replace("f","")}\n");
                    return;
                }
                if (varChild.childs.Count != 0) 
                    Translation(varChild, z_buffer + 1);
                //Translation (rightChild, z_buffer + 1);
                if (rightChild.type == "STRING")
                {
                    if (!stringConsts.Keys.Contains(rightChild.token.value))
                    {
                        stringConsts.Add(rightChild.token.value, $"str_const_{stringConstsIndex}");
                        _objProg.data.Append($"str_const_{stringConstsIndex} db {rightChild.token.value}");
                        stringConstsIndex++;
                    }

                    _objProg.code.Append($"mov eax, {stringConsts[rightChild.token.value]}\n");
                    _objProg.code.Append($"mov {varChild.token.value}, eax\n");
                    //_objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                    return;
                }
                else if (rightChild.type == "CALL")
                {
                    translationCall(rightChild, z_buffer + 1, $"lea eax, [{varChild.token.value}]\n");
                }
                else if (!(rightChild.type == "NUMBER"))
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }
                else if (rightChild.type == "NUMBER")
                {
                    _objProg.code.Append($"mov [{varChild.token.value}], {rightChild.token.value}\n");
                }
                return;
            }
            if (new string[] { "+=", "-=", "*=", "/=", "+", "-", "*", "/" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                //string ebx = "ebx"; shr-/ shl-*

                if (rightChild.type == "FLOAT")
                {
                    string floatVar = getFloatConst(rightChild);
                    rightChild.token.value = rightChild.token.value.Replace("f","");
                    if (leftChild.type == "VAR")
                    {
                        //mov eax, [a]
                        //cvtsi2ss xmm0, eax
                        //addss xmm0, [float_const_1];
                        //movss[c], xmm0

                        //movss xmm0, [a]
                        //divss xmm0, [b]
                        //movss[c], xmm0
                        _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                        _objProg.code.Append($"cvtsi2ss xmm0, eax\n");
                        string operation = root.token.value.Replace("=", "");
                        switch (operation)
                        {
                            case "+": _objProg.code.Append($"addss xmm0, [{floatVar}]\n"); break;
                            case "-": _objProg.code.Append($"subss xmm0, [{floatVar}]\n"); break;
                            case "*": _objProg.code.Append($"mulss xmm0, [{floatVar}]\n"); break;
                            case "/": _objProg.code.Append($"divss xmm0, [{floatVar}]\n"); break;
                        }
                        _objProg.code.Append($"movss [{leftChild.token.value}], xmm0\n");
                    }
                }
                if (leftChild.type == "FLOAT")
                {
                    string floatVar = getFloatConst(leftChild);
                    leftChild.token.value = leftChild.token.value.Replace("f", "");
                    if (rightChild.type == "VAR")
                    {
                        _objProg.code.Append($"movss xmm0, [{floatVar}]\n");
                        string operation = root.token.value.Replace("=", "");
                        switch (operation)
                        {
                            case "+": _objProg.code.Append($"addss xmm0, [{rightChild.token.value}]\n"); break;
                            case "-": _objProg.code.Append($"subss xmm0, [{rightChild.token.value}]\n"); break;
                            case "*": _objProg.code.Append($"mulss xmm0, [{rightChild.token.value}]\n"); break;
                            case "/": _objProg.code.Append($"divss xmm0, [{rightChild.token.value}]\n"); break;
                        }
                        //_objProg.code.Append($"movss [{rightChild}], xmm0\n");
                    }
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("*") && (Convert.ToInt32(rightChild.token.value)%2) == 0)
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"shl eax, {Convert.ToInt32(rightChild.token.value)/2}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("/") && (Convert.ToInt32(rightChild.token.value)%2) == 0)
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"shr eax, {Convert.ToInt32(rightChild.token.value)/2}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("+"))
                {
                    _objProg.code.Append($"add [{leftChild.token.value}], {rightChild.token.value}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("-"))
                {
                    _objProg.code.Append($"sub [{leftChild.token.value}], {rightChild.token.value}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("*"))
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"imul eax, {rightChild.token.value}\n");
                    if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                        _objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "VAR")
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    string operation = root.token.value.Replace("=","");
                    switch (operation)
                    {
                        case "+": _objProg.code.Append($"add eax, [{rightChild.token.value}]\n"); break;
                        case "-": _objProg.code.Append($"sub eax, [{rightChild.token.value}]\n"); break;
                        case "*": _objProg.code.Append($"imul eax, [{rightChild.token.value}]\n"); break;
                        case "/": _objProg.code.Append($"cdq\nidiv [{rightChild.token.value}]\n"); break;
                    }
                    if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                        _objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
                    return;
                }

                translationLeftRightNodes(leftChild, rightChild, z_buffer);



                string str = root.token.value.Replace("=", "");
                switch (str)
                {
                    case "+":
                        _objProg.code.Append($"add eax, ebx\n");
                        break;
                    case "-":
                        _objProg.code.Append($"sub eax, ebx\n");
                        break;
                    case "*":
                        _objProg.code.Append($"imul eax, ebx\n");
                        break;
                    case "/":
                        _objProg.code.Append("cdq\n");
                        _objProg.code.Append("idiv ebx\n");
                        break;
                }
                if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                    _objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
            }
        }
        public void translationFloatBinOper(CommonNode root, int z_buffer)
        {
            if (root.token.value == "=")
            {
                // child
                CommonNode varChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                

                //_objProg.code.Append("xor eax, eax\n");
                if (rightChild.type == "FLOAT" && varChild.type == "VAR" && varChild.childs.Count > 0)
                {
                    _objProg.data.Append($"{varChild.token.value} dd {rightChild.token.value.Replace("f", "")}\n");
                    return;
                }
                if (varChild.childs.Count != 0)
                    Translation(varChild, z_buffer + 1);
                //Translation (rightChild, z_buffer + 1);
                if (rightChild.type == "CALL")
                {
                    translationCall(rightChild, z_buffer + 1, $"lea eax, [{varChild.token.value}]\n");
                    _objProg.code.Append($"movss xmm1, eax\n");
                }
                else if (rightChild.type == "NUMBER")
                {
                    _objProg.code.Append($"mov [{varChild.token.value}], {rightChild.token.value}\n");
                }
                else if (rightChild.type == "FLOAT")
                {
                    string floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"mov eax, [{floatVar}]\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }
                else if (rightChild.type == "FLOATBINOPER")
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss [{varChild.token.value}], xmm0\n");
                }
                else if (!(rightChild.type == "NUMBER"))
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }
                return;
            }
            if (new string[] { "+=", "-=", "*=", "/=", "+", "-", "*", "/" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                string leftString = "xmm0";
                string rightString = "xmm1";

                //string ebx = "ebx"; shr-/ shl-*


                if (leftChild.type == "VAR")
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"cvtsi2ss xmm0, eax\n"); // [{leftChild.token.value}]
                    //return;
                } else if (leftChild.type == "NUMBER")
                {
                    _objProg.code.Append($"movss xmm0, {leftChild.token.value}\n");
                } else if (leftChild.type == "FLOAT")
                {
                    string floatVar = getFloatConst(leftChild);
                    _objProg.code.Append($"movss xmm0, [{floatVar}]\n");
                } else if (leftChild.type == "CALL")
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm0, eax\n");
                } else if (leftChild.type == "FLOATBINOPER")
                {
                    Translation(leftChild, z_buffer + 1);
                }

                if (rightChild.type == "VAR")
                {
                    rightString = $"[{rightChild.token.value}]";
                    //_objProg.code.Append($"movss xmm0, [{rightChild.token.value}]\n");
                    //return;
                }
                else if (rightChild.type == "NUMBER")
                {
                    //_objProg.code.Append($"movss xmm0, {rightChild.token.value}\n");
                    rightString = rightChild.token.value;
                }
                else if (rightChild.type == "FLOAT")
                {
                    string floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"movss xmm1, [{floatVar}]\n");
                }
                else if (rightChild.type == "CALL")
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm1, eax\n");
                }
                else if (rightChild.type == "FLOATBINOPER")
                {
                    Translation(rightChild, z_buffer + 1);
                }

                string str = root.token.value.Replace("=", "");
                switch (str)
                {
                    case "+":
                        _objProg.code.Append($"addss {leftString}, {rightString}\n");
                        break;
                    case "-":
                        _objProg.code.Append($"subss {leftString}, {rightString}\n");
                        break;
                    case "*":
                        _objProg.code.Append($"mulss {leftString}, {rightString}\n");
                        break;
                    case "/":
                        _objProg.code.Append("divss {leftString}, {rightString}\n");
                        break;
                }
                if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                    _objProg.code.Append($"movss [{leftChild.token.value}], xmm0\n");
            }
        }
        public void translationConst (CommonNode root, int  z_buffer)
        {
            CommonNode constValue = take(root, 0);

            //_objProg.data.Append($"const_{constsIndex} db {root.token.value}");

            if (constValue.type == "STRING")
            {
                consts.Add(root.token.value, $"const_{constsIndex}");
                _objProg.data.Append($"{consts[root.token.value]} db {constValue.token.value}\n");
                constsIndex++;
            }
            else if (constValue.type == "NUMBER")
            {
                consts.Add(root.token.value, $"const_{constsIndex}");
                _objProg.data.Append($"{root.token.value} equ {constValue.token.value}\n");
                constsIndex++;
            }
        }
        public void translationString (CommonNode root, int z_buffer)
        {
            if (!stringConsts.Keys.Contains(root.token.value))
            {
                stringConsts.Add(root.token.value, $"str_const_{stringConstsIndex}");
                _objProg.data.Append($"str_const_{stringConstsIndex} db {root.token.value}\n");
                stringConstsIndex++;
            }
        }
        public void translationStruct (CommonNode root, int z_buffer)
        {
            setWriteData(CodeData.macroData);
            _objProg.code.Append($"struct {root.token.value}\n");
            for (int i = 0; i < root.childs.Count; i++)
            {
                CommonNode modifier = take(root, i);
                if (modifier.token.value == "public")
                {
                    foreach (CommonNode child in modifier.childs)
                    {
                        if (child.type == "VAR")
                        {
                            if (types.Keys.Contains(take(child, 0).token.value))
                                _objProg.code.Append($"    {child.token.value} {types[take(child, 0).token.value]} 0\n");
                            else
                                _objProg.code.Append($"    {child.token.value} {take(child, 0).token.value}\n");
                        }
                    }
                }
            }
            //_objProg.code.Append($"    sizeof_{root.token.value}:\n");
            _objProg.code.Append("ends\n");

            _objProg.data.Append($"SIZE_{root.token.value.ToUpper()} = sizeof.{root.token.value} / 4\n");
            //_objProg.data.Append($"virtual at 0");
            //_objProg.data.Append($"  Point Point");
            //_objProg.data.Append($"  POINT_SIZE = $");
            //_objProg.data.Append($"end virtual");

            setWriteData(CodeData.codeData);
        }
        public void translationInline (CommonNode root, int z_buffer)
        {
            setWriteData(CodeData.macroData);
            //CommonNode types2 = take(root, 0);
            CommonNode signatureInline = take(root, 0);
            string argsInline = string.Empty;
            for (int i = 0; i < signatureInline.childs.Count; i++)
            {
                argsInline += signatureInline.childs[i].token.value;
                if (i < signatureInline.childs.Count - 1)
                {
                    argsInline += ",";
                }
            }
            _objProg.code.Append($"macro {root.token.value} {argsInline}\n");
            _objProg.code.Append("{\n");
            Translation(take(root, 1), z_buffer + 1);
            _objProg.code.Append("}\n");
            setWriteData(CodeData.codeData);
        }
        public void translationFunc (CommonNode root, int z_buffer)
        {
            _objProg.code.Append($"; FUNC {root.token.value}\n");
            setWriteData(CodeData.procData);
            //CommonNode types2 = take(root, 0);
            CommonNode signature = take(root, 1);
            string args = string.Empty;
            for (int i = 0; i < signature.childs.Count; i++)
            {
                args += ",";
                //if (signature.childs[i].token.value == "resualtPtr")
                    //args += $"{signature.childs[i].token.value}:DWORD";
                if (typesarg.ContainsKey(signature.childs[i].childs[0].token.value))
                    args += $"{signature.childs[i].token.value}:{typesarg[signature.childs[i].childs[0].token.value]}";
                else
                    //args += $"{signature.childs[i].token.value}:{signature.childs[i].childs[0].token.value}";
                    args += $"{signature.childs[i].token.value}:DWORD";
                args += " ";
            }
            if (args.Length != 0)
                _objProg.code.Append($"proc {root.token.value} uses eax{args}\n");
            else
                _objProg.code.Append($"proc {root.token.value} uses eax\n");
            local();
            returnType = take(root, 0);
            funcName = root.token.value;
            Translation(take(root, 2), z_buffer + 1);

            /*
                mov edi, mc2
                mov ecx, 2
                rep movsd
             */
            //_objProg.code.Append($"    ret\n");
            if (ProgramAst.resualtFunc[root.token.value] == null)
            {

            }
            else if (typesarg.Keys.Contains(ProgramAst.resualtFunc[root.token.value].token.value))
            {
                _objProg.code.Append($"{root.token.value}.return:\n");
                _objProg.code.Append($"    test eax, eax\n");
                _objProg.code.Append($"    jz {root.token.value}.retn\n");
                _objProg.code.Append($"    mov ebx, [resualtPtr]\n");
                _objProg.code.Append($"    mov [ebx], eax\n");
            }
            else
            {
                _objProg.code.Append($"{root.token.value}.return:\n");
                _objProg.code.Append($"    mov edi, [resualtPtr]\n");
                _objProg.code.Append($"    test edi, edi\n");
                _objProg.code.Append($"    jz {root.token.value}.retn\n");
                //_objProg.code.Append($"    mov edi, [resualtPtr]\n");
                _objProg.code.Append($"    mov ecx, SIZE_{ProgramAst.resualtFunc[root.token.value].token.value.ToUpper()}\n");
                _objProg.code.Append($"    rep movsd\n");
            }
            _objProg.code.Append($"{root.token.value}.retn:\n");
            _objProg.code.Append($"    ret\n");
            _objProg.code.Append($"endp\n");
            local();
            setWriteData(CodeData.codeData);
        }
        public void translationCall (CommonNode root, int z_buffer, string resualtPtr=null)
        {
            //_objProg.code.Append($"; CALL {root.token.value}\n");
            CommonNode signatureCall = take(root, 0);
            if (ProgramAst.inlineNames.Contains(root.token.value))
            {
                _objProg.code.Append($"{root.token.value} ");
                string args = string.Empty;
                for (int i = 0; i < signatureCall.childs.Count; i++)
                {
                    if (signatureCall.childs[i].type == "NUMBER" || signatureCall.childs[i].type == "VAR")
                        args += signatureCall.childs[i].token.value;
                    else if (signatureCall.childs[i].type == "STRING")
                    {
                        Translation(signatureCall.childs[i], z_buffer + 1);
                        args += stringConsts[signatureCall.childs[i].token.value];
                    }
                    else
                        //throw new Exception("У Токена:" + signatureCall.childs[i].token.pos+"ошибка в вызове инлайн функции может быть только строка, переменная или число!!");
                        Syntax.SyntaxError("У Токена:" + signatureCall.childs[i].token.pos + "ошибка в вызове инлайн функции может быть только строка, переменная или число!!", signatureCall);
                    if (i < signatureCall.childs.Count-1)
                        args += ",";
                }
                _objProg.code.Append($"{args}\n");
                return;
            }

            for (int i = signatureCall.childs.Count - 1; i >= 0; i--)
            {
                if (signatureCall.childs[i].type == "VAR" && !typesarg.Keys.Contains(ProgramAst.varTypes[signatureCall.childs[i].token.value].token.value))
                {
                    _objProg.code.Append($"lea eax, [{signatureCall.childs[i].token.value}]\n");
                    _objProg.code.Append($"push eax\n");
                }
                else if (signatureCall.childs[i].type == "STRING")
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push {stringConsts[signatureCall.childs[i].token.value]}\n");
                }
                else
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push eax\n");
                }
            }
            if (resualtPtr!=null && ProgramAst.resualtFunc[root.token.value] != null)
            {
                _objProg.code.Append(resualtPtr);
                _objProg.code.Append($"push eax\n");
            }
            _objProg.code.Append($"call {root.token.value}\n");
        }
        
        public string getFloatConst (CommonNode node)
        {
            if (floatConsts.Keys.Contains(node.token.value))
            {
                return floatConsts[node.token.value];
            } else
            {
                floatConsts.Add(node.token.value, $"float_const_{floatConstsIndex}");
                _objProg.data.Append($"float_const_{floatConstsIndex} dd {node.token.value}\n");
                floatConstsIndex++;
                return floatConsts[node.token.value];
            }
        }
        
        // ВРЕМЕННЫЙ СУПЕР ГОВНОКОД   
        public void parseAsm (CommonNode root)
        {
            StringBuilder chars = new StringBuilder();
            char[] strs = root.token.value.ToCharArray();
            string str = root.token.value;
            int pos = 0;

            while (pos < str.Length)
            {
                Match regx = Regex.Match(str.Substring(pos), "^" + TokenTypeList.tokenTypes["STRING"].regx);
                if (regx.Success && !string.IsNullOrEmpty(regx.Value))
                    pos += regx.Length;
                else
                {
                    chars.Append(strs[pos]);
                    pos++;
                }
            }
            string temp = chars.ToString();
            str = string.Empty;

            string[] strings = temp.Split(new char[] { ';'}, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < strings.Length; i++)
                _objProg.code.Append(strings[i] + "\n");
        }

        public string ConcatData (TypeApp typeApp)
        {
            string file = string.Empty;
            file += _objProg.includes.ToString();
            file += "\n";
            file += _objProg.macroData.ToString();
            if (_objProg.data.Length != 0)
                file += "\nsection '.data' data readable writable\n";
            file += _objProg.data.ToString();
            if (_objProg.procData.Length != 0)
                file += "\nsection '.code' code readable executable\n";
            file += _objProg.procData.ToString();
            // section '.code' code readable executable
            // section '.data' data readable writable

            if (typeApp == TypeApp.asmmodule)
            {
            }
            else if (typeApp == TypeApp.dll)
            {
                //file += "\nsection '.code' code readable executable\n";
                file = "format PE DLL\n\n" + file;
            }
            else if (typeApp == TypeApp.program32)
            {
                file = "format PE console\n\nentry start\n" + file;
                //file += "\nsection '.code' code readable executable\n";
                file += "start: ;START MAIN\n";
                file += _objProg.codeData.ToString();
            }else if (typeApp == TypeApp.program64)
            {
                file = "format PE console\n\nentry start\n" + file;
                //file += "\nsection '.code' code readable executable\n";
                file += "start: ;START MAIN\n";
                file += _objProg.codeData.ToString();
            }
            return file;
        }
        public void WriteCode (string data, string nameFile, string pathCompile, string extend = "asm")
        {
            string path = $"{pathCompile}\\{nameFile}\\{nameFile}.{extend}";
            //File.Delete(path);
            Console.WriteLine($"{pathCompile}\\{nameFile}\\");
            Directory.CreateDirectory($"{pathCompile}\\{nameFile}\\");
            FileStream fileStream = File.Create(path);
            fileStream.Close();
            File.WriteAllText(path, data);
        }
        public void Compilation (string nameFile, string extend="asm")
        {
            //string path = "/compile" + "/" + nameFile.Replace(".qs", "") + "/" + nameFile;
            string path = $"/compile/{nameFile}/{nameFile}.{extend}";

            var proc = new Process();
            proc.StartInfo.FileName = fasmCompilerPath;
            proc.StartInfo.Arguments = path;
            proc.Start();

            proc.WaitForExit();
            proc.Close();
        }
    }
}

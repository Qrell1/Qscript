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
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Qscript
{
    public enum CodeData
    { 
        codeData, procData, macroData
    }
    public enum TypeApp
    {
        dll, program32, program64, asmmodule, h, gui
    }
    public class objProgram
    {
        public StringData data = new StringData();
        public StringData code;

        public StringData codeData = new StringData();
        public StringData procData = new StringData();
        public StringData macroData = new StringData();

        public StringBuilder includes = new StringBuilder();

        public StringBuilder stringsConsts = new StringBuilder();

        public bool local;
    }
    public class Compiler
    {
        public objProgram _objProg = new objProgram();
        public string fasmCompilerPath;
        private ProgramNode ProgramAst;
        private string refVarStr = string.Empty;

        private VarSpace varSpace = new VarSpace();

        public static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int64", "dq"},
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "du"},
            {"char", "db"},
            {"float", "dd"},
            {"double", "dq"},
            {"int32_a", "dd"},
            {"bool", "db"},
            {"long", "dd"},
            {"half", "dw"},
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
            {"string", "WORD"},
            {"char", "BYTE"},
            {"float", "DWORD"},
            {"double", "QWORD"},
            {"int32_a", "DWORD"},
            {"bool", "BYTE"},
            {"long", "DWORD"},
            {"half", "WORD"}
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
            {"float",   4},
            {"double",  8},
            {"int32_a", 4},
            {"bool",    1},
            {"long",    4},
            {"half",    2},
            {"dq",      8},
            {"dd",      4},
            {"dw",      2},
            {"db",      1}
        };
        public Dictionary<string, string> stringConsts = new Dictionary<string, string>();
        public int stringConstsIndex;

        private Dictionary<string, string> consts = new Dictionary<string, string>();
        private int constsIndex;

        public Dictionary<string, string> floatConsts = new Dictionary<string, string>();
        public int floatConstsIndex;

        public List<string> tempStructs = new List<string>();

        public CommonNode returnType;
        public string funcName;
        public bool func;

        private int trueTagIndex;
        private int falseTagIndex;
        private int elsesTagIndex;
        private int tempTagIndex;

        private int iterTagIndex;


        public Compiler(string _fasmCompilerPath, ProgramNode ast) { fasmCompilerPath = _fasmCompilerPath; ProgramAst = ast; varSpace.VarsData = ast.varTypes; }


        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count>=i+1)
            {
                return node.childs[i];
            }
            return null;
        }

        private Dictionary<string, CommonNode> copyVars(Dictionary<string, CommonNode> copy)
        {
            Dictionary<string, CommonNode> ts = new Dictionary<string, CommonNode>();

            foreach (var var in copy)
                ts.Add(var.Key, var.Value);

            return ts;
        }

        /*public void localVarTypes (CommonNode vars = null)
        {
            if (!localVars)
            {
                tempVars = copyVars(ProgramAst.varTypes);
                localVars = true;
                foreach (var var in vars.childs) ProgramAst.varTypes.Add(var.token.value, var.childs[0]);
            } else
            {
                ProgramAst.varTypes = copyVars(tempVars);
                localVars = false;
            }
        }*/

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
                case "BOOL":
                    char boolChar = (root.token.value == "true") ? '1' : '0'; _objProg.code.Append($"mov eax, {boolChar}\n");
                    break;
                case "CONST":
                    translationConst(root, z_buffer);
                    break;
                case "STRING":
                    translationString(root, z_buffer);
                    break;
                case "CHAR":
                    translationChar(root, z_buffer);
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
                case "FOR":
                    translationFor(root, z_buffer);
                    break;
                case "WHILE":
                    translationWhile(root, z_buffer);
                    break;
                case "ENUMERATOR":
                    translationEnumerator(root, z_buffer);
                    break;
                case "REPT":
                    translationRept(root, z_buffer);
                    break;
                case "RETURN":
                    translationReturn(root, z_buffer);
                    break;
                case "REFVAR":
                    translationRefVar(root, z_buffer);
                    break;
                case "SIZEOF":
                    translationSizeof(root, z_buffer);
                    break;
                case "TYPEOF":
                    translationTypeof(root, z_buffer);
                    break;
                case "ADDRESS":
                    translationAddress(root, z_buffer);
                    break;
                case "PREUNAROPER":
                    translationPreUnarOper(root, z_buffer);
                    break;
                case "POSTUNAROPER":
                    translationPostUnarOper(root, z_buffer);
                    break;
                case "TAG":
                    _objProg.code.Append($"{root.token.value}:\n");
                    break;
                case "JMP":
                    _objProg.code.Append($"jmp {take(root, 0).token.value}\n");
                    break;
                case "BODY":
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        Translation(root.childs[i], z_buffer + 1);
                    }
                    break;
                case "ASM":
                    parseAsm(root);
                    break;
                case "USING":
                    _objProg.includes.Append($"include '{take(root,0).token.value}'\n");
                    break;
            }
        }
        //public void
        public void translationWhile (CommonNode root, int z_buffer)
        {
            CommonNode cmpNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);
            
            int iterNumber = ++iterTagIndex;

            _objProg.code.Append($"iter{iterNumber}:\n");

            Translation(bodyNode, z_buffer + 1);

            Translation(cmpNode, z_buffer + 1);
            _objProg.code.Append($"jmp iter{iterNumber}\n");
            _objProg.code.Append($"false{falseTagIndex}:\n");
            falseTagIndex++;

            //iterTagIndex++;
        }
        public void translationFor (CommonNode root, int z_buffer)
        {
            CommonNode initNode = take(root, 0);
            CommonNode cmpNode = take(root, 1);
            CommonNode stepNode = take(take(root, 2),0);
            CommonNode bodyNode = take(root, 3);

            int iterNumber = ++iterTagIndex;

            Translation(initNode, z_buffer + 1);
            _objProg.code.Append($"iter{iterNumber}:\n");

            Translation(bodyNode, z_buffer + 1);

            Translation(stepNode, z_buffer + 1);
            Translation(cmpNode, z_buffer + 1);
            _objProg.code.Append($"jmp iter{iterNumber}\n");
            _objProg.code.Append($"false{falseTagIndex}:\n");
            falseTagIndex++;

            //iterTagIndex++;
        }
        public void translationRept (CommonNode root, int z_buffer)
        {
            CommonNode signatureNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            _objProg.code.Append($"rept {signatureNode.token.value} {{\n");
            Translation(bodyNode, z_buffer + 1);
            _objProg.code.Append("}");
        }
        public void translationEnumerator (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            CommonNode countNode = take(root, 1);
            CommonNode bodyNode = take(root, 2);

            int iterNumber = ++iterTagIndex;
            /*  local i dd 0
                enumer0:

                inc [i]
                cmp [i], 12
                jne enumer0
            */
            translationVar(varNode, z_buffer + 1);

            string countString = "ecx";
            switch (countNode.type)
            {
                case "NUMBER": countString = countNode.token.value; break;
                case "VAR": countString = $"[{countNode.token.value}]"; break;
                case "FLOATBINOPER": Syntax.SyntaxError("Невозможно использовать флотовую операцию в качестве числа енумераций!", countNode); break;
                case "FLOAT": Syntax.SyntaxError("Невозможно использовать флотовое число в качестве числа енумераций!", countNode); break;
                default: Translation(countNode, z_buffer + 1); _objProg.code.Append($"mov ecx, eax\n"); break;
            }
            _objProg.code.Append($"iter{iterNumber}:\n");

            if (countString == "ecx") _objProg.code.Append($"push ecx\n");
            Translation(bodyNode, z_buffer + 1);
            if (countString == "ecx") _objProg.code.Append($"pop ecx\n");
             
            _objProg.code.Append($"inc [{varNode.token.value}]\n");
            if (countString.First() == '[') countString = "eax";
            _objProg.code.Append($"cmp [{varNode.token.value}], {countString}\n");
            _objProg.code.Append($"jne iter{iterNumber}\n");

            //iterTagIndex++;
        }
        public void translationPreUnarOper (CommonNode root, int z_buffer)//, bool mov = true)
        {
            CommonNode varNode = take(root, 0);
            string oper = (root.token.value == "++") ? "inc" : "dec" ;
            _objProg.code.Append($"{oper} [{varNode.token.value}]\n");
            _objProg.code.Append($"mov eax, [{varNode.token.value}]\n"); // if (mov) 
        }
        public void translationPostUnarOper(CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            string oper = (root.token.value == "++") ? "inc" : "dec";
            _objProg.code.Append($"mov eax, [{varNode.token.value}]\n");
            _objProg.code.Append($"{oper} [{varNode.token.value}]\n");
        }
        public void translationAddress (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            _objProg.code.Append($"lea eax, [{varNode.token.value}]\n");
        }
        public void translationSizeof (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            string size = getSize(varNode.token.value, varNode);
            _objProg.code.Append($"mov eax, {size}\n");
        }
        public void translationTypeof(CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            string size = string.Empty;

            if (types.ContainsKey(varNode.token.value))
            {
                switch (types[varNode.token.value])
                {
                    case "dd": size = "4"; break;
                    case "dw": size = "2"; break;
                    case "db": size = "1"; break;
                }
            }
            else { size = $"SIZE_{varNode.token.value.ToUpper()}"; }

            _objProg.code.Append($"mov eax, {size}\n");
        }
        public void translationRefVar (CommonNode root, int z_buffer)
        {
            CommonNode call = take(root, 0);
            _objProg.code.Append($"sub esp, SIZE_{ProgramAst.resualtFunc[call.token.value].token.value.ToUpper()}\n");
            translationCall(call, z_buffer + 1, "mov eax, esp\n");
            _objProg.code.Append($"mov eax, [esp+{ProgramAst.resualtFunc[call.token.value].token.value}.{root.token.value}-4]\n");
            //_objProg.code.Append($"mov [eax], [esp-{ProgramAst.resualtFunc[call.token.value].token.value}.{root.token.value}]\n");
            _objProg.code.Append($"add esp, SIZE_{ProgramAst.resualtFunc[call.token.value].token.value.ToUpper()}\n");
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
            int iterNumber = ++iterTagIndex;
            if (countNode.type == "NUMBER")
            {
                _objProg.code.Append($"xor ecx, ecx\n");
                _objProg.code.Append($"iter{iterNumber}:\n");

                _objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                _objProg.code.Append($"pop ecx\n");

                _objProg.code.Append($"inc ecx\n");
                _objProg.code.Append($"cmp ecx, {countNode.token.value}\n");
                _objProg.code.Append($"jne iter{iterNumber}\n");
            } else
            {
                Translation(countNode, z_buffer + 1);
                _objProg.code.Append($"xor ecx, ecx\n");
                _objProg.code.Append($"iter{iterNumber}:\n");

                _objProg.code.Append($"push eax\n");
                _objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                _objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"pop eax\n");

                _objProg.code.Append($"inc ecx\n");
                _objProg.code.Append($"cmp ecx, eax\n");
                _objProg.code.Append($"jne iter{iterNumber}\n");
            }
            //iterTagIndex++;
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
                _objProg.code.Append($"jmp .elses{elsesTagIndex}\n");
                _objProg.code.Append($".false{falseTagIndex}:\n");
                falseTagIndex++;
                Translation(elses.childs[0], z_buffer + 2);
                _objProg.code.Append($".elses{elsesTagIndex}:\n");
                elsesTagIndex++;
            }
            else
            {
                _objProg.code.Append($".false{falseTagIndex}:\n");
                falseTagIndex++;
            }
        }
        public void translationElse (CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 0);
            Translation(body, z_buffer + 1);
        }
        public void translationCmp (CommonNode root, int z_buffer, bool cmp=false)
        {
            if ("true" == root.token.value)
            {
                if (cmp) _objProg.code.Append($"jmp .true{trueTagIndex}\n");
                return;
            }
            if ("false" == root.token.value)
            {
                _objProg.code.Append($"jmp .false{falseTagIndex}\n");
            }
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

                _objProg.code.Append($"jmp .false{falseTagIndex}\n");
                _objProg.code.Append($".true{trueTagIndex}:\n");
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
                } else if (rightChild.type == "BOOL")
                {
                    Translation(leftChild, z_buffer + 1);
                    char boolChar = (rightChild.token.value == "true") ? '1' : '0';
                    _objProg.code.Append($"mov ebx, {boolChar}\n");
                    _objProg.code.Append($"cmp eax, ebx\n");
                } else if (rightChild.type == "STRING")
                {
                    Translation(leftChild, z_buffer + 1);
                    translationString(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov ebx, {stringConsts[rightChild.token.value]}\n");
                    _objProg.code.Append($"cmp eax, ebx\n");
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
                            _objProg.code.Append($"jne .false{falseTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"je .false{falseTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jl .false{falseTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jg .false{falseTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jle .false{falseTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jge .false{falseTagIndex}\n");
                            break;
                    }
                } else
                {
                    switch (root.token.value)
                    {
                        case "==":
                            _objProg.code.Append($"je .true{trueTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"jne .true{trueTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jge .true{trueTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jle .true{trueTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jg .true{trueTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jl .true{trueTagIndex}\n");
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
            if (root.childs.Count > 0 && root.childs[0].type == "OFFSET")
            {
                CommonNode offset = root.childs[0];
                //_objProg.code.Append($"push eax\n");
                Translation(offset.childs[0], z_buffer);
                _objProg.code.Append($"mov ecx, eax\n");
                //_objProg.code.Append($"pop eax\n");
                // offset
                _objProg.code.Append($"imul ecx, {getSize(root.token.value, root)}\n");
                _objProg.code.Append($"mov ebx, [{root.token.value.Replace(",", ".")}]\n");
                _objProg.code.Append($"mov eax, [ebx+ecx]\n");
                //_objProg.code.Append($"push ecx\n");
                //_objProg.code.Append($"mov eax, [{root.token.value}+eax]\n");
                return;
            }
            
            if (root.childs.Count == 0 && !types.Keys.Contains(root.token.value))
            {
                _objProg.code.Append($"mov eax, [{root.token.value}]\n");
                return;
            }
            if (root.childs.Count == 0 && typesarg.Keys.Contains(root.token.value))
            {
                _objProg.code.Append($"mov eax, [{root.token.value}]\n");
                return;
            }
            CommonNode type = take(root, 0);
            string classes = "";
            if (types.Keys.Contains(type.token.value))
                classes = types[type.token.value];
            else
                classes = type.token.value;
            if (root.childs.Count > 0 && root.childs[0].type == "INDICATOR")
            { classes = "*" + $"{root.childs[0].token.value}"; }
            if (!varSpace.PeekContainsKey(root.token.value) && type != null && _objProg.local == false)
            {
                _objProg.data.Append($"{root.token.value} {classes} 0\n");
            }
            if (!varSpace.PeekContainsKey(root.token.value) && type != null && _objProg.local == true)
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

                if (varChild.childs.Count > 0 && varChild.childs[0].type == "OFFSET")
                {
                    translationOffset(varChild, rightChild, z_buffer);
                    return;
                }

                if (varChild.childs.Count > 0 && varChild.childs[0].type == "INDICATOR")
                {
                    //_objProg.code.Append("lea eax, [eax]\n");
                    translationVar(varChild, z_buffer + 1);
                    //_objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }

                else if (varChild.childs.Count != 0 && varChild.childs[0].type != "INDICATOR") 
                    Translation(varChild, z_buffer + 1);
                
                //Translation (rightChild, z_buffer + 1);
                if (rightChild.type == "STRING")
                {
                    if (!stringConsts.Keys.Contains(rightChild.token.value))
                    {
                        stringConsts.Add(rightChild.token.value, $"str_const_{stringConstsIndex}");
                        _objProg.stringsConsts.Append($"str_const_{stringConstsIndex} db {rightChild.token.value}\n");
                        stringConstsIndex++;
                    }

                    _objProg.code.Append($"mov eax, {stringConsts[rightChild.token.value]}\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                    //_objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                    return;
                }
                else if (rightChild.type == "CHAR")
                {
                    _objProg.code.Append($"mov al, {rightChild.token.value}\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], al\n");
                }
                else if (rightChild.type == "CALL")
                {
                    //translationCall(rightChild, z_buffer + 1);//, $"lea eax, [{varChild.token.value}]\n");
                    Console.WriteLine(rightChild.token.value + " | " + ProgramAst.resualtFunc[rightChild.token.value].token.value + " CALL");
                    if (types.Keys.Contains(ProgramAst.resualtFunc[rightChild.token.value].token.value))
                    { translationCall(rightChild, z_buffer + 1); _objProg.code.Append($"mov [{varChild.token.value}], eax\n"); }
                    else translationCall(rightChild, z_buffer + 1, $"lea eax, [{varChild.token.value}]\n");
                }
                else if (rightChild.type == "NUMBER")
                {
                    _objProg.code.Append($"mov [{varChild.token.value}], dword {rightChild.token.value}\n");
                }
                else if (!(rightChild.type == "NUMBER"))
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }

                return;
            }
            if (new string[] { "+=", "-=", "*=", "/=", "%=", "+", "-", "*", "/", "%" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                //string ebx = "ebx"; shr-/ shl-*
                /*if (leftChild.type == "VAR" && leftChild.childs.Count > 0 && leftChild.childs[0].type == "OFFSET")
                {
                    translationOffset(leftChild, rightChild, z_buffer);
                    return;
                }
                if (rightChild.type == "VAR" && rightChild.childs.Count > 0 && rightChild.childs[0].type == "OFFSET")
                {
                    CommonNode offset = rightChild.childs[0];
                    //_objProg.code.Append($"push eax\n");
                    Translation(offset.childs[0], z_buffer);
                    _objProg.code.Append($"mov ecx, eax\n");
                    //_objProg.code.Append($"pop eax\n");
                    // offset
                    _objProg.code.Append($"imul ecx, {getSize(rightChild.token.value, rightChild)}\n");
                    _objProg.code.Append($"mov ebx, [{rightChild.token.value}]\n");
                    _objProg.code.Append($"pop ecx\n");
                    rightChild.token.value = $"ebx+ecx";
                }*/

                
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("*") && (Convert.ToInt32(rightChild.token.value)%2) == 0)
                {
                    //_objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    //_objProg.code.Append($"shl eax, {Convert.ToInt32(rightChild.token.value)/2}\n");
                    //return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("/") && (Convert.ToInt32(rightChild.token.value)%2) == 0)
                {
                    //_objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    //_objProg.code.Append($"shr eax, {Convert.ToInt32(rightChild.token.value)/2}\n");
                    //return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("+") && !(root.token.value == "+="))
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"add eax, {rightChild.token.value}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("-") && !(root.token.value == "-="))
                {
                    _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"sub eax, {rightChild.token.value}\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("+"))
                {
                    _objProg.code.Append($"add [{leftChild.token.value}], {rightChild.token.value}\n");
                    if (func) _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                    return;
                }
                if (leftChild.type == "VAR" && rightChild.type == "NUMBER" && root.token.value.Contains("-"))
                {
                    _objProg.code.Append($"sub [{leftChild.token.value}], {rightChild.token.value}\n");
                    if (func) _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
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
                        case "/": _objProg.code.Append($"cdq\n"); _objProg.code.Append($"idiv [{rightChild.token.value}]\n"); break;
                    }
                    if (leftChild.type == "VAR" && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                        _objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
                    return;
                }
                // $"lea eax, [{varChild.token.value}]\n"
                if (!root.token.value.Contains("=") && leftChild.type == "CALL" && rightChild.type != "CALL")
                {
                    translationCall(leftChild, z_buffer + 1);//$"lea eax, [{getTempVarReturn(leftChild)}]\n");
                    //_objProg.code.Append($"mov eax, [{getTempVarReturn(leftChild)}]\n");
                    _objProg.code.Append($"push eax\n");
                    if (rightChild.type == "NUMBER")
                        _objProg.code.Append($"mov ebx, {rightChild.token.value}\n");
                    else
                    {
                        //_objProg.code.Append($"push eax\n");
                        Translation(rightChild, z_buffer + 1);
                        _objProg.code.Append($"mov ebx, eax\n");
                        //_objProg.code.Append($"pop eax\n");
                    }
                    _objProg.code.Append($"pop eax\n");
                }
                else if (!root.token.value.Contains("=") && rightChild.type == "CALL" && leftChild.type != "CALL")
                {
                    Translation(leftChild, z_buffer + 1);
                    //_objProg.code.Append($"mov ecx, eax\n"); // add esp, 8
                    _objProg.code.Append($"push eax\n");
                    translationCall(rightChild, z_buffer + 1);//$"lea eax, [{getTempVarReturn(rightChild)}]\n");
                    _objProg.code.Append($"mov ebx, eax\n");//[{getTempVarReturn(rightChild)}]\n");
                    _objProg.code.Append($"pop eax\n");
                    //_objProg.code.Append($"mov eax, ecx\n");
                }
                else if (!root.token.value.Contains("=") && leftChild.type == "CALL" && rightChild.type == "CALL")
                {
                    translationCall(leftChild, z_buffer + 1);//"lea eax, [{getTempVarReturn(leftChild)}]\n");
                    _objProg.code.Append($"push eax\n");
                    translationCall(rightChild, z_buffer +1);//$"lea eax, [{getTempVarReturn(rightChild)}]\n");
                    _objProg.code.Append($"mov ebx, eax\n");//[{getTempVarReturn(rightChild)}]\n");
                    _objProg.code.Append($"pop eax\n");
                }
                else
                {
                    translationLeftRightNodes(leftChild, rightChild, z_buffer);
                }



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
                    case "%":
                        _objProg.code.Append("xor edx, edx\n");
                        _objProg.code.Append("div ebx\n");
                        _objProg.code.Append("mov eax, edx\n");
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
                if (varChild.childs.Count > 0 && varChild.childs[0].type == "OFFSET")
                {
                    translationOffset(varChild, rightChild, z_buffer);
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
                else if (rightChild.type == "CALL")
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm0, eax\n");
                    _objProg.code.Append($"movss [{varChild.token.value}], xmm0\n");
                }
                else if (rightChild.type == "VAR" && rightChild.childs.Count > 0 && rightChild.childs[0].type == "OFFSET")
                {
                    translationVar(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov ebx, [{rightChild.token.value}]\n");
                    _objProg.code.Append($"mov eax, [ebx+ecx]\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }
                else if (!(rightChild.type == "NUMBER"))
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                }
                return;
            }
            if (new string[] { "+=", "-=", "*=", "/=", "%=", "+", "-", "*", "/", "%" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                string leftString = "xmm0";
                string rightString = "xmm1";

                //string ebx = "ebx"; shr-/ shl-*
                /*if (leftChild.type == "VAR" && leftChild.childs.Count > 0 && leftChild.childs[0].type == "OFFSET")
                {
                    CommonNode offset = leftChild.childs[0];
                    //_objProg.code.Append($"push eax\n");
                    Translation(offset.childs[0], z_buffer);
                    _objProg.code.Append($"mov ecx, eax\n");
                    //_objProg.code.Append($"pop eax\n");
                    // offset
                    _objProg.code.Append($"imul ecx, {getSize(leftChild.token.value, leftChild)}\n");
                    _objProg.code.Append($"mov ebx, [{leftChild.token.value}]\n");

                    leftChild.token.value = $"ebx+ecx";
                }
                if (rightChild.type == "VAR" && rightChild.childs.Count > 0 && rightChild.childs[0].type == "OFFSET")
                {
                    CommonNode offset = rightChild.childs[0];
                    //_objProg.code.Append($"push eax\n");
                    Translation(offset.childs[0], z_buffer);
                    _objProg.code.Append($"mov ecx, eax\n");
                    //_objProg.code.Append($"pop eax\n");
                    // offset
                    _objProg.code.Append($"imul ecx, {getSize(rightChild.token.value, rightChild)}\n");
                    _objProg.code.Append($"mov ebx, [{rightChild.token.value}]\n");
                    rightChild.token.value = $"ebx+ecx";
                }*/

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
                } else
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"cvtsi2ss xmm0, eax\n");
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
                } else
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"cvtsi2ss xmm1, eax\n");
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
                        _objProg.code.Append($"divss {leftString}, {rightString}\n");
                        break;
                    case "%":
                        _objProg.code.Append("xor edx, edx\n");
                        _objProg.code.Append("div ebx\n");
                        _objProg.code.Append("mov eax, edx\n");
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
                _objProg.stringsConsts.Append($"str_const_{stringConstsIndex} du {root.token.value}\n");
                stringConstsIndex++;
            }
        }
        public void translationChar(CommonNode root, int z_buffer)
        {
            _objProg.code.Append($"mov al, {root.token.value}");
        }
        public void translationStruct (CommonNode root, int z_buffer)
        {
            setWriteData(CodeData.macroData);
            _objProg.code.Append($"struct {root.token.value}\n");
            //for (int i = 0; i < root.childs.Count; i++)
            //{
                //CommonNode modifier = take(root, i);
                //if (modifier.token.value == "public")
                //{
                    foreach (CommonNode child in root.childs)
                    {
                        if (child.type == "VAR" && child.childs[0].type == "INDICATOR")
                        {
                            _objProg.code.Append($"    {child.token.value} dd 0\n");
                        }
                        else if (child.type == "VAR")
                        {
                            if (types.Keys.Contains(take(child, 0).token.value))
                                _objProg.code.Append($"    {child.token.value} {types[take(child, 0).token.value]} 0\n");
                            else
                                _objProg.code.Append($"    {child.token.value} {take(child, 0).token.value} 0\n");
                        }
                    }
                //}
            //}
            //_objProg.code.Append($"    sizeof_{root.token.value}:\n");
            _objProg.code.Append("ends\n");

            _objProg.data.Append($"SIZE_{root.token.value.ToUpper()} = sizeof.{root.token.value}\n");
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
            bool qsFunc = ProgramAst.qsFunction.Contains(root.token.value);
            //CommonNode types2 = take(root, 0);
            CommonNode signature = take(root, 1);
            string args = string.Empty;
            for (int i = 0; i < signature.childs.Count; i++)
            {
                if (!Compiler.types.ContainsKey(signature.childs[i].childs[0].token.value))
                    signature.childs[i].childs[0].type = "INDICATOR";
            }
            for (int i = ((qsFunc) ? 1 : 0); i < signature.childs.Count; i++)
            {
                if (i != 0 && i != ((qsFunc)?1:0)) args += " , ";
                //if (signature.childs[i].token.value == "resualtPtr")
                    //args += $"{signature.childs[i].token.value}:DWORD";
                if (typesarg.ContainsKey(signature.childs[i].childs[0].token.value))
                    args += $"{signature.childs[i].token.value}:{typesarg[signature.childs[i].childs[0].token.value]}";
                else
                    args += $"{signature.childs[i].token.value}:{signature.childs[i].childs[0].token.value}";
                    //args += $"{signature.childs[i].token.value}:DWORD";
                args += " ";
            }
            if (args.Length != 0)
                _objProg.code.Append($"proc {root.token.value} {args}\n");
            else
                _objProg.code.Append($"proc {root.token.value} \n");
            local();
            returnType = take(root, 0);
            funcName = root.token.value;
            func = true;
            if (signature.childs.Count > 0 && qsFunc)
            {
                translationVar(signature.childs[0], z_buffer + 2);
                _objProg.code.Append($"    mov [{signature.childs[0].token.value}], eax");
            }
            varSpace.OpenSpace();
            foreach (CommonNode child in signature.childs) varSpace.AddVar(child.token.value, child.childs[0]);
            Translation(take(root, 2), z_buffer + 1);
            varSpace.CloseSpace();
            func = false;
            /*
                mov edi, mc2
                mov ecx, 2
                rep movsd
             */
            //_objProg.code.Append($"    ret\n");
            if (ProgramAst.resualtFunc[root.token.value] == null || ProgramAst.resualtFunc[root.token.value].token.value == "void")
            {

            }
            else if (typesarg.Keys.Contains(ProgramAst.resualtFunc[root.token.value].token.value))
            {
                _objProg.code.Append($"{root.token.value}.return:\n");
                //_objProg.code.Append($"    test eax, eax\n");
                //_objProg.code.Append($"    jz {root.token.value}.retn\n");
                //_objProg.code.Append($"    mov eax, [resualtPtr]\n");
                //_objProg.code.Append($"    mov ebx, [resualtPtr]\n");
                //_objProg.code.Append($"    mov [ebx], eax\n");
            }
            else
            {
                _objProg.code.Append($"{root.token.value}.return:\n");
                _objProg.code.Append($"    mov edi, [resualtPtr]\n");
                _objProg.code.Append($"    test edi, edi\n");
                _objProg.code.Append($"    jz {root.token.value}.retn\n");
                //_objProg.code.Append($"    mov edi, [resualtPtr]\n");
                _objProg.code.Append($"    mov ecx, SIZE_{ProgramAst.resualtFunc[root.token.value].token.value.ToUpper()}/4\n");
                _objProg.code.Append($"    rep movsd\n");
                //_objProg.code.Append($"    lea eax, [resualtPtr]\n");
            }
            _objProg.code.Append($"{root.token.value}.retn:\n");
            //_objProg.code.Append($"    leave\n");
            _objProg.code.Append($"    ret\n");
            _objProg.code.Append($"endp\n");
            local();
            setWriteData(CodeData.codeData);
        }
        public void translationCall (CommonNode root, int z_buffer, string resualtPtr=null)
        {
            //_objProg.code.Append($"; CALL {root.token.value}\n");
            CommonNode signatureCall = take(root, 0);
            CommonNode firstArg = null;
            bool qsFunc = ProgramAst.qsFunction.Contains(root.token.value);
            if (qsFunc && signatureCall.childs.Count > 0)
            {
                firstArg = signatureCall.childs[0];
                //signatureCall.childs.RemoveAt(0);
            }
            if (ProgramAst.inlineNames.Contains(root.token.value))
            {
                //_objProg.code.Append($"{root.token.value} ");
                string args = string.Empty;
                for (int i = 0; i < signatureCall.childs.Count; i++)
                {
                    //if (signatureCall.childs[i].type == "VAR" && signatureCall.childs[i].childs.Count > 0 && signatureCall.childs[i].childs[0].type == "INDICATOR")
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
                _objProg.code.Append($"{root.token.value} {args}\n");
                return;
            }
            Console.WriteLine(root.token.value);
            if (ProgramAst.resualtFunc[root.token.value] != null) Console.WriteLine(ProgramAst.resualtFunc[root.token.value].token.value);
            for (int i = signatureCall.childs.Count - 1; i >= ((qsFunc)?1:0); i--)
            {
                Console.WriteLine($"- {signatureCall.childs[i].token.value}");
                if (signatureCall.childs[i].type == "VAR" && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[i].childs[0].token.value) && varSpace.GetType(signatureCall.childs[i].token.value).type == "INDICATOR")
                {
                    _objProg.code.Append($"mov eax, [{signatureCall.childs[i].token.value}]\n");
                    _objProg.code.Append($"push eax\n");
                }
                else if (signatureCall.childs[i].type == "VAR" && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[i].childs[0].token.value))
                {
                    _objProg.code.Append($"lea eax, [{signatureCall.childs[i].token.value}]\n");
                    _objProg.code.Append($"push eax\n");
                }
                else if (signatureCall.childs[i].type == "STRING")
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    //if (signatureCall.type == "INDICATOR")
                    //{
                    //_objProg.code.Append($"mov eax, [{stringConsts[signatureCall.childs[i].token.value]}]\n");
                    //_objProg.code.Append($"push eax\n");
                    //} else {
                    //_objProg.code.Append($"lea eax, [{stringConsts[signatureCall.childs[i].token.value]}]\n");
                    _objProg.code.Append($"push {stringConsts[signatureCall.childs[i].token.value]}\n");
                }
                else if (signatureCall.childs[i].type == "VAR" && signatureCall.childs[i].childs.Count > 0 && signatureCall.childs[i].childs[0].type == "OFFSET")
                {
                    //CommonNode offset = take(signatureCall.childs[i], 0);
                    //Translation(take(signatureCall, i), z_buffer + 1);
                    //_objProg.code.Append($"mov ebx, [{signatureCall.childs[i].token.value}]\n");
                    //signatureCall.childs[i].token.value = $"ebx+ecx";
                    //_objProg.code.Append($"pop ecx\n");
                    //_objProg.code.Append($"mov eax, [ebx+ecx]\n");
                    //_objProg.code.Append($"push eax\n");
                    Translation(signatureCall.childs[i], z_buffer + 1);
                    _objProg.code.Append($"push eax\n");
                } else
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push eax\n");
                }
            }
            Console.WriteLine("--end");
            /*if (resualtPtr!=null && ProgramAst.resualtFunc[root.token.value] != null)
            {
                _objProg.code.Append(resualtPtr);
                _objProg.code.Append($"push eax\n");
            } else if (ProgramAst.resualtFunc[root.token.value] != null)
            {
                _objProg.code.Append($"lea eax, [{getTempVarReturn(root)}]\n");
                _objProg.code.Append($"push eax\n");
            }*/
            if (resualtPtr != null)
            {
                _objProg.code.Append(resualtPtr);
                _objProg.code.Append($"push eax\n");
            }
            if (resualtPtr == null)
            {
                //_objProg.code.Append("sub ebp, 4\n");
                //_objProg.code.Append("mov eax, 0\n");
                //_objProg.code.Append($"push eax\n");
            }
            if (qsFunc && firstArg != null)
            {
                if (firstArg.type == "VAR" && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[0].childs[0].token.value) && ProgramAst.varTypes[firstArg.token.value].type == "INDICATOR")
                    _objProg.code.Append($"mov eax, [{firstArg.token.value}]\n");
                else if (firstArg.type == "VAR" && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[0].childs[0].token.value))
                    _objProg.code.Append($"lea eax, [{firstArg.token.value}]\n");
                else if (firstArg.type == "STRING")
                {
                    Translation(firstArg, z_buffer + 1);
                    _objProg.code.Append($"lea eax, [{stringConsts[firstArg.token.value]}]\n");
                }
                else if (firstArg.type == "VAR" && firstArg.childs.Count > 0 && firstArg.childs[0].type == "OFFSET")
                    Translation(firstArg, z_buffer + 1);
                else
                    Translation(firstArg, z_buffer + 1);
            }
            bool flag = false;
            foreach (var library in ProgramAst.externLibrarys.Keys)
                if (ProgramAst.externFuncs[library].Contains(root.token.value)) flag = true;
            if (flag)
            {
                _objProg.code.Append($"call [{root.token.value}]\n");
                /*int size = 0;
                foreach (var child in signatureCall.childs)
                {
                    if (child.childs[0].type == "INDICATOR") size += 4;
                    else if (child.childs[0].token.value == "int8" || child.childs[0].token.value == "char" || child.childs[0].token.value == "string") size += 1;
                    else if (child.childs[0].token.value == "int16" || child.childs[0].token.value == "short") size += 2;
                    else if (child.childs[0].token.value == "byte") size += 1;
                    else size += 4;
                }*/
                //_objProg.code.Append($"add esp, {size}\n");
            }
            else _objProg.code.Append($"call {root.token.value}\n");
            //if (ProgramAst.resualtFunc[root.token.value] != null && resualtPtr == null) _objProg.code.Append($"mov eax, [{getTempVarReturn(root)}]\n");
            
        }

        public void translationOffset(CommonNode varNode, CommonNode rightNode, int z_buffer)
        {
            Translation(rightNode, z_buffer + 1);
            _objProg.code.Append($"push eax\n");

            CommonNode offset = varNode.childs[0];
            Translation(offset.childs[0], z_buffer);
            _objProg.code.Append($"mov ecx, eax\n");
            _objProg.code.Append($"imul ecx, {getSize(varNode.token.value, varNode)}\n");

            _objProg.code.Append($"mov ebx, [{varNode.token.value}]\n");
            _objProg.code.Append($"pop eax\n");
            _objProg.code.Append($"mov [ebx+ecx], eax\n");
        }
        public string getTempVarReturn (CommonNode call)
        {
            CommonNode type = ProgramAst.resualtFunc[call.token.value];
            if (tempStructs.Contains($"temp_struct_{type.token.value}"))
            {
                return $"temp_struct_{type.token.value}";
            }
            else
            {
                _objProg.data.Append($"temp_struct_{tempTagIndex} {type.token.value} 0\n");
                tempStructs.Add($"temp_struct_{tempTagIndex}");
                return $"temp_struct_{tempTagIndex}";
            }
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
        public string getSize(string name, CommonNode tagError)
        {
            CommonNode type;
            if (name.First() == '*') name.Remove(0, 1);
            if (name.Contains(".") || name.Contains(","))
            {
                int n = 0;
                string[] strs = name.Split('.', ',');
                string strct = varSpace.GetType(strs[0]).token.value;
                start:
                if (
                    !ProgramAst.structs.ContainsKey(strct) ||
                    !ProgramAst.structs[strct].ContainsKey(strs[n+1])
                    )
                {
                    Syntax.SyntaxError($"Ошибка Поле:{strs[n+1]} не существует в {strct}", tagError);
                }
                type = ProgramAst.structs[strct][strs[n+1]];
                strct = type.token.value;
                //if (n == strs.Length) { }
                if (!types.ContainsKey(type.token.value)) { n++; goto start; } // strct = ProgramAst.structs[strct][strs[n]].token.value; 
            } else type = varSpace.GetType(name);
            //CommonNode type = ProgramAst.varTypes[name];
            string classes = "";
            string sizeConst = "";
            if (types.Keys.Contains(type.token.value))
            {
                classes = types[type.token.value];
                switch (classes)
                {
                    case "db": sizeConst = "1"; break;
                    case "dw": sizeConst = "2"; break;
                    case "dd": sizeConst = "4"; break;
                }
            }
            else sizeConst = $"SIZE_{type.token.value.ToUpper()}";
            //classes = type.token.value;
            //string sizeConst = $"sizeof.{name}";
            return sizeConst;
        }

        /*public string getSize(string name)
        {
            CommonNode type;
            string namePtr;
            if (name.Contains("."))
            {
                int n = 0;
                string[] strs = name.Split('.');
                string strct = ProgramAst.varTypes[strs[0]].token.value;
            start:
                if (
                    !ProgramAst.structs.ContainsKey(strct) ||
                    !ProgramAst.structs[strct].ContainsKey(strs[n + 1])
                    )
                {
                    Syntax.SyntaxError($"Ошибка Поле:{strs[n + 1]} не существует в {strct}", ProgramAst);
                }
                type = ProgramAst.structs[strct][strs[n + 1]];
                strct = type.token.value;
                //if (n == strs.Length) { }
                if (!types.ContainsKey(type.token.value)) { n++; goto start; } // strct = ProgramAst.structs[strct][strs[n]].token.value; 
            }
            else type = ProgramAst.varTypes[name];
            //CommonNode type = ProgramAst.varTypes[name];
            //string classes = "";
            //string sizeConst = "";
            if (types.Keys.Contains(type.token.value))
            {
                //classes = types[type.token.value];
                /*switch (classes)
                {
                    case "db": sizeConst = "1"; break;
                    case "dw": sizeConst = "2"; break;
                    case "dd": sizeConst = "4"; break;
                }
                return name;
            }
            else
            {
                return  type.token.value;
            }
            //sizeConst = $"SIZE_{type.token.value.ToUpper()}";
            //classes = type.token.value;
            //string sizeConst = $"sizeof.{name}";
            return sizeConst;
        }*/

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
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    Console.WriteLine($"<< {types.ElementAt(i)} | {typesarg.ElementAt(i)} | {aligns.ElementAt(i)}");
                }
            } catch { }
            _objProg = PostGen.PostTranslation(_objProg, ProgramAst);
            //_objProg = CrossCompiler.Compile(_objProg);
            string file = string.Empty;
            file += _objProg.includes.ToString();
            file += "\n";
            file += _objProg.macroData.ToString();
            if (_objProg.data.Length != 0 || _objProg.stringsConsts.Length != 0)
                file += "\nsection '.data' data readable writable\n";
            file += _objProg.stringsConsts.ToString();
            file += _objProg.data.ToString();
            //if (_objProg.procData.Length != 0)
                file += "\nsection '.code' code readable executable\n";
            file += _objProg.procData.ToString();
            file += "ret\n";
            // section '.code' code readable executable
            // section '.data' data readable writable

            ///if (typeApp == TypeApp.h)
            //{
            //file += "start: ;START MAIN\n";
            //file += _objProg.codeData.ToString();
            //return file;
            ///}
            ///
            Console.ReadLine();
            if (typeApp == TypeApp.gui)
            {
                file = "format PE GUI 4.0\n\nentry start\n" + file;
                file += "start: ;START MAIN\n";
                file += _objProg.codeData.ToString();
            }
            else if (typeApp == TypeApp.asmmodule)
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
            }
            else if (typeApp == TypeApp.program64)
            {
                file = "format PE console\n\nentry start\n" + file;
                //file += "\nsection '.code' code readable executable\n";
                file += "start: ;START MAIN\n";
                file += _objProg.codeData.ToString();
            }
            if (TypeApp.asmmodule != typeApp && ProgramAst.externLibrarys.Count > 0)
            {
                file += "section '.idata' import data readable\n";
                string libraryInclude = "library ";
                foreach (var library in ProgramAst.externLibrarys)//for (int i = 0; i < ProgramAst.externLibrarys.Count; i++)
                {
                    libraryInclude += $"    {library.Key}, '{library.Value}'";
                    if (library.Key != ProgramAst.externLibrarys.Last().Key) libraryInclude += ",\\ \n";
                }
                file += libraryInclude;
                string funcsString = string.Empty;
                foreach (var funcs in ProgramAst.externFuncs)
                {
                    if (funcs.Value.Count == 0) continue;
                    funcsString += $"\nimport {funcs.Key},\\ \n";
                    foreach (var library in ProgramAst.externFuncs[funcs.Key])//for (int i = 0; i < ProgramAst.externLibrarys.Count; i++)
                    {
                        funcsString += $"    {library}, '{library}'";
                        if (library != ProgramAst.externFuncs[funcs.Key].Last()) funcsString += ",\\ \n";
                    }
                }
                file += funcsString + "\n";
            }
            if (ProgramAst.sectionNodes.Count != 0) file += "section '.data' data readable writable\n";
            foreach (CommonNode section in ProgramAst.sectionNodes)
            {
                StringBuilder chars = new StringBuilder();
                char[] strs = section.token.value.ToCharArray();
                string str = section.token.value;
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

                string[] strings = temp.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < strings.Length; i++)
                    file += strings[i] + "\n";

            }
            return file;
        }
        public void WriteCode (string data, string nameFile, string pathCompile, string extend = "asm")
        {
            string path = $"{pathCompile}\\bin\\{nameFile}.{extend}";
            //File.Delete(path);
            Console.WriteLine($"{pathCompile}\\bin\\");
            Directory.CreateDirectory($"{pathCompile}\\bin\\");
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

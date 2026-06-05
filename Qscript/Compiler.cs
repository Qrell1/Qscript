using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Qscript
{
    public enum CodeData
    { 
        codeData, procData, macroData, tempData
    }
    public enum TypeApp
    {
        dll, program32, program64, asmmodule, h, gui, bin
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


        public Dictionary<string, string> stringConsts = new Dictionary<string, string>();
        public int stringConstsIndex;

        private Dictionary<string, string> consts = new Dictionary<string, string>();
        private int constsIndex;

        public Dictionary<string, string> floatConsts = new Dictionary<string, string>();
        public int floatConstsIndex;

        public Dictionary<string, string> varRegisters = new Dictionary<string, string>();

        public Dictionary<string, string> inlineArgs = new Dictionary<string, string>();

        public List<string> tempStructs = new List<string>();

        public CommonNode returnType;
        public string funcName;
        public bool func;

        private int trueTagIndex;
        private int falseTagIndex;
        private int elsesTagIndex;
        private int tempTagIndex;

        private int iterTagIndex;
        private long regIndex = 1000;
        private string regPrefer;
        private string regReturn;

        private bool regString;
        private string offsetTemp;
        private string[] regTemp;

        private StringData tempData = new StringData();
        private CodeData codeData = CodeData.codeData;

        public Compiler(string _fasmCompilerPath, ProgramNode ast) { fasmCompilerPath = _fasmCompilerPath; ProgramAst = ast; varSpace.VarsData = ast.varTypes; }


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
            if (data == CodeData.tempData)
            {
                _objProg.code = tempData;
            }
            codeData = data;
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
                case NT.ROOT:
                    setWriteData(CodeData.codeData);
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        CommonNode child = root.childs[i];
                        Translation(child, z_buffer + 1);
                    }
                    break;
                case NT.OFFSET:
                    translationOffset(root, z_buffer);
                    break;
                case NT.VAR:
                    translationVar(root, z_buffer);
                    break;
                case NT.BINOPER:
                    translationBinOper(root, z_buffer);
                    break;
                case NT.FLOATBINOPER:
                    translationFloatBinOper(root, z_buffer);
                    break;
                case NT.NUMBER:
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {root.token.value}\n");
                    regReturn = $".reg{regIndex-1}{regPrefer}";
                    break;
                case NT.BOOL:
                    char boolChar = (root.token.value == "true") ? '1' : '0'; _objProg.code.Append($"mov .reg{regIndex++}, {boolChar}\n");
                    regReturn = $".reg{regIndex-1}";
                    break;
                case NT.CONST:
                    translationConst(root, z_buffer);
                    break;
                case NT.STRING:
                    translationString(root, z_buffer);
                    break;
                case NT.CHAR:
                    translationChar(root, z_buffer);
                    break;
                case NT.FUNC:
                    translationFunc(root, z_buffer);
                    break;
                case NT.INLINE:
                    translationInline(root, z_buffer);
                    break;
                case NT.ASMINLINE:
                    translationAsmInline(root, z_buffer);
                    break;
                case NT.STRUCT:
                    translationStruct(root, z_buffer);
                    break;
                case NT.ALLOCMEMSTATICOBJECT:
                    Translation(take(root, 0), z_buffer + 1);
                    break;
                case NT.CALL:
                    translationCall(root, z_buffer);
                    break;
                case NT.CMP:
                    translationCmp(root, z_buffer);
                    break;
                case NT.IF:
                    translationIf(root, z_buffer);
                    break;
                case NT.ELSEIF:
                    translationIf(root, z_buffer);
                    break;
                case NT.ELSE:
                    translationElse(root, z_buffer);
                    break;
                case NT.ITER:
                    translationIter(root, z_buffer);
                    break;
                case NT.FOR:
                    translationFor(root, z_buffer);
                    break;
                case NT.WHILE:
                    translationWhile(root, z_buffer);
                    break;
                case NT.ENUMERATOR:
                    translationEnumerator(root, z_buffer);
                    break;
                case NT.REPT:
                    translationRept(root, z_buffer);
                    break;
                case NT.RETURN:
                    translationReturn(root, z_buffer);
                    break;
                case NT.REFVAR:
                    translationRefVar(root, z_buffer);
                    break;
                case NT.SIZEOF:
                    translationSizeof(root, z_buffer);
                    break;
                case NT.TYPEOF:
                    translationTypeof(root, z_buffer);
                    break;
                case NT.ADDRESS:
                    translationAddress(root, z_buffer);
                    break;
                case NT.PREUNAROPER:
                    translationPreUnarOper(root, z_buffer);
                    break;
                case NT.POSTUNAROPER:
                    translationPostUnarOper(root, z_buffer);
                    break;
                case NT.TAG:
                    _objProg.code.Append($"{root.token.value}:\n");
                    break;
                case NT.JMP:
                    _objProg.code.Append($"jmp {take(root, 0).token.value}\n");
                    break;
                case NT.REGDECL:
                    translationRegDeclaration(root, z_buffer);
                    break;
                case NT.REGUSE:
                    translationRegUses(root, z_buffer);
                    break;
                case NT.TYPE:
                case NT.TYPEOPER:
                    Translation(root.childs[0], z_buffer + 1);
                    break;
                case NT.BREAK:
                    translationBreak(root, z_buffer);
                    break;
                case NT.CONTINUE:
                    translationContinue(root, z_buffer);
                    break;
                case NT.BODY:
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        Translation(root.childs[i], z_buffer + 1);
                    }
                    break;
                case NT.ASM:
                    parseAsm(root);
                    break;
                case NT.USING:
                    _objProg.includes.Append($"include '{take(root,0).token.value}'\n");
                    break;
            }
        }
        private void translationBreak(CommonNode root, int z_buffer)
        {
            string pre = (funcName != "") ? funcName + "." : "";
            _objProg.code.Append($"jmp break{pre}iter{iterTagIndex}");
        }
        private void translationContinue(CommonNode root, int z_buffer)
        {
            string pre = (funcName != "") ? funcName + "." : "";
            _objProg.code.Append($"jmp continue{pre}iter{iterTagIndex}");
        }
        private string[] translationOffset(CommonNode root, int z_buffer, bool flag = true)
        {
            CommonNode exprNode = root.childs[0];
            CommonNode varNode = root.childs[1];

            offsetTemp = string.Empty;
            string type = string.Empty;
            string size = string.Empty;
            string varString = string.Empty;

            if (varNode.type == NT.VAR)
                varString = $"[{varNode.token.value}]";
            else
            {
                Translation(varNode, z_buffer+1);
                varString = regReturn;
            }
            if (offsetTemp != "")
                type = offsetTemp;
            else switch (DataBase.getFormulaNodeSize(varNode, ref varSpace, ref ProgramAst))
            {
                case 8: size = "qword"; break;
                case 4: size = "dword"; break;
                case 2: size = "word"; break;
                case 1: size = "byte"; break;
                default: size = "dword"; break;
            }

            Translation(exprNode, z_buffer+1);
            string reg = regReturn;

            type = (type != "") ? type : getSize(varNode.token.value, varNode);
            _objProg.code.Append($"imul {reg}, {type}\n");
            _objProg.code.Append($"mov .reg{regIndex++}, {varString}\n");


            if (flag)
            {
                _objProg.code.Append($"mov .reg{regIndex++}, {size} [.reg{regIndex-2}+{reg}]\n");
                regReturn = $".reg{regIndex-1}";
                offsetTemp = size;
            }
            else
            {
                regReturn = $"{size} [.reg{regIndex-1}+{reg}]";
                return new string[] { $".reg{regIndex - 1}", reg };
            }
            offsetTemp = type;
            return null;
        }
        private void translationUseAddressVar (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            _objProg.code.Append($"mov .reg{regIndex++}, [{varNode.token.value}]");
            regReturn = $".reg{regIndex-1}";
        }
        private void translationRegDeclaration  (CommonNode root, int z_buffer)
        {
            varRegisters.Add(root.token.value, $".reg{regIndex++}");
        }
        private void translationRegUses (CommonNode root, int z_buffer)
        {
            regReturn = varRegisters[root.token.value];
        }
        private void translationWhile (CommonNode root, int z_buffer)
        {
            CommonNode cmpNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);
            
            int iterNumber = ++iterTagIndex;
            string pre = (funcName != "") ? funcName + "." : "";

            _objProg.code.Append($"{pre}iter{iterNumber}:\n");

            Translation(bodyNode, z_buffer + 1);
            _objProg.code.Append($"continue{pre}iter{iterNumber}:");
            Translation(cmpNode, z_buffer + 1);
            _objProg.code.Append($"jmp {pre}iter{iterNumber}\n");
            _objProg.code.Append($"{funcName}.false{falseTagIndex}:\n");
            falseTagIndex++;

            if (isNodeType(NT.BREAK, bodyNode)) _objProg.code.Append($"breaK{pre}iter{iterNumber}:");
        }
        private void translationFor (CommonNode root, int z_buffer)
        {
            CommonNode initNode = take(root, 0);
            CommonNode cmpNode = take(root, 1);
            CommonNode stepNode = take(take(root, 2),0);
            CommonNode bodyNode = take(root, 3);

            int iterNumber = ++iterTagIndex;
            string pre = (funcName != "") ? funcName + "." : "";

            Translation(initNode, z_buffer + 1);
            _objProg.code.Append($"{pre}iter{iterNumber}:\n");

            Translation(bodyNode, z_buffer + 1);

            Translation(stepNode, z_buffer + 1);
            _objProg.code.Append($"continue{pre}iter{iterNumber}:");
            Translation(cmpNode, z_buffer + 1);
            _objProg.code.Append($"jmp {pre}iter{iterNumber}\n");
            _objProg.code.Append($"false{falseTagIndex}:\n");
            falseTagIndex++;

            if (isNodeType(NT.BREAK, bodyNode)) _objProg.code.Append($"break{pre}iter{iterNumber}:");
            //iterTagIndex++;
        }
        private void translationRept (CommonNode root, int z_buffer)
        {
            CommonNode signatureNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            _objProg.code.Append($"rept {signatureNode.token.value} {{\n");
            Translation(bodyNode, z_buffer + 1);
            _objProg.code.Append("}");
        }
        private void translationEnumerator (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            CommonNode countNode = take(root, 1);
            CommonNode bodyNode = take(root, 2);

            int iterNumber = ++iterTagIndex;
            string pre = (funcName != "") ? funcName + "." : "";
            translationVar(varNode, z_buffer + 1);


            string countString = "ecx";
            switch (countNode.type)
            {
                case NT.NUMBER: countString = countNode.token.value; break;
                case NT.VAR: _objProg.code.Append($"mov ecx, [{countNode.token.value}]\n"); countString = $"[{countNode.token.value}]"; break;
                case NT.FLOATBINOPER: Syntax.SyntaxError("Невозможно использовать флотовую операцию в качестве числа енумераций!", countNode); break;
                case NT.FLOAT: Syntax.SyntaxError("Невозможно использовать флотовое число в качестве числа енумераций!", countNode); break;
                default: Translation(countNode, z_buffer + 1); _objProg.code.Append($"mov ecx, eax\n"); break;
            }
            if (countNode.type == NT.NUMBER && Convert.ToInt32(countString) <= 0) return;
            /*if (countNode.type == NT.NUMBER && Convert.ToInt32(countNode.token.value) <= 10 && totalNodes(bodyNode) <= 32) // plan 1
            {
                for (int i = 0; i < Convert.ToInt32(countNode.token.value); i++)
                {
                    Translation(bodyNode, z_buffer + 1);
                }
            }
            else if (countNode.type == NT.NUMBER && Convert.ToInt32(countNode.token.value) % 3 == 0 && totalNodes(bodyNode) <= 32)
            {
                int newIter = Convert.ToInt32(countNode.token.value) / 3;
                bool isVarUse = isNodeValue(varNode.token.value, bodyNode);

                string reg = $".reg{regIndex++}safe";
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");
                //_objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                if (isVarUse) _objProg.code.Append($"inc [{varNode.token.value}]\n");
                Translation(bodyNode, z_buffer + 1);
                if (isVarUse) _objProg.code.Append($"inc [{varNode.token.value}]\n");
                Translation(bodyNode, z_buffer + 1);
                //_objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                if (isVarUse) _objProg.code.Append($"mov [{varNode.token.value}], {reg}]\n");
                _objProg.code.Append($"cmp {reg}, {newIter}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
            }
            else if (countNode.type == NT.NUMBER && Convert.ToInt32(countNode.token.value) % 2 == 0 && totalNodes(bodyNode) <= 32)
            {
                int newIter = Convert.ToInt32(countNode.token.value) / 2;
                bool isVarUse = isNodeValue(varNode.token.value, bodyNode);
                string reg = $".reg{regIndex++}safe";

                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");
                //_objProg.code.Append($"push {reg}\n");
                Translation(bodyNode, z_buffer + 1);
                if (isVarUse) _objProg.code.Append($"inc [{varNode.token.value}]\n");
                Translation(bodyNode, z_buffer + 1);
                //_objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                if (isVarUse) _objProg.code.Append($"mov [{varNode.token.value}], {reg}\n");
                _objProg.code.Append($"cmp {reg}, {newIter}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
            }*/
            else if (countNode.type == NT.NUMBER)
            {
                bool isVarUse = isNodeValue(varNode.token.value, bodyNode);
                string reg = $".reg{regIndex++}safe";

                _objProg.code.Append($"xor {reg}, {reg}\n");
                if (isVarUse) _objProg.code.Append($"mov [{varNode.token.value}], 0\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");
                //_objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                //_objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"continue{pre}iter{iterNumber}:");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                if (isVarUse) _objProg.code.Append($"mov [{varNode.token.value}], {reg}\n");
                _objProg.code.Append($"cmp {reg}, {countString}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }
            else if (countNode.type == NT.VAR)
            {
                string reg = $".reg{regIndex++}safe";

                _objProg.code.Append($"mov {reg}, {countString}\n");
                _objProg.code.Append($"mov [{varNode.token.value}], 0\n");
                _objProg.code.Append($"cmp {reg}, 0\n");
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");

                //_objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                //_objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"continue{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                _objProg.code.Append($"mov [{varNode.token.value}], {reg}\n");

                _objProg.code.Append($"cmp {reg}, {countString}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }
            else
            {
                string reg = $".reg{regIndex++}safe";
                string regVar = $".reg{regIndex++}safe";

                Translation(countNode, z_buffer + 1);
                _objProg.code.Append($"mov [{varNode.token.value}], 0\n");
                _objProg.code.Append($"cmp {regVar}, 0\n");
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");

                //_objProg.code.Append($"push ecx\n");
                Translation(bodyNode, z_buffer + 1);
                //_objProg.code.Append($"pop ecx\n");
                _objProg.code.Append($"continue{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                Translation(countNode, z_buffer + 1);

                _objProg.code.Append($"mov [{varNode.token.value}], {reg}\n");
                _objProg.code.Append($"cmp {reg}, {regVar}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }
        }
        private void translationPreUnarOper (CommonNode root, int z_buffer)
        {
            if (root.token.value == "-")
            {
                CommonNode exprNode = take(root, 0);

                Translation(exprNode, z_buffer + 1);
                _objProg.code.Append($"neg {regReturn}\n");
                return;
            }
            else if (root.token.value == "!")
            {
                CommonNode exprNode = take(root, 0);

                Translation(exprNode, z_buffer + 1);
                _objProg.code.Append($"xor {regReturn}, 1\n");
                return;
            }
            CommonNode varNode = take(root, 0);
            string oper = (root.token.value == "++") ? "inc" : "dec";
            _objProg.code.Append($"{oper} [{varNode.token.value}]\n");
            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{varNode.token.value}]\n"); // if (mov) 
            regReturn = $".reg{regIndex - 1}{regPrefer}";
        }
        private void translationPostUnarOper (CommonNode root, int z_buffer)
        {
            if (root.token.value == "-")
            {
                CommonNode exprNode = take(root, 0);

                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{exprNode.token.value}]\n");
                _objProg.code.Append($"neg .reg{regIndex-1}{regPrefer}\n");
                _objProg.code.Append($"mov [{exprNode.token.value}], .reg{regIndex-1}{regPrefer}\n");
            }
            else if (root.token.value == "!")
            {
                CommonNode exprNode = take(root, 0);

                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{exprNode.token.value}]\n");
                _objProg.code.Append($"not .reg{regIndex-1}{regPrefer}\n");
                _objProg.code.Append($"mov [{exprNode.token.value}], .reg{regIndex-1}{regPrefer}\n");
            }
            CommonNode varNode = take(root, 0);
            string oper = (root.token.value == "++") ? "inc" : "dec";
            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{varNode.token.value}]\n");
            _objProg.code.Append($"{oper} [{varNode.token.value}]\n");
            regReturn = $".reg{regIndex - 1}{regPrefer}";
        }
        private void translationAddress (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            _objProg.code.Append($"lea .reg{regIndex++}{regPrefer}, [{varNode.token.value}]\n");
            regReturn = $".reg{regIndex-1}{regPrefer}";
        }
        private void translationSizeof (CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            string size = getSize(varNode.token.value, varNode);
            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {size}\n");
            regReturn = $".reg{regIndex-1}{regPrefer}";
        }
        private void translationTypeof(CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);

            regReturn = $".reg{regIndex+1}{regPrefer}";
            if (DataBase.types.ContainsKey(varNode.token.value))
            {
                switch (DataBase.types[varNode.token.value])
                {
                    case "dq": _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, 8\n"); return;
                    case "dd": _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, 4\n"); return;
                    case "dw": _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, 2\n"); return;
                    case "db": _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, 1\n"); return;       
                    case "du": _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, 2\n"); return;
                }
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {DataBase.aligns["long"]}\n");
            }
            else { _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, SIZE_{varNode.token.value.ToUpper()}\n"); }
        }
        private void translationRefVar (CommonNode root, int z_buffer)
        {
            CommonNode call = take(root, 0);
            _objProg.code.Append($"sub esp, SIZE_{ProgramAst.resualtFunc[call.token.value].token.value.ToUpper()}\n");
            translationCall(call, z_buffer + 1, "mov eax, esp\n");
            _objProg.code.Append($"mov eax, [esp+{ProgramAst.resualtFunc[call.token.value].token.value}.{root.token.value}-4]\n");
            //_objProg.code.Append($"mov [eax], [esp-{ProgramAst.resualtFunc[call.token.value].token.value}.{root.token.value}]\n");
            _objProg.code.Append($"add esp, SIZE_{ProgramAst.resualtFunc[call.token.value].token.value.ToUpper()}\n");
        } // TODO: Переделать регистры на .reg
        private void translationReturn (CommonNode root, int z_buffer)
        {
            if (root.childs.Count == 0)
            {
                _objProg.code.Append($"jmp {funcName}.retn\n");
                return;
            }
            CommonNode returnValue = take(root, 0);


            if (!DataBase.typesarg.Keys.Contains(returnType.token.value))
            {
                Translation(returnValue, z_buffer + 1);
                _objProg.code.Append($"mov esi, {regReturn}\n");
            } else if (returnType.type == NT.VAR)
            {
                Translation(returnValue, z_buffer + 1);
                _objProg.code.Append($"lea .reg{regIndex++}eax, [{returnValue.token.value}]\n");
                //_objProg.code.Append($"mov esi, eax\n");
            } else
            {
                regPrefer = "eax";
                Translation(returnValue, z_buffer + 1);
                regPrefer = "";
                _objProg.code.Append($"mov eax, {regReturn}\n");
                //_objProg.code.Append($"mov esi, eax\n");
            }
            _objProg.code.Append($"jmp {funcName}.return\n");
        }
        private void translationIter (CommonNode root, int z_buffer)
        {
            CommonNode countNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);
            int iterNumber = ++iterTagIndex;
            string pre = (funcName != "") ? funcName + "." : "";
            if (countNode.type == NT.NUMBER && Convert.ToInt32(countNode.token.value) <= 0) return;

            /*else if (countNode.type == NT.NUMBER
                && Convert.ToInt32(countNode.token.value) >= 16
                && Convert.ToInt32(countNode.token.value) % 4 == 0 && totalNodes(bodyNode) <= 32)
            {
                int newIter = Convert.ToInt32(countNode.token.value) / 4;
                string reg = $".reg{regIndex++}safe";
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");

                Translation(bodyNode, z_buffer + 1);
                Translation(bodyNode, z_buffer + 1);
                Translation(bodyNode, z_buffer + 1);
                Translation(bodyNode, z_buffer + 1);

                _objProg.code.Append($"continue{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                _objProg.code.Append($"cmp {reg}, {newIter}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");

                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }
            else if (countNode.type == NT.NUMBER
                && Convert.ToInt32(countNode.token.value) >= 16
                && Convert.ToInt32(countNode.token.value) % 2 == 0
                && totalNodes(bodyNode) <= 32)
            {
                int newIter = Convert.ToInt32(countNode.token.value) / 2;
                string reg = $".reg{regIndex++}safe";
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"push {reg}\n");

                Translation(bodyNode, z_buffer + 1);
                Translation(bodyNode, z_buffer + 1);

                _objProg.code.Append($"continue{pre}iter{iterNumber}:\n");
                _objProg.code.Append($"pop {reg}\n");
                _objProg.code.Append($"inc {reg}\n");
                _objProg.code.Append($"cmp {reg}, {newIter}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }*/
            else
            { // {reg}
                string reg = $".reg{regIndex++}safe";
                string reg2 = string.Empty;
                if (countNode.type == NT.NUMBER)
                    reg2 = countNode.token.value;
                else
                {
                    Translation(countNode, z_buffer + 1);
                    reg2 = regReturn + "safe";
                }
                _objProg.code.Append($"xor {reg}, {reg}\n");
                _objProg.code.Append($"{pre}iter{iterNumber}:\n");

                _objProg.code.Append($"push {reg}\n");
                if (countNode.type != NT.NUMBER) _objProg.code.Append($"push {reg2}\n");

                Translation(bodyNode, z_buffer + 1);

                _objProg.code.Append($"continue{pre}iter{iterNumber}:\n");
                if (countNode.type != NT.NUMBER) _objProg.code.Append($"pop {reg2}\n");
                _objProg.code.Append($"pop {reg}\n");

                _objProg.code.Append($"inc {reg}\n");
                _objProg.code.Append($"cmp {reg}, {reg2}\n");
                _objProg.code.Append($"jne {pre}iter{iterNumber}\n");
                if (isNodeType(NT.BREAK, bodyNode))
                {
                    _objProg.code.Append($"jmp nop{pre}iter{iterNumber}");
                    _objProg.code.Append($"break{pre}iter{iterNumber}:");
                    if (countNode.type != NT.NUMBER) _objProg.code.Append($"pop {reg2}\n");
                    _objProg.code.Append($"pop {reg}\n");
                    _objProg.code.Append($"nop{pre}iter{iterNumber}:");
                }
            }
        }
        private void translationIf (CommonNode root, int z_buffer)
        {
            CommonNode cmp = take(root, 0);
            CommonNode body = take(root, 1);
            CommonNode elses = take(root, 2);
            translationCmp(cmp, z_buffer + 1);
            Translation(body, z_buffer + 1);
            if (elses != null)
            {
                _objProg.code.Append($"jmp {funcName}.elses{elsesTagIndex}\n");
                _objProg.code.Append($"{funcName}.false{falseTagIndex}:\n");
                falseTagIndex++;
                Translation(elses.childs[0], z_buffer + 2);
                _objProg.code.Append($"{funcName}.elses{elsesTagIndex}:\n");
                elsesTagIndex++;
            }
            else
            {
                _objProg.code.Append($"{funcName}.false{falseTagIndex}:\n");
                falseTagIndex++;
            }
        }
        private void translationElse (CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 0);
            Translation(body, z_buffer + 1);
        }
        private void translationCmp (CommonNode root, int z_buffer, bool cmp=false)
        {
            if (root.childs.Count == 1 && root.childs[0].token.value == "true")
            {
                if (cmp) _objProg.code.Append($"jmp {funcName}.true{trueTagIndex}\n");
                return;
            }
            if (root.childs.Count == 1 && root.childs[0].token.value == "false")
            {
                _objProg.code.Append($"je {funcName}.false{falseTagIndex}");
                return;
            }
            if (root.childs.Count == 1)
            {
                Translation(root.childs[0], z_buffer + 1);
                _objProg.code.Append($"cmp {regReturn}, 0");
                _objProg.code.Append($"je {funcName}.false{falseTagIndex}");
                if (cmp) _objProg.code.Append($"jmp {funcName}.true{trueTagIndex}\n");
                return;
            }

            if ("true" == root.token.value)
            {
                if (cmp) _objProg.code.Append($"jmp {funcName}.true{trueTagIndex}\n");
                return;
            }
            if ("false" == root.token.value)
            {
                _objProg.code.Append($"jmp {funcName}.false{falseTagIndex}\n");
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

                _objProg.code.Append($"jmp {funcName}.false{falseTagIndex}\n");
                _objProg.code.Append($"{funcName}.true{trueTagIndex}:\n");
                trueTagIndex++;
            }
            if (new string[] { "==", "!=", ">=", "<=", "<", ">" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);
                string floatVar;
                string floatVar2;

                _objProg.code.Append($"; CMP\n");

                if (leftChild.type == NT.NUMBER && rightChild.type == NT.NUMBER)
                {
                    string reg = $".reg{regIndex++}";
                    _objProg.code.Append($"mov {reg}, {leftChild.token.value}\n");
                    _objProg.code.Append($"cmp {reg}, {rightChild.token.value}\n");
                }
                if (leftChild.type == NT.FLOAT && rightChild.type == NT.FLOAT)
                {
                    string reg = $".reg{regIndex++}";
                    floatVar = getFloatConst(leftChild);
                    floatVar2 = getFloatConst(rightChild);
                    _objProg.code.Append($"mov {reg}, [{floatVar}]\n");
                    _objProg.code.Append($"cmp {reg}, [{floatVar2}]\n");
                }
                else if (rightChild.type == NT.NUMBER)
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"cmp {regReturn}, {rightChild.token.value}\n");
                }
                else if (rightChild.type == NT.FLOAT)
                {
                    Translation(leftChild, z_buffer + 1);
                    floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"cmp {regReturn}, [{floatVar}]\n");
                } else if (rightChild.type == NT.BOOL)
                {
                    Translation(leftChild, z_buffer + 1);
                    char boolChar = (rightChild.token.value == "true") ? '1' : '0';
                    _objProg.code.Append($"mov .reg{regIndex++}, {boolChar}\n");
                    _objProg.code.Append($"cmp {regReturn}, .reg{regIndex-1}\n");
                } else if (rightChild.type == NT.STRING)
                {
                    Translation(leftChild, z_buffer + 1);
                    translationString(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov .reg{regIndex++}, {stringConsts[rightChild.token.value]}\n");
                    _objProg.code.Append($"cmp {regReturn}, .reg{regIndex-1}\n");
                }
                else
                {
                    Translation(leftChild, z_buffer + 1);
                    string _regReturn1 = regReturn;
                    Translation(rightChild, z_buffer + 1);
                    string _regReturn2 = regReturn;
                    _objProg.code.Append($"cmp {_regReturn1}, {_regReturn2}\n");
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
                            _objProg.code.Append($"jne {funcName}.false{falseTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"je {funcName}.false{falseTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jl {funcName}.false{falseTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jg {funcName}.false{falseTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jle {funcName}.false{falseTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jge {funcName}.false{falseTagIndex}\n");
                            break;
                    }
                } else
                {
                    switch (root.token.value)
                    {
                        case "==":
                            _objProg.code.Append($"je {funcName}.true{trueTagIndex}\n");
                            break;
                        case "!=":
                            _objProg.code.Append($"jne {funcName}.true{trueTagIndex}\n");
                            break;
                        case ">=":
                            _objProg.code.Append($"jge {funcName}.true{trueTagIndex}\n");
                            break;
                        case "<=":
                            _objProg.code.Append($"jle {funcName}.true{trueTagIndex}\n");
                            break;
                        case ">":
                            _objProg.code.Append($"jg {funcName}.true{trueTagIndex}\n");
                            break;
                        case "<":
                            _objProg.code.Append($"jl {funcName}.true{trueTagIndex}\n");
                            break;
                    }
                }

                //_objProg.code.Append($"false{falseTagIndex}:");
                //falseTagIndex++;
                //if (leftChild.type == NT.VAR && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                //_objProg.code.Append($"mov [{leftChild.token.value}], eax\n");
            }
        }
        private (string, string) translationLeftRightNodes (CommonNode leftChild, CommonNode rightChild, int z_buffer)
        {
            string regReturn1 = string.Empty;
            string regReturn2 = string.Empty;
            if (leftChild.type == NT.NUMBER && rightChild.type == NT.NUMBER)
            {
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {leftChild.token.value}\n");
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {rightChild.token.value}\n");
                return ($".reg{regIndex - 2}{regPrefer}", $".reg{regIndex - 1}{regPrefer}");
            }
            else if (rightChild.type == NT.NUMBER)
            {
                Translation(leftChild, z_buffer + 1);
                regReturn1 = regReturn;
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {rightChild.token.value}\n");
                return (regReturn1, $".reg{regIndex-1}{regPrefer}");
            }
            else
            {
                Translation(leftChild, z_buffer + 1);
                //_objProg.code.Append($"mov .reg{regIndex++}, {regReturn}\n");
                _objProg.code.Append($"push {regReturn}\n");
                regReturn1 = $".reg{regIndex++}";

                Translation(rightChild, z_buffer + 1);
                regReturn2 = regReturn;
                _objProg.code.Append($"pop {regReturn1}\n");
                return (regReturn1, regReturn2);
            }
        }
        private void translationVar (CommonNode root,  int z_buffer)
        {
            if (root.childs.Count > 0 && root.childs[0].type == NT.OFFSET)
            {
                CommonNode offset = root.childs[0];
                Translation(offset.childs[0], z_buffer);
                string reg = regReturn;

                _objProg.code.Append($"imul {reg}, {getSize(root.token.value, root)}\n");
                _objProg.code.Append($"mov .reg{regIndex++}, [{root.token.value}]\n");
                _objProg.code.Append($"mov .reg{regIndex++}, [.reg{regIndex-2}+{reg}]\n");
                regReturn = $".reg{regIndex-1}";
                return;
            }

            string varType = varSpace.GetTypeValue(root.token.value);
            if (varType != null && root.childs.Count == 0 && varSpace.GetType(root.token.value).type == NT.INDICATOR)
            {
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{root.token.value}]\n");
                regReturn = $".reg{regIndex-1}{regPrefer}";
                return;
            }
            if (varType != null && root.childs.Count == 0 && !DataBase.types.ContainsKey(varType))
            {
                _objProg.code.Append($"lea .reg{regIndex++}{regPrefer}, [{root.token.value}]\n");
                regReturn = $".reg{regIndex-1}{regPrefer}";
                return;
            }
            if (root.childs.Count == 0)
            {
                _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{root.token.value}]\n");
                regReturn = $".reg{regIndex-1}{regPrefer}";
                return;
            }

            CommonNode type = take(root, 0);
            string classes = "";
            if (DataBase.types.Keys.Contains(type.token.value))
                classes = DataBase.types[type.token.value];
            else
                classes = type.token.value;
            if (root.childs.Count > 0 && root.childs[0].type == NT.INDICATOR)
            { classes = "*" + $"{root.childs[0].token.value}"; }
            if (!varSpace.PeekContainsKey(root.token.value) && type != null && _objProg.local == false)
            {
                _objProg.data.Append($"{root.token.value} {classes} 0\n");
            }
            if (!varSpace.PeekContainsKey(root.token.value) && type != null && _objProg.local == true)
            {
                _objProg.code.Append($"local {root.token.value} {classes} 0\n");
            }
            if (!varSpace.ContainsKey(root.token.value) && type != null) varSpace.AddVar(root.token.value, type);
        }
        private void translationBinOper (CommonNode root, int z_buffer)
        {
            if (root.token.value == "=")
            {
                // child
                CommonNode varChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                string varString = string.Empty;
                if (varChild.type == NT.OFFSET)
                {
                    string[] strs = translationOffset(varChild, z_buffer, false);
                    string reg = regReturn;
                    varString = regReturn;
                    _objProg.code.Append($"push {strs[0]}\n");
                    _objProg.code.Append($"push {strs[1]}\n");
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"pop {strs[1]}\n");
                    _objProg.code.Append($"pop {strs[0]}\n");
                    _objProg.code.Append($"mov {reg}, {regReturn}\n");
                    return;
                }
                else if (varChild.type == NT.USEADDRESSVAR)
                {
                    translationUseAddressVar(varChild, z_buffer + 1);
                    varString = $"[{regReturn}]";
                }
                else if (varChild.type == NT.REGDECL)
                {
                    translationRegDeclaration(varChild, z_buffer + 1);
                    varString = getRegisterUse(varChild.token.value);
                }
                else if (varChild.type == NT.REGUSE) varString = getRegisterUse(varChild.token.value);
                else varString = $"[{varChild.token.value}]";


                if (rightChild.type == NT.FLOAT)
                {
                    _objProg.data.Append($"{varString} dd {rightChild.token.value.Replace("f","")}\n");
                    return;
                }

                if (varChild.childs.Count > 0 && varChild.type == NT.VAR)
                    translationVar(varChild, z_buffer + 1);
                

                if (rightChild.type == NT.STRING)
                {
                    translationString(rightChild, z_buffer + 1);

                    _objProg.code.Append($"lea .reg{regIndex++}{regPrefer}, [{stringConsts[rightChild.token.value]}]\n");
                    _objProg.code.Append($"mov {varString}, .reg{regIndex-1}{regPrefer}\n");
                    return;
                }
                else if (rightChild.type == NT.CHAR)
                {
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {rightChild.token.value}\n");
                    _objProg.code.Append($"mov {varString}, .reg{regIndex-1}{regPrefer}\n");
                }
                else if (rightChild.type == NT.CALL)
                {
                    //regPrefer = "eax";
                    if (DataBase.types.ContainsKey(ProgramAst.resualtFunc[rightChild.token.value].token.value) || ProgramAst.resualtFunc[rightChild.token.value].type == NT.INDICATOR)
                    { translationCall(rightChild, z_buffer + 1); _objProg.code.Append($"mov {varString}, .reg{regIndex++}eax\n"); }
                    else translationCall(rightChild, z_buffer + 1, $"lea .reg{regIndex++}eax, {varString}\n");
                }
                else if (rightChild.type == NT.NUMBER)
                {
                    _objProg.code.Append($"mov {varString}, {rightChild.token.value}\n");
                }
                else
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov {varString}, {regReturn}\n");
                }

                return;
            }

            CommonNode leftType = DataBase.getFormulaNodeType(root.childs[0], ref varSpace, ref ProgramAst);
            CommonNode rightType = DataBase.getFormulaNodeType(root.childs[1], ref varSpace, ref ProgramAst);

            int tempOperationIndex = DataBase.isRightOperator(root.token.value, leftType, rightType, ref varSpace, ref ProgramAst);
            if (tempOperationIndex != -1)
            {
                string OperatorName = ProgramAst.operatorFunctions.ElementAt(tempOperationIndex).Value;

                regString = true;
                Translation(root.childs[0], z_buffer + 1);
                string reg1 = regReturn;
                Translation(root.childs[1], z_buffer + 1);
                string reg2 = regReturn;
                regString = false;

                varRegisters.Add(reg1, reg1);
                varRegisters.Add(reg2, reg2);

                CommonNode callNode = new CommonNode(NT.CALL, new Token(TT.NULL, OperatorName, root.token.pos));
                callNode.childs.Add(new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", root.token.pos)));
                callNode.childs[0].childs.Add(new CommonNode(NT.REGUSE, new Token(TT.NULL, reg1, root.childs[0].token.pos)));
                callNode.childs[0].childs.Add(new CommonNode(NT.REGUSE, new Token(TT.NULL, reg2, root.childs[1].token.pos)));
                //regPrefer = "eax";
                translationCall(callNode, z_buffer + 1);
                if (root.token.value.Contains("=")) _objProg.code.Append($"mov [{root.childs[0].token.value}], {regReturn}\n");
                return;
            }

            if (root.token.value == "+"
                || root.token.value == "-"
                || root.token.value == "*"
                || root.token.value == "/"
                || root.token.value == "%"
                || root.token.value == "&"
                || root.token.value == "|")
            {
                CommonNode leftChild = take(root, 0);
                CommonNode rightChild = take(root, 1);

                string leftRegM = string.Empty;//$".reg{regIndex++}{regPrefer}";
                string rightRegM = string.Empty;//$".reg{regIndex++}{regPrefer}";

                if (leftChild.type == NT.VAR && rightChild.type == NT.NUMBER)
                {
                    rightRegM = rightChild.token.value;
                    leftRegM = $".reg{regIndex++}{regPrefer}";
                    _objProg.code.Append($"mov {leftRegM}, [{leftChild.token.value}]");
                } else if (leftChild.type == NT.NUMBER && rightChild.type == NT.VAR)
                {
                    rightRegM = $"[{rightChild.token.value}]";
                    leftRegM = $".reg{regIndex++}{regPrefer}";
                    _objProg.code.Append($"mov {leftRegM}, {leftChild.token.value}");
                } else if (leftChild.type == NT.CALL && rightChild.type == NT.VAR)
                {
                    rightRegM = $"[{rightChild.token.value}]";
                    //regPrefer = "eax";
                    translationCall(leftChild, z_buffer + 1);
                    leftRegM = regReturn;
                } else if (leftChild.type == NT.VAR && rightChild.type == NT.CALL)
                {
                    leftRegM = $".reg{regIndex++}{regPrefer}";
                    _objProg.code.Append($"mov {leftRegM}, [{leftChild.token.value}]");
                    //regPrefer = "eax";
                    translationCall(rightChild, z_buffer + 1);
                    rightRegM = regReturn;
                } else if (leftChild.type == NT.VAR && rightChild.type == NT.BINOPER)
                {
                    leftRegM = $".reg{regIndex++}{regPrefer}";
                    translationBinOper(rightChild, z_buffer + 1);
                    rightRegM = regReturn;
                    _objProg.code.Append($"mov {leftRegM}, [{leftChild.token.value}]");
                } else if (leftChild.type == NT.BINOPER && rightChild.type == NT.VAR)
                {
                    translationBinOper(leftChild, z_buffer + 1);
                    leftRegM = regReturn;
                    rightRegM = $".reg{regIndex++}{regPrefer}";
                    _objProg.code.Append($"mov {rightRegM}, [{rightChild.token.value}]");
                } else if (leftChild.type == NT.VAR && rightChild.type == NT.BINOPER)
                {
                    translationBinOper(rightChild, z_buffer + 1);
                    rightRegM = regReturn;
                    leftRegM = $".reg{regIndex++}{regPrefer}";
                    _objProg.code.Append($"mov {leftRegM}, [{leftChild.token.value}]");
                }


                else {
                    (leftRegM, rightRegM) = translationLeftRightNodes(leftChild, rightChild, z_buffer);
                }
                switch (root.token.value)
                {
                    case "+":
                        _objProg.code.Append($"add {leftRegM}, {rightRegM}\n");
                        break;
                    case "-":
                        _objProg.code.Append($"sub {leftRegM}, {rightRegM}\n");
                        break;
                    case "*":
                        _objProg.code.Append($"imul {leftRegM}, {rightRegM}\n");
                        break;
                    case "/":
                        _objProg.code.Append("cdq\n");
                        //if (leftRegM != "eax")
                            _objProg.code.Append($"mov .reg{regIndex++}eax, {leftRegM}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}, {rightRegM}\n");
                        _objProg.code.Append($"idiv .reg{regIndex-1}\n");
                        _objProg.code.Append($"mov {leftRegM}, .reg{regIndex-2}eax\n");
                        break;
                    case "%":
                        _objProg.code.Append($"xor .reg{regIndex++}edx, .reg{regIndex-1}edx\n");
                        //if (leftRegM != "eax")
                            _objProg.code.Append($"mov .reg{regIndex++}eax, {leftRegM}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}, {rightRegM}\n");
                        _objProg.code.Append($"idiv .reg{regIndex++}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, .reg{regIndex-3}edx\n");
                        _objProg.code.Append($"mov {leftRegM}, .reg{regIndex++}edx\n");
                        break;
                    case "&":
                        _objProg.code.Append($"and {leftRegM}, {rightRegM}\n");
                        break;
                    case "|":
                        _objProg.code.Append($"or {leftRegM}, {rightRegM}\n");
                        break;
                }
                if (leftRegM.StartsWith(".reg")) regReturn = leftRegM;
                //if (leftRegM == "ebx") _objProg.code.Append("mov eax, ebx\n");
                return;
            }
            if (root.token.value == "+="
                || root.token.value == "-="
                || root.token.value == "*="
                || root.token.value == "/="
                || root.token.value == "%=")
            {
                CommonNode leftChild = take(root, 0);
                CommonNode rightChild = take(root, 1);

                string leftRegM = $"[{leftChild.token.value}]";
                string rightRegM = "ebx";

                if (leftChild.type == NT.REGUSE) leftRegM = getRegisterUse(leftChild.token.value);

                if (rightChild.type == NT.NUMBER)
                {
                    rightRegM = rightChild.token.value;
                }
                else if (rightChild.type == NT.CALL)
                {
                    //regPrefer = "eax";
                    translationCall(rightChild, z_buffer + 1);
                    rightRegM = regReturn;
                }
                else if (leftChild.type == NT.OFFSET)
                {
                    string[] strs = translationOffset(leftChild, z_buffer, false);
                    string reg = regReturn;
                    leftRegM = regReturn;
                    _objProg.code.Append($"push {strs[0]}\n");
                    _objProg.code.Append($"push {strs[1]}\n");
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"pop {strs[1]}\n");
                    _objProg.code.Append($"pop {strs[0]}\n");
                    rightRegM = regReturn;
                }
                else
                {
                    Translation(rightChild, z_buffer + 1);
                    rightRegM = regReturn;
                }
                switch (root.token.value)
                {
                    case "+=":
                        _objProg.code.Append($"add {leftRegM}, {rightRegM}\n");
                        break;
                    case "-=":
                        _objProg.code.Append($"sub {leftRegM}, {rightRegM}\n");
                        break;
                    case "*=":
                        _objProg.code.Append($"mov .reg{regIndex++}ebx, {leftRegM}");
                        _objProg.code.Append($"imul .reg{regIndex-1}ebx, {rightRegM}\n");
                        _objProg.code.Append($"mov {leftRegM}, .reg{regIndex-1}ebx");
                        break;
                    case "/=":
                        _objProg.code.Append("cdq\n");
                        _objProg.code.Append($"mov .reg{regIndex++}eax, {leftRegM}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}, {rightRegM}\n");
                        _objProg.code.Append($"idiv .reg{regIndex-1}\n");
                        _objProg.code.Append($"mov {leftRegM}, .reg{regIndex-2}eax");
                        break;
                    case "%=":
                        _objProg.code.Append($"xor .reg{regIndex++}edx, .reg{regIndex-1}edx\n");
                        _objProg.code.Append($"mov .reg{regIndex++}eax, {leftRegM}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}, {rightRegM}\n");
                        _objProg.code.Append($"idiv .reg{regIndex-1}\n");
                        _objProg.code.Append($"mov {leftRegM}, .reg{regIndex-3}edx\n");
                        break;
                }
                return;
            }
        }
        private void translationFloatBinOper(CommonNode root, int z_buffer)
        {
            if (root.token.value == "=")
            {
                // child
                CommonNode varChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                

                //_objProg.code.Append("xor eax, eax\n");
                if (rightChild.type == NT.FLOAT && varChild.type == NT.VAR && varChild.childs.Count > 0)
                {
                    _objProg.data.Append($"{varChild.token.value} dd {rightChild.token.value.Replace("f", "")}\n");
                    return;
                }
                if (varChild.childs.Count > 0 && varChild.childs[0].type == NT.OFFSET)
                {
                    translationOffset(varChild, rightChild, z_buffer);
                    return;
                }
                if (varChild.childs.Count != 0)
                    Translation(varChild, z_buffer + 1);
                //Translation (rightChild, z_buffer + 1);
                if (rightChild.type == NT.CALL)
                {
                    translationCall(rightChild, z_buffer + 1, $"lea .reg{regIndex++}{regPrefer}, [{varChild.token.value}]\n");
                    _objProg.code.Append($"movss xmm1, {regReturn}\n");
                }
                else if (rightChild.type == NT.NUMBER)
                {
                    _objProg.code.Append($"mov [{varChild.token.value}], {rightChild.token.value}\n");
                }
                else if (rightChild.type == NT.FLOAT)
                {
                    string floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{floatVar}]\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], .reg{regIndex-1}{regPrefer}\n");
                }
                else if (rightChild.type == NT.FLOATBINOPER)
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss [{varChild.token.value}], xmm0\n");
                }
                else if (rightChild.type == NT.CALL)
                {
                    //regPrefer = "eax";
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm0, {regReturn}\n");
                    _objProg.code.Append($"movss [{varChild.token.value}], xmm0\n");
                }
                else if (rightChild.type == NT.VAR && rightChild.childs.Count > 0 && rightChild.childs[0].type == NT.OFFSET)
                {
                    translationVar(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{rightChild.token.value}]\n");
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [.reg{regIndex-2}{regPrefer}+.reg{regIndex-3}{regPrefer}]\n");
                    _objProg.code.Append($"mov [{varChild.token.value}], {regReturn}\n");
                }
                else if (!(rightChild.type == NT.NUMBER))
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"mov [{varChild.token.value}], {regReturn}\n");
                }
                return;
            }
            if (new string[] { "+=", "-=", "*=", "/=", "%=", "+", "-", "*", "/", "%" }.Contains(root.token.value))
            {
                CommonNode leftChild = take(root, 0); // eax
                CommonNode rightChild = take(root, 1);

                string leftString = "xmm0";
                string rightString = "xmm1";

                if (leftChild.type == NT.VAR)
                {
                    _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{leftChild.token.value}]\n");
                    _objProg.code.Append($"cvtsi2ss xmm0, .reg{regIndex-1}{regPrefer}\n"); // [{leftChild.token.value}]
                    //return;
                } else if (leftChild.type == NT.NUMBER)
                {
                    _objProg.code.Append($"movss xmm0, {leftChild.token.value}\n");
                } else if (leftChild.type == NT.FLOAT)
                {
                    string floatVar = getFloatConst(leftChild);
                    _objProg.code.Append($"movss xmm0, [{floatVar}]\n");
                } else if (leftChild.type == NT.CALL)
                {
                    //regPrefer = "eax";
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm0, {regReturn}\n");
                } else if (leftChild.type == NT.FLOATBINOPER)
                {
                    Translation(leftChild, z_buffer + 1);
                } else
                {
                    Translation(leftChild, z_buffer + 1);
                    _objProg.code.Append($"cvtsi2ss xmm0, {regReturn}\n");
                }

                if (rightChild.type == NT.VAR)
                {
                    rightString = $"[{rightChild.token.value}]";
                    //_objProg.code.Append($"movss xmm0, [{rightChild.token.value}]\n");
                    //return;
                }
                else if (rightChild.type == NT.NUMBER)
                {
                    //_objProg.code.Append($"movss xmm0, {rightChild.token.value}\n");
                    rightString = rightChild.token.value;
                }
                else if (rightChild.type == NT.FLOAT)
                {
                    string floatVar = getFloatConst(rightChild);
                    _objProg.code.Append($"movss xmm1, [{floatVar}]\n");
                }
                else if (rightChild.type == NT.CALL)
                {
                    //regPrefer = "eax";
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"movss xmm1, {regReturn}\n");
                }
                else if (rightChild.type == NT.FLOATBINOPER)
                {
                    Translation(rightChild, z_buffer + 1);
                } else
                {
                    Translation(rightChild, z_buffer + 1);
                    _objProg.code.Append($"cvtsi2ss xmm1, {regReturn}\n");
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
                        _objProg.code.Append($"xor .reg{regIndex++}edx, .reg{regIndex-1}edx\n");
                        _objProg.code.Append($"div .reg{regIndex-2}{regPrefer}\n");
                        _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, .reg{regIndex-2}edx\n");
                        break;
                }
                if (leftChild.type == NT.VAR && !(new string[] { "+", "-", "*", "/" }.Contains(root.token.value)))
                    _objProg.code.Append($"movss [{leftChild.token.value}], xmm0\n");
            }
        } // FIXME: Переделать как в биноперах
        private void translationConst (CommonNode root, int  z_buffer)
        {
            CommonNode constValue = take(root, 0);

            //_objProg.data.Append($"const_{constsIndex} db {root.token.value}");

            if (constValue.type == NT.STRING)
            {
                consts.Add(root.token.value, $"const_{constsIndex}");
                _objProg.data.Append($"{consts[root.token.value]} db {constValue.token.value}\n");
                constsIndex++;
            }
            else if (constValue.type == NT.NUMBER)
            {
                consts.Add(root.token.value, $"const_{constsIndex}");
                _objProg.data.Append($"{root.token.value} equ {constValue.token.value}\n");
                constsIndex++;
            }
        } // DELETE: Удалить констант больше нет
        private void translationString (CommonNode root, int z_buffer)
        {
            root.token.value = root.token.value.Replace("\\n", "', 13, 10, '");
            root.token.value = root.token.value.Replace("\\t", "', 9, '");
            if (!stringConsts.Keys.Contains(root.token.value))
            {
                stringConsts.Add(root.token.value, $"str_const_{stringConstsIndex}");
                
                _objProg.stringsConsts.Append($"str_const_{stringConstsIndex++} du '{root.token.value}', 0\n");
                _objProg.code.Append($"lea .reg{regIndex++}, [{stringConsts[root.token.value]}]");
                regReturn = $".reg{regIndex - 1}";
                return;
            }
            if (regString)
            {
                _objProg.code.Append($"lea .reg{regIndex++}, [{stringConsts[root.token.value]}]");
                regReturn = $".reg{regIndex - 1}";
            }
        }
        private void translationChar(CommonNode root, int z_buffer) 
        {
            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, {root.token.value}");
            regReturn = $".reg{regIndex-1}{regPrefer}";
        } // TODO: Возможно не будет работать с .reg
        private void translationStruct (CommonNode root, int z_buffer)
        {
            if (ProgramAst.externStructs.Contains(root.token.value)) return;
            setWriteData(CodeData.macroData);
            _objProg.code.Append($"struct {root.token.value}\n");

            int offset = 0;
            foreach (CommonNode child in root.childs)
            {
                if (offset % 4 == 3) _objProg.code.Append($"    align 2\n   align 1\n");
                else if (offset % 4 != 0) _objProg.code.Append($"    align {offset % 4}\n");
                if (child.type == NT.VAR && child.childs[0].type == NT.INDICATOR)
                {
                    _objProg.code.Append($"    {child.token.value} dd 0\n");
                    offset += 4;
                }
                else if (child.type == NT.VAR)
                {
                    int varsize = 0;
                    if (DataBase.types.Keys.Contains(take(child, 0).token.value))
                    {
                        _objProg.code.Append($"    {child.token.value} {DataBase.types[take(child, 0).token.value]} 0\n");
                        varsize = DataBase.aligns[take(child, 0).token.value];
                    }
                    else
                    {
                        _objProg.code.Append($"    {child.token.value} {take(child, 0).token.value} 0\n");
                        varsize = getStructSize(take(child, 0).token.value);
                    }
                    offset += varsize;
                }
            }

            int size = getStructSize(root.token.value);

            if (size % 4 != 0) _objProg.code.Append($"    align {4 - size % 4}\n");

            _objProg.code.Append("ends\n");

            _objProg.data.Append($"SIZE_{root.token.value.ToUpper()} = sizeof.{root.token.value}\n");

            setWriteData(CodeData.codeData);
        }
        private void translationInline (CommonNode root, int z_buffer)
        {
            setWriteData(CodeData.macroData);
            //CommonNode types2 = take(root, 0);
            CommonNode signatureInline = take(root, 0);
            string argsInline = string.Empty;
            Dictionary<string, string> args = new Dictionary<string, string>();
            for (int i = 0; i < signatureInline.childs.Count; i++)
            {
                //argsInline += signatureInline.childs[i].token.value;
                //if (i < signatureInline.childs.Count - 1)
                //{
                //    argsInline += ",";
                //}

                if (signatureInline.childs[i].token.value == "resualtPtr")
                {
                    _objProg.data.Append($"{root.token.value}{i} dd 0\n");
                    args.Add($"{signatureInline.childs[i].token.value}", "dd");
                    argsInline += signatureInline.childs[i].token.value + $"_{root.token.value}" + ",";
                } else if (DataBase.types.ContainsKey(signatureInline.childs[i].childs[0].token.value))
                {
                    _objProg.data.Append($"{root.token.value}{i} {DataBase.types[signatureInline.childs[i].childs[0].token.value]} 0\n");
                    args.Add($"{signatureInline.childs[i].token.value}", DataBase.types[signatureInline.childs[i].childs[0].token.value]);
                    argsInline += signatureInline.childs[i].token.value + $"_{root.token.value}" + ",";
                } else
                {
                    _objProg.data.Append($"{root.token.value}{i} dd 0\n");
                    args.Add($"{signatureInline.childs[i].token.value}", "dd");
                    argsInline += signatureInline.childs[i].token.value + $"_{root.token.value}" + ",";
                }
            }
            if (argsInline.Length != 0) argsInline = argsInline.Remove(argsInline.Length - 1, 1);
            _objProg.code.Append($"macro {root.token.value}\n"); // {argsInline}
            Dictionary<string, string> table = new Dictionary<string, string>();
            int index = 0;
            foreach (var arg in args)
            {
                _objProg.code.Append($"    mlocal {arg.Key}_{root.token.value} {arg.Value} 0\n");
                table.Add($"{arg.Key}", $"{root.token.value}{index}");
                index++;
            }
            _objProg.code.Append("{\n");
            varSpace.OpenSpace();
            int _index = 0;
            foreach (CommonNode child in signatureInline.childs)
            {
                varSpace.AddVar(root.token.value + _index, child.childs[0]);
                _index++;
            }
            Translation(repcaleNode(take(root, 1), ref table), z_buffer + 1);
            varSpace.CloseSpace();
            _objProg.code.Append("}\n");
            setWriteData(CodeData.codeData);
        }
        private void translationAsmInline (CommonNode root, int z_buffer)
        {
            setWriteData(CodeData.macroData);
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
        private void translationFunc (CommonNode root, int z_buffer)
        {
            _objProg.code.Append($"; FUNC {root.token.value}\n");
            regPrefer = "";
            setWriteData(CodeData.procData);
            bool qsFunc = ProgramAst.qsFunction.Contains(root.token.value);
            //CommonNode types2 = take(root, 0);
            CommonNode signature = take(root, 1);
            string args = string.Empty;
            for (int i = 0; i < signature.childs.Count; i++)
            {
                if (!DataBase.types.ContainsKey(signature.childs[i].childs[0].token.value) && signature.childs[i].token.value != "resualtPtr")
                    signature.childs[i].childs[0].type = NT.INDICATOR;
            }
            for (int i = ((qsFunc) ? 1 : 0); i < signature.childs.Count; i++)
            {
                if (i != 0 && i != ((qsFunc)?1:0)) args += " , ";
                if (signature.childs[i].token.value == "resualtPtr")
                    args += $"{signature.childs[i].token.value}:DWORD";
                else if (DataBase.typesarg.ContainsKey(signature.childs[i].childs[0].token.value))
                    args += $"{signature.childs[i].token.value}:{DataBase.typesarg[signature.childs[i].childs[0].token.value]}";
                else
                    args += $"{signature.childs[i].token.value}:{signature.childs[i].childs[0].token.value}";
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
            regPrefer = "";
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
            else if (DataBase.typesarg.Keys.Contains(ProgramAst.resualtFunc[root.token.value].token.value) || ProgramAst.resualtFunc[root.token.value].type == NT.INDICATOR)
                _objProg.code.Append($"{root.token.value}.return:\n");
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
        } // TODO: Не трогать регистры не менять на .reg
        private void translationCall (CommonNode root, int z_buffer, string resualtPtr=null)
        {
            //_objProg.code.Append($"; CALL {root.token.value}\n");
            //_objProg.code.Append($"precall\n");
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
                    string child = signatureCall.childs[i].token.value;
                    if (signatureCall.childs[i].type == NT.NUMBER)
                    {
                        if (!ProgramAst.asmInlineNames.Contains(root.token.value))
                            _objProg.code.Append($"mov [{root.token.value}{i}], {child}");
                        args += signatureCall.childs[i].token.value;
                    }
                    else if (signatureCall.childs[i].type == NT.VAR)
                    {
                        if (!ProgramAst.asmInlineNames.Contains(root.token.value))
                        {
                            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{child}]");
                            _objProg.code.Append($"mov [{root.token.value}{i}], .reg{regIndex-1}{regPrefer}");
                        }
                        args += signatureCall.childs[i].token.value;
                    }
                    else if (signatureCall.childs[i].type == NT.ADDRESS)
                    {
                        if (!ProgramAst.asmInlineNames.Contains(root.token.value))
                        {
                            translationAddress(signatureCall.childs[i], z_buffer + 1);
                            _objProg.code.Append($"mov [{root.token.value}{i}], {regReturn}");
                        }
                        args += signatureCall.childs[i].token.value;
                    }
                    else if (signatureCall.childs[i].type == NT.STRING)
                    {
                        Translation(signatureCall.childs[i], z_buffer + 1);
                        if (!ProgramAst.asmInlineNames.Contains(root.token.value)) _objProg.code.Append($"mov [{root.token.value}{i}], {regReturn}");
                        args += stringConsts[signatureCall.childs[i].token.value];
                    }
                    else
                        //throw new Exception("У Токена:" + signatureCall.childs[i].token.pos+"ошибка в вызове инлайн функции может быть только строка, переменная или число!!");
                        Syntax.SyntaxError("У Токена:" + signatureCall.childs[i].token.pos + "ошибка в вызове инлайн функции может быть только строка, переменная или число!!", signatureCall);
                    if (i < signatureCall.childs.Count-1)
                        args += ",";
                }
                if (ProgramAst.asmInlineNames.Contains(root.token.value)) _objProg.code.Append($"{root.token.value} {args}\n");
                else _objProg.code.Append($"{root.token.value}\n");
                return;
            }
            //Console.WriteLine(root.token.value);
            //if (ProgramAst.resualtFunc[root.token.value] != null) Console.WriteLine(ProgramAst.resualtFunc[root.token.value].token.value);
            string regForArgs = $".reg{regIndex++}{regPrefer}";
            for (int i = signatureCall.childs.Count - 1; i >= ((qsFunc)?1:0); i--)
            {
                //Console.WriteLine($"- {signatureCall.childs[i].token.value}");
                /*if (signatureCall.childs[i].type == NT.VAR && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[i].childs[0].token.value) && varSpace.GetType(signatureCall.childs[i].token.value).type == NT.INDICATOR)
                {
                    _objProg.code.Append($"mov {regForArgs}, [{signatureCall.childs[i].token.value}]\n");
                    _objProg.code.Append($"push {regForArgs}\n");
                }
                else if (signatureCall.childs[i].type == NT.VAR && !typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[i].childs[0].token.value))
                {
                    _objProg.code.Append($"lea {regForArgs}, [{signatureCall.childs[i].token.value}]\n");
                    _objProg.code.Append($"push {regForArgs}\n");
                }*/
                if (signatureCall.childs[i].type == NT.VAR)
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push {regReturn}\n");
                }
                else if (signatureCall.childs[i].type == NT.STRING)
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push {stringConsts[signatureCall.childs[i].token.value]}\n");
                }
                else if (signatureCall.childs[i].type == NT.VAR && signatureCall.childs[i].childs.Count > 0 && signatureCall.childs[i].childs[0].type == NT.OFFSET)
                {
                    Translation(signatureCall.childs[i], z_buffer + 1);
                    _objProg.code.Append($"push {regReturn}\n");
                } 
                else if (signatureCall.childs[i].type == NT.CALL)
                {
                    //regPrefer = "eax";
                    translationCall(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push {regReturn}");
                }
                else
                {
                    Translation(take(signatureCall, i), z_buffer + 1);
                    _objProg.code.Append($"push {regReturn}\n");
                }
            }
            //Console.WriteLine("--end");
            if (resualtPtr != null)
            {
                _objProg.code.Append(resualtPtr);
                _objProg.code.Append($"push {resualtPtr.Split(' ', ',')[1]}\n");
            }
            //regPrefer = "eax";
            regPrefer = "";
            if (qsFunc && firstArg != null)
            {
                if (firstArg.type == NT.VAR && !DataBase.typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[0].childs[0].token.value) && ProgramAst.varTypes[firstArg.token.value].type == NT.INDICATOR)
                    _objProg.code.Append($"mov eax, [{firstArg.token.value}]\n");
                else if (firstArg.type == NT.VAR && !DataBase.typesarg.Keys.Contains(ProgramAst.typesArgsFunc[root.token.value].childs[0].childs[0].token.value))
                    _objProg.code.Append($"lea eax, [{firstArg.token.value}]\n");
                else if (firstArg.type == NT.STRING)
                {
                    Translation(firstArg, z_buffer + 1);
                    _objProg.code.Append($"lea eax, [{stringConsts[firstArg.token.value]}]\n");
                }
                else if (firstArg.type == NT.VAR && firstArg.childs.Count > 0 && firstArg.childs[0].type == NT.OFFSET)
                    Translation(firstArg, z_buffer + 1);
                else
                    Translation(firstArg, z_buffer + 1);
            }
            bool flag = false;
            foreach (var library in ProgramAst.externLibrarys.Keys)
                if (ProgramAst.externFuncs[library].Contains(root.token.value)) flag = true;
            if (flag || (!ProgramAst.resualtFunc.ContainsKey(root.token.value) && varSpace.ContainsKey(root.token.value)))
            {
                _objProg.code.Append($"call [{root.token.value}]\n");
            }
            else _objProg.code.Append($"call {root.token.value}\n");

            regPrefer = "";
            regReturn = $".reg{regIndex++}eax";
        } // TODO: Возможно переделать первый аргумент через qs на .reg

        private void translationOffset(CommonNode varNode, CommonNode rightNode, int z_buffer)
        {
            Translation(rightNode, z_buffer + 1);
            string rightRegReturn = regReturn;

            CommonNode offset = varNode.childs[0];
            Translation(offset.childs[0], z_buffer);
            _objProg.code.Append($"imul {regReturn}, {getSize(varNode.token.value, varNode)}\n");

            _objProg.code.Append($"mov .reg{regIndex++}{regPrefer}, [{varNode.token.value}]\n");
            _objProg.code.Append($"mov [.reg{regIndex-1}{regPrefer}+{regReturn}], {rightRegReturn}\n");
        }
        private string getFloatConst (CommonNode node)
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
        private string getSize(string name, CommonNode tagError)
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
                if (!DataBase.types.ContainsKey(type.token.value)) { n++; goto start; } // strct = ProgramAst.structs[strct][strs[n]].token.value; 
            } else type = varSpace.GetType(name);
            //CommonNode type = ProgramAst.varTypes[name];
            string classes = "";
            string sizeConst = "";
            if (DataBase.types.Keys.Contains(type.token.value))
            {
                classes = DataBase.types[type.token.value];
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
        private string getRegisterUse(string value)
        {
            return varRegisters[value];
        }

        private static bool isNodeType(NT type, CommonNode root)
        {
            bool isNode = false;

            foreach (var child in root.childs)
            {
                if (child.type == type) return true;
                else if (child.childs.Count > 0) isNode = (isNodeType(type, child)) ? true : isNode;
            }

            return isNode;
        }
        private static bool isNodeValue(string value, CommonNode root)
        {
            bool isNode = false;

            foreach (var child in root.childs)
            {
                if (child.token.value == value) return true;
                else if (child.childs.Count > 0) isNode = (isNodeValue(value, child)) ? true : isNode;
            }

            return isNode;
        }
        private static int totalNodes(CommonNode root)
        {
            int total = 1;

            foreach (var child in root.childs)
            {
                total += totalNodes(child);
            }

            return total;
        }
        private static CommonNode repcaleNode (CommonNode root, ref Dictionary<string, string> table)
        {
            if (table.ContainsKey(root.token.value)) root.token.value = table[root.token.value];

            for (int i = 0; i < root.childs.Count; i++)
            {
                root.childs[i] = repcaleNode(root.childs[i], ref table);
            }
            return root;
        }

        private int getStructSize(string type)
        {
            int size = 0;

            foreach (var _var in ProgramAst.structs[type].Values)
            {
                if (_var.type == NT.INDICATOR) size += 4;
                else if (DataBase.types.ContainsKey(_var.token.value))
                {
                    string classsize = DataBase.types[_var.token.value];
                    if (classsize == "dq") size += 8;
                    else if (classsize == "dd") size += 4;
                    else if (classsize == "dw") size += 2;
                    else if (classsize == "db") size += 1;
                }
                else if (_var.token.value == "dq") size += 8;
                else if (_var.token.value == "dd") size += 4;
                else if (_var.token.value == "dw") size += 2;
                else if (_var.token.value == "db") size += 1;
                else size += getStructSize(_var.token.value);
            }

            return size;
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
                Match regx = Regex.Match(str.Substring(pos), "^" + TokenTypeList.tokenTypes[TT.STRING]);
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

        public string ConcatData(TypeApp typeApp)
        {
            /*try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    Console.WriteLine($"<< {types.ElementAt(i)} | {typesarg.ElementAt(i)} | {aligns.ElementAt(i)}");
                }
            } catch { }*/
            Console.WriteLine("Start PostGen...");
            _objProg = PostGen.PostTranslation(_objProg, ProgramAst);
            //_objProg = CrossCompiler.Compile(_objProg);
            string file = string.Empty;
            file += _objProg.includes.ToString();
            file += "\n";
            file += _objProg.macroData.ToString();
            file += "\nsection '.code' code readable executable\n";
            file += _objProg.procData.ToString();
            file += "ret\n";
            if (_objProg.data.Length != 0 || _objProg.stringsConsts.Length != 0)
                file += "\nsection '.data' data readable writable\n";
            int offset = 0;
            foreach (string str in _objProg.data.Data)
            {
                try
                {
                    int size;
                    string type = str.Trim().Split(' ')[1];
                    switch (type)
                    {
                        case "du": size = 2; break;
                        case "db": size = 1; break;
                        case "dw": size = 2; break;
                        case "dd": size = 4; break;
                        case "dq": size = 8; break;
                        case "=": size = 4; break;
                        default:
                            size = 4;
                            break;
                    }
                    if (4 - offset % 4 == 3)
                    {
                        file += $"align 2\n";
                        file += $"align 1\n";
                        offset += 3;
                    }
                    else if (offset % 4 != 0)
                    {
                        file += $"align {(4 - offset % 4)}\n";
                        offset += offset % 4;
                    }
                    file += str;
                    offset += size;
                }
                catch (Exception e) { Console.WriteLine($"На переменной {str} возникла ошибка: {e.Message}, {e.StackTrace}");  }
            }
            file += _objProg.stringsConsts.ToString();
            //if (_objProg.procData.Length != 0)

            // section '.code' code readable executable
            // section '.data' data readable writable

            ///if (typeApp == TypeApp.h)
            //{
            //file += "start: ;START MAIN\n";
            //file += _objProg.codeData.ToString();
            //return file;
            ///}
            ///
            if (typeApp == TypeApp.bin)
            {
                file = "format binary as \"bin\"\n" + file;
                file += _objProg.codeData.ToString();
            }
            else if (typeApp == TypeApp.gui)
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
                    Match regx = Regex.Match(str.Substring(pos), "^" + TokenTypeList.tokenTypes[TT.STRING]);
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
            Console.WriteLine($"out: {pathCompile}\\bin\\");
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
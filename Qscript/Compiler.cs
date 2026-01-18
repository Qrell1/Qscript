using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qscript
{
    public enum CodeData
    { 
        codeData, procData
    }

    public class objProgram
    {
        //public List<CommonNode> functions;
        public StringBuilder data = new StringBuilder();
        public StringBuilder code;

        public StringBuilder codeData = new StringBuilder();
        public StringBuilder procData = new StringBuilder();

        //public StringBuilder localsData = new StringBuilder();

        public bool local;
    }
    public class Compiler
    {
        public objProgram _objProg = new objProgram();
        public string fasmCompilerPath;
        private List<CommonNode> list;

        private List<string> vars = new List<string>();

        public Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "db"},
            {"char", "db"}
        };
        public Dictionary<string, string> typesarg = new Dictionary<string, string>()
        {
            {"int32", "DWORD"},
            {"int16", "WORD"},
            {"int8", "BYTE"},
            {"byte", "BYTE"},
            {"string", "BYTE"},
            {"char", "BYTE"}
        };
        public Dictionary<string, string> stringConsts = new Dictionary<string, string>();
        public int stringConstsIndex;

        public Dictionary<string, string> consts = new Dictionary<string, string>();
        public int constsIndex;

        public int pos;
        public Compiler(string _fasmCompilerPath) { fasmCompilerPath = _fasmCompilerPath; }

        //
        public bool peek(string type)
        {
            if (list[pos].type == type)
            {
                return true;
            }
            return false;
        }
        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count>=i+1)
            {
                return node.childs[i];
            }
            return null;
        }
        public void skip()
        {
            pos++;
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
        }
        public void local()
        {
            _objProg.local = !_objProg.local;
        }

        public void Translation (CommonNode root, int z_buffer)
        {
            //Console.WriteLine($"[DEBUG]--PrintAST>{GenSpaces(z_buffer)} |Тип:{root.type} [{root.token.value}] Дочерних узлов:{root.childs.Count}");
            

            switch (root.type)
            {
                case "ROOT":
                    setWriteData(CodeData.codeData);
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        CommonNode child = root.childs[i];
                        Translation (child, z_buffer+1);
                    }
                    break;
                case "VAR":
                    if (root.childs.Count == 0)
                    {
                        //_objProg.code.Append($"mov eax, [{root.token.value}]\n");
                        break;
                    }
                    CommonNode type = take(root, 0);
                    string classes = "";
                    if (types.Keys.Contains(type.token.value))
                        classes = types[type.token.value];
                    else
                        classes = type.token.value;
                    if (!vars.Contains(root.token.value) && type != null && _objProg.local == false)
                    {
                        _objProg.data.Append($"{root.token.value} {classes} ??\n");
                    }
                    if (!vars.Contains(root.token.value) && type != null && _objProg.local == true)
                    {
                        _objProg.code.Append($"local {root.token.value} {classes} ??\n");
                    }
                    break;
                case "BINOPER":
                    if (root.token.value == "=")
                    {
                        // child
                        CommonNode varChild = take(root, 0); // eax
                        CommonNode rightChild = take(root, 1);
                        //_objProg.code.Append("xor eax, eax\n");
                        Translation (varChild, z_buffer + 1);
                        //Translation (rightChild, z_buffer + 1);
                        if (rightChild.type == "STRING")
                        {
                            if (!stringConsts.Keys.Contains(rightChild.token.value))
                            {
                                stringConsts.Add(rightChild.token.value, $"str_const_{stringConstsIndex}");
                                _objProg.data.Append($"str_const_{stringConstsIndex} db {rightChild.token.value}");
                                stringConstsIndex++;
                            }
                            _objProg.code.Append($"mov [{varChild.token.value}], [{stringConsts[rightChild.token.value]}]\n");
                            //_objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                            break;
                        }
                        Translation(rightChild, z_buffer + 1);
                        _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                        break;
                    }
                    if (new string[] { "+=", "-=", "*=", "/=" }.Contains(root.token.value))
                    {
                        // child
                        CommonNode varChild = take(root, 0); // eax
                        CommonNode rightChild = take(root, 1);
                        _objProg.code.Append($"mov eax, [{varChild.token.value}]\n");
                        //Translation(varChild, z_buffer + 1);
                        //Translation(rightChild, z_buffer + 1);
                        if (rightChild.type == "NUMBER")
                            _objProg.code.Append($"mov ebx, {rightChild.token.value}\n");
                        else if (rightChild.type == "VAR")
                            _objProg.code.Append($"mov ebx, [{rightChild.token.value}]\n");
                        else
                        {
                            _objProg.code.Append($"push eax\n");
                            Translation(rightChild, z_buffer + 1);
                            _objProg.code.Append($"mov ebx, eax\n");
                            _objProg.code.Append($"pop eax\n");
                        }
                        switch (root.token.value)
                        {
                            case "+=":
                                _objProg.code.Append($"add eax, ebx\n");
                                break;
                            case "-=":
                                _objProg.code.Append($"sub eax, ebx\n");
                                break;
                            case "*=":
                                _objProg.code.Append($"mul ebx\n");
                                break;
                            case "/=":
                                _objProg.code.Append("cdq\n");
                                _objProg.code.Append("idiv ebx\n");
                                break;
                        }
                        _objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                        break;
                    }
                    if (new string[] { "+", "-", "*", "/" }.Contains(root.token.value))
                    {
                        CommonNode leftChild = take(root, 0);
                        CommonNode rightChild = take(root, 1);
                        /*if (leftChild.type == "NUMBER" &&  rightChild.type == "NUMBER")
                        {
                            _objProg.code.Append($mov);
                        }*/
                        /*if (leftChild.type == "NUMBER")
                        {
                            _objProg.code.Append("push eax\n");
                            _objProg.code.Append($"mov eax, {leftChild.token.value}\n");
                        }
                        else
                        {
                            Translation(leftChild, z_buffer + 1);
                        }
                        if (rightChild.type == "NUMBER")
                        {
                            //_objProg.code.Append($"");
                            string instuct = string.Empty;
                            switch (root.token.value)
                            {
                                case "+":
                                    instuct = "add";
                                    break;
                                case "-":
                                    instuct = "sub";
                                    break;
                                case "*":
                                    instuct = "mul";
                                    break;
                                case "/":
                                    instuct = "div";
                                    break;
                            }
                            _objProg.code.Append($"{instuct} eax, {rightChild.token.value}\n");
                        }
                        else
                        {
                            Translation(rightChild, z_buffer + 1);
                        }*/


                        //Translation(leftChild, z_buffer + 1);
                        if (leftChild.type == "NUMBER")
                            _objProg.code.Append($"mov eax, {leftChild.token.value}\n");
                        else if (leftChild.type == "VAR")
                            _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                        else
                            Translation(leftChild, z_buffer + 1);
                        _objProg.code.Append("push eax\n");

                        //Translation(rightChild, z_buffer + 1);
                        if (rightChild.type == "NUMBER")
                            _objProg.code.Append($"mov eax, {rightChild.token.value}\n");
                        else if (rightChild.type == "VAR")
                            _objProg.code.Append($"mov eax, [{rightChild.token.value}]\n");
                        else
                            Translation(rightChild, z_buffer + 1);
                        _objProg.code.Append("mov ebx, eax\n");

                        _objProg.code.Append("pop eax\n");



                        string instuct = string.Empty;
                        switch (root.token.value)
                        {
                            case "+":
                                _objProg.code.Append($"add eax, ebx\n");
                                break;
                            case "-":
                                _objProg.code.Append($"sub eax, ebx\n");
                                break;
                            case "*":
                                _objProg.code.Append($"mul ebx\n");
                                break;
                            case "/":
                                _objProg.code.Append("cdq\n");
                                _objProg.code.Append("idiv ebx\n");
                                break;
                        }
                        //_objProg.code.Append($"{instuct} eax, ebx\n");
                    }
                    /*if (root.token.value == "=")
                    {
                        CommonNode var = take(root, 0);
                        CommonNode type2 = take(var, 0);
                        CommonNode oper2 = take(root, 1);
                        if (!vars.Contains(root.token.value) && type2 != null)
                            _objProg.data.Append($"{var.token.value} {types[type2.token.value]} {oper2.token.value}\n");
                    }*/
                    break;
                case "NUMBER":
                    _objProg.code.Append($"mov eax, {root.token.value}\n");
                    break;
                case "CONST":
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
                    break;
                case "STRING":
                    if (!stringConsts.Keys.Contains(root.token.value))
                    {
                        stringConsts.Add(root.token.value, $"str_const_{stringConstsIndex}");
                        _objProg.data.Append($"str_const_{stringConstsIndex} db {root.token.value}");
                        stringConstsIndex++;
                    }
                    _objProg.code.Append($"mov eax, {stringConsts[root.token.value]}\n");
                    break;
                case "FUNC":
                    setWriteData(CodeData.procData);
                    //CommonNode types2 = take(root, 0);
                    CommonNode signature = take(root, 1);
                    string args = string.Empty;
                    for (int i = 0; i < signature.childs.Count; i++)
                    {
                        if (typesarg.ContainsKey(signature.childs[i].childs[0].token.value))
                            args += $"{signature.childs[i].token.value}:{typesarg[signature.childs[i].childs[0].token.value]}";
                        else
                            args += $"{signature.childs[i].token.value}:{signature.childs[i].childs[0].token.value}";
                        args += " ";
                    }
                    _objProg.code.Append($"proc uses eax, {args}\n");
                    local();
                    Translation(take(root, 2), z_buffer + 1);
                    local();
                    _objProg.code.Append($"endp\n");
                    local();
                    setWriteData(CodeData.codeData);
                    break;
                case "BODY":
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        Translation(root.childs[i], z_buffer + 1);
                    }
                    break;

            }
            /*for (int i = 0; i < root.childs.Count; i++)
            {
                Translation(root.childs[i], z_buffer + 1);
            }*/
        }
        /*public void transNode ()
        {
            if (peek("VAR"))
            {
                string varName = take().token.value;
                if (peek("TYPE"))
                {
                    string typeOper = types[take().token.value];
                    //objProg.data.Add("  ");
                    //if (peek("OPER") && take().token.)
                }
            }
        }*/
        public void Compilation (string nameFile)
        {
            //string path = "/compile" + "/" + nameFile.Replace(".qs", "") + "/" + nameFile;
            string path = $"/compile/{nameFile}/{nameFile}.asm";

            var proc = new Process();
            proc.StartInfo.FileName = fasmCompilerPath;
            proc.StartInfo.Arguments = path;
            proc.Start();

            proc.WaitForExit();
            proc.Close();
        }
    }
}

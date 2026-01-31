using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private List<CommonNode> list;
        private ProgramNode ProgramAst;
        private string refVarStr = string.Empty;

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
        public Compiler(string _fasmCompilerPath, ProgramNode ast) { fasmCompilerPath = _fasmCompilerPath; ProgramAst = ast; }

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
                    if (root.childs.Count == 0)
                    {
                        _objProg.code.Append($"mov eax, [{root.token.value}]\n");
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
                case "REFVAR":
                    /*CommonNode var = take(root, 0);
                    
                    if (var.type == "REFVAR")
                    {
                        refVarStr += "." + var.token.value;
                        var.token.value = refVarStr;
                        Translation(var, z_buffer + 1);
                    } else
                    {
                        var.token.value = root.token.value + "." + var.token.value;
                        Translation(var, z_buffer + 1);
                        refVarStr = string.Empty;
                    }*/

                    break;
                case "BINOPER":
                    if (root.token.value == "=")
                    {
                        // child
                        CommonNode varChild = take(root, 0); // eax
                        CommonNode rightChild = take(root, 1);
                        //_objProg.code.Append("xor eax, eax\n");
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
                            _objProg.code.Append($"mov [{varChild.token.value}], [{stringConsts[rightChild.token.value]}]\n");
                            //_objProg.code.Append($"mov [{varChild.token.value}], eax\n");
                            break;
                        }
                        Translation(rightChild, z_buffer + 1);
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

                        if (new string[] { leftChild.type, rightChild.type }.Contains("VAR"))
                        {
                            _objProg.code.Append($"mov eax, [{leftChild.token.value}]\n");
                            switch (root.token.value)
                            {
                                case "+":
                                    _objProg.code.Append($"add eax, eax\n");
                                    break;
                                case "-":
                                    _objProg.code.Append($"sub eax, eax\n");
                                    break;
                                case "*":
                                    _objProg.code.Append($"mul eax\n");
                                    break;
                                case "/":
                                    _objProg.code.Append("cdq\n");
                                    _objProg.code.Append("idiv eax\n");
                                    break;
                            }
                            break;
                        }


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


                    switchinstruct:
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
                    _objProg.code.Append($"; FUNC {root.token.value}\n");
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
                    _objProg.code.Append($"proc {root.token.value} uses eax, {args}\n");
                    local();
                    Translation(take(root, 2), z_buffer + 1);
                    _objProg.code.Append($"endp\n");
                    local();
                    setWriteData(CodeData.codeData);
                    break;
                case "INLINE":
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
                    break;
                case "CALL":
                    //CommonNode types2 = take(root, 0);
                    parseCall(root, z_buffer);

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
            
            /*for (int i = 0; i < root.childs.Count; i++)
            {
                Translation(root.childs[i], z_buffer + 1);
            }*/
        }
        public void parseCall (CommonNode root, int z_buffer)
        {
            _objProg.code.Append($"; CALL {root.token.value}\n");
            CommonNode signatureCall = take(root, 0);
            if (ProgramAst.inlineNames.Contains(root.token.value))
            {
                _objProg.code.Append($"{root.token.value} ");
                string args = string.Empty;
                for (int i = 0; i < signatureCall.childs.Count; i++)
                {
                    if (signatureCall.childs[i].type == "NUMBER" || signatureCall.childs[i].type == "VAR")
                        args += signatureCall.childs[i].token.value;
                    else
                        throw new Exception("У Токена:" + signatureCall.childs[i].token.pos+"ошибка в вызове инлайн функции может быть только переменная или число!!");
                    if (i < signatureCall.childs.Count-1)
                        args += ",";
                }
                _objProg.code.Append($"{args}\n");
                return;
            }

            for (int i = signatureCall.childs.Count - 1; i >= 0; i--)
            {
                Translation(take(signatureCall, i), z_buffer + 1);
                _objProg.code.Append($"push eax\n");
            }
            _objProg.code.Append($"call {root.token.value}\n");
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
        /*
         section '.idata' import data readable
            library kernel, 'kernel32.dll',\
                    msvcrt, 'msvcrt.dll',\
                    user, 'user32.dll'
            import kernel,\
                ExitProcess, 'ExitProcess',\
                SetConsoleTitle, 'SetConsoleTitleA',\
                GetStdHandle, 'GetStdHandle',\
                SetConsoleTextAttribute, 'SetConsoleTextAttribute',\
                SetConsoleCursorPosition, 'SetConsoleCursorPosition',\
                GetProcessHeap, 'GetProcessHeap',\
                HeapAlloc, 'HeapAlloc',\
                HeapFree, 'HeapFree',\
                VirtualAlloc, 'VirtualAlloc',\
                VirtualFree, 'VirtualFree',\
                WriteConsoleA, 'WriteConsoleA'
            import msvcrt,\
                printf, 'printf',\
                getch, '_getch',\
                scanf, 'scanf'
            import user,\
                MessageBox, 'MessageBoxA'
         */
        public string ConcatData (TypeApp typeApp)
        {
            string file = string.Empty;
            file += _objProg.includes.ToString();
            file += "\n";
            file += _objProg.macroData.ToString();
            file += "\nsection '.data' data readable writable\n";
            file += _objProg.data.ToString();
            file += "\nsection '.code' code readable executable\n";
            file += _objProg.procData.ToString();
            // section '.code' code readable executable
            // section '.data' data readable writable

            if (typeApp == TypeApp.asmmodule)
            {
                
            }else if (typeApp == TypeApp.dll)
            {
                file += "\nsection '.code' code readable executable\n";
                file = "PE DLL\n\n" + file;
            }
            else if (typeApp == TypeApp.program32)
            {
                file = "PE console\n\nentry start\n\n" + file;
                file += "\nsection '.code' code readable executable\n";
                file += "    start:\n";
                file += _objProg.codeData.ToString();
            }else if (typeApp == TypeApp.program64)
            {
                file = "PE console\n\nentry start\n\n" + file;
                file += "\nsection '.code' code readable executable\n";
                file += "    start:\n";
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
            Console.ReadKey();
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

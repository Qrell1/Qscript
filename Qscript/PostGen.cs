using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    enum arg
    { 
        r,v,m,n,c,o,t,s
    }

    public class patternNode {
        public string key;
        public string value;

        public patternNode(string _key, string _value) { key = _key; value = _value; }
    }
    public class inst
    {
        public string value = "";
        public List<patternNode> pattern = new List<patternNode>();

        public override string ToString() 
        {
            string resualt = value + " ";
            int i = 0;
            bool flag = true;
            foreach (patternNode node in pattern)
            {
                if (node.key == "t" && !flag) flag = true;
                else if (node.key == "t") continue;
                else flag = false;

                resualt += node.value;
                i++;
            }
            if (resualt.Length > 0) resualt.Remove(resualt.Length - 1);
            if (resualt.Replace(" ", "").Length == 0) resualt = "";
            if (resualt.Length > 0) resualt += "\n";
            return resualt;
        }
    }

    // str
    // NODE INSTRACT -> ARGS (m,v,r,c)
    public static class PostGen
    {
        public static readonly Dictionary<string, string> match = new Dictionary<string, string>()
        {
            //{ "r", "(eax|edx|ebx|ecx|esi|edi|esp|ebp)"},
            { "t", " "},
            { "ri", @".reg[0-9]*[a-zA-Z]*"},
            { "r", @"\b(rax|rdx|rbx|rcx|rsi|rdi|rsp|rbp|eax|edx|ebx|ecx|esi|edi|esp|ebp|al|dx|bx|cx|si|di|sp|bp)\b"},
            //{ "c", @"\b[A-Z_A-Z_]+[0-9]*\b"},
            { "i", "\\.?[a-z\\\\.A-Z_][a-z\\\\.A-Z\\\\.0-9_]*\\:" },
            { "m", "\\[[^\\[\\]]+\\]" },
            //{ "m", @"\b\[^\[\]+\]\b" },
            //{ "m", @"\[^\[\]+\]" },
            { "v", @"\b[\\.a-z\\.A-Z_][\\.a-z\\.A-Z\\.0-9_]*\b" },
            { "c", @"\b[A-Z_A-Z_]+[0-9]*\b"},
            //{ "c", @"\b[A-Z_A-Z_]+[0-9]*\b"},
            { "n", "-?[0-9]+" },
            { "o", "(/|\\*|\\-|\\+)"},
            { "s", "'[^'']*'" },
            { "ts", "(\\.|\\,)"},
            { "fg", "(\\{|\\})"},
            { "l", "(\n|\t)"}
            
            //{ "cm", @";^\[\]"}
        };
        public static readonly string[] registers =
        {
                "rax",
                "rdx",
                "rbx",
                "rcx",
                "rsi",
                "rdi",
                "rsp",
                "rbp",
                "eax",
                "edx",
                "ebx",
                "ecx",
                "esi",
                "edi",
                "esp",
                "ebp",
                "al",
                "dx",
                "bx",
                "cx",
                "si",
                "di",
                "sp",
                "bp",
        };

        public static ProgramNode ProgramAst;

        //public static  pattern1;// = {{  }, {" mov","m","eax" }};
        //public static List<> patterns;
        static PostGen ()
        {

        }

        public static objProgram PostTranslation(objProgram _objProgram, ProgramNode _ProgramAst)
        {
            objProgram objProgramResualt = new objProgram();
            objProgramResualt.stringsConsts = _objProgram.stringsConsts;
            objProgramResualt.macroData = _objProgram.macroData;
            objProgramResualt.codeData = _objProgram.codeData;
            objProgramResualt.includes = _objProgram.includes;
            
            ProgramAst = _ProgramAst;

            int line = 0;
            Dictionary<string, string> varGlobal = new Dictionary<string, string>();
            objProgramResualt.data = new StringData();
            for (int i = 0; i < _objProgram.data.Length; i++)
            {
                string[] strs = _objProgram.data.Data[i].Split(' ');
                if (!varGlobal.ContainsKey(strs[0])) varGlobal.Add(strs[0], strs[1]);
                if (strs[1].First() == '*') strs[1] = "dd";
                string str = string.Empty;
                foreach (string s in strs) str += s + " ";
                objProgramResualt.data.Append(str);
            }

            StringData procData = new StringData();
            List<inst> procList = LexInstructs(_objProgram.procData);
            procList = LexInstructs(RegisterMachine(procList, true));
            //line = procList.Count;
            while (true)
            {
                objProgramResualt.procData = Translation(procList, varGlobal);
                procList.Clear();
                procList = LexInstructs(objProgramResualt.procData);
                objProgramResualt.procData = new StringData();
                objProgramResualt.procData = IndicatorsCorrection(procList, varGlobal);
                //objProgramResualt.procData = Translation(procList, varGlobal);
                if (line <= objProgramResualt.procData.Data.Count) break;
                line = objProgramResualt.procData.Data.Count;
            }

            StringData codeData = new StringData();
            List<inst> codeList = LexInstructs(_objProgram.codeData);
            codeList = LexInstructs(RegisterMachine(codeList, false));
            objProgramResualt.codeData = Translation(codeList, varGlobal);
            //line = objProgramResualt.codeData.Data.Count;
            while (true)
            {
                codeList = LexInstructs(objProgramResualt.codeData);
                objProgramResualt.codeData = Translation(codeList, varGlobal);
                codeList.Clear();
                codeList = LexInstructs(objProgramResualt.codeData);
                objProgramResualt.codeData = IndicatorsCorrection(codeList, varGlobal);
                if (line <= objProgramResualt.codeData.Data.Count) break;
                line = objProgramResualt.codeData.Data.Count;
            }

            //objProgramResualt.codeData = _objProgram.codeData;


            return objProgramResualt;
        }

        public static List<inst> LexInstructs (StringData data)
        {
            List<inst> resualtList = new List<inst>();
            string str;
            string instruct;
            for (int i = 0; i < data.Length; i++)
            {
                str = data.Data[i];
                str = str.Trim();
                instruct = str.Split(' ')[0];
                if (str.StartsWith(";")) continue;
                int pos = instruct.Length;
                inst patt = new inst();
                patt.value = instruct.Replace("\t", "").Replace("\n", "");
                while (true)
                {
                    if (pos >= str.Length) break;
                    bool done = false;
                    foreach (var key in match)
                    {
                        Match regx = Regex.Match(str.Substring(pos), "^" + key.Value);
                        if (regx.Success && !string.IsNullOrEmpty(regx.Value))
                        {
                            patt.pattern.Add(new patternNode(key.Key, regx.Value));
                            pos += regx.Value.Length;
                            done = true;
                        }
                    }
                    //if (!done) pos++;
                }
                //List<patternNode> clearingList = new List<patternNode>();
                //for (int j = 0; j < patt.pattern.Count; j++)
                //{ if (patt.pattern[j].key != "t" && patt.pattern[j].key != "l") clearingList.Add(patt.pattern[j]); // && patt.pattern[j].key != "ts"
                //}
                //patt.pattern = clearingList;
                resualtList.Add(patt);
            }
            return resualtList;
        }
        public static Dictionary<string, string> CheakRegisterFunctions(List<inst> instructs)
        {
            bool func = false;
            string funcName = string.Empty;

            List<string> regs = new List<string>();

            Dictionary<string, string> resualt = new Dictionary<string, string>();

            for (int i = 0; i < instructs.Count; i++)
            {
                inst _inst = instructs[i];
                if (_inst.value ==  "proc")
                {
                    foreach (var pat in _inst.pattern) { if (pat.key == "v") {  func = true; funcName = pat.value.Trim(); break; } }
                }

                if (func)
                {
                    foreach (var pat in _inst.pattern)
                    {
                        if (pat.key == "r" && !regs.Contains(pat.value))
                            regs.Add(pat.value);
                        else if (pat.key == "m")
                        {
                            string[] strs = pat.value.Remove(pat.value.Length-1, 1).Remove(0,1).Split('+','-','*','/',' ');
                            foreach (var str in strs)
                            {
                                string _str = str.Trim();
                                if (registers.Contains(_str) && !regs.Contains(_str)) regs.Add(_str);
                            }
                        }
                    }
                }

                if (_inst.value == "endp")
                {
                    func = false;
                    // TODO: Сделать финализацию сбора информации о функции
                }

            }

            return resualt;
        }
        public static StringData RegisterMachine(List<inst> instructs, bool line)
        {
            Console.WriteLine("Start RegisterMachine...");
            StringData resualt = new StringData();
            Dictionary<string, string> tableRegisters = new Dictionary<string, string>();
            Dictionary<string, int> timelineRegisters = new Dictionary<string, int>();
            string[] asmRegisters = { "ecx", "edx", "edi", "esi", "ebx", "eax" };
            if (line) asmRegisters = new string[] { "ebx", "esi", "edi", "edx", "ecx", "eax" };

            Random rand = new Random();
            bool regUsed (string reg)
            {
                int count = 0;
                foreach (var value in tableRegisters.Values)
                {
                    if (value == reg) count++;
                }
                return (count > 1) ? true : false;
            }
            string getFreeReg(string reg)
            {
                if (reg.EndsWith("x") || reg.EndsWith("l") || reg.EndsWith("h"))
                {
                    string regPrefer = reg.Remove(0, 4);
                    string number = string.Empty;
                    try
                    {
                        for (int i = 0; i < regPrefer.Length; i++)
                        {
                            number += regPrefer[i];
                            int temp = Convert.ToInt32(number);
                        }
                    } catch { number = number.Remove(number.Length-1, 1); }
                    regPrefer = regPrefer.Replace(number, "");

                    if (!tableRegisters.ContainsValue(regPrefer)) return regPrefer;
                    else return "push" + regPrefer;
                }

                for (int i = 0; i < asmRegisters.Length; i++)
                {
                    if (!tableRegisters.ContainsValue(asmRegisters[i])) return asmRegisters[i];
                }
               
                return "push" + asmRegisters[rand.Next(0,5)];
            }
            for (int i = 0; i < instructs.Count; i++)
            {
                inst _inst = instructs[i];
                foreach (var pat in _inst.pattern)
                {
                    if (pat.key == "ri")
                    {
                        if (!timelineRegisters.ContainsKey(pat.value)) timelineRegisters.Add(pat.value, i);
                        else timelineRegisters[pat.value] = i;
                    }
                    else if (pat.key == "m")
                    {
                        string[] strs = pat.value.Remove(pat.value.Length-1,1).Remove(0,1).Split('+','-','*','/',' ');
                        foreach (string str in strs)
                        {
                            string strtrim = str.Trim();
                            if (strtrim.StartsWith(".reg"))
                            {
                                if (!timelineRegisters.ContainsKey(strtrim)) timelineRegisters.Add(strtrim, i);
                                else timelineRegisters[strtrim] = i;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < instructs.Count; i++)
            {
                inst _inst = instructs[i];

                foreach (var pat in _inst.pattern)
                {
                    if (pat.key == "ri") Console.WriteLine(pat.value);
                    if (pat.key == "ri")
                    {
                        if (!tableRegisters.ContainsKey(pat.value))
                        {
                            string freeReg = getFreeReg(pat.value);
                            tableRegisters.Add(pat.value, (freeReg.StartsWith("push")) ? freeReg.Remove(0, 4).ToString() : freeReg);
                            if (freeReg.StartsWith("push")) resualt.Append($"push {freeReg.Remove(0,4)}\n");
                        } else
                        {
                            if (regUsed(tableRegisters[pat.value])) resualt.Append($"pop {pat.value}\n");
                        }
                        string oldReg = pat.value;
                        pat.value = tableRegisters[pat.value];
                        pat.key = "r";
                        if (timelineRegisters[oldReg] == i) tableRegisters.Remove(oldReg);
                    }
                    else if (pat.key == "m")
                    {
                        string[] strs = pat.value.Remove(pat.value.Length - 1, 1).Remove(0, 1).Split('+', '-', '*', '/', ' ');
                        foreach (string str in strs)
                        {
                            string strtrim = str.Trim();
                            if (strtrim.StartsWith(".reg"))
                            {
                                if (!tableRegisters.ContainsKey(strtrim))
                                {
                                    string freeReg = getFreeReg(strtrim);
                                    tableRegisters.Add(strtrim, (freeReg.StartsWith("push")) ? freeReg.Remove(0, 4).ToString() : freeReg);
                                    if (freeReg.StartsWith("push")) resualt.Append($"push {freeReg.Remove(0, 4)}\n");
                                }
                                else
                                {
                                    if (regUsed(tableRegisters[strtrim])) resualt.Append($"pop {strtrim}\n");
                                }
                                pat.value = pat.value.Replace(strtrim, tableRegisters[strtrim]);
                                if (timelineRegisters[strtrim] == i) tableRegisters.Remove(strtrim);
                            }
                        }
                    }
                }

                resualt.Append(_inst.ToString());
            }
            //Console.WriteLine(resualt.ToString()); Console.ReadLine();
            return resualt;
        }
        public static StringData IndicatorsCorrection(List<inst> instructs, Dictionary<string, string> vars)
        {
            /*
             * Создать Строки Данных
             * Пробежаться по всем строкам
             * Пробежаться по всем патерннам 
             * И найти работу над памятью
             */
            Console.WriteLine("Start IndicatorsCorrection...");
            StringData resualt = new StringData();
            List <string> args = new List<string>();
            for (int i = 0; i < instructs.Count; i++)
            {
                inst _inst = instructs[i];
                List<patternNode> patterns = new List<patternNode>();
                foreach (var child in _inst.pattern) if (child.key != "t" && child.key != "ts") patterns.Add(child);

                // proceture preparing
                // // proc Name arg1:DWORD arg2:WORD
                if (_inst.value == "proc")
                {
                    string resualtProcString = $"proc {patterns[0].value} ";
                    for (int j = 1; j < patterns.Count; j++)
                    {
                        patterns[j].value = patterns[j].value.Remove(patterns[j].value.Length - 1, 1);
                        if (!Compiler.typesarg.ContainsValue(patterns[j + 1].value)) {
                            resualtProcString += $" {patterns[j].value}:DWORD ,";
                            if (vars.ContainsKey(patterns[j].value))
                            { vars[patterns[j].value] = "*" + patterns[j + 1].value; }
                            else { vars.Add(patterns[j].value, "*" + patterns[j + 1].value); }
                        }
                        else {
                            resualtProcString += $" {patterns[j].value}:{patterns[j + 1].value} ,";
                            if (vars.ContainsKey(patterns[j].value))
                            { vars[patterns[j].value] = patterns[j + 1].value; }
                            else { vars.Add(patterns[j].value, patterns[j + 1].value); }
                        }
                        args.Add(patterns[j].value);
                        j++;
                    }
                    if (patterns.Count > 1) resualtProcString = resualtProcString.Remove(resualtProcString.Length - 1, 1);
                    Console.WriteLine("Resualt Proc - " + resualtProcString);
                    resualt.Append(resualtProcString + "\n");
                    continue;
                }

                // endp preparing
                if (_inst.value == "endp")
                {
                    foreach (var child in args) { Console.WriteLine(child);
                        vars.Remove(child);}
                    args.Clear();
                }

                // local vars
                // // local Name dd 0
                if (_inst.value == "local")
                {
                    string resualtLocalString = $"local {patterns[0].value.Trim()} ";
                    Console.WriteLine(resualtLocalString + " || " + patterns[1]);
                    if (patterns[1].value.Trim().First() == '*')
                    {
                        resualtLocalString += $" dd 0\n";
                        if (!vars.ContainsKey(patterns[0].value)) vars.Add(patterns[0].value.Trim(), "*" + patterns[2].value);
                        else vars[patterns[0].value] = "*" + patterns[2].value;
                    }
                    else
                    {
                        resualtLocalString += $" {patterns[1].value} 0\n";
                        if (!vars.ContainsKey(patterns[0].value)) vars.Add(patterns[0].value.Trim(), patterns[1].value.Trim());
                        else vars[patterns[0].value] = patterns[1].value.Trim();
                    }
                    resualt.Append(resualtLocalString);
                    continue;
                }


                // memory preparing
                // // mov eax, [List.Node.next]
                for (int j = 0; j < _inst.pattern.Count; j++)
                {
                    if (_inst.pattern[j].key == "m"
                        && !_inst.pattern[j].value.Contains("+")
                        && !_inst.pattern[j].value.Contains("-")
                        && !_inst.pattern[j].value.Contains("*")
                        && !_inst.pattern[j].value.Contains("/")
                        )
                    {
                        string resualtMemory = string.Empty;
                        string memory = _inst.pattern[j].value.Trim().Remove(0,1);
                        memory = memory.Remove(memory.Length-1, 1);
                        string[] strs = memory.Split('.', ',');
                        string type = string.Empty;
                        string typeVar = string.Empty;
                        if (strs.Length >= 2)
                        {
                            Console.WriteLine("var: " + strs[0]);
                            type = vars[strs[0]];
                            if (type.First() == '*') { resualtMemory += "ebx" + " + "; resualt.Append($"mov ebx, [{strs[0]}]\n"); }
                            else resualtMemory += strs[0];
                            
                            for (int k = 1; k < strs.Length; k++)
                            {
                                Console.WriteLine(strs[k] + " | " + type);
                                if (type.First() == '*' || typeVar == "INDICATOR")
                                {   
                                    if (type.First() == '*') type = type.Remove(0, 1);
                                    if (k+1 < strs.Length)
                                    {
                                        resualtMemory += $"{type}.{strs[k]}";
                                        resualt.Append($"mov ebx, [{resualtMemory}]\n");
                                        resualtMemory = "ebx" + "+";
                                    }
                                    else resualtMemory += $"{type}.{strs[k]}+";
                                }
                                else resualtMemory += "." + strs[k];
                                Console.WriteLine("-- " + strs[k] + " | " + type);
                                typeVar = ProgramAst.structs[type][strs[k]].type;
                                type = ProgramAst.structs[type][strs[k]].token.value;
                            }
                            if (resualtMemory.EndsWith("+")) resualtMemory = resualtMemory.Remove(resualtMemory.Length-1, 1);
                        } else
                        {
                            resualtMemory = strs[0];
                        }
                        _inst.pattern[j].value = "[" + resualtMemory + "]";
                        if (type != string.Empty) for (int k = 0; k < _inst.pattern.Count; k++)
                            {
                                if (_inst.pattern[k].value == "esi")
                                {
                                    if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                        _inst.pattern[k].value = "esi";
                                    else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                    {
                                        string reg = string.Empty;
                                        switch (Compiler.typesregs[type])
                                        {
                                            case "rax": _inst.pattern[k].value = "rsi"; break;
                                            case "eax": _inst.pattern[k].value = "esi"; break;
                                            case "ax": _inst.pattern[k].value = "si"; break;
                                            default: _inst.pattern[k].value = "ah"; break;
                                        }
                                    }
                                }      // DELETE: Временное решение fix1
                                else if (_inst.pattern[k].value == "edi")
                                {
                                    if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                        _inst.pattern[k].value = "edi";
                                    else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                    {
                                        string reg = string.Empty;
                                        switch (Compiler.typesregs[type])
                                        {
                                            case "rax": _inst.pattern[k].value = "rdi"; break;
                                            case "eax": _inst.pattern[k].value = "edi"; break;
                                            case "ax": _inst.pattern[k].value = "di"; break;
                                            default: _inst.pattern[k].value = "dh"; break;
                                        }
                                    }
                                } // DELETE: Временное решение fix1
                                else if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                    _inst.pattern[k].value = "eax".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                    _inst.pattern[k].value = GetReg(type).Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                if (_inst.pattern[k].key == "r" 
                                    && _inst.pattern[k].value != "esi" 
                                    && _inst.pattern[k].value != "edi" 
                                    && !_inst.pattern[k].value.StartsWith("e")) _inst.value = (_inst.value == "mov") ? "movzx" : _inst.value;
                                //if (_inst.pattern[k].key == "r" && _inst.value == "mov" && !_inst.pattern[k].value.Contains("e")) _inst.value = "movzx";
                            }
                        else
                        {
                            try // FIXME: fix1 Как же мне всё таки сделать чтобы esi && edi не попадалюсь на 8 битные задачи
                            {
                                type = vars[strs[0]];
                                for (int k = 0; k < _inst.pattern.Count; k++)
                                { // rax eax ax al ah
                                    if (_inst.pattern[k].value == "esi")
                                    {
                                        if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                            _inst.pattern[k].value = "esi";
                                        else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                        {
                                            string reg = string.Empty;
                                            switch (Compiler.typesregs[type])
                                            {
                                                case "rax": _inst.pattern[k].value = "rsi"; break;
                                                case "eax": _inst.pattern[k].value = "esi"; break;
                                                case "ax": _inst.pattern[k].value = "si"; break;
                                                default: _inst.pattern[k].value = "ah"; break;
                                            }
                                        }
                                        else if (_inst.pattern[k].key == "r" && type == "QWORD") _inst.pattern[k].value = "rsi";
                                        else if (_inst.pattern[k].key == "r" && type == "DWORD") _inst.pattern[k].value = "esi";
                                        else if (_inst.pattern[k].key == "r" && type == "WORD") _inst.pattern[k].value = "si";
                                    }      // DELETE: Временное решение fix1
                                    else if (_inst.pattern[k].value == "edi")
                                    {
                                        if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                            _inst.pattern[k].value = "edi";
                                        else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                        {
                                            string reg = string.Empty;
                                            switch (Compiler.typesregs[type])
                                            {
                                                case "rax": _inst.pattern[k].value = "rdi"; break;
                                                case "eax": _inst.pattern[k].value = "edi"; break;
                                                case "ax": _inst.pattern[k].value = "di"; break;
                                                default: _inst.pattern[k].value = "dh"; break;
                                            }
                                        }
                                        else if (_inst.pattern[k].key == "r" && type == "QWORD") _inst.pattern[k].value = "rdi";
                                        else if (_inst.pattern[k].key == "r" && type == "DWORD") _inst.pattern[k].value = "edi";
                                        else if (_inst.pattern[k].key == "r" && type == "WORD") _inst.pattern[k].value = "di";
                                    } // DELETE: Временное решение fix1
                                    else if (_inst.pattern[k].key == "r" && typeVar == "INDICATOR")
                                        _inst.pattern[k].value = "eax".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    else if (_inst.pattern[k].key == "r" && Compiler.typesregs.ContainsKey(type))
                                        _inst.pattern[k].value = GetReg(type).Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    else if (_inst.pattern[k].key == "r" && type == "QWORD")
                                        _inst.pattern[k].value = "rax".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    else if (_inst.pattern[k].key == "r" && type == "DWORD")
                                        _inst.pattern[k].value = "eax".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    else if (_inst.pattern[k].key == "r" && type == "WORD")
                                        _inst.pattern[k].value = "ax".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    else if (_inst.pattern[k].key == "r" && type == "BYTE")
                                        _inst.pattern[k].value = "al".Replace("a", Compiler.regschars[_inst.pattern[k].value]);
                                    //if (_inst.pattern[k].key == "r" && _inst.value == "mov" && !_inst.pattern[k].value.Contains("e")) _inst.value = "movzx";
                                    if (_inst.pattern[k].key == "r" 
                                        && !_inst.pattern[k].value.StartsWith("e")) _inst.value = (_inst.value == "mov") ? "movzx" : _inst.value;
                                }
                            }
                            catch { }
                        }
                    }
                }

                resualt.Append(InstructConcat(_inst));
            }
            foreach (var child in vars) Console.WriteLine($" Key: {child.Key} --Value: {child.Value}");
            return resualt;
        }

        public static StringData Translation (List<inst> instructs, Dictionary<string, string> varGlobal)
        {
            StringData resualt = new StringData();
            bool func = false;
            Dictionary<string, string> varLocal = new Dictionary<string, string>();
            List<string> args = new List<string>();
            for (int i = 0; i < instructs.Count; i++)
            {
                inst _instuct = instructs[i];
                inst _instructSecond = null;
                bool done = false;
                try
                {
                    _instructSecond = instructs[i + 1];

                    (List<patternNode> pattern1, string str) = GetPattern(_instuct);
                    (List<patternNode> pattern2, string str2) = GetPattern(_instructSecond);

                    bool flagI = true;
                    if (pattern1.Count == pattern2.Count)
                    {
                        for (int p = 0; p < pattern1.Count; p++)
                        {
                            if (pattern1[p].value == pattern2[p].value) { }
                            else { flagI = false; }
                        }

                        if (flagI && _instuct.value == _instructSecond.value && _instuct.value != "push" && _instuct.value != "pop")
                        {
                            resualt.Append(InstructConcat(_instuct));
                            i++;
                            continue;
                        }
                    }
                    if (pattern1.Count == pattern2.Count && pattern1.Count == 2 && _instuct.value == _instructSecond.value &&
                        pattern1[0].value == pattern2[1].value && pattern1[1].value == pattern2[0].value)
                    {
                        resualt.Append(InstructConcat(_instuct)); i++;
                        continue;
                    }
                    if (_instuct.value == "imul" && pattern1[1].key == "n"  && Convert.ToInt32(pattern1[1].value) == 2)
                    {
                        resualt.Append($"shl {pattern1[0].value}, 1");
                        continue;
                    }
                    if (_instuct.value == "imul" && pattern1[1].key == "n" && Convert.ToInt32(pattern1[1].value) == 4)
                    {
                        resualt.Append($"shl {pattern1[0].value}, 2");
                        continue;
                    }
                    if (_instuct.value == "imul" && pattern1[1].key == "n" && Convert.ToInt32(pattern1[1].value) == 8)
                    {
                        resualt.Append($"shl {pattern1[0].value}, 3");
                        continue;
                    }
                    /*if (_instuct.value == "mov" && pattern1[0].key == "m" && 
                        pattern1[1].key == "r" && pattern2[0].key == "r" && pattern1[1].value == pattern2[0].value)
                    {
                        continue;
                    }*/
                    // |case1| -- global
                    //if (InstructPattern(str, str2) && _instuct.value == "mov")
                    //{
                    //resualt.Append(InstructConcat(_instuct)); i++;
                    //continue;
                    //}
                    // |case1| -- cmp
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "cmp|rn") && InstructCmpReg(pattern1, pattern2))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        //continue;
                    }
                    // |case2| -- cmp
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "cmp|rc") && InstructCmpReg(pattern1, pattern2))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        //continue;
                    }
                    // |case3| - cmp
                    if (InstructPattern(str, "mov|rn") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "n"))); i++;
                        continue;
                    }
                    // |case4| - cmp - not realistic
                    if (InstructPattern(str, "mov|rr") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "r", 1))); i++;
                        continue;
                    }
                    // |case5| - cmp
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "c"))); i++;
                        //continue;
                    }
                    // |case6| - call
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        //continue;
                    }
                    // |case7| - call
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "c"))); i++;
                        continue;
                    }
                    // |case8| - call
                    if (InstructPattern(str, "mov|rn") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "n"))); i++;
                        continue;
                    }
                    // |case9| - void
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "r"))); i++;
                        //continue;
                    }
                    // |case10| - void
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "r"))); i++;
                        continue;
                    }
                    // |case11| - void
                    if (InstructPattern(str, "pop|r") && InstructPattern(str2, "mov|mr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "m"))); i++;
                        //continue;
                    }
                    // |case12| - void
                    if (InstructPattern(str, "mov|rr") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "r"))); i++;
                        continue;
                    }

                    // |case13| - void
                    if (InstructPattern(str, "mov|rn") && InstructPattern(str2, "push|n") && pattern1[1].value == pattern2[0].value)
                    {
                        resualt.Append(InstructConcat(_instructSecond)); i++;
                        continue;
                    }

                    // |case14| - void
                    if (InstructPattern(str, "mov|mr") && InstructPattern(str2, "mov|rm") && pattern1[1].value == pattern2[0].value)
                    {
                        //resualt.Append(InstructConcat(_instuct)); i++;
                        //continue;
                    }
                }
                catch { }
                resualt.Append(InstructConcat(_instuct));
            }

            return resualt;
        }

        public static StringData TranslationPushPop(List<inst> instructs, Dictionary<string, string> varGlobal)
        {
            throw new NotImplementedException();
            StringData resualt = new StringData();
            Stack<Dictionary<string, bool>> pushregs = new Stack<Dictionary<string, bool>>();

            for (int i = 0; i < instructs.Count; i++)
            {
                inst _instuct = instructs[i];
                
                if (pushregs.Count == 0)
                {
                    resualt.Append(InstructConcat(_instuct));
                    continue;
                }

                if (_instuct.value == "push")
                {
                    int j = i + 1;
                    (List<patternNode> pattern, string str) = GetPattern(_instuct);
                    string value = pattern[0].value;
                    bool flag = false;
                    while (true)
                    {
                        if (instructs[j].value == "pop") break;
                        (List<patternNode> _pattern, string _str) = GetPattern(instructs[j]);
                        foreach (var pat in _pattern)
                        {
                            if (pat.value == value) flag = true;
                        }
                        j++;
                    }
                    if (flag)
                    {

                    }
                    continue;
                }
            }

            return resualt;
        }

        public static string GetReg (string type)
        {
            return Compiler.typesregs[type];
        }
        public static (List<patternNode>, string) GetPattern (inst _instruct)
        {
            List<patternNode> pattern1 = new List<patternNode>();

            string str = _instruct.value + "|";
            foreach (var key in _instruct.pattern) { if (key.key == "ts" || key.key == "t") continue; str += key.key; pattern1.Add(key); }

            return (pattern1, str);
        }
        public static inst CopyArgInstruct (inst left, inst right, string patt, int index = 0)
        {
            int pos = 0;
            for (int i = 0; i < right.pattern.Count; i++)
            {
                if (right.pattern[i].key == patt && index == 0) { pos = i; break; }
                else if (right.pattern[i].key == patt && index != 0) index--;
            }
            int pos2 = 0;
            while (true)
            {
                if (left.pattern[pos2].key.Equals("t") || left.pattern[pos2].key.Equals("ts")) pos2++;
                else break;
            }
            left.pattern[pos2] = right.pattern[pos];
            return left;
        }
        public static bool InstructPattern(string str, string patt)
        {
            string istr = str.Split('|')[0];
            string ipatt = patt.Split('|')[0];
            if (ipatt == "?") ipatt = "?";
            else if (ipatt != istr) return false;

            char[] cstr = str.Split('|')[1].ToCharArray();
            char[] cpatt = patt.Split('|')[1].ToCharArray();

            if (cstr.Length != cpatt.Length) return false;

            if (cpatt[0] == '~') return true;
            for (int i = 0; i < cstr.Length; i++)
            {
                if (cpatt[i] == '?') continue;
                else if (cpatt[i] == cstr[i]) { continue; }
                else return false;
            }
            return true;
        }
        public static bool InstructCmpReg (List<patternNode> pattern1, List<patternNode> pattern2, int i = 0)
        {
            if ((pattern1[0].key == "r" && pattern2[i].key == "r") && pattern1[0].value == pattern2[i].value) return true;
            return false;
        }
        public static bool InstructCmp (inst left, inst right)
        {
            for (int i = 0; i < left.pattern.Count; i++)
            {
                if (left.pattern[i].key != right.pattern[i].key) return false;
            }
            return true;
        }
        public static string InstructConcat (inst instruct)
        {
            string resualt = instruct.value + " ";
            int i = 0;
            bool flag = true;
            foreach (patternNode node in instruct.pattern)
            {
                //if (node.key == "t") continue;
                if (node.key == "t" && !flag) flag = true;
                else if (node.key == "t") continue;
                else flag = false;

                resualt += node.value; //+ " ";
                i++;
                //if (i < instruct.pattern.Count && i != 1) resualt += ", ";
            }
            if (resualt.Length > 0) resualt.Remove(resualt.Length - 1); 
            if (resualt.Replace(" ", "").Length == 0) resualt = "";
            if (resualt.Length > 0) resualt += "\n";
            return resualt;
        }
    }
}

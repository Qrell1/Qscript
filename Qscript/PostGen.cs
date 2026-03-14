using System;
using System.Collections.Generic;
using System.Data.Common;
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
    public class instruct
    {
        public string value = "";
        public List<patternNode> pattern = new List<patternNode>();
    }

    // str
    // NODE INSTRACT -> ARGS (m,v,r,c)
    public static class PostGen
    {
        public static Dictionary<string, string> match = new Dictionary<string, string>()
        {
            //{ "r", "(eax|edx|ebx|ecx|esi|edi|esp|ebp)"},
            { "t", " "},
            { "r", @"\b(rax|rdx|rbx|rcx|rsi|rdi|rsp|rbp|eax|edx|ebx|ecx|esi|edi|esp|ebp|al|dx|bx|cx|si|di|sp|bp)\b"},
            { "c", @"\b[A-Z_A-Z_]+[0-9]*\b"},
            { "i", "\\.?[a-z\\\\.A-Z_][a-z\\\\.A-Z\\\\.0-9_]*\\:" },
            { "m", "\\[[^\\[\\]]+\\]" },
            //{ "m", @"\b\[^\[\]+\]\b" },
            //{ "m", @"\[^\[\]+\]" },
            { "v", "[a-z\\.A-Z_][a-z\\.A-Z\\.0-9_]*" },
            { "n", "-?[0-9]+" },
            { "o", "(/|\\*|\\-|\\+)"},
            { "s", "'[^'']*'" },
            { "ts", @"(.|,)"},
            { "fg", "(\\{|\\})"},
            { "l", "(\n|\t)"}
            //{ "cm", @";^\[\]"}
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
            List<instruct> procList = LexInstructs(_objProgram.procData);
            //line = procList.Count;
            while (true)
            {
                objProgramResualt.procData = Translation(procList, varGlobal);
                procList = LexInstructs(objProgramResualt.procData);
                objProgramResualt.procData = Translation(procList, varGlobal);
                if (line <= objProgramResualt.procData.Data.Count) break;
                line = objProgramResualt.procData.Data.Count;
            }

            StringData codeData = new StringData();
            List<instruct> codeList = LexInstructs(_objProgram.codeData);
            objProgramResualt.codeData = Translation(codeList, varGlobal);
            //line = objProgramResualt.codeData.Data.Count;
            while (true)
            {
                codeList = LexInstructs(objProgramResualt.codeData);
                objProgramResualt.codeData = Translation(codeList, varGlobal);
                if (line <= objProgramResualt.codeData.Data.Count) break;
                line = objProgramResualt.codeData.Data.Count;
            }

            //objProgramResualt.codeData = _objProgram.codeData;


            return objProgramResualt;
        }

        public static List<instruct> LexInstructs (StringData data)
        {
            List<instruct> resualtList = new List<instruct>();
            string str;
            string instruct;
            for (int i = 0; i < data.Length; i++)
            {
                str = data.Data[i];
                str = str.Trim();
                instruct = str.Split(' ')[0];
                if (str.StartsWith(";")) continue;
                int pos = instruct.Length;
                instruct patt = new instruct();
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

        public static StringData Translation (List<instruct> instructs, Dictionary<string, string> varGlobal)
        {
            StringData resualt = new StringData();
            bool func = false;
            Dictionary<string, string> varLocal = new Dictionary<string, string>();
            for (int i = 0; i < instructs.Count; i++)
            {
                instruct _instuct = instructs[i];
                instruct _instructSecond = null;
                bool done = false;
                try // mov|rm | cmp|r? -~> cmp|m?  
                {
                    _instructSecond = instructs[i + 1];



                    List<patternNode> pattern1 = new List<patternNode>();
                    List<patternNode> pattern2 = new List<patternNode>();

                    string str = _instuct.value + "|";
                    foreach (var key in _instuct.pattern) { if (key.key == "ts" || key.key == "t") continue; str += key.key; pattern1.Add(key); }
                    string str2 = _instructSecond.value + "|";
                    foreach (var key in _instructSecond.pattern) { if (key.key == "ts" || key.key == "t") continue; str2 += key.key; pattern2.Add(key); }

                    if (InstructPattern(str, "?|rm") && _instuct.value != "lea" && !_instuct.value.EndsWith("zx"))
                    {
                        string pstr = pattern1[1].value;
                        pstr = pstr.Remove(pstr.Length - 1, 1);
                        pstr = pstr.Remove(0, 1); pstr = pstr.Trim();
                        if (!pstr.Contains(" ") &&
                            !pstr.Contains("+") &&
                            !pstr.Contains("-") &&
                            !pstr.Contains("*") &&
                            !pstr.Contains("/")
                            )
                        {
                            string[] strs = pstr.Split('.');
                            int n = 1;

                            string type = string.Empty;

                            if (!func) type = varGlobal[strs[0]];
                            else if (varLocal.ContainsKey(strs[0])) type = varLocal[strs[0]];
                            else type = varGlobal[strs[0]];

                            if (type.First() == '*') type.Remove(0, 1);
                            while (n < strs.Length)
                            {
                                type = ProgramAst.structs[type][strs[n]].token.value;
                                //if (n + 1 == strs.Length) {  }
                                n++;
                            }
                            int pos = 0;
                            while (pos < _instuct.pattern.Count)
                            {
                                if (_instuct.pattern[pos].key == "r") break;
                                pos++;
                            }

                            Dictionary<string, string> keys = new Dictionary<string, string>()
                            {
                                {"eax",    "a"},
                                {"ebx",    "b"},
                                {"edx",    "d"},
                                {"ecx",    "c"}
                            };
                            string reg = _instuct.pattern[pos].value;
                            if (type == "int32") _instuct.pattern[pos].value = $"e{keys[_instuct.pattern[pos].value]}x";
                            if (type == "int16") _instuct.pattern[pos].value = $"{keys[_instuct.pattern[pos].value]}x";
                            if (type == "int8") _instuct.pattern[pos].value = $"{keys[_instuct.pattern[pos].value]}l";

                            if (type == "dd") _instuct.pattern[pos].value = $"e{keys[_instuct.pattern[pos].value]}x";
                            if (type == "dw") _instuct.pattern[pos].value = $"{keys[_instuct.pattern[pos].value]}x";
                            if (type == "db") _instuct.pattern[pos].value = $"{keys[_instuct.pattern[pos].value]}l";

                            //resualt.Append($"xor {reg}, {reg}\n");
                            //resualt.Append(InstructConcat(_instuct));
                            //continue;
                        }
                    }

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

                    if (_instuct.value == "proc")
                    {
                        func = true;
                        int pos = 1;
                        while (true)
                        {
                            if (pattern1[pos].key == "i")
                            {
                                varLocal.Add(pattern1[pos].value.Remove(pattern1[pos].value.Length - 1, 1), pattern1[pos + 1].value);
                                pattern1[pos + 1].value = "DWORD";
                                pos += 2;
                            }
                            else { break; }
                        }
                        pos = 0;
                        while (true) { if (_instuct.pattern[pos].key == "v") break; else pos++; }
                        pos++;
                        for (int j = pos; j < _instuct.pattern.Count; j++)
                            if (_instuct.pattern[j].key == "v")
                                _instuct.pattern[j].value = "DWORD";
                        resualt.Append(InstructConcat(_instuct));
                        continue;
                    }
                    else if (_instuct.value == "endp") { func = false; varLocal.Clear(); resualt.Append("endp\n"); continue; }

                    if (func && _instuct.value == "local")
                    {
                        varLocal.Add(pattern1[0].value, pattern1[1].value);
                        resualt.Append(InstructConcat(_instuct));
                        continue;
                    }
                        
                    if (func)
                    {
                        bool flag = false;
                        for (int j = 0; j < _instuct.pattern.Count; j++)
                        {
                            if (_instuct.pattern[j].key == "m")
                            {
                                CommonNode type;
                                string v1 = _instuct.pattern[j].value.Trim();
                                v1 = v1.Remove(v1.Length - 1, 1);//.Remove(0, 1);
                                v1 = v1.Remove(0, 1);
                                int n = 0;
                                string[] strs = v1.Split('.');
                                if (!varLocal.ContainsKey(strs[0]) || strs.Length < 2) break;
                                if (Compiler.typesarg.ContainsValue(varLocal[strs[0]])) break;
                                string var = string.Empty;
                                for (int k = 1; k < strs.Length; k++)
                                {
                                    var += "." + strs[k];
                                }
                                
                                string resualtStr;
                                string st = strs[0];//(varLocal[strs[0]] == "DWORD") ? strs[0] : "ecx";
                                resualt.Append($"mov edx, [{strs[0]}]\n");
                                if (strs.Length < 2) resualtStr = $"[ecx]";//$"[{strs[0]}]";
                                else resualtStr = "[" + $"edx" + " + " + varLocal[strs[0]] + var + "]";
                                _instuct.pattern[j].value = resualtStr;
                                flag = true;
                            }
                        }
                        if (flag)
                        {
                            resualt.Append(InstructConcat(_instuct));
                            flag = false;
                            continue;
                        }
                        //if (flag) continue;
                    }

                    bool flagM = false;
                    for (int j = 0; j < _instuct.pattern.Count; j++)
                    {
                        if (_instuct.pattern[j].key == "m")
                        {
                            CommonNode type;
                            string v1 = _instuct.pattern[j].value.Trim();
                            v1 = v1.Remove(v1.Length - 1, 1);//.Remove(0, 1);
                            v1 = v1.Remove(0, 1);
                            int n = 0;
                            string[] strs = v1.Split('.');
                            if (varGlobal.ContainsKey(strs[0]) && varGlobal[strs[0]].First() != '*') break;
                            if (Compiler.types.ContainsValue(varGlobal[strs[0]])) break;
                            string var = string.Empty;
                            for (int k = 1; k < strs.Length; k++)
                            {
                                var += "." + strs[k];
                            }
                            //resualt.Append($"mov ecx, [{strs[0]}]");
                            string resualtStr;
                            if (strs.Length < 2)
                            {
                                resualtStr = $"[{strs[0]}]";
                                _instuct.pattern[j].value = resualtStr;
                                //_instuct.value = $"mov ecx, [{strs[0]}]\n" + _instuct.value;
                            }
                            else
                            {
                                resualtStr = "[" + $"edx" + " + " + varGlobal[strs[0]].Remove(0, 1) + var + "]";
                                _instuct.pattern[j].value = resualtStr;
                                _instuct.value = $"mov edx, [{strs[0]}]\n" + _instuct.value;
                            }
                            flagM = true;
                        }
                    }
                    if (flagM)
                    {
                        resualt.Append(InstructConcat(_instuct));
                        //flagM = false;
                        continue;
                    }
                    // |case1| -- global
                    //if (InstructPattern(str, str2) && _instuct.value == "mov")
                    //{
                    //resualt.Append(InstructConcat(_instuct)); i++;
                    //continue;
                    //}


                    // |case1| -- cmp
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "cmp|rn") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        continue;
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


                    
                    if (InstructPattern(str, "mov|rm"))
                    {
                        string pstr = pattern1[1].value;
                        pstr = pstr.Remove(pstr.Length - 1, 1);
                        pstr = pstr.Remove(0, 1); pstr = pstr.Trim();
                        if (!pstr.Contains(" ") &&
                            !pstr.Contains("+") &&
                            !pstr.Contains("-") &&
                            !pstr.Contains("*") &&
                            !pstr.Contains("/")
                            )
                        {
                            string[] strs = pstr.Split('.');
                            int n = 1;

                            string type = varGlobal[strs[0]];
                            while (n < strs.Length)
                            {
                                type = ProgramAst.structs[type][strs[n]].token.value;
                                //if (n + 1 == strs.Length) {  }
                                n++;
                            }
                            if (type == "int16" || type == "db" || type == "dw" || type == "int8") _instuct.value = "movzx";
                            //if (type == "int8") _instuct.value = "movzx";
                            //else if (type == "int16") _instuct.value = "movzx";

                            resualt.Append(InstructConcat(_instuct));
                            continue;
                        }
                    }

                    

                    if (InstructPattern(str, "mov|rm"))
                    {
                        int pos = 0;
                        int posDword = 0;
                        bool dword = false;
                        while (true)
                        {
                            if (_instuct.pattern[pos].value.Trim() == "dword") { posDword = pos; pos++; dword = true; }
                            if (_instuct.pattern[pos].key == "m") break;
                            else pos++;
                        }

                        if (!dword) { _instuct.pattern[pos].value = " dword " + _instuct.pattern[pos].value; _instuct.pattern[pos].key = "Q"; }
                        //resualt.Append($"xor {pattern1[0].value}, {pattern1[0].value}");
                        //resualt.Append(InstructConcat(_instuct));
                        //continue;
                    }

                    // inline macro
                    //if (InstructPattern(str, "?|~"))
                    //{
                        //resualt.Append(InstructConcat(_instuct));
                        //continue;
                    //}

                }
                catch { }
                resualt.Append(InstructConcat(_instuct));
            }

            return resualt;
        }
        public static instruct CopyArgInstruct (instruct left, instruct right, string patt, int index = 0)
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
        public static bool InstructCmp (instruct left, instruct right)
        {
            for (int i = 0; i < left.pattern.Count; i++)
            {
                if (left.pattern[i].key != right.pattern[i].key) return false;
            }
            return true;
        }
        public static string InstructConcat (instruct instruct)
        {
            string resualt = instruct.value + " ";
            int i = 0;
            foreach (patternNode node in instruct.pattern)
            {
                if (node.key == "t") continue;
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

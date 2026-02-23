using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            { "r", "(rax|rdx|rbx|rcx|rsi|rdi|rsp|rbp|eax|edx|ebx|ecx|esi|edi|esp|ebp|al|dx|bx|cx|si|di|sp|bp)"},
            { "c", "[A-Z_A-Z_]+[0-9]*"},
            { "i", "\\.?[a-z\\\\.A-Z_][a-z\\\\.A-Z\\\\.0-9_]*\\:" },
            { "m", "\\[[^\\[\\]]+\\]" },
            { "v", ".?[a-z\\.A-Z_][a-z\\.A-Z\\.0-9_]*" },
            { "n", "-?[0-9]+" },
            { "o", "(/|\\*|\\-|\\+)"},
            { "s", "'[^'']*'" },
            { "ts", @"(.|,)"},
            { "fg", "(\\{|\\})"},
            { "l", "(\n|\t)"}
            //{ "cm", @";^\[\]"}
        };

        //public static  pattern1;// = {{  }, {" mov","m","eax" }};
        //public static List<> patterns;
        static PostGen ()
        {

        }

        public static objProgram PostTranslation(objProgram _objProgram)
        {
            objProgram objProgramResualt = new objProgram();
            objProgramResualt.stringsConsts = _objProgram.stringsConsts;
            objProgramResualt.macroData = _objProgram.macroData;
            objProgramResualt.codeData = _objProgram.codeData;
            objProgramResualt.includes = _objProgram.includes;
            objProgramResualt.data = _objProgram.data;

            StringData procData = new StringData();
            List<instruct> procList = LexInstructs(_objProgram.procData);
            objProgramResualt.procData = Translation(procList);

            objProgramResualt.codeData = Translation(LexInstructs(_objProgram.codeData));



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
                int pos = instruct.Length+1;
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

        public static StringData Translation (List<instruct> instructs)
        {
            StringData resualt = new StringData();

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

                    // |case1| -- global
                    //if (InstructPattern(str, str2) && _instuct.value == "mov")
                    //{
                        //resualt.Append(InstructConcat(_instuct)); i++;
                        //continue;
                    //}

                    // |case1| -- cmp
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        continue;
                    }
                    // |case2| - cmp
                    if (InstructPattern(str, "mov|rn") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "n"))); i++;
                        continue;
                    }
                    // |case3| - cmp - not realistic
                    if (InstructPattern(str, "mov|rr") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "r", 1))); i++;
                        continue;
                    }
                    // |case4| - cmp
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "cmp|r?") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "c"))); i++;
                        continue;
                    }
                    // |case5| - call
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        continue;
                    }
                    // |case6| - call
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "c"))); i++;
                        continue;
                    }
                    // |case7| - call
                    if (InstructPattern(str, "mov|rn") && InstructPattern(str2, "push|r") && InstructCmpReg(pattern1, pattern2))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instructSecond, _instuct, "m"))); i++;
                        continue;
                    }
                    // |case8| - void
                    if (InstructPattern(str, "mov|rm") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "m"))); i++;
                        continue;
                    }
                    // |case9| - void
                    if (InstructPattern(str, "mov|rc") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "c"))); i++;
                        continue;
                    }
                    // |case10| - void
                    if (InstructPattern(str, "pop|r") && InstructPattern(str2, "mov|mr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        //resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "m"))); i++;
                        //continue;
                    }
                    // |case11| - void
                    if (InstructPattern(str, "mov|rr") && InstructPattern(str2, "mov|rr") && InstructCmpReg(pattern1, pattern2, 1))
                    {
                        resualt.Append(InstructConcat(CopyArgInstruct(_instuct, _instructSecond, "r"))); i++;
                        continue;
                    }

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

            left.pattern[0] = right.pattern[pos];
            return left;
        }
        public static bool InstructPattern (string str, string patt)
        {
            string istr = str.Split('|')[0];
            string ipatt = patt.Split('|')[0];
            if (ipatt != istr) return false;

            char[] cstr = str.Split('|')[1].ToCharArray();
            char[] cpatt = patt.Split('|')[1].ToCharArray();

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

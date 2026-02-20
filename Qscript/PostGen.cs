using System;
using System.Collections.Generic;
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
    class instruct {
        public string intruct;
        public arg[] arg;
    }
    class patternNode {
        public string key;
        public string value;

        public patternNode(string _key, string _value) { key = _key; value = _value; }
    }
    class patterns
    {
        public List<patternNode> pattern = new List<patternNode>();
    }


    public static class PostGen
    {
        public static Dictionary<string, string> match = new Dictionary<string, string>()
        {
            { "r", "(rax|rdx|rbx|rcx|rsi|rdi|rsp|rbp|eax|edx|ebx|ecx|esi|edi|esp|ebp|al|dx|bx|cx|si|di|sp|bp)"},
            { "v", @"[a-z\\.A-Z_][a-z\\.A-Z\\.0-9_]*" },
            { "m", "\\[[^]\\]" },
            { "n", "-?[0-9]+" },
            { "c", "[A-Z_A-Z]*"},
            { "o", "(/|*|\\-|\\+)"},
            { "t", " "},
            { "s", "'[^'']*'" }
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
            List<patterns> procList = new List<patterns>();
            List<string> commnadList = new List<string>();
            string str;
            string instruct;
            string[] args;
            bool func = false;
            for (int i = 0; i < _objProgram.procData.Length; i++)
            {
                str = _objProgram.procData.Data[i];
                instruct = str.Split(' ')[0];

                if (instruct == "proc") func = true;
                else if (instruct == "endp")
                { 
                    func = false;
                    for (int j = 0; j < procList.Count; j++)
                    {
                        // mov|m
                        if (procList[j].pattern)
                        {
                            //commnadList[j].Replace("mov ", "");
                            //int ps = 0; int z = 0;
                            //while (true) { if (commnadList[j][ps] == ']' && z == 0) break;
                                //else if (commnadList[j][ps] == '[') { z++; ps++; }
                                //else if (commnadList[j][ps] == ']' && z != 0) { z--; ps++; } else ps++; }
                            //string ins = commnadList[j].Remove(ps);

                        }
                }
                else if (func == true)
                {
                    int pos = 0;
                    //string done = instruct + "|";
                    patterns patt = new patterns();
                    while (true)
                    {
                        if (pos >= str.Length) break;
                        foreach (var key in match)
                        {
                            Match regx = Regex.Match(str.Substring(pos), "^" + key.Value);
                            if (regx.Success && !string.IsNullOrEmpty(regx.Value))
                            {
                                patt.pattern.Add(new patternNode(key.Key, regx.Value));
                                //done += key.Key;
                                pos += regx.Value.Length;
                            }
                        }
                    }
                    procList.Add(patt);
                    commnadList.Add(str);
                }
                
            }
            objProgramResualt.procData = procData;



            return _objProgram;
        }
    }
}

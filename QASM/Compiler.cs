using QASM.Structs;
using QASM.Utils;
using System.Text.RegularExpressions;

namespace QASM
{
    public static class Compiler
    {
        private static Dictionary<string, string> match = new Dictionary<string, string>()
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

        private static Dictionary<string, Hex> opcodes = new Dictionary<string, Hex>()
        {
            { "mov|rr", new Hex("89")},
            { "mov|rn", new Hex("B8")},
            { "mov|rm", new Hex("8B")},
            { "mov|mr", new Hex("89")},
            { "mov|mn", new Hex("C7")}
            //mov|rr = 89
            //mov|rn = B8
            //mov|rm = 8B
            //mov|mr = 89
            //mov|mn = C7
        };

        public static Dictionary<string, string> regcodes = new Dictionary<string, string>()
        {
            //eax:000,
            //ecx:001,
            //edx:010,
            //ebx:011,
            //esp:100,
            //ebp:101,
            //esi:110,
            //edi:111,
            //addres:101
            { "eax", "000" },
            { "ecx", "001" },
            { "edx", "010" },
            { "ebx", "011" },
            { "esp", "100" },
            { "ebp", "101" },
            { "esi", "110" },
            { "edi", "111" },
            { "addr", "101" }
        };

        public static objProgram Translation(objProgram _objProgram)
        {
            objProgram objProgramResualt = new objProgram();
            //objProgramResualt.stringsConsts = _objProgram.stringsConsts;
            //objProgramResualt.macroData = _objProgram.macroData;
            //objProgramResualt.codeData = _objProgram.codeData;
            //objProgramResualt.includes = _objProgram.includes;
            //objProgramResualt.data = _objProgram.data;

            // proc
            List<instruct> procList = LexInstructs(_objProgram.procData);
            List<instruct> codeList = LexInstructs(_objProgram.codeData);
            List<instruct> dataList = LexInstructs(_objProgram.data);

            Dictionary<string, Hex> varOffsets = TranslationFirst(codeList);
            Console.WriteLine(" ---===|HEX|===--- ");
            foreach (var offset in varOffsets)
            {
                Console.WriteLine("|"+offset.Key + " : " + offset.Value.value);
            }
            //Console.WriteLine(); Console.WriteLine("Hex : " + (new Hex("0x400000") + new Hex("0x000066")).value);
            
            //procList = TranslationSecond(procList, varOffsets);
            //codeList = TranslationSecond(codeList, varOffsets);

            objProgramResualt.procData = Translation(procList);
            // code
            objProgramResualt.codeData = Translation(LexInstructs(_objProgram.codeData));



            return objProgramResualt;
        }

        public static List<instruct> LexInstructs(StringData data)
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
                int pos = instruct.Length + 1;
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
                }
                resualtList.Add(patt);
            }
            return resualtList;
        }

        public static Dictionary<string, Hex> TranslationFirst(List<instruct> instructs)
        {
            Dictionary<string, Hex> resualt = new Dictionary<string, Hex>();
            Hex offset = new Hex("400000");

            for (int i = 0; i < instructs.Count; i++)
            {
                instruct _instuct = instructs[i];
                string resualtStr = string.Empty;
                List<patternNode> pattern = new List<patternNode>();

                string str = _instuct.value + "|";
                foreach (var key in _instuct.pattern) { if (key.key == "ts" || key.key == "t") continue; str += key.key; pattern.Add(key); }


                if (InstructPattern(str, "?|vn"))
                {
                    Hex size = GetSizeOffset(pattern[0].value);
                    resualt.Add(_instuct.value, size + offset);
                    offset += size;
                    continue;
                }
            }

            return resualt;
        }

        public static StringData Translation(List<instruct> instructs)
        {
            StringData resualt = new StringData();

            for (int i = 0; i < instructs.Count; i++)
            {
                instruct _instuct = instructs[i];
                string resualtStr = string.Empty;
                List<patternNode> pattern = new List<patternNode>();

                string str = _instuct.value + "|";
                foreach (var key in _instuct.pattern) { if (key.key == "ts" || key.key == "t") continue; str += key.key; pattern.Add(key); }


                /*foreach (var opcode in opcodes)
                {
                    if (InstructPattern(str, opcode.Key))
                    {
                        resualtStr += opcode.Value + " ";
                        string binmod = "11";
                        patternNode reg; string regStr = string.Empty;
                        patternNode mem; string memStr = string.Empty;
                        if (pattern[0].key == "m") { binmod = "00"; memStr = "101"; }
                        else if (pattern[1].key == "m") { binmod = "00"; memStr = "101"; } 

                        if (pattern[0].key == "r" && pattern[1].key == "m") regStr = regcodes[pattern[0].value];
                        else if (pattern[0].key == "m" && pattern[1].key == "r") regStr = regcodes[pattern[0].value];
                        else if (pattern[0].key == "r" && pattern[1].key == "r")
                        {
                            regStr = regcodes[pattern[0].value];
                            memStr = regcodes[pattern[1].value];
                        }
                        binmod += regStr + memStr;
                        Console.WriteLine(binmod);
                    }
                }*/
                //mov|rr = 89
                //mov|rn = B8
                //mov|rm = 8B
                //mov|mr = 89
                //mov|mn = C7
                if (InstructPattern(str, "mov|rm"))
                {
                    Hex hex = opcodes["mov|rn"];
                    hex = hex + Hex.ConvtWithBinCode(regcodes[pattern[0].value]);
                    resualtStr = hex.value;
                    resualtStr += " " + Hex.ConvrtForx86(Hex.Convt(Convert.ToInt32(pattern[1].value)));
                }
                if (InstructPattern(str, "mov|rr"))
                {
                    resualtStr = opcodes["mov|rr"].value;
                    string modRM = "11"; modRM += regcodes[pattern[1].value];
                    modRM += regcodes[pattern[0].value];
                    //Console.WriteLine(modRM);
                    //Console.WriteLine(Hex.ConvtWithBinCode(modRM).value);
                    resualtStr += " " + Hex.ConvtWithBinCode(modRM).value;
                }
                if (InstructPattern(str, "mov|rn"))
                {
                    Hex hex = opcodes["mov|rn"];
                    hex = hex + Hex.ConvtWithBinCode(regcodes[pattern[0].value]);
                    resualtStr = hex.value;
                    resualtStr += " " + Hex.ConvrtForx86(Hex.Convt(Convert.ToInt32(pattern[1].value)));
                }


                Console.WriteLine(resualtStr); continue;
                throw new Exception("Невозможно это странслировать в машинный код");
            }
            return resualt;
        }
        public static bool InstructPattern(string str, string patt)
        {
            string istr = str.Split('|')[0];
            string ipatt = patt.Split('|')[0];
            if (ipatt == "?") ipatt = "?";
            else if (ipatt != istr) return false;

            char[] cstr = str.Split('|')[1].ToCharArray();
            char[] cpatt = patt.Split('|')[1].ToCharArray();

            if (cpatt[0] == '~') return true;
            for (int i = 0; i < cstr.Length; i++)
            {
                if (cpatt[i] == '?') continue;
                else if (cpatt[i] == cstr[i]) { continue; }
                else return false;
            }
            return true;
        }

        public static string InstructConcat(instruct instruct)
        {
            string resualt = instruct.value + " ";
            int i = 0;
            foreach (patternNode node in instruct.pattern)
            {
                if (node.key == "t") continue;
                resualt += node.value; //+ " ";
                i++;
            }
            if (resualt.Length > 0) resualt.Remove(resualt.Length - 1);
            if (resualt.Replace(" ", "").Length == 0) resualt = "";
            if (resualt.Length > 0) resualt += "\n";
            return resualt;
        }

        public static Hex GetSizeOffset(string name)
        {
            int sizeConst = 0;

            switch (name)
            {
                case "db": sizeConst = 1; break;
                case "dw": sizeConst = 2; break;
                case "dd": sizeConst = 4; break;
            }

            Hex hex = new Hex(Hex.Convt(sizeConst));

            return hex;
        }

    }
}

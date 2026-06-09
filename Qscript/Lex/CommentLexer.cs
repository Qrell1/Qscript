using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Qscript.Lex
{
    public class CommentLexer
    {
        public CommentLexer() { }
        public string lexCodes(string[] codes, string file)
        {
            StringBuilder code = new StringBuilder();
            string str = string.Empty;
            bool asm = false;
            string strasm = string.Empty;
            //char[] sep = "//".ToCharArray();
            bool multiComment = false;
            Syntax.code.strings.Add(file, new List<string>());
            Syntax.code.stringsSize.Add(file, new List<int>());
            for (int i = 0; i < codes.Length; i++)
            {
                if (!multiComment && codes[i].IndexOf("/*") >= 0)
                {
                    str = codes[i].Substring(0, codes[i].IndexOf("/*"));
                    code.Append(str);
                    Syntax.code.strings[file].Add(str);
                    Syntax.code.stringsSize[file].Add(str.Length);
                    multiComment = true;
                    continue;
                }
                if (multiComment && codes[i].IndexOf("*/") >= 0)
                {
                    int len = codes[i].IndexOf("*/");
                    str = codes[i].Substring(len + 2, codes[i].Length - len - 2);
                    code.Append(str);
                    Syntax.code.strings[file].Add(str);
                    Syntax.code.stringsSize[file].Add(str.Length);
                    multiComment = false;
                    continue;
                }
                if (multiComment) 
                {
                    Syntax.code.strings[file].Add(string.Empty);
                    Syntax.code.stringsSize[file].Add(0);
                    continue;
                }

                int index = codes[i].IndexOf("//");
                if (index >= 0)
                {
                    str = codes[i].Substring(0, index);
                    code.Append(str);
                    Syntax.code.strings[file].Add(str);
                    Syntax.code.stringsSize[file].Add(str.Length);
                } else
                {
                    code.Append(codes[i]);
                    Syntax.code.strings[file].Add(codes[i]);
                    Syntax.code.stringsSize[file].Add(codes[i].Length);
                }
            }
            return code.ToString();
        }
    }
}

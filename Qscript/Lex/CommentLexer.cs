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
        public (string, CodeStruct) lexCodes(string[] codes)
        {
            StringBuilder code = new StringBuilder();
            CodeStruct codeData = new CodeStruct();
            string str = string.Empty;
            bool asm = false;
            string strasm = string.Empty;
            //char[] sep = "//".ToCharArray();
            bool multiComment = false;
            for (int i = 0; i < codes.Length; i++)
            {
                if (!multiComment && codes[i].IndexOf("/*") >= 0)
                {
                    str = codes[i].Substring(0, codes[i].IndexOf("/*"));
                    code.Append(str);
                    codeData.strings.Add(str);
                    codeData.stringsSize.Add(str.Length);
                    multiComment = true;
                    continue;
                }
                if (multiComment && codes[i].IndexOf("*/") >= 0)
                {
                    str = codes[i].Substring(codes[i].IndexOf("*/"), codes[i].Length-2);
                    code.Append(str);
                    codeData.strings.Add(str);
                    codeData.stringsSize.Add(str.Length);
                    multiComment = false;
                    continue;
                }
                if (multiComment) continue;

                int index = codes[i].IndexOf("//");
                if (index >= 0)
                {
                    str = codes[i].Substring(0, index);
                    code.Append(str);
                    codeData.strings.Add(str);
                    codeData.stringsSize.Add(str.Length);
                } else
                {
                    code.Append(codes[i]);
                    codeData.strings.Add(codes[i]);
                    codeData.stringsSize.Add(codes[i].Length);
                }
            }
            return (code.ToString(), codeData);
        }
    }
}

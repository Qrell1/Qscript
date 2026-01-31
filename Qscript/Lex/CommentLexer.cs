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
        public string lexCodes(string[] codes)
        {
            StringBuilder code = new StringBuilder();
            string str = string.Empty;
            bool asm = false;
            string strasm = string.Empty;
            //char[] sep = "//".ToCharArray();
            for (int i = 0; i < codes.Length; i++)
            {
                int index = codes[i].IndexOf("//");
                if (index >= 0)
                {
                    str = codes[i].Substring(0, index);
                    code.Append(str);
                } else
                {
                    code.Append(codes[i]);
                }
            }
            return code.ToString();
        }
    }
}

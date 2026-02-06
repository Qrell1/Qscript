using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript.Lex
{
    public struct Include
    {
        public string fileName;
    }
    public class IncludeLexer
    {
        public IncludeLexer() { }
        public List<Token> lexIncludes(List<Token> code)
        {
            List<Token> tokens = new List<Token>();
            bool flag = false;
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].type.type == "ENDINCLUDE")
                {
                    flag = true;
                    break;
                }
                tokens.Add(code[i]);
            }
            if (!flag)
                return new List<Token> { };
            List<string> includes = new List<string>();
            for (int j = 0; j < tokens.Count; j++)
            {
                if (tokens[j].type.type == "INCLUDE" && tokens[j+1].type.type == "STRING")
                {
                    includes.Add(tokens[j+1].value);
                }
            }
            tokens = fileCodeIncludes(includes.ToArray(), 0);
            //tokens = destroyIncludes(tokens);
            //code = destroyIncludes(code);
            return tokens;
        }
        public List<Token> fileCodeIncludes(string[] includes, int index)
        {
            List<Token> list = new List<Token>();
            string[] codes = File.ReadAllLines(includes[index]);

            CommentLexer commentLexer = new CommentLexer();
            commentLexer.lexCodes(codes);
            Lexer lexer = new Lexer();
            List<Token> fileTokens = lexer.lexAnalysis();
            List<Token> ts = lexIncludes(fileTokens);
            fileTokens = destroyIncludes(fileTokens);
            fileTokens.AddRange(ts);
            list = fileTokens;
            


            index++;
            if (index < includes.Length)
            {
                List<Token> tokens = fileCodeIncludes(includes, index);
                tokens = destroyIncludes(tokens);
                list.AddRange(tokens);
                return list;
            }
            return list;
        }

        public List<Token> destroyIncludes(List<Token> code)
        {
            int l = 0;
            //List<Token> tokens = new List<Token>();
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].type.type == "ENDINCLUDE")
                {
                    return code.GetRange(i + 2, code.Count - i - 2);
                }
            }
            return code;
        }
    }
}

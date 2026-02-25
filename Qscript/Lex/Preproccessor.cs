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
    public class Preproccessor
    {
        public List<string> fileIncludes = new List<string>();

        public Preproccessor() { }
        public List<Token> lexIncludes(List<Token> code)
        {
            List<Token> nonIncludeCode = new List<Token>();
            List<string> includes = new List<string>();

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].type.type == "INCLUDE")
                {
                    i++; if (code[i].type.type == "STRING") { includes.Add(code[i].value); i++; }
                    if (code[i].type.type != "SEM") Syntax.SyntaxError("Неправильное подключение файла", code[i]); 
                    continue;
                }
                else nonIncludeCode.Add(code[i]);
            }
            if (includes.Count == 0) return nonIncludeCode;

            List<Token> tokens = fileCodeIncludes(includes.ToArray());
            tokens.AddRange(nonIncludeCode);
            return tokens;
        }
        public List<Token> fileCodeIncludes(string[] includes)
        {
            List<Token> list = new List<Token>();
            foreach (string include in includes)
            {
                if (fileIncludes.Contains(include)) return list;
                Console.WriteLine($"Загружаем Файл : {include}");
                fileIncludes.Add(include);

                string[] codes = File.ReadAllLines(include);

                CommentLexer commentLexer = new CommentLexer();
                (string temp, CodeStruct codeStruct) = commentLexer.lexCodes(codes);
                Lexer lexer = new Lexer(codeStruct);

                List<Token> fileTokens = lexer.lexAnalysis();
                //Preproccessor includeLexer = new Preproccessor();
         
                List<Token> ts = lexIncludes(fileTokens);
                ts.AddRange(list);
                list = ts;
            }
            return list;
        }
    }
}

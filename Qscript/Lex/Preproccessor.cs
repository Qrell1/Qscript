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
        Dictionary<string, Token> defines = new Dictionary<string, Token>();

        public Preproccessor() { }
        public List<Token> lexIncludes(List<Token> code)
        {
            code = lexDefine(code);
            code = lexTypedef(code);
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
        public List<Token> lexDefine(List<Token> code)
        {
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].value == "define")
                {
                    i += 1; if (code[i].type.type != "VAR") Syntax.SyntaxError($"Неверный Токен: {code[i].value}", code[i]);
                    string replace = code[i].value; i++;
                    Token value = code[i];
                    if (!defines.ContainsKey(replace)) defines.Add(replace, value);
                    continue;
                }
            }

            List<Token> tokens = new List<Token>();
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].value == "define") { i += 2; continue; }
                else if (defines.ContainsKey(code[i].value)) { tokens.Add(defines[code[i].value]); }
                else { tokens.Add(code[i]); }
            }

            return tokens;
        }

        public List<Token> lexTypedef (List<Token> code)
        {
            List<Token> tokens = new List<Token>();
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].value == "typedef")
                {
                    i += 1; if (code[i].type.type != "VAR") Syntax.SyntaxError($"Неверный Токен: {code[i].value}", code[i]);
                    string name = code[i].value; i++; if (code[i].type.type != "VAR") Syntax.SyntaxError($"Неверный Токен: {code[i].value}", code[i]);
                    string value = code[i].value;

                    string t_t = Compiler.types[value];
                    string a_t = Compiler.typesarg[value];
                    int    l_t = Compiler.aligns[value];
                    string r_t = Compiler.typesregs[value];

                    if (!Compiler.types.ContainsKey(name)) Compiler.types.Add(name, t_t);
                    if (!Compiler.typesarg.ContainsKey(name)) Compiler.typesarg.Add(name, a_t);
                    if (!Compiler.aligns.ContainsKey(name)) Compiler.aligns.Add(name, l_t);
                    if (!Compiler.typesregs.ContainsKey(name)) Compiler.typesregs.Add(name, r_t);

                    continue;
                } else {  tokens.Add(code[i]); }
            }
            return tokens;
        }

        public List<Token> fileCodeIncludes(string[] includes)
        {
            List<Token> list = new List<Token>();
            foreach (string include in includes)
            {
                if (fileIncludes.Contains(include)) return lexTypedef(lexDefine(list));
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
                list = lexDefine(ts);
                list = lexTypedef(list);
            }
            list = lexDefine(list);
            list = lexTypedef(list);
            return list;
        }
    }
}

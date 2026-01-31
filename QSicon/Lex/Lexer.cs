using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Qscript.Lex
{
    public class Lexer
    {
        public string code;
        public int pos = 0;
        public int len = 0;

        public Lexer()
        {}

        public void lexAnalysis(string[] codes)
        {
            for (int i = 0; i < codes.Length; i++)
            {
                len = codes[i].Length;
                code = codes[i];
                pos = 0;
                while (nextToken())
                {

                }
                Console.WriteLine();
            }
        }

        private bool nextToken()
        {
            if (pos >= code.Length)
            {
                return false;
            }

            
            /*if (skipWhitespace())
            {
                return true;
            }*/
            /*if (skipComments())
            {
                return true;
            }*/

            // Обрабатываем остальные токены
            foreach (TokenType tokenType in TokenTypeList.tokenTypes.Values)
            {
                if (tokenType.type == "SPACE")
                {
                    //Console.Write("");
                    continue;
                }
                if (tokenType.type == "TAB")
                {
                    //Console.Write(" ");
                    continue;
                }

                Match regx = Regex.Match(code.Substring(pos), "^" + tokenType.regx);
                if (regx.Success && !string.IsNullOrEmpty(regx.Value))
                {
                    //Console.WriteLine($"[LEXER] Найден токен: {tokenType.type} значение: {regx.Value}");

                    string str = string.Empty;
                    str = regx.Value;
                    if (tokenType.type == "STRING")
                    {
                        string value = regx.Value;
                        value = value.Substring(1, value.Length - 2);
                        value = value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                        str = value;
                    }
                    /*else if (tokenType.type == "ASM")
                    {
                        (string value, int length) = lexAsmInsert(pos, code);
                        pos += length;
                        str = value;
                    }*/



                    //"LPAR", new TokenType("LPAR", "\\("));
                    //"RPAR", new TokenType("RPAR", "\\)"));
                    //"PARS", new TokenType("PARS", "\\'"));

                    //"LFIG", new TokenType("LFIG", "\\{"));
                    //"RFIG", new TokenType("RFIG", "\\}"));

                    //"LK", new TokenType("LK", "\\["));
                    //"RK", new TokenType("RK", "\\]"));

                    //"LKN", new TokenType("LKN", "\\<"));
                    //"RKN", new TokenType("RKN", "\\>"));

                    //"SPACE", new TokenType("SPACE", " "));
                    //"TAB", new TokenType("TAB", "\t"));

                    ConsoleColor color;
                    if (new string[] { "ELSEIF", "IF", "ELSE" }.Contains(tokenType.type))
                        color = ConsoleColor.Green;
                    else if (new string[] { "OPER", "ENDINCLUDE", "ASMINCLUDE", "INCLUDE", "USING", "ASM" }.Contains(tokenType.type))
                        color = ConsoleColor.Yellow;
                    else if (new string[] { "RETURN", "BREAK", "CONTINUE", "BOOL", "INLINE", "MODIFIER", "FOR", "WHILE", "STRUCT", "CLASS" }.Contains(tokenType.type))
                        color = ConsoleColor.Red;
                    else if (new string[] { "CONST", "VAR" }.Contains(tokenType.type))
                        color = ConsoleColor.DarkYellow;
                    else if ("NUMBER" == tokenType.type)
                        color = ConsoleColor.Cyan;
                    else if ("STRING" == tokenType.type)
                        color = ConsoleColor.DarkYellow;
                    else if (new string[] { "LPAR", "RPAR", "PARS", "LFIG", "RFIG", "LK", "RK", "LKN", "RKN" }.Contains(tokenType.type))
                        color = ConsoleColor.DarkGreen;
                    else if ("NUMBERSTRING" == tokenType.type)
                    {
                        str = str.Replace("|", "");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(str);
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.Write("|");
                        Console.ResetColor();
                        pos += regx.Value.Length;
                        return true;
                    }
                    else
                        color = ConsoleColor.White;
                    Console.ForegroundColor = color;
                    if (tokenType.type == "TAB")
                        Console.Write("");
                    else if (tokenType.type == "SPACE")
                        Console.Write("");
                    else
                        Console.Write(str);
                    Console.ResetColor();

                    pos += regx.Value.Length;
                    return true;
                }
            }

            return false;
            //throw new Exception($"На позиции {pos} синтаксическая ошибка. Символ: '{code[pos]}'");
        }

        private bool skipWhitespace()
        {
            Match whitespace = Regex.Match(code.Substring(pos), @"^\s+");
            if (whitespace.Success)
            {
                pos += whitespace.Value.Length;
                return true;
            }
            return false;
        }

        private (string, int) lexAsmInsert(int startPos, string _code)
        {
            int endPos = _code.IndexOf("asm", startPos + 3); // |>
            if (endPos == -1)
            {
                //throw new Exception($"На позиции {startPos} незавершенная asm вставка!");
            }

            string asmCode = _code.Substring(startPos + 3, endPos - startPos - 3);
            int length = endPos - startPos + 3; // 2

            return (asmCode, length);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Qscript.Lex
{
    public class Lexer
    {
        public string code;
        public int pos = 0;
        public List<Token> tokenList = new List<Token>();

        private int stringIndex = 0;
        private int tokenValue;

        public Lexer(string _code)
        {
            code = _code;
        }

        public List<Token> lexAnalysis()
        {
            while (nextToken())
            {
                Error.codeLenght = Error.codeLenght;

                
                if (Error.codeLenght.Count > stringIndex && tokenValue >= Error.codeLenght[stringIndex])
                {
                    stringIndex++;
                    passEmpetyString:
                    if (Error.codeLenght.Count > stringIndex && Error.codeLenght[stringIndex] == 0)
                    {
                        stringIndex++;
                        goto passEmpetyString;
                    }
                    //tokenValue = 0;
                }
            }

            // Фильтруем пробелы и комментарии
            List<Token> filteredTokens = new List<Token>();
            int index = 0;
            foreach (Token token in tokenList)
            {
                if (token.type.type != "SPACE" &&
                    token.type.type != "TAB" &&
                    token.type.type != "COMMENT")
                {
                    filteredTokens.Add(token);

                    // Отладочный вывод
                    string spaces = "";
                    for (int i = 0; i < 20 - token.type.type.Length; i++)
                    {
                        spaces += " ";
                    }
                    Console.WriteLine($"[LEXER] Index:{index} Token pos:{token.pos} type:{token.type.type}{spaces}value:{token.value}");
                    index++;
                }
            }

            return filteredTokens;
        }

        private bool nextToken()
        {
            if (pos >= code.Length)
            {
                return false;
            }

            if (skipWhitespace())
            {
                return true;
            }
            /*if (skipComments())
            {
                return true;
            }*/

            // Обрабатываем остальные токены
            foreach (TokenType tokenType in TokenTypeList.tokenTypes.Values)
            {
                if (tokenType.type == "SPACE" ||
                    tokenType.type == "TAB")
                {
                    continue;
                }

                Match regx = Regex.Match(code.Substring(pos), "^" + tokenType.regx);
                if (regx.Success && !string.IsNullOrEmpty(regx.Value))
                {
                    //tokenValue = regx.Value.Length;
                    //Console.WriteLine($"[LEXER] Найден токен: {tokenType.type} значение: {regx.Value}");
                    int length = regx.Value.Length;
                    Token token;
                    if (tokenType.type == "STRING")
                    {
                        string value = regx.Value;
                        value = value.Substring(1, value.Length - 2);
                        value = value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                        token = new Token(tokenType, value, stringIndex);
                    }
                    else if (tokenType.type == "ASM")
                    {
                        (string value, int len) = lexAsmInsert(pos, code);
                        token = new Token(tokenType, value, stringIndex);
                        length = len;
                        tokenList.Add(token);
                        return true;
                    }
                    else
                    {
                        token = new Token(tokenType, regx.Value, stringIndex);
                        length = regx.Value.Length;
                    }

                    pos += length;
                    tokenValue += length;
                    tokenList.Add(token);
                    return true;
                }
            }

            throw new Exception($"На позиции {pos} синтаксическая ошибка. Символ: '{code[pos]}'");
        }

        private bool skipWhitespace()
        {
            Match whitespace = Regex.Match(code.Substring(pos), @"^\s+");
            if (whitespace.Success)
            {
                tokenValue += whitespace.Value.Length;
                pos += whitespace.Value.Length;
                return true;
            }
            return false;
        }

        /*private bool skipComments()
        {
            // Однострочные комментарии
            Match singleLineComment = Regex.Match(code.Substring(pos), @"^//[^\n]*");
            if (singleLineComment.Success)
            {
                pos += singleLineComment.Value.Length;
                return true;
            }

            // Многострочные комментарии
            Match multiLineComment = Regex.Match(code.Substring(pos), @"/\*.*?\*"<-/, RegexOptions.Singleline);
            if (multiLineComment.Success)
            {
                pos += multiLineComment.Value.Length;
                return true;
            }

            return false;
        }*/

        private (string, int) lexAsmInsert(int startPos, string _code)
        {
            int endPos = _code.IndexOf("asm", startPos + 3); // |>
            if (endPos == -1)
            {
                throw new Exception($"На позиции {startPos} незавершенная asm вставка!");
            }

            string asmCode = _code.Substring(startPos + 3, endPos - startPos - 3);
            int length = endPos - startPos + 3; // 2
            //tokenValue += length;

            return (asmCode, length);
        }
    }
}

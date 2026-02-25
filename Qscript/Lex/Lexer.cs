using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Qscript.Lex
{
    public class Lexer
    {
        public CodeStruct code;
        public int pos = 0;
        public List<Token> tokenList = new List<Token>();

        private int stringIndex = 0;
        //private int tokenValue;
        //private int len;

        public Lexer()
        {
            code = Syntax.code;
        }
        public Lexer(CodeStruct codeStruct)
        {
            code = codeStruct;
        }

        public List<Token> lexAnalysis()
        {
            while (nextToken())
            {
                /*Error.codeLenght = Error.codeLenght;

                
                if (Error.codeLenght.Count > stringIndex && tokenValue >= Error.codeLenght[stringIndex])
                {
                    stringIndex++;
                    passEmpetyString:
                    if (Error.codeLenght.Count > stringIndex && Error.codeLenght[stringIndex] == 0)
                    {
                        stringIndex++;
                        goto passEmpetyString;
                    }
                }*/
            }

            List<Token> tokens = new List<Token>();
            for (int i = 0; i < tokenList.Count; i++)
            {
                if (tokenList[i].type.type != "SPACE" &&
                    tokenList[i].type.type != "TAB" &&
                    tokenList[i].type.type != "COMMENT")
                {
                    tokens.Add(tokenList[i]);

                    // Отладочный вывод
                    string spaces = "";
                    for (int j = 0; j < 20 - tokenList[i].type.type.Length; j++)
                    {
                        spaces += " ";
                    }
                    Console.WriteLine($"[LEXER] Index:{i} Token pos:{tokenList[i].pos} type:{tokenList[i].type.type}{spaces}value:{tokenList[i].value}");
                }
            }

            return tokens;
        }

        private bool nextToken()
        {
            /*if (pos >= code.totalSize)
            {
                return false;
            }*/
            if (code.stringsSize.Count <= stringIndex)
                return false;
            if (pos >= code.stringsSize[stringIndex])
            {
                if (code.stringsSize.Count-1 == stringIndex)
                    return false;
                pos = 0;
                stringIndex++;
                return true;
            }
            if (skipSpace())
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
                /*if (tokenType.type == "SPACE" ||
                    tokenType.type == "TAB")
                {
                    continue;
                }*/

                Match regx = Regex.Match(code.strings[stringIndex].Substring(pos), "^" + tokenType.regx);
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
                        (string value, int len) = lexAsmInsert(pos, code.strings[stringIndex]);
                        token = new Token(tokenType, value, stringIndex);
                        //length = len;
                        length = 3;
                        tokenList.Add(token);
                        return true;
                    }
                    else
                    {
                        token = new Token(tokenType, regx.Value, stringIndex);
                        length = regx.Value.Length;
                    }

                    pos += length;
                    //tokenValue += length;
                    tokenList.Add(token);
                    return true;
                }
            }

            throw new Exception($"На позиции {pos} синтаксическая ошибка. Символ: '{code.strings[stringIndex]}'");
        }

        private bool skipSpace()
        {
            Match whitespace = Regex.Match(code.strings[stringIndex].Substring(pos), @"^\s+");
            if (whitespace.Success)
            {
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
            /*int endPos = _code.IndexOf("asm", startPos + 3); // |>
            if (endPos == -1)
            {
                throw new Exception($"На позиции {startPos} незавершенная asm вставка!");
            }

            string asmCode = _code.Substring(startPos + 3, endPos - startPos - 3);
            int length = endPos - startPos + 3; // 2
            pos += length;*/
            startPos += 3;
            string str = code.strings[stringIndex].Substring(startPos);
            stringIndex++;
            while (true)
            {
                if (code.strings[stringIndex].Contains("asm"))
                {
                    //str += code.strings[stringIndex];
                    pos = 0;
                    stringIndex++;
                    break;
                }
                else
                {
                    str += code.strings[stringIndex] + ";";
                    stringIndex++;
                }
            }


            return (str, str.Length);
        }
    }
}

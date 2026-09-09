using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Qscript.Lex
{
    public class Lexer
    {
        public CodeStruct code;
        public string file;
        public int pos = 0;
        public List<Token> tokenList = new List<Token>();

        private int stringIndex = 0;
        private int offset;
        //private int tokenValue;
        //private int len;

        public Lexer(CodeStruct codeStruct, string _file, int _offset)
        {
            code = codeStruct;
            file = _file;
            offset = _offset;
            //Console.WriteLine($"[Lexer] {file} | offset: {offset}");
        }

        public List<Token> lexAnalysis()
        {
            string value = string.Empty;
            string str = string.Empty;
            char cr;
            bool ff = false;
            TT flag = TT.NULL;
            int len = 1;
            Token token;
            int indexString = 0;
            // 1 - ASTRING
            // 2 - NUMBER
            // 3 - FLOAT
            // 4 - HEX
            // 5 - LITHEX

            bool minusCharCheak()
            {
                string _v = value;
                int _p = pos;
                char _cr = '-';

                while (true)
                {
                    _cr = code.strings[file][stringIndex].Substring(_p, 1)[0];
                    _v += _cr;
                    _p++;
                    if (_v == "extern-func" || _v == "extern-library"
                        || _v == "end-include" || _v == "asm-include"
                        || _v == "asm-inline" || _v == "else-if")
                        return true;
                    if (_p >= code.strings[file][stringIndex].Length)
                        return false;
                }
            }

            while (true)
            {
                start:
                if (pos >= code.strings[file][stringIndex].Length)
                {
                    pos = 0;
                    stringIndex++;
                    if (code.stringsSize[file].Count <= stringIndex)
                        break;
                    if (code.strings[file][stringIndex].Length == 0) goto end;
                }
                if (code.stringsSize[file].Count <= stringIndex)
                    break;

                //Console.WriteLine(pos + " | " + stringIndex);
                cr = code.strings[file][stringIndex].Substring(pos, 1)[0];

                //Console.Write(file + "\t" + cr + "\t");
                //Console.Write(value + "\t");
                //Console.WriteLine(flag);

                if (code.stringsSize[file][stringIndex] - pos >= 2)
                { ff = true; str = code.strings[file][stringIndex].Substring(pos, 2); }
                else ff = false;

                //multi-comment flag
                if (flag == TT.COMMENT)
                {
                    if (!ff || str == "*/")
                        goto end;
                    pos++;
                    flag = TT.NULL;
                    goto end;
                }

                if (flag == TT.VAR)
                {
                    if (((cr >= 'A' && cr <= 'Z') ||
                        (cr >= 'А' && cr <= 'Я') ||
                        (cr >= 'а' && cr <= 'я') ||
                        (cr >= 'a' && cr <= 'z') ||
                        (cr >= 48 && cr <= 57) ||
                        (cr == '_' || (cr == '-' && ff && str[1] != '-' && minusCharCheak())))
                        && indexString == stringIndex)
                    {
                        value += cr;
                        goto end;
                    }
                    switch (value)
                    {
                        // 1
                        case "elif":
                        case "else-if": flag = TT.ELSEIF; break;
                        case "if":      flag = TT.IF;     break;
                        case "else":    flag = TT.ELSE;   break;
                        case "defif":   flag = TT.DEFIF;  break;
                        case "let":     flag = TT.VARDECL;break;
                        case "end-include": flag = TT.ENDINCLUDE; break;
                        case "asm-include": flag = TT.ASMINCLUDE; break;
                        case "include": flag = TT.INCLUDE;break;
                        case "using":   flag = TT.USING; break;
                        // 2
                        case "namespace":      flag = TT.NAMESPACE;     break;
                        case "extern-func":    flag = TT.EXTERNFUNC;    break;
                        case "extern-library": flag = TT.EXTERNLIBRARY; break;
                        case "extern":         flag = TT.EXTERN;        break;
                        case "from":           flag = TT.FROM;          break;
                        case "asm":            flag = TT.ASM;           break;
                        // 3
                        case "return":    flag = TT.RETURN;   break;
                        case "break":     flag = TT.BREAK;    break;
                        case "continue":  flag = TT.CONTINUE; break;
                        case "jump":      flag = TT.JMP;      break;
                        case "loop":      flag = TT.LOOP;     break;
                        case "iter":      flag = TT.ITER;     break;
                        case "for":       flag = TT.FOR;      break;
                        case "while":     flag = TT.WHILE;    break;
                        case "enumerator": flag = TT.ENUMERATOR; break;
                        case "rept":      flag = TT.REPT;     break;
                        case "lambda":    flag = TT.LAMBDA;   break;
                        case "struct":    flag = TT.STRUCT;   break;
                        case "class":     flag = TT.CLASS;    break;
                        case "enum":      flag = TT.ENUM;     break;
                        case "virtual":   flag = TT.VIRTUAL;  break;
                        case "override":  flag = TT.OVERRIDE; break;
                        // 4
                        case "define":    flag = TT.DEFINE;  break;
                        case "typedef":   flag = TT.TYPEDEF; break;
                        case "typeif":    flag = TT.TYPEIF;  break;
                        case "const":     flag = TT.CONST;   break;
                        case "section":   flag = TT.SECTION; break;
                        case "native":    flag = TT.NATIVE;  break;
                        case "inline":    flag = TT.INLINE;  break;
                        case "asminline": flag = TT.ASMINLINE;break;
                        // 5
                        case "operator": flag = TT.OPERATOR; break;
                        case "sizeof":   flag = TT.SIZEOF;   break;
                        case "typeof":   flag = TT.TYPEOF;   break;
                        case "in":       flag = TT.IN;       break;
                        case "public":   flag = TT.MODIFIER; break;
                        case "private":  flag = TT.MODIFIER; break;
                        case "protected":flag = TT.MODIFIER; break;
                        case "true":     flag = TT.BOOL;     break;
                        case "false":    flag = TT.BOOL;     break;
                        
                        default: flag = TT.VAR; break;
                    }
                    if (flag == TT.ASM)
                    {
                        (string _value, int _len) = lexAsmInsert(pos - 2, code.strings[file][stringIndex]);
                        token = new Token(TT.ASM, _value, indexString + offset);
                        tokenList.Add(token);
                        pos = 0;
                        flag = TT.NULL;
                        value = "";
                        indexString = 0;
                        goto start;
                    }
                    else
                    {
                        token = new Token(flag, value, indexString + offset);
                        tokenList.Add(token);
                    }
                    flag = TT.NULL;
                    value = "";
                    indexString = 0;
                }
                if (flag == TT.NUMBER)
                {
                    if (cr >= 48 && cr <= 57 && indexString == stringIndex)
                    {
                        value += cr;
                        goto end;
                    }
                    if (cr == '.' && indexString == stringIndex)
                    {
                        flag = TT.FLOAT;
                        value += cr;
                        goto end;
                    }
                    if (cr == 'f' && indexString == stringIndex)
                    {
                        value += 'f';
                        token = new Token(TT.FLOAT, value, indexString + offset);
                        tokenList.Add(token);
                        flag = TT.NULL;
                        value = string.Empty;
                        indexString = 0;
                        goto end;
                    }
                    token = new Token(TT.NUMBER, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    indexString = 0;
                }
                if (flag == TT.FLOAT)
                {
                    if (cr >= 48 && cr <= 57 && indexString == stringIndex)
                    {
                        value += cr;
                        goto end;
                    }
                    if (cr == 'f' && indexString == stringIndex)
                    {
                        value += "f";
                        token = new Token(TT.FLOAT, value, indexString + offset);
                        tokenList.Add(token);
                        flag = TT.NULL;
                        value = string.Empty;
                        indexString = 0;
                        goto end;
                    }
                    token = new Token(TT.FLOAT, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    indexString = 0;
                }
                if (flag == TT.HEX)
                {
                    if (((cr >= 48 && cr <= 57) ||
                        (cr >= 'A' && cr <= 'F'))
                        && indexString == stringIndex)
                    {
                        value += cr;
                        goto end;
                    }
                    token = new Token(TT.HEX, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    indexString = 0;
                }
                if (flag == TT.LITHEX)
                {
                    if (((cr >= 48 && cr <= 57) ||
                        (cr >= 'A' && cr <= 'F'))
                        && indexString == stringIndex)
                    {
                        value += cr;
                        goto end;
                    }
                    token = new Token(TT.LITHEX, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    indexString = 0;
                }
                if (flag == TT.ASTRING)
                {
                    if (cr != '"')
                    {
                        value += cr;
                        goto end;
                    }
                    token = new Token(TT.ASTRING, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    goto end;
                }
                if (flag == TT.STRING)
                {
                    if (cr != '"' && cr != '\'')
                    {
                        value += cr;
                        goto end;
                    }
                    token = new Token(TT.STRING, value, indexString + offset);
                    tokenList.Add(token);
                    flag = TT.NULL;
                    value = string.Empty;
                    goto end;
                }

                if (ff && (str == "++" || str == "--" || str == "<>")) // PREFIX
                {
                    token = new Token(TT.PREFIX, str, stringIndex + offset);
                    tokenList.Add(token);
                    pos += 1;
                    goto end;
                }
                else if (ff && (str == "==" || str == "!=" || str == "<="
                    || str == ">=" || str == "&&" || str == "||"
                    || str == "+=" || str == "-=" || str == "*="
                    || str == "/=" || str == "%=" //|| str == "&="
                    || str == "|="))
                {
                    token = new Token(TT.OPER, str, stringIndex + offset);
                    tokenList.Add(token);
                    pos += 1;
                    goto end;
                }

                if (ff && str == "A\"" && flag == TT.NULL) // ASTRING
                {
                    flag = TT.ASTRING;
                    pos++;
                    value = string.Empty;
                    goto end;
                }
                else if ((cr == '"' || cr == '\'') && flag == TT.NULL)
                {
                    flag = TT.STRING;
                    value = string.Empty;
                    goto end;
                }
                if (ff && str == "/*")
                {
                    flag = TT.COMMENT;
                    pos++;
                    goto end;
                }

                if (ff && str == "..")
                {
                    token = new Token(TT.TSS, str, stringIndex + offset);
                    tokenList.Add(token);
                    pos += 1;
                    goto end;
                }


                if (ff && str == "0x")
                {
                    flag = TT.HEX;
                    pos++;
                    value = str;
                    indexString = stringIndex;
                    goto end;
                }
                else if (ff && str == "0l")
                {
                    flag = TT.LITHEX;
                    pos++;
                    value = str;
                    indexString = stringIndex;
                    goto end;
                }
                else if (ff && str == "0.")
                {
                    flag = TT.FLOAT;
                    pos++;
                    value = str;
                    indexString = stringIndex;
                    goto end;
                }
                else if (cr >= 48 && cr <= 57) // NUMBER
                {
                    flag = TT.NUMBER;
                    value += cr;
                    indexString = stringIndex;
                    goto end;
                }

                switch (cr)
                {
                    case '?':
                    case '&':
                        token = new Token(TT.PREFIX, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '$':
                        token = new Token(TT.REGDECL, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '@':
                    case '!':
                    case '|':
                    case '*':
                    case '/':
                    case '-':
                    case '+':
                    case '%':
                    case ':':
                    case '~':
                    case '=':
                        token = new Token(TT.OPER, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '(':
                        token = new Token(TT.LPAR, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case ')':
                        token = new Token(TT.RPAR, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '\'':
                        token = new Token(TT.PARS, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '{':
                        token = new Token(TT.LFIG, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '}':
                        token = new Token(TT.RFIG, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '[':
                        token = new Token(TT.LK, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case ']':
                        token = new Token(TT.RK, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '<':
                        token = new Token(TT.OPER, cr.ToString(), stringIndex + offset); // TT.LKN
                        tokenList.Add(token);
                        goto end;
                    case '>':
                        token = new Token(TT.OPER, cr.ToString(), stringIndex + offset); // TT.RKN
                        tokenList.Add(token);
                        goto end;
                    case ' ':
                        //token = new Token(TT.SPACE, cr.ToString(), stringIndex);
                        //tokenList.Add(token);
                        goto end;
                    case '\t':
                        //token = new Token(TT.TAB, cr.ToString(), stringIndex);
                        //tokenList.Add(token);
                        goto end;
                    case '\n':
                        //token = new Token(TT.N, cr.ToString(), stringIndex);
                        //tokenList.Add(token);
                        goto end;
                    case ',':
                        token = new Token(TT.PS, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case '.':
                        token = new Token(TT.TS, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                    case ';':
                        token = new Token(TT.SEM, cr.ToString(), stringIndex + offset);
                        tokenList.Add(token);
                        goto end;
                }

                if (flag == TT.NULL)
                {
                    value += cr;
                    flag = TT.VAR;
                    indexString = stringIndex;
                }
                end:

                pos++;
                if (pos >= code.strings[file][stringIndex].Length)
                {
                    pos = 0;
                    stringIndex++;
                    if (code.strings[file].Count <= stringIndex)
                        break;
                    if (code.strings[file][stringIndex].Length == 0) goto end;
                    //if (code.stringsSize[file].Count - 1 <= stringIndex)
                    //    break;
                }
            }

            if (flag != TT.NULL && value != string.Empty)
            {
                token = new Token(flag, value, indexString);
                tokenList.Add(token);
            }

            List<Token> tokens = new List<Token>();
            for (int i = 0; i < tokenList.Count; i++)
            {
                //Console.WriteLine(tokenList[i].ToString());
                if (tokenList[i].type != TT.SPACE &&
                    tokenList[i].type != TT.TAB &&
                    tokenList[i].type != TT.COMMENT)
                {
                    tokens.Add(tokenList[i]);

                    // Отладочный вывод
                    string spaces = "";
                    for (int j = 0; j < 20 - tokenList[i].type.ToString().Length; j++)
                    {
                        spaces += " ";
                    }
                    //if (tokenList[i].type == TT.LITHEX)
                    //{
                        //Console.WriteLine($"[LEXER] Index:{i} Token pos:{tokenList[i].pos} type:{tokenList[i].type}{spaces}value:{tokenList[i].value}");
                    //}
                }
            }

            return tokens;
        }

        private bool skipSpace()
        {
            Match whitespace = Regex.Match(code.strings[file][stringIndex].Substring(pos), @"^\s+");
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
            string str = _code.Substring(startPos) + ";";//= code.strings[file][stringIndex].Substring(startPos);
            stringIndex++;
            while (true)
            {
                if (code.strings[file][stringIndex].Contains("asm"))
                {
                    //str += code.strings[stringIndex];
                    pos = 0;
                    stringIndex++;
                    break;
                }
                else
                {
                    str += code.strings[file][stringIndex] + ";";
                    stringIndex++;
                }
            }


            return (str, str.Length);
        }
    }
}

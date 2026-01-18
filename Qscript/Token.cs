using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Qscript
{
    //Match regx = Regex.Match(config_html, "<p>(.*)</p></article>");

    //string content = regx.Groups[1].Value;
    public class Token
    {
        public TokenType type;
        public string value;
        public int pos;

        public Token(TokenType _type, string _value, int _pos)
        {
            type = _type;
            value = _value;
            pos = _pos;
        }
    }
    public class TokenType
    {
        public string type;
        public string regx;

        public TokenType(string _type, string _regx)
        {
            type = _type;
            regx = _regx;
        }
    }

    public static class TokenTypeList
    {
        public static Dictionary<string, TokenType> tokenTypes = new Dictionary<string, TokenType>();
        public static Dictionary<string, string> rightPar = new Dictionary<string, string>();

        public static Dictionary<string, int> permissionOper = new Dictionary<string, int>()
        {
            { "=", 1  },
            { "+=", 1 },
            { "-=", 1 },
            { "*=", 1 },
            { "/=", 1 },
            { "+", 2 },
            { "-", 2 },
            { "*", 3 },
            { "/", 3}
        };


        static TokenTypeList()
        {
            // TYPES
            
            //tokenTypes.Add("TYPE", new TokenType("TYPE", "(int32|int16|int8|float|string|char)"));
            //tokenTypes.Add("TYPE", new TokenType("TYPE", ":?"));
            //tokenTypes.Add("INT32TYPE", new TokenType("INT32TYPE", "int32"));
            //tokenTypes.Add("INT16TYPE", new TokenType("INT16TYPE", "int16"));
            //tokenTypes.Add("INT8TYPE", new TokenType("INT8TYPE", "int8"));
            //tokenTypes.Add("FLOATTYPE", new TokenType("FLOATTYPE", "float"));
            //tokenTypes.Add("STRINGTYPE", new TokenType("STRINGTYPE", "string"));
            //tokenTypes.Add("CHARTYPE", new TokenType("CHARTYPE", "char"));

            // Logic
            tokenTypes.Add("ELSEIF", new TokenType("ELSEIF", "else-if"));
            tokenTypes.Add("IF", new TokenType("IF", "if"));
            tokenTypes.Add("ELSE", new TokenType("ELSE", "else"));

            // Logic Values
            //tokenTypes.Add("SAMEOPER", new TokenType("SAME", "=="));
            //tokenTypes.Add("NOTSAMEOPER", new TokenType("NOTSAMEOPER", "!="));
            //tokenTypes.Add("BIGOPER", new TokenType("BIGOPER", "<<"));
            //tokenTypes.Add("SMALLOPER", new TokenType("SMALLOPER", ">>"));
            //tokenTypes.Add("BIGSAMEOPER", new TokenType("BIGSAMEOPER", "<="));
            //tokenTypes.Add("SMALLSAMEOPER", new TokenType("SMALLSAMEOPER", ">="));
            //tokenTypes.Add("OPER", new TokenType("OPER", @"(==|!=|<<|>>|<=|>=|=<|=>|=?)"));
            //tokenTypes.Add("INC", new TokenType("INC", "[\\--]*"));
            //tokenTypes.Add("DEC", new TokenType("DEC", "[\\++]*"));
            tokenTypes.Add("OPER", new TokenType("OPER", "(\\++|\\--|==|!=|<<|>>|<=|>=|&&|\\|\\||\\+=|-=|\\*=|\\/=|%=|&=|\\|=|\\^=|<<=|>>=|->|[+\\-*/%=<>&|!?:~])"));
            //tokenTypes.Add("ASSIGN", new TokenType("ASSIGN", "<"));
            //tokenTypes.Add("ASSIGN", new TokenType("ASSIGN", ">"));

            // Keys
            //tokenTypes.Add("OUT", new TokenType("OUT", "out"));
            tokenTypes.Add("ENDINCLUDE", new TokenType("ENDINCLUDE", "end-include"));
            tokenTypes.Add("ASMINCLUDE", new TokenType("ASMINCLUDE", "asm-include"));
            tokenTypes.Add("INCLUDE", new TokenType("INCLUDE", "include"));
            tokenTypes.Add("USING", new TokenType("USING", "using"));
            tokenTypes.Add("ASM", new TokenType("ASM", "asm"));
            tokenTypes.Add("SEM", new TokenType("SEM", ";"));
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro[A-Z]+"));
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro"));
            tokenTypes.Add("RETURN", new TokenType("RETURN", "return"));
            tokenTypes.Add("BREAK", new TokenType("BREAK", "break"));
            tokenTypes.Add("CONTINUE", new TokenType("CONTINUE", "continue"));

            tokenTypes.Add("FOR", new TokenType("FOR", "for"));
            tokenTypes.Add("WHILE", new TokenType("WHILE", "while"));

            tokenTypes.Add("STRUCT", new TokenType("STRUCT", "struct"));
            tokenTypes.Add("CLASS", new TokenType("CLASS", "class"));

            tokenTypes.Add("INLINE", new TokenType("INLINE", "inline"));


            // modifecator модификаторы 
            //tokenTypes.Add("PUBLIC", new TokenType("PUBLIC", "public"));
            //tokenTypes.Add("PRIVATE", new TokenType("PRIVATE", "private"));
            //tokenTypes.Add("PROTECTED", new TokenType("PROTECTED", "protected"));
            tokenTypes.Add("MODIFIER", new TokenType("MODIFIER", "(public|private|protected)"));


            tokenTypes.Add("BOOL", new TokenType("BOOL", @"(true|false)"));
            tokenTypes.Add("CONST", new TokenType("CONST", @"[A-Z]*"));
            tokenTypes.Add("VAR", new TokenType("VAR", @"[a-zA-Z_][a-zA-Z0-9_]*"));
            //tokenTypes.Add("CONST", new TokenType("CONST", @"[A-Z]*"));
            tokenTypes.Add("NUMBER", new TokenType("NUMBER", "[0-9]+"));
            tokenTypes.Add("STRING", new TokenType("STRING", @"""[^""]*"""));//@"""[^""//]*[^""\\]*(?:\\.[^""\\]*)*"""));
            //tokenTypes.Add("CHAR", new TokenType("CHAR", @"'[^'\\]*(?:\\.[^'\\]*)*'"));

            // Arifmetic
            //tokenTypes.Add("ASSIGN", new TokenType("ASSIGN", "="));
            //tokenTypes.Add("PLUS", new TokenType("PLUS", "\\+"));
            //tokenTypes.Add("MINUS", new TokenType("MINUS", "\\-"));
            //tokenTypes.Add("MUL", new TokenType("MUL", "\\*"));
            //tokenTypes.Add("DIV", new TokenType("DIV", "\\/"));

            // Pars
            tokenTypes.Add("LPAR", new TokenType("LPAR", "\\("));
            tokenTypes.Add("RPAR", new TokenType("RPAR", "\\)"));
            tokenTypes.Add("PARS", new TokenType("PARS", "\\'"));

            tokenTypes.Add("LFIG", new TokenType("LFIG", "\\{"));
            tokenTypes.Add("RFIG", new TokenType("RFIG", "\\}"));

            tokenTypes.Add("LK", new TokenType("LK", "\\["));
            tokenTypes.Add("RK", new TokenType("RK", "\\]"));

            tokenTypes.Add("LKN", new TokenType("LKN", "\\<"));
            tokenTypes.Add("RKN", new TokenType("RKN", "\\>"));

            tokenTypes.Add("SPACE", new TokenType("SPACE", " "));
            tokenTypes.Add("TAB", new TokenType("TAB", "\t"));

            tokenTypes.Add("PS", new TokenType("PS", ","));
            tokenTypes.Add("TS", new TokenType("TS", "."));

            rightPar.Add("LPAR", "RPAR");
            rightPar.Add("LFIG", "RFIG");
            rightPar.Add("LK", "RK");
            rightPar.Add("LKN", "RKN");
            //tokenTypes.Add("SP", new TokenType("SP", " "));
        }
    }
}

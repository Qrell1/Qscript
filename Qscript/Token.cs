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
        public int posCode;

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
            tokenTypes.Add("COMMENT", new TokenType("COMMENT", @"//.*$|/\*[\s\S]*?\*/"));
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
            tokenTypes.Add("ELSEIF", new TokenType("ELSEIF", @"\belse-if\b"));
            tokenTypes.Add("IF", new TokenType("IF", @"\bif\b"));
            tokenTypes.Add("ELSE", new TokenType("ELSE", @"\belse\b"));

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
            tokenTypes.Add("FLOAT", new TokenType("FLOAT", @"([0-9]+\.[0-9]*f?|\.[0-9]+f?|[0-9]+f)"));
            tokenTypes.Add("NUMBER", new TokenType("NUMBER", "[0-9]+"));
            tokenTypes.Add("PREFIX", new TokenType("PREFIX", "(\\+\\+|--|\\<>|\\?|&)"));
            tokenTypes.Add("OPER", new TokenType("OPER", "(@|\\+=|==|!=|<=|>=|<|>|&&|\\|\\||-=|\\*=|\\/=|%=|&=|\\|=|\\^=|<<=|>>=|->|[+\\-\\*/%=|!:~])"));
            //tokenTypes.Add("ASSIGN", new TokenType("ASSIGN", "<"));
            //tokenTypes.Add("ASSIGN", new TokenType("ASSIGN", ">"));

            // Keys
            //tokenTypes.Add("OUT", new TokenType("OUT", "out"));
            tokenTypes.Add("ENDINCLUDE", new TokenType("ENDINCLUDE", @"\bend-include\b"));
            tokenTypes.Add("ASMINCLUDE", new TokenType("ASMINCLUDE", @"\basm-include\b"));
            tokenTypes.Add("INCLUDE", new TokenType("INCLUDE", @"\binclude\b"));
            tokenTypes.Add("USING", new TokenType("USING", @"\busing\b"));

            tokenTypes.Add("EXTERNFUNC", new TokenType("EXTERNFUNC", @"\bextern-func\b"));
            tokenTypes.Add("EXTERNLIBRARY", new TokenType("EXTERNLIBRARY", @"\bextern-library\b"));
            tokenTypes.Add("EXTERN", new TokenType("EXTERN", @"\bextern\b"));
            tokenTypes.Add("FROM", new TokenType("FROM", @"\bfrom\b"));

            tokenTypes.Add("ASM", new TokenType("ASM", @"\basm\b"));
            tokenTypes.Add("SEM", new TokenType("SEM", ";"));
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro[A-Z]+"));
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro"));
            tokenTypes.Add("RETURN", new TokenType("RETURN", @"(\breturn\b|\bвернуть\b)"));
            tokenTypes.Add("BREAK", new TokenType("BREAK", @"(\bbreak\b|\bпрервать\b)"));
            tokenTypes.Add("CONTINUE", new TokenType("CONTINUE", @"(\bcontinue\b|\bпродолжить\b)"));
            tokenTypes.Add("JMP", new TokenType("JMP", @"(\bjump\b|\bпрыгнуть\b)"));

            tokenTypes.Add("ITER", new TokenType("ITER", @"\biter\b"));
            tokenTypes.Add("FOR", new TokenType("FOR", @"\bfor\b"));
            tokenTypes.Add("WHILE", new TokenType("WHILE", @"\bwhile\b"));
            tokenTypes.Add("ENUMERATOR", new TokenType("ENUMERATOR", @"\benumerator\b"));
            tokenTypes.Add("REPT", new TokenType("REPT", @"\brept\b"));

            tokenTypes.Add("STRUCT", new TokenType("STRUCT", @"(\bstruct\b|\bструктура\b)"));
            tokenTypes.Add("CLASS", new TokenType("CLASS", @"(\bclass\b|\bкласс\b)"));

            tokenTypes.Add("VIRTUAL", new TokenType("VIRTUAL", @"\bvirtual\b"));
            tokenTypes.Add("OVERRIDE", new TokenType("OVERRIDE", @"\boverride\b"));
            //tokenTypes.Add("FUNCTION", new TokenType("FUNCTION", @"\bfunction\b"));

            tokenTypes.Add("DEFINE", new TokenType("DEFINE", @"(\bdefine\b|\bзаменить\b)"));
            tokenTypes.Add("TYPEDEF", new TokenType("TYPEDEF", @"(\btypedef\b|\bсоздать_тип\b)"));
            tokenTypes.Add("TYPEIF", new TokenType("TYPEIF", @"(\btypeif\b|\bесли_тип\b)"));

            tokenTypes.Add("CONST", new TokenType("CONST", @"(\bconst\b|\bконст\b)"));

            tokenTypes.Add("SECTION", new TokenType("SECTION", @"\bsection\b"));
            tokenTypes.Add("NATIVE", new TokenType("NATIVE", @"\bnative\b"));
            tokenTypes.Add("INLINE", new TokenType("INLINE", @"\binline\b"));
            tokenTypes.Add("SIZEOF", new TokenType("SIZEOF", @"\bsizeof\b"));
            tokenTypes.Add("TYPEOF", new TokenType("TYPEOF", @"\btypeof\b"));

            tokenTypes.Add("IN", new TokenType("IN", @"\bin\b"));

            // modifecator модификаторы 
            //tokenTypes.Add("PUBLIC", new TokenType("PUBLIC", "public"));
            //tokenTypes.Add("PRIVATE", new TokenType("PRIVATE", "private"));
            //tokenTypes.Add("PROTECTED", new TokenType("PROTECTED", "protected"));
            tokenTypes.Add("MODIFIER", new TokenType("MODIFIER", @"(\bpublic\b|\bprivate\b|\bprotected\b)"));


            tokenTypes.Add("BOOL", new TokenType("BOOL", @"(\btrue\b|\bfalse\b)"));
            //tokenTypes.Add("CONST", new TokenType("CONST", @"\b[A-Z]*\b"));
            tokenTypes.Add("VAR", new TokenType("VAR", @"[а-яА-Яa-zA-Z_][а-яА-Яa-zA-Z0-9_]*"));
            //tokenTypes.Add("CONST", new TokenType("CONST", @"[A-Z]*"));
            //tokenTypes.Add("FLOAT", new TokenType("FLOAT", @"([0-9]+\.[0-9]*f?|\.[0-9]+f?|[0-9]+f)"));
            //tokenTypes.Add("NUMBER", new TokenType("NUMBER", "[0-9]+"));
            tokenTypes.Add("CHAR", new TokenType("CHAR", @"'[^'\\]*(?:\\.[^'\\]*)*'"));
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
            tokenTypes.Add("N", new TokenType("N", "\n"));

            tokenTypes.Add("PS", new TokenType("PS", "\\,"));
            tokenTypes.Add("TS", new TokenType("TS", "\\."));

            rightPar.Add("LPAR", "RPAR");
            rightPar.Add("LFIG", "RFIG");
            rightPar.Add("LK", "RK");
            rightPar.Add("LKN", "RKN");
            //tokenTypes.Add("SP", new TokenType("SP", " "));
        }
    }
}

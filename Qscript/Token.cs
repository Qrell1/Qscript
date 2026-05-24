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
        public TT type;
        public string value;
        public int pos;
        public int posCode;

        public Token(TT _type, string _value, int _pos)
        {
            type = _type;
            value = _value;
            pos = _pos;
        }
        public Token(string _value, int _pos)
        {
            type = TT.NULL;
            value = _value;
            pos = _pos;
        }
    }

    public enum TT
    {   
        COMMENT,
        ELSEIF, IF, ELSE,
        FLOAT, NUMBER,
        PREFIX, REGDECL, OPER, VARDECL,
        ENDINCLUDE, ASMINCLUDE, INCLUDE, USING, 
        NAMESPACE, EXTERNFUNC, EXTERNLIBRARY, EXTERN, FROM,
        ASM, SEM, 
        RETURN, BREAK, CONTINUE, JMP, 
        ITER, FOR, WHILE, ENUMERATOR, REPT,
        LAMBDA, STRUCT, CLASS, ENUM, VIRTUAL, OVERRIDE, 
        DEFINE, TYPEDEF, TYPEIF,
        CONST, SECTION, NATIVE, INLINE, ASMINLINE, OPERATOR, 
        SIZEOF, TYPEOF, IN, MODIFIER,
        BOOL, VAR, CHAR, STRING, 
        LPAR, RPAR, PARS, LFIG, RFIG, LK, RK, LKN, RKN,
        SPACE, TAB, N, PS, TS,
        NULL
    }

    public static class TokenTypeList
    {
        public static Dictionary<TT, string> tokenTypes = new Dictionary<TT, string>();
        public static Dictionary<TT, TT> rightPar = new Dictionary<TT, TT>();

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
            tokenTypes.Add(TT.COMMENT, @"//.*$|/\*[\s\S]*?\*/");
            //tokenTypes.Add("COMMENT", new TokenType("COMMENT", @"//.*$|/\*[\s\S]*?\*/"));
            
            // TYPES
            //tokenTypes.Add("TYPE", new TokenType("TYPE", "(int32|int16|int8|float|string|char)"));
            //tokenTypes.Add("TYPE", new TokenType("TYPE", ":?"));
            //tokenTypes.Add("INT32TYPE", new TokenType("INT32TYPE", "int32"));
            //tokenTypes.Add("INT16TYPE", new TokenType("INT16TYPE", "int16"));
            //tokenTypes.Add("INT8TYPE", new TokenType("INT8TYPE", "int8"));
            //tokenTypes.Add("FLOATTYPE", new TokenType("FLOATTYPE", "float"));
            //tokenTypes.Add("STRINGTYPE", new TokenType("STRINGTYPE", "string"));
            //tokenTypes.Add("CHARTYPE", new TokenType("CHARTYPE", "char"));
            //tokenTypes.Add("DECLVAR", new TokenType("DECLVAR", @"\bvar\b"));
            // Logic
            tokenTypes.Add(TT.ELSEIF, @"\belse-if\b");
            tokenTypes.Add(TT.IF,     @"\bif\b");
            tokenTypes.Add(TT.ELSE,   @"\belse\b");

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
            tokenTypes.Add(TT.FLOAT,   @"([0-9]+\.[0-9]*f?|\.[0-9]+f?|[0-9]+f)");
            tokenTypes.Add(TT.NUMBER,  "[0-9]+");
            tokenTypes.Add(TT.PREFIX,  "(\\+\\+|--|\\<>|\\?|&)");
            tokenTypes.Add(TT.REGDECL, "\\$");
            tokenTypes.Add(TT.OPER,    "(=>|@|\\+=|==|!=|<=|>=|<|>|&&|\\|\\||-=|\\*=|\\/=|%=|&=|\\|=|\\^=|<<=|>>=|->|[+\\-\\*/%=|!:~])");
            tokenTypes.Add(TT.VARDECL, @"(\bvarriable\b|\blet\b)");

            tokenTypes.Add(TT.ENDINCLUDE, @"\bend-include\b");
            tokenTypes.Add(TT.ASMINCLUDE, @"\basm-include\b");
            tokenTypes.Add(TT.INCLUDE,    @"\binclude\b");
            tokenTypes.Add(TT.USING,      @"\busing\b");

            tokenTypes.Add(TT.NAMESPACE,     @"\bnamespace\b");
            tokenTypes.Add(TT.EXTERNFUNC,    @"\bextern-func\b");
            tokenTypes.Add(TT.EXTERNLIBRARY, @"\bextern-library\b");
            tokenTypes.Add(TT.EXTERN,        @"\bextern\b");
            tokenTypes.Add(TT.FROM,          @"\bfrom\b");

            tokenTypes.Add(TT.ASM, @"\basm\b");
            tokenTypes.Add(TT.SEM, ";");
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro[A-Z]+"));
            //tokenTypes.Add("MACRO", new TokenType("MACRO", "macro"));
            tokenTypes.Add(TT.RETURN,    @"(\breturn\b|\bвернуть\b)");
            tokenTypes.Add(TT.BREAK,     @"(\bbreak\b|\bпрервать\b)");
            tokenTypes.Add(TT.CONTINUE,  @"(\bcontinue\b|\bпродолжить\b)");
            tokenTypes.Add(TT.JMP,       @"(\bjump\b|\bпрыгнуть\b)");

            
            tokenTypes.Add(TT.ITER,       @"\biter\b");
            tokenTypes.Add(TT.FOR,        @"\bfor\b");
            tokenTypes.Add(TT.WHILE,      @"\bwhile\b");
            tokenTypes.Add(TT.ENUMERATOR, @"\benumerator\b");
            tokenTypes.Add(TT.REPT,       @"\brept\b");

            tokenTypes.Add(TT.LAMBDA,  @"\blambda\b");
            tokenTypes.Add(TT.STRUCT,  @"(\bstruct\b|\bструктура\b)");
            tokenTypes.Add(TT.CLASS,   @"(\bclass\b|\bкласс\b)");
            tokenTypes.Add(TT.ENUM,    @"(\benum\b|\bсловарь\b)");

            tokenTypes.Add(TT.VIRTUAL,  @"\bvirtual\b");
            tokenTypes.Add(TT.OVERRIDE, @"\boverride\b");
            //tokenTypes.Add("FUNCTION", new TokenType("FUNCTION", @"\bfunction\b"));

            tokenTypes.Add(TT.DEFINE,   @"(\bdefine\b|\bзаменить\b)");
            tokenTypes.Add(TT.TYPEDEF,  @"(\btypedef\b|\bсоздать_тип\b)");
            tokenTypes.Add(TT.TYPEIF,   @"(\btypeif\b|\bесли_тип\b)");

            tokenTypes.Add(TT.CONST,    @"(\bconst\b|\bконст\b)");

            tokenTypes.Add(TT.SECTION,    @"\bsection\b");
            tokenTypes.Add(TT.NATIVE,     @"\bnative\b");
            tokenTypes.Add(TT.INLINE,     @"\binline\b");
            tokenTypes.Add(TT.ASMINLINE,  @"(\basm-inline\b|\basminline\b)");
            tokenTypes.Add(TT.OPERATOR,   @"(\boperator\b|\bоператор\b)");
            tokenTypes.Add(TT.SIZEOF,     @"\bsizeof\b");
            tokenTypes.Add(TT.TYPEOF,     @"\btypeof\b");

            tokenTypes.Add(TT.IN,         @"\bin\b");


            tokenTypes.Add(TT.MODIFIER,   @"(\bpublic\b|\bprivate\b|\bprotected\b)");


            tokenTypes.Add(TT.BOOL,       @"(\btrue\b|\bfalse\b)");
            tokenTypes.Add(TT.VAR,        @"[_а-я_А-Я_a-z_A-Z_][а-я_А-Я_a-z_A-Z_0-9_]*");
            tokenTypes.Add(TT.CHAR,       @"'[^'\\]*(?:\\.[^'\\]*)*'");
            tokenTypes.Add(TT.STRING,     @"""[^""]*""");//@"""[^""//]*[^""\\]*(?:\\.[^""\\]*)*"""));
            //tokenTypes.Add("CHAR", new TokenType("CHAR", @"'[^'\\]*(?:\\.[^'\\]*)*'"));

            tokenTypes.Add(TT.LPAR,  "\\(");
            tokenTypes.Add(TT.RPAR,  "\\)");
            tokenTypes.Add(TT.PARS,  "\\'");

            tokenTypes.Add(TT.LFIG, "\\{");
            tokenTypes.Add(TT.RFIG, "\\}");

            tokenTypes.Add(TT.LK, "\\[");
            tokenTypes.Add(TT.RK, "\\]");

            tokenTypes.Add(TT.LKN, "\\<");
            tokenTypes.Add(TT.RKN, "\\>");

            tokenTypes.Add(TT.SPACE, " ");
            tokenTypes.Add(TT.TAB,   "\t");
            tokenTypes.Add(TT.N,     "\n");

            tokenTypes.Add(TT.PS, "[,]");
            tokenTypes.Add(TT.TS, "[.]");

            rightPar.Add(TT.LPAR, TT.RPAR);
            rightPar.Add(TT.LFIG, TT.RFIG);
            rightPar.Add(TT.LK,   TT.RK);
            rightPar.Add(TT.LKN,  TT.RKN);
            //tokenTypes.Add("SP", new TokenType("SP", " "));
        }
    }
}

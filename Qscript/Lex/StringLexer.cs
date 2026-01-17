using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    internal class StringLexer
    {
        //public List<List<Token>> stringsTokens = new List<List<Token>>();
        public List<Token> tokens;
        public int pos = 0;

        public StringLexer (List<Token> _tokens)
        {
            tokens = _tokens;
        }

        public bool match(string type)
        {
            Token currentToken = tokens[pos];
            if (type == currentToken.type.type)
            {

                return true;
            }
            return false;
        }
        public List<List<Token>> LexStrings ()
        {
            List<Token> tokenString = new List<Token>();
            List<List<Token>> stringsTokens = new List<List<Token>>();

            foreach (Token token in tokens)
            {
                if (token.type.type == "SEM")
                {
                    stringsTokens.Add(tokenString);
                    tokenString = new List<Token>();
                } else
                {
                    tokenString.Add(token);
                }
                pos += 1;
            }
            return stringsTokens;
        }
    }
}

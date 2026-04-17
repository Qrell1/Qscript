using System;
using System.Collections.Generic;
using System.Linq;

namespace Qscript
{
    internal class Parser
    {
        public List<Token> tokens;
        public int pos = 0;
        public bool sem = false;

        private ProgramNode root;
        private string NamespaceString;

        public CommonNode nullNode = new CommonNode("NULLNODE", new Token(null, "", 0));


        public Parser(List<Token> _tokens)
        {
            tokens = _tokens;
            // Пропускаем пробелы в начале
            while (pos < tokens.Count && tokens[pos].type.type == "SPACE")
            {
                pos++;
            }
        }

        /// <summary>
        /// Мы проверяем токен на нужный тип на текущей позиции, но не переносим коретку
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool peek(TokenType type)
        {
            if (pos < tokens.Count)
            {
                Token token = tokens[pos];
                if (token.type == type)
                {
                    //pos++;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Мы проверяем токен на нужный тип на текущей позиции, но не переносим коретку
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool peek(string type)
        {
            if (pos < tokens.Count)
            {
                Token token = tokens[pos];
                if (token.type.type == type)
                {
                    //pos++;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Мы берём токен на позиции и перемещаем корретку
        /// </summary>
        public Token take()
        {

            Token token = tokens[pos];
            pos++;
            return token;
        }
        /// <summary>
        /// Сентаксическая функциия которая явно указывает что тут должен быть токен нужного типа!
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool expect(string type)
        {
            if (tokens[pos].type.type == type)
            {
                return true;
            }
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{type}", tokens[pos].pos);
            return false;
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{type}");
        }
        /// <summary>
        /// Сентаксическая функциия которая явно указывает что тут должен быть токен нужного типа!
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool expect(string[] types)
        {
            if (types.Contains(tokens[pos].type.type))
            {
                return true;
            }
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{types}");
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{types}", tokens[pos]);
            return false;
        }
        public void skip()
        {
            pos++;
        }

        /// <summary>
        /// При вызове pos должен указывать на "<" выходе он будет указывать на то что после ">"
        /// </summary>
        public int tryParseDeclarator(int offset=1)
        {
            int ps = pos + offset;
            int z = 0;
            if (tokens[pos].value == "@")
                try
                {
                    while (true)
                    {
                        if (tokens[ps].value == "@") { z++; ps++; }
                        else if (tokens[ps].value == ",") { ps++; }
                        else if (tokens[ps].value == "*") { ps++; }
                        else if (tokens[ps].type.type == "VAR" && (tokens[ps+1].value == "," || tokens[ps+1].value == "*" || tokens[ps+1].value == "@")) { ps++; }
                        else  { ps++;  break; }
                    }
                } catch { return -1; }
            else 
            try
            {
                while (true)
                {
                    if (tokens[ps].value == "<") { z++; ps++; }
                    else if (tokens[ps].value == ">" && z != 0) { z--; ps++; }
                    else if (tokens[ps].value == ">" && z == 0) { ps++; break; }
                    else if (tokens[ps].value != ">") { ps++; }
                }
            }
            catch { return -1; }//SyntaxError("Ну тип ошибка в вызове декларотивной функции"); }
            return ps;
        }
        public CommonNode tryParseVarPath(CommonNode varNode)
        {
            while (peek("TS") || (tokens[pos].value == ":" && tokens[pos+1].value == ":" && ++pos != pos))
            {
                skip(); 
                varNode.token.value += "." + take().value;
                if (tokens[pos].type.type != "TS") break;
                //if (tokens[pos].value != ":" && tokens[pos+1].value != ":") break;
            }
            return varNode;
        }

        public bool peekFig(int _pos)
        {
            int ps = _pos;
            while (true)
            {
                if (tokens[ps].value == ")") break;
                else ps++;
            }
            if (tokens[ps+1].type.type == "LFIG") return true;
            else return false;
        }
        public List<Token> parseParBody(string type)
        {
            string strType = type;
            string strEndTokenType = TokenTypeList.rightPar[strType];
            List<Token> list = new List<Token>();
            if (pos < tokens.Count)
            {
                for (int i = pos; i < tokens.Count; i++)
                {
                    if (tokens[i].type.type == strEndTokenType)
                    {
                        return list;
                    }
                    list.Add(tokens[i]);
                }
            }
            throw new Exception($"Ожидался Токен:{strEndTokenType} для завершения тела Скобки:{strType}");
        }

        public CommonNode parsePar()
        {
            if (tokens[pos].value == "@"  && tokens[pos + 1].type.type == "VAR")
            {
                skip();
                CommonNode typeOper = new CommonNode("TYPEOPER", take());
                CommonNode parNode = parsePar();
                typeOper.childs.Add(parNode);
                return typeOper;
            }
            if (tokens[pos].type.type == "LPAR" && tokens[pos + 1].type.type == "VAR" && tokens[pos + 2].type.type == "RPAR")
            {
                skip();
                CommonNode typeOper = new CommonNode("TYPEOPER", take());
                skip();
                CommonNode parNode = parsePar();
                typeOper.childs.Add(parNode);
                return typeOper;
            }
            if (peek("LPAR"))
            {
                skip();
                CommonNode node = parseFormula();

                if (!peek("RPAR"))
                {
                    throw new Exception($"Ожидалась закрывающая скобка на позиции {pos}");
                }
                skip();
                return node;
            }
            else
            {
                return parseVariableOrNumberOrFunction();
            }
        }
        public CommonNode parseVariableOrNumberOrFunction()
        {
            Token token = take();
            if (token.type.type == "SEM") Syntax.SyntaxError("Мдамс получается ты тут накосячил. Честно я не знаю как.\n Но совет если при вызове функции не передаёшь аргументы всегда пиши ()!", new CommonNode(token.type.type, token));
            /*if (token.type.type == "LPAR" && tokens[pos + 1].type.type == "VAR" && tokens[pos + 2].type.type == "RPAR")
            {
                CommonNode typeOper = new CommonNode("TYPEOPER", take());
                skip();
                token = take();
            }*/
            if (token.value == "-" && peek("NUMBER"))
            {
                Token number = take();
                number.value = token.value + number.value;
                return new CommonNode("NUMBER", number);
            }
            if (token.value == "-" && peek("FLOAT"))
            {
                Token floatn = take();
                floatn.value = token.value + floatn.value;
                return new CommonNode("FLOAT", floatn);
            }
            if (token.type.type == "PREFIX" && (token.value == "++" || token.value == "--"))
            {
                CommonNode unarNode = new CommonNode("PREUNAROPER", token); expect("VAR");
                CommonNode node = new CommonNode("VAR", take());
                node = tryParseVarPath(node);
                unarNode.childs.Add(node);
                return unarNode;
            }
            if (token.type.type == "PREFIX" && token.value == "&")
            {
                CommonNode addr = new CommonNode("ADDRESS", token);
                expect("VAR");
                CommonNode node = new CommonNode("VAR", take());
                node = tryParseVarPath(node);
                node = parseCall(node);
                addr.childs.Add(node);
                return addr;
            }
            if (token.type.type == "SIZEOF")
            {
                CommonNode sizeofNode = new CommonNode("SIZEOF", token);
                expect("TS"); skip(); expect("VAR");
                CommonNode node = new CommonNode("VAR", take());
                node = tryParseVarPath(node);
                sizeofNode.childs.Add(node);
                return sizeofNode;
            }
            if (token.type.type == "TYPEOF")
            {
                CommonNode sizeofNode = new CommonNode("TYPEOF", token);
                expect("TS"); skip(); expect("VAR");
                CommonNode node = new CommonNode("VAR", take());
                node = tryParseVarPath(node);
                sizeofNode.childs.Add(node);
                return sizeofNode;
            }
            if (token.type.type == "VAR")
            {
                CommonNode node = new CommonNode("VAR", token);
                node = tryParseVarPath(node);
                if (tokens[pos].value == "@")
                {
                    node = new CommonNode("CALL", node.token);
                    node = parseCall(node);
                    return node;
                }

                if (tokens[pos].value == "<")
                {
                    int ps = tryParseDeclarator();
                    if (ps != -1 && tokens[ps].type.type == "LPAR")
                    {
                        node = new CommonNode("CALL", node.token);
                        node = parseCall(node);
                        return node;
                    }
                }
                if (tokens[pos].type.type == "LPAR")
                {
                    node.type = "CALL";
                    node = parseCall(node);
                    return node;
                }

                if (peek("LK"))
                {
                    CommonNode offsetNode = new CommonNode("OFFSET", take());
                    offsetNode.childs.Add(parseFormula()); expect("RK"); skip();
                    node.childs.Add(offsetNode);
                }

                if (peek("PREFIX") && (tokens[pos].value == "++" || tokens[pos].value == "--"))
                {
                    CommonNode unarNode = new CommonNode("POSTUNAROPER", take());
                    unarNode.childs.Add(node);
                    return unarNode;
                }

                return node;
            }

            if (token.type.type == "NUMBER") return new CommonNode("NUMBER", token);
            if (token.type.type == "STRING") return new CommonNode("STRING", token);
            if (token.type.type == "CHAR")   return new CommonNode("CHAR",   token);
            if (token.type.type == "CONST")  return new CommonNode("CONST" , token);
            if (token.type.type == "BOOL")   return new CommonNode("BOOL"  , token);
            if (token.type.type == "FLOAT")  return new CommonNode("FLOAT" , token);

            Syntax.SyntaxError($"Ошибка в парсинге формулы из-за Токена:{token.value}", token);
            return null;
        }

        public CommonNode parseFormula(CommonNode leftOper = null)
        {
            CommonNode buffer;
            CommonNode left; // token 1
            if (leftOper == null)
                left = parseTerm(); // token 1
            else
                left = leftOper;
            Token operatpor = null;          // token 2
            if (peek("OPER") && (new string[] { "&&", "||" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parseTerm();
                buffer = left;
                left = new CommonNode("CMP", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == "CMP")
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением логики в арифметике!");
                if (peek("OPER") && (new string[] { "&&", "||" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }

            return left;
        }
        public CommonNode parseTerm()
        {
            CommonNode buffer;
            CommonNode left = parseTerm2(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek("OPER") && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while (peek("OPER") && (new string[] { "==", "!=", "<=", ">=", "<", ">" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm2();

                buffer = left;
                left = new CommonNode("CMP", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == "CMP")
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением логики в арифметике!");
            }

            return left;
        }

        public CommonNode parseTerm2()
        {
            CommonNode buffer;
            CommonNode left = parseTerm3(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek("OPER") && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while (peek("OPER") && (new string[] { "+", "-" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm3();

                buffer = left;
                left = new CommonNode("BINOPER", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == "BINOPER")
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением арифметики в логике!");
            }

            return left;
        }
        public CommonNode parseTerm3()
        {
            CommonNode buffer;
            CommonNode left = parsePar(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek("OPER") && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while (peek("OPER") && (new string[] { "*", "/", "%" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parsePar();

                buffer = left;
                left = new CommonNode("BINOPER", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == "BINOPER")
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением арифметики в логике!");
            }

            return left;
        }

        /*public CommonNode parseFormula()
        {
            CommonNode buffer;
            CommonNode left = parsePar(); // token 1
            Token operatpor = null;          // token 2
            if (peek("OPER") && (new string[] { "+", "-", "*", "/" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parsePar();
                buffer = left;
                if (left.token.type.type == "OPER" && TokenTypeList.permissionOper[left.token.value] < TokenTypeList.permissionOper[operatpor.value])
                {
                    left = new CommonNode("BINOPER", operatpor);
                    left.childs.Add(buffer);
                    left.childs.Add(right);
                }
                else
                {
                    left = new CommonNode("BINOPER", operatpor);
                    left.childs.Add(buffer);
                    left.childs.Add(right);
                }
                if (peek("OPER") && (new string[] { "+", "-", "*", "/" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }

            return left;
        }*/
        public CommonNode parseVarWTypeSignature()
        {
            expect("LPAR"); skip();
            if (peek("RPAR"))
            {
                skip();
                return new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos].pos));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos].pos));
            while (true)
            {
                expect("VAR"); CommonNode typeNode = new CommonNode("TYPE", take());
                CommonNode declarator = parseDeclarator();
                if (declarator != null) typeNode.childs.Add(declarator);
                if (peek("OPER") && tokens[pos].value == "*")
                {
                    skip();
                    typeNode.type = "INDICATOR";
                }
                CommonNode declaratorPart = parseDeclarator();
                if (declaratorPart != null) typeNode.childs.Add(declaratorPart);
                expect("VAR"); CommonNode varNode = new CommonNode("VAR", take());
                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);
                root.childs.Add(varNode);
                if (!peek("PS"))
                {
                    expect("RPAR"); skip();
                    return root;
                }
                skip();
            }
        }
        public CommonNode parseVarSignature(string leftType = "LPAR", string type = "VAR")
        {
            string rightType = TokenTypeList.rightPar[leftType];
            expect(leftType); skip();
            if (peek(rightType))
            {
                skip();
                return new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos].pos));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos].pos));
            while (true)
            {
                expect("VAR"); CommonNode varNode = new CommonNode(type, take());
                root.childs.Add(varNode);
                if (!peek("PS"))
                {
                    expect(rightType); skip();
                    return root;
                }
                skip();
            }
        }
        public CommonNode parseFormulaSignature(string leftType = "LPAR")
        {
            string rightType = TokenTypeList.rightPar[leftType];
            expect(leftType); skip();
            if (peek(rightType))
            {
                skip();
                return new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos - 1].pos));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", tokens[pos - 1].pos));
            while (true)
            {
                CommonNode formulaNode = parseFormula();
                root.childs.Add(formulaNode);
                if (!peek("PS"))
                {
                    expect(rightType); skip();
                    return root;
                }
                skip();
            }
        }
        public CommonNode parseIfSignature()
        {
            if (peek("LPAR")) { expect("LPAR"); skip(); }
            if (peek("RPAR"))
            {
                skip();
                return new CommonNode("CMP", new Token(null, "true", tokens[pos - 1].pos));
            }
            CommonNode left = parseFormula();
            if (peek("RPAR")) { expect("RPAR"); skip(); }
            left.type = "CMP";
            return left;
        }
        public CommonNode parseIfSignatureWOther()
        {
            CommonNode buffer;
            CommonNode left = parseFormula(); // token 1
            Token operatpor = null;          // token 2
            if (peek("OPER") && (new string[] { "==", "!=", "<=", ">=", "<", ">", "&&", "||" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parseFormula();
                buffer = left;
                left = new CommonNode("CMP", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                if (peek("OPER") && (new string[] { "==", "!=", "<=", ">=", "<", ">", "&&", "||" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }
            if (sem || peek("SEM")) { expect("SEM"); skip(); }

            return left;
        }

        public CommonNode parseBody()
        {
            if (!peek("LFIG") && !peek("SEM"))
            {
                CommonNode node = new CommonNode("BODY", new Token(null, "{}", tokens[pos].pos));
                node.childs.Add(parse());
                return node;
            }
            expect(new string[] { "LFIG", "SEM" });
            if (peek("SEM"))
            {
                skip();
                return new CommonNode("BODY", new Token(null, "{}", tokens[pos - 1].pos));
            }
            skip();
            if (peek("RFIG"))
            {
                skip();
                return new CommonNode("BODY", new Token(null, "{}", tokens[pos - 1].pos));
            }
            //skip();

            CommonNode root = new CommonNode("BODY", new Token(null, "{}", tokens[pos].pos));

            while (true)
            {
                CommonNode node = parse();
                root.childs.Add(node);
                if (peek("RFIG"))
                {
                    skip();
                    return root;
                }
            }
        }

        public CommonNode parseStack(string type)
        {
            expect("LFIG");
            CommonNode stackNode = new CommonNode("STACK", take());

            while (peek(type))
            {
                Token varToken = take();
                while (peek("TS"))
                {
                    skip();
                    varToken.value += "." + take().value;
                    if (tokens[pos].type.type != "TS")
                        break;
                }
                stackNode.childs.Add(new CommonNode(type, varToken));
                if (tokens[pos].value == ":")
                {
                    skip(); expect("VAR");
                    CommonNode typeNode = new CommonNode("TYPE", take());
                    stackNode.childs[stackNode.childs.Count-1].childs.Add(typeNode);
                }
                if (peek("PS")) skip();
            }

            expect("RFIG"); skip();
            return stackNode;
        }
        public CommonNode parseEnumStack(string type)
        {
            expect("LFIG");
            CommonNode stackNode = new CommonNode("BODY", take());

            while (peek(type))
            {
                Token varToken = take();
                while (peek("TS"))
                {
                    skip();
                    varToken.value += "." + take().value;
                    if (tokens[pos].type.type != "TS")
                        break;
                }
                stackNode.childs.Add(new CommonNode(type, varToken));
                if (tokens[pos].value == "=")
                {
                    skip(); expect("NUMBER");
                    CommonNode typeNode = new CommonNode("NUMBER", take());
                    stackNode.childs[stackNode.childs.Count - 1].childs.Add(typeNode);
                }
                if (peek("PS")) skip();
            }

            expect("RFIG"); skip();
            return stackNode;
        }

        public CommonNode parseDeclarator()
        {
            if (peek("OPER") && tokens[pos].value == "@")
            {
                CommonNode declarotivePart = new CommonNode("DECLARATOR", take());

                while (peek("VAR"))
                {
                    CommonNode type = new CommonNode("TYPE", take());
                    if (peek("OPER") && tokens[pos].value == "*")
                    {
                        skip();
                        type.type = "INDICATOR";
                    }
                    CommonNode declar = parseDeclarator(); // Point<Point<int32[]>>
                    if (declar != null) type.childs.Add(declar);

                    declarotivePart.childs.Add(type);

                    if (!peek("PS")) break;
                    else { skip(); expect("OPER"); skip(); }//declarotivePart.childs.Add(parseDeclarator()); break; }
                }

                return declarotivePart;
            }
            if (peek("OPER") && tokens[pos].value == "<")
            {
                CommonNode declarotivePart = new CommonNode("DECLARATOR", take());

                while (peek("VAR"))
                {
                    CommonNode type = new CommonNode("TYPE", take());
                    if (peek("OPER") && tokens[pos].value == "*")
                    {
                        skip();
                        type.type = "INDICATOR";
                    }
                    CommonNode declar = parseDeclarator(); // Point<Point<int32[]>>
                    if (declar != null) type.childs.Add(declar);

                    declarotivePart.childs.Add(type);

                    if (!peek("PS")) break;
                    else skip();
                }
                expect("OPER"); if (tokens[pos].value != ">") Syntax.SyntaxError("Ожидался Токен: >", take());
                skip();

                return declarotivePart;
            }
            return null;
        }

        public CommonNode parseType(CommonNode typeNode)
        {
            if (peek("OPER") && tokens[pos].value == "*")
            {
                CommonNode indicator = new CommonNode("INDICATOR", take());
                typeNode.type = "INDICATOR";
            }

            if (peek("OPER") && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                typeNode.childs.Add(parseDeclarator());

                return parseType(typeNode);
            }

            expect("VAR");
            CommonNode varNode = new CommonNode("VAR", take());
            varNode.childs.Add(typeNode);

            varNode = tryParseVarPath(varNode);

            if (peek("SEM"))
            {
                skip();
                return varNode;
            }

            if (peek("PREFIX") && tokens[pos].value == "?")
            {
                CommonNode initMemStaticObject = new CommonNode("ALLOCMEMSTATICOBJECT", take());
                initMemStaticObject.childs.Add(varNode); if (sem || peek("SEM")) { expect("SEM"); skip(); }
                //root.varTypes.Add(varNode.token.value, typeNode.token.value);
                return initMemStaticObject;
            }

            if (peek("OPER") && tokens[pos].value == "=")
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode("BINOPER", oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                //root.varTypes.Add(varNode.token.value, typeNode.token.value);
                //if (peek("SEM")) { expect("SEM"); skip(); }
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }

            if (peek("LPAR"))
            {
                if (varNode.childs[0].childs.Count > 0 && varNode.childs[0].type == "TYPE") Syntax.SyntaxError("После возвращаемого типа функции не может идти Декларотивный Кортеж!", varNode.childs[0].childs[0]);
                bool qsFlag = false;
                CommonNode child;
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM", "OPER", "VAR" }); if (tokens[pos].value == "qs") { skip(); qsFlag = true; }
                CommonNode declarator = parseDeclarator();
                CommonNode body = parseBody();
                varNode.token.value = NamespaceString + varNode.token.value;
                varNode.type = "FUNC";
                varNode.childs.Add(args);
                varNode.childs.Add(body);
                if (declarator != null) varNode.childs.Add(declarator);

                if (root.functionOver.ContainsKey(varNode.token.value) && root.functionOver[varNode.token.value].Count != 0)
                {
                    string keyTemp = varNode.token.value;
                    varNode.token.value += root.functionOver[varNode.token.value].Count;
                    root.functionOver[keyTemp].Add(varNode);
                }                 

                if (varNode.childs[0].token.value == "void")
                    root.resualtFunc.Add(varNode.token.value, null);
                else
                    root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);

                if (declarator != null) root.declarotivePatternsFunctions.Add(varNode.token.value, varNode);
                if (!root.functionOver.ContainsKey(varNode.token.value)) root.functionOver.Add(varNode.token.value, new List<CommonNode>() { varNode });
                if (qsFlag) root.qsFunction.Add(varNode.token.value);
                return varNode;
            }

            if (!sem) { return varNode; }
            Syntax.SyntaxError($"На позиции Токена:{pos} ожились токены OPER, SEM, LPAR, OPER! {tokens[pos].value} {tokens[pos].type.type}", tokens[pos]);
            return null;
        }
        public CommonNode parseVarOperation(CommonNode node = null)
        {
            CommonNode varNode;
            if (node == null)
                varNode = new CommonNode("VAR", take());
            else
                varNode = node;
            expect(new string[] { "OPER", "PREFIX", "SEM", "LPAR", "TS", "VAR", "LK" });

            varNode = tryParseVarPath(varNode);

            if (peek("VAR") && varNode.token.value == "qs")
            {
                varNode = new CommonNode("CALL", varNode.token);
                varNode = parseCall(varNode); if (sem || peek("SEM")) { expect("SEM"); skip(); }
                root.qsFunction.Add(varNode.token.value);
                return varNode;
            }

            if (peek("OPER") && tokens[pos].value == ":")
            {
                skip();
                varNode.type = "TAG";
                return varNode;
            }

            if (peek("OPER") && tokens[pos].value == "*")
            {
                //CommonNode indicator = new CommonNode("INDICATOR", take());
                //varNode.childs.Add(indicator);
                //skip();
                CommonNode typeNode = parseType(varNode);
                //typeNode.type = "INDICATOR";
                //typeNode.childs[0].type = "INDICATOR";
                return typeNode;
            }

            if (peek("LK"))
            {
                CommonNode offsetNode = new CommonNode("OFFSET", take());
                offsetNode.childs.Add(parseFormula()); expect("RK"); skip();
                varNode.childs.Add(offsetNode);
                //varNode = offsetNode;
            }

            if (peek("OPER") && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                int ps = tryParseDeclarator();
                if (tokens[ps].type.type == "LPAR" && !peekFig(ps))
                {
                    // CALL
                    varNode = new CommonNode("CALL", varNode.token);
                    varNode = parseCall(varNode); if (sem || peek("SEM")) { expect("SEM"); skip(); }
                    return varNode;
                }
            }

            if (peek("OPER") && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                CommonNode typeNode = new CommonNode("TYPE", varNode.token);
                typeNode.childs.Add(parseDeclarator());

                return parseType(typeNode);
            }
            if (peek("VAR"))
            {
                CommonNode typeNode = new CommonNode("TYPE", varNode.token);

                return parseType(typeNode);
            }
            if (peek("SEM"))
            {
                skip();
                return varNode;
            }

            if (peek("OPER") && new string[] { "=", "+=", "-=", "*=", "/=", "%=" }.Contains(tokens[pos].value))
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode("BINOPER", oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);

                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }
            if (peek("PREFIX") && (tokens[pos].value == "++" || tokens[pos].value == "--"))
            {
                Token oper = take();
                CommonNode operNode = new CommonNode("PREUNAROPER", oper);
                operNode.childs.Add(varNode); if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }

            if (peek("LPAR"))
            {
                varNode = parseCall(varNode);
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return varNode;
            }




            //if (peek("OPER") && new string[] { "-=", "+=", "*=", "/=" }.Contains(tokens[pos].value))
            SyntaxError($"Невозможный Токен:{tokens[pos].value}");
            return null;
        }
       
        public CommonNode parseCall(CommonNode varNode)
        {
            varNode = tryParseVarPath(varNode);
            CommonNode declarator = parseDeclarator();
            if (!peek("LPAR")) return varNode;
            CommonNode args = parseFormulaSignature();
            varNode.type = "CALL";
            if (declarator != null) varNode.childs.Add(declarator);
            varNode.childs.Add(args);
            if (peek("TS"))
            {
                skip(); expect("VAR");
                CommonNode var = new CommonNode("VAR", take());
                var = tryParseVarPath(var);
                var.type = "REFVAR";
                var.childs.Add(varNode);
                varNode = var;
            }
            //if (declarator != null && !root.declarotivePatternsFunctions.Keys.Contains(varNode.token.value)) root.declarotivePatternsFunctions.Add(varNode.token.value, varNode);

            return varNode;

            SyntaxError();
            return null;
        }
        public CommonNode parseInline()
        {

            expect("INLINE"); skip(); expect("VAR");
            CommonNode nameNode = new CommonNode("INLINE", take());
            while (peek("TS"))
            {
                skip();
                nameNode.token.value += "." + take().value;
                if (peek("LPAR"))
                    break;
            }
            nameNode.type = "INLINE";
            //expect("LPAR");


            if (peek("LPAR"))
            {
                //CommonNode args = parseFormula();
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM" });
                CommonNode body = parseBody();
                nameNode.childs.Add(args);
                nameNode.childs.Add(body);
                //expect("SEM"); skip();
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return nameNode;
            }

            SyntaxError();
            return null;
        }
        public CommonNode parseLpar()
        {
            int i = tokens.Count - pos;
            if (i < 2) SyntaxError();
            if (peek("LPAR") && (tokens[pos + 2].type.type == "PS" || tokens[pos + 2].type.type == "RPAR"))
            {
                CommonNode types = parseVarSignature("LPAR", "TYPE");
                types.type = "TYPEFORMULA";
                return parseType(types);
            }
            if (peek("LPAR"))
            {
                CommonNode signature = parseVarWTypeSignature();

                expect(new string[] { "OPER", "SEM" });
                if (peek("OPER") && tokens[pos].value == "=")
                {

                    Token oper = take();
                    CommonNode rightOperand = parseFormula();
                    CommonNode operNode = new CommonNode("BINOPER", oper);
                    operNode.childs.Add(signature);
                    operNode.childs.Add(rightOperand);
                    if (sem || peek("SEM")) { expect("SEM"); skip(); }
                    return operNode;
                }
                if (peek("SEM"))
                {
                    return signature;
                }

            }

            SyntaxError($"Слушай на позиции:{pos} ты поставил Токен:{tokens[pos].value} ! Но он не подходит!!!!!!!!!!");
            return null;
        }

        public CommonNode parseIfStrurct()
        {
            expect(new string[] { "IF", "ELSEIF", "ELSE" });
            Token ifToken = take();
            CommonNode ifNode = new CommonNode(ifToken.type.type, ifToken);
            CommonNode ifBodyNode;
            if (ifNode.type == "ELSE")
            {
                ifBodyNode = parseBody();
                ifNode.childs.Add(ifBodyNode);
                return ifNode;
            }
            CommonNode ifSignatureNode = parseIfSignature();
            ifBodyNode = parseBody();
            ifNode.childs.Add(ifSignatureNode);
            ifNode.childs.Add(ifBodyNode);
            if (peek("ELSE") || peek("ELSEIF"))
            {
                CommonNode ifElsesNode = new CommonNode("ELSES", new Token(null, "elses", tokens[pos].pos));
                ifElsesNode.childs.Add(parseIfStrurct());
                ifNode.childs.Add(ifElsesNode);
            }
            return ifNode;
        }

        public CommonNode parseQueueControlOperator()
        {
            expect(new string[] { "RETURN", "BREAK", "CONTINUE", "JMP" });
            //Token operToken = take();

            if (peek("RETURN"))
            {
                CommonNode operNode = new CommonNode("RETURN", take());
                CommonNode rightNode;
                if (peek("SEM") || tokens[pos].value == "void")
                {
                    skip();
                    return operNode;
                }
                else if (peek("LPAR"))
                    rightNode = parseFormulaSignature();
                else
                    rightNode = parseFormula();
                //rightNode.type = "VARFORMULA";
                operNode.childs.Add(rightNode);
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }
            if (peek("BREAK"))
            {
                CommonNode operNode = new CommonNode("BREAK", take());
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }
            if (peek("CONTINUE"))
            {
                CommonNode operNode = new CommonNode("CONTINUE", take());
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return operNode;
            }
            if (peek("JMP"))
            {
                CommonNode jmpNode = new CommonNode("JMP", take()); expect("VAR");
                jmpNode.childs.Add(new CommonNode("TAG", take()));
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return jmpNode;
            }

            SyntaxError();
            return null;
        }

        public CommonNode parseCycle()
        {
            expect(new string[] { "FOR", "WHILE", "ITER", "ENUMERATOR", "REPT" });
            Token cycleToken = take();
            CommonNode cycleNode = new CommonNode(cycleToken.type.type, cycleToken);


            if (cycleNode.type == "FOR")
            {
                // for (int32 i = 0; i < 10; i++)
                if (peek("LPAR")) { expect("LPAR"); skip(); }
                //expect("VAR"); Token type = take();
                CommonNode varNode = parse();
                CommonNode initNode = varNode;//new CommonNode(varNode.type, varNode.token);
                //initNode.childs.Add(new CommonNode("TYPE", type));
                CommonNode cmpNode = parseIfSignatureWOther();
                expect("VAR");
                CommonNode formulaNode = parseFormula();
                CommonNode stepNode = new CommonNode("STEP", formulaNode.token);
                stepNode.childs.Add(formulaNode);
                if (peek("RPAR")) { expect("RPAR"); skip(); }
                CommonNode bodyNode = parseBody();
                cycleNode.childs.Add(initNode);
                cycleNode.childs.Add(cmpNode);
                cycleNode.childs.Add(stepNode);
                cycleNode.childs.Add(bodyNode);
                return cycleNode;
            }

            if (cycleNode.type == "WHILE")
            {
                //expect("LPAR");
                CommonNode signature = parseIfSignature();
                CommonNode body = parseBody();
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(body);
                return cycleNode;
            }

            if (cycleNode.type == "ITER")
            {
                if (peek("LPAR")) { expect("LPAR"); skip(); }
                CommonNode signature = parseFormula(); if (peek("RPAR")) { expect("RPAR"); skip(); }
                CommonNode body = parseBody();
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(body);
                return cycleNode;
            }

            if (cycleNode.type == "ENUMERATOR")
            {
                if (peek("LPAR")) { expect("LPAR"); skip(); }
                expect("VAR");
                CommonNode typeNode = new CommonNode("TYPE", take()); expect("VAR");
                CommonNode varNode = new CommonNode("VAR", take()); varNode.childs.Add(typeNode);
                expect("PS"); skip();
                CommonNode signature = parseFormula();
                if (peek("RPAR")) { expect("RPAR"); skip(); }

                cycleNode.childs.Add(varNode);
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(parseBody());
                return cycleNode;
            }

            if (cycleNode.type == "REPT")
            {
                if (peek("LPAR")) { skip(); }
                expect("NUMBER");
                CommonNode signature = new CommonNode("NUMBER", take());
                if (peek("RPAR")) { skip(); }

                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(parseBody());
                return cycleNode;
            }

            SyntaxError();
            return null;
        }

        public CommonNode parseStructChildren(string nameStruct)
        {
            //
            //expect(new string[] { "VAR", "OPER" });
            //SyntaxError($"Ну типо ты в структуре данных на позиции:{pos} используешь первым токеном оператором не того типа!!!");

            //
            if (peek("VAR") && (tokens[pos + 1].type.type == "VAR" || tokens[pos + 1].value == "*"))
            {
                CommonNode typeNode = new CommonNode("TYPE", take()); if (tokens[pos].value == "*") { typeNode.type = "INDICATOR"; skip(); }
                CommonNode declarationPart = parseDeclarator();
                if (declarationPart != null) typeNode.childs.Add(declarationPart);
                CommonNode varNode = new CommonNode("VAR", take());

                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);

                if (peek("LPAR"))
                {
                    CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM", "OPER" });
                    CommonNode declarator = parseDeclarator();
                    CommonNode body = parseBody();
                    varNode.type = "FUNC";
                    varNode.childs.Add(args);
                    varNode.childs.Add(body);
                }
                else if (sem || peek("SEM")) { expect("SEM"); skip(); }

                return varNode;
            }
            if (peek("VAR") && tokens[pos].value == nameStruct)
            {
                Token nameToken = take();
                CommonNode constructor = new CommonNode("CONSTRUCTOR", nameToken);
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM" });
                CommonNode body = parseBody();
                constructor.childs.Add(new CommonNode("TYPE", new Token(nameToken.type, "void", nameToken.pos)));
                constructor.childs.Add(args);
                constructor.childs.Add(body);
                //SyntaxError($"На позиции:{pos} странный токен не подходящий для объявления члена структуре данных!");
                return constructor;
            }
            if (peek("OPER") && tokens[pos].value == "~")
            {
                skip();
                Token destructorToken = take();
                CommonNode destructorNode = new CommonNode("DESTRUCTOR", destructorToken);
                CommonNode body = parseBody();
                destructorNode.childs.Add(new CommonNode("TYPE", new Token(destructorToken.type, "void", destructorToken.pos)));
                destructorNode.childs.Add(new CommonNode("SIGNATURE", new Token(destructorToken.type, "()", destructorToken.pos)));
                destructorNode.childs.Add(body);
                return destructorNode;
            }
            //

            SyntaxError("Неправильное объявление члена структуры данных");
            return null;
        }

        public CommonNode parseStrurct()
        {
            expect(new string[] { "STRUCT", "CLASS" }); Token typeStructToken = take();
            expect("VAR"); Token nameToken = take();
            CommonNode structNode = new CommonNode(typeStructToken.type.type, nameToken);
            CommonNode declarotivePart = null;
            declarotivePart = parseDeclarator();
            
            /*if (peek("OPER"))
            {
                skip(); expect("VAR"); CommonNode varNode = new CommonNode("VAR", take());
                CommonNode declarotivePartSecond = parseDeclarator();
                if (declarotivePartSecond != null) varNode.childs.Add(declarotivePartSecond);
                root.parentsStructs.Add(structNode.token.value, varNode);
            }*/

            if (declarotivePart != null) structNode.childs.Add(declarotivePart);
            expect("LFIG"); skip(); if (peek("RFIG")) { skip(); return structNode; }

            if (peek("MODIFIER") && typeStructToken.value == "struct")
            {
                Syntax.SyntaxError("В структурах запрещенно использование модификаторов доступа!", structNode);
            }
            else if (!peek("MODIFIER") && typeStructToken.value == "struct")
            {
                while (true)
                {
                    CommonNode varNode = parseStructChildren(nameToken.value);
                    if (varNode == null)
                        varNode = parse(); if (!(new string[] { "VAR" }.Contains(varNode.type))) SyntaxError($"Ты чё в структуре Узел Типа:{varNode.type} не может находиться!");
                    structNode.childs.Add(varNode);


                    if (peek("RFIG"))
                    {
                        skip();
                        if (declarotivePart != null) root.declarotivePatternsStruct.Add(nameToken.value, structNode);
                        if (declarotivePart != null) return null;
                        return structNode;
                    }
                    else if (peek("MODIFIER"))
                    {
                        Syntax.SyntaxError("В структурах запрещенно использование модификаторов доступа!", structNode);
                    }
                }
            }

            //
            if (peek("MODIFIER"))
            {
                Syntax.SyntaxError("В классах запрещенно использование модификаторов доступа!", structNode);
            }
            else
            {
                while (true)
                {
                    /*CommonNode varNode = parse();
                    if (!(new string[] { "VAR", "FUNC" }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Токен Типа:{varNode.type} не может первым находиться");
                    modifierNode.childs.Add(varNode);*/
                    CommonNode varNode = parseStructChildren(nameToken.value);
                    if (varNode == null)
                        varNode = parse();
                    if (!(new string[] { "VAR", "FUNC", "CONSTRUCTOR", "DESTRUCTOR" }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Узел Типа:{varNode.type} не может первым находиться");
                    //modifierNode.childs.Add(varNode);
                    structNode.childs.Add(varNode);

                    if (peek("RFIG"))
                    {
                        skip();
                        //structNode.childs.Add(modifierNode);

                        if (declarotivePart != null) root.declarativeClassNames.Add(nameToken.value);
                        return structNode;
                    }
                }
            }
            //

            SyntaxError();
            return null;
        }

        public CommonNode parseUsing()
        {
            expect("USING");
            CommonNode usingNode = new CommonNode("USING", take());

            if (peek("STRING"))
            {
                expect("STRING"); usingNode.childs.Add(new CommonNode("NAME", take()));
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
            }
            else if (peek("VAR"))
            {
                usingNode.childs.Add(new CommonNode("NAME", take()));
                while (peek("TS"))
                {
                    skip();
                    usingNode.childs[0].token.value += "." + take().value;
                    if (tokens[pos].type.type != "TS")
                        break;
                }
                if (peek("INLINE")) usingNode.childs.Add(new CommonNode("INLINE", take()));
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
            }
            else if (peek("INLINE"))
            {
                //usingNode.childs.Add(new CommonNode("INLINE", take()));
                skip();
                CommonNode stack = parseStack("VAR");
                foreach (var child in stack.childs)
                {
                    root.inlineNames.Add(child.token.value);
                }
                //expect("SEM"); skip();
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return null;
            }
            else if (peek("NATIVE"))
            {
                //usingNode.childs.Add(new CommonNode("INLINE", take()));
                //CommonNode stack = parseStack("VAR");
                skip();
                expect("LFIG"); skip();

                while (true)
                {
                    CommonNode typeNode = new CommonNode("TYPE", take());
                    CommonNode varNode = new CommonNode("FUNC", take());
                    varNode = tryParseVarPath(varNode);
                    varNode.childs.Add(typeNode);

                    //if (varNode.childs[0].childs.Count > 0 && varNode.childs[0].type == "TYPE") Syntax.SyntaxError("После возвращаемого типа функции не может идти Декларотивный Кортеж!", varNode.childs[0].childs[0]);
                    CommonNode args = parseVarWTypeSignature();

                    if (varNode.childs[0].token.value == "void")
                        root.resualtFunc.Add(varNode.token.value, null);
                    else
                        root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);
                    root.typesArgsFunc.Add(varNode.token.value, args);

                    if (peek("RFIG")) break;
                    else if (!peek("PS")) break;
                    else if (peek("PS")) skip();
                }
                expect("RFIG"); skip();
                //expect("SEM"); skip();
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return null;
            }
            else if (peek("SECTION"))
            {
                skip(); expect("LFIG"); skip();
                expect("ASM");
                root.sectionNodes.Add(new CommonNode("SECTION", take()));
                expect("RFIG"); skip();
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                return null;
            }

            return usingNode;
        }

        public CommonNode parseExtern()
        {
            expect(new string[] { "EXTERN", "EXTERNLIBRARY", "EXTERNFUNC" });
            CommonNode externNode = new CommonNode(tokens[pos].type.type, take());

            if (externNode.type == "EXTERN")
            {
                List<string> listFuncNode = new List<string> ();
                string libraryString = string.Empty;
                while (true)
                {
                    if (peek("EXTERN"))
                    {
                        skip(); expect("FROM"); skip();
                        expect("VAR");
                        libraryString = take().value;
                        break;
                    }
                    else listFuncNode.Add(parseExtern().token.value);
                }
                if (!root.externFuncs.ContainsKey(libraryString)) root.externFuncs.Add(libraryString, listFuncNode);
                else root.externFuncs[libraryString].AddRange(listFuncNode);
                return null;
            }
            if (externNode.type == "EXTERNLIBRARY")
            {
                expect("VAR");
                CommonNode libraryNode = new CommonNode("VAR", take()); expect("STRING");
                if (!root.externLibrarys.ContainsKey(libraryNode.token.value)) root.externLibrarys.Add(libraryNode.token.value, take().value);
                if (!root.externFuncs.ContainsKey(libraryNode.token.value)) root.externFuncs.Add(libraryNode.token.value, new List<string>());
                return null;
            }
            if (externNode.type == "EXTERNFUNC")
            {
                expect("VAR");
                CommonNode typeNode = new CommonNode("TYPE", take());
                expect("VAR");
                CommonNode varNode = new CommonNode("FUNC", take());
                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);


                CommonNode args = parseVarWTypeSignature();

                if (varNode.childs[0].token.value == "void")
                    root.resualtFunc.Add(varNode.token.value, null);
                else
                    root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);
                root.typesArgsFunc.Add(varNode.token.value, args);
                //root.resualtFunc.Add(varNode.token.value, typeNode);
                if (!peek("FROM")) return varNode;

                expect("FROM"); skip(); expect("VAR");
                string libraryString = take().value;
                if (!root.externFuncs.ContainsKey(libraryString)) root.externFuncs.Add(libraryString, new List<string>() { varNode.token.value });
                else root.externFuncs[libraryString].Add(varNode.token.value);

                return varNode;
            }
            return null;
        }

        public CommonNode parseConst()
        {
            expect("CONST"); skip();  expect("VAR");
            CommonNode constNode = new CommonNode("VAR", take());
            expect("OPER"); if (tokens[pos].value != "=") SyntaxError($"На Позиции:{pos} после константы ожидался оператор =");
            skip();
            CommonNode valueNode = parseFormula();
            if (!root.consts.ContainsKey(constNode.token.value)) root.consts.Add(constNode.token.value, valueNode);
            return null;
        }

        public CommonNode parseTypeif()
        {
            expect("TYPEIF");
            CommonNode typeifNode = new CommonNode("TYPEIF", take()); expect("VAR");
            typeifNode.childs.Add(new CommonNode("TYPE", take())); expect("VAR");
            typeifNode.childs.Add(new CommonNode("TYPE", take()));
            typeifNode.childs.Add(parseBody());
            return typeifNode;
        }

        public CommonNode parseNamespace()
        {
            skip(); expect("VAR");
            Token namespaceToken = take();
            NamespaceString = namespaceToken.value + ".";
            CommonNode bodyNode = parseBody();
            CommonNode recurse (CommonNode root)
            {
                if (root.type != "VAR")
                {
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        root.childs[i] = recurse(root.childs[i]);
                    }
                    return root;
                }
                root.token.value = namespaceToken.value + "." + root.token.value;
                return root;
            }
            NamespaceString = "";
            return recurse(bodyNode);
        }

        public CommonNode parseEnum()
        {
            skip(); expect("VAR");
            Token enumToken = take();
            CommonNode consts = parseEnumStack("VAR");
            int index = 0;
            Compiler.types.Add(enumToken.value, Compiler.types["long"]);
            Compiler.typesarg.Add(enumToken.value, Compiler.typesarg["long"]);
            Compiler.aligns.Add(enumToken.value, Compiler.aligns["long"]);
            foreach (CommonNode cnst in consts.childs)
            {
                if (cnst.childs.Count > 0)
                {
                    if (!root.consts.ContainsKey(cnst.token.value)) root.consts.Add(enumToken.value + "." + cnst.token.value, cnst.childs[0]);
                } else
                {
                    if (!root.consts.ContainsKey(cnst.token.value))
                        root.consts.Add(enumToken.value + "." + cnst.token.value,
                        new CommonNode("NUMBER", new Token(TokenTypeList.tokenTypes["NUMBER"], index.ToString(), cnst.token.pos)));
                }
                index++;
            }
            return null;
        }

        public CommonNode parseOperator()
        {
            skip(); expect("VAR");
            CommonNode typeNode = new CommonNode("TYPE", take());
            if (tokens[pos].value == "*") { skip(); typeNode.type = "INDICATOR"; }
            CommonNode varNode = new CommonNode("OPER", take());
            string operatorChar = varNode.token.value;
            switch (varNode.token.value)
            {
                case "+": varNode.token.value = "PLUS" + typeNode.token.value;        break;
                case "-": varNode.token.value = "MINUS" + typeNode.token.value;       break;
                case "*": varNode.token.value = "MUL" + typeNode.token.value;         break;
                case "/": varNode.token.value = "DIV" + typeNode.token.value;         break;
                case "%": varNode.token.value = "DDIV" + typeNode.token.value;        break;
                case "+=": varNode.token.value = "PLUSASSIGN" + typeNode.token.value; break;
                case "-=": varNode.token.value = "MINUSASSIGN" + typeNode.token.value;break;
                case "*=": varNode.token.value = "MULASSIGN" + typeNode.token.value;  break;
                case "/=": varNode.token.value = "DIVASSIGN" + typeNode.token.value;  break;
                case "%=": varNode.token.value = "DDIVASSIGN" + typeNode.token.value; break;
            }
            if (root.operatorFunctions.ContainsValue(varNode.token.value)) varNode.token.value += root.operatorFunctions.Count;

            bool qsFlag = false;
            CommonNode child;
            CommonNode args = parseVarWTypeSignature();
            if (args.childs.Count != 2) Syntax.SyntaxError("Невозможное количество аргументов оператора!", args);
            expect(new string[] { "LFIG", "SEM", "OPER", "VAR" }); if (tokens[pos].value == "qs") { skip(); qsFlag = true; }
            CommonNode body = parseBody();
            varNode.token.value = NamespaceString + varNode.token.value;
            varNode.type = "FUNC";
            varNode.childs.Add(typeNode);
            varNode.childs.Add(args);
            varNode.childs.Add(body);

            if (varNode.childs[0].token.value == "void")
                root.resualtFunc.Add(varNode.token.value, null);
            else
                root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);

            if (qsFlag) root.qsFunction.Add(varNode.token.value);
            root.operatorFunctions.Add((operatorChar, args.childs[0].childs[0].token.value, args.childs[1].childs[0].token.value), varNode.token.value);
            return varNode;
        }

        public ProgramNode parseCode()
        {
            root = new ProgramNode("ROOT", new Token(null, "ROOT", -999));
            while (pos < tokens.Count)
            {
                if (pos >= tokens.Count) break;

                CommonNode node = parse();
                if (node == null) continue;
                if (sem || peek("SEM")) { expect("SEM"); skip(); }
                root.childs.Add(node);
                //Program.PrintAST(node, 0);
            }
            return root;
        }

        public CommonNode parse() // 22 keywords
        {
            if (peek("VAR"))
            {
                return parseVarOperation();
            }
            if (peek("IF") || peek("ELSEIF") || peek("ELSE"))
            {
                return parseIfStrurct();
            }
            if (peek("RETURN") || peek("BREAK") || peek("CONTINUE") || peek("JMP"))
            {
                return parseQueueControlOperator();
            }
            if (peek("FOR") || peek("WHILE") || peek("ITER") || peek("ENUMERATOR") || peek("REPT"))
            {
                return parseCycle();
            }
            if (peek("LPAR"))
            {
                return parseLpar();
            }
            if (peek("STRUCT") || peek("CLASS"))
            {
                return parseStrurct();
            }
            if (peek("USING"))
            {
                return parseUsing();
            }
            if (peek("EXTERN") || peek("EXTERNLIBRARY") || peek("EXTERNFUNC"))
            {
                parseExtern();
                return null;
            }
            if (peek("ASM"))
            {
                return new CommonNode("ASM", take());
            }
            if (peek("CONST"))
            {
                return parseConst();
            }
            if (peek("INLINE"))
            {
                return parseInline();
            }
            if (peek("TYPEIF"))
            {
                return parseTypeif();
            }
            if (peek("NAMESPACE"))
            {
                return parseNamespace();
            }
            if (peek("ENUM"))
            {
                return parseEnum();
            }
            if (peek("OPERATOR"))
            {
                return parseOperator();
            }
            return null;
        }


        // ERROR
        public void SyntaxError(string text = "Хз какая синтаксическая ошибка! Или мне лень её описывать)))")
        {
            Syntax.SyntaxError(text, tokens[pos]);
            Console.WriteLine("Ну ладно попытаюсь скомпилировать...(");
        }
    }
}
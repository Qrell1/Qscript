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


        public Parser(List<Token> _tokens)
        {
            tokens = _tokens;
        }

        /// <summary>
        /// Мы проверяем токен на нужный тип на текущей позиции, но не переносим коретку
        /// </summary>
        /// <param name=NT.TYPE></param>
        /// <returns></returns>
        private bool peek(TT type)
        {
            if (pos < tokens.Count)
            {
                Token token = tokens[pos];
                if (token.type == type)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Мы берём токен на позиции и перемещаем корретку
        /// </summary>
        private Token take()
        {

            Token token = tokens[pos];
            pos++;
            return token;
        }
        /// <summary>
        /// Сентаксическая функциия которая явно указывает что тут должен быть токен нужного типа!
        /// </summary>
        /// <param name=NT.TYPE></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool expect(TT type)
        {
            if (tokens[pos].type == type)
            {
                return true;
            }
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{type}", tokens[pos].pos);
            return false;
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{type}");
        }
        /// <summary>
        /// Сентаксическая функциия которая явно указывает что тут должен быть токен нужного типа и значение!
        /// </summary>
        /// <param name=NT.TYPE></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool expect(TT type, string value)
        {
            if (tokens[pos].type == type && tokens[pos].value == value)
            {
                return true;
            }
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{type} с значением: {value}", tokens[pos].pos);
            return false;
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{type}");
        }
        /// <summary>
        /// Сентаксическая функциия которая явно указывает что тут должен быть токен нужного типа!
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool expect(TT[] types)
        {
            if (types.Contains(tokens[pos].type))
            {
                return true;
            }
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{types}");
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{types}", tokens[pos]);
            return false;
        }
        private void skip()
        {
            pos++;
        }

        private NT getNodeType (TT type)
        {
            return (NT)type;
        }

        /// <summary>
        /// При вызове pos должен указывать на "<" выходе он будет указывать на то что после ">"
        /// </summary>
        private int tryParseDeclarator(int offset=1)
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
                        else if (tokens[ps].type == TT.VAR && (tokens[ps+1].value == "," || tokens[ps+1].value == "*" || tokens[ps+1].value == "@")) { ps++; }
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
        private CommonNode tryParseVarPath(CommonNode varNode)
        {
            while (peek(TT.TS) || (tokens[pos].value == ":" && tokens[pos+1].value == ":" && ++pos == pos))
            {
                //skip(); 
                varNode.token.value += take().value.Replace(":", ".") + take().value;
                //if (tokens[pos].type != TT.TS && (tokens[pos].value != ":" || tokens[pos + 1].value != ":")) break;
                //if (tokens[pos].value != ":" && tokens[pos+1].value != ":") break;
            }
            return varNode;
        }
        private bool peekFig(int _pos)
        {
            int ps = _pos;
            while (true)
            {
                if (tokens[ps].value == ")") break;
                else ps++;
            }
            if (tokens[ps+1].type == TT.LFIG) return true;
            else return false;
        }

        private CommonNode parsePar()
        {
            if (tokens[pos].value == "@"  && tokens[pos + 1].type == TT.VAR)
            {
                skip();
                CommonNode typeOper = new CommonNode(NT.TYPEOPER, take());
                CommonNode parNode = parsePar();
                typeOper.childs.Add(parNode);
                return typeOper;
            }
            if (tokens[pos].type == TT.LPAR && tokens[pos + 1].type == TT.VAR && tokens[pos + 2].type == TT.RPAR)
            {
                skip();
                CommonNode typeOper = new CommonNode(NT.TYPEOPER, take());
                skip();
                CommonNode parNode = parsePar();
                typeOper.childs.Add(parNode);
                return typeOper;
            }
            if (peek(TT.LPAR))
            {
                skip();
                CommonNode node = parseFormula();

                if (!peek(TT.RPAR))
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
        private CommonNode parseVariableOrNumberOrFunction()
        {
            Token token = take();
            if (token.type == TT.SEM) Syntax.SyntaxError("Мдамс получается ты тут накосячил. Честно я не знаю как.\n Но совет если при вызове функции не передаёшь аргументы всегда пиши ()!", new CommonNode(getNodeType(token.type), token));

            if (token.value == "-" && peek(TT.NUMBER))
            {
                Token number = take();
                number.value = token.value + number.value;
                return new CommonNode(NT.NUMBER, number);
            }
            else if (token.value == "-" && peek(TT.FLOAT))
            {
                Token floatn = take();
                floatn.value = token.value + floatn.value;
                return new CommonNode(NT.FLOAT, floatn);
            }
            else if (token.value == "-" && peek(TT.VAR))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = parseVariableOrNumberOrFunction();
                unarNode.childs.Add(node);
                return unarNode;
            }
            else if (token.value == "-" && peek(TT.LPAR))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = parseFormula();
                unarNode.childs.Add(node);
                return unarNode;
            }

            if (token.value == "!" && peek(TT.NUMBER))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = new CommonNode(NT.NUMBER, take());
                unarNode.childs.Add(node);
                return unarNode;
            }
            else if (token.value == "!" && peek(TT.FLOAT))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = new CommonNode(NT.FLOAT, take());
                unarNode.childs.Add(node);
                return unarNode;
            }
            else if (token.value == "!" && peek(TT.VAR))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = parseVariableOrNumberOrFunction();
                unarNode.childs.Add(node);
                return unarNode;
            }
            else if (token.value == "!" && peek(TT.LPAR))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token);
                CommonNode node = parseFormula();
                unarNode.childs.Add(node);
                return unarNode;
            }


            if (token.type == TT.PREFIX && (token.value == "++" || token.value == "--"))
            {
                CommonNode unarNode = new CommonNode(NT.PREUNAROPER, token); expect(TT.VAR);
                CommonNode node = new CommonNode(NT.VAR, take());
                node = tryParseVarPath(node);
                unarNode.childs.Add(node);
                return unarNode;
            }
            if (token.type == TT.PREFIX && token.value == "&")
            {
                CommonNode addr = new CommonNode(NT.ADDRESS, token);
                CommonNode node;
                if (tokens[pos].value == "(" && tokens[pos + 1].value == ")")
                {
                    skip(); skip();
                    node = new CommonNode(NT.CALLADDRESS, take());
                    node = tryParseVarPath(node);
                    node.childs.Add(new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", node.token.pos)));
                    addr.childs.Add(node);
                    return addr;
                }
                expect(TT.VAR);
                node = new CommonNode(NT.VAR, take());
                node = tryParseVarPath(node);
                //node = parseCall(node);
                addr.childs.Add(node);
                return addr;
            }
            if (token.type == TT.SIZEOF)
            {
                CommonNode sizeofNode = new CommonNode(NT.SIZEOF, token);
                expect(TT.TS); skip(); expect(TT.VAR);
                CommonNode node = new CommonNode(NT.VAR, take());
                node = tryParseVarPath(node);
                sizeofNode.childs.Add(node);
                return sizeofNode;
            }
            if (token.type == TT.TYPEOF)
            {
                CommonNode sizeofNode = new CommonNode(NT.TYPEOF, token);
                expect(TT.TS); skip(); expect(TT.VAR);
                CommonNode node = new CommonNode(NT.VAR, take());
                node = tryParseVarPath(node);
                sizeofNode.childs.Add(node);
                return sizeofNode;
            }
            if (token.type == TT.VAR)
            {
                CommonNode node = new CommonNode(NT.VAR, token);
                node = tryParseVarPath(node);

                if (tokens[pos].type == TT.LPAR)
                {
                    node.type = NT.CALL;
                    node = parseCall(node);
                    return node;
                }

                if (tokens[pos].value == "@")
                {
                    node = new CommonNode(NT.CALL, node.token);
                    node = parseCall(node);
                    return node;
                }

                if (tokens[pos].value == "<")
                {
                    int ps = tryParseDeclarator();
                    if (ps != -1 && tokens[ps].type == TT.LPAR)
                    {
                        node = new CommonNode(NT.CALL, node.token);
                        node = parseCall(node);
                        return node;
                    }
                }
                

                if (peek(TT.LK))
                {
                    CommonNode offsetNode;

                    while (peek(TT.LK))
                    {
                        offsetNode = new CommonNode(NT.OFFSET, take());
                        offsetNode.childs.Add(parseFormula()); expect(TT.RK); skip();
                        offsetNode.childs.Add(node);
                        node = offsetNode;
                    }
                }

                if (peek(TT.PREFIX) && (tokens[pos].value == "++" || tokens[pos].value == "--"))
                {
                    CommonNode unarNode = new CommonNode(NT.POSTUNAROPER, take());
                    unarNode.childs.Add(node);
                    return unarNode;
                }

                return node;
            }

            if (token.type == TT.LAMBDA) return parseLambda(token);
            if (token.type == TT.NUMBER) return new CommonNode(NT.NUMBER, token);
            if (token.type == TT.HEX)    return new CommonNode(NT.HEX, token);
            if (token.type == TT.STRING) return new CommonNode(NT.STRING, token);
            if (token.type == TT.CHAR)   return new CommonNode(NT.CHAR,   token);
            if (token.type == TT.CONST)  return new CommonNode(NT.CONST , token);
            if (token.type == TT.BOOL)   return new CommonNode(NT.BOOL ,  token);
            if (token.type == TT.FLOAT)  return new CommonNode(NT.FLOAT,  token);

            Syntax.SyntaxError($"Ошибка в парсинге формулы из-за Токена:{token.value}", token);
            return null;
        }
        private CommonNode parseLambda(Token lambdaToken)
        {
            CommonNode lambdaNode = new CommonNode(NT.LAMBDA, lambdaToken);

            if (peek(TT.LPAR))
            {
                CommonNode signatureCallNode = parseFormulaSignature();
                lambdaNode.childs.Add(signatureCallNode);
            }

            if (tokens[pos].value == "->")
            {
                CommonNode functionTempleteNode = new CommonNode(NT.FUNCTEMPLETE, take());
                CommonNode functionSignatureNode = parseVarWTypeSignature();
                expect(TT.OPER, "=>"); skip();

                functionTempleteNode.childs.Add(new CommonNode(NT.TYPE, new Token(TT.NULL, "function", lambdaNode.token.pos)));
                functionTempleteNode.childs.Add(functionSignatureNode);
                functionTempleteNode.childs.Add(parseBody());
                lambdaNode.childs.Add(functionTempleteNode);
                return lambdaNode;
            }

            Syntax.SyntaxError($"Неправильное объявление лямбда функции", tokens[pos]);
            return null;
        }

        private CommonNode parseFormula(CommonNode leftOper = null, bool cmp = false)
        {
            CommonNode buffer;
            CommonNode left;                 // token 1
            if (leftOper == null)
                left = parseTerm(cmp);          // token 1
            else
                left = leftOper;
            Token operatpor = null;          // token 2
            if (peek(TT.OPER) && (new string[] { "&&", "||" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parseTerm(cmp);
                buffer = left;
                left = new CommonNode(NT.CMP, operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                if (peek(TT.OPER) && (new string[] { "&&", "||" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }

            return left;
        }
        private CommonNode parseTerm(bool cmp = false)
        {
            CommonNode buffer;
            CommonNode left = parseTerm2(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek(NT.OPER) && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while ((new string[] { "==", "!=", "<=", ">=", "<", ">" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm2();

                buffer = left;
                left = new CommonNode(NT.CMP, operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == "CMP")
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением логики в арифметике!");
            }
            if (operatpor == null && cmp)
            {
                buffer = left;
                left = new CommonNode(NT.CMP, new Token(TT.NULL, "()", left.token.pos));
                left.childs.Add(buffer);
            }
            return left;
        }
        private CommonNode parseTerm2()
        {
            CommonNode buffer;
            CommonNode left = parseTerm3(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek(NT.OPER) && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while (peek(TT.OPER) && (new string[] { "+", "-" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm3();

                buffer = left;
                left = new CommonNode(NT.BINOPER, operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == NT.BINOPER)
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением арифметики в логике!");
            }

            return left;
        }
        private CommonNode parseTerm3()
        {
            CommonNode buffer;
            CommonNode left = parsePar(); // token 1
            Token operatpor = null;          // token 2
                                             //if (peek(NT.OPER) && (new string[] {"*", "/" }.Contains(tokens[pos].value)))
                                             //operatpor = take();
            while ((peek(TT.OPER) || peek(TT.PREFIX)) && (new string[] { "*", "/", "%", "|", "&" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parsePar();

                buffer = left;
                left = new CommonNode(NT.BINOPER, operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                //if (right.type == NT.BINOPER)
                //SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением арифметики в логике!");
            }

            return left;
        }

        /*public CommonNode parseFormula()
        {
            CommonNode buffer;
            CommonNode left = parsePar(); // token 1
            Token operatpor = null;          // token 2
            if (peek(NT.OPER) && (new string[] { "+", "-", "*", "/" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parsePar();
                buffer = left;
                if (left.token.type.type == NT.OPER && TokenTypeList.permissionOper[left.token.value] < TokenTypeList.permissionOper[operatpor.value])
                {
                    left = new CommonNode(NT.BINOPER, operatpor);
                    left.childs.Add(buffer);
                    left.childs.Add(right);
                }
                else
                {
                    left = new CommonNode(NT.BINOPER, operatpor);
                    left.childs.Add(buffer);
                    left.childs.Add(right);
                }
                if (peek(NT.OPER) && (new string[] { "+", "-", "*", "/" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }

            return left;
        }*/


        private CommonNode parseVarWTypeSignature()
        {
            expect(TT.LPAR); skip();
            if (peek(TT.RPAR))
            {
                skip();
                return new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos].pos));
            }
            CommonNode root = new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos].pos));
            while (true)
            {
                expect(TT.VAR); CommonNode typeNode = new CommonNode(NT.TYPE, take());
                CommonNode declarator = parseDeclarator();
                if (declarator != null) typeNode.childs.Add(declarator);
                //if (typeNode.token.value == "string") typeNode.type = NT.INDICATOR; // TODO: Временная фигня совместимости!!!!!!
                if (tokens[pos].value == "*")
                {
                    skip();
                    typeNode.type = NT.INDICATOR;
                }
                CommonNode declaratorPart = parseDeclarator();
                if (declaratorPart != null) typeNode.childs.Add(declaratorPart);
                expect(TT.VAR); CommonNode varNode = new CommonNode(NT.VAR, take());
                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);
                root.childs.Add(varNode);
                if (!peek(TT.PS))
                {
                    expect(TT.RPAR); skip();
                    return root;
                }
                skip();
            }
        }
        private CommonNode parseVarSignature(TT leftType = TT.LPAR, TT type = TT.VAR)
        {
            TT rightType = TokenTypeList.rightPar[leftType];
            expect(leftType); skip();
            if (peek(rightType))
            {
                skip();
                return new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos].pos));
            }
            CommonNode root = new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos].pos));
            while (true)
            {
                expect(TT.VAR); CommonNode varNode = new CommonNode(getNodeType(type), take());
                root.childs.Add(varNode);
                if (!peek(TT.PS))
                {
                    expect(rightType); skip();
                    return root;
                }
                skip();
            }
        }
        private CommonNode parseFormulaSignature(TT leftType = TT.LPAR)
        {
            TT rightType = TokenTypeList.rightPar[leftType];
            expect(leftType); skip();
            if (peek(rightType))
            {
                skip();
                return new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos - 1].pos));
            }
            CommonNode root = new CommonNode(NT.SIGNATURE, new Token(TT.NULL, "()", tokens[pos - 1].pos));
            while (true)
            {
                CommonNode formulaNode = parseFormula();
                root.childs.Add(formulaNode);
                if (!peek(TT.PS))
                {
                    expect(rightType); skip();
                    return root;
                }
                skip();
            }
        }
        private CommonNode parseIfSignature()
        {
            if (peek(TT.LPAR)) { skip(); }
            if (peek(TT.RPAR))
            {
                skip();
                return new CommonNode(NT.CMP, new Token(TT.NULL, "true", tokens[pos - 1].pos));
            }
            CommonNode left = parseFormula(null, true);
            if (peek(TT.RPAR)) { expect(TT.RPAR); skip(); }
            left.type = NT.CMP;
            return left;
        }
        private CommonNode parseIfSignatureWOther()
        {
            CommonNode buffer;
            CommonNode left = parseFormula(); // token 1
            Token operatpor = null;          // token 2
            if (peek(TT.OPER) && (new string[] { "==", "!=", "<=", ">=", "<", ">", "&&", "||" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parseFormula();
                buffer = left;
                left = new CommonNode(NT.CMP, operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                if (peek(TT.OPER) && (new string[] { "==", "!=", "<=", ">=", "<", ">", "&&", "||" }.Contains(tokens[pos].value)))
                    operatpor = take();
                else
                    operatpor = null;
            }
            if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }

            return left;
        }

        private CommonNode parseBody()
        {
            if ((!peek(TT.LFIG) && !peek(TT.SEM)))
            {
                CommonNode node = new CommonNode(NT.BODY, new Token(TT.NULL, "{}", tokens[pos].pos));
                node.childs.Add(parse());
                return node;
            }
            if (peek(TT.OPER) && tokens[pos].value == "=>")
            {
                skip();
                CommonNode node = new CommonNode(NT.BODY, new Token(TT.NULL, "{}", tokens[pos].pos));
                node.childs.Add(parse());
                return node;
            }
            expect(new TT[] { TT.LFIG, TT.SEM });
            if (peek(TT.SEM))
            {
                skip();
                return new CommonNode(NT.BODY, new Token(TT.NULL, "{}", tokens[pos - 1].pos));
            }
            skip();
            if (peek(TT.RFIG))
            {
                skip();
                return new CommonNode(NT.BODY, new Token(TT.NULL, "{}", tokens[pos - 1].pos));
            }
            //skip();

            CommonNode root = new CommonNode(NT.BODY, new Token(TT.NULL, "{}", tokens[pos].pos));

            while (true)
            {
                CommonNode node = parse();
                root.childs.Add(node);
                if (peek(TT.RFIG))
                {
                    skip();
                    return root;
                }
            }
        }

        private CommonNode parseStack(TT type)
        {
            expect(TT.LFIG);
            CommonNode stackNode = new CommonNode(NT.STACK, take());

            while (peek(type))
            {
                Token varToken = take();
                while (peek(TT.TS))
                {
                    skip();
                    varToken.value += "." + take().value;
                    if (tokens[pos].type != TT.TS)
                        break;
                }
                stackNode.childs.Add(new CommonNode(getNodeType(type), varToken));
                if (tokens[pos].value == ":")
                {
                    skip(); expect(TT.VAR);
                    CommonNode typeNode = new CommonNode(NT.TYPE, take());
                    stackNode.childs[stackNode.childs.Count-1].childs.Add(typeNode);
                }
                if (peek(TT.PS)) skip();
            }

            expect(TT.RFIG); skip();
            return stackNode;
        }
        private CommonNode parseEnumStack(TT type)
        {
            expect(TT.LFIG);
            CommonNode stackNode = new CommonNode(NT.BODY, take());

            while (peek(type))
            {
                Token varToken = take();
                while (peek(TT.TS))
                {
                    skip();
                    varToken.value += "." + take().value;
                    if (tokens[pos].type != TT.TS)
                        break;
                }
                stackNode.childs.Add(new CommonNode(getNodeType(type), varToken));
                if (tokens[pos].value == "=")
                {
                    skip(); expect(TT.NUMBER);
                    CommonNode typeNode = new CommonNode(NT.NUMBER, take());
                    stackNode.childs[stackNode.childs.Count - 1].childs.Add(typeNode);
                }
                if (peek(TT.PS)) skip();
            }

            expect(TT.RFIG); skip();
            return stackNode;
        }

        private CommonNode parseDeclarator()
        {
            if (peek(TT.OPER) && tokens[pos].value == "@")
            {
                CommonNode declarotivePart = new CommonNode(NT.DECLARATOR, take());

                while (peek(TT.VAR))
                {
                    CommonNode type = new CommonNode(NT.TYPE, take());
                    if (peek(TT.OPER) && tokens[pos].value == "*")
                    {
                        skip();
                        type.type = NT.INDICATOR;
                    }
                    CommonNode declar = parseDeclarator(); // Point<Point<int32[]>>
                    if (declar != null) type.childs.Add(declar);

                    declarotivePart.childs.Add(type);

                    if (!peek(TT.PS)) break;
                    else { skip(); expect(TT.OPER); skip(); }//declarotivePart.childs.Add(parseDeclarator()); break; }
                }

                return declarotivePart;
            }
            if (peek(TT.OPER) && tokens[pos].value == "<")
            {
                CommonNode declarotivePart = new CommonNode(NT.DECLARATOR, take());

                while (peek(TT.VAR))
                {
                    CommonNode type = new CommonNode(NT.TYPE, take());
                    if (peek(TT.OPER) && tokens[pos].value == "*")
                    {
                        skip();
                        type.type = NT.INDICATOR;
                    }
                    CommonNode declar = parseDeclarator(); // Point<Point<int32[]>>
                    if (declar != null) type.childs.Add(declar);

                    declarotivePart.childs.Add(type);

                    if (!peek(TT.PS)) break;
                    else skip();
                }
                expect(TT.OPER); if (tokens[pos].value != ">") Syntax.SyntaxError("Ожидался Токен: >", take());
                skip();

                return declarotivePart;
            }
            return null;
        }

        private CommonNode parseDeclarationFunction(CommonNode varNode)
        {
            if (varNode.childs[0].childs.Count > 0 && varNode.childs[0].type == NT.TYPE) Syntax.SyntaxError("После возвращаемого типа функции не может идти Декларотивный Кортеж!", varNode.childs[0].childs[0]);
            bool qsFlag = false;
            CommonNode child;
            CommonNode args = parseVarWTypeSignature(); //expect(new TT[] { TT.LFIG, TT.SEM, TT.OPER, TT.VAR });
            if (tokens[pos].value == "qs") { skip(); qsFlag = true; }
            CommonNode declarator = parseDeclarator();
            CommonNode body = parseBody();
            varNode.token.value = NamespaceString + varNode.token.value;
            varNode.type = NT.FUNC;
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

        private CommonNode parseType(CommonNode typeNode)
        {
            if (peek(TT.OPER) && tokens[pos].value == "*")
            {
                CommonNode indicator = new CommonNode(NT.INDICATOR, take());
                typeNode.type = NT.INDICATOR;
            }

            if (peek(TT.OPER) && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                typeNode.childs.Add(parseDeclarator());

                return parseType(typeNode);
            }

            expect(TT.VAR);
            CommonNode varNode = new CommonNode(NT.VAR, take());
            varNode.childs.Add(typeNode);

            varNode = tryParseVarPath(varNode);

            if (peek(TT.SEM))
            {
                skip();
                return varNode;
            }

            if (peek(TT.PREFIX) && tokens[pos].value == "?")
            {
                CommonNode initMemStaticObject = new CommonNode(NT.ALLOCMEMSTATICOBJECT, take());
                initMemStaticObject.childs.Add(varNode); if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return initMemStaticObject;
            }

            if (peek(TT.OPER) && tokens[pos].value == "=")
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode(NT.BINOPER, oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }

            if (peek(TT.LPAR))
            {
                return parseDeclarationFunction(varNode);
            }

            if (!sem) { return varNode; }
            Syntax.SyntaxError($"На позиции Токена:{pos} ожились токены OPER, SEM, LPAR, OPER! {tokens[pos].value} {tokens[pos].type}", tokens[pos]);
            return null;
        }
        private CommonNode parseVarOperation(CommonNode node = null)
        {
            CommonNode varNode;
            if (node == null)
                varNode = new CommonNode(NT.VAR, take());
            else
                varNode = node;
            expect(new TT[] { TT.OPER, TT.PREFIX, TT.SEM, TT.LPAR, TT.TS, TT.VAR, TT.LK });
            varNode = tryParseVarPath(varNode);

            if (peek(TT.OPER) && (tokens[pos].value == "!" || tokens[pos].value == "-"))
            {
                CommonNode unarOper = new CommonNode(NT.POSTUNAROPER, take());
                unarOper.childs.Add(varNode);
                return unarOper;
            }
            if (tokens[pos].value == "&")
            {
                CommonNode useAddressVarNode = new CommonNode(NT.USEADDRESSVAR, take());
                useAddressVarNode.childs.Add(varNode);
                varNode = useAddressVarNode;
            }


            if (peek(TT.VAR) && varNode.token.value == "qs")
            {
                varNode = new CommonNode(NT.CALL, varNode.token);
                varNode = parseCall(varNode); if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                root.qsFunction.Add(varNode.token.value);
                return varNode;
            }
            if (peek(TT.OPER) && tokens[pos].value == ":")
            {
                skip();
                varNode.type = NT.TAG;
                return varNode;
            }
            if (peek(TT.OPER) && tokens[pos].value == "*")
            {
                CommonNode typeNode = parseType(varNode);
                return typeNode;
            }

            if (peek(TT.LK))
            {
                CommonNode offsetNode;

                while (peek(TT.LK))
                {
                    offsetNode = new CommonNode(NT.OFFSET, take());
                    offsetNode.childs.Add(parseFormula()); expect(TT.RK); skip();
                    offsetNode.childs.Add(varNode);
                    varNode = offsetNode;
                }
            }
            if (peek(TT.OPER) && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                int ps = tryParseDeclarator();
                if (tokens[ps].type == TT.LPAR && !peekFig(ps))
                {
                    // CALL
                    varNode = new CommonNode(NT.CALL, varNode.token);
                    varNode = parseCall(varNode); if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                    return varNode;
                }
            }
            if (peek(TT.OPER) && (tokens[pos].value == "<" || tokens[pos].value == "@"))
            {
                CommonNode typeNode = new CommonNode(NT.TYPE, varNode.token);
                typeNode.childs.Add(parseDeclarator());

                return parseType(typeNode);
            }
            if (peek(TT.VAR))
            {
                CommonNode typeNode = new CommonNode(NT.TYPE, varNode.token);

                return parseType(typeNode);
            }
            if (peek(TT.SEM))
            {
                skip();
                return varNode;
            }
            if (peek(TT.OPER) && new string[] { "=", "+=", "-=", "*=", "/=", "%=" }.Contains(tokens[pos].value))
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode(NT.BINOPER, oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }
            if (peek(TT.PREFIX) && (tokens[pos].value == "++" || tokens[pos].value == "--"))
            {
                Token oper = take();
                CommonNode operNode = new CommonNode(NT.PREUNAROPER, oper);
                operNode.childs.Add(varNode); if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }
            if (peek(TT.LPAR))
            {
                varNode = parseCall(varNode);
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return varNode;
            }


            SyntaxError($"Невозможный Токен:{tokens[pos].value}");
            return null;
        }
        private CommonNode parseVarDeclaration()
        {
            CommonNode typeNode = new CommonNode(NT.TYPE, take());
            if (tokens[pos].value == "*") { skip(); typeNode.type = NT.INDICATOR; } expect(TT.VAR);
            CommonNode varNode = new CommonNode(NT.VAR, take());
            varNode = tryParseVarPath(varNode);

            varNode.childs.Add(typeNode);

            expect(TT.OPER, "=");

            CommonNode varDeclNode = new CommonNode(NT.VARDECL, take());
            varDeclNode.childs.Add(varNode);
            varDeclNode.childs.Add(parseFormula());
            return varDeclNode;
        }


        private CommonNode parseCall(CommonNode varNode, bool flag = false)
        {
            if (!flag) varNode = tryParseVarPath(varNode);
            CommonNode declarator = parseDeclarator();
            if (!peek(TT.LPAR)) return varNode;
            CommonNode args = parseFormulaSignature();
            varNode.type = NT.CALL;
            if (declarator != null) varNode.childs.Add(declarator);
            varNode.childs.Add(args);
            /*if (peek(TT.TS))
            {
                skip(); expect(TT.VAR);
                CommonNode var = new CommonNode(NT.VAR, take());
                var = tryParseVarPath(var);
                var.type = NT.REFVAR;
                var.childs.Add(varNode);
                varNode = var;
            }*/

            return varNode;

            SyntaxError();
            return null;
        }
        private CommonNode parseInline()
        {

            expect(TT.INLINE); skip(); expect(TT.VAR);
            CommonNode nameNode = new CommonNode(NT.INLINE, take());
            while (peek(TT.TS))
            {
                skip();
                nameNode.token.value += "." + take().value;
                if (peek(TT.LPAR))
                    break;
            }
            nameNode.type = NT.INLINE;
            //expect("LPAR");


            if (peek(TT.LPAR))
            {
                //CommonNode args = parseFormula();
                CommonNode args = parseVarWTypeSignature(); expect(new TT[] { TT.LFIG, TT.SEM });
                CommonNode body = parseBody();
                nameNode.childs.Add(args);
                nameNode.childs.Add(body);
                //expect("SEM"); skip();
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return nameNode;
            }

            SyntaxError();
            return null;
        }
        private CommonNode parseAsmInline()
        {
            skip(); expect(TT.VAR);
            CommonNode nameNode = new CommonNode(NT.ASMINLINE, take());
            
            while (peek(TT.TS))
            {
                skip();
                nameNode.token.value += "." + take().value;
                if (peek(TT.LPAR))
                    break;
            }
            root.asmInlineNames.Add(nameNode.token.value);

            if (peek(TT.LPAR))
            {
                CommonNode args = parseVarWTypeSignature(); expect(new TT[] { TT.LFIG, TT.SEM });
                CommonNode body = parseBody();
                nameNode.childs.Add(args);
                nameNode.childs.Add(body);

                return nameNode;
            }

            SyntaxError();
            return null;
        }
        /*
        public CommonNode parseLpar()
        {
            int i = tokens.Count - pos;
            if (i < 2) SyntaxError();
            if (peek("LPAR") && (tokens[pos + 2].type.type == "PS" || tokens[pos + 2].type.type == "RPAR"))
            {
                CommonNode types = parseVarSignature("LPAR", NT.TYPE);
                types.type = "TYPEFORMULA";
                return parseType(types);
            }
            if (peek("LPAR"))
            {
                CommonNode signature = parseVarWTypeSignature();

                expect(new string[] { NT.OPER, "SEM" });
                if (peek(NT.OPER) && tokens[pos].value == "=")
                {

                    Token oper = take();
                    CommonNode rightOperand = parseFormula();
                    CommonNode operNode = new CommonNode(NT.BINOPER, oper);
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
        */

        private CommonNode parseIfStrurct()
        {
            expect(new TT[] { TT.IF, TT.ELSEIF, TT.ELSE });
            Token ifToken = take();
            CommonNode ifNode = new CommonNode(getNodeType(ifToken.type), ifToken);
            CommonNode ifBodyNode;
            if (ifNode.type == NT.ELSE)
            {
                ifBodyNode = parseBody();
                ifNode.childs.Add(ifBodyNode);
                return ifNode;
            }
            CommonNode ifSignatureNode = parseIfSignature();
            ifBodyNode = parseBody();
            ifNode.childs.Add(ifSignatureNode);
            ifNode.childs.Add(ifBodyNode);
            if (peek(TT.ELSE) || peek(TT.ELSEIF))
            {
                CommonNode ifElsesNode = new CommonNode(NT.ELSES, new Token(TT.NULL, "ELSES", tokens[pos].pos));
                ifElsesNode.childs.Add(parseIfStrurct());
                ifNode.childs.Add(ifElsesNode);
            }
            return ifNode;
        }
        private CommonNode parseFormulaIf()
        {
            CommonNode ifNode = new CommonNode(NT.IF, tokens[pos]);
            CommonNode ifSignatureNode = parseIfSignature();
            CommonNode ifBodyNode = parseBody();

            ifNode.childs.Add(ifSignatureNode);
            ifNode.childs.Add(ifBodyNode);

            if (peek(TT.LPAR))
            {
                CommonNode elseifNode = parseFormulaIf();
                elseifNode.type = NT.ELSEIF;
                ifNode.childs.Add(elseifNode);
            }

            if (peek(TT.LFIG))
            {
                CommonNode elseNode = new CommonNode(NT.ELSE, tokens[pos]);
                elseNode.childs.Add(parseBody());
                ifNode.childs.Add(elseNode);
            }
            return ifNode;
        }

        private CommonNode parseQueueControlOperator()
        {
            expect(new TT[] { TT.RETURN, TT.BREAK, TT.CONTINUE, TT.JMP });
            //Token operToken = take();

            if (peek(TT.RETURN))
            {
                CommonNode operNode = new CommonNode(NT.RETURN, take());
                CommonNode rightNode;
                if (peek(TT.SEM) || tokens[pos].value == "void")
                {
                    skip();
                    return operNode;
                }
                else if (peek(TT.LPAR))
                    rightNode = parseFormulaSignature();
                else
                    rightNode = parseFormula();
                //rightNode.type = "VARFORMULA";
                operNode.childs.Add(rightNode);
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }
            if (peek(TT.BREAK))
            {
                CommonNode operNode = new CommonNode(NT.BREAK, take());
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }
            if (peek(TT.CONTINUE))
            {
                CommonNode operNode = new CommonNode(NT.CONTINUE, take());
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return operNode;
            }
            if (peek(TT.JMP))
            {
                CommonNode jmpNode = new CommonNode(NT.JMP, take()); expect(TT.VAR);
                jmpNode.childs.Add(new CommonNode(NT.TAG, take()));
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return jmpNode;
            }

            SyntaxError();
            return null;
        }
        private CommonNode parseCycle()
        {
            expect(new TT[] { TT.FOR, TT.WHILE, TT.ITER, TT.ENUMERATOR, TT.REPT });
            Token cycleToken = take();
            CommonNode cycleNode = new CommonNode(getNodeType(cycleToken.type), cycleToken);


            if (cycleNode.type == NT.FOR)
            {
                // for (int32 i = 0; i < 10; i++)
                if (peek(TT.LPAR)) { expect(TT.LPAR); skip(); }
                //expect(NT.VAR); Token type = take();
                CommonNode varNode = parse();
                CommonNode initNode = varNode;//new CommonNode(varNode.type, varNode.token);
                //initNode.childs.Add(new CommonNode(NT.TYPE, type));
                CommonNode cmpNode = parseIfSignatureWOther();
                expect(TT.VAR);
                CommonNode formulaNode = parseFormula();
                CommonNode stepNode = new CommonNode(NT.STEP, formulaNode.token);
                stepNode.childs.Add(formulaNode);
                if (peek(TT.RPAR)) { expect(TT.RPAR); skip(); }
                CommonNode bodyNode = parseBody();
                cycleNode.childs.Add(initNode);
                cycleNode.childs.Add(cmpNode);
                cycleNode.childs.Add(stepNode);
                cycleNode.childs.Add(bodyNode);
                return cycleNode;
            }

            if (cycleNode.type == NT.WHILE)
            {
                //expect("LPAR");
                CommonNode signature = parseIfSignature();
                CommonNode body = parseBody();
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(body);
                return cycleNode;
            }

            if (cycleNode.type == NT.ITER)
            {
                if (peek(TT.LPAR)) { expect(TT.LPAR); skip(); }
                CommonNode signature = parseFormula(); if (peek(TT.RPAR)) { expect(TT.RPAR); skip(); }
                CommonNode body = parseBody();
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(body);
                return cycleNode;
            }

            if (cycleNode.type == NT.ENUMERATOR)
            {
                if (peek(TT.LPAR)) { expect(TT.LPAR); skip(); }
                expect(TT.VAR);
                CommonNode typeNode = new CommonNode(NT.TYPE, take()); expect(TT.VAR);
                CommonNode varNode = new CommonNode(NT.VAR, take()); varNode.childs.Add(typeNode);
                expect(TT.PS); skip();
                CommonNode signature = parseFormula();
                if (peek(TT.RPAR)) { expect(TT.RPAR); skip(); }

                cycleNode.childs.Add(varNode);
                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(parseBody());
                return cycleNode;
            }

            if (cycleNode.type == NT.REPT)
            {
                if (peek(TT.LPAR)) { skip(); }
                expect(TT.NUMBER);
                CommonNode signature = new CommonNode(NT.NUMBER, take());
                if (peek(TT.RPAR)) { skip(); }

                cycleNode.childs.Add(signature);
                cycleNode.childs.Add(parseBody());
                return cycleNode;
            }

            SyntaxError();
            return null;
        }

        private CommonNode parseStructChildren(string nameStruct)
        {
            //
            //expect(new string[] { NT.VAR, NT.OPER });
            //SyntaxError($"Ну типо ты в структуре данных на позиции:{pos} используешь первым токеном оператором не того типа!!!");

            //
            if (peek(TT.VAR) && (tokens[pos + 1].type == TT.VAR || tokens[pos + 1].value == "*"))
            {
                CommonNode typeNode = new CommonNode(NT.TYPE, take()); if (tokens[pos].value == "*") { typeNode.type = NT.INDICATOR; skip(); }
                CommonNode declarationPart = parseDeclarator();
                if (declarationPart != null) typeNode.childs.Add(declarationPart);
                CommonNode varNode = new CommonNode(NT.VAR, take());

                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);

                if (peek(TT.LPAR))
                {
                    CommonNode args = parseVarWTypeSignature(); expect(new TT[] { TT.LFIG, TT.SEM, TT.OPER });
                    CommonNode declarator = parseDeclarator();
                    CommonNode body = parseBody();
                    varNode.type = NT.FUNC;
                    varNode.childs.Add(args);
                    varNode.childs.Add(body);
                }
                else if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }

                return varNode;
            }
            if (peek(TT.VAR) && tokens[pos].value == nameStruct)
            {
                Token nameToken = take();
                CommonNode constructor = new CommonNode(NT.CONSTRUCTOR, nameToken);
                CommonNode args = parseVarWTypeSignature(); expect(new TT[] { TT.LFIG, TT.SEM });
                CommonNode body = parseBody();
                constructor.childs.Add(new CommonNode(NT.TYPE, new Token(nameToken.type, "void", nameToken.pos)));
                constructor.childs.Add(args);
                constructor.childs.Add(body);
                //SyntaxError($"На позиции:{pos} странный токен не подходящий для объявления члена структуре данных!");
                return constructor;
            }
            if (peek(TT.OPER) && tokens[pos].value == "~")
            {
                skip();
                Token destructorToken = take();
                CommonNode destructorNode = new CommonNode(NT.DESTRUCTOR, destructorToken);
                CommonNode body = parseBody();
                destructorNode.childs.Add(new CommonNode(NT.TYPE, new Token(destructorToken.type, "void", destructorToken.pos)));
                destructorNode.childs.Add(new CommonNode(NT.SIGNATURE, new Token(destructorToken.type, "()", destructorToken.pos)));
                destructorNode.childs.Add(body);
                return destructorNode;
            }
            //

            SyntaxError("Неправильное объявление члена структуры данных");
            return null;
        }
        private CommonNode parseStrurct()
        {
            expect(new TT[] { TT.STRUCT, TT.CLASS }); Token typeStructToken = take();
            expect(TT.VAR); Token nameToken = take();
            CommonNode structNode = new CommonNode(getNodeType(typeStructToken.type), nameToken);
            CommonNode declarotivePart = null;

            if (peek(TT.OPER) && tokens[pos].value == ":")
            {
                skip(); expect(TT.VAR);
                root.ClassesInheritances.Add(nameToken.value, take().value);
            }

            declarotivePart = parseDeclarator();
            
            /*if (peek(NT.OPER))
            {
                skip(); expect(NT.VAR); CommonNode varNode = new CommonNode(NT.VAR, take());
                CommonNode declarotivePartSecond = parseDeclarator();
                if (declarotivePartSecond != null) varNode.childs.Add(declarotivePartSecond);
                root.parentsStructs.Add(structNode.token.value, varNode);
            }*/

            if (declarotivePart != null) structNode.childs.Add(declarotivePart);
            expect(TT.LFIG); skip(); if (peek(TT.RFIG)) { skip(); return structNode; }

            if (peek(TT.MODIFIER) && typeStructToken.value == "struct")
            {
                Syntax.SyntaxError("В структурах запрещенно использование модификаторов доступа!", structNode);
            }
            else if (!peek(TT.MODIFIER) && typeStructToken.value == "struct")
            {
                while (true)
                {
                    CommonNode varNode = parseStructChildren(nameToken.value);
                    if (varNode == null)
                        varNode = parse(); if (!(new NT[] { NT.VAR }.Contains(varNode.type))) SyntaxError($"Ты чё в структуре Узел Типа:{varNode.type} не может находиться!");
                    structNode.childs.Add(varNode);


                    if (peek(TT.RFIG))
                    {
                        skip();
                        if (declarotivePart != null) root.declarotivePatternsStruct.Add(nameToken.value, structNode);
                        if (declarotivePart != null) return null;
                        return structNode;
                    }
                    else if (peek(TT.MODIFIER))
                    {
                        Syntax.SyntaxError("В структурах запрещенно использование модификаторов доступа!", structNode);
                    }
                }
            }

            //
            if (peek(TT.MODIFIER))
            {
                Syntax.SyntaxError("В классах запрещенно использование модификаторов доступа!", structNode);
            }
            else
            {
                while (true)
                {
                    /*CommonNode varNode = parse();
                    if (!(new string[] { NT.VAR, NT.FUNC }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Токен Типа:{varNode.type} не может первым находиться");
                    modifierNode.childs.Add(varNode);*/
                    CommonNode varNode = parseStructChildren(nameToken.value);
                    if (varNode == null)
                        varNode = parse();
                    if (!(new NT[] { NT.VAR, NT.FUNC, NT.CONSTRUCTOR, NT.DESTRUCTOR }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Узел Типа:{varNode.type} не может первым находиться");
                    //modifierNode.childs.Add(varNode);
                    structNode.childs.Add(varNode);

                    if (peek(TT.RFIG))
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

        private CommonNode parseUsing()
        {
            expect(TT.USING);
            CommonNode usingNode = new CommonNode(NT.USING, take());

            if (peek(TT.STRING))
            {
                expect(TT.STRING); usingNode.childs.Add(new CommonNode(NT.NAME, take()));
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
            }
            else if (peek(TT.VAR))
            {
                usingNode.childs.Add(new CommonNode(NT.NAME, take()));
                while (peek(TT.TS))
                {
                    skip();
                    usingNode.childs[0].token.value += "." + take().value;
                    if (tokens[pos].type != TT.TS)
                        break;
                }
                if (peek(TT.INLINE)) usingNode.childs.Add(new CommonNode(NT.INLINE, take()));
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
            }
            else if (peek(TT.INLINE))
            {
                //usingNode.childs.Add(new CommonNode(NT.INLINE, take()));
                skip();
                CommonNode stack = parseStack(TT.VAR);
                foreach (var child in stack.childs)
                {
                    root.inlineNames.Add(child.token.value);
                }
                //expect("SEM"); skip();
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return null;
            }
            else if (peek(TT.ASMINLINE))
            {
                skip();
                CommonNode stack = parseStack(TT.VAR);
                foreach (var child in stack.childs)
                {
                    root.asmInlineNames.Add(child.token.value);
                    root.inlineNames.Add(child.token.value);
                }

                return null;
            }
            else if (peek(TT.NATIVE))
            {
                //usingNode.childs.Add(new CommonNode(NT.INLINE, take()));
                //CommonNode stack = parseStack(NT.VAR);
                skip();
                expect(TT.LFIG); skip();

                while (true)
                {
                    CommonNode typeNode = new CommonNode(NT.TYPE, take());
                    CommonNode varNode = new CommonNode(NT.FUNC, take());
                    varNode = tryParseVarPath(varNode);
                    varNode.childs.Add(typeNode);

                    //if (varNode.childs[0].childs.Count > 0 && varNode.childs[0].type == NT.TYPE) Syntax.SyntaxError("После возвращаемого типа функции не может идти Декларотивный Кортеж!", varNode.childs[0].childs[0]);
                    CommonNode args = parseVarWTypeSignature();

                    if (varNode.childs[0].token.value == "void")
                        root.resualtFunc.Add(varNode.token.value, null);
                    else
                        root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);
                    root.typesArgsFunc.Add(varNode.token.value, args);

                    if (peek(TT.RFIG)) break;
                    else if (!peek(TT.PS)) break;
                    else if (peek(TT.PS)) skip();
                }
                expect(TT.RFIG); skip();
                //expect("SEM"); skip();
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return null;
            }
            else if (peek(TT.SECTION))
            {
                skip(); expect(TT.LFIG); skip();
                expect(TT.ASM);
                root.sectionNodes.Add(new CommonNode(NT.SECTION, take()));
                expect(TT.RFIG); skip();
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                return null;
            }

            return usingNode;
        }
        private CommonNode parseExtern()
        {
            expect(new TT[] { TT.EXTERN, TT.EXTERNLIBRARY, TT.EXTERNFUNC });
            CommonNode externNode = new CommonNode(getNodeType(tokens[pos].type), take());

            if (externNode.type == NT.EXTERN && peek(TT.STRUCT))
            {
                CommonNode structNode = parseStrurct();
                root.externStructs.Add(structNode.token.value);
                return structNode;
            }
            if (externNode.type == NT.EXTERN)
            {
                List<string> listFuncNode = new List<string> ();
                string libraryString = string.Empty;
                while (true)
                {
                    if (peek(TT.EXTERN))
                    {
                        skip(); expect(TT.FROM); skip();
                        expect(TT.VAR);
                        libraryString = take().value;
                        break;
                    }
                    else listFuncNode.Add(parseExtern().token.value);
                }
                if (!root.externFuncs.ContainsKey(libraryString)) root.externFuncs.Add(libraryString, listFuncNode);
                else root.externFuncs[libraryString].AddRange(listFuncNode);
                return null;
            }
            if (externNode.type == NT.EXTERNLIBRARY)
            {
                expect(TT.VAR);
                CommonNode libraryNode = new CommonNode(NT.VAR, take()); expect(TT.STRING);
                if (!root.externLibrarys.ContainsKey(libraryNode.token.value)) root.externLibrarys.Add(libraryNode.token.value, take().value);
                if (!root.externFuncs.ContainsKey(libraryNode.token.value)) root.externFuncs.Add(libraryNode.token.value, new List<string>());
                return null;
            }
            if (externNode.type == NT.EXTERNFUNC)
            {
                expect(TT.VAR);
                CommonNode typeNode = new CommonNode(NT.TYPE, take());
                expect(TT.VAR);
                CommonNode varNode = new CommonNode(NT.FUNC, take());
                varNode = tryParseVarPath(varNode);
                varNode.childs.Add(typeNode);


                CommonNode args = parseVarWTypeSignature();

                if (varNode.childs[0].token.value == "void")
                    root.resualtFunc.Add(varNode.token.value, null);
                else
                    root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);
                root.typesArgsFunc.Add(varNode.token.value, args);
                //root.resualtFunc.Add(varNode.token.value, typeNode);
                if (!peek(TT.FROM)) return varNode;

                expect(TT.FROM); skip(); expect(TT.VAR);
                string libraryString = take().value;
                if (!root.externFuncs.ContainsKey(libraryString)) root.externFuncs.Add(libraryString, new List<string>() { varNode.token.value });
                else root.externFuncs[libraryString].Add(varNode.token.value);

                return null;
            }
            return null;
        }
        private CommonNode parseConst()
        {
            expect(TT.CONST); skip();  expect(TT.VAR);
            CommonNode constNode = new CommonNode(NT.VAR, take());
            expect(TT.OPER); if (tokens[pos].value != "=") SyntaxError($"На Позиции:{pos} после константы ожидался оператор =");
            skip();
            CommonNode valueNode = parseFormula();
            if (!root.consts.ContainsKey(constNode.token.value)) root.consts.Add(constNode.token.value, valueNode);
            return null;
        }
        private CommonNode parseTypeif()
        {
            expect(TT.TYPEIF);
            CommonNode typeifNode = new CommonNode(NT.TYPEIF, take()); expect(TT.VAR);
            typeifNode.childs.Add(new CommonNode(NT.TYPE, take())); expect(TT.VAR);
            typeifNode.childs.Add(new CommonNode(NT.TYPE, take()));
            typeifNode.childs.Add(parseBody());
            return typeifNode;
        }

        private CommonNode parseNamespace()
        {
            skip(); expect(TT.VAR);
            Token namespaceToken = take();
            NamespaceString = namespaceToken.value + ".";
            CommonNode bodyNode = parseBody();
            CommonNode recurse (CommonNode root)
            {
                if (root.type != NT.VAR)
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
        private CommonNode parseEnum()
        {
            skip(); expect(TT.VAR);
            Token enumToken = take();
            CommonNode consts = parseEnumStack(TT.VAR);
            int index = 0;
            DataBase.types.Add(enumToken.value, DataBase.types["long"]);
            DataBase.typesarg.Add(enumToken.value, DataBase.typesarg["long"]);
            DataBase.aligns.Add(enumToken.value, DataBase.aligns["long"]);
            foreach (CommonNode cnst in consts.childs)
            {
                if (cnst.childs.Count > 0)
                {
                    if (!root.consts.ContainsKey(cnst.token.value)) root.consts.Add(enumToken.value + "." + cnst.token.value, cnst.childs[0]);
                } else
                {
                    if (!root.consts.ContainsKey(cnst.token.value))
                        root.consts.Add(enumToken.value + "." + cnst.token.value,
                        new CommonNode(NT.NUMBER, new Token(TT.NUMBER, index.ToString(), cnst.token.pos)));
                }
                index++;
            }
            return null;
        }
        private CommonNode parseOperator()
        {
            skip(); expect(TT.VAR);
            CommonNode typeNode = new CommonNode(NT.TYPE, take());
            if (tokens[pos].value == "*") { skip(); typeNode.type = NT.INDICATOR; }
            CommonNode varNode = new CommonNode(NT.OPER, take());
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
            expect(new TT[] { TT.LFIG, TT.SEM, TT.OPER, TT.VAR }); if (tokens[pos].value == "qs") { skip(); qsFlag = true; }
            CommonNode body = parseBody();
            varNode.token.value = NamespaceString + varNode.token.value;
            varNode.type = NT.FUNC;
            varNode.childs.Add(typeNode);
            varNode.childs.Add(args);
            varNode.childs.Add(body);

            if (varNode.childs[0].token.value == "void")
                root.resualtFunc.Add(varNode.token.value, null);
            else
                root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);

            if (qsFlag) root.qsFunction.Add(varNode.token.value);
            root.operatorFunctions.Add((operatorChar, args.childs[0].childs[0], args.childs[1].childs[0]), varNode.token.value);
            return varNode;
        }

        private CommonNode parseRegDeclaration()
        {
            skip(); expect(TT.VAR);
            CommonNode regDeclationNode = new CommonNode(NT.REGDECL, take());
            if (tokens[pos].value == "=")
            {
                CommonNode assignNode = new CommonNode(NT.BINOPER, take());
                CommonNode rightNode = parseFormula();
                assignNode.childs.Add(regDeclationNode);
                assignNode.childs.Add(rightNode);
                return assignNode;
            }
            return regDeclationNode;
        }
        private CommonNode parseAsm()
        {
            CommonNode asmNode = new CommonNode(NT.ASM, take());
            asmNode.token.value = string.Empty;

            while (tokens[pos].value != "@")
            {
                asmNode.token.value += " " + take().value;
            }
            skip();

            return asmNode;
        }

        public ProgramNode parseCode()
        {
            root = new ProgramNode(NT.ROOT, new Token(TT.NULL, "ROOT", -999));
            while (pos < tokens.Count)
            {
                if (pos >= tokens.Count) break;

                CommonNode node = parse();
                if (node == null) continue;
                if (sem || peek(TT.SEM)) { expect(TT.SEM); skip(); }
                root.childs.Add(node);
            }
            return root;
        }

        public CommonNode parse() // 30 keywords
        {
            if (peek(TT.VARDECL))
            {
                return parseVarDeclaration();
            }
            if (peek(TT.VAR))
            {
                return parseVarOperation();
            }
            if (peek(TT.IF) || peek(TT.ELSEIF) || peek(TT.ELSE))
            {
                return parseIfStrurct();
            }
            if (peek(TT.RETURN) || peek(TT.BREAK) || peek(TT.CONTINUE) || peek(TT.JMP))
            {
                return parseQueueControlOperator();
            }
            if (peek(TT.FOR) || peek(TT.WHILE) || peek(TT.ITER) || peek(TT.ENUMERATOR) || peek(TT.REPT))
            {
                return parseCycle();
            }
            if (peek(TT.LPAR))
            {
                return parseFormulaIf();
            }
            if (peek(TT.STRUCT) || peek(TT.CLASS))
            {
                return parseStrurct();
            }
            if (peek(TT.USING))
            {
                return parseUsing();
            }
            if (peek(TT.EXTERN) || peek(TT.EXTERNLIBRARY) || peek(TT.EXTERNFUNC))
            {
                return parseExtern();
            }
            if (peek(TT.ASM))
            {
                return new CommonNode(NT.ASM, take());
            }
            if (peek(TT.CONST))
            {
                return parseConst();
            }
            if (peek(TT.INLINE))
            {
                return parseInline();
            }
            if (peek(TT.ASMINLINE))
            {
                return parseAsmInline();
            }
            if (peek(TT.TYPEIF))
            {
                return parseTypeif();
            }
            if (peek(TT.NAMESPACE))
            {
                return parseNamespace();
            }
            if (peek(TT.ENUM))
            {
                return parseEnum();
            }
            if (peek(TT.OPERATOR))
            {
                return parseOperator();
            }
            if (tokens[pos].value == "@")
            {
                return parseAsm();
            }
            if (tokens[pos].value == "$")
            {
                return parseRegDeclaration();
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
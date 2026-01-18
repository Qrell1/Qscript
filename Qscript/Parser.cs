using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    internal class Parser
    {
        public List<Token> tokens;
        public int pos = 0;


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
            throw new Exception($"На позиции:{pos} Ожидался Токен:{type}");
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
            throw new Exception($"На позиции:{pos} Ожидался Токен:{types}");
        }
        public void skip()
        {
            pos++;
        }
        /*public Token match(List<TokenType> types)
        {
            if (pos < tokens.Count)
            {
                Token currentToken = tokens[pos];
                foreach (TokenType t in types)
                {
                    if (t.type == currentToken.type.type)
                    {
                        pos++;
                        return currentToken;
                    }
                }
            }
            return null;
        }
        public Token match(TokenType type)
        {
            return match(new List<TokenType>() { type });
        }

        public Token require(List<TokenType> types)
        {
            Token token = match(types);
            if (token == null)
            {
                throw new Exception($"[Parser] На позиции:{pos} ожидался токен {types[0].type}");
            }
            return token;
        }

        public Token require(TokenType type)
        {
            return require(new List<TokenType> { type });
        }*/
        /*public Node parseVarOrNumber()
        {
            // Пропускаем пробелы
            while (pos < tokens.Count && tokens[pos].type.type == "SPACE")
            {
                pos++;
            }

            if (pos >= tokens.Count)
            {
                throw new Exception($"Ожидалось число или переменная на позиции:{pos}, но достигнут конец токенов");
            }

            var number = match2(TokenTypeList.tokenTypes["NUMBER"]);
            if (number != null)
            {
                return new NumberNode(number);
            }

            var var = match2(TokenTypeList.tokenTypes["VAR"]);
            if (var != null)
            {
                return new VarNode(var);
            }

            throw new Exception($"Ожидалось число или переменная на позиции:{pos}, но найден токен: {tokens[pos].type.type} ('{tokens[pos].value}')");
        }*/
        /*public Node parsePar()
        {
            // Пропускаем пробелы
            while (pos < tokens.Count && tokens[pos].type.type == "SPACE")
            {
                pos++;
            }

            if (match2(TokenTypeList.tokenTypes["LPAR"]) != null)
            {
                //var node = parseFormula();
                require2(TokenTypeList.tokenTypes["RPAR"]);
                return node;
            }
            else
            {
                return parseVarOrNumber();
            }
        }*/

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
            }else
            {
                return parseVariableOrNumberOrFunction();
            }
        }
        public CommonNode parseVariableOrNumberOrFunction()
        {
            if (peek("VAR") && tokens[pos+1].type.type == "LPAR")
            {
                // CALL
                CommonNode varNode = new CommonNode("CALL", take());
                CommonNode args = parseFormulaSignature();
                varNode.childs.Add(args);
                return varNode;
            }
            if (peek("VAR") && tokens[pos+1].type.type == "TS")
            {
                CommonNode varNode = new CommonNode("REFVAR", take());
                skip();
                varNode.childs.Add(parseVariableOrNumberOrFunction());
                return varNode;
            }
            if (peek("VAR"))
            {
                return new CommonNode("VAR", take());
            }
            if (peek("NUMBER"))
            {
                return new CommonNode("NUMBER", take());
            }
            if (peek("STRING"))
            {
                return new CommonNode("STRING", take());
            }
            if (peek("CONST"))
            {
                return new CommonNode("CONST", take());
            }
            if (peek("BOOL"))
            {
                return new CommonNode("BOOL", take());
            }

            SyntaxError();
            return null;
        }

        public CommonNode parseFormula()
        {
            CommonNode buffer;
            CommonNode left = parseTerm(); // token 1
            Token operatpor = null;          // token 2
            if (peek("OPER") && (new string[] { "+", "-" }.Contains(tokens[pos].value)))
                operatpor = take();
            while (operatpor != null)
            {
                CommonNode right = parseTerm();
                buffer = left;
                left = new CommonNode("BINOPER", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
                if (right.type == "CMP")
                    SyntaxError($"В условии на позиции Токена:{pos} Ошибка вызваная переплетением логики в арифметике!");
                if (peek("OPER") && (new string[] { "+", "-" }.Contains(tokens[pos].value)))
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
            while (peek("OPER") && (new string[] { "*", "/" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm2();

                buffer = left;
                left = new CommonNode("BINOPER", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
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
            while (peek("OPER") && (new string[] { "==", "!=", "<=", ">=", "<", ">" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parseTerm3();

                buffer = left;
                left = new CommonNode("CMP", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
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
            while (peek("OPER") && (new string[] { "&&", "||" }.Contains(tokens[pos].value)))
            {
                operatpor = take();
                CommonNode right = parsePar();

                buffer = left;
                left = new CommonNode("CMP", operatpor);
                left.childs.Add(buffer);
                left.childs.Add(right);
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
                return new CommonNode("SIGNATURE", new Token(null, "()", 0));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", 0));
            while (true)
            {
                expect("VAR"); CommonNode typeNode = new CommonNode("TYPE", take());
                expect("VAR"); CommonNode varNode = new CommonNode("VAR", take());
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
        public CommonNode parseVarSignature(string leftType="LPAR", string type="VAR")
        {
            string rightType = TokenTypeList.rightPar[leftType];
            expect(leftType); skip();
            if (peek(rightType))
            {
                skip();
                return new CommonNode("SIGNATURE", new Token(null, "()", 0));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", 0));
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
                return new CommonNode("SIGNATURE", new Token(null, "()", 0));
            }
            CommonNode root = new CommonNode("SIGNATURE", new Token(null, "()", 0));
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
            expect("LPAR"); skip();
            if (peek("RPAR"))
            {
                skip();
                return new CommonNode("CMP", new Token(null, "true", -10));
            }
            /*CommonNode buffer;
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
            }*/
            CommonNode left = parseFormula();
            expect("RPAR"); skip();
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
            expect("SEM"); skip();
            
            return left;
        }

        public CommonNode parseBody()
        {
            expect(new string[] { "LFIG", "SEM" });
            if (peek("SEM"))
            {
                skip();
                return new CommonNode("BODY", new Token(null, "{}", -10));
            }
            skip();
            if (peek("RFIG"))
            {
                skip();
                return new CommonNode("BODY", new Token(null, "{}", -10));
            }
            //skip();

            CommonNode root = new CommonNode("BODY", new Token(null, "{}", -10));

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


        public CommonNode parseType(CommonNode typeNode)
        {
            //Token typeToken;//= take();
            expect("VAR");
            //CommonNode typeNode = new CommonNode("TYPE", typeToken);
            CommonNode varNode = new CommonNode("VAR", take());
            varNode.childs.Add(typeNode);
            expect(new string[] { "OPER", "SEM", "LPAR" });
            if (peek("SEM"))
            {
                skip();
                return varNode;
            }

            if (peek("OPER") && tokens[pos].value == "=")
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode("BINOPER", oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                expect("SEM");skip();
                return operNode;
            }

            if (peek("LPAR"))
            {
                //CommonNode args = parseFormula();
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM" });
                CommonNode body = parseBody();
                varNode.type = "FUNC";
                varNode.childs.Add(args);
                varNode.childs.Add(body);
                //expect("SEM"); skip();
                return varNode;
            }

            SyntaxError();
            return null;
        }
        public CommonNode parseVarOperation()
        {
            CommonNode varNode = new CommonNode("VAR", take());
            expect(new string[] { "OPER", "SEM", "LPAR", "TS", "VAR" });

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

            if (peek("OPER") && new string[] { "=", "+=", "-=", "*=", "/=" }.Contains(tokens[pos].value))
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode("BINOPER", oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                expect("SEM"); skip();
                return operNode;
            }
            if (peek("OPER") && new string[] { "--", "++" }.Contains(tokens[pos].value))
            {
                Token oper = take();
                CommonNode operNode = new CommonNode("UNAROPER", oper);
                operNode.childs.Add(varNode);
                expect("SEM"); skip();
                return operNode;
            }

            if (peek("LPAR"))
            {
                CommonNode args = parseFormulaSignature();
                varNode.type = "CALL";
                varNode.childs.Add(args);
                expect("SEM"); skip();
                return varNode;
            }

            if (peek("TS"))
            {
                skip();
                varNode.type = "REFVAR";
                varNode.childs.Add(parseVarOperation());
                return varNode;
            }



            //if (peek("OPER") && new string[] { "-=", "+=", "*=", "/=" }.Contains(tokens[pos].value))
            SyntaxError();
            return null;
        }
        public CommonNode parseLpar()
        {
            int i = tokens.Count - pos;
            if (i < 2) SyntaxError();
            if (peek("LPAR") && (tokens[pos+2].type.type == "PS" || tokens[pos+2].type.type == "RPAR"))
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
                    expect("SEM"); skip();
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
            expect(new string[] {"IF","ELSEIF","ELSE"});
            Token ifToken= take();
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
                CommonNode ifElsesNode = new CommonNode("ELSES", new Token(null, "elses", -10));
                ifElsesNode.childs.Add(parseIfStrurct());
                ifNode.childs.Add(ifElsesNode);
            }
            return ifNode;
        }

        public CommonNode parseQueueControlOperator()
        {
            expect(new string[] { "RETURN", "BREAK", "CONTINUE" });
            //Token operToken = take();

            if (peek("RETURN"))
            {
                CommonNode operNode = new CommonNode("RETURN", take());
                CommonNode rightNode;
                if (peek("SEM"))
                {
                    skip();
                    return operNode;
                }
                else if (peek("LPAR"))
                    rightNode = parseFormulaSignature();
                else
                    rightNode = parseFormula();
                rightNode.type = "VARFORMULA";
                operNode.childs.Add(rightNode);
                expect("SEM"); skip();
                return operNode;
            }
            if (peek("BREAK"))
            {
                CommonNode operNode = new CommonNode("BREAK", take());
                expect("SEM"); skip();
                return operNode;
            }
            if (peek("CONTINUE"))
            {
                CommonNode operNode = new CommonNode("CONTINUE", take());
                expect("SEM"); skip();
                return operNode;
            }

            SyntaxError();
            return null;
        }

        public CommonNode parseCycle()
        {
            expect(new string[] {"FOR","WHILE"});
            Token cycleToken = take();
            CommonNode cycleNode = new CommonNode(cycleToken.type.type, cycleToken);


            if (cycleNode.type == "FOR")
            {
                // for (int32 i = 0; i < 10; i++)
                expect("LPAR"); skip();
                expect("VAR"); Token type = take();
                CommonNode varNode = parse();
                CommonNode initNode = new CommonNode("INIT", varNode.token);
                initNode.childs.Add(varNode);
                CommonNode ifNode = parseIfSignatureWOther();
                expect("VAR");
                CommonNode formulaNode = parseVarOperation();
                CommonNode stepNode = new CommonNode("STEP", formulaNode.token);
                stepNode.childs.Add(formulaNode);
                expect("RPAR"); skip();
                CommonNode bodyNode = parseBody();
                cycleNode.childs.Add(initNode);
                cycleNode.childs.Add(ifNode);
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

            SyntaxError();
            return null;
        }

        public CommonNode parseStructChildren(string nameStruct)
        {
            //
            //expect(new string[] { "VAR", "OPER" });
            //SyntaxError($"Ну типо ты в структуре данных на позиции:{pos} используешь первым токеном оператором не того типа!!!");

            //
            if (peek("VAR") && tokens[pos].value == nameStruct)
            {
                Token nameToken = take();
                CommonNode constructor = new CommonNode("CONSTRUCTOR", nameToken);
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM" });
                CommonNode body = parseBody();
                constructor.childs.Add(args);
                constructor.childs.Add(body);
                /*if (peek("LPAR"))
                {
                    CommonNode varNode = new CommonNode("FUNC", nameToken);
                    CommonNode typeNode = new CommonNode("TYPE", t);
                    CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM" });
                    CommonNode body = parseBody();
                    varNode.childs.Add(args);
                    varNode.childs.Add(body);
                    //expect("SEM"); skip();
                    return varNode;
                }*/
                /*if (peek("OPER") && tokens[pos].value == "=")
                {
                    CommonNode varNode = new CommonNode("VAR", nameToken);
                    CommonNode typeNode = new CommonNode("TYPE", typeToken);
                    varNode.childs.Add(typeNode);
                    CommonNode operNode = new CommonNode("BINOPER", take());
                    CommonNode rightOperand = parseFormula();
                    operNode.childs.Add(varNode);
                    operNode.childs.Add(rightOperand);
                    expect("SEM"); skip();
                    return operNode;
                }*/
                /*if (peek("SEM"))
                {
                    skip();
                    CommonNode varNode = new CommonNode("VAR", nameToken);
                    CommonNode typeNode = new CommonNode("TYPE", typeToken);
                    varNode.childs.Add(typeNode);
                    return varNode;
                }*/
                //SyntaxError($"На позиции:{pos} странный токен не подходящий для объявления члена структуре данных!");
                return constructor;
            }
            if (peek("OPER") && tokens[pos].value == "~")
            {
                skip();
                Token destructorToken = take();
                CommonNode destructorNode = new CommonNode("DESTRUCTOR", destructorToken);
                CommonNode body = parseBody();
                destructorNode.childs.Add(body);
                return destructorNode;
            }
            //

            //SyntaxError("Неправильное объявление члена структуры данных");
            return null;
        }

        public CommonNode parseStrurct()
        {
            expect(new string[] { "STRUCT","CLASS" }); Token typeStructToken = take();
            expect("VAR"); Token nameToken = take();
            CommonNode structNode = new CommonNode(typeStructToken.type.type, nameToken);
            expect("LFIG"); skip(); if (peek("RFIG")) { skip(); return structNode; }

            //
            if (peek("MODIFIER"))
            {
                CommonNode modifierNode = new CommonNode("MODIFIER", take());
                expect("OPER"); if (tokens[pos].value != ":") SyntaxError($"Ну типо после модификатора доступа на позиции Токена:{pos} нужно писать токен :");
                skip();
                //expect("VAR");

                while (true)
                {
                    /*CommonNode varNode = parse();
                    if (!(new string[] { "VAR", "FUNC" }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Токен Типа:{varNode.type} не может первым находиться");
                    modifierNode.childs.Add(varNode);*/
                    CommonNode varNode = parseStructChildren(nameToken.value);
                    if (varNode == null)
                        varNode = parse(); if (!(new string[] { "VAR", "FUNC", "CONSTRUCTOR", "DESTRUCTOR" }.Contains(varNode.type))) SyntaxError($"Ты чё в структурн Узел Типа:{varNode.type} не может первым находиться");
                    modifierNode.childs.Add(varNode);


                    if (peek("RFIG"))
                    {
                        skip();
                        structNode.childs.Add(modifierNode);
                        return structNode;
                    } else if (peek("MODIFIER"))
                    {
                        structNode.childs.Add(modifierNode);
                        modifierNode = new CommonNode("MODIFIER", take());
                        expect("OPER"); if (tokens[pos].value != ":") SyntaxError($"Ну типо после модификатора доступа на позиции Токена:{pos} нужно писать токен :");
                        skip();
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
            expect("STRING"); usingNode.childs.Add(new CommonNode("NAME", take()));
            expect("SEM"); skip();

            return usingNode;
        }

        public CommonNode parseConst()
        {
            expect("CONST");
            Token constToken = take();
            CommonNode constNode = new CommonNode("CONST", constToken);
            expect("OPER"); if (tokens[pos].value != "=") SyntaxError($"На Позиции:{pos} после константы ожидался оператор =");
            skip();
            CommonNode valueNode = parseFormula();
            constNode.childs.Add(valueNode);
            expect("SEM"); skip();
            return constNode;
        }

        public ProgramNode parseCode()
        {
            var root = new ProgramNode("ROOT", new Token(null, "ROOT", -1));
            while (pos < tokens.Count)
            {
                if (pos >= tokens.Count) break;

                CommonNode node = parse();
                if (node == null) break;
                root.childs.Add(node);

                // Проверяем точку с запятой (если есть)
                if (pos < tokens.Count && tokens[pos].type.type == "SEM")
                {
                    //require(TokenTypeList.tokenTypes["SEM"]);
                }
            }
            return root;
        }

        public CommonNode parse()
        {
            if (peek("VAR"))
            {
                return parseVarOperation();
            }
            if (peek("IF") || peek("ELSEIF") || peek("ELSE"))
            {
                return parseIfStrurct();
            }
            if (peek("RETURN") || peek("BREAK") || peek("CONTINUE"))
            {
                return parseQueueControlOperator();
            }
            if (peek("FOR") || peek("WHILE"))
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
            if (peek("ASM"))
            {
                return new CommonNode("ASM", take());
            }
            if (peek("CONST"))
            {
                return parseConst();
            }
            return null;
        }
        

        // ERROR
        public void SyntaxError(string text = "Хз какая синтаксическая ошибка! Или мне лень её описывать)))")
        {
            throw new Exception(text);
        }

    /*public int run(Node node)
    {
        if (node.GetType() == typeof(NumberNode))
        {
            NumberNode node2 = (NumberNode)node;
            return Convert.ToInt32((node2.number.value));
        }
        if (node.GetType() == typeof(UnarOpNode))
        {
            UnarOpNode node2 = (UnarOpNode)node;
            switch (node2.op.type.type)
            {
                case "OUT":
                    Console.WriteLine(node2.operand);
                    return 0;
            }
        }
        if (node.GetType() == typeof(BinOpNode))
        {
            BinOpNode node2 = (BinOpNode)node;
            switch (node2.op.type.type)
            {
                case "PLUS":
                    return run(node2.leftNode) + run(node2.rightNode);
                case "MINUS":
                    return run(node2.leftNode) - run(node2.rightNode);
                case "ASSIGN":
                    var resualt = run(node2.rightNode);
                    var varNode = (VarNode)node2.leftNode;
                    scope.Add(varNode.var.value, resualt);
                    return resualt;
            }
        }
        if (node.GetType() == typeof(VarNode))
        {
            VarNode node2 = (VarNode)node;
            if (scope.ContainsKey(node2.var.value))
            {
                return scope[node2.var.value];
            } else
            {
                throw new Exception($"Переменная с названием {node2.var.value} не найдена !");
            }
        }
        if (node.GetType() == typeof(StatementsNode))
        {
            StatementsNode node2 = (StatementsNode)node;
            foreach (var str in node2.codeStrings)
            {
                run(str);
            }
            return 0;
        }
        throw new Exception($"Что-то пошло не так! P.S.Лучше заного проверить код позиция:{pos}");
    }*/
}
}

/*public Token match(List<TokenType> types)
        {
            if (pos < tokens.Count)
            {
                Token currentToken = tokens[pos];
                foreach (TokenType t in types)
                {
                    if (t == currentToken.type)
                    {
                        pos += 1;
                        return currentToken;
                    }

                }
            }
            return null;
        }

        public Token require(List<TokenType> types)
        {
            Token token = match(types);
            if (token == null)
            {
                throw new Exception($"[Parser] На позиции:{pos} ожидался токен {types[0].type}");
            }
            return token;
        }*/

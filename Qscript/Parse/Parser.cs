using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Qscript
{
    internal class Parser
    {
        public List<Token> tokens;
        public int pos = 0;

        private ProgramNode root;

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
            //throw new Exception($"На позиции:{pos} Ожидался Токен:{types}");
            Syntax.SyntaxError($"На позиции:{pos} Ожидался Токен:{types}", tokens[pos]);
            return false;
        }
        public void skip()
        {
            pos++;
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
            if (peek("VAR") && tokens[pos + 1].value == "<")
            {
                int ps = pos+1;
                int z = -1;
                try{while (true)
                    {
                        if (tokens[ps].value == "<") { z++; ps++; }
                        else if (tokens[ps].value == ">" && z != 0) { z--; ps++; }
                        else if (tokens[ps].value != ">") { ps++; }
                        else if (tokens[ps].value == ">" && z == 0) break;
                    }} catch { SyntaxError("Ну тип ошибка в вызове декларотивной функции"); }
                ps++;
                if (tokens[ps].value == "(")
                {
                    // CALL
                    CommonNode varNode = new CommonNode("CALL", take());
                    varNode = parseCall(varNode);
                    return varNode;
                }
            }
            if (peek("VAR") && tokens[pos + 1].type.type == "LPAR")
            {
                // CALL
                CommonNode varNode = new CommonNode("CALL", take());
                varNode = parseCall(varNode);
                return varNode;
            }
            if (peek("VAR") && tokens[pos+1].type.type == "TS")
            {
                CommonNode varNode = new CommonNode("REFVAR", take()); skip();
                return parseRefvar(varNode);
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
            if (peek("FLOAT"))
            {
                return new CommonNode("FLOAT", take());
            }

            SyntaxError();
            return null;
        }

        public CommonNode parseFormula(CommonNode leftOper=null)
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
            while (peek("OPER") && (new string[] { "*", "/"  }.Contains(tokens[pos].value)))
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
            expect("LPAR"); skip();
            if (peek("RPAR"))
            {
                skip();
                return new CommonNode("CMP", new Token(null, "true", tokens[pos - 1].pos));
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

        public CommonNode parseStack (string type)
        {
            expect("LFIG");
            CommonNode stackNode = new CommonNode("STACK", take());

            while(peek(type))
            {
                stackNode.childs.Add(new CommonNode(type, take()));
                if (peek("PS")) skip();
            }

            expect("RFIG"); skip();
            return stackNode;
        }

        public CommonNode parseDeclarator()
        {
            if (peek("OPER") && tokens[pos].value == "<")
            {
                CommonNode declarotivePart = new CommonNode("DECLARATOR", take());

                while (peek("VAR"))
                {
                    CommonNode type = new CommonNode("TYPE", take());
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
            /*if (peek("LK"))
            {
                skip(); expect("NUMBER");
                CommonNode count = new CommonNode("COUNT", take()); expect("RK"); skip();
                //varNode.type = "ARRAY";
                typeNode.childs.Add(count); //expect("SEM"); skip();
            }
            */

            expect("VAR");
            CommonNode varNode = new CommonNode("VAR", take());
            varNode.childs.Add(typeNode);

            /*if (peek("LK"))
            {
                skip(); expect("NUMBER");
                CommonNode count = new CommonNode("COUNT", take()); expect("RK"); skip();
                //varNode.type = "ARRAY";
                varNode.childs.Add(count); expect("SEM"); skip();
                return varNode;
            }*/

            
            if (peek("SEM"))
            {
                skip();
                return varNode;
            }

            if (peek("POSTFIX") && tokens[pos].value == "?")
            {
                CommonNode initMemStaticObject = new CommonNode("ALLOCMEMSTATICOBJECT", take());
                initMemStaticObject.childs.Add(varNode); expect("SEM"); skip();
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
                expect("SEM");skip();
                return operNode;
            }

            if (peek("LPAR"))
            {
                if (varNode.childs[0].childs.Count > 0 && varNode.childs[0].type == "TYPE") Syntax.SyntaxError("После возвращаемого типа функции не может идти Декларотивный Кортеж!", varNode.childs[0].childs[0]);
                CommonNode child;
                CommonNode args = parseVarWTypeSignature(); expect(new string[] { "LFIG", "SEM", "OPER" });
                CommonNode declarator = parseDeclarator();
                CommonNode body = parseBody();
                varNode.type = "FUNC";
                varNode.childs.Add(args);
                varNode.childs.Add(body);
                if (declarator != null) varNode.childs.Add(declarator);
                if (varNode.childs[0].token.value == "void")
                    root.resualtFunc.Add(varNode.token.value, null);
                else
                    root.resualtFunc.Add(varNode.token.value, varNode.childs[0]);

                if (declarator != null) root.declarotivePatternsFunctions.Add(varNode.token.value, varNode);
                return varNode;
            }
            if (peek("TS"))
            {
                skip(); varNode.type = "REFVAR";
                CommonNode refs = parseRefvar(varNode);
                return refs;
            }

            SyntaxError($"На позиции Токена:{pos} ожились токены OPER, SEM, LPAR!");
            return null;
        }
        public CommonNode parseVarOperation(CommonNode node=null)
        {
            CommonNode varNode;
            if (node == null)
                varNode = new CommonNode("VAR", take());
            else 
                varNode = node;
            expect(new string[] { "OPER", "POSTFIX", "SEM", "LPAR", "TS", "VAR" });

            if (peek("OPER") && tokens[pos].value == "<")
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
            if (peek("POSTFIX") && new string[] { "--", "++" }.Contains(tokens[pos].value))
            {
                Token oper = take();
                CommonNode operNode = new CommonNode("UNAROPER", oper);
                operNode.childs.Add(varNode);
                expect("SEM"); skip();
                return operNode;
            }

            if (peek("LPAR"))//|| (tokens[pos].value == "?" && tokens[pos+1].value == "(")
            {
                varNode = parseCall(varNode);
                //expect("SEM"); skip();
                /*if (peek("TS"))
                {
                    Token ts = take(); expect("VAR");
                    //CommonNode refs = new CommonNode("REFVAR", take());
                    CommonNode binoper = new CommonNode("CALLRETURN", ts); ;
                    CommonNode refs = parseRefvar();
                    binoper.childs.Add(varNode);
                    binoper.childs.Add(refs);

                    return binoper;
                }*/
                expect("SEM"); skip();
                return varNode;
            }

            if (peek("TS"))
            {
                skip(); varNode.type = "REFVAR";
                CommonNode refs = parseRefvar(varNode);
                return refs;
            }



            //if (peek("OPER") && new string[] { "-=", "+=", "*=", "/=" }.Contains(tokens[pos].value))
            SyntaxError(pos.ToString());
            return null;
        }
        public CommonNode parseRefvar(CommonNode refvar=null)
        {
            //CALL
            //OPERATION
            //REFVAR

            expect("VAR");
            CommonNode varNode = new CommonNode("VAR", take());
            if (refvar != null)
            {
                varNode.token.value = refvar.token.value + "." + varNode.token.value;
                varNode.childs = refvar.childs;
            }

            //REFVAR
            while (peek("TS"))
            {
                skip();
                varNode.token.value += "." + take().value;
                if (tokens[pos].type.type != "TS")
                    break;
            }

            //OPERATION
            if (peek("OPER"))
            {
                Token oper = take();
                CommonNode rightOperand = parseFormula();
                CommonNode operNode = new CommonNode("BINOPER", oper);
                operNode.childs.Add(varNode);
                operNode.childs.Add(rightOperand);
                varNode = operNode;
            } else if (peek("OPER"))
            {
                SyntaxError($"На позиции Токена:{pos} после вызова функции не может идти  оператор {tokens[pos].value}");
            }

            //CALL
            if (peek("LPAR"))
            {
                varNode = parseCall(varNode);
            }

            //SEM
            if (peek("SEM"))
                skip();


            return varNode;

            /*if (peek("VAR") && tokens[pos + 1].type.type == "LPAR")
            {
                CommonNode varNode = new CommonNode("CALL", take());
                CommonNode args = parseFormulaSignature(); expect("SEM"); skip();
                varNode.childs.Add(args);
                //varNode.token.value =  refvar.token.value +  "." + varNode.token.value;
                refvar.token.value = refvar.token.value + "." + varNode.token.value;
                refvar.childs = varNode.childs;//childs[0].childs;
                refvar.type = varNode.type;//childs[0].type;
                return varNode;
            }
            //REFVAR
            if (peek("VAR") && tokens[pos + 1].type.type == "TS")
            {
                CommonNode varNode = new CommonNode("REFVAR", take()); skip();
                varNode = parseRefvar(varNode);
                if (refvar.type != "BINOPER")
                    refvar.token.value = refvar.token.value + "." + varNode.token.value;
                else
                    refvar.token.value = refvar.token.value + "." + varNode.childs[0].token.value;
                refvar.childs = varNode.childs;//childs[0].childs;
                refvar.type = varNode.type;//childs[0].type;
                return refvar;
            }
            //VAROPERATION
            if (peek("VAR") && (tokens[pos+1].type.type=="OPER" || tokens[pos + 1].type.type == "POSFIX"))
            {
                CommonNode operationNode = parseVarOperation(); //expect("SEM"); skip();
                refvar.token.value = refvar.token.value + "." + operationNode.childs[0].token.value;
                refvar.childs = operationNode.childs[0].childs;
                refvar.type = "VAR";
                operationNode.childs[0] = refvar;
                return operationNode;
            }
            //VAR
            if (peek("VAR"))
            {
                //CommonNode refs = parseVarOperation();
                CommonNode refs = new CommonNode("VAR", take());
                refvar.token.value =  refvar.token.value + "." + refs.token.value;
                refvar.childs = refs.childs;
                refvar.type = refs.type;
                return refvar;
            }*/

            SyntaxError($"На позиции Токена:{pos} ожидался токен VAR");
            return null;
        }
        public CommonNode parseCall(CommonNode varNode)
        {
            if (peek("?"))
            {
                skip(); varNode.type = "INLINECALL";
            }
            CommonNode declarator = parseDeclarator();
            CommonNode args = parseFormulaSignature();
            varNode.type = "CALL";
            if (declarator != null) varNode.childs.Add(declarator);
                varNode.childs.Add(args);
            if (peek("TS"))
            {
                skip(); expect("VAR");
                CommonNode var = new CommonNode("VAR", take());
                while (peek("TS"))
                {
                    skip();
                    var.token.value += "." + take().value;
                    if (tokens[pos].type.type != "TS")
                        break;
                }
                //if (peek("POSTFIX") || peek("SEM") || peek("LPAR"))
                    //var = parseVarOperation(var);
                //else
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
                nameNode.token.value +=  "." + take().value;
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
                return nameNode;
            }

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
                CommonNode ifElsesNode = new CommonNode("ELSES", new Token(null, "elses", tokens[pos].pos));
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
                //rightNode.type = "VARFORMULA";
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
            expect(new string[] {"FOR","WHILE", "ITER"});
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

            if (cycleNode.type == "ITER")
            {
                expect("LPAR");
                CommonNode signature = parseFormula(); //expect("RPAR"); skip();
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
            CommonNode declarotivePart = null;
            if (peek("OPER") && tokens[pos].value == "<")
            {
                declarotivePart = new CommonNode("DECLARATOR", take());
                while (peek("VAR"))
                {
                    declarotivePart.childs.Add(new CommonNode("CONST", take()));
                    if (!peek("PS")) break;
                    else skip();
                } expect("OPER"); if (tokens[pos].value != ">") Syntax.SyntaxError("Ожидался Токен: >", take());
                skip(); structNode.childs.Add(declarotivePart);
            }
            expect("LFIG"); skip(); if (peek("RFIG")) { skip(); return structNode; }

            if (peek("MODIFIER") && typeStructToken.value == "struct")
            {
                Syntax.SyntaxError("В структурах запрещенно использование модификаторов доступа!", structNode);
            } else if (!peek("MODIFIER") && typeStructToken.value == "struct")
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
                        if (declarotivePart != null) root.declarotivePatternsStruct.Add(nameToken.value, structNode);
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

            if (peek("STRING"))
            {
                expect("STRING"); usingNode.childs.Add(new CommonNode("NAME", take()));
                expect("SEM"); skip();
            }
            else if (peek("VAR"))
            {
                usingNode.childs.Add(new CommonNode("NAME", take()));
                if (peek("INLINE")) usingNode.childs.Add(new CommonNode("INLINE", take()));
                expect("SEM"); skip();
            } else if (peek("LFIG"))
            {
                CommonNode stack = parseStack("VAR");
                usingNode.childs.Add(stack);
                if (peek("INLINE")) usingNode.childs.Add(new CommonNode("INLINE", take()));
                expect("SEM"); skip();
            }

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
            root = new ProgramNode("ROOT", new Token(null, "ROOT", -999));
            while (pos < tokens.Count)
            {
                if (pos >= tokens.Count) break;

                CommonNode node = parse();
                if (node == null) continue;
                root.childs.Add(node);
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
            if (peek("FOR") || peek("WHILE") || peek("ITER"))
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
            if (peek("INLINE"))
            {
                return parseInline();
            }
            return null;
        }
        

        // ERROR
        public void SyntaxError(string text = "Хз какая синтаксическая ошибка! Или мне лень её описывать)))")
        {
            Syntax.SyntaxError(text, tokens[pos]);
            //throw new Exception(text);
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

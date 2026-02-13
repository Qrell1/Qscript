using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public class AbbreviationParser
    {
        public ProgramNode ast;
        public Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> varTypesLocal = new Dictionary<string, CommonNode>();

        private Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "db"},
            {"char", "db"},
            {"float", "dd"},
            {"int32_a", "dd"}
        };

        public AbbreviationParser() { }

        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count >= i + 1)
            {
                return node.childs[i];
            }
            return null;
        }

        public ProgramNode abbParse (ProgramNode root)
        {
            ast = root;
            ast.childs = parse(root, 0).childs;
            ast.varTypes = varTypes;
            return ast;
        }

        public CommonNode parse (CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case "BINOPER":
                    CommonNode leftNode = take(root, 0);
                    CommonNode rightNode = take(root, 1);


                    if (
                        (leftNode.type == "VAR" &&  rightNode.type == "VAR") &&
                        varTypes.Keys.Contains(leftNode.token.value) && varTypes.Keys.Contains(rightNode.token.value) &&
                        !(leftNode.token.value.Contains(".") || rightNode.token.value.Contains("."))
                        )
                    {
                        if (varTypes[leftNode.token.value].token.value == "float" || varTypes[rightNode.token.value].token.value == "float")
                        {
                            CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                            CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                            root.childs[0] = leftNodeTemp;
                            root.childs[1] = rightNodeTemp;
                            root.type = "FLOATBINOPER";
                            return root;
                        }
                    } else if ((leftNode.type == "VAR" && rightNode.type == "VAR") &&
                        !(leftNode.token.value.Contains(".") || rightNode.token.value.Contains(".")) &&
                        !varTypes.Keys.Contains(leftNode.token.value) && !varTypes.Keys.Contains(rightNode.token.value)
                        )
                    {
                        Syntax.SyntaxError($"Нельзя складывать не объявленные Переменные: {leftNode.token.value}, {rightNode.token.value}", leftNode);
                    }
                    if (leftNode.type == "FLOAT" || rightNode.type == "FLOAT")
                    {
                        CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                        CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                        root.childs[0] = leftNodeTemp;
                        root.childs[1] = rightNodeTemp;
                        root.type = "FLOATBINOPER";
                        return root;
                    }
                    if (leftNode.type == "BINOPER" || rightNode.type == "BINOPER")
                    {
                        if (leftNode.type == "BINOPER")
                        {
                            CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                            //root.childs[0] = leftNodeTemp;
                            if (leftNodeTemp.type == "FLOATBINOPER") root.type = "FLOATBINOPER";
                        }
                        if (rightNode.type == "BINOPER")
                        {
                            CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                            //root.childs[0] = rightNodeTemp;
                            if (rightNodeTemp.type == "FLOATBINOPER") root.type = "FLOATBINOPER";
                        }
                    }
                    if (leftNode.type == "BINOPER" && rightNode.type == "BINOPER")
                    {
                        CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                        CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                        if (leftNode.type == "FLOATBINOPER" || rightNode.type == "FLOATBINOPER")
                        {
                            root.childs[0] = leftNodeTemp;
                            root.childs[1] = rightNodeTemp;
                            root.type = "FLOATBINOPER";
                            return root;
                        }
                    }
                    

                    if (leftNode.type == "NUMBER" && rightNode.type == "NUMBER")
                    {
                        int resualt = 0;
                        switch (root.token.value)
                        {
                            case "+":
                                resualt = Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value);
                                break;
                            case "-":
                                resualt = Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value);
                                break;
                            case "*":
                                resualt = Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value);
                                break;
                            case "/":
                                resualt = Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value);
                                break;
                        }
                        root.type = "NUMBER";
                        root.childs = new List<CommonNode>();
                        root.token.type.type = "NUMBER";
                        root.token.value = resualt.ToString();
                        break;
                    }
                    if (leftNode.type != "NUMBER")
                        leftNode = parse(leftNode, z_buffer);
                    if (rightNode.type != "NUMBER")
                        rightNode = parse(rightNode, z_buffer);

                    if (leftNode.type == "NUMBER" && rightNode.type == "NUMBER")
                    {
                        int resualt = 0;
                        switch (root.token.value)
                        {
                            case "+":
                                resualt = Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value);
                                break;
                            case "-":
                                resualt = Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value);
                                break;
                            case "*":
                                resualt = Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value);
                                break;
                            case "/":
                                resualt = Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value);
                                break;
                        }
                        root.type = "NUMBER";
                        root.childs = new List<CommonNode>();
                        root.token.type.type = "NUMBER";
                        root.token.value = resualt.ToString();
                        break;
                    }

                    break;
                case "CONST":
                    if (take(root, 0).type == "NUMBER")
                    {
                        root.childs[0] = parse(root.childs[0], z_buffer + 1);
                        return root;
                    }
                    else if (take(root, 0).type == "STRING")
                    {
                        root.childs[0] = parse(root.childs[0], z_buffer + 1);
                        return root;
                    }
                    else if (take(root, 0).type != "BINOPER")
                        throw new Exception("Ошибка не верный токен ");

                    root.childs[0] = parse(root.childs[0],z_buffer + 1);

                    break;
                case "STRING":
                    root.token.value = $"'{root.token.value}', 0";
                    return root;
                    break;
                case "INLINE":
                    CommonNode body = take(root, 1);
                    for (int i = 0; i < body.childs.Count; i++)
                    {
                        if (body.childs[i].type == "RETURN")
                            throw new Exception("Ошибка в инлайн функции не может быть return");
                    }
                    ast.inlineNames.Add(root.token.value);
                    return root;
                    break;
                case "FUNC":
                    if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value) || ast.declarotiveNames.Contains(root.token.value)) return new CommonNode("AIR", root.token);
                    CommonNode signature = take(root, 1);
                    CommonNode bodyFunc = take(root, 2);
                    List<CommonNode> childs = new List<CommonNode>();
                    if (ast.resualtFunc[root.token.value] != null && !types.Keys.Contains(ast.resualtFunc[root.token.value].token.value))
                    {
                        CommonNode resualtVar = new CommonNode("VAR", new Token(null, "resualtPtr", signature.token.pos));
                        resualtVar.childs.Add(ast.resualtFunc[root.token.value]);
                        childs.Add(resualtVar);
                    }
                    Dictionary<string, CommonNode>  varTypesTemp = varTypes;
                    foreach (CommonNode child in signature.childs)
                    {
                        varTypes.Add(child.token.value, child.childs[0]);
                    }
                    for (int i = 0; i < bodyFunc.childs.Count; i++)
                    {
                        bodyFunc.childs[i] = parse(bodyFunc.childs[i], z_buffer + 2);
                    }
                    varTypes = varTypesTemp;
                    childs.AddRange(signature.childs);
                    ast.typesArgsFunc.Add(root.token.value, signature);
                    signature.childs = childs;
                    root.childs[1] = signature;
                    root.childs[2] = bodyFunc;
                    return root;
                    break;
                case "CALL":
                    if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) root = generationDeclarationFunc(root);

                    CommonNode signatureCall = take(root, 0);
                    for (int j = 0; j < signatureCall.childs.Count; j++)
                    {
                        signatureCall.childs[j] = parse(signatureCall.childs[j], z_buffer + 2);
                    }
                    root.childs[0] = signatureCall;
                    return root;
                    break;
                case "STRUCT":
                    if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
                    return root;
                    break;
                case "CLASS":
                    if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
                    return root;
                    break;
                /*case "REFVAR":
                    CommonNode var = take(root, 0);
                    return root;
                    if (var.type == "REFVAR")
                    {
                        CommonNode vr = parse(var, z_buffer + 1);
                        var.token.value = root.token.value + "." + vr.token.value;
                        var.type = vr.type;
                        var.childs = vr.childs;
                    }
                    else
                    {
                        CommonNode vr = parse(var, z_buffer + 1);
                        var.token.value = root.token.value + "." + vr.token.value;
                    }
                    return var;
                    break;*/
                case "VAR":
                    if (root.childs.Count > 0)
                    {
                        if (varTypes.Keys.Contains(root.token.value))
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем {root.token.value}!", root);

                        CommonNode type = root.childs[0];
                        if (type.childs.Count > 0)
                        {
                            type.token.value = generationDeclarationStruct(type, type.childs[0]);
                            type.childs.Clear();
                        }
                        varTypes.Add(root.token.value, type);
                    }
                    break;
                case "MODIFIER":
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        if (root.childs[i].type != "VAR") root.childs[i] = parse(root.childs[i], z_buffer + 1);
                    }
                    return root;
                    break;
                default:
                    if (root.childs.Count == 0)
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        root.childs[i] = parse(root.childs[i], z_buffer + 1);
                    }
                    return root;
                    break;
            }
            return root;
        }

        public CommonNode replaceNodes (CommonNode rootNode, ref Dictionary<string, string> table)
        {
            CommonNode root = new CommonNode(rootNode.type, new Token(rootNode.token.type, rootNode.token.value, rootNode.token.pos));
            root.childs = new List<CommonNode>();

            if (table.Keys.Contains(root.token.value))
                root.token.value = table[root.token.value];
            for (int i = 0; i < rootNode.childs.Count; i++)
            {
                root.childs.Add(replaceNodes(rootNode.childs[i], ref table));
            }
            return root;
        }

        public string generationDeclarationStruct (CommonNode type, CommonNode declarator)
        {
            if (!ast.declarotivePatternsStruct.Keys.Contains(type.token.value)) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{type.token.value} так как его не существует!", type);
            CommonNode pattern = ast.declarotivePatternsStruct[type.token.value];
            CommonNode patternDeclarator = take(pattern, 0);

            if (patternDeclarator.childs.Count != declarator.childs.Count) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{type.token.value} так как количесва типов разные!", declarator);

            string newName = string.Empty;
            newName += pattern.token.value;
            for (int i = 0; i < declarator.childs.Count; i++)
            {
                newName += $"_{declarator.childs[i].token.value}";
            }
            if (ast.declarotiveNames.Contains(newName)) return newName;

            Dictionary<string, string> declaratorTypes = new Dictionary<string, string>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, declarator.childs[i].token.value);
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newStruct = replaceNodes(pattern, ref declaratorTypes);
            newStruct.childs.Remove(patternDeclarator);
            newStruct.token.value = newName;

            ast.childs.Add(newStruct);

            return newName;
        }
        public CommonNode generationDeclarationFunc(CommonNode root)
        {
            if (!ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{root.token.value} так как его не существует!", root);
            CommonNode pattern = ast.declarotivePatternsFunctions[root.token.value];
            CommonNode patternDeclarator = pattern.childs.Last();

            CommonNode declarator = take(root, 0);

            if (patternDeclarator.childs.Count != declarator.childs.Count) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{root.token.value} так как количесва типов разные!", declarator);

            string newName = string.Empty;
            newName += pattern.token.value;
            for (int i = 0; i < declarator.childs.Count; i++)
            {
                newName += $"_{declarator.childs[i].token.value}";
            }
            root.token.value = newName;
            root.childs[0].childs.Remove(declarator);
            if (ast.declarotiveNames.Contains(newName)) return root;

            Dictionary<string, string> declaratorTypes = new Dictionary<string, string>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, declarator.childs[i].token.value);
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newFunc = replaceNodes(pattern, ref declaratorTypes);
            ast.resualtFunc.Add(newName, take(newFunc, 0));
            ast.typesArgsFunc.Add(newName, take(newFunc, 1));
            newFunc.childs.Remove(newFunc.childs.Last());
            newFunc.token.value = newName;


            ast.childs.Add(newFunc);
            
            return root;
        }
    }
}

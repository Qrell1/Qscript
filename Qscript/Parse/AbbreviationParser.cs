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
                        !(leftNode.token.value.Contains(".") || rightNode.token.value.Contains(".")))
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
                    CommonNode signature = take(root, 1);
                    CommonNode bodyFunc = take(root, 2);
                    List<CommonNode> childs = new List<CommonNode>();
                    if (ast.resualtFunc[root.token.value] != null)
                    {
                        CommonNode resualtVar = new CommonNode("VAR", new Token(null, "resualtPtr", signature.token.pos));
                        resualtVar.childs.Add(ast.resualtFunc[root.token.value]);
                        childs.Add(resualtVar);
                    }
                    for (int i = 0; i < bodyFunc.childs.Count; i++)
                    {
                        bodyFunc.childs[i] = parse(bodyFunc.childs[i], z_buffer + 2);
                    }
                    childs.AddRange(signature.childs);
                    signature.childs = childs;
                    root.childs[1] = signature;
                    root.childs[2] = bodyFunc;
                    return root;
                    break;
                case "CALL":
                    CommonNode signatureCall = take(root, 0);
                    for (int j = 0; j < signatureCall.childs.Count; j++)
                    {
                        signatureCall.childs[j] = parse(signatureCall.childs[j], z_buffer + 2);
                    }
                    root.childs[0] = signatureCall;
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
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем!", root);
                        CommonNode type = root.childs[0];
                        varTypes.Add(root.token.value, type);
                    }
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
    }
}

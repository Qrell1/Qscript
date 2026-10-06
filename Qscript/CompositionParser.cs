using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    public enum MCS
    {
        PARTICAL, COMPOSITION, REF, NAME, ABSTRACT,
        STATIC, ALIGMENT, UNALIGMENT, PADDING
    }

    //public class Modifair { public MCS type; }
    //public class InsertModifair : Modifair { public string data; }
    //public class NameModifair : InsertModifair { }
    //public class RefModifair : InsertModifair { }
    //public class PaddingModifair : Modifair { public int count; }

    public class Field
    { 
        public string name;
        public CommonNode type;
        public List<MCS> modifairs;
        public CommonNode varnode;
        //public string parent;
        public Field(string name, CommonNode type, List<MCS> modifairs=null) 
        {
            this.name = name;
            this.type = type;
            this.modifairs = modifairs;
            if (modifairs == null) this.modifairs = new List<MCS>();
        }
    }

    public class ParticalStruct
    {
        public string nametype;
        public CommonNode structNode;
        public string name;
        public List<MCS> modifairs;
        public List<Field> fields;
        public List<CommonNode> metods;
        public ParticalStruct(string nametype, string name, List<MCS> modifairs, List<Field> fields)
        {
            this.nametype = nametype;
            this.name = name;
            this.modifairs = modifairs;
            this.fields = fields;
        }
        public (string, string, List<Field>, List<CommonNode>) run()
        {
            bool staticflag = false;
            bool abstractflag = false;
            bool refflag = false;
            bool nameflag = false;
            bool aligmentflag = false;
            bool unaligmentflag = false;

            string retString = string.Empty;

            foreach (MCS mod in modifairs)
            {
                switch (mod)
                {
                    case MCS.STATIC: staticflag = true; break;
                    case MCS.ABSTRACT: abstractflag = true; break;
                    case MCS.REF: refflag = true; break;
                    case MCS.NAME: nameflag = true; break;
                    case MCS.ALIGMENT: aligmentflag = true; break;
                    case MCS.UNALIGMENT: unaligmentflag = true; break;
                }
            }
            if (refflag && nameflag)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Syntax.SyntaxError("Невозможное сочитание модификаторов @ref & @name", structNode);
                Console.ResetColor();
            }
            if (abstractflag && staticflag)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Syntax.SyntaxError("Невозможное сочитание модификаторов @static & @abstract", structNode);
                Console.ResetColor();
            }
            if (refflag)
            {
                retString = null;   
            }
            else if (nameflag)
            {
                retString = name;
            }


            if (staticflag)
            {
                if (aligmentflag || unaligmentflag)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    (string file, int offset) = Syntax.getFileName(structNode.token.pos);
                    Console.WriteLine($" ! Предупреждение в файле {file} на строчке {offset+1} модификатор static может сочитаться только с модификаторами @partical, @composition, @name, @ref\nНесовместимые модификаторы не будут учитываться!");
                    Console.ResetColor();
                }

                if (fields != null)
                {
                    foreach (var child in fields)
                    {
                        //CompositionParser.staticConsts.Add(structNode.token.value + (retString != "" ? $".{retString}." : ".") + child.name, child.type);
                        child.name = (retString != null ? $".{retString}." : "") + child.name;
                        Console.WriteLine("name: " + child.name + " | " + nametype);
                        if (!child.modifairs.Contains(MCS.STATIC)) child.modifairs.Add(MCS.STATIC);
                    }
                    //fields.Clear();
                }
                if (metods != null)
                {
                    foreach (var child in metods)
                    {
                        //CompositionParser.staticMetods.Add(structNode.token.value + (retString != "" ? $".{retString}." : ".") + child.token.value, child);
                        child.token.value = (retString != null ? $".{retString}." : "") + child.token.value;
                        child.type = NT.STATICFUNCTION;
                    }
                    //metods.Clear();
                }
                return (name, null, fields, metods);
            }


            //bool mm = staticflag || abstractflag;
            /*if (abstractflag && fields != null)
            foreach (Field child in fields)
            {
                if (child.modifairs == null) child.modifairs = new List<MCS>();
                if (!child.modifairs.Contains(MCS.ABSTRACT)) child.modifairs.Add(MCS.ABSTRACT);
            }*/

            if (nameflag)
            {
                CommonNode newStructNode = new CommonNode(structNode.type, new Token(structNode.token.value, structNode.token.pos));
                newStructNode.token.value += "_" + name;
                foreach (var child in metods)
                {
                    if (child.type != NT.STATICFUNCTION)
                    {
                        newStructNode.childs.Add(child);
                    }
                }
                CommonNode newVar;
                foreach (var child in fields)
                {
                    if (!child.modifairs.Contains(MCS.STATIC))
                    {
                        newVar = new CommonNode(TT.VAR, new Token(child.name, ((child.varnode != null) ? child.varnode.token.pos : structNode.token.pos)));
                        newVar.childs.Add(child.type);
                        newStructNode.childs.Add(newVar);
                    }
                }
                CompositionParser.tempParticalStructs.Add(structNode.token.value + "_" + name, newStructNode);
                fields.Clear();
                metods.Clear();
                if (abstractflag) CompositionParser.ast.classAbstracts.Add(structNode.token.value + "_" + name);
                return (name, structNode.token.value + "_" + name, null, null);
            }

            return (name, retString, fields, metods);
        }
    }

    public class CompositionStruct
    {
        public string name;
        public CommonNode structNode;
        public bool general;
        public List<MCS> modifairs;
        public List<Field> fields;
        public List<CommonNode> metods;
        public CompositionStruct(string name, List<MCS> modifairs, List<Field> fields)
        {
            this.name = name;
            this.modifairs = modifairs;
            this.fields = fields;
        }

        public (string, string, List<Field>, List<CommonNode>) run(ref Dictionary<string, (string, List<Field>, List<CommonNode>)> particals)
        {
            List<Field> newFields = new List<Field>();
            //List<CommonNode> newMetods = new List<CommonNode>();
            
            for (int i = 0; i < fields.Count; i++)
            {
                if (particals.Keys.Contains(fields[i].name))
                {
                    string key = particals.ElementAt(i).Key;
                    (string v1, List<Field> v2, List<CommonNode> v3) = particals.ElementAt(i).Value;
                    if (v1 != null)
                    {
                        newFields.Add(new Field(key, new CommonNode(NT.TYPE, new Token(v1, structNode.token.pos))));
                    }
                    else
                    {
                        foreach (Field f in v2)
                        {
                            newFields.Add(f);
                        }
                        foreach (CommonNode m in v3)
                        {
                            metods.Add(m);
                        }

                    }
                } else newFields.Add(fields[i]);
            }
            fields.Clear();
            fields = newFields;

            bool staticflag = false;
            bool abstractflag = false;
            bool refflag = false;
            bool nameflag = false;
            bool aligmentflag = false;
            bool unaligmentflag = false;

            string retString = string.Empty;

            foreach (MCS mod in modifairs)
            {
                switch (mod)
                {
                    case MCS.STATIC: staticflag = true; break;
                    case MCS.ABSTRACT: abstractflag = true; break;
                    case MCS.REF: refflag = true; break;
                    case MCS.NAME: nameflag = true; break;
                    case MCS.ALIGMENT: aligmentflag = true; break;
                    case MCS.UNALIGMENT: unaligmentflag = true; break;
                }
            }
            if (refflag && nameflag)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Syntax.SyntaxError("Невозможное сочитание модификаторов @ref & @name", structNode);
                Console.ResetColor();
            }
            if (abstractflag && staticflag)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Syntax.SyntaxError("Невозможное сочитание модификаторов @static & @abstract", structNode);
                Console.ResetColor();
            }
            if (refflag)
            {
                retString = null;
            }
            else if (nameflag)
            {
                retString = name;
            }


            if (staticflag)
            {
                if (refflag || aligmentflag || unaligmentflag)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    (string file, int offset) = Syntax.getFileName(structNode.token.pos);
                    Console.WriteLine($" ! Предупреждение в файле {file} на строчке {offset + 1} модификатор static может сочитаться только с модификаторами @partical, @composition, @name\nНесовместимые модификаторы не будут учитываться!");
                    Console.ResetColor();
                }

                if (fields != null)
                {
                    foreach (var child in fields)
                    {
                        //CompositionParser.staticConsts.Add(structNode.token.value + (retString != "" ? $".{retString}." : ".") + child.name, child.type);
                        child.name = (retString != null ? $".{retString}." : ".") + child.name;
                        if (!child.modifairs.Contains(MCS.STATIC)) child.modifairs.Add(MCS.STATIC);
                    }
                    //fields.Clear();
                }
                if (metods != null)
                {
                    foreach (var child in metods)
                    {
                        //CompositionParser.staticMetods.Add(structNode.token.value + (retString != "" ? $".{retString}." : ".") + child.token.value, child);
                        child.token.value = (retString != null ? $".{retString}." : ".") + child.token.value;
                        child.type = NT.STATICFUNCTION;
                    }
                    //metods.Clear();
                }
                return (name, null, fields, metods);
            }


            //bool mm = staticflag || abstractflag;
            /*if (abstractflag && fields != null)
            foreach (Field child in fields)
            {
                if (child.modifairs == null) child.modifairs = new List<MCS>();
                if (!child.modifairs.Contains(MCS.ABSTRACT)) child.modifairs.Add(MCS.ABSTRACT);
            }*/

            if (nameflag)
            {
                CommonNode newStructNode = new CommonNode(structNode.type, new Token(structNode.token.value, structNode.token.pos));
                newStructNode.token.value += "_" + name;
                foreach (var child in metods)
                {
                    if (child.type != NT.STATICFUNCTION) 
                    {
                        newStructNode.childs.Add(child);
                    }
                }
                CommonNode newVar;
                foreach (var child in fields)
                {
                    if (!child.modifairs.Contains(MCS.STATIC))
                    {
                        newVar = new CommonNode(TT.VAR, new Token(child.name, ((child.varnode != null) ? child.varnode.token.pos : structNode.token.pos)));
                        newVar.childs.Add(child.type);
                        newStructNode.childs.Add(newVar);
                    }
                }
                CompositionParser.tempParticalStructs.Add(structNode.token.value + "_" + name, newStructNode);
                //fields.Clear();
                //metods.Clear();
                if (abstractflag) CompositionParser.ast.classAbstracts.Add(structNode.token.value + "_" + name);
                return (name, structNode.token.value + "_" + name, null, null);
            }

            return (name, retString, fields, metods);
        }
    }
    // (offset % 4 != 0)
    public class StructSpace
    {
        public string name;
        public CommonNode nametype;
        public List<ParticalStruct> structParticals;
        public Dictionary<string, CompositionStruct> structCompositions;
        public Dictionary<string, (string, List<Field>, List<CommonNode>)> queueParticals;
        //public CompositionStruct general;
        public StructSpace(string name, CommonNode nametype)
        {
            this.name = name;
            this.nametype = nametype;
            this.structParticals = new List<ParticalStruct>();
            this.structCompositions = new Dictionary<string, CompositionStruct>();
            this.queueParticals = new Dictionary<string, (string, List<Field>, List<CommonNode>)>();
        }

        public CommonNode run()
        {
            /*
            if (nametype.token.value == "Random")
            {
                foreach (var part in structParticals)
                {
                    Console.WriteLine($"@p name: {part.name} | count: {part.fields.Count} | type: {part.nametype}");
                }
                foreach (var part in structCompositions)
                {
                    Console.WriteLine($"@c name: {part.Key} | count: {part.Value.fields.Count} | type: {part.Value.structNode.token.value}");
                }
            }
            */
            //RealessStruct resualt;// = new RealessStruct(name, filds);

            foreach (var st in structParticals)
            {
                (string name, string str, List<Field> fields, List<CommonNode> metods) = st.run();
                if (str == null && fields == null && metods == null) continue;
                else if (str != null)
                {
                    queueParticals.Add(name, (str, fields, metods)); // @name
                }
                else if (str == null)
                {
                    queueParticals.Add(name, (str, fields, metods)); // @ref
                }
            }

            Dictionary<string, List<string>> sortComposition = new Dictionary<string, List<string>>();
            List<string> islist = new List<string>();

            CompositionStruct general = null;

            foreach (var st in structCompositions)
            {
                if (!st.Value.modifairs.Contains(MCS.NAME) && !st.Value.modifairs.Contains(MCS.REF))
                {
                    if (general != null)
                    {
                        Syntax.SyntaxError($"У структуры: {name} невозможно иметь несколько главных композиций", nametype);
                    }
                    general = st.Value;
                }
            }
            if (general == null)
            {
                Syntax.SyntaxError($"Не удалось найти гланую компазицию структуры: {name}", nametype);
            }

            foreach (var st in structCompositions)
            {
                List<string> list = new List<string>(); 
                foreach (var field in st.Value.fields)
                {
                    if (structCompositions.Keys.Contains(field.name))
                    {
                        list.Add(field.name);
                    }
                }
                if (list.Count == 0) islist.Add(st.Value.name);
                sortComposition.Add(st.Value.name, list);
            }

            List<string> isActive = new List<string>();
            foreach(var st in sortComposition)
            {
                if (st.Value.Count != 0)
                {
                    bool isactive = true;
                    foreach (var c in sortComposition[st.Key])
                        if (!islist.Contains(c)) { isActive.Add(st.Key); isactive = false; break; }
                    if (isactive) { islist.Add(st.Key); }

                    List<string> _isActive = new List<string>(isActive);
                    foreach (var v2 in _isActive)
                    {
                        if (sortComposition[v2].Count != 0)
                        {
                            bool _isactive = true;
                            foreach (var c in sortComposition[v2])
                                if (!islist.Contains(c)) { _isactive = false; break; }
                            if (_isactive) { islist.Add(v2); isActive.Remove(v2); }
                        }
                    }
                }
            }

            foreach (var st in islist)
            {
                (string name, string str, List<Field> fields, List<CommonNode> metods) = structCompositions[st].run(ref queueParticals);
                if (str == null && fields == null && metods == null) continue;
                else if (str != null)
                {
                    queueParticals.Add(name, (str, fields, metods)); // @name
                }
                else if (str == null)
                {
                    queueParticals.Add(name, (str, fields, metods)); // @ref
                }
            }

            nametype.childs.Clear();
            foreach (var child in general.metods)
            {
                if (child.type != NT.STATICFUNCTION)
                {
                    nametype.childs.Add(child);
                } else
                {
                    child.type = NT.FUNC;
                    CompositionParser.staticMetods.Add(name + "." + child.token.value, child);
                }
            }
            CommonNode newVar;
            foreach (var child in general.fields)
            {
                if (!child.modifairs.Contains(MCS.STATIC))
                {
                    newVar = new CommonNode(TT.VAR, new Token(child.name, ((child.varnode != null) ? child.varnode.token.pos : nametype.token.pos)));
                    newVar.childs.Add(child.type);
                    nametype.childs.Add(newVar);
                } else
                {
                    Console.WriteLine(name + "." + child.name);
                    CompositionParser.staticConsts.Add(name + "." + child.name, child.type);
                }
            }

            if (general.metods.Count == 0)
                nametype.type = NT.STRUCT;
            else nametype.type = NT.CLASS;

            return nametype;
        }
    }

    public class FluentlyStruct : StructSpace
    {
        public FluentlyStruct(string name, CommonNode nametype) : base(name, nametype) { }
    }


    public class RealessStruct
    { 
        public string name;
        public List<MCS> fields;
        public RealessStruct(string name, List<MCS> fields)
        {
            this.name = name;
            this.fields = fields;
        }
    }

    public static class CompositionParser
    {
        public static ProgramNode ast;
        public static Dictionary<string, RealessStruct> staticStructs = new Dictionary<string, RealessStruct>();
        public static Dictionary<string, RealessStruct> abstractStructs = new Dictionary<string, RealessStruct>();
        //public Dictionary<string, ParticalStruct> particalsStruct;
        public static Dictionary<string, StructSpace> structSpaces = new Dictionary<string, StructSpace>();
        public static Dictionary<string, FluentlyStruct> fluentlyStructs = new Dictionary<string, FluentlyStruct>();
        public static Dictionary<string, CommonNode> staticConsts = new Dictionary<string, CommonNode>();
        public static Dictionary<string, CommonNode> tempParticalStructs = new Dictionary<string, CommonNode>();
        public static Dictionary<string, CommonNode> staticMetods = new Dictionary<string, CommonNode>();

        public static ProgramNode parse(ProgramNode root)
        {
            ast = root;
            //foreach(var child in ast.childs)
            //{
            //    if (child.type == NT.STRUCT || child.type == NT.CLASS)
            //        Console.WriteLine($"-|{child.token.value}");
            //}
            foreach (var child in structSpaces)
            {
            //    Console.WriteLine(child.Key);
                ast.childs.Add(child.Value.run());
            }
            //foreach (var child in ast.childs)
            //{
            //    if (child.type == NT.STRUCT)
            //        Console.WriteLine($"\\\\\\-|{child.token.value}");
            //}
            //Console.WriteLine("----------------------------");
            foreach (var child in tempParticalStructs)
            {
            //    Console.WriteLine(child.Key);
                ast.childs.Add(child.Value);
            }
            //Console.WriteLine(tempParticalStructs.Count);
            //foreach (var child in ast.childs)
            //{
            //    if (child.type == NT.STRUCT)
            //        Console.WriteLine($"//////-|{child.token.value}");
            //}
            foreach (var child in staticConsts)
            {
                Console.WriteLine(child.Key);
            }

            return ast;
        }
    }
}

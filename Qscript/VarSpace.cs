using System.Collections.Generic;

namespace Qscript
{
    public class VarSpace
    {
        public Stack<Dictionary<string, CommonNode>> VarsSpaces = new Stack<Dictionary<string, CommonNode>>();
        public Dictionary<string, CommonNode> VarsData = new Dictionary<string, CommonNode>();


        public VarSpace() { }

        public bool ContainsKey (string key)
        {
            bool r = (VarsSpaces.Count != 0) ? VarsSpaces.Peek().ContainsKey(key) : false;

            if (!r)
            {
                foreach (var space in VarsSpaces)
                {
                    r = (space.ContainsKey(key)) ? true : r;
                    if (r) break;
                }
            }

            if (r) return true;
            return VarsData.ContainsKey(key);
        }

        public bool ContainsValue(CommonNode value)
        {
            bool r = (VarsSpaces.Count != 0) ? VarsSpaces.Peek().ContainsValue(value) : false;
            if (r) return true;
            return VarsData.ContainsValue(value);
        }

        public bool PeekContainsKey(string key)
        {
            if (VarsSpaces.Count == 0) return false;
            return VarsSpaces.Peek().ContainsKey(key);
        }

        public bool PeekContainsValue(CommonNode value)
        {
            if (VarsSpaces.Count == 0) return false;
            return VarsSpaces.Peek().ContainsValue(value);
        }

        public void OpenSpace()
        {
            if (VarsSpaces.Count != 0) VarsSpaces.Push(new Dictionary<string, CommonNode>());
            else VarsSpaces.Push(new Dictionary<string, CommonNode>());
        }

        public void CloseSpace ()
        { 
            if (VarsSpaces.Count != 0) VarsSpaces.Pop();
        }

        public CommonNode GetType(string key)
        {
            if (VarsData.ContainsKey(key)) return VarsData[key];
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key)) return VarsSpaces.Peek()[key];
            return null;
        }
        public string GetTypeValue(string key)
        {
            if (VarsData.ContainsKey(key)) return VarsData[key].token.value;
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key)) return VarsSpaces.Peek()[key].token.value;
            return null;
        }
        public void AddVar(string key, CommonNode value)
        {
            if (VarsSpaces.Count != 0) VarsSpaces.Peek().Add(key, value);
            else VarsData.Add(key, value);
        }

        public void SwapType (string key, CommonNode newType)
        {
            if (VarsData.ContainsKey(key)) VarsData[key] = newType;
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key)) VarsSpaces.Peek()[key] = newType;
        }
        public void SwapTypeValue(string key, string newTypeValue)
        {
            if (VarsData.ContainsKey(key)) VarsData[key].token.value = newTypeValue;
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key)) VarsSpaces.Peek()[key].token.value = newTypeValue;
        }
        public void SwapTypeType(string key, NT newTypeType)
        {
            if (VarsData.ContainsKey(key)) VarsData[key].type = newTypeType;
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key)) VarsSpaces.Peek()[key].type = newTypeType;
        }

        public bool VarIsType(string key, string typeValue)
        {
            if (VarsData.ContainsKey(key) && VarsData[key].token.value == typeValue) return true;
            if (VarsSpaces.Count != 0 && VarsSpaces.Peek().ContainsKey(key) && VarsSpaces.Peek()[key].token.value == typeValue) return true;
            return false;
        }
    }
}

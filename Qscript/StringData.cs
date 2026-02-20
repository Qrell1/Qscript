using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public class StringData
    {
        public List<string> Data;
        public int Length;

        public StringData()
        {
            Data = new List<string>();
            Length = 0;
        }
        public void Append(string str)
        {
            Data.Add(str);
            Length++;
        }

        public string ToString()
        {
            string resualt = string.Empty;
            foreach (string str in Data)
            {
                resualt += str;
            }
            return resualt;
        }
    }
}

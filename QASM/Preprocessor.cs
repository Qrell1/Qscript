using QASM.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QASM
{
    public static class Preprocessor
    {
        public static objProgram PreTranslation(string[] codes)
        {
            objProgram objProgram = new objProgram();

            // CODE DATA
            StringData codeData = new StringData();
            for (int i = 0; i < codes.Length; i++)
            {
                codeData.Append(codes[i]);
            }
            objProgram.codeData = codeData;
            // END CODE DATA


            return objProgram;
        }
    }
}

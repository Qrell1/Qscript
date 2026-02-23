using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QASM.Utils
{
    public class Hex
    {
        public string value;
        
        public Hex (string value)
        {
            this.value = value;
        }
        public Hex ()
        {
            this.value = string.Empty;
        }

        public static string ConvtWithNumber(string number)
        {
            List<int> chars = new List<int>();
            int value = Convert.ToInt32(number);

            while (value != 0)
            {
                int ost = value % 2;
                value = value / 2;
                chars.Add(ost);
            }
            string resualt = string.Empty;
            int j = 0;
            for (int id = chars.Count - 1; id >= 0; id--)
            {
                int i = chars[id];
                resualt += Convert.ToString(i);
                j++;
            }

            /*for (int i = 0; i < 32-resualt.Length; i++)
            {
                resualt = "0" + resualt;
            }*/

            
            return resualt;
        }

        public static string Convt(int hex)
        {
            List<int> chars = new List<int>();
            int value = hex;

            while (value != 0)
            {
                int ost = value % 16;
                value = value / 16;
                chars.Add(ost);
            }
            string resualt = "0x";
            int j = 0;
            for (int id = chars.Count-1; id >= 0; id--)
            {
                int i = chars[id];
                //ABCDEF
                if (i < 10) resualt += Convert.ToString(i);
                else
                {
                    if (i == 10) resualt += "A";
                    else if (i == 11) resualt += "B";
                    else if (i == 12) resualt += "C";
                    else if (i == 13) resualt += "D";
                    else if (i == 14) resualt += "E";
                    else if (i == 15) resualt += "F";
                }
                j++;
                if (j == 2)
                {
                    //resualt = "0x" + resualt;
                    j = 0;
                }
            }
            if (j == 1)
            {
                char ch = resualt[resualt.Length - 1];
                resualt = resualt.Remove(resualt.Length - 1);
                //resualt += "0";
                resualt += ch;
            }
              
            return resualt;
        }

        public static int Convt(string hex)
        {
            int len = hex.Length;

            //int j = 0;
            string str = string.Empty;
            List<int> chars = new List<int>();

            for (int i = 2; i < len; i++) // ABCDEF
            {
                if (hex[i] == 'A') chars.Add(10);
                else if (hex[i] == 'B') chars.Add(11);
                else if (hex[i] == 'C') chars.Add(12);
                else if (hex[i] == 'D') chars.Add(13);
                else if (hex[i] == 'E') chars.Add(14);
                else if (hex[i] == 'F') chars.Add(15);
                else chars.Add(Convert.ToInt32(hex[i].ToString()));
            }
            int f = 0; int rf = 0;
            int j = chars.Count-1;
            for (int r = 0; r < chars.Count; r++)
            {
                if (j == 0) f = 1;
                else if (j == 1) f = 16;
                else f = (int)Math.Pow(16, j);
                rf += f * Convert.ToInt32(chars[r].ToString());
                j--;
            }
            return rf;
        }

        public static Hex ConvtWithBinCode (string bincode, int sizeHex = 1)
        {
            int f = 0; int rf = 0;
            int j = bincode.Length - 1;
            for (int r = 0; r < bincode.Length; r++)
            {
                if (j == 0) f = 1;
                else if (j == 1) f = 2;
                else f = (int)Math.Pow(2, j);
                rf += f * Convert.ToInt32(bincode[r].ToString());
                j--;
            }
            Hex resualt = new Hex(Convt(rf));
            resualt.value = resualt.value.Remove(0,2);
            int len = --sizeHex - (resualt.value.Length - 2) / 2;
            for (int i =0 ; i < len; i++)
            {
                resualt.value = resualt.value + "00";
            }
            resualt.value = "0x" + resualt.value;
            return resualt;
        }

        public static string ReConcat(string hex)
        {
            hex = hex.Remove(0, 2);
            int len = (hex.Length);
            string resualt = string.Empty;
            for (int i = 0; i < len; i+=2)
            {
                resualt += " 0x" + hex[i] + hex[i+1];   
            } resualt = resualt.Trim();
            return resualt;
        }

        //public static 

        public static Hex operator +(Hex left, Hex right)
        {
            int leftInt = Convt(left.value);
            int rightInt = Convt(right.value);
            int resualtInt = leftInt + rightInt;
            Hex resualtHex = new Hex();
            resualtHex.value = Convt(resualtInt);
            return resualtHex;
        }
        public static Hex operator -(Hex left, Hex right)
        {
            int leftInt = Convt(left.value);
            int rightInt = Convt(right.value);
            int resualtInt = leftInt - rightInt;
            Hex resualtHex = new Hex();
            resualtHex.value = Convt(resualtInt);
            return resualtHex;
        }
        public static Hex operator *(Hex left, Hex right)
        {
            int leftInt = Convt(left.value);
            int rightInt = Convt(right.value);
            int resualtInt = leftInt * rightInt;
            Hex resualtHex = new Hex();
            resualtHex.value = Convt(resualtInt);
            return resualtHex;
        }
        public static Hex operator /(Hex left, Hex right)
        {
            int leftInt = Convt(left.value);
            int rightInt = Convt(right.value);
            int resualtInt = leftInt / rightInt;
            Hex resualtHex = new Hex();
            resualtHex.value = Convt(resualtInt);
            return resualtHex;
        }
    }
}

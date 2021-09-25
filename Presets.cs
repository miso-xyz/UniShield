using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UniShield.Protections;
using dnlib.DotNet.Emit;
using dnlib.DotNet;
using UniShield.Helpers;

namespace UniShield
{
    class Presets
    {
        public class MethodRef
        {
            public MethodRef(string ns, string cl, string methods)
            {
                Namespace = ns;
                Class = cl;
                Method = methods;
            }

            public static MethodRef FromInstruction(Instruction inst) { return new MethodRef(((TypeRef)((MemberRef)inst.Operand).Class).Namespace, ((MemberRef)inst.Operand).Class.Name, ((MemberRef)inst.Operand).Name); }

            public string Namespace;
            public string Class;
            public string Method;
        }
        public class ClassRef
        {
            public ClassRef(string ns, string cl)
            {
                Namespace = ns;
                Class = cl;
            }

            public static ClassRef FromInstruction(Instruction inst) { return new ClassRef(((TypeRef)((MemberRef)inst.Operand).Class).Namespace, ((MemberRef)inst.Operand).Class.Name); }

            public string Namespace;
            public string Class;
        }

        public MethodRef SymmetricAlgorithm_Decryptor = new MethodRef("System.Security.Cryptography", "SymmetricAlgorithm", "CreateDecryptor");
        public MethodRef Encoding_GetAscii = new MethodRef("System.Text", "Encoding", "get_ASCII");
        public MethodRef Encoding_GetBytes = new MethodRef("System.Text", "Encoding", "GetBytes");
        public ClassRef HMACSHA256_HashGen = new ClassRef("System.Security.Cryptography", "HMACSHA256");
        public ClassRef SHA256_CryptoService = new ClassRef("System.Security.Cryptography", "SHA256CryptoServiceProvider");
        public ClassRef SupressIldasmAttribute = new ClassRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");

        public void Read(string path)
        {
            string[] fileData = File.ReadAllLines(path);
            foreach (string line in fileData)
            {
                if (line.StartsWith("[") || line.StartsWith("//") || line == "") { continue; }
                string p1 = line.Split("=".ToCharArray())[0].Replace("\t", null);
                string p2 = line.Split("=".ToCharArray())[1].Remove(0, 1);
                switch (p1)
                {
                    case "SymmetricAlgorithm_Decryptor":
                        SymmetricAlgorithm_Decryptor = (MethodRef)Utils.FormatCall(p2);
                        break;
                    case "Encoding_GetAscii":
                        Encoding_GetAscii = (MethodRef)Utils.FormatCall(p2);
                        break;
                    case "Encoding_GetBytes":
                        Encoding_GetBytes = (MethodRef)Utils.FormatCall(p2);
                        break;
                    case "HMACSHA256_HashGen":
                        HMACSHA256_HashGen = (ClassRef)Utils.FormatCall(p2);
                        break;
                    case "SHA256_CryptoService":
                        SHA256_CryptoService = (ClassRef)Utils.FormatCall(p2);
                        break;
                    case "SupressIldasmAttribute":
                        SupressIldasmAttribute = (ClassRef)Utils.FormatCall(p2);
                        break;
                }
            }
        }
    }
}

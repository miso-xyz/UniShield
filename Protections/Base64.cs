using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UniShield.Helpers;

namespace UniShield.Protections
{
    class Base64
    {
        private static readonly HashSet<char> _base64Characters = new HashSet<char>() { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '=' };

        public static bool IsBase64String(string value)
        {
            if (string.IsNullOrEmpty(value)) { return false; }
            else if (value.Any(c => !_base64Characters.Contains(c))) { return false; }
            try { Convert.FromBase64String(value); return true; }
            catch (FormatException) { return false; }
        }

        public static string TryDecodeBase64(string encData)
        {
            try { if (IsBase64String(encData)) { return Encoding.Default.GetString(Convert.FromBase64String(encData)); } }
            catch { }
            return encData;
        }

        public static void Fix()
        {
            int count = 0;
            for (int x_type = 0; x_type < Program.asm.Types.Count; x_type++)
            {
                TypeDef type = Program.asm.Types[x_type];
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) { continue; }
                    string methodNameFormatted = Utils.FormatStatusString(type.Name, method.Name);
                    Program.SetStatusText("Decoding Base64 Strings - " + methodNameFormatted + "\t\t\t\t\t\t\t\t\t (" + x_type + "/" + (Program.asm.Types.Count - 1) + ")", ConsoleColor.White, ConsoleColor.DarkCyan);
                    if (Program.config.DetailedLog) { Program.AddToLog("[Base64]: Now Cleaning '" + methodNameFormatted + "'...", ConsoleColor.Green); }
                    for (int x = 0; x < method.Body.Instructions.Count(); x++)
                    {
                        Instruction inst = method.Body.Instructions[x];
                        if (inst.OpCode.Equals(OpCodes.Ldstr))
                        {
                            string dec = TryDecodeBase64(inst.Operand.ToString());
                            if (dec != inst.Operand.ToString())
                            {
                                if (Program.config.DetailedLog)
                                {
                                    Program.AddToLog("");
                                    Program.AddToLog("  ┌ IL_" + inst.Offset.ToString("X4"), ConsoleColor.DarkGreen, ConsoleColor.Black);
                                    Program.AddToLog("  ├ Original: " + inst.Operand.ToString(), ConsoleColor.DarkMagenta, ConsoleColor.Black);
                                    Program.AddToLog("  └  Decoded: " + dec, ConsoleColor.Magenta, ConsoleColor.Black);
                                }
                                count++;
                                inst.Operand = dec;
                            }
                            try
                            {
                                if (method.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Call) && method.Body.Instructions[x + 2].OpCode.Equals(OpCodes.Callvirt)) { method.Body.Instructions.RemoveAt(x + 1); method.Body.Instructions.RemoveAt(x + 1); }
                                if (method.Body.Instructions[x - 1].OpCode.Equals(OpCodes.Call)) { if (Utils.isFullMethod(method.Body.Instructions[x - 1].Operand, Program.preset.Encoding_GetUTF8)) { method.Body.Instructions.RemoveAt(x - 1); x--; } }
                            }
                            catch { }
                        }
                    }
                }
            }
            if (count != 0) { Program.AddToLog(count + " Base64 Strings Decoded!", ConsoleColor.Green); } else { Program.AddToLog("No Base64 Strings Decoded!"); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UniShield.Helpers;

namespace UniShield.Protections
{
    class Calli
    {
        public static void Fix()
        {
            int count = 0;
            for (int x_type = 0; x_type < Program.asm.Types.Count; x_type++)
            {
                TypeDef type = Program.asm.Types[x_type];
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) { continue; }
                    string formatedName = Utils.FormatStatusString(type.Name, method.Name);
                    if (Program.config.DetailedLog) { Program.AddToLog("[Callis]: Now Cleaning '" + formatedName + "'...", ConsoleColor.DarkGreen); }
                    Program.SetStatusText("Cleaning Control Flow - " + formatedName + "\t\t\t\t\t\t\t\t\t (" + x_type + "/" + (Program.asm.Types.Count - 1) + ")", ConsoleColor.White, ConsoleColor.DarkMagenta);
                    for (int x = 0; x < method.Body.Instructions.Count(); x++)
                    {
                        Instruction inst = method.Body.Instructions[x];
                        if (inst.OpCode.Equals(OpCodes.Ldftn) && method.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Calli))
                        {
                            inst.OpCode = OpCodes.Call;
                            method.Body.Instructions.RemoveAt(x + 1);
                            x--;
                            count++;
                            if (Program.config.DetailedLog) { Program.AddToLog("Fixed Calli at " + inst.Offset.ToString("X4"), ConsoleColor.DarkGreen); }
                        }
                    }
                }
            }
            if (count != 0) { Program.AddToLog("Removed " + count + " Calls!", ConsoleColor.Green); } else { Program.AddToLog("No Calli Found!"); }
        }
    }
}

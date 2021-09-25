using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Protections
{
    class IntConfusion
    {
        public static void Fix()
        {
            int count = 0;
            foreach (TypeDef type in Program.asm.Types)
            {
                if (type.IsGlobalModuleType) { continue; }
                foreach (MethodDef methods in type.Methods)
                {
                    string methodNameFormatted = type.Name + "." + methods.Name;
                    if (methodNameFormatted.Length > 60) { methodNameFormatted = methodNameFormatted.Substring(0, 60); }
                    if (Program.config.DetailedLog) { Program.AddToLog("Cleaning IntConfusion - " + methodNameFormatted, ConsoleColor.Green); }
                    Program.SetStatusText("Cleaning IntConfusion - " + methodNameFormatted, ConsoleColor.White, ConsoleColor.DarkGreen);
                    if (!methods.HasBody) { continue; }
                    methods.Body.InitLocals = false;
                    for (int x = 0; x < methods.Body.Instructions.Count(); x++)
                    {
                        Instruction inst = methods.Body.Instructions[x];
                        switch (inst.OpCode.Code)
                        {
                            case Code.Stloc:
                                Local currentVar = (Local)inst.Operand;
                                try
                                {
                                    if (methods.Body.Instructions[x + 4].OpCode == OpCodes.Xor &&
                                    methods.Body.Instructions[x + 6].OpCode == OpCodes.Bne_Un &&
                                    (Instruction)methods.Body.Instructions[x + 6].Operand == methods.Body.Instructions[x + 11] &&
                                    (Local)methods.Body.Instructions[x + 8].Operand == currentVar)
                                    {
                                        if (Program.config.DetailedLog) { Program.AddToLog("[IntConfusion]: Found! Now Cleaning from IL_" + methods.Body.Instructions[x - 1].Offset.ToString("X4") + " -> IL_" + methods.Body.Instructions[x + 11].Offset.ToString("X4") + "...", ConsoleColor.DarkGreen); }
                                        int ogint = methods.Body.Instructions[x + 1].GetLdcI4Value() + 4;
                                        int curPos = methods.Body.Instructions.IndexOf(inst);
                                        for (int x_remInst = 0; x_remInst < 12; x_remInst++) { methods.Body.Instructions.RemoveAt(curPos); }
                                        //if (x - 11 < 0) { x = 0; } else { x = x - 11; }
                                        //methods.Body.Instructions.Insert(curPos, Instruction.CreateLdcI4(ogint));
                                        if (Program.config.DetailedLog) { Program.AddToLog("[IntConfusion - ConfVar]: Removing '" + currentVar + "'!", ConsoleColor.DarkGreen); }
                                        methods.Body.Variables.Remove(currentVar);
                                        count++;
                                    }
                                }
                                catch {}
                                break;
                        }
                    }
                }
            }
            if (count != 0) { Program.AddToLog(count + " Integers Fixed!", ConsoleColor.Green); } else { Program.AddToLog("No Integers Fixed"); }
        }
    }
}
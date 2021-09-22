using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Protections
{
    class CFlow
    {
        /* From DevT02's Junk Remover*/ class NopCleaning 
        {
            public static void RemoveUselessNops(MethodDef method)
            {
                for (int x = 0; x < method.Body.Instructions.Count(); x++)
                {
                    Instruction inst = method.Body.Instructions[x];
                    if (inst.OpCode == OpCodes.Nop &&
                        !IsNopBranchTarget(method, inst) &&
                        !IsNopSwitchTarget(method, inst) &&
                        !IsNopExceptionHandlerTarget(method, inst))
                    {
                        method.Body.Instructions.Remove(inst);
                        x--;
                    }
                }
            }

            private static bool IsNopBranchTarget(MethodDef method, Instruction nopInstr)
            {
                var instr = method.Body.Instructions;
                for (int i = 0; i < instr.Count; i++)
                {
                    if (instr[i].OpCode.OperandType == OperandType.InlineBrTarget || instr[i].OpCode.OperandType == OperandType.ShortInlineBrTarget && instr[i].Operand != null)
                    {
                        Instruction instruction2 = (Instruction)instr[i].Operand;
                        if (instruction2 == nopInstr)
                            return true;
                    }
                }
                return false;
            }

            private static bool IsNopSwitchTarget(MethodDef method, Instruction nopInstr)
            {
                var instr = method.Body.Instructions;
                for (int i = 0; i < instr.Count; i++)
                {
                    if (instr[i].OpCode.OperandType == OperandType.InlineSwitch && instr[i].Operand != null)
                    {
                        Instruction[] source = (Instruction[])instr[i].Operand;
                        if (source.Contains(nopInstr))
                            return true;
                    }
                }
                return false;
            }

            private static bool IsNopExceptionHandlerTarget(MethodDef method, Instruction nopInstr)
            {
                bool result;
                if (!method.Body.HasExceptionHandlers)
                    result = false;
                else
                {
                    var exceptionHandlers = method.Body.ExceptionHandlers;
                    foreach (var exceptionHandler in exceptionHandlers)
                    {
                        if (exceptionHandler.FilterStart == nopInstr ||
                            exceptionHandler.HandlerEnd == nopInstr ||
                            exceptionHandler.HandlerStart == nopInstr ||
                            exceptionHandler.TryEnd == nopInstr ||
                            exceptionHandler.TryStart == nopInstr)
                            return true;
                    }
                    result = false;
                }
                return result;
            }
        }

        public static void Fix()
        {
            bool hasCflow = false;
            int count = 0;
            foreach (TypeDef type in Program.asm.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) { continue; }
                    string methodNameFormatted = type.Name + "." + method.Name;
                    if (methodNameFormatted.Length > 60) { methodNameFormatted = methodNameFormatted.Substring(0, 60); }
                    method.Body.KeepOldMaxStack = true;
                    Local cflowVar = null;
                    try { cflowVar = GetCFlowLocal(method); hasCflow = true; }
                    catch { continue; }
                    if (Program.config.DetailedLog) { Program.AddToLog("[CFlow]: Now Cleaning '" + methodNameFormatted + "'...", ConsoleColor.DarkGreen); }
                    Program.SetStatusText("Cleaning Control Flow - " + methodNameFormatted, ConsoleColor.White, ConsoleColor.DarkMagenta);
                    RemoveIntCycle(method);
                    RemoveCFlowLocalCalls(method, cflowVar);
                    //RemoveEndingGo->s(method);
                    RemoveCFlowLocalRefs(method, cflowVar);
                    NopCleaning.RemoveUselessNops(method);
                    RemoveUselessBRs(method);
                    count++;
                }
            }
            if (!hasCflow) { Program.AddToLog("Control Flow Obfuscation Not Found"); }
            if (count != 0) { Program.AddToLog(count + " Methods Cleaned!", ConsoleColor.Green); } else { Program.AddToLog("No Methods Cleaned"); }
        }

        private static void RemoveUselessBRs(MethodDef method)
        {
            for (int x = 0; x < method.Body.Instructions.Count(); x++)
            {
                Instruction inst = method.Body.Instructions[x];
                try
                {
                    if (inst.OpCode.Equals(OpCodes.Br) || inst.OpCode.Equals(OpCodes.Br_S))
                    {
                        if ((Instruction)inst.Operand == method.Body.Instructions[x + 1])
                        {
                            if (Program.config.DetailedLog) { Program.AddToLog("[CFlow - Useless BRs]:\t\t\tRemoved\tIL_" + inst.Offset.ToString("X4"), ConsoleColor.Green); }
                            method.Body.Instructions.Remove(inst);
                            x--;
                        }
                    }
                }
                catch { }
            }
        }

        private static Local GetCFlowLocal(MethodDef method)
        {
            if (method.Body.Instructions[0].IsLdcI4() &&
                method.Body.Instructions[1].OpCode.Equals(OpCodes.Stloc) &&
                method.Body.Instructions[2].OpCode.Equals(OpCodes.Br))
            { return (Local)method.Body.Instructions[1].Operand; }
            return null;
        }

        private static void RemoveGotos(MethodDef method)
        {
            for (int x = 0; x < method.Body.Instructions.Count(); x++)
            {
                Instruction inst = method.Body.Instructions[x];
                Instruction nextInst = null;
                try { nextInst = method.Body.Instructions[x + 1]; }
                catch { return; }
                if (nextInst.OpCode.Equals(OpCodes.Br))
                {
                    switch (inst.OpCode.Code)
                    {
                        case Code.Nop:
                        case Code.Brfalse:
                            if (Program.config.DetailedLog) { Program.AddToLog("[CFlow - GOTOs]:\t\t\tRemoved\tIL_" + nextInst.Offset.ToString("X4"), ConsoleColor.Green); }
                            method.Body.Instructions.Remove(nextInst);
                            break;
                    }
                }
            }
        }

        private static void RemoveIntCycle(MethodDef method)
        {
            for (int x = 0; x < method.Body.Instructions.Count(); x++)
            {
                Instruction inst = method.Body.Instructions[x];
                try
                {
                    if (inst.OpCode.Equals(OpCodes.Nop) && // gets removed by NOP cleaner
                        method.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Ldloc) &&
                        method.Body.Instructions[x + 2].IsLdcI4() &&
                        method.Body.Instructions[x + 3].OpCode.Equals(OpCodes.Ceq) &&
                        method.Body.Instructions[x + 4].OpCode.Equals(OpCodes.Brfalse))
                    {
                        if (Program.config.DetailedLog) { Program.AddToLog("[CFlow - IntCycle]:\t\t\tRemoved\tIL_" + method.Body.Instructions[x + 1].Offset.ToString("X4") + " -> IL_" + method.Body.Instructions[x + 4].Offset.ToString("X4") + " (5)", ConsoleColor.Green); }
                        method.Body.Instructions[x + 1].OpCode = OpCodes.Nop;
                        method.Body.Instructions[x + 2].OpCode = OpCodes.Nop;
                        method.Body.Instructions[x + 3].OpCode = OpCodes.Nop;
                        method.Body.Instructions[x + 4].OpCode = OpCodes.Nop;
                    }
                }
                catch { return; }
            }
        }

        private static void RemoveCFlowLocalCalls(MethodDef method, Local cflowLocal)
        {
            for (int x = 0; x < method.Body.Instructions.Count(); x++)
            {
                Instruction inst = method.Body.Instructions[x];
                try
                {
                    if ((Local)method.Body.Instructions[x + 1].Operand == cflowLocal)
                    {
                        if (inst.IsLdcI4() &&
                        method.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Stloc) &&
                        method.Body.Instructions[x + 2].OpCode.Equals(OpCodes.Br))
                        {
                            if (Program.config.DetailedLog) { Program.AddToLog("[CFlow - NewCycleInt]:\t\t\tRemoved\tIL_" + inst.Offset.ToString("X4") + " -> IL_" + method.Body.Instructions[x + 2].Offset.ToString("X4") + " (3)", ConsoleColor.Green); }
                            inst.OpCode = OpCodes.Nop;
                            method.Body.Instructions[x + 1].OpCode = OpCodes.Nop;
                            method.Body.Instructions[x + 2].OpCode = OpCodes.Nop;
                            x--;
                        }
                    }
                }
                catch { return; }
            }
        }

        private static void RemoveCFlowLocalRefs(MethodDef method, Local cflowLocal)
        {
            for (int x = 0; x < method.Body.Instructions.Count(); x++)
            {
                Instruction inst = method.Body.Instructions[x];
                switch (inst.OpCode.Code)
                {
                    case Code.Ldloc:
                    case Code.Stloc:
                        if ((Local)inst.Operand == cflowLocal)
                        {
                            if (Program.config.DetailedLog) { Program.AddToLog("[CFlow - CFlowVariableReference]:\tRemoved\tIL_" + inst.Offset.ToString("X4") + " -> IL_" + method.Body.Instructions[x + 2].Offset.ToString("X4") + " (2)", ConsoleColor.Green); }
                            method.Body.Instructions.RemoveAt(x - 1);
                            method.Body.Instructions.Remove(inst);
                            x -= 2;
                        }
                        break;
                }
            }
            method.Body.Variables.Remove(cflowLocal);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UniShield.Helpers;

/*namespace UniShield.Protections
{
    class CFlow
    {
        class NopCleaning // From DevT02's Junk Remover
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
                    CFlowChunks cflow = CFlowChunks.Parse(method);
                    if (cflow == null) { continue; }
                    Chunk[] chunks = cflow.GetChunks();
                    foreach (Chunk chunk in chunks)
                    {
                        Instruction[] insts = cflow.RemoveCFlowJunk(chunk.GetInstructions(method));
                        insts.Reverse();
                        for (int x = chunk.from; x < method.Body.Instructions.Count(); x++)
                        {
                            method.Body.Instructions.RemoveAt(x);
                            if (x == chunk.to) { break; }
                        }
                        for (int x = 0; x < insts.Count(); x++)
                        {
                            method.Body.Instructions.Insert(chunk.from, insts[x]);
                        }
                    }
                    //RemoveIntCycle(method);
                    //RemoveCFlowLocalCalls(method, cflowVar);
                    //RemoveEndingGotos(method);
                    //RemoveCFlowLocalRefs(method, cflowVar);
                    NopCleaning.RemoveUselessNops(method);
                    RemoveUselessBRs(method);
                    count++;
                }
            }
            if (!hasCflow) { Program.AddToLog("Control Flow Obfuscation Not Found"); }
            if (count != 0) { Program.AddToLog(count + " Methods Cleaned!", ConsoleColor.Green); } else { Program.AddToLog("No Methods Cleaned"); }
        }

        class Chunk
        {
            public Chunk(int chunkNum, int endChunkNum, int startIndex, int endIndex) { num = chunkNum; nextNum = endChunkNum; from = startIndex; to = endIndex; }
            public int num, nextNum, from, to;

            public Instruction[] GetInstructions(MethodDef srcMethod)
            {
                List<Instruction> insts = new List<Instruction>();
                for (int x = from; x < srcMethod.Body.Instructions.Count(); x++)
                {
                    insts.Add(srcMethod.Body.Instructions[x]);
                    if (x == to) { break; }
                }
                return insts.ToArray(); ;
            }
        }

        class CFlowChunks
        {
            public static CFlowChunks Parse(MethodDef method)
            {
                if (HasHeader(method))
                {
                    CFlowChunks header = new CFlowChunks();
                    header.method = method;
                    header.cflowVar = (Local)method.Body.Instructions[1].Operand;
                    header.startingValue = method.Body.Instructions[0].GetLdcI4Value();
                    header.chunksStart = Utils.SimplifyBR(method.Body.Instructions[3]);
                    return header;
                }
                return null;
            }

            public MethodDef method;
            public Local cflowVar;
            public int startingValue;
            public Instruction chunksStart;
            public List<Chunk> methodChunks = new List<Chunk>();

            public static bool HasHeader(MethodDef method)
            {
                try
                {
                    if (method.Body.Instructions[0].IsLdcI4() &&
                    method.Body.Instructions[1].OpCode.Equals(OpCodes.Stloc) &&
                    (method.Body.Instructions[2].OpCode.Equals(OpCodes.Br) || method.Body.Instructions[2].OpCode.Equals(OpCodes.Br_S)))
                    { return true; }
                }
                catch { }
                return false;
            }

            public int CountTotalChunks()
            {
                int count = 0;
                for (int x = 0; x < method.Body.Instructions.Count(); x++)
                {
                    Instruction inst = method.Body.Instructions[x];
                    if (inst.OpCode.Equals(OpCodes.Ldloc))
                    {
                        if ((Local)inst.Operand == cflowVar)
                        {
                            if (isChunkStart(new Instruction[] { inst, method.Body.Instructions[x + 1], method.Body.Instructions[x + 2] })) { count++; }
                        } 
                    }
                }
                return count - 1;
            }

            public Chunk[] GetChunks()
            {
                List<Chunk> chunks = new List<Chunk>();
                int rangeStart = -1, rangeEnd = -1, currentChunk = 0, endCurrentChunkNum = 0;
                bool hasCflow = false;
                if (Program.asm.EntryPoint.DeclaringType.Methods.Contains(method)) { int.Parse("0"); }
                for (int x = 0; x < method.Body.Instructions.Count(); x++)
                {
                    Instruction inst = method.Body.Instructions[x];
                    switch (inst.OpCode.Code)
                    {
                        case Code.Ldloc:
                            if ((Local)inst.Operand == cflowVar)
                            {
                                if (isChunkStart(new Instruction[] { inst, method.Body.Instructions[x + 1], method.Body.Instructions[x + 2] }))
                                { rangeStart = x; currentChunk = method.Body.Instructions[x + 1].GetLdcI4Value(); hasCflow = true; }
                            }
                            break;
                        case Code.Stloc:
                            if ((Local)inst.Operand == cflowVar)
                            {
                                if (isChunkEnd(new Instruction[] { inst, method.Body.Instructions[x + 1], method.Body.Instructions[x + 2], method.Body.Instructions[x + 3] }))
                                { rangeEnd = x; endCurrentChunkNum = method.Body.Instructions[x - 1].GetLdcI4Value(); hasCflow = true; }
                            }
                            break;
                    }
                    if (rangeStart != -1 && rangeEnd != -1) { chunks.Add(new Chunk(currentChunk, endCurrentChunkNum, rangeStart, rangeEnd)); rangeStart = -1; rangeEnd = -1; }
                }
                if (hasCflow)
                {
                    for (int x_ch = 0; x_ch < chunks.Count(); x_ch++)
                    {
                        Chunk tempChunk = chunks[x_ch];
                        if (isUselessChunk(RemoveCFlowJunk(tempChunk.GetInstructions(method)))) { chunks.Remove(tempChunk); x_ch--;}
                    }
                    return SortChunks(chunks.ToArray());
                }
                return null;
            }

            public Chunk[] SortChunks(Chunk[] chunks)
            {
                Chunk[] sortedList = new Chunk[chunks.Count()];
                //for (int x = startingValue; x < chunks.Count(); x++) { if (chunks[x].num == highestCur) { sortedList.Add(chunks[x]); highestCur = chunks[x].nextNum; x = 0; } }
                for (int x = startingValue; x < chunks.Count(); x++) {sortedList[chunks[x].num] = chunks[x]; }
                return sortedList.ToArray();
            }

            // 0001 | ldloc  %CFLOWVAR%  - Start of new chunk
            // 0002 | ldc.i4 ???
            // 0003 | ceq
            private bool isChunkStart(Instruction[] instArr) { try { if ((Local)instArr[0].Operand == cflowVar && instArr[1].IsLdcI4() && instArr[2].OpCode.Equals(OpCodes.Ceq)) { return true; } } catch { } return false; }

            // 0000 | stloc  %CFLOWVAR%  - What ChunkNum to go next
            // 0001 | nop                - End of current chunk
            // 0002 | ldloc  %CFLOWVAR%  - Start of new chunk
            // 0003 | ldc.i4 ???
            private bool isChunkEnd(Instruction[] instArr) { try { if ((Local)instArr[0].Operand == cflowVar && (Local)instArr[2].Operand == cflowVar && instArr[3].IsLdcI4()) { return true; } } catch { } return false; }

            private bool isUselessChunk(Instruction[] instArr)
            {
                try
                {
                    if (instArr[instArr.Count() - 1].Operand == cflowVar &&
                        instArr[instArr.Count() - 2].IsLdcI4())
                    {
                        return true;
                    }
                }
                catch {}
                return false;
            }

            /*private Chunk Recover(Chunk chunk)
            {
                List<Instruction> insts = GetInstructions(chunk).ToList();
                if (!insts[0].IsLdloc()) { insts.Insert(0, Instruction.Create(OpCodes.Ldloc, cflowVar)); chunk.to++; }
                if (!insts[insts.Count() - 1].IsStloc()) { insts.Insert(insts.Count() - 1, Instruction.Create(OpCodes.Stloc, cflowVar)); chunk.to++; }//insts.Insert(0, Instruction.CreateLdcI4(chunk.num)); insts.RemoveAt(insts.Count() - 1);
                if (!isChunkStart(new Instruction[] { insts[0], insts[1], insts[2] }))
                {
                    switch (insts[insts.Count()].OpCode.Code)
                    {
                        case Code.Ceq: chunk.from -= 2; break;
                        case Code.Ldc_I4: chunk.from--; break;
                    }
                }
                if (!isChunkEnd(new Instruction[] { insts[insts.Count() - 4], insts[insts.Count() - 3], insts[insts.Count() - 2], insts[insts.Count() - 1] }))
                {
                    switch (insts[insts.Count()-1].OpCode.Code)
                    {
                        case Code.Nop: chunk.to+=2; break;
                        case Code.Ldloc: chunk.to++; break;
                        case Code.Stloc: chunk.to += 3; break;
                    }
                }
                return chunk;
            }

            private bool Validate(Chunk chunk)
            {
                //List<Instruction> insts = GetInstructions(chunk).ToList();
                List<Instruction> insts = chunk.GetInstructions(method).ToList();
                if (!isChunkStart(new Instruction[] { insts[0], insts[1], insts[2] })) { return false; }
                if (!isChunkEnd(new Instruction[] {insts[insts.Count() - 4],insts[insts.Count() - 3],insts[insts.Count() - 2],insts[insts.Count() - 1]})) { return false; }
                return true;
            }

            public Instruction[] RemoveCFlowJunk(Instruction[] instructions)
            {
                //List<Instruction> insts = GetInstructions(chunk).ToList();
                //while (!Validate(chunk)) { chunk = Recover(chunk); }
                //List<Instruction> insts = chunk.GetInstructions(method).ToList();
                List<Instruction> insts = instructions.ToList();
                for (int x = 0; x < insts.Count(); x++)
                {
                    Instruction inst = insts[x];
                    switch (inst.OpCode.Code)
                    {
                        case Code.Ldloc:
                            if ((Local)inst.Operand == cflowVar)
                            {
                                /*for (int x_ = 0; x_ < insts.Count(); x_++)
                                {
                                    insts.
                                }
                                insts.RemoveRange(x, 4);
                            }
                            break;
                        case Code.Stloc:
                            if ((Local)inst.Operand == cflowVar)
                            {
                                insts.RemoveRange(x - 1, 2);
                            }
                            break;
                    }
                }
                return insts.ToArray();
            }
        }

        private static Instruction[] GetRange(MethodDef method, int from, int to) { List<Instruction> insts = new List<Instruction>(); for (int x = from; x < method.Body.Instructions.Count(); x++) { if (x == to) { break; } insts.Add(method.Body.Instructions[x]); } return insts.ToArray(); }

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
}*/
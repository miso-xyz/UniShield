using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Protections
{
    class Renaming // Unusued
    {
        public static void Fix()
        {
            Program.asm.EntryPoint.Name = "Main";
            Program.asm.EntryPoint.DeclaringType.Name = "Program";
            foreach (TypeDef type in Program.asm.Types)
            {
                for (int x = 0; x < type.Methods.Count(); x++)
                {
                    MethodDef method = type.Methods[x];
                    if (!method.HasBody) { continue; }
                    Program.SetStatusText("[Renaming]: Reading '" + method.Name + "'...", ConsoleColor.White, ConsoleColor.DarkYellow);
                    bool methodRenamed = false;
                    for (int x_ = 0; x_ < method.Body.Instructions.Count(); x_++)
                    {
                        Instruction inst = method.Body.Instructions[x_];
                        switch (inst.OpCode.Code)
                        {
                            case Code.Callvirt:
                                UniShield.Presets.MethodRef methodRef = UniShield.Presets.MethodRef.FromInstruction(inst);
                                if (methodRef == Program.preset.SymmetricAlgorithm_Decryptor && Presets.ClassRef.FromInstruction(method.Body.Instructions[3]) == Program.preset.SHA256_CryptoService)
                                {
                                    Program.AddToLog("[Renaming]: " + '"' + method.Name + '"' + " -> " + '"' + "SHA256Decryptor" + '"', ConsoleColor.Green);
                                    method.Name = "SHA256Decryptor"; methodRenamed = true;
                                }
                                break;
                            case Code.Newobj:
                                UniShield.Presets.ClassRef classRef = UniShield.Presets.ClassRef.FromInstruction(inst);
                                if (classRef == Program.preset.HMACSHA256_HashGen &&
                                    UniShield.Presets.MethodRef.FromInstruction(method.Body.Instructions[x_ - 4]) == Program.preset.Encoding_GetAscii &&
                                    UniShield.Presets.MethodRef.FromInstruction(method.Body.Instructions[x_ - 2]) == Program.preset.Encoding_GetBytes)
                                {
                                    Program.AddToLog("[Renaming]: " + '"' + method.Name + '"' + " -> " + '"' + "HashingHWID" + '"', ConsoleColor.Green);
                                    method.Name = "HashingHWID"; methodRenamed = true;
                                }
                                break;
                            case Code.Ldstr:
                                if (inst.Operand.ToString().StartsWith("SELECT * FROM Win32_")) { Program.AddToLog("[Renaming]: " + '"' + method.Name + '"' + " -> " + '"' + "GetHWID" + '"', ConsoleColor.Green); method.Name = "GetHWID"; methodRenamed = true; }
                                if (inst.Operand.ToString() == "External component has thrown an exception.") { Program.AddToLog("[Renaming]: " + '"' + method.Name + '"' + " -> " + '"' + "CloseHandleAntiDebug" + '"', ConsoleColor.Green); method.Name = "CloseHandleAntiDebug"; methodRenamed = true; }
                                break;
                        }
                        if (methodRenamed) { break; }
                    }
                }
            }
        }
    }
}

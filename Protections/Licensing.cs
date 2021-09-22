using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.IO;
using UniShield.Helpers;

namespace UniShield.Protections
{
    class Licensing
    {
        public static string GetLicensingType()
        {
            foreach (MethodDef methods in Program.asm.EntryPoint.DeclaringType.Methods)
            {
                if (!methods.HasBody) { continue; }
                foreach (string line in Utils.GetStringsFromMethod(methods))
                {
                    if (line.Contains("Win32_DiskDrive")) { return "USB"; }
                    if (line.Contains("Win32_Processor") || line.Contains("Win32_BIOS")) { return "HWIDLocked"; }
                    if (line.Contains("License.dat")) { return "LicenseFile"; }
                }
            }
            return "???";
        }

        public static string GetEncryptedASMIV()
        {
            foreach (MethodDef methods in Program.asm.EntryPoint.DeclaringType.Methods)
            {
                if (!methods.IsConstructor || !methods.HasBody) { continue; }
                foreach (Instruction inst in methods.Body.Instructions) { if (inst.OpCode.Equals(OpCodes.Ldstr) && inst.Operand.ToString().Length == 16) { return inst.Operand.ToString(); } }
            }
            return null;
        }

        public static byte[] GetEncryptedASM()
        {
            foreach (MethodDef methods in Program.asm.EntryPoint.DeclaringType.Methods)
            {
                if (!methods.IsConstructor || !methods.HasBody) { continue; }
                foreach (Instruction inst in methods.Body.Instructions)
                {
                    if (inst.OpCode.Equals(OpCodes.Ldstr) && inst.Operand.ToString().Length > 16)
                    {
                        return Convert.FromBase64String(inst.Operand.ToString());
                        /*Program.AddToLog("Extracting " + '"' + Path.GetFileName(Program.path) + "_UniShield-ExtractedASM.exe" + '"' + "...", ConsoleColor.Yellow);
                        try
                        {
                            File.WriteAllBytes(Path.GetFileName(Program.path) + "_UniShield-ExtractedASM.exe", Convert.FromBase64String(inst.Operand.ToString()));
                            Program.AddToLog("Successfully Extracted " + '"' + Path.GetFileName(Program.path) + "_UniShield-ExtractedASM.exe" + '"' + "!", ConsoleColor.Green);
                        }
                        catch (Exception ex) { Program.AddToLog("Failed to extract " + '"' + Path.GetFileName(Program.path) + "_UniShield-ExtractedASM.exe" + '"' + "! - " + ex.Message, ConsoleColor.Green); }*/
                    }
                }
            }
            return null;
        }
    }
}
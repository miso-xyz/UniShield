using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.IO;
using UniShield.Helpers;
using System.Text.RegularExpressions;
using UniShield.Rendering;
using System.Windows.Forms;

namespace UniShield.Protections
{
    class Licensing
    {
        public static byte[] GetFromFile(string b64Key = "")
        {
            byte[] decryptedASM = null;
            FileBrowser filebrowser = new FileBrowser();
            Program.AddToLog("  - Encrypted using License File", ConsoleColor.Yellow);
            Program.AddToLog("");
            string file = "";
            Program.AddToLog("    Searching for License.dat...", ConsoleColor.Yellow);
            if (!File.Exists("License.dat") || !File.Exists(Directory.GetParent(Program.path).FullName + @"\License.dat"))
            {
                Program.AddToLog("    Coudn't Find License.dat in Application Folder, you'll have to locate it yourself", ConsoleColor.Red);
                Program.AddToLog("");
                if (Program.config.MinimalLayout) { Program.config.CustomFileBrowser = false; }
                if (Program.config.CustomFileBrowser)
                {
                    FileBrowser.ShowTutorial();
                    Program.AddToLog("");
                    Program.AddLogSeparator();
                    Program.SetStatusText("Waiting for User Input...", ConsoleColor.White, ConsoleColor.DarkCyan);
                    filebrowser.title = "Select License File";
                    filebrowser.defaultExtFilter = 1;
                    filebrowser.ListDrives();
                    file = filebrowser.Initialize();
                    Program.DrawAbout();
                    if (file == null) { Program.AddToLog("Operation Cancelled!", ConsoleColor.Red); Program.SetStatusText("Operation Cancelled!", ConsoleColor.White, ConsoleColor.DarkRed); return null; }
                }
                else
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "License File|License.dat|All Files|*.*";
                    Program.SetStatusText("Waiting for User Input...", ConsoleColor.White, ConsoleColor.DarkCyan);
                    if (ofd.ShowDialog() == DialogResult.OK) { file = ofd.FileName; }
                    else { Program.AddToLog("Operation Cancelled!", ConsoleColor.Red); Program.SetStatusText("Operation Cancelled!", ConsoleColor.White, ConsoleColor.DarkRed); return null; }
                }
            }
            else { Program.AddToLog("    License.dat Found!", ConsoleColor.Green); Program.AddToLog(""); }
            if (File.Exists("License.dat")) { file = "License.dat"; }
            else if (File.Exists(Directory.GetParent(Program.path).FullName)) { file = Directory.GetParent(Program.path).FullName + "\\License.dat"; }
            Program.SetStatusText("Attempting Decryption...", ConsoleColor.White, ConsoleColor.DarkYellow);
            Program.AddToLog("Attempting Decryption...", ConsoleColor.DarkYellow);
            try
            {
                string dataTemp = File.ReadAllText(file);
                if (b64Key != "") {dataTemp = Encrypt(dataTemp, b64Key);}
                decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), dataTemp, Licensing.GetEncryptedASMIV());
                Program.SetStatusText("File Successfully Decrypted...", ConsoleColor.White, ConsoleColor.DarkGreen);
                Program.AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                Program.AddToLog("  └ Decryption Key: '" + File.ReadAllText(file) + "'", ConsoleColor.DarkMagenta);
                Program.AddToLog("");
            }
            catch { throw; Program.AddToLog("Failed to decrypt Packed Assembly!", ConsoleColor.Red); Program.SetStatusText("Decryption Failed!", ConsoleColor.White, ConsoleColor.DarkRed); Console.ReadKey(); Application.Exit(); }
            return decryptedASM;
        }
        public static byte[] GetFromRemovableDrive(string b64Key = "")
        {
            byte[] decryptedASM = null;
            Program.AddToLog("  - Encrypted using Removable Drive HWID (Hardware ID)", ConsoleColor.Yellow);
            Program.AddToLog("");
            Program.SetStatusText("Calculating Removable Drives HWID...", ConsoleColor.White, ConsoleColor.DarkMagenta);
            EncryptionHelper.RemovableDriveHWID[] removableDrivesHWIDs = EncryptionHelper.GetRemovableDrivesHWID();
            if (removableDrivesHWIDs.Count() == 0) { Program.AddToLog("No Removable Drives Found!", ConsoleColor.Red); return null; }
            foreach (EncryptionHelper.RemovableDriveHWID HWID in removableDrivesHWIDs)
            {
                string hwidTemp = HWID.HWID;
                if (b64Key != "") { hwidTemp = Encrypt(hwidTemp, b64Key); }
                Program.SetStatusText("Attempting Decryption - " + HWID.drive.Name, ConsoleColor.White, ConsoleColor.DarkYellow);
                Program.AddToLog("Attempting Decryption - " + HWID.drive.Name, ConsoleColor.DarkYellow);
                try
                {
                    decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), hwidTemp, Licensing.GetEncryptedASMIV());
                    Program.AddToLog("");
                    Program.AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                    Program.AddToLog("  ├ Drive Letter: " + HWID.drive.Name, ConsoleColor.Magenta);
                    Program.AddToLog("  └ Drive HWID:   " + HWID.HWID, ConsoleColor.DarkMagenta);
                    Program.AddToLog("");
                    break;
                }
                catch { Program.AddToLog("Invalid HWID!", ConsoleColor.DarkRed); Program.AddToLog(""); }
            }
            if (decryptedASM == null) { Program.AddToLog("No Removable Drives were valid!", ConsoleColor.Red); }
            return decryptedASM;
        }
        public static byte[] GetFromHWID(string b64Key = "")
        {
            byte[] decryptedASM = null;
            Program.AddToLog("  - Encrypted using Computer HWID (Hardware ID)", ConsoleColor.Yellow);
            Program.AddToLog("");
            try
            {
                Program.SetStatusText("Calculating HWID...", ConsoleColor.White, ConsoleColor.DarkMagenta);
                string compHWID = EncryptionHelper.GetHWID();
                if (b64Key != "") { compHWID = Encrypt(compHWID, b64Key); }
                Program.SetStatusText("Attempting Decryption...", ConsoleColor.White, ConsoleColor.DarkYellow);
                Program.AddToLog("Attempting Decryption...", ConsoleColor.DarkYellow);
                decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), compHWID, Licensing.GetEncryptedASMIV());
                Program.AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                Program.AddToLog("  └ HWID: " + compHWID, ConsoleColor.DarkMagenta);
                Program.AddToLog("");
            }
            catch { throw; Program.AddToLog("Failed to decrypt Packed Assembly!", ConsoleColor.Red); Program.SetStatusText("Decryption Failed!", ConsoleColor.White, ConsoleColor.DarkRed); }
            return decryptedASM;
        }

        public static string Encrypt(string data, string b64Key)
        {
            StringBuilder str = new StringBuilder(), str2 = new StringBuilder();
            for (int x = 0; x < data.Length; x++) { str.Append((char)data[x] ^ (char)Convert.FromBase64String(b64Key)[x % 4]); }
            foreach (char c in str.ToString()) { if (Regex.IsMatch(c.ToString(), "[A-Za-z]")) { str2.Append((char)((int)(((c & 'ß') - '4') % '\u001a' + (c & ' ') + 'A'))); } }
            return str2.ToString();
        }

        public static void Fix()
        {
            Program.AddToLog("Application is Packed - Licensing Protection Found!", ConsoleColor.Green);
            Program.AddToLog("");
            Program.SetStatusText("Figuring out Licensing Type...", ConsoleColor.White, ConsoleColor.DarkYellow);
            string licType = GetLicensingType();
            string b64Key = "";
            byte[] output = null;
            if (licType.Contains("V2 - ")) { b64Key = licType.Substring(licType.IndexOf("V2 - ") + 5, licType.Length - (licType.IndexOf("V2 - ") + 5)); }
            if (licType.StartsWith("USB")) { output = GetFromRemovableDrive(b64Key); }
            else if (licType.StartsWith("LicenseFile")) { output = GetFromFile(b64Key); }
            else if (licType.StartsWith("HWIDLocked")) { output = GetFromHWID(b64Key); }
            if (output != null)
            {
                Program.SetStatusText("Saving File...", ConsoleColor.White, ConsoleColor.DarkYellow);
                Program.AddToLog("Saving Decrypted ASM...", ConsoleColor.Yellow);
                File.WriteAllBytes(Path.GetFileNameWithoutExtension(Program.path) + "_UniSheild-ExtractedASM.exe", Convert.FromBase64String(Encoding.Default.GetString(output)));
                Program.AddToLog("Decrypted ASM Saved! (Saved as: " + '"' + Path.GetFileName(Program.path) + "_UniSheild-ExtractedASM.exe" + '"', ConsoleColor.Green);
            }
        }

        public static bool isLicensingV2(MethodDef method, out string b64key)
        {
            b64key = "";
            Instruction[] insts = method.Body.Instructions.ToArray();
            for (int x = 0; x < insts.Count(); x++)
            {
                Instruction inst = Program.asm.EntryPoint.Body.Instructions[x];
                try
                {
                    if (inst.OpCode.Equals(OpCodes.Ldstr) &&
                        insts[x - 2].IsLdloc() && insts[x - 3].IsLdloc() && insts[x - 4].IsLdloc() &&
                        (insts[x + 1].OpCode.Equals(OpCodes.Call) && Utils.isFullMethod(insts[x + 1].Operand, Program.preset.DecodeBase64String)) &&
                        insts[x + 3].GetLdcI4Value() == 4 &&
                        insts[x + 4].OpCode.Equals(OpCodes.Rem) &&
                        insts[x + 6].OpCode.Equals(OpCodes.Xor)) { b64key = inst.Operand.ToString(); return true; }
                }
                catch { }
            }
            return false;
        }

        public static string GetLicensingType()
        {
            foreach (MethodDef methods in Program.asm.EntryPoint.DeclaringType.Methods)
            {
                if (!methods.HasBody) { continue; }
                foreach (string line in Utils.GetStringsFromMethod(methods))
                {
                    if (line.Contains("Win32_DiskDrive")) { string key = ""; if (isLicensingV2(Program.asm.EntryPoint, out key)) { return "USBV2 - " + key; } else { return "USB"; } }
                    if (line.Contains("Win32_Processor") || line.Contains("Win32_BIOS")) { string key = "";  if (isLicensingV2(Program.asm.EntryPoint, out key)) { return "HWIDLockedV2 - " + key; } else { return "HWIDLocked"; } }
                    if (line.Contains("License Not Found")) { string key = ""; if (isLicensingV2(Program.asm.EntryPoint, out key)) { return "LicenseFileV2 - " + key; } else { return "LicenseFile"; } }
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
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using UniShield.Protections;
using System.IO;
using System.Windows.Forms;
using UniShield.Rendering;
using UniShield.Helpers;
using System.Drawing;

namespace UniShield
{
    class Program
    {
        public static ModuleDefMD asm;
        public static string path;
        public static Presets preset = new Presets();
        public static Config config = new Config();
        private static FileBrowser filebrowser = new FileBrowser();
        private static void AddLogSeparator() { AddToLog("-------------------------------------------------------------------------------------------------", ConsoleColor.DarkGray); }

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "UniShield - https://github.com/miso-xyz/UniShield";
            Console.WindowWidth = 160;
            Console.WindowHeight = 50;
            Console.BufferHeight = 50;
            Console.CursorVisible = false;
            if (File.Exists("config.txt")) { config.Read("config.txt"); AddToLog("'config.txt' Loaded!", ConsoleColor.Green); } else { AddToLog("Coudn't find config file, using default config", ConsoleColor.DarkRed); }
            if (File.Exists("preset.txt")) { preset.Read("preset.txt"); AddToLog("'preset.txt' Loaded!", ConsoleColor.Green); } else { AddToLog("Coudn't find preset file, using default preset", ConsoleColor.DarkRed); }
            AddToLog("");
            AddToLog("Rendering Objects...", ConsoleColor.Yellow);
            PrintCopypasta();
            DrawBorder();
            DrawAbout();
            AddLogSeparator();
            if (args.Count() == 0) { AddToLog("No input file found!", ConsoleColor.Red); SetStatusText("No input file found!", ConsoleColor.White, ConsoleColor.DarkRed); goto end; }
            AddToLog("Reading " + '"' + Path.GetFileName(args[0]) + '"' + "...", ConsoleColor.Yellow);
            SetStatusText("Reading " + '"' + Path.GetFileName(args[0]) + '"' + "...", ConsoleColor.White, ConsoleColor.DarkYellow);
            path = args[0];
            try { asm = ModuleDefMD.Load(args[0]); }
            catch (Exception ex)
            {
                SetStatusText("Failed to read " + '"' + Path.GetFileName(args[0]) + '"' + "!", ConsoleColor.White, ConsoleColor.DarkRed);
                AddToLog("Failed to read " + '"' + Path.GetFileName(args[0]) + '"' + "!", ConsoleColor.Red);
                AddToLog("  └ '" + ex.Message + "'", ConsoleColor.DarkRed);
                goto end;
            }
            AddToLog('"' + Path.GetFileName(args[0]) + '"' + " Loaded!", ConsoleColor.Green);
            AddLogSeparator();
            //InitializeDrawingEnv();
            AddToLog("Checking if packed...", ConsoleColor.Yellow);
            SetStatusText("Checking if packed...", ConsoleColor.White, ConsoleColor.DarkYellow);
            bool checkIfPacked = Licensing.GetLicensingType() != "???";
            if (checkIfPacked)
            {
                byte[] decryptedASM = null;
                AddToLog("Application is Packed - Licensing Protection Found!", ConsoleColor.Green);
                AddToLog("");
                SetStatusText("Figuring out Licensing Type...", ConsoleColor.White, ConsoleColor.DarkYellow);
                switch (Licensing.GetLicensingType())
                {
                    case "USB":
                        AddToLog("  - Encrypted using Removable Drive HWID (Hardware ID)", ConsoleColor.Yellow);
                        AddToLog("");
                        SetStatusText("Calculating Removable Drives HWID...", ConsoleColor.White, ConsoleColor.DarkMagenta);
                        EncryptionHelper.RemovableDriveHWID[] removableDrivesHWIDs = EncryptionHelper.GetRemovableDrivesHWID();
                        if (removableDrivesHWIDs.Count() == 0) { AddToLog("No Removable Drives Found!", ConsoleColor.Red); break; }
                        foreach (EncryptionHelper.RemovableDriveHWID HWID in removableDrivesHWIDs)
                        {
                            SetStatusText("Attempting Decryption - " + HWID.drive.Name, ConsoleColor.White, ConsoleColor.DarkYellow);
                            AddToLog("Attempting Decryption - " + HWID.drive.Name, ConsoleColor.DarkYellow);
                            try
                            {
                                decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), HWID.HWID, Licensing.GetEncryptedASMIV());
                                AddToLog("");
                                AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                                AddToLog("  ├ Drive Letter: " + HWID.drive.Name, ConsoleColor.Magenta);
                                AddToLog("  └ Drive HWID:   " + HWID.HWID, ConsoleColor.DarkMagenta);
                                AddToLog("");
                                break;
                            }
                            catch { AddToLog("Invalid HWID!", ConsoleColor.DarkRed); AddToLog(""); }
                        }
                        if (decryptedASM == null) { AddToLog("No Removable Drives were valid!", ConsoleColor.Red); }
                        break;
                    case "HWIDLocked":
                        AddToLog("  - Encrypted using Computer HWID (Hardware ID)", ConsoleColor.Yellow);
                        AddToLog("");
                        try
                        {
                            SetStatusText("Calculating HWID...", ConsoleColor.White, ConsoleColor.DarkMagenta);
                            string compHWID = EncryptionHelper.GetHWID();
                            SetStatusText("Attempting Decryption...", ConsoleColor.White, ConsoleColor.DarkYellow);
                            AddToLog("Attempting Decryption...", ConsoleColor.DarkYellow);
                            decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), compHWID, Licensing.GetEncryptedASMIV());
                            AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                            AddToLog("  └ HWID: " + compHWID, ConsoleColor.DarkMagenta);
                            AddToLog("");
                        }
                        catch { AddToLog("Failed to decrypt Packed Assembly!", ConsoleColor.Red); SetStatusText("Decryption Failed!", ConsoleColor.White, ConsoleColor.DarkRed); }
                        break;
                    case "LicenseFile":
                        AddToLog("  - Encrypted using License File", ConsoleColor.Yellow);
                        AddToLog("");
                        string file = "";
                        AddToLog("    Searching for License.dat...", ConsoleColor.Yellow);
                        if (!File.Exists("License.dat") || !File.Exists(Directory.GetParent(Path.GetFullPath(path)).FullName + @"\License.dat"))
                        {
                            AddToLog("    Coudn't Find License.dat in Application Folder, you'll have to locate it yourself", ConsoleColor.Red);
                            AddToLog("");
                            if (config.CustomFileBrowser)
                            {
                                FileBrowser.ShowTutorial();
                                AddToLog("");
                                AddLogSeparator();
                                SetStatusText("Waiting for User Input...", ConsoleColor.White, ConsoleColor.DarkCyan);
                                filebrowser.title = "Select License File";
                                filebrowser.defaultExtFilter = 1;
                                filebrowser.ListDrives();
                                file = filebrowser.Initialize();
                                DrawAbout();
                                if (file == null) { AddToLog("Operation Cancelled!", ConsoleColor.Red); SetStatusText("Operation Cancelled!", ConsoleColor.White, ConsoleColor.DarkRed); goto end; }
                            }
                            else
                            {
                                OpenFileDialog ofd = new OpenFileDialog();
                                ofd.Filter = "License File|License.dat|All Files|*.*";
                                SetStatusText("Waiting for User Input...", ConsoleColor.White, ConsoleColor.DarkCyan);
                                if (ofd.ShowDialog() == DialogResult.OK) { file = ofd.FileName; }
                                else { AddToLog("Operation Cancelled!", ConsoleColor.Red); SetStatusText("Operation Cancelled!", ConsoleColor.White, ConsoleColor.DarkRed); goto end; }
                            }
                        }
                        else { AddToLog("    License.dat Found!", ConsoleColor.Green); AddToLog(""); }
                        if (File.Exists("License.dat")) { file = "License.dat"; }
                        else if (File.Exists(Directory.GetParent(Path.GetFullPath(path)).FullName)) { file = Directory.GetParent(Path.GetFullPath(path)).FullName + "\\License.dat"; }
                        SetStatusText("Attempting Decryption...", ConsoleColor.White, ConsoleColor.DarkYellow);
                        AddToLog("Attempting Decryption...", ConsoleColor.DarkYellow);
                        try
                        {
                            decryptedASM = EncryptionHelper.DecryptPackedASM(Licensing.GetEncryptedASM(), File.ReadAllText(file), Licensing.GetEncryptedASMIV());
                            SetStatusText("File Successfully Decrypted...", ConsoleColor.White, ConsoleColor.DarkGreen);
                            AddToLog("Packed Assembly Successfully Decrypted!", ConsoleColor.Green);
                            AddToLog("  └ Decryption Key: '" + File.ReadAllText(file) + "'", ConsoleColor.DarkMagenta);
                            AddToLog("");
                        }
                        catch { AddToLog("Failed to decrypt Packed Assembly!", ConsoleColor.Red); SetStatusText("Decryption Failed!", ConsoleColor.White, ConsoleColor.DarkRed); Console.ReadKey(); Application.Exit(); }
                        break;
                }
                if (decryptedASM != null)
                {
                    SetStatusText("Saving File...", ConsoleColor.White, ConsoleColor.DarkYellow);
                    AddToLog("Saving Decrypted ASM...", ConsoleColor.Yellow);
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(path) + "_UniSheild-ExtractedASM.exe", Convert.FromBase64String(Encoding.Default.GetString(decryptedASM)));
                    AddToLog("Decrypted ASM Saved! (Saved as: " + '"' + Path.GetFileName(path) + "_UniSheild-ExtractedASM.exe" + '"', ConsoleColor.Green);
                }
            }
            else
            {
                AddToLog("Application not Packed!", ConsoleColor.Green);
                AddToLog("");
                //AddToLog("Now Renaming Methods...", ConsoleColor.Yellow);
                //Renaming.Fix();
                if (config.Prot_Base64Strings)
                {
                    AddToLog("Now Decoding Base64 Strings...", ConsoleColor.Yellow);
                    Base64.Fix();
                    AddToLog("");
                }
                if (config.Prot_AntiDe4Dots)
                {
                    AddToLog("Now Removing AntiDe4Dots...", ConsoleColor.Yellow);
                    AntiDe4dot.Fix();
                    AddToLog("");
                }
                if (config.Prot_FakeAtrribs)
                {
                    AddToLog("Now Removing Fake Attributes...", ConsoleColor.Yellow);
                    FakeAttribs.Fix();
                    AddToLog("");
                }
                if (config.Prot_CFlow)
                {
                    AddToLog("Now Cleaning Control Flow...", ConsoleColor.Yellow);
                    CFlow.Fix();
                    AddLogSeparator();
                }
                AddToLog("Now saving Cleaned ASM...", ConsoleColor.Yellow);
                SetStatusText("Now saving Cleaned ASM...", ConsoleColor.White, ConsoleColor.DarkYellow);
                string outputName = path + "_UniShield_IL-Cleaned.exe";
                try
                {
                    if (asm.IsILOnly) { asm.Write(outputName); }
                    else { outputName = path + "_UniShield_Native-Cleaned.exe"; asm.NativeWrite(outputName); }
                }
                catch (Exception ex)
                {
                    SetStatusText("Failed to save cleaned ASM!", ConsoleColor.White, ConsoleColor.DarkRed);
                    AddToLog("Failed to save cleaned ASM!", ConsoleColor.Red);
                    AddToLog("  └ '" + ex.Message + "'", ConsoleColor.DarkRed);
                    goto end;
                }
                AddToLog("Cleaned ASM Successfully Saved!", ConsoleColor.Green);
            }
            SetStatusText("Done!", ConsoleColor.White, ConsoleColor.DarkGreen);
        end:
            AddLogSeparator();
            AddToLog("Press any key to exit...");
            Console.ReadKey();
        }

        class LogLine
        {
            public LogLine(string data, ConsoleColor back, ConsoleColor fore)
            {
                text = data;
                backColor = back;
                foreColor = fore;
            }

            public string text;
            public ConsoleColor backColor;
            public ConsoleColor foreColor;
        }

        static List<LogLine> LogText = new List<LogLine>();
        public static void AddToLog(string text, ConsoleColor foreColor = ConsoleColor.Gray, ConsoleColor backColor = ConsoleColor.Black)
        {
            LogText.Insert(0,new LogLine(text, backColor, foreColor));
            UpdateLog();
        }

        static void UpdateLog(bool endLine = true)
        {
            Console.SetCursorPosition(158, Console.WindowHeight - 2);
            for (int x = 0; x <= LogText.Count() - 1; x++)
            {
                LogLine currentLine = LogText[x];
                if (Console.CursorTop < 11) { break; }
                Console.SetCursorPosition(47, (Console.WindowHeight - 2) - x);
                string text = currentLine.text;
                /*if (!currentLine.endLine)
                {
                    for (int x_ = x; x_ < LogText.Count(); x_++)
                    {
                        LogLine curConLine = LogText[x_];
                        Console.BackgroundColor = curConLine.backColor;
                        Console.ForegroundColor = curConLine.foreColor;
                        string conText = curConLine.text;
                        if (conText.Length > Console.WindowWidth - Console.CursorLeft) { try { conText = conText.Substring(0, (Console.WindowWidth - Console.CursorLeft) - 3) + "..."; } catch { continue; } }
                        Console.Write(conText);
                        if (curConLine.endLine) { break; }
                    }
                    Console.SetCursorPosition(47, (Console.WindowHeight - 2) - x);
                }*/
                if (text.Length > Console.WindowWidth - Console.CursorLeft) { try { text = text.Substring(0, (Console.WindowWidth - Console.CursorLeft) - 4) + "..."; } catch { continue; } }
                Console.BackgroundColor = currentLine.backColor;
                Console.ForegroundColor = currentLine.foreColor;
                Console.Write(text);
                /*char[] textArr = currentLine.text.ToCharArray();
                for (int x_CH = 0; x_CH < textArr.Count(); x_CH++)
                {
                    char ch = textArr[x_CH];
                    Console.Write(ch);
                    if (Console.CursorLeft > 45) { Console.CursorLeft -= 2; continue; }
                    else { break; }
                }*/
                FinishLinePadding();
                Console.WriteLine();
                Console.CursorTop -= 2;
            }
            Console.SetCursorPosition(0, 0);
        }

        static void DrawAbout()
        {
            Console.SetCursorPosition(0, 0);
            if (File.Exists("drawing.png")) { new AsciiImage(new Bitmap(Image.FromFile("drawing.png"), 0x2b, 0x19)).PrintAscii(true); }
            Console.WriteLine(); Console.WriteLine();
            for (int x = 0; x < 45; x++)
            {
                Console.CursorLeft = x;
                Console.Write("─");
            }
            Console.CursorTop += 2;
            Console.CursorLeft = 0;
            Console.WriteLine(" - UniShield");
            Console.Write("    └ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("https://github.com/miso-xyz/UniShield");
            Console.ResetColor();
            Console.Write("    └ Version: ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine(" 1.0 ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(" fuck the following:");
            Console.WriteLine("   - mayonaise - suck bollux xd!11!1");
            Console.WriteLine("   - horni     - he weird");
            Console.WriteLine("   - skull     - beat mah meet");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(" big forerheade kiss t o:");
            Console.WriteLine("   - drako     - actuél webb");
            Console.WriteLine("   - pikami    - redplide mf");
            Console.WriteLine();
            Console.WriteLine(" hnoralbe mantion");
            Console.WriteLine("   - sar       - forgor her bday but");
            Console.WriteLine("                 is really cool");
            Console.WriteLine();
            Console.WriteLine("i am ahve stork");
        }

        static void DrawBorder()
        {
            Console.ResetColor();
            Console.CursorLeft = 45;
            Console.Write("├");
            FinishLinePadding(1, false, '─');
            Console.Write("┘");
            for (int y = 11; y < Console.WindowHeight - 1; y++)
            {
                Console.CursorTop = y;
                Console.CursorLeft = 45;
                if (y == 27) { Console.Write("┤"); }
                else { Console.Write("│"); }
            }
        }

        static void PrintCopypasta()
        {
            int yPos = Console.CursorTop;
            Console.SetCursorPosition(45, 0);
            Console.ResetColor();
            Console.Write("│");
            Console.Write(" I've come to make an announcement");
            FinishLinePadding();
            Console.ResetColor();
            Console.Write("│");
            for (int y = Console.CursorTop; y < 10; y++)
            {
                Console.CursorLeft = 45;
                switch (y)
                {
                    case 1:
                        Console.Write("├");
                        FinishLinePadding(1, false, '─');
                        Console.Write("┤");
                        break;
                    case 2:
                        //Console.CursorTop++;
                        Console.Write("│ Shadow the Hedgehog's a bitch ass motherfucker. He pissed on my wife. That's right." ); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ He took his hedgehog fuckin' quilly dick out and he pissed on my FUCKING wife, and he said his dick was"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ THIS BIG, and I said that's disgusting. So I'm making a callout post on my Twitter.com. Shadow the Hedgehog,"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ you got a small dick. It's the size of this walnut except WAY smaller. And guess what? Here's what my dong looks"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ like. That's right, baby. Tall points, no quills, no pillows, look at that, it looks like two balls and a bong."); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ He fucked my wife, so guess what, I'm gonna fuck the earth. That's right, this is what you get! My SUPER LASER"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ PISS! Except I'm not gonna piss on the earth. I'm gonna go higher. I'm pissing on the MOOOON! How do you like"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        Console.Write("│ that, OBAMA? I PISSED ON THE MOON, YOU IDIOT! You have 23 hours before the piss DROPLETS hit the fucking earth"); FinishLinePadding(); Console.Write("│"); Console.CursorLeft = 45;
                        break; //  
                }
            }
        }

        static void FinishLinePadding(int posLeft = 1, bool finishLine = false, char ch = ' ') { for (int x = Console.CursorLeft; x < Console.WindowWidth - posLeft; x++) { Console.Write(ch); } if (finishLine) { Console.WriteLine(); } }

        public static void SetStatusText(string text, ConsoleColor foreColor, ConsoleColor backColor)
        {
            int yPos = Console.CursorTop;
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ForegroundColor = foreColor;
            Console.BackgroundColor = backColor;
            Console.Write(" " + text);
            FinishLinePadding();
            Console.ResetColor();
            Console.CursorTop = yPos;
        }
    }
}

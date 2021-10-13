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
using System.Threading;
using System.Diagnostics;

namespace UniShield
{
    class Program
    {
        public static ModuleDefMD asm;
        public static string path;
        public static string appPath;
        public static Presets preset = new Presets();
        public static Config config = new Config();

        public static void AddLogSeparator() { string text = ""; int max = Console.WindowWidth - (Console.CursorLeft + 1 + Convert.ToInt32(config.MinimalLayout)); if (!config.MinimalLayout) { max = Console.WindowWidth - 48; } for (int x = 0; x < max; x++) { text += "-"; } AddToLog(text, ConsoleColor.DarkGray); }

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            appPath = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.GetFullPath(args[0]);
            Console.Clear();
            try { config.Read(appPath + @"\config.txt"); } catch { }
            try { preset.Read(appPath + @"\preset.txt"); } catch { }
            if (config.MinimalLayout)
            {
                Console.Write(" UniShield v" + Updater.CurrentVersion + " - ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(Updater.RepoLink);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  └ Using Minimal Layout");
                Console.ResetColor();
                Console.WriteLine();
            }
            Console.Title = "UniShield - " + Updater.RepoLink;
            Console.CursorVisible = false;
            CheckForUpdates();
            if (!config.MinimalLayout) { Console.WindowWidth = 160; Console.WindowHeight = 50; Console.BufferHeight = 50; }
            if (!config.MinimalLayout)
            {
                if (File.Exists(appPath + @"\config.txt")) { AddToLog("'config.txt' Loaded!", ConsoleColor.Green); } else { AddToLog("Coudn't find config file, using default config", ConsoleColor.DarkRed); }
                if (File.Exists(appPath + @"\preset.txt")) { AddToLog("'preset.txt' Loaded!", ConsoleColor.Green); } else { AddToLog("Coudn't find preset file, using default preset", ConsoleColor.DarkRed); }
                if (File.Exists(appPath + @"\drawing.png")) { AddToLog("Found image file!", ConsoleColor.Green); } else { AddToLog("Coudn't find image file", ConsoleColor.DarkRed); }
                AddToLog("");
                AddToLog("Rendering Objects...", ConsoleColor.Yellow);
                PrintCopypasta();
                DrawBorder();
                DrawAbout();
                AddLogSeparator();
            }
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
            if (Licensing.GetLicensingType() != "???") { Licensing.Fix(); }
            else
            {
                AddToLog("Application not Packed!", ConsoleColor.Green);
                AddToLog("");

                //AddToLog("Now Renaming Methods...", ConsoleColor.Yellow);
                //Renaming.Fix();
                if (config.Prot_ILDasm) { AddToLog("Now Removing ILDasm...", ConsoleColor.Yellow); ILDasm.Fix(); AddToLog(""); }
                if (config.Prot_Calli) { AddToLog("Now Callis...", ConsoleColor.Yellow); Calli.Fix(); AddToLog(""); }
                if (config.Prot_Base64Strings) { AddToLog("Now Decoding Base64 Strings...", ConsoleColor.Yellow); Base64.Fix(); AddToLog(""); }
                if (config.Prot_AntiDe4Dots) { AddToLog("Now Removing AntiDe4Dots...", ConsoleColor.Yellow); AntiDe4dot.Fix(); AddToLog(""); }
                if (config.Prot_FakeAtrribs) { AddToLog("Now Removing Fake Attributes...", ConsoleColor.Yellow); FakeAttribs.Fix(); AddToLog(""); }
                if (config.Prot_IntConf) { AddToLog("Now Fixing Int Confusion...", ConsoleColor.Yellow); IntConfusion.Fix(); AddToLog(""); }
                if (config.Prot_CFlow) { AddToLog("Now Cleaning Control Flow...", ConsoleColor.Yellow); CFlow.Fix(); AddLogSeparator(); }

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
            if (config.MinimalLayout)
            {
                Console.ForegroundColor = foreColor;
                Console.BackgroundColor = backColor;
                Console.WriteLine(" " + text);
                Console.ResetColor();
            }
            else { LogText.Insert(0, new LogLine(text, backColor, foreColor)); UpdateLog(); }
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

        public static void DrawAbout()
        {
            if (config.MinimalLayout) { return; }
            Console.SetCursorPosition(0, 0);
            if (File.Exists(appPath + @"\drawing.png")) { new AsciiImage(new Bitmap(Image.FromFile(appPath + @"\drawing.png"), 0x2b, 0x19)).PrintAscii(true); }
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
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("https://github.com/miso-xyz/UniShield");
            Console.ResetColor();
            Console.Write("    └ Version: ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(" v" + Updater.CurrentVersion + " ");
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
            if (config.MinimalLayout) { return; }
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
            if (config.MinimalLayout) { return; }
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

        public static void CheckForUpdates()
        {
            if (!Updater.HasInternetConnection()) { return; }
            Updater update = new Updater(Updater.GetUpdate());
            if (!update.IsRunningLatest())
            {
                if (DrawUpdateDialog(update))
                {
                    update.DownloadUpdate();
                    Application.Exit();
                }
            }
        }

        public static bool DrawUpdateDialog(Updater updaterClass)
        {
            Console.Clear();
            Console.WindowWidth = 120; Console.WindowHeight = 18; Console.BufferHeight = 18;
            int buttonSelected = 1;
            while (true)
            {
                Console.SetCursorPosition(0, 1);
                if (File.Exists(appPath + @"\info.png")) { new AsciiImage(new Bitmap(Image.FromFile(appPath + @"\info.png"), 25, 16)).PrintAscii(true, 2); }
                Console.SetCursorPosition(30, 2);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("New Update Found!");
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.SetCursorPosition(30, 4);
                Console.Write("Current Version: " + Updater.CurrentVersion);
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.SetCursorPosition(30, 5);
                Console.Write(" Latest Version: " + updaterClass.LatestVersion);
                Console.SetCursorPosition(30, 7);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Size: " + updaterClass.UpdateSize);
                Console.ResetColor();
                Console.SetCursorPosition(30, 9);
                foreach (string line in updaterClass.ChangeLog)
                {
                    Console.CursorLeft = 30;
                    if (line.StartsWith("<"))
                    {
                        Console.ResetColor();
                        string[] args = line.Replace("<", null).Replace(">", null).Split(',');
                        foreach (string argLine in args)
                        {
                            string p1 = argLine.Split('=')[0];
                            string p2 = argLine.Split('=')[1];
                            ConsoleColor color = ConsoleColor.Gray;
                            switch (p2)
                            {
                                case "Green": color = ConsoleColor.Green; break;
                                case "DarkGreen": color = ConsoleColor.DarkGreen; break;
                                case "Cyan": color = ConsoleColor.Cyan; break;
                                case "DarkCyan": color = ConsoleColor.DarkCyan; break;
                                //case "Gray": color = ConsoleColor.Gray; break;
                                case "DarkGray": color = ConsoleColor.DarkGray; break;
                                case "Yellow": color = ConsoleColor.Yellow; break;
                                case "DarkYellow": color = ConsoleColor.DarkYellow; break;
                                case "Blue": color = ConsoleColor.Blue; break;
                                case "DarkBlue": color = ConsoleColor.DarkBlue; break;
                                case "Black": color = ConsoleColor.Black; break;
                                case "White": color = ConsoleColor.White; break;
                                case "Red": color = ConsoleColor.Red; break;
                                case "DarkRed": color = ConsoleColor.DarkRed; break;
                                case "Magenta": color = ConsoleColor.Magenta; break;
                                case "DarkMagenta": color = ConsoleColor.Magenta; break;
                            }
                            switch (p1)
                            {
                                case "FG": Console.ForegroundColor = color; break;
                                case "BG": Console.BackgroundColor = color; break;
                            }
                        }
                    }
                    else { Console.WriteLine(" " + line + " "); }
                }
                ConsoleColor bg = ConsoleColor.Black, fg = ConsoleColor.Yellow;
                if (buttonSelected == 0) { bg = ConsoleColor.Yellow; fg = ConsoleColor.Black; } Console.SetCursorPosition(100, 16); DrawUpdateButtons(" No ", bg, fg);
                if (buttonSelected == 1) { bg = ConsoleColor.Yellow; fg = ConsoleColor.Black; } Console.SetCursorPosition(110, 16); DrawUpdateButtons(" Yes ", bg, fg);
            wait:
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.LeftArrow:
                        buttonSelected = 0;
                         Console.SetCursorPosition(100, 16); DrawUpdateButtons(" No ", ConsoleColor.Yellow, ConsoleColor.Black);
                         Console.SetCursorPosition(110, 16); DrawUpdateButtons(" Yes ", ConsoleColor.Black, ConsoleColor.Yellow);
                         goto wait;
                    case ConsoleKey.RightArrow:
                         buttonSelected = 1;
                         Console.SetCursorPosition(100, 16); DrawUpdateButtons(" No ", ConsoleColor.Black, ConsoleColor.Yellow);
                         Console.SetCursorPosition(110, 16); DrawUpdateButtons(" Yes ", ConsoleColor.Yellow, ConsoleColor.Black);
                         goto wait;
                    case ConsoleKey.Enter:
                        Console.Clear();
                        return Convert.ToBoolean(buttonSelected);
                }
            }
        }

        private static void DrawUpdateButtons(string text, ConsoleColor backcolor, ConsoleColor foreColor)
        {
            Console.BackgroundColor = backcolor;
            Console.ForegroundColor = foreColor;
            Console.Write(text);
            Console.ResetColor();
        }

        static void FinishLinePadding(int posLeft = 1, bool finishLine = false, char ch = ' ') { for (int x = Console.CursorLeft; x < Console.WindowWidth - posLeft; x++) { Console.Write(ch); } if (finishLine) { Console.WriteLine(); } }

        public static void SetStatusText(string text, ConsoleColor foreColor, ConsoleColor backColor)
        {
            if (config.MinimalLayout) { return; }
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

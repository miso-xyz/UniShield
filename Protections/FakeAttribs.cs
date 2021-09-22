using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Protections
{
    class FakeAttribs
    {
        private static string[] knownFakeAttribNames = new string[] { "ConfusedByAttribute", "YanoAttribute", "NetGuard", "DotfuscatorAttribute", "BabelAttribute" };

        public static void Fix()
        {
            int count = 0;
            int fakeAttribsCount = 0;
            for (int x = 0; x < Program.asm.Types.Count(); x++)
            {
                TypeDef type = Program.asm.Types[x];
                Program.SetStatusText("Searching Useless Types - " + type.Name, ConsoleColor.Black, ConsoleColor.Magenta);
                if (type == Program.asm.GlobalType) { continue; }
                if (!type.HasMethods)
                {
                    string text = "[FakeAttributes]: Removed '" + type.Name + "'!";
                    if (knownFakeAttribNames.Contains(type.Name.ToString())) { text += " - Known Fake Attribute"; fakeAttribsCount++; }
                    Program.asm.Types.Remove(type); x--; count++;
                    if (Program.config.DetailedLog) { Program.AddToLog(text, ConsoleColor.Green); }
                }
            }
            if (count != 0) { Program.AddToLog("Removed " + count + " Useless Types!", ConsoleColor.Green); if (fakeAttribsCount != 0) { Program.AddToLog("  └ Removed " + fakeAttribsCount + " Fake Attributes!", ConsoleColor.DarkGreen); } } else { Program.AddToLog("No Useless Types Found!"); }
        }
    }
}

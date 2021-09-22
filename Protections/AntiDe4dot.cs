using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Protections
{
    class AntiDe4dot
    {
        public static void Fix()
        {
            int count = 0;
            for (int x = 0; x < Program.asm.Types.Count(); x++ )
            {
                TypeDef type = Program.asm.Types[x];
                Program.SetStatusText("Removing AntiDe4Dots... - "+type.Name, ConsoleColor.Black, ConsoleColor.Cyan);
                if (type.HasInterfaces)
                {
                    foreach (InterfaceImpl interface_ in type.Interfaces) { if (interface_.Interface.ResolveTypeDef() == type) { if (Program.config.DetailedLog) { Program.AddToLog("[AntiDe4Dot]: Removed '" + type.Name + "'!", ConsoleColor.Green); } Program.asm.Types.Remove(type); x--; count++; } }
                }
            }
            if (count != 0) { Program.AddToLog("Removed " + count + " AntiDe4Dots!", ConsoleColor.Green); } else { Program.AddToLog("No AntiDe4Dots Found!"); }
        }
    }
}

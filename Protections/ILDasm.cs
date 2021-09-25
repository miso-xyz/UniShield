using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UniShield.Helpers;

namespace UniShield.Protections
{
    class ILDasm
    {
        public static void Fix()
        {
            int count = 0;
            foreach (ModuleDef module in Program.asm.Assembly.Modules)
            {
                if (Program.config.DetailedLog) { Program.AddToLog("Searching ILDasm - " + module.Name, ConsoleColor.Green); }
                Program.SetStatusText("Searching ILDasm - " + module.Name, ConsoleColor.White, ConsoleColor.DarkYellow);
                if (module.HasCustomAttributes)
                {
                    for (int x = 0; x < module.CustomAttributes.Count(); x++)
                    {
                        CustomAttribute CA = module.CustomAttributes[x];
                        if (((TypeRef)CA.AttributeType).Namespace == Program.preset.SupressIldasmAttribute.Namespace &&
                            ((TypeRef)CA.AttributeType).Name == Program.preset.SupressIldasmAttribute.Class)
                        {
                            if (Program.config.DetailedLog) { Program.AddToLog("[ILDasm]: Removed ILDasm!", ConsoleColor.Green); }
                            module.CustomAttributes.Remove(CA); count++;
                        }
                    }
                }
            }
            if (count != 0) { Program.AddToLog(count + " ILDasm Removed", ConsoleColor.Green); } else { Program.AddToLog("No ILDasm Removed!"); }
        }
    }
}

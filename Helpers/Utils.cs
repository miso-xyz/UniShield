using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace UniShield.Helpers
{
    class Utils
    {
        public static bool isFullClass(object instOp, Presets.ClassRef ClassRef)
        {
            if ((((TypeRef)((MemberRef)instOp).Class)).Namespace == ClassRef.Namespace &&
                ((MemberRef)instOp).Class.Name == ClassRef.Class) { return true; }
            return false;
        }
        public static bool isFullMethod(object instOp, Presets.MethodRef MethodRef)
        {
            if ((((TypeRef)((MemberRef)instOp).Class)).Namespace == MethodRef.Namespace &&
                ((MemberRef)instOp).Class.Name == MethodRef.Class &&
                ((MemberRef)instOp).Name == MethodRef.Method) { return true; }
            return false;
        }

        public static bool isClass(Instruction inst, string ClassName)
        {
            try { return ((MemberRef)inst.Operand).Class.Name == ClassName; }
            catch { return false; }
        }
        public static bool isMethod(Instruction inst, string MethodName)
        {
            try { return ((MemberRef)inst.Operand).Name == MethodName; }
            catch { return false; }
        }
        public static bool isNamespace(Instruction inst, string Namespace)
        {
            try { return (((TypeRef)((MemberRef)inst.Operand).Class)).Namespace == Namespace; }
            catch { return false; }
        }

        public static object FormatCall(string line)
        {
            string[] split = line.Split(',');
            switch (split.Count())
            {
                case 2:
                    return new UniShield.Presets.ClassRef(split[0], split[1]);
                case 3:
                    return new UniShield.Presets.MethodRef(split[0], split[1], split[2]);
            }
            return null;
        }

        public static string[] GetStringsFromMethod(MethodDef method)
        {
            List<string> strList = new List<string>();
            foreach (Instruction inst in method.Body.Instructions) { if (inst.OpCode.Equals(OpCodes.Ldstr)) { strList.Add(inst.Operand.ToString()); } }
            return strList.ToArray();
        }
    }
}

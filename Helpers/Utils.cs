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

        public static string FormatStatusString(string text, int charLimit = 60, char fillChar = ' ')
        {
            if (text.Length > charLimit) { text = text.Substring(0, charLimit); }
            else { for (int x_char = 0; x_char < text.Length; x_char++) { if (text.Length == charLimit) { break; } text += fillChar; } }
            return text;
        }

        public static string FormatStatusString(string typeName, string methodName, int charLimit = 60, char fillChar = ' ')
        {
            string methodNameFormatted = typeName + "." + methodName;
            if (methodNameFormatted.Length > charLimit) { methodNameFormatted = methodNameFormatted.Substring(0, charLimit); }
            else { for (int x_char = 0; x_char < methodNameFormatted.Length; x_char++) { if (methodNameFormatted.Length == charLimit) { break; } methodNameFormatted += fillChar; } }
            return methodNameFormatted;
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

        public static Instruction SimplifyBR(Instruction brInst)
        {
            Instruction inst = (Instruction)brInst;
            while (true)
            {
                if (inst.OpCode.Equals(OpCodes.Br) || inst.OpCode.Equals(OpCodes.Br_S)) { inst = (Instruction)inst; }
                else { break; }
            }
            return inst;
        }
    }
}

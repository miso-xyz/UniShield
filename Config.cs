using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UniShield
{
    class Config
    {
        public bool DetailedLog = false;
        public bool CustomFileBrowser = true;

        public bool Prot_PackedRemDrives = true;
        public bool Prot_PackedComputerHWID = true;
        public bool Prot_PackedLicFile = true;
        public bool Prot_AntiDe4Dots = true;
        public bool Prot_FakeAtrribs = true;
        public bool Prot_JunkMethods = true;
        public bool Prot_CFlow = true;
        public bool Prot_Base64Strings = true;
        public bool Prot_ILDasm = true;
        public bool Prot_IntConf = true;

        public void Read(string path)
        {
            string[] fileData = File.ReadAllLines(path);
            foreach (string line in fileData)
            {
                if (line.StartsWith("[") || line.StartsWith("//") || line == "") { continue; }
                string p1 = line.Split("=".ToCharArray())[0].Replace("\t", null);
                string p2 = line.Split("=".ToCharArray())[1].Remove(0, 1);
                switch (p1)
                {
                    case "DetailedLog":
                        DetailedLog = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "UseCustomFileBrowser":
                        CustomFileBrowser = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "Packed_RemovableDrive":
                        Prot_PackedRemDrives = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "Packed_ComputerHWID":
                        Prot_PackedComputerHWID = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "Packed_LicenseFile":
                        Prot_PackedLicFile = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "AntiDe4Dots":
                        Prot_AntiDe4Dots = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "FakeAttribs":
                        Prot_FakeAtrribs = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "JunkMethods":
                        Prot_JunkMethods = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "Base64Strings":
                        Prot_Base64Strings = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "CFlow":
                        Prot_CFlow = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "ILDasm":
                        Prot_ILDasm = Convert.ToBoolean(int.Parse(p2));
                        break;
                    case "IntConfusion":
                        Prot_IntConf = Convert.ToBoolean(int.Parse(p2));
                        break;
                }
            }
        }

    }
}

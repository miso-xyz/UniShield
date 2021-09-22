using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.IO;

namespace UniShield.Helpers
{
    class EncryptionHelper
    {
        public const string HWIDHash_Key = "bAI!J6XwWO&A";

        public static string HashHWID(string HWID, string HashKey = HWIDHash_Key)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("bAI!J6XwWO&A");
            byte[] array = new HMACSHA256 { Key = Encoding.ASCII.GetBytes(HashKey) }.ComputeHash(Encoding.UTF8.GetBytes(HWID));
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < array.Length; i++) { stringBuilder.Append(array[i].ToString("x2")); }
            return stringBuilder.ToString();
        }

        public static byte[] DecryptPackedASM(byte[] data, string DecKey, string IV)
        {
            using (SHA256CryptoServiceProvider sha256CryptoServiceProvider = new SHA256CryptoServiceProvider())
			{
				byte[] key = sha256CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(DecKey));
				using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider { Key = key, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
				{
					aesCryptoServiceProvider.IV = Encoding.ASCII.GetBytes(IV);;
					ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateDecryptor();
                    return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
				}
			}
        }

        public static string GetHWID()
        {
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            string cpuDetails = null;
            foreach (ManagementBaseObject managementBaseObject in managementObjectCollection)
            {
                ManagementObject managementObject = (ManagementObject)managementBaseObject;
                cpuDetails = managementObject["ProcessorType"].ToString() + managementObject["ProcessorId"].ToString();
            }
            ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            ManagementObjectCollection managementObjectCollection2 = managementObjectSearcher2.Get();
            string biosDetails = null;
            foreach (ManagementBaseObject managementBaseObject2 in managementObjectCollection2)
            {
                ManagementObject managementObject2 = (ManagementObject)managementBaseObject2;
                biosDetails = managementObject2["Manufacturer"].ToString() + managementObject2["Version"].ToString();
            }
            return HashHWID(cpuDetails + biosDetails);
        }

        public class RemovableDriveHWID
        {
            public RemovableDriveHWID(DriveInfo Drive, string hwid)
            {
                drive = Drive;
                HWID = hwid;
            }

            public DriveInfo drive;
            public string HWID;
        }

        public static RemovableDriveHWID[] GetRemovableDrivesHWID()
        {
            List<RemovableDriveHWID> HWIDs = new List<RemovableDriveHWID>();
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType != DriveType.Removable) { continue; }
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
                foreach (ManagementBaseObject managementBaseObject in managementObjectCollection)
                {
                    ManagementObject managementObject = (ManagementObject)managementBaseObject;
                    bool flag2 = managementObject["MediaType"].ToString() == "Removable Media";
                    if (flag2)
                    {
                        HWIDs.Add(new RemovableDriveHWID(driveInfo, HashHWID(driveInfo.TotalSize.ToString() + managementObject["SerialNumber"].ToString() + managementObject["PNPDeviceID"].ToString())));
                    }
                }
            }
            return HWIDs.ToArray();
        }

    }
}

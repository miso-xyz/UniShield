# UniShield
Deobfuscator for <a href="https://github.com/AhmedMinegames/NetShield_Protector">NetShield Protector</a>

# Notes
All 3 Licensing Protections use encryption, UniShield only extract the encrypted data & tries to decrypt with what's available.</br>(they're stupid protections anyway)</br>
- - - -
***The CFlow Protection currently present in UniShield is outdated***, It only currently works with `NetShield v1.0`</br>(caused by blocks being randomised after the mentioned version)
- - - -
# Supported Protections
Name      | Status     | Known Version Supported
--------- | ---------- | :----------: |
Licensing - HWID Locked (Computer) | Fully Supported, Requires valid Hardware | 1.0 -> 1.3.1
Licensing - HWID Locked (Removable Device) | Fully Supported, Requires valid Removable Device | 1.0 -> 1.3.1
Licensing - License Locked | Fully Supported, Requires valid License | 1.0 -> 1.3.1
| |
Anti-ILDasm | Fully Supported | 1.2 -> 1.3.1
Int Confusion | Fully Supported | 1.2 -> 1.3.1
Base64 String Encoding | Fully Supported | 1.0 -> 1.3.1
Anti-De4Dots | Fully Supported | 1.0 -> 1.3.1
Fake Attributes | Fully Supported | 1.0 -> 1.3.1
Junk Types | Fully Supported | 1.0 -> 1.3.1
CFlow | Well Supported for NetShield Protector v1.0, needs support for randomised Blocks. | 1.0 only

# Screenshots
<img src="https://i.imgur.com/KIFrqpV.png">
<img src="https://i.imgur.com/jNN333h.png">
<!--
<img src="https://i.imgur.com/xifRIhP.png">
<img src="https://i.imgur.com/fKkmyis.png">
-->

# Misc
<details>
  <summary>Default Configuration File (config.txt)</summary>
  <pre>[Rendering]
// Can get laggy if turned on
DetailedLog		= 0
[Misc]
UseCustomFileBrowser	= 1
[Protections]
// Renaming not supported since names are randomised
// You might need to do some small manual work to have the protected application running after cleaning CFlow.
Base64Strings		= 1
Packed_RemovableDrive	= 1
Packed_ComputerHWID	= 1
Packed_LicenseFile	= 1
AntiDe4Dots		= 1
FakeAttribs		= 1
JunkMethods		= 1
ILDasm			= 1
CFlow			= 1
IntConfusion		= 1</pre>
</details>
<details>
  <summary>Default Preset File (preset.txt)</summary>
  <pre>[TextEncoding]
Encoding_GetAscii		= System.Text,Encoding,get_ASCII
Encoding_GetBytes		= System.Text,Encoding,GetBytes
[Encryption]
SymmetricAlgorithm_Decryptor	= System.Security.Cryptography,SymmetricAlgorithm,CreateDecryptor
HMACSHA256_HashGen		= System.Security.Cryptography,HMACSHA256
SHA256_CryptoService		= System.Security.Cryptography,SHA256CryptoServiceProvider
[ILDasm]
SupressIldasmAttribute		= System.Runtime.CompilerServices,SuppressIldasmAttribute</pre>
</details>
<details>
  <summary>Default Image (drawing.png)</summary>
  <p>(Right click -> <code>Save Image as...</code>)</p>
  <hr>
  <img src="https://i.ibb.co/VxMjpDh/drawing.png">
  <hr>
</details>

# Credits
- <a href="https://github.com/Washi1337">Washi</a> - <a href="https://github.com/Washi1337/OldRod">OldRod</a> (<a href="https://github.com/Washi1337/OldRod/blob/840d11a7c0bf7fef4a9b2d2e7244cf3e01be6ecd/src/OldRod/ConsoleAsciiImage.cs">ConsoleAsciiImage</a>)</br>
- <a href="https://github.com/DevT02/">DevT02</a> - <a href="https://github.com/DevT02/Junk-Remover">Junk Remover</a> (Useless NOP Remover)
- <a href="https://github.com/0xd4d/dnlib">wtfsck/0xd4d</a> - <a href="https://github.com/0xd4d/dnlib">dnlib</a>
- <a href="https://github.com/AhmedMinegames/">AhmedMinegames</a> - <a href="https://github.com/AhmedMinegames/NetShield_Protector">NetShield Protector</a>

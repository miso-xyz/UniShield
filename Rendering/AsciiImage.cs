using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace UniShield.Rendering
{
    class AsciiImage
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public AsciiImage(Bitmap image)
		{
			if (image == null)
			{
				throw new ArgumentNullException("image");
			}
			this.Image = image;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x0000206E File Offset: 0x0000026E
        public Bitmap Image;

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002076 File Offset: 0x00000276
		public string CharacterMap
		{
			get
			{
				return " .:owM";
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002080 File Offset: 0x00000280
		public unsafe void PrintAscii(bool asciiMode, int xOffsetPrint = 0)
		{
			BitmapData info = this.Image.LockBits(new Rectangle(0, 0, this.Image.Width, this.Image.Height), ImageLockMode.ReadOnly, this.Image.PixelFormat);
			try
			{
				for (int y = 0; y < info.Height; y++)
				{
                    Console.CursorLeft += xOffsetPrint;
					for (int x = 0; x < info.Width; x++)
					{
						Color color = Color.FromArgb(*(int*)((void*)(info.Scan0 + y * info.Stride + x * 4)));
						if (asciiMode)
						{
							Console.ForegroundColor = this.GetClosestConsoleColor(color);
							Console.Write(this.CharacterMap[(int)((float)color.A / 255f * (float)(this.CharacterMap.Length - 1))]);
						}
						else
						{
							Console.BackgroundColor = this.GetClosestConsoleColor(color);
							Console.Write(' ');
						}
					}
					Console.WriteLine();
				}
			}
			finally
			{
				this.Image.UnlockBits(info);
			}
			Console.ResetColor();
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002190 File Offset: 0x00000390
		private ConsoleColor GetClosestConsoleColor(Color color)
		{
			Color best = Color.White;
			ConsoleColor bestMapping = ConsoleColor.White;
			foreach (KeyValuePair<Color, ConsoleColor> entry in AsciiImage.ColorMapping)
			{
				if (AsciiImage.GetDifference(color, entry.Key) < AsciiImage.GetDifference(color, best))
				{
					best = entry.Key;
					bestMapping = entry.Value;
				}
			}
			return bestMapping;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002208 File Offset: 0x00000408
		private static int GetDifference(Color color, Color other)
		{
			int num = Math.Abs((int)(color.R - other.R));
			int gdiff = Math.Abs((int)(color.G - other.G));
			int bdiff = Math.Abs((int)(color.B - other.B));
			return (num + gdiff + bdiff) / 3;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000225C File Offset: 0x0000045C
		// Note: this type is marked as 'beforefieldinit'.
		static AsciiImage()
		{
			Dictionary<Color, ConsoleColor> dictionary = new Dictionary<Color, ConsoleColor>();
			Color red = Color.Red;
			dictionary[red] = ConsoleColor.Red;
			Color darkRed = Color.DarkRed;
			dictionary[darkRed] = ConsoleColor.DarkRed;
			Color blue = Color.Blue;
			dictionary[blue] = ConsoleColor.Blue;
			Color darkBlue = Color.DarkBlue;
			dictionary[darkBlue] = ConsoleColor.DarkBlue;
			Color gray = Color.Gray;
			dictionary[gray] = ConsoleColor.Gray;
			Color dimGray = Color.DimGray;
			dictionary[dimGray] = ConsoleColor.DarkGray;
			Color cyan = Color.Cyan;
			dictionary[cyan] = ConsoleColor.Cyan;
			Color darkCyan = Color.DarkCyan;
			dictionary[darkCyan] = ConsoleColor.DarkCyan;
			Color green = Color.Green;
			dictionary[green] = ConsoleColor.Green;
			Color darkGreen = Color.DarkGreen;
			dictionary[darkGreen] = ConsoleColor.DarkGreen;
			Color yellow = Color.Yellow;
			dictionary[yellow] = ConsoleColor.Yellow;
			Color darkGoldenrod = Color.DarkGoldenrod;
			dictionary[darkGoldenrod] = ConsoleColor.DarkYellow;
			Color magenta = Color.Magenta;
			dictionary[magenta] = ConsoleColor.Magenta;
			Color darkMagenta = Color.DarkMagenta;
			dictionary[darkMagenta] = ConsoleColor.DarkMagenta;
			Color black = Color.Black;
			dictionary[black] = ConsoleColor.Black;
			Color white = Color.White;
			dictionary[white] = ConsoleColor.White;
			AsciiImage.ColorMapping = dictionary;
		}

		private static readonly IDictionary<Color, ConsoleColor> ColorMapping;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO.Compression;
using System.Numerics;

using Tesseract;
using Tesseract.Droid;
using AEnv = Android.OS.Environment;
using FilePath = System.IO.Path;
using Plugin.ImageEdit.Abstractions;

namespace LocoVoznjaSkener {
	class OCRData {
		public string Text;
		public byte[] PngOriginal;
		public byte[] PngCropped;
		public byte[] PngProcessed;

		string FailReason;

		public bool IsProbablyValid() {
			if (Text.Length > 10) {
				FailReason = "IsProbablyValid: Text has more than 10 chars";
				return false;
			}

			int LetterCount = 0;
			int NumberCount = 0;

			for (int i = 0; i < Text.Length; i++) {
				if (char.IsLetter(Text[i]))
					LetterCount++;

				if (char.IsNumber(Text[i]))
					NumberCount++;
			}

			if (LetterCount > 2) {
				FailReason = "IsProbablyValid: More than 2 letters found";
				return false;
			}

			return true;
		}

		public bool TryParseKM(out int Result) {
			Result = 0;

			if (!IsProbablyValid())
				return false;

			string Nums = Text.ToLower().Trim();

			if (Nums.EndsWith("km"))
				Nums = Nums.Substring(0, Nums.Length - 2).Trim();

			if (int.TryParse(Nums, out Result)) {
				if (Result < 0 || Result > 999999) {
					FailReason = "TryParseKM: Result out of range";
					return false;
				}

				return true;
			}

			FailReason = "TryParseKM: Failed to parse Nums '" + Nums + "'";
			return false;
		}

		public string CreateInfoString() {
			StringBuilder SBuilder = new StringBuilder();

			SBuilder.AppendLine("Text = " + Text);
			SBuilder.AppendLine("IsProbablyValid = " + IsProbablyValid().ToString());

			bool TryParseKMRes = TryParseKM(out int Res);
			SBuilder.AppendLine("TryParseKMRes = " + TryParseKMRes);
			SBuilder.AppendLine("TryParseKM = " + Res);

			SBuilder.AppendLine("FailReason = " + (FailReason ?? "none"));

			return SBuilder.ToString();
		}
	}

	static class OCR {
		static TesseractApi TesAPI;

		public static async Task<OCRData> Detect(IEditableImage Img, Context Ctx, int RectWidth, int RectHeight) {
			if (TesAPI == null) {
				TesAPI = new TesseractApi(Ctx, AssetsDeployment.OncePerInitialization);
				await TesAPI.Init("eng");

				TesAPI.SetVariable("tessedit_char_whitelist", "0123456789kmKM");
			}

			OCRData Result = new OCRData();
			Result.PngOriginal = Img.ToPng();

			// Crop the detection region
			Img = Img.Crop((Img.Width / 2) - (RectWidth / 2), (Img.Height / 2) - (RectHeight / 2), RectWidth, RectHeight).ToMonochrome();
			Result.PngCropped = Img.ToPng();

			using (MemoryStream PngImage = new MemoryStream()) {
				using (Bitmap Pic = ProcessImage((Bitmap)Img.GetNativeImage()))
					await Pic.CompressAsync(Bitmap.CompressFormat.Png, 100, PngImage);

				PngImage.Seek(0, SeekOrigin.Begin);
				await TesAPI.SetImage(PngImage);

				Result.PngProcessed = PngImage.ToArray();
			}

			Result.Text = TesAPI.Text;
			return Result;
		}

		public static unsafe Bitmap ProcessImage(Bitmap Img) {
			Bitmap Bmp = Utils.Blur(Img, 20);
			int[] Pixels = new int[Bmp.Width * Bmp.Height];
			Bmp.GetPixels(Pixels, 0, Bmp.Width, 0, 0, Bmp.Width, Bmp.Height);

			fixed (int* PixelsPtr = Pixels) {
				Pixel* Pix = (Pixel*)PixelsPtr;
				const float Contrast = 6;
				const float Offset = 0.5f;

				// Calculate corner colors to determine if it's black on white or white on black
				Vector4 CornerAverage = Pix[0].ToVector() + Pix[Bmp.Width].ToVector() + Pix[Bmp.Width * Bmp.Height - 1].ToVector() + Pix[Bmp.Width * Bmp.Height - Bmp.Width].ToVector();
				CornerAverage = CornerAverage / 4;
				float Avg = (CornerAverage.X + CornerAverage.Y + CornerAverage.Z) / 3;
				bool FlipColor = Avg < 0.5f;

				for (int i = 0; i < Pixels.Length; i++) {
					Vector4 Clr = Pix[i].ToVector();

					Clr = (Clr - new Vector4(new Vector3(Offset), 0)) * new Vector4(new Vector3(Contrast), 1);
					Clr = Utils.Clamp(Clr, Vector4.Zero, Vector4.One);

					if (FlipColor)
						Clr = Vector4.One - Clr;

					Pix[i] = new Pixel(Clr);
				}
			}

			Bmp.SetPixels(Pixels, 0, Bmp.Width, 0, 0, Bmp.Width, Bmp.Height);
			return Bmp;
		}

		public static void SaveDebug(OCRData OCRData) {
			string FolderPath = AEnv.ExternalStorageDirectory.AbsolutePath;
			FolderPath = FilePath.Combine(FolderPath, "LocoVoznja_OCR");

			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);

			string FName = FilePath.Combine(FolderPath, Utils.GetCurrentTimeName() + ".zip");

			using (FileStream FStream = new FileStream(FName, FileMode.Create)) {
				ZipEntry Orig = new ZipEntry("orig.png", OCRData.PngOriginal);
				ZipEntry Crop = new ZipEntry("crop.png", OCRData.PngCropped);
				ZipEntry Proc = new ZipEntry("proc.png", OCRData.PngProcessed);
				ZipEntry Info = new TextZipEntry("info.txt", OCRData.CreateInfoString());

				ZipUtils.CreateZip(FStream, Orig, Crop, Proc, Info);
			}
		}
	}
}
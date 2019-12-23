using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.ImageEdit;
using Plugin.ImageEdit.Abstractions;
using DDebug = System.Diagnostics.Debug;
using System.Numerics;

using Tesseract;
using Tesseract.Droid;
using Android.Graphics;
using Android.Util;
using System.Runtime.InteropServices;
using Android.Renderscripts;

namespace LocoVoznjaSkener {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct Pixel {
		public byte B;
		public byte G;
		public byte R;
		public byte A;

		public Vector4 ToVector() {
			return new Vector4(R, G, B, A) / 255.0f;
		}

		public Pixel(Vector4 V) {
			R = (byte)(V.X * 255);
			G = (byte)(V.Y * 255);
			B = (byte)(V.Z * 255);
			A = (byte)(V.W * 255);
		}
	}

	static class Utils {
		static TesseractApi TesAPI;

		public static float Clamp(float Val, float Min, float Max) {
			if (Val < Min)
				return Min;
			if (Val > Max)
				return Max;
			return Val;
		}

		public static Vector4 Clamp(Vector4 Val, Vector4 Min, Vector4 Max) {
			return new Vector4(Clamp(Val.X, Min.X, Max.X), Clamp(Val.Y, Min.Y, Max.Y), Clamp(Val.Z, Min.Z, Max.Z), Clamp(Val.W, Min.W, Max.W));
		}

		public static void Log(string Fmt, params object[] Args) {
			DDebug.WriteLine(string.Format(Fmt, Args));
		}

		public static Thread NewThread(ThreadStart Act) {
			Thread T = new Thread(Act);
			T.Start();
			return T;
		}

		public static unsafe Bitmap CropImage(byte[] ImgData, bool IsHorizontal) {
			IEditableImage Img = CrossImageEdit.Current.CreateImage(ImgData);

			if (!IsHorizontal)
				Img = Img.Rotate(90);

			int W = Img.Width;
			int H = Img.Height;

			int Width = (int)(W * 0.5f);
			int Height = DpToPx(100);

			Img = Img.Crop((W / 2) - (Width / 2), (H / 2) - (Height / 2), Width, Height).ToMonochrome();
			//Img = Img.Resize(0, (int)(Img.Height * 0.5f));

			Bitmap Bmp = Blur((Bitmap)Img.GetNativeImage());
			int[] Pixels = new int[Bmp.Width * Bmp.Height];
			Bmp.GetPixels(Pixels, 0, Bmp.Width, 0, 0, Bmp.Width, Bmp.Height);

			fixed (int* PixelsPtr = Pixels) {
				Pixel* Pix = (Pixel*)PixelsPtr;
				const float Ctr = 6;
				const float Offset = 0.5f;

				Vector4 CornerAverage = Pix[0].ToVector() + Pix[Bmp.Width].ToVector() + Pix[Bmp.Width * Bmp.Height - 1].ToVector() + Pix[Bmp.Width * Bmp.Height - Bmp.Width].ToVector();
				CornerAverage = CornerAverage / 4;
				float Avg = (CornerAverage.X + CornerAverage.Y + CornerAverage.Z) / 3;
				bool FlipColor = Avg < 0.5f;

				for (int i = 0; i < Pixels.Length; i++) {
					Vector4 Clr = Pix[i].ToVector();

					Clr = (Clr - new Vector4(Offset, Offset, Offset, 0)) * new Vector4(Ctr, Ctr, Ctr, 1);
					Clr = Utils.Clamp(Clr, Vector4.Zero, Vector4.One);

					if (FlipColor)
						Clr = Vector4.One - Clr;

					Pix[i] = new Pixel(Clr);
				}
			}

			Bmp.SetPixels(Pixels, 0, Bmp.Width, 0, 0, Bmp.Width, Bmp.Height);
			//return Blur(Bmp);
			return Bmp;
		}

		public static Bitmap Blur(Bitmap BMap) {
			Bitmap MutableBMap = BMap.Copy(Bitmap.Config.Argb8888, true);
			RenderScript RS = RenderScript.Create(Application.Context);

			Allocation Input = Allocation.CreateFromBitmap(RS, MutableBMap, Allocation.MipmapControl.MipmapFull, AllocationUsage.Script);
			Allocation Output = Allocation.CreateTyped(RS, Input.Type);

			ScriptIntrinsicBlur Script = ScriptIntrinsicBlur.Create(RS, Element.U8_4(RS));
			Script.SetInput(Input);
			Script.SetRadius(20);
			Script.ForEach(Output);

			Output.CopyTo(MutableBMap);
			return MutableBMap;
		}

		public static int DpToPx(float DP) {
			DisplayMetrics DispMetrics = Application.Context.Resources.DisplayMetrics;
			return (int)Math.Round(DP * (DispMetrics.Xdpi / (int)DisplayMetricsDensity.Default));
		}

		public static async Task<string> OCR(string FileName, Context Ctx) {
			if (TesAPI == null) {
				TesAPI = new TesseractApi(Ctx, AssetsDeployment.OncePerInitialization);
				await TesAPI.Init("eng");

				TesAPI.SetVariable("tessedit_char_whitelist", "0123456789km");
			}

			await TesAPI.SetImage(FileName);
			return TesAPI.Text;
		}

		public static bool RequestPermissions(Activity Act, params string[] Perms) {
			List<string> RequestPerms = new List<string>();

			for (int i = 0; i < Perms.Length; i++)
				if (Act.CheckSelfPermission(Perms[i]) != Android.Content.PM.Permission.Granted)
					RequestPerms.Add(Perms[i]);

			if (RequestPerms.Count > 0)
				Act.RequestPermissions(RequestPerms.ToArray(), 0);

			return RequestPerms.Count == 0;
		}
	}
}
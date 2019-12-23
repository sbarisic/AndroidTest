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

using Tesseract;
using Tesseract.Droid;
using Android.Graphics;
using Android.Util;

namespace LocoVoznjaSkener {
	static class Utils {
		static TesseractApi TesAPI;

		public static void Log(string Fmt, params object[] Args) {
			DDebug.WriteLine(string.Format(Fmt, Args));
		}

		public static Thread NewThread(ThreadStart Act) {
			Thread T = new Thread(Act);
			T.Start();
			return T;
		}

		public static Bitmap CropImage(byte[] ImgData, bool IsHorizontal) {
			IEditableImage Img = CrossImageEdit.Current.CreateImage(ImgData);

			if (!IsHorizontal)
				Img = Img.Rotate(90);

			int W = Img.Width;
			int H = Img.Height;

			int Width = (int)(W * 0.5f);
			int Height = DpToPx(100);

			Img = Img.Crop((W / 2) - (Width / 2), (H / 2) - (Height / 2), Width, Height).ToMonochrome();
			//Img = Img.Resize(0, 256).ToMonochrome();
			return (Bitmap)Img.GetNativeImage();
		}

		public static int DpToPx(float DP) {
			DisplayMetrics DispMetrics = Application.Context.Resources.DisplayMetrics;
			return (int)Math.Round(DP * (DispMetrics.Xdpi / (int)DisplayMetricsDensity.Default));
		}

		public static async Task<string> OCR(string FileName, Context Ctx) {
			if (TesAPI == null) {
				TesAPI = new TesseractApi(Ctx, AssetsDeployment.OncePerInitialization);

				await TesAPI.Init("eng");
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
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

using Android.Graphics;
using Android.Util;
using System.Runtime.InteropServices;
using Android.Renderscripts;
using System.IO;
using Android.Locations;

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
		static Random Rnd = new Random();

		public static string GetCurrentTimeName() {
			return DateTime.Now.ToString("yyyyMMdd_HHmmss");
		}

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

		public static async Task<Address> ReverseGeocodeCurrentLocation(Context Ctx, double Lat, double Long) {
			Geocoder GCoder = new Geocoder(Ctx);
			IList<Address> AddrList = await GCoder.GetFromLocationAsync(Lat, Long, 10);
			return AddrList.FirstOrDefault();
		}

		public static bool GetLocation(out double Lat, out double Long) {
			Lat = 0;
			Long = 0;
			


			return false;
		}

		public static Bitmap Blur(Bitmap BMap, float Radius) {
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
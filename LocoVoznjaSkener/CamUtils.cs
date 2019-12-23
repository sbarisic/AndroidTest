using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Camera = Android.Hardware.Camera;
using DDebug = System.Diagnostics.Debug;

namespace LocoVoznjaSkener {
	static class CamUtils {
		static Camera Cam;
		static int Orientation;

		public static void Start(SurfaceTexture Surface, int Width, int Height) {
			if (Cam == null)
				Cam = Camera.Open();

			Camera.Parameters Parms = Cam.GetParameters();

			if (Parms.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture)) {
				Parms.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
				Cam.SetParameters(Parms);
			}

			Cam.SetPreviewTexture(Surface);
			Cam.SetDisplayOrientation(90);
			StartPreview();
			SetVertical();
		}

		public static void StartPreview() {
			Cam.StartPreview();
		}

		public static void Stop() {
			StopPreview();
			Cam.Release();
			Cam = null;
		}

		public static void StopPreview() {
			Cam.StopPreview();
		}

		public static void SetHorizontal() {
			Cam.SetDisplayOrientation(Orientation = 0);
		}

		public static void SetVertical() {
			Cam.SetDisplayOrientation(Orientation = 90);
		}

		public static int GetOrientation() {
			return Orientation;
		}

		public static void TakePicture(PictureCallback.OnPictureFunc OnPicture) {
			PictureCallback PCallback = new PictureCallback(OnPicture);
			Cam.TakePicture(PCallback, null, PCallback);
		}
	}

	class PictureCallback : Java.Lang.Object, Camera.IPictureCallback, Camera.IShutterCallback {
		public delegate Task OnPictureFunc(byte[] Data, Bitmap Img);

		OnPictureFunc OnPicture;

		public PictureCallback(OnPictureFunc OnPicture) {
			this.OnPicture = OnPicture;
		}

		public void OnPictureTaken(byte[] Data, Camera Cam) {
			CamUtils.StopPreview();

			using (MemoryStream MS = new MemoryStream(Data)) {
				MS.Seek(0, SeekOrigin.Begin);

				//Bitmap ImageBmp = BitmapFactory.DecodeByteArray(Data, 0, Data.Length);
				Bitmap ImageBmp = Utils.CropImage(Data, CamUtils.GetOrientation() == 0);

				OnPicture(Data, ImageBmp);
			}

			CamUtils.StartPreview();
		}


		public void OnShutter() {

		}
	}
}
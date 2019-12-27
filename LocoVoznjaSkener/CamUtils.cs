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
using Plugin.ImageEdit;
using Plugin.ImageEdit.Abstractions;
using Camera = Android.Hardware.Camera;
using DDebug = System.Diagnostics.Debug;

namespace LocoVoznjaSkener {
	static class CamUtils {
		static Camera Cam;
		static int Orientation;

		static int MaxZoom;
		static float ZoomScaleNum;

		public static float ZoomScale {
			get {
				return ZoomScaleNum;
			}

			set {
				ZoomScaleNum = Math.Clamp(value, 0, 1);
				Zoom((int)(ZoomScaleNum * MaxZoom));
			}
		}

		public static void Start(SurfaceTexture Surface, int Width, int Height) {
			if (Cam == null)
				Cam = Camera.Open();

			Camera.Parameters Parms = Cam.GetParameters();
			MaxZoom = Parms.MaxZoom;

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

		public static void Zoom(int Z) {
			Z = Math.Clamp(Z, 0, MaxZoom);

			Camera.Parameters Parms = Cam.GetParameters();
			Parms.Zoom = Z;
			Cam.SetParameters(Parms);
		}

		public static void TakePicture(PictureCallback.OnPictureFunc OnPicture) {
			PictureCallback PCallback = new PictureCallback(OnPicture);
			Cam.TakePicture(PCallback, null, PCallback);
		}
	}

	class PictureCallback : Java.Lang.Object, Camera.IPictureCallback, Camera.IShutterCallback {
		public delegate Task OnPictureFunc(IEditableImage Img);

		OnPictureFunc OnPicture;

		public PictureCallback(OnPictureFunc OnPicture) {
			this.OnPicture = OnPicture;
		}

		public void OnPictureTaken(byte[] Data, Camera Cam) {
			CamUtils.StopPreview();

			Utils.NewThread(() => {
				IEditableImage Img = CrossImageEdit.Current.CreateImage(Data);
				CamUtils.StartPreview();

				if (CamUtils.GetOrientation() != 0)
					Img = Img.Rotate(90);

				OnPicture(Img).Wait();
			});
		}


		public void OnShutter() {

		}
	}
}
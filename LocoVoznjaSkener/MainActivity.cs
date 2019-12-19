using System;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using Android.Content.PM;
using Camera = Android.Hardware.Camera;
using Android;

namespace LocoVoznjaSkener {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
	public class MainActivity : AppCompatActivity, TextureView.ISurfaceTextureListener {
		const int CAMERA_PERMISSION_REQUEST_CODE = 3;

		TextureView texView;
		Camera cam;

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) {
			cam = Camera.Open();

			try {
				cam.SetPreviewTexture(surface);
				cam.SetDisplayOrientation(90);
				cam.StartPreview();
			} catch (Exception E) {

				//throw;
			}
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface) {
			cam.StopPreview();
			cam.Release();
			return true;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height) {
			//throw new System.NotImplementedException();
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface) {
			//throw new System.NotImplementedException();
		}

		void StartCamera() {
			texView = FindViewById<TextureView>(Resource.Id.texView);
			texView.SurfaceTextureListener = this;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);

			SetContentView(Resource.Layout.CamLayout);

			if (CheckSelfPermission(Manifest.Permission.Camera) != Permission.Granted) {
				RequestPermissions(new[] { Manifest.Permission.Camera }, CAMERA_PERMISSION_REQUEST_CODE);
			} else
				StartCamera();
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			switch (requestCode) {
				case CAMERA_PERMISSION_REQUEST_CODE:
					if (grantResults.Length > 0 && grantResults[0] == Permission.Granted) {
						StartCamera();
					}
					break;

				default:
					break;
			}

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
	}
}
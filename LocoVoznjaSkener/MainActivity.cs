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
using Android.Content.Res;
using Java.Interop;
using Android.Content;
using Android.Util;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
//using System.Drawing;
//using GBitmap = System.Drawing.Bitmap;

namespace LocoVoznjaSkener {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class MainActivity : AppCompatActivity, TextureView.ISurfaceTextureListener {
		const int CAMERA_PERMISSION_REQUEST_CODE = 3;
		const int STORAGE_PERMISSION_REQUEST_CODE = 4;

		TextureView texView;
		TextView camLabel;
		Button btnSnap;

		public void OnSurfaceTextureAvailable(SurfaceTexture Surface, int Width, int Height) {
			CamUtils.Start(Surface, Width, Height);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface) {
			CamUtils.Stop();
			return true;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height) {
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface) {
		}

		void StartCamera() {
			texView = FindViewById<TextureView>(Resource.Id.texView);
			texView.SurfaceTextureListener = this;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.CamLayout);

			if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) != Permission.Granted || CheckSelfPermission(Manifest.Permission.WriteExternalStorage) != Permission.Granted)
				RequestPermissions(new[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, STORAGE_PERMISSION_REQUEST_CODE);

			if (CheckSelfPermission(Manifest.Permission.Camera) != Permission.Granted) {
				RequestPermissions(new[] { Manifest.Permission.Camera }, CAMERA_PERMISSION_REQUEST_CODE);
			} else
				StartCamera();

			btnSnap = FindViewById<Button>(Resource.Id.btnSnap);
			btnSnap.Click += OnSnap;

			camLabel = FindViewById<TextView>(Resource.Id.camLabel);
			camLabel.Visibility = ViewStates.Gone;
		}

		private void OnSnap(object sender, EventArgs e) {
			ShowLabel("Processing...");
			CamUtils.TakePicture(OnPicture);
		}

		void OnPicture(Bitmap Pic) {
			ShowLabel("69000 km");
		}

		void ShowLabel(string Text) {
			camLabel.Text = Text;
			camLabel.Visibility = ViewStates.Visible;
		}

		void SaveBitmap(Bitmap bitmap) {
			return;

			var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
			var filePath = System.IO.Path.Combine(sdCardPath, "yourImageName.png");

			if (File.Exists(filePath))
				File.Delete(filePath);

			var stream = new FileStream(filePath, FileMode.Create);
			bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
			stream.Close();
		}

		void SwitchHorizontal() {
			CamUtils.SetHorizontal();

			if (btnSnap != null) {
				RelativeLayout.LayoutParams LParams = (RelativeLayout.LayoutParams)btnSnap.LayoutParameters;
				LParams.RemoveRule(LayoutRules.AlignParentBottom);
				LParams.RemoveRule(LayoutRules.CenterHorizontal);

				LParams.AddRule(LayoutRules.AlignParentRight);
				LParams.AddRule(LayoutRules.CenterVertical);
			}
		}

		void SwitchVertical() {
			CamUtils.SetVertical();

			if (btnSnap != null) {
				RelativeLayout.LayoutParams LParams = (RelativeLayout.LayoutParams)btnSnap.LayoutParameters;
				LParams.RemoveRule(LayoutRules.AlignParentRight);
				LParams.RemoveRule(LayoutRules.CenterVertical);

				LParams.AddRule(LayoutRules.AlignParentBottom);
				LParams.AddRule(LayoutRules.CenterHorizontal);
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			switch (requestCode) {
				case CAMERA_PERMISSION_REQUEST_CODE:
					if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
						StartCamera();

					break;

				default:
					break;
			}

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		public override void OnConfigurationChanged(Configuration newConfig) {
			base.OnConfigurationChanged(newConfig);

			if (newConfig.Orientation == Android.Content.Res.Orientation.Portrait)
				SwitchVertical();
			else if (newConfig.Orientation == Android.Content.Res.Orientation.Landscape)
				SwitchHorizontal();
		}
	}
}
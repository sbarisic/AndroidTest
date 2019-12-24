using System;
using Android.App;
using Android.OS;
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
using Plugin.ImageEdit.Abstractions;
//using System.Drawing;
//using GBitmap = System.Drawing.Bitmap;

namespace LocoVoznjaSkener {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class MainActivity : Activity, TextureView.ISurfaceTextureListener {
		const int CAMERA_RC = 80;
		const int STOR_READ_RC = 81;
		const int STOR_WRITE_RC = 82;

		TextureView texView;
		TextView camLabel;
		Button btnSnap;
		ImageView centerRect;

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

			var UIOpts = SystemUiFlags.HideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky;

			if (Window != null && Window.DecorView != null)
				Window.DecorView.SystemUiVisibility = (StatusBarVisibility)UIOpts;

			string[] Perms = new[] {
					Manifest.Permission.ReadExternalStorage,
					Manifest.Permission.WriteExternalStorage,
					Manifest.Permission.Camera,
					Manifest.Permission.Internet,
					Manifest.Permission.AccessCoarseLocation,
					Manifest.Permission.AccessFineLocation
				};

			if (Utils.RequestPermissions(this, Perms))
				StartCamera();

			centerRect = FindViewById<ImageView>(Resource.Id.centerRect);

			btnSnap = FindViewById<Button>(Resource.Id.btnSnap);
			btnSnap.Click += OnSnap;

			camLabel = FindViewById<TextView>(Resource.Id.camLabel);
			camLabel.Visibility = ViewStates.Gone;
		}

		private void OnSnap(object sender, EventArgs e) {
			ShowLabel("Processing...");
			CamUtils.TakePicture(OnPicture);
		}

		async Task OnPicture(IEditableImage Img) {
			int W = Img.Width;
			int H = Img.Height;

			OCRData OCRData = await OCR.Detect(Img, ApplicationContext, (int)(W * 0.52f), (int)(H * 0.13f));
			OCR.SaveDebug(OCRData);

			if (OCRData.TryParseKM(out int KM)) {
				ShowLabel(string.Format("S: {0} km", KM));
			} else
				ShowLabel(string.Format("F: {0}", OCRData.Text));
		}

		void ShowLabel(string Text) {
			camLabel.Text = Text;
			camLabel.Visibility = ViewStates.Visible;
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

		public override void OnRequestPermissionsResult(int ReqCode, string[] Perms, [GeneratedEnum] Permission[] GrantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(ReqCode, Perms, GrantResults);

			for (int i = 0; i < Perms.Length; i++) {
				if (Perms[i] == Manifest.Permission.Camera && GrantResults[i] == Permission.Granted)
					StartCamera();
			}

			base.OnRequestPermissionsResult(ReqCode, Perms, GrantResults);
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
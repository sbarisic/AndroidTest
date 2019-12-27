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
using Android.Locations;
//using System.Drawing;
//using GBitmap = System.Drawing.Bitmap;

namespace LocoVoznjaSkener {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class MainActivity : Activity, TextureView.ISurfaceTextureListener, ILocationListener {
		ScaleGestureDetector ScaleDetector;
		GestureDetector GestureDetector;

		ScaleHandler ScaleHandler;
		GestureHandler GestureHandler;

		TextureView texView;
		TextView camLabel;
		Button btnSnap;
		ImageView centerRect;

		bool DoBeginLocoVoznja;
		int Kilometers;

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
			//Xamarin.Essentials.Platform.Init(this, savedInstanceState);

			SetContentView(Resource.Layout.CamLayout);
			DoBeginLocoVoznja = false;

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
			ShowLabel(null);

			ScaleHandler = new ScaleHandler();
			ScaleDetector = new ScaleGestureDetector(this, ScaleHandler);

			GestureHandler = new GestureHandler();
			GestureDetector = new GestureDetector(this, GestureHandler);
		}

		private void OnSnap(object sender, EventArgs e) {
			ShowLabel("Snapping...");
			CamUtils.TakePicture(OnPicture);
		}

		async Task OnPicture(IEditableImage Img) {
			ShowLabel("Processing...");

			int W = Img.Width;
			int H = Img.Height;

			OCRData OCRData = await OCR.Detect(Img, ApplicationContext, (int)(W * 0.52f), (int)(H * 0.13f));
			OCR.SaveDebug(OCRData);

			if (OCRData.TryParseKM(out int KM)) {
				string KMFormat = string.Format("{0} km", KM);
				ShowLabel(KMFormat);

				RunOnUiThread(() => {
					AlertDialog.Builder AlertBuilder = new AlertDialog.Builder(this);
					AlertBuilder.SetTitle("Success");
					AlertBuilder.SetMessage("Use the following? " + KMFormat);
					AlertBuilder.SetPositiveButton("Yes", (S, E) => {
						RunOnUiThread(() => {
							ShowLabel("Fetching location...");

							Kilometers = KM;
							DoBeginLocoVoznja = true;

							LocationManager LocMgr = (LocationManager)GetSystemService(LocationService);
							LocMgr.RequestSingleUpdate(LocationManager.GpsProvider, this, Looper.MainLooper);
						});
					});

					AlertBuilder.SetNegativeButton("No", (S, E) => {
						ShowLabel("User cancelled");
					});

					AlertBuilder.Show();
				});

			} else {
				ShowLabel(null);
				ShowInfoDialog("Info", string.Format("Failed to parse km, got '{0}'", OCRData.Text ?? "none"));
			}
		}

		void ShowInfoDialog(string Title, string Text) {
			RunOnUiThread(() => {
				AlertDialog.Builder AlertBuilder = new AlertDialog.Builder(this);

				AlertBuilder.SetTitle(Title);
				AlertBuilder.SetMessage(Text);
				AlertBuilder.SetPositiveButton("OK", (S, E) => { });

				AlertBuilder.Show();
			});
		}

		void ShowLabel(string Text) {
			RunOnUiThread(() => {
				if (!string.IsNullOrEmpty(Text)) {
					camLabel.Text = Text;
					camLabel.Visibility = ViewStates.Visible;
				} else
					camLabel.Visibility = ViewStates.Gone;
			});
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
			//Xamarin.Essentials.Platform.OnRequestPermissionsResult(ReqCode, Perms, GrantResults);

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

		public override bool OnTouchEvent(MotionEvent e) {
			ScaleDetector.OnTouchEvent(e);
			GestureDetector.OnTouchEvent(e);
			return base.OnTouchEvent(e);
		}

		public void OnLocationChanged(Location Loc) {
			if (DoBeginLocoVoznja) {
				DoBeginLocoVoznja = false;
				ShowLabel("Starting...");

				Address StartAddr = Utils.GetAddress(this, Loc.Latitude, Loc.Longitude);
				LocoVoznja.BeginLocoVoznja(Kilometers, StartAddr);
			}
		}

		public void OnProviderDisabled(string provider) {
		}

		public void OnProviderEnabled(string provider) {
		}

		public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras) {
		}
	}

	class GestureHandler : GestureDetector.SimpleOnGestureListener {
		public override bool OnDoubleTap(MotionEvent E) {
			CamUtils.ZoomScale = 0;
			return true;
		}
	}

	class ScaleHandler : ScaleGestureDetector.SimpleOnScaleGestureListener {
		public override bool OnScale(ScaleGestureDetector Det) {
			float ZoomAmt = ((float)Math.Round(Det.ScaleFactor, 2) - 1.0f) / 4.0f;
			CamUtils.ZoomScale = CamUtils.ZoomScale + ZoomAmt;
			return true;
		}
	}
}
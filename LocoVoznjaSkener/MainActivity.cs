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
//using System.Drawing;
//using GBitmap = System.Drawing.Bitmap;

namespace LocoVoznjaSkener {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class MainActivity : AppCompatActivity, TextureView.ISurfaceTextureListener {
		const int CAMERA_PERMISSION_REQUEST_CODE = 3;
		const int STORAGE_PERMISSION_REQUEST_CODE = 4;

		TextureView texView;
		Camera cam;

		Button btnSnap;

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) {
			cam = Camera.Open();

			Camera.Parameters parms = cam.GetParameters();

			if (parms.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture)) {
				parms.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
				cam.SetParameters(parms);
			}

			cam.SetPreviewTexture(surface);
			cam.StartPreview();
			cam.SetDisplayOrientation(90);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface) {
			cam.StopPreview();
			cam.Release();
			cam = null;
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

			if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) != Permission.Granted || CheckSelfPermission(Manifest.Permission.WriteExternalStorage) != Permission.Granted)
				RequestPermissions(new[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, STORAGE_PERMISSION_REQUEST_CODE);

			if (CheckSelfPermission(Manifest.Permission.Camera) != Permission.Granted) {
				RequestPermissions(new[] { Manifest.Permission.Camera }, CAMERA_PERMISSION_REQUEST_CODE);
			} else
				StartCamera();

			btnSnap = FindViewById<Button>(Resource.Id.btnSnap);
			btnSnap.Click += OnSnap;
		}

		private void OnSnap(object sender, EventArgs e) {
			if (cam == null)
				return;

			cam.TakePicture(null, null, new PictureCallback(this, (data, camera) => {
				using (MemoryStream MS = new MemoryStream(data)) {
					MS.Seek(0, SeekOrigin.Begin);

					Bitmap Bmp = BitmapFactory.DecodeByteArray(data, 0, data.Length);

					//Bitmap.CreateScaledBitmap()

					Bitmap NewBmp = Bmp.Copy(Bitmap.Config.Argb8888, true);

					Paint NewPaint = new Paint();
					NewPaint.Color = Color.Red;
					NewPaint.StrokeWidth = 10;
					NewPaint.SetStyle(Paint.Style.Fill);
					

					using (Canvas Can = new Canvas(NewBmp)) {
						Can.Rotate(90);
						Can.DrawRect(100, 100, 200, 200, NewPaint);
						Can.Save();
					}

					SaveBitmap(NewBmp);
				}
			}));
		}

		void SaveBitmap(Bitmap bitmap) {
			var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
			var filePath = System.IO.Path.Combine(sdCardPath, "yourImageName.png");

			if (File.Exists(filePath))
				File.Delete(filePath);

			var stream = new FileStream(filePath, FileMode.Create);
			bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
			stream.Close();
		}

		void SwitchHorizontal() {
			cam?.SetDisplayOrientation(0);

			if (btnSnap != null) {
				RelativeLayout.LayoutParams LParams = (RelativeLayout.LayoutParams)btnSnap.LayoutParameters;
				LParams.RemoveRule(LayoutRules.AlignParentBottom);
				LParams.RemoveRule(LayoutRules.CenterHorizontal);

				LParams.AddRule(LayoutRules.AlignParentRight);
				LParams.AddRule(LayoutRules.CenterVertical);
			}
		}

		void SwitchVertical() {
			cam?.SetDisplayOrientation(90);

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

	class PictureCallback : Java.Lang.Object, Camera.IPictureCallback {
		public delegate void OnPictureTakenAction(byte[] Data, Camera Cam);

		const string APP_NAME = "SimpleCameraApp";
		Context _context;
		OnPictureTakenAction OnPicture;

		public PictureCallback(Context cont, OnPictureTakenAction OnPicture) {
			_context = cont;
			this.OnPicture = OnPicture;
		}

		/// <summary>
		/// Callback when the picture is taken by the Camera
		/// </summary>
		/// <param name="data"></param>
		/// <param name="camera"></param>
		public void OnPictureTaken(byte[] data, Camera camera) {
			OnPicture?.Invoke(data, camera);

			/*using (MemoryStream MS = new MemoryStream(data)) {
				MS.Seek(0, SeekOrigin.Begin);

				Bitmap Bmp = BitmapFactory.DecodeByteArray(data, 0, data.Length);

				//Bitmap.CreateScaledBitmap()

				Bitmap NewBmp = Bmp.Copy(Bitmap.Config.Argb8888, true);

				Paint NewPaint = new Paint();
				NewPaint.Color = Color.Red;
				NewPaint.StrokeWidth = 10;
				NewPaint.SetStyle(Paint.Style.Fill);

				using (Canvas Can = new Canvas(NewBmp)) {
					Can.Rotate(90);
					Can.DrawRect(100, 100, 200, 200, NewPaint);
					Can.Save();
				}

				SaveBitmap(NewBmp);
			}*/
		}
	}
}
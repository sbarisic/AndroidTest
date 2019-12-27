using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocoVoznjaSkener {
	static class LocoVoznja {
		static string TimeToStr(DateTime T) {
			return T.ToString("dd.MM.yyyy HH:mm");
		}

		public static void BeginLocoVoznja(int KM, Address StartAddr) {
			string Addr = string.Format("{0}, {1}", StartAddr.Thoroughfare, StartAddr.Locality);
			DateTime StartTime = DateTime.Now;

			//var Res = CrossPlacePicker.Current.Display();

			//PlacePicker.IntentBuilder IB;
		}
	}
}
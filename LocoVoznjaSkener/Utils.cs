using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;


namespace LocoVoznjaSkener {
	static class Utils {

		public static Thread NewThread(ThreadStart Act) {
			Thread T = new Thread(Act);
			T.Start();
			return T;
		}
	}
}
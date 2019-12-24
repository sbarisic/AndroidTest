using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;

namespace LocoVoznjaSkener {
	public class ZipEntry {
		public string FileName;
		public byte[] Data;

		public ZipEntry(string FileName, byte[] Data) {
			this.FileName = FileName;
			this.Data = Data;
		}
	}

	public class TextZipEntry : ZipEntry {
		public TextZipEntry(string Name, string Content) : base(Name, Encoding.UTF8.GetBytes(Content)) {
		}
	}

	static class ZipUtils {
		public static void CreateZip(Stream OutStream, params ZipEntry[] Entries) {
			using (ZipArchive Arc = new ZipArchive(OutStream, ZipArchiveMode.Create, true)) {
				foreach (var E in Entries) {
					ZipArchiveEntry ZEntry = Arc.CreateEntry(E.FileName, CompressionLevel.Optimal);

					using (BinaryWriter ZWriter = new BinaryWriter(ZEntry.Open()))
						ZWriter.Write(E.Data);
				}
			}
		}
	}
}
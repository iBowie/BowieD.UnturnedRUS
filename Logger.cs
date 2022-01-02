using System;
using System.IO;
using System.Text;

namespace BowieD.UnturnedRUS;

public static class Logger
{
	public static void Log(string txt)
	{
		using StreamWriter streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "log", append: true, Encoding.UTF8);
		streamWriter.WriteLine($"[{DateTime.Now}] - {txt}");
	}

	public static void ClearLog()
	{
		using StreamWriter streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "log", append: false, Encoding.UTF8);
		streamWriter.Write("");
	}
}

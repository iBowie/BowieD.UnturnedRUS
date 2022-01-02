using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using BowieD.UnturnedRUS.Properties;
using SevenZip;

namespace BowieD.UnturnedRUS;

public static class Installer
{
	private static bool complete;

	public static System.Windows.Controls.ProgressBar progressBar => MainWindow.Instance.progress;

	public static async Task<bool> Install(string dir, string modDir, bool installMods, bool installCity, bool installBase = true)
	{
		if ((installBase && dir == "") || !File.Exists(dir + "\\Unturned.exe"))
		{
			System.Windows.MessageBox.Show("Укажите папку установки русификатора (действие делается один раз)." + Environment.NewLine + "Путь должен выглядеть примерно так: ДИСК:/ПУТЬ ДО ИГРЫ");
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			if (folderBrowserDialog.ShowDialog() == DialogResult.Cancel)
			{
				return false;
			}
			if (!File.Exists(folderBrowserDialog.SelectedPath + "\\Unturned.exe"))
			{
				return false;
			}
			Settings.Default.baseInstallationFolder = folderBrowserDialog.SelectedPath;
			dir = folderBrowserDialog.SelectedPath;
			Settings.Default.Save();
		}
		if (modDir == "" && installMods)
		{
			System.Windows.MessageBox.Show("Укажите папку установки русификатора модов (действие делается один раз)." + Environment.NewLine + "Путь должен выглядеть примерно так: ДИСК:/ПУТЬ ДО STEAM/steamapps/common/workshop/304930");
			FolderBrowserDialog folderBrowserDialog2 = new FolderBrowserDialog();
			if (folderBrowserDialog2.ShowDialog() == DialogResult.Cancel)
			{
				return false;
			}
			Settings.Default.modInstallationFolder = folderBrowserDialog2.SelectedPath;
			modDir = folderBrowserDialog2.SelectedPath;
			Settings.Default.Save();
		}
		using (WebClient webClient = new WebClient())
		{
			if (installBase)
			{
				if (MainWindow.config.baseDownloadURL == "")
				{
					installBase = false;
				}
				else
				{
					webClient.DownloadFile(MainWindow.config.baseDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "rus.zip");
				}
			}
			if (installMods)
			{
				if (MainWindow.config.modDownloadURL == "")
				{
					installMods = false;
				}
				else
				{
					webClient.DownloadFile(MainWindow.config.modDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "mod.zip");
				}
			}
			if (installCity)
			{
				if (MainWindow.config.cityDownloadURL == "")
				{
					installCity = false;
				}
				else
				{
					webClient.DownloadFile(MainWindow.config.cityDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "city.zip");
				}
			}
			if (installBase || installCity || installMods)
			{
				webClient.DownloadFile("http://bowiestuff.at.ua/7z.dll", AppDomain.CurrentDomain.BaseDirectory + "7z.dll");
			}
		}
		if (installBase)
		{
			using (FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "rus.zip", FileMode.Open))
			{
				using SevenZipExtractor sevenZipExtractor = new SevenZipExtractor(fileStream.Name);
				sevenZipExtractor.ExtractionFinished += Sze_ExtractionFinished;
				sevenZipExtractor.Extracting += Sze_Extracting;
				sevenZipExtractor.BeginExtractArchive(dir + "\\");
				complete = false;
				while (!complete)
				{
					await Task.Delay(1);
				}
				sevenZipExtractor.ExtractionFinished -= Sze_ExtractionFinished;
				sevenZipExtractor.Extracting -= Sze_Extracting;
			}
			File.Delete(AppDomain.CurrentDomain.BaseDirectory + "rus.zip");
		}
		if (installMods)
		{
			using (FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "mod.zip", FileMode.Open))
			{
				using SevenZipExtractor sevenZipExtractor = new SevenZipExtractor(fileStream.Name);
				sevenZipExtractor.ExtractionFinished += Sze_ExtractionFinished;
				sevenZipExtractor.Extracting += Sze_Extracting;
				sevenZipExtractor.BeginExtractArchive(AppDomain.CurrentDomain.BaseDirectory + "temp\\");
				complete = false;
				while (!complete)
				{
					await Task.Delay(1);
				}
				DirectoryInfo[] directories = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "temp\\").GetDirectories("*", SearchOption.TopDirectoryOnly);
				foreach (DirectoryInfo directoryInfo in directories)
				{
					if (Settings.Default.allowedMods.Contains(directoryInfo.Name))
					{
						MainWindow.MoveDirectory(directoryInfo.FullName, modDir + "\\" + directoryInfo.Name);
					}
					else
					{
						directoryInfo.Delete(recursive: true);
					}
				}
				Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "temp", recursive: true);
				sevenZipExtractor.Extracting -= Sze_Extracting;
				sevenZipExtractor.ExtractionFinished -= Sze_ExtractionFinished;
			}
			File.Delete(AppDomain.CurrentDomain.BaseDirectory + "mod.zip");
		}
		if (installCity)
		{
			using (FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "city.zip", FileMode.Open))
			{
				using SevenZipExtractor sevenZipExtractor = new SevenZipExtractor(fileStream.Name);
				sevenZipExtractor.ExtractionFinished += Sze_ExtractionFinished;
				sevenZipExtractor.Extracting += Sze_Extracting;
				sevenZipExtractor.BeginExtractArchive(dir + "\\");
				complete = false;
				while (!complete)
				{
					await Task.Delay(1);
				}
				sevenZipExtractor.Extracting -= Sze_Extracting;
				sevenZipExtractor.ExtractionFinished -= Sze_ExtractionFinished;
			}
			File.Delete(AppDomain.CurrentDomain.BaseDirectory + "city.zip");
		}
		if (installBase)
		{
			Settings.Default.updated = DateTime.Now;
			Settings.Default.installed = true;
			Settings.Default.version = MainWindow.config.rusVersion;
			Settings.Default.unturnedVersion = MainWindow.config.unturnedVersion;
			MainWindow.Instance.installButton.Content = "Переустановить русификатор";
		}
		if (installMods)
		{
			Settings.Default.modsVersion = MainWindow.config.modVersion;
		}
		Settings.Default.Save();
		return true;
	}

	private static void Sze_Extracting(object sender, ProgressEventArgs e)
	{
		progressBar.Maximum = 100.0;
		progressBar.Value = (int)e.PercentDone;
	}

	public static async Task<bool> Update(string dir, string modDir, bool updateMods, bool updateCity)
	{
		return await Install(dir, modDir, updateMods, updateCity);
	}

	public static async Task<bool> Delete(string dir)
	{
		_ = 1;
		try
		{
			progressBar.Value = 0.0;
			List<FileInfo> fi = new DirectoryInfo(dir).GetFiles("Russian.dat", SearchOption.AllDirectories).ToList();
			progressBar.Maximum = fi.ToList().Count();
			for (int i = 0; i < fi.ToList().Count(); i++)
			{
				fi.ElementAt(i).Delete();
				progressBar.Value++;
				if (i % 25 == 0)
				{
					await Task.Delay(1);
				}
				else
				{
					await Task.Yield();
				}
			}
			Directory.Delete(dir + "\\Localization\\Russian", recursive: true);
		}
		catch (Exception)
		{
		}
		MainWindow.Instance.installButton.Content = "Установить";
		return true;
	}

	private static void Sze_ExtractionFinished(object sender, EventArgs e)
	{
		complete = true;
	}
}

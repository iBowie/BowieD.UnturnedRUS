using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using BowieD.UnturnedRUS.Properties;

namespace BowieD.UnturnedRUS;

public class MainWindow : Window, IComponentConnector
{
	public const string version = "22";

	public static bool init = true;

	public static MainWindow Instance;

	public static RusConfig config;

	public static bool updateAvailable = false;

	public static EAction installButtonAction = EAction.NONE;

	internal Button installButton;

	internal Button deleteButton;

	internal MenuItem MENU_DONATE;

	internal MenuItem MENU_COMMUNICATION;

	internal MenuItem menu_INFO;

	internal CheckBox MOD_TRANSLATION_CHECKBOX;

	internal CheckBox CITY_TRANSLATION_CHECKBOX;

	internal TextBlock unturnedChanges;

	internal TextBlock MENU_DONATIONS_SHOW;

	internal TextBlock MENU_MESSAGE;

	internal ProgressBar progress;

	internal Button modsInstallButton;

	private bool _contentLoaded;

	public bool IsConnected
	{
		get
		{
			try
			{
				return new Ping().Send("195.216.243.130", 3000).Status == IPStatus.Success;
			}
			catch (Exception ex)
			{
				Logger.Log(ex.ToString());
				return false;
			}
		}
	}

	public MainWindow()
	{
		InitializeComponent();
		if (AppDomain.CurrentDomain.BaseDirectory.StartsWith("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Temp"))
		{
			MessageBox.Show("Распакуйте файл установщика прежде чем его использовать.");
			Application.Current.Shutdown();
			return;
		}
		Logger.ClearLog();
		Logger.Log("Приложение запущено.");
		Logger.Log("Версия: 22");
		if (!IsConnected)
		{
			MessageBox.Show("Нет соединения с сервером загрузок. Попробуйте позже.");
			Application.Current.Shutdown();
			return;
		}
		config = Downloader.DownloadAndDeserialize<RusConfig>("http://bowiestuff.at.ua/rusinfo.xml");
		if (config.installerVersion != "22")
		{
			using (WebClient webClient = new WebClient())
			{
				webClient.DownloadFile("http://bowiestuff.at.ua/updater.e", AppDomain.CurrentDomain.BaseDirectory + "updaterINSTALLER.exe");
			}
			Process.Start(AppDomain.CurrentDomain.BaseDirectory + "updaterINSTALLER.exe", Path.GetFileName(Assembly.GetExecutingAssembly().Location));
			Application.Current.Shutdown();
		}
		else if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "updaterINSTALLER.exe"))
		{
			try
			{
				File.Delete(AppDomain.CurrentDomain.BaseDirectory + "updaterINSTALLER.exe");
			}
			catch
			{
				Logger.Log("Не удалось удалить программу обновления.");
			}
		}
		Instance = this;
		if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "SevenZipSharp.dll"))
		{
			Logger.Log("Докачивание библиотеки SevenZipSharp");
			Downloader.DownloadFile("http://bowiestuff.at.ua/SevenZipSharp.dll", AppDomain.CurrentDomain.BaseDirectory + "SevenZipSharp.dll");
			Logger.Log("Перезапуск приложения...");
			Restart(Process.GetCurrentProcess().Id, Process.GetCurrentProcess().MainModule.FileName);
			return;
		}
		unturnedChanges.Text = Downloader.DownloadString("http://bowiestuff.at.ua/changes.txt");
		RegisterEvents();
		CustomURL[] urls = config.urls;
		for (int i = 0; i < urls.Length; i++)
		{
			MenuItem asMenuItem = urls[i].AsMenuItem;
			asMenuItem.Click += Mi_Click;
			MENU_COMMUNICATION.Items.Add(asMenuItem);
		}
		MENU_MESSAGE.Text = config.message;
		if (Settings.Default.version != config.rusVersion)
		{
			updateAvailable = true;
		}
		if (Settings.Default.modsVersion != config.modVersion)
		{
			modsInstallButton.Content = "Доступно обновление";
		}
		if (Settings.Default.installed && !updateAvailable)
		{
			installButton.Content = "Переустановить русификатор";
			installButtonAction = EAction.REINSTALL;
		}
		else if (Settings.Default.installed && updateAvailable)
		{
			installButton.Content = "Обновить русификатор";
			installButtonAction = EAction.UPDATE;
		}
		else if (!Settings.Default.installed)
		{
			installButton.Content = "Установить русификатор";
			installButtonAction = EAction.INSTALL;
		}
		if (Settings.Default.allowedMods.Length > 0)
		{
			MOD_TRANSLATION_CHECKBOX.IsChecked = true;
		}
		DonationView();
		UpdateMenu();
		init = false;
	}

	private void Mi_Click(object sender, RoutedEventArgs e)
	{
		Process.Start(((MenuItem)sender).Tag.ToString());
	}

	public void Restart(int pid, string applicationName)
	{
		Process.Start(Application.ResourceAssembly.Location);
		Application.Current.Shutdown();
	}

	public static void UpdateMenu()
	{
		if (Settings.Default.installed)
		{
			Instance.menu_INFO.Header = $"Версия русификатора: {Settings.Default.version} | Версия Unturned: {Settings.Default.unturnedVersion} | Последнее обновление: {Settings.Default.updated}";
		}
		else
		{
			Instance.menu_INFO.Header = "Не установлено";
		}
		Instance.menu_INFO.ToolTip = "Директория установки: " + ((Settings.Default.baseInstallationFolder == "") ? "Не указано" : Settings.Default.baseInstallationFolder);
	}

	private static bool ExtractResourceAsFile(string resourceName, string filename)
	{
		if (string.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException(resourceName, "Не указано имя извлекаемого ресурса.");
		}
		if (string.IsNullOrEmpty(filename))
		{
			throw new ArgumentNullException(filename, "Не указано имя файла, в который сохранять ресурс.");
		}
		if (!(BowieD.UnturnedRUS.Properties.Resources.ResourceManager.GetObject(resourceName) is byte[] bytes))
		{
			return false;
		}
		try
		{
			File.WriteAllBytes(filename, bytes);
		}
		catch (Exception ex)
		{
			throw ex;
		}
		return File.Exists(filename);
	}

	public void RegisterEvents()
	{
		MENU_DONATE.Click += MENU_DONATE_Click;
		installButton.Click += InstallButton_Click;
		deleteButton.Click += DeleteButton_Click;
		Instance.Closed += Instance_Closed;
		MOD_TRANSLATION_CHECKBOX.Checked += MOD_TRANSLATION_CHECKBOX_Checked;
		CITY_TRANSLATION_CHECKBOX.Checked += CITY_TRANSLATION_CHECKBOX_Checked;
	}

	private void MOD_TRANSLATION_CHECKBOX_Checked(object sender, RoutedEventArgs e)
	{
		if (init)
		{
			return;
		}
		ModsSelect modsSelect = new ModsSelect(config.mods, Settings.Default.allowedMods.Split(' '));
		if (modsSelect.ShowDialog() == true)
		{
			MessageBox.Show("Моды выбраны.");
			Settings.Default.allowedMods = modsSelect.allowed;
			Settings.Default.Save();
			if (Settings.Default.allowedMods.Length == 0)
			{
				((CheckBox)sender).IsChecked = false;
			}
		}
		else if (Settings.Default.allowedMods.Length == 0)
		{
			((CheckBox)sender).IsChecked = false;
		}
	}

	private void CITY_TRANSLATION_CHECKBOX_Checked(object sender, RoutedEventArgs e)
	{
		MessageBox.Show("Установка перевода городов не рекомендуется по причине того," + Environment.NewLine + "что этот перевод заменяет файлы оригинального Unturned.");
	}

	public static void MoveDirectory(string source, string target)
	{
		string text = source.TrimEnd('\\', ' ');
		string newValue = target.TrimEnd('\\', ' ');
		foreach (IGrouping<string, string> item in from s in Directory.EnumerateFiles(text, "*", SearchOption.AllDirectories)
			group s by Path.GetDirectoryName(s))
		{
			string text2 = item.Key.Replace(text, newValue);
			Directory.CreateDirectory(text2);
			foreach (string item2 in item)
			{
				string text3 = Path.Combine(text2, Path.GetFileName(item2));
				if (File.Exists(text3))
				{
					File.Delete(text3);
				}
				File.Move(item2, text3);
			}
		}
		Directory.Delete(source, recursive: true);
	}

	private void Instance_Closed(object sender, EventArgs e)
	{
		Logger.Log($"Приложение закрыто с кодом {Environment.ExitCode}");
		Settings.Default.Save();
	}

	private async void DeleteButton_Click(object sender, RoutedEventArgs e)
	{
		Logger.Log("Пользователь нажал кнопку удаления русификатора.");
		deleteButton.IsEnabled = false;
		if (!Settings.Default.installed)
		{
			if (MessageBox.Show("Русификатор не был установлен. Вы действительно хотите произвести удаление?", "Нет установки", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				await Installer.Delete(Settings.Default.baseInstallationFolder);
				Settings.Default.installed = false;
				Settings.Default.baseInstallationFolder = "";
				Settings.Default.modInstallationFolder = "";
			}
		}
		else if (MessageBox.Show("Вы действительно хотите удалить русификатор?", "Удаление русификатора", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			await Installer.Delete(Settings.Default.baseInstallationFolder);
			Settings.Default.installed = false;
			Settings.Default.baseInstallationFolder = "";
			Settings.Default.modInstallationFolder = "";
		}
		Settings.Default.Save();
		deleteButton.IsEnabled = true;
		UpdateMenu();
	}

	private async void InstallButton_Click(object sender, RoutedEventArgs e)
	{
		Logger.Log("Пользователь нажал кнопку установки русификатора.");
		installButton.IsEnabled = false;
		deleteButton.IsEnabled = false;
		modsInstallButton.IsEnabled = false;
		switch (installButtonAction)
		{
		case EAction.INSTALL:
			await Installer.Install(Settings.Default.baseInstallationFolder, Settings.Default.modInstallationFolder, MOD_TRANSLATION_CHECKBOX.IsChecked.Value, CITY_TRANSLATION_CHECKBOX.IsChecked.Value);
			break;
		case EAction.UPDATE:
			await Installer.Update(Settings.Default.baseInstallationFolder, Settings.Default.modInstallationFolder, MOD_TRANSLATION_CHECKBOX.IsChecked.Value, CITY_TRANSLATION_CHECKBOX.IsChecked.Value);
			break;
		case EAction.REINSTALL:
			await Installer.Delete(Settings.Default.baseInstallationFolder);
			await Installer.Install(Settings.Default.baseInstallationFolder, Settings.Default.modInstallationFolder, MOD_TRANSLATION_CHECKBOX.IsChecked.Value, CITY_TRANSLATION_CHECKBOX.IsChecked.Value);
			break;
		}
		installButton.IsEnabled = true;
		deleteButton.IsEnabled = true;
		modsInstallButton.IsEnabled = true;
		UpdateMenu();
	}

	public async void DonationView()
	{
		while (true)
		{
			for (int i = 0; i < config.donationList.Length; i++)
			{
				MENU_DONATIONS_SHOW.Text = config.Donations.ElementAt(i).ToString();
				for (double op2 = 0.0; op2 < 1.0; op2 += 0.1)
				{
					MENU_DONATIONS_SHOW.Opacity = op2;
					await Task.Delay(10);
				}
				await Task.Delay(3000);
				for (double op2 = 1.0; op2 > 0.0; op2 -= 0.1)
				{
					MENU_DONATIONS_SHOW.Opacity = op2;
					await Task.Delay(10);
				}
			}
			await Task.Delay(1000);
		}
	}

	private void MENU_DONATE_Click(object sender, RoutedEventArgs e)
	{
		Logger.Log("Пользователь нажал кнопку \"Задонатить\".");
		MessageBox.Show($"Вы можете задонатить мне с помощью след. систем: {Environment.NewLine}QIWI: {config.qiwiNumber}{Environment.NewLine}WebMoney: {config.webMoneyNumber}{Environment.NewLine}Донаты от {config.minDonation} рублей попадут в список донатов.");
	}

	private async void Button_Click(object sender, RoutedEventArgs e)
	{
		installButton.IsEnabled = false;
		deleteButton.IsEnabled = false;
		modsInstallButton.IsEnabled = false;
		await Installer.Install(Settings.Default.baseInstallationFolder, Settings.Default.modInstallationFolder, installMods: true, installCity: false, installBase: false);
		modsInstallButton.Content = "Только моды";
		installButton.IsEnabled = true;
		deleteButton.IsEnabled = true;
		modsInstallButton.IsEnabled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/BowieD.UnturnedRUS;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			installButton = (Button)target;
			break;
		case 2:
			deleteButton = (Button)target;
			break;
		case 3:
			MENU_DONATE = (MenuItem)target;
			break;
		case 4:
			MENU_COMMUNICATION = (MenuItem)target;
			break;
		case 5:
			menu_INFO = (MenuItem)target;
			break;
		case 6:
			MOD_TRANSLATION_CHECKBOX = (CheckBox)target;
			break;
		case 7:
			CITY_TRANSLATION_CHECKBOX = (CheckBox)target;
			break;
		case 8:
			unturnedChanges = (TextBlock)target;
			break;
		case 9:
			MENU_DONATIONS_SHOW = (TextBlock)target;
			break;
		case 10:
			MENU_MESSAGE = (TextBlock)target;
			break;
		case 11:
			progress = (ProgressBar)target;
			break;
		case 12:
			modsInstallButton = (Button)target;
			modsInstallButton.Click += Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}

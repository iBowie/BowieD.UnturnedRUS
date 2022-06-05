using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Net.NetworkInformation;

namespace BowieD.UnturnedRUS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string version = "22";

        public MainWindow()
        {
            InitializeComponent();
            if (AppDomain.CurrentDomain.BaseDirectory.StartsWith($"C:\\Users\\{Environment.UserName}\\AppData\\Local\\Temp"))
            {
                MessageBox.Show("Распакуйте файл установщика прежде чем его использовать.");
                Application.Current.Shutdown();
                return;
            }
            Logger.ClearLog();
            Logger.Log("Приложение запущено.");
            Logger.Log("Версия: " + version);
            if (!IsConnected)
            {
                MessageBox.Show("Нет соединения с сервером загрузок. Попробуйте позже.");
                Application.Current.Shutdown();
                return;
            }
            config = Downloader.DownloadAndDeserialize<RusConfig>("http://bowiestuff.at.ua/rusinfo.xml");
            if (config.installerVersion != version)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile("http://bowiestuff.at.ua/updater.e", AppDomain.CurrentDomain.BaseDirectory + "updaterINSTALLER.exe");
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
                catch { Logger.Log("Не удалось удалить программу обновления."); }
            }
            Instance = this;
            if (!File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}SevenZipSharp.dll"))
            {
                Logger.Log("Докачивание библиотеки SevenZipSharp");
                Downloader.DownloadFile("http://bowiestuff.at.ua/SevenZipSharp.dll", $"{AppDomain.CurrentDomain.BaseDirectory}SevenZipSharp.dll");
                Logger.Log("Перезапуск приложения...");
                Restart(Process.GetCurrentProcess().Id, Process.GetCurrentProcess().MainModule.FileName);
                return;
            }
            unturnedChanges.Text = Downloader.DownloadString("http://bowiestuff.at.ua/changes.txt");
            RegisterEvents();
            foreach (CustomURL curl in config.urls)
            {
                MenuItem mi = curl.AsMenuItem;
                mi.Click += Mi_Click;
                MENU_COMMUNICATION.Items.Add(mi);
            }
            MENU_MESSAGE.Text = config.message;
            if (Properties.Settings.Default.version != config.rusVersion)
                updateAvailable = true;
            if (Properties.Settings.Default.modsVersion != config.modVersion)
            {
                modsInstallButton.Content = "Доступно обновление";
            }
            if (Properties.Settings.Default.installed && !updateAvailable)
            {
                installButton.Content = "Переустановить русификатор";
                installButtonAction = EAction.REINSTALL;
            }
            else if (Properties.Settings.Default.installed && updateAvailable)
            {
                installButton.Content = "Обновить русификатор";
                installButtonAction = EAction.UPDATE;
            }
            else if (!Properties.Settings.Default.installed)
            {
                installButton.Content = "Установить русификатор";
                installButtonAction = EAction.INSTALL;
            }
            if (Properties.Settings.Default.allowedMods.Length > 0)
            {
                MOD_TRANSLATION_CHECKBOX.IsChecked = true;
            }
            DonationView();
            UpdateMenu();
            init = false;
        }
        public static bool init = true;
        public bool IsConnected
        {
            get
            {
                try
                {
                    Ping p = new Ping();
                    PingReply reply = p.Send("195.216.243.130", 3000);
                    return reply.Status == IPStatus.Success;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                    return false;
                }
            }
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
            if (Properties.Settings.Default.installed)
            {
                Instance.menu_INFO.Header = $"Версия русификатора: {Properties.Settings.Default.version} | Версия Unturned: {Properties.Settings.Default.unturnedVersion} | Последнее обновление: {Properties.Settings.Default.updated}";
            }
            else
            {
                Instance.menu_INFO.Header = $"Не установлено";
            }
            Instance.menu_INFO.ToolTip = $"Директория установки: {(Properties.Settings.Default.baseInstallationFolder == "" ? "Не указано" : Properties.Settings.Default.baseInstallationFolder)}";
        }
        public static MainWindow Instance;
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
            var resourceBuffer = Properties.Resources.ResourceManager.GetObject(resourceName) as byte[];
            if (resourceBuffer == null) return false;
            try
            {
                File.WriteAllBytes(filename, resourceBuffer);
            }
            catch (Exception e)
            {
                throw e; //Поскольку в приложении есть обработчик ошибок, то просто генерим ошибку, которую он отловит.
            }
            return File.Exists(filename);
        }
        public void RegisterEvents()
        {
            MENU_DONATE.Click += MENU_DONATE_Click;
            installButton.Click += InstallButton_Click;
            deleteButton.Click += DeleteButton_Click;
            Instance.Closed += Instance_Closed;
            //modsTranslatedListButton.Click += ModsTranslatedListButton_Click;
            MOD_TRANSLATION_CHECKBOX.Checked += MOD_TRANSLATION_CHECKBOX_Checked;
            CITY_TRANSLATION_CHECKBOX.Checked += CITY_TRANSLATION_CHECKBOX_Checked;
        }

        private void MOD_TRANSLATION_CHECKBOX_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
                return;
            ModsSelect ms = new ModsSelect(config.mods, Properties.Settings.Default.allowedMods.Split(' '));
            if (ms.ShowDialog() == true)
            {
                MessageBox.Show("Моды выбраны.");
                Properties.Settings.Default.allowedMods = ms.allowed;
                Properties.Settings.Default.Save();
                if (Properties.Settings.Default.allowedMods.Length == 0)
                {
                    ((CheckBox)sender).IsChecked = false;
                }
            }
            else if (Properties.Settings.Default.allowedMods.Length == 0)
            {
                ((CheckBox)sender).IsChecked = false;
            }
        }

        private void CITY_TRANSLATION_CHECKBOX_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Установка перевода городов не рекомендуется по причине того,{Environment.NewLine}что этот перевод заменяет файлы оригинального Unturned.");
        }

        public static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }

        //private async void ModsTranslatedListButton_Click(object sender, RoutedEventArgs e)
        //{
        //    modsTranslatedListButton.IsEnabled = false;
        //    string oldlabel = modsTranslatedListButton.Content.ToString();
        //    modsTranslatedListButton.Content = "Загрузка...";
        //    Dictionary<string, string> dict = new Dictionary<string, string>();
        //    foreach (LocalizedMod lm in config.mods)
        //    {
        //        await Task.Delay(1);
        //        string title = Steam.GetModNameByTitle(Steam.GetUrlTitle($"https://steamcommunity.com/sharedfiles/filedetails/?id={lm.id}"));
        //        dict.Add(lm.id, title);
        //        await Task.Delay(250);
        //    }
        //    Dictionary<string, string> temporary = new Dictionary<string, string>();
        //    if (dict.Count() == 1)
        //    {
        //        MessageBox.Show($"{dict.ElementAt(0).Key} - {dict.ElementAt(0).Value}");
        //    }
        //    else if (dict.Count() <= 10)
        //    {
        //        string view = "";
        //        for (int k = 0; k < dict.Count(); k++)
        //        {
        //            view += $"{dict.ElementAt(k).Key} - {dict.ElementAt(k).Value}{Environment.NewLine}";
        //        }
        //        MessageBox.Show(view);
        //    }
        //    else if (dict.Count() > 10)
        //    {
        //        for (int k = 0; k < dict.Count(); k++)
        //        {
        //            if (k % 10 == 0 || (k + 10 > dict.Count()))
        //            {
        //                string view = "";
        //                foreach (string key in temporary.Keys)
        //                {
        //                    view += $"{key} - {temporary[key]}{Environment.NewLine}";
        //                }
        //                MessageBox.Show(view);
        //                temporary = new Dictionary<string, string>();
        //            }
        //            else
        //            {
        //                temporary.Add(dict.ElementAt(k).Key, dict.ElementAt(k).Value);
        //            }
        //        }
        //    }
        //    modsTranslatedListButton.Content = oldlabel;
        //    modsTranslatedListButton.IsEnabled = true;
        //}
        private void Instance_Closed(object sender, EventArgs e)
        {
            Logger.Log($"Приложение закрыто с кодом {Environment.ExitCode}");
            Properties.Settings.Default.Save();
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Пользователь нажал кнопку удаления русификатора.");
            deleteButton.IsEnabled = false;
            if (!Properties.Settings.Default.installed)
            {
                var result = MessageBox.Show("Русификатор не был установлен. Вы действительно хотите произвести удаление?", "Нет установки", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await Installer.Delete(Properties.Settings.Default.baseInstallationFolder);
                    Properties.Settings.Default.installed = false;
                    Properties.Settings.Default.baseInstallationFolder = "";
                    Properties.Settings.Default.modInstallationFolder = "";
                }
            }
            else
            {
                var result = MessageBox.Show("Вы действительно хотите удалить русификатор?", "Удаление русификатора", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await Installer.Delete(Properties.Settings.Default.baseInstallationFolder);
                    Properties.Settings.Default.installed = false;
                    Properties.Settings.Default.baseInstallationFolder = "";
                    Properties.Settings.Default.modInstallationFolder = "";
                }
            }
            Properties.Settings.Default.Save();
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
                case EAction.NONE:
                    break;
                case EAction.INSTALL:
                    await Installer.Install(Properties.Settings.Default.baseInstallationFolder, Properties.Settings.Default.modInstallationFolder, (bool)MOD_TRANSLATION_CHECKBOX.IsChecked, (bool)CITY_TRANSLATION_CHECKBOX.IsChecked);
                    break;
                case EAction.UPDATE:
                    await Installer.Update(Properties.Settings.Default.baseInstallationFolder, Properties.Settings.Default.modInstallationFolder, (bool)MOD_TRANSLATION_CHECKBOX.IsChecked, (bool)CITY_TRANSLATION_CHECKBOX.IsChecked);
                    break;
                case EAction.REINSTALL:
                    await Installer.Delete(Properties.Settings.Default.baseInstallationFolder);
                    await Installer.Install(Properties.Settings.Default.baseInstallationFolder, Properties.Settings.Default.modInstallationFolder, (bool)MOD_TRANSLATION_CHECKBOX.IsChecked, (bool)CITY_TRANSLATION_CHECKBOX.IsChecked);
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
                for (int k = 0; k < config.donationList.Length; k++)
                {
                    MENU_DONATIONS_SHOW.Text = config.Donations.ElementAt(k).ToString();
                    for (double op = 0; op < 1; op += 0.1)
                    {
                        MENU_DONATIONS_SHOW.Opacity = op;
                        await Task.Delay(10);
                    }
                    await Task.Delay(3000);
                    for (double op = 1; op > 0; op -= 0.1)
                    {
                        MENU_DONATIONS_SHOW.Opacity = op;
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
        public static RusConfig config;
        public static bool updateAvailable = false;
        public static EAction installButtonAction = EAction.NONE;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            installButton.IsEnabled = false;
            deleteButton.IsEnabled = false;
            modsInstallButton.IsEnabled = false;

            await Installer.Install(Properties.Settings.Default.baseInstallationFolder, Properties.Settings.Default.modInstallationFolder, true, false, false);
            modsInstallButton.Content = "Только моды";

            installButton.IsEnabled = true;
            deleteButton.IsEnabled = true;
            modsInstallButton.IsEnabled = true;
        }
    }
    public enum EAction
    {
        NONE, INSTALL, UPDATE, REINSTALL
    }
    public class RusConfig
    {
        [XmlAttribute]
        public string unturnedVersion { get; set; }
        [XmlAttribute]
        public string rusVersion { get; set; }
        [XmlAttribute]
        public string installerVersion { get; set; }
        [XmlAttribute]
        public string modVersion { get; set; }
        public string modDownloadURL { get; set; }
        public string cityDownloadURL { get; set; }
        public string baseDownloadURL { get; set; }
        public string qiwiNumber { get; set; }
        public string webMoneyNumber { get; set; }
        public string message { get; set; }
        public float minDonation { get; set; }
        public CustomURL[] urls;
        [XmlArray("mods"), XmlArrayItem("mod")]
        public LocalizedMod[] mods;
        public Donation[] donationList;
        [XmlIgnore]
        public List<Donation> Donations
        {
            get
            {
                return donationList.ToList();
            }
        }
    }
    public class Donation
    {
        [XmlAttribute]
        public string displayName { get; set; }
        [XmlAttribute]
        public float donated { get; set; }
        public override string ToString()
        {
            return displayName + " - " + donated + " р.";
        }
    }
    public class CustomURL
    {
        [XmlAttribute]
        public string url;
        [XmlAttribute]
        public string name;

        [XmlIgnore]
        public MenuItem AsMenuItem
        {
            get
            {
                MenuItem result = new MenuItem();
                result.Header = name;
                result.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/" + name + ".png")) };
                result.Tag = url;
                return result;
            }
        }
    }
    public static class Downloader
    {
        public static void DownloadFile(string url, string fileName)
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile(url, fileName);
            }
        }
        public static string DownloadString(string url)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                return wc.DownloadString(url);
            }
        }
        public static T DownloadAndDeserialize<T>(string url) where T : class
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                string downloaded = wc.DownloadString(url);
                using (TextReader tr = new StringReader(downloaded))
                {
                    XmlReader reader = XmlReader.Create(tr);
                    XmlSerializer ser = new XmlSerializer(typeof(T));
                    return ser.Deserialize(reader) as T;
                }
            }
        }
    }
    public static class Logger
    {
        public static void Log(string txt)
        {
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "log", true, Encoding.UTF8))
            {
                sw.WriteLine($"[{DateTime.Now}] - {txt}");
            }
        }
        public static void ClearLog()
        {
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "log", false, Encoding.UTF8))
            {
                sw.Write("");
            }
        }
    }
    public static class Installer
    {
        public static async Task<bool> Install(string dir, string modDir, bool installMods, bool installCity, bool installBase = true)
        {
            if (installBase && dir == "" || !File.Exists(dir + @"\Unturned.exe"))
            {
                MessageBox.Show($"Укажите папку установки русификатора (действие делается один раз).{Environment.NewLine}Путь должен выглядеть примерно так: ДИСК:/ПУТЬ ДО ИГРЫ");
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return false;
                else
                {
                    if (!File.Exists(fbd.SelectedPath + @"\Unturned.exe"))
                        return false;
                }
                Properties.Settings.Default.baseInstallationFolder = fbd.SelectedPath;
                dir = fbd.SelectedPath;
                Properties.Settings.Default.Save();
            }
            if (modDir == "" && installMods)
            {
                MessageBox.Show($"Укажите папку установки русификатора модов (действие делается один раз).{Environment.NewLine}Путь должен выглядеть примерно так: ДИСК:/ПУТЬ ДО STEAM/steamapps/common/workshop/304930");
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return false;
                Properties.Settings.Default.modInstallationFolder = fbd.SelectedPath;
                modDir = fbd.SelectedPath;
                Properties.Settings.Default.Save();
            }
            using (WebClient wc = new WebClient())
            {
                if (installBase)
                {
                    if (MainWindow.config.baseDownloadURL == "")
                    {
                        installBase = false;
                    }
                    else
                        wc.DownloadFile(MainWindow.config.baseDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "rus.zip");
                }
                if (installMods)
                {
                    if (MainWindow.config.modDownloadURL == "")
                    {
                        installMods = false;
                    }
                    else
                    {
                        wc.DownloadFile(MainWindow.config.modDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "mod.zip");
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
                        wc.DownloadFile(MainWindow.config.cityDownloadURL, AppDomain.CurrentDomain.BaseDirectory + "city.zip");
                    }
                }
                if (installBase || installCity || installMods)
                    wc.DownloadFile("http://bowiestuff.at.ua/7z.dll", AppDomain.CurrentDomain.BaseDirectory + "7z.dll");
            }
            if (installBase)
            {
                using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "rus.zip", FileMode.Open))
                {
                    using (SevenZip.SevenZipExtractor sze = new SevenZip.SevenZipExtractor(fs.Name))
                    {
                        sze.ExtractionFinished += Sze_ExtractionFinished;
                        sze.Extracting += Sze_Extracting;
                        sze.BeginExtractArchive(dir + "\\");
                        complete = false;
                        while (!complete)
                            await Task.Delay(1);
                        sze.ExtractionFinished -= Sze_ExtractionFinished;
                        sze.Extracting -= Sze_Extracting;
                    }
                }
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "rus.zip");
            }
            if (installMods)
            {
                using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "mod.zip", FileMode.Open))
                {
                    using (var sze = new SevenZip.SevenZipExtractor(fs.Name))
                    {
                        sze.ExtractionFinished += Sze_ExtractionFinished;
                        sze.Extracting += Sze_Extracting;
                        //sze.BeginExtractArchive(modDir + "\\");
                        sze.BeginExtractArchive(AppDomain.CurrentDomain.BaseDirectory + @"temp\");
                        complete = false;
                        while (!complete)
                            await Task.Delay(1);
                        DirectoryInfo[] dirs = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"temp\").GetDirectories("*", SearchOption.TopDirectoryOnly);
                        foreach (DirectoryInfo ddir in dirs)
                        {
                            if (Properties.Settings.Default.allowedMods.Contains(ddir.Name))
                            {
                                //ddir.MoveTo(modDir + @"\" + ddir.Name);
                                MainWindow.MoveDirectory(ddir.FullName, $@"{modDir}\{ddir.Name}");
                            }
                            else
                            {
                                ddir.Delete(true);
                            }
                        }
                        Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "temp", true);
                        sze.Extracting -= Sze_Extracting;
                        sze.ExtractionFinished -= Sze_ExtractionFinished;
                    }
                }
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "mod.zip");
            }
            if (installCity)
            {
                using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "city.zip", FileMode.Open))
                {
                    using (var sze = new SevenZip.SevenZipExtractor(fs.Name))
                    {
                        sze.ExtractionFinished += Sze_ExtractionFinished;
                        sze.Extracting += Sze_Extracting;
                        sze.BeginExtractArchive(dir + "\\");
                        complete = false;
                        while (!complete)
                            await Task.Delay(1);
                        sze.Extracting -= Sze_Extracting;
                        sze.ExtractionFinished -= Sze_ExtractionFinished;
                    }
                }
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "city.zip");
            }
            if (installBase)
            {
                Properties.Settings.Default.updated = DateTime.Now;
                Properties.Settings.Default.installed = true;
                Properties.Settings.Default.version = MainWindow.config.rusVersion;
                Properties.Settings.Default.unturnedVersion = MainWindow.config.unturnedVersion;
                MainWindow.Instance.installButton.Content = "Переустановить русификатор";
            }
            if (installMods)
            {
                Properties.Settings.Default.modsVersion = MainWindow.config.modVersion;
            }
            Properties.Settings.Default.Save();
            return true;
        }

        private static void Sze_Extracting(object sender, SevenZip.ProgressEventArgs e)
        {
            progressBar.Maximum = 100;
            progressBar.Value = e.PercentDone;
        }

        public static async Task<bool> Update(string dir, string modDir, bool updateMods, bool updateCity)
        {
            return await Install(dir, modDir, updateMods, updateCity);
        }

        public static async Task<bool> Delete(string dir)
        {
            try
            {
                progressBar.Value = 0;
                List<FileInfo> fi = new DirectoryInfo(dir).GetFiles("Russian.dat", SearchOption.AllDirectories).ToList();
                progressBar.Maximum = fi.ToList().Count();
                //foreach (FileInfo fl in fi)
                //{
                //    await Task.Yield();
                //    fl.Delete();
                //    progressBar.Value++;
                //}
                for (int k = 0; k < fi.ToList().Count(); k++)
                {
                    fi.ElementAt(k).Delete();
                    progressBar.Value++;
                    if (k % 25 == 0)
                        await Task.Delay(1);
                    else
                        await Task.Yield();
                }
                Directory.Delete(dir + @"\Localization\Russian", true);
            }
            catch (Exception) { }
            MainWindow.Instance.installButton.Content = "Установить";
            return true;
        }

        private static void Sze_ExtractionFinished(object sender, EventArgs e)
        {
            complete = true;
        }

        private static bool complete = false;
        
        public static ProgressBar progressBar
        {
            get
            {
                return MainWindow.Instance.progress;
            }
        }
    }
    public static class Steam
    {
        public static string GetUrlTitle(string url)
        {
            using (WebClient wc = new WebClient())
            {
                string source = wc.DownloadString(url);
                return Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
            }
        }
        public static string GetModNameByTitle(string title)
        {
            string sub = title.Substring(title.LastIndexOf(':'));
            sub = sub.Substring(1);
            return sub;
        }
    }
    public class LocalizedMod
    {
        [XmlAttribute]
        public string id;
        [XmlAttribute]
        public string name;
    }
}

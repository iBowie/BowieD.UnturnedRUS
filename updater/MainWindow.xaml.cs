using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace updater
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public async void MainCycle()
        {
            int tries = 0;
            while (Process.GetProcessesByName(oldName).Count() > 0)
            {
                if (tries > 50)
                {
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + oldName);
                    Application.Current.Shutdown();
                    break;
                }
                try
                {
                    Process.GetProcessesByName(oldName)[0].Kill();
                }
                catch { tries++; }
                await Task.Delay(1);
            }
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri("http://bowiestuff.at.ua/BowieD.UnturnedRUS.e"), AppDomain.CurrentDomain.BaseDirectory + oldName);
                while (!end)
                {
                    await Task.Delay(1);
                }
            }
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + oldName);
            Application.Current.Shutdown();
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            end = true;
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Maximum = e.TotalBytesToReceive;
            progressBar.Value = e.BytesReceived;
        }

        public string oldName;
        public static bool end = false;
    }
}

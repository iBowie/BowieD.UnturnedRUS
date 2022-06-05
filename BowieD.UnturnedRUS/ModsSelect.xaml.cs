using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace BowieD.UnturnedRUS
{
    /// <summary>
    /// Логика взаимодействия для ModsSelect.xaml
    /// </summary>
    public partial class ModsSelect : Window
    {
        public ModsSelect(LocalizedMod[] idList, string[] preAllowed)
        {
            InitializeComponent();
            Title = "Выбор модов";
            saveButton.Click += SaveButton_Click;
            foreach (var id in idList)
            {
                CheckBox item = new CheckBox();
                item.Content = id.name;
                item.Tag = id.id;
                if (preAllowed.Contains(id.id))
                {
                    item.IsChecked = true;
                }
                treeView.Items.Add(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            GetWindow(this).DialogResult = true;
            GetWindow(this).Close();
        }

        public string allowed
        {
            get
            {
                string result = "";
                foreach (CheckBox cb in treeView.Items)
                {
                    if (cb.IsChecked == true)
                        result += $"{cb.Tag.ToString()} ";
                }
                if (result.Length > 0)
                {
                    return result.Substring(0, result.Length - 1);
                }
                else
                    return result;
            }
        }
    }
}

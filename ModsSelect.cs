using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BowieD.UnturnedRUS;

public partial class ModsSelect : Window
{
	public string allowed
	{
		get
		{
			string text = "";
			foreach (CheckBox item in (IEnumerable)treeView.Items)
			{
				if (item.IsChecked == true)
				{
					text = text + item.Tag.ToString() + " ";
				}
			}
			if (text.Length > 0)
			{
				return text.Substring(0, text.Length - 1);
			}
			return text;
		}
	}

	public ModsSelect(LocalizedMod[] idList, string[] preAllowed)
	{
		InitializeComponent();
		base.Title = "Выбор модов";
		saveButton.Click += SaveButton_Click;
		foreach (LocalizedMod localizedMod in idList)
		{
			CheckBox checkBox = new CheckBox
			{
				Content = localizedMod.name,
				Tag = localizedMod.id
			};
			if (preAllowed.Contains(localizedMod.id))
			{
				checkBox.IsChecked = true;
			}
			treeView.Items.Add(checkBox);
		}
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		Window.GetWindow(this).DialogResult = true;
		Window.GetWindow(this).Close();
	}
}

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace BowieD.UnturnedRUS;

public class App : Application
{
	private void Application_Startup(object sender, StartupEventArgs e)
	{
		new MainWindow().Show();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		base.Startup += Application_Startup;
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}

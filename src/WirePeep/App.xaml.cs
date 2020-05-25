#region Using Directives

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	public partial class App : Application
	{
		#region Constructors

		public App()
		{
			WindowsUtility.InitializeApplication(nameof(WirePeep), null);
		}

		#endregion

		#region Protected Methods

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			MainWindow mainWindow = new MainWindow();
			this.MainWindow = mainWindow;

			AppOptions appOptions = mainWindow.LoadSettings();
			if (appOptions.StartMinimized)
			{
				mainWindow.WindowState = WindowState.Minimized;

				if (appOptions.MinimizeToTray)
				{
					mainWindow.MinimizeToTray();
				}
			}

			if (mainWindow.ShowInTaskbar)
			{
				mainWindow.Show();
			}
		}

		#endregion
	}
}

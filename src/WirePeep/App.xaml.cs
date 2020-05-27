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
#pragma warning disable CA1001 // Types that own disposable fields should be disposable. WPF Applications can't be disposable. OnExit does the clean-up.
	public partial class App : Application
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		#region Private Data Members

		private MainWindow mainWindow;

		#endregion

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

			this.mainWindow = new MainWindow();

			AppOptions appOptions = this.mainWindow.LoadNonWindowSettings();
			if (appOptions.StartMinimized)
			{
				this.mainWindow.WindowState = WindowState.Minimized;

				if (appOptions.MinimizeToTray)
				{
					this.mainWindow.ShowInTaskbar = false;
				}
			}

			if (this.mainWindow.ShowInTaskbar)
			{
				this.mainWindow.Show();
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			// We have to use our member variable here because the Application.MainWindow property is null by this point.
			this.mainWindow?.SaveNonWindowSettings();
			this.mainWindow?.Dispose();
		}

		#endregion
	}
}

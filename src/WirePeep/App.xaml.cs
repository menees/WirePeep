#region Using Directives

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Menees;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable. WPF Applications can't be disposable. OnExit does the clean-up.
	public partial class App : Application
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		#region Private Data Members

		private Mutex singleInstanceMutex;
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
			// Only allow one instance of this executable to run at a time.
			string executable = ApplicationInfo.ExecutableFile;

			// Mutex names can't contain backslash because it's used for kernel object namespaces.
			// https://stackoverflow.com/a/4314132/1882616 and https://stackoverflow.com/a/20714164/1882616
			string mutexName = executable.Replace('\\', '|');
			this.singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);
			if (!createdNew)
			{
				// In a perfect world, we'd activate the other running instance, but that's tricky.
				// The .NET Process object doesn't give us a direct way to communicate with other
				// processes using the same executable, and the other process may not even have a
				// visible main window (e.g., if it's minimized to the tray). We could implement some
				// RPC call with extra work, or we could try to shoehorn in VB's single-instance
				// support via WindowsFormsApplicationBase. But for now, I'll just silently quit.
				this.Shutdown();
			}
			else
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
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			// We have to use our member variable here because the Application.MainWindow property is null by this point.
			this.mainWindow?.SaveNonWindowSettings();
			this.mainWindow?.Dispose();

			// We can't safely call ReleaseMutex because we can't guarantee that the thread that acquired it is the current thread.
			// But our process is exiting, so Windows will release the mutex automatically when we fully exit.
			// https://stackoverflow.com/a/32535863/1882616
			this.singleInstanceMutex?.Dispose();
		}

		#endregion
	}
}

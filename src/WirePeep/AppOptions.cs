#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Menees;
using Microsoft.Win32;

#endregion

namespace WirePeep
{
	/// <summary>
	/// These options relate to the application user interface (not the common back-end objects).
	/// </summary>
	internal sealed class AppOptions
	{
		#region Constructors

		public AppOptions(ISettingsNode settingsNode)
		{
			if (settingsNode != null)
			{
				this.RunAtLogin = settingsNode.GetValue(nameof(this.RunAtLogin), this.RunAtLogin);
				this.AutoStartMinimized = settingsNode.GetValue(nameof(this.AutoStartMinimized), this.AutoStartMinimized);
				this.MinimizeToTray = settingsNode.GetValue(nameof(this.MinimizeToTray), this.MinimizeToTray);
				this.AlwaysOnTop = settingsNode.GetValue(nameof(this.AlwaysOnTop), this.AlwaysOnTop);
				this.ConfirmClose = settingsNode.GetValue(nameof(this.ConfirmClose), this.ConfirmClose);
			}

			// Interesting info about %SystemRoot% vs %WinDir%: https://superuser.com/a/638335/430448
			const string DefaultFailureSound = @"%SystemRoot%\Media\Windows Notify System Generic.wav";
			const string DefaultReconnectSound = @"%SystemRoot%\Media\Windows Background.wav";
			this.FailureOptions = new AlertOptions(true, DefaultFailureSound, settingsNode?.GetSubNode(nameof(this.FailureOptions), false));
			this.ReconnectOptions = new AlertOptions(false, DefaultReconnectSound, settingsNode?.GetSubNode(nameof(this.ReconnectOptions), false));
			this.CommonOptions = new CommonOptions(settingsNode?.GetSubNode(nameof(this.CommonOptions), false));
		}

		#endregion

		#region Public Properties

		public bool RunAtLogin { get; set; }

		public bool AutoStartMinimized { get; set; }

		public bool MinimizeToTray { get; set; }

		public bool AlwaysOnTop { get; set; }

		public bool ConfirmClose { get; set; }

		public AlertOptions FailureOptions { get; }

		public AlertOptions ReconnectOptions { get; }

		public CommonOptions CommonOptions { get; }

		#endregion

		#region Public Methods

		public void Save(ISettingsNode settingsNode)
		{
			settingsNode.SetValue(nameof(this.RunAtLogin), this.RunAtLogin);
			settingsNode.SetValue(nameof(this.AutoStartMinimized), this.AutoStartMinimized);
			settingsNode.SetValue(nameof(this.MinimizeToTray), this.MinimizeToTray);
			settingsNode.SetValue(nameof(this.AlwaysOnTop), this.AlwaysOnTop);
			settingsNode.SetValue(nameof(this.ConfirmClose), this.ConfirmClose);

			this.FailureOptions.Save(settingsNode.GetSubNode(nameof(this.FailureOptions), true));
			this.ReconnectOptions.Save(settingsNode.GetSubNode(nameof(this.ReconnectOptions), true));
			this.CommonOptions.Save(settingsNode.GetSubNode(nameof(this.CommonOptions), true));
		}

		public void Apply(Window window)
		{
			using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
			{
				if (this.RunAtLogin)
				{
					string commandLine = TextUtility.EnsureQuotes(ApplicationInfo.ExecutableFile);
					if (this.AutoStartMinimized)
					{
						commandLine += " /Minimize";
					}

					runKey.SetValue(nameof(WirePeep), commandLine);
				}
				else
				{
					runKey.DeleteValue(nameof(WirePeep), false);
				}
			}

			if (window != null)
			{
				window.Topmost = this.AlwaysOnTop;
			}
		}

		#endregion
	}
}

#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Menees;
using Menees.Windows.Presentation;
using Microsoft.Win32;

#endregion

namespace WirePeep
{
	public partial class OptionsDialog : ExtendedDialog
	{
		#region Private Data Members

		private MediaPlayer? mediaPlayer;

		#endregion

		#region Constructors

		public OptionsDialog()
		{
			this.InitializeComponent();
		}

		#endregion

		#region Private Properties

		private string LogFolder => this.logFolder.Text.Trim();

		#endregion

		#region Internal Methods

		internal bool Execute(Window owner, AppOptions appOptions)
		{
			this.Owner = owner;

			this.runAtLogin.IsChecked = appOptions.RunAtLogin;
			this.autoStartMinimized.IsChecked = appOptions.AutoStartMinimized;
			this.minimizeToTray.IsChecked = appOptions.MinimizeToTray;
			this.alwaysOnTop.IsChecked = appOptions.AlwaysOnTop;
			this.confirmClose.IsChecked = appOptions.ConfirmClose;

			this.showWindowOnFailure.IsChecked = appOptions.FailureOptions.ShowWindow;
			this.showNotificationOnFailure.IsChecked = appOptions.FailureOptions.ShowNotification;
			this.colorTaskbarOnFailure.IsChecked = appOptions.FailureOptions.ColorInactiveTaskbarItem;
			this.playSoundOnFailure.IsChecked = appOptions.FailureOptions.PlaySound;
			this.soundOnFailure.ToolTip = appOptions.FailureOptions.SoundFileName;

			this.showWindowOnReconnect.IsChecked = appOptions.ReconnectOptions.ShowWindow;
			this.showNotificationOnReconnect.IsChecked = appOptions.ReconnectOptions.ShowNotification;
			this.colorTaskbarOnReconnect.IsChecked = appOptions.ReconnectOptions.ColorInactiveTaskbarItem;
			this.playSoundOnReconnect.IsChecked = appOptions.ReconnectOptions.PlaySound;
			this.soundOnReconnect.ToolTip = appOptions.ReconnectOptions.SoundFileName;

			this.logFileNameFormat.SelectedIndex = (int)appOptions.CommonOptions.LogFileNameFormat;
			this.logFolder.Text = appOptions.CommonOptions.LogFolder;

			bool result = this.ShowDialog() ?? false;
			if (result)
			{
				appOptions.RunAtLogin = this.runAtLogin.IsChecked ?? false;
				appOptions.AutoStartMinimized = this.autoStartMinimized.IsChecked ?? false;
				appOptions.MinimizeToTray = this.minimizeToTray.IsChecked ?? false;
				appOptions.AlwaysOnTop = this.alwaysOnTop.IsChecked ?? false;
				appOptions.ConfirmClose = this.confirmClose.IsChecked ?? false;

				appOptions.FailureOptions.ShowWindow = this.showWindowOnFailure.IsChecked ?? false;
				appOptions.FailureOptions.ShowNotification = this.showNotificationOnFailure.IsChecked ?? false;
				appOptions.FailureOptions.ColorInactiveTaskbarItem = this.colorTaskbarOnFailure.IsChecked ?? false;
				appOptions.FailureOptions.PlaySound = this.playSoundOnFailure.IsChecked ?? false;
				appOptions.FailureOptions.SoundFileName = this.soundOnFailure.ToolTip?.ToString() ?? string.Empty;

				appOptions.ReconnectOptions.ShowWindow = this.showWindowOnReconnect.IsChecked ?? false;
				appOptions.ReconnectOptions.ShowNotification = this.showNotificationOnReconnect.IsChecked ?? false;
				appOptions.ReconnectOptions.ColorInactiveTaskbarItem = this.colorTaskbarOnReconnect.IsChecked ?? false;
				appOptions.ReconnectOptions.PlaySound = this.playSoundOnReconnect.IsChecked ?? false;
				appOptions.ReconnectOptions.SoundFileName = this.soundOnReconnect.ToolTip?.ToString() ?? string.Empty;

				appOptions.CommonOptions.LogFileNameFormat = (LogFileNameFormat)this.logFileNameFormat.SelectedIndex;
				appOptions.CommonOptions.LogFolder = this.LogFolder;
			}

			this.mediaPlayer?.Stop();
			return result;
		}

		#endregion

		#region Private Methods

		private void SelectSoundFile(string title, FrameworkElement element)
		{
			OpenFileDialog dialog = new()
			{
				Filter = "Sound files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*",
				FileName = Environment.ExpandEnvironmentVariables(element.ToolTip?.ToString() ?? string.Empty),
				Title = title,
			};

			if (dialog.ShowDialog(this) ?? false)
			{
				// Change C:\Windows references to the SystemRoot environment variable to make settings files more portable.
				string? systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
				if (systemRoot.IsNotEmpty())
				{
					element.ToolTip = TextUtility.Replace(dialog.FileName, systemRoot, "%SystemRoot%", StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		private void PlaySoundFile(FrameworkElement element)
		{
			AlertOptions.PlaySoundFile(element.ToolTip?.ToString(), ref this.mediaPlayer);
		}

		#endregion

		#region Private Event Handlers

		private void OKClicked(object sender, RoutedEventArgs e)
		{
			List<string> errors = new();

			string folder = this.LogFolder;
			if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
			{
				errors.Add("The specified log folder doesn't exist.");
			}

			if (errors.Count == 0)
			{
				this.DialogResult = true;
			}
			else
			{
				WindowsUtility.ShowError(this, string.Join(Environment.NewLine, errors));
			}
		}

		private void SelectLogFolderClicked(object sender, RoutedEventArgs e)
		{
			string? folder = WindowsUtility.SelectFolder(this, "Select Log Folder", this.LogFolder);
			if (folder.IsNotEmpty())
			{
				this.logFolder.Text = folder;
			}
		}

		private void SelectFailureClicked(object sender, RoutedEventArgs e)
		{
			this.SelectSoundFile("Select Failure Sound", this.soundOnFailure);
		}

		private void SelectReconnectClicked(object sender, RoutedEventArgs e)
		{
			this.SelectSoundFile("Select Reconnect Sound", this.soundOnReconnect);
		}

		private void PlayFailureClicked(object sender, RoutedEventArgs e)
		{
			this.PlaySoundFile(this.soundOnFailure);
		}

		private void PlayReconnectClicked(object sender, RoutedEventArgs e)
		{
			this.PlaySoundFile(this.soundOnReconnect);
		}

		#endregion
	}
}

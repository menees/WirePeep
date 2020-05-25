#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using Menees;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	/// <summary>
	/// These options apply to either "On Failure" or "On Reconnect"
	/// </summary>
	internal sealed class AlertOptions
	{
		#region Constructors

		public AlertOptions(bool isFailure, string defaultSoundFileName, ISettingsNode settingsNode)
		{
			this.IsFailure = isFailure;
			this.SoundFileName = defaultSoundFileName;

			if (settingsNode != null)
			{
				this.ShowWindow = settingsNode.GetValue(nameof(this.ShowWindow), this.ShowWindow);
				this.ShowNotification = settingsNode.GetValue(nameof(this.ShowNotification), this.ShowNotification);
				this.ColorInactiveTaskbarItem = settingsNode.GetValue(nameof(this.ColorInactiveTaskbarItem), this.ColorInactiveTaskbarItem);
				this.PlaySound = settingsNode.GetValue(nameof(this.PlaySound), this.PlaySound);
				this.SoundFileName = settingsNode.GetValue(nameof(this.SoundFileName), this.SoundFileName);
			}
		}

		#endregion

		#region Public Properties

		public bool ShowWindow { get; set; } = true;

		public bool ShowNotification { get; set; } = true;

		public bool ColorInactiveTaskbarItem { get; set; } = true;

		public bool PlaySound { get; set; } = true;

		public string SoundFileName { get; set; }

		#endregion

		#region Private Properties

		private bool IsFailure { get; }

		#endregion

		#region Public Methods

		public static void PlaySoundFile(string soundFileName, ref MediaPlayer mediaPlayer)
		{
			if (!string.IsNullOrEmpty(soundFileName))
			{
				string filePath = Environment.ExpandEnvironmentVariables(soundFileName);
				if (File.Exists(filePath))
				{
					if (mediaPlayer == null)
					{
						mediaPlayer = new MediaPlayer();
					}

					// https://www.wpf-tutorial.com/audio-video/playing-audio/
					mediaPlayer.Open(new Uri(filePath));
					mediaPlayer.Volume = 1;
					mediaPlayer.Play();
				}
			}
		}

		public void Save(ISettingsNode settingsNode)
		{
			settingsNode.SetValue(nameof(this.ShowWindow), this.ShowWindow);
			settingsNode.SetValue(nameof(this.ShowNotification), this.ShowNotification);
			settingsNode.SetValue(nameof(this.ColorInactiveTaskbarItem), this.ColorInactiveTaskbarItem);
			settingsNode.SetValue(nameof(this.PlaySound), this.PlaySound);
			settingsNode.SetValue(nameof(this.SoundFileName), this.SoundFileName);
		}

		public void Alert(
			Window window,
			string notificationMessage,
			NotifyIcon notifyIcon,
			ref MediaPlayer mediaPlayer)
		{
			if (window != null)
			{
				if (this.ShowWindow)
				{
					WindowsUtility.BringToFront(window);
				}
				else if (this.ColorInactiveTaskbarItem && !ApplicationInfo.IsActivated)
				{
					TaskbarItemInfo taskbarItem = window.TaskbarItemInfo;
					if (taskbarItem == null)
					{
						taskbarItem = new TaskbarItemInfo();
						window.TaskbarItemInfo = taskbarItem;
					}

					taskbarItem.ProgressState = this.IsFailure ? TaskbarItemProgressState.Error : TaskbarItemProgressState.Normal;
					taskbarItem.ProgressValue = 1;
				}
			}

			if (notifyIcon != null && this.ShowNotification)
			{
				const int IgnoredTimeout = 5000;
				notifyIcon.ShowBalloonTip(IgnoredTimeout, nameof(WirePeep), notificationMessage, this.IsFailure ? ToolTipIcon.Error : ToolTipIcon.Info);
			}

			if (this.PlaySound)
			{
				PlaySoundFile(this.SoundFileName, ref mediaPlayer);
			}
		}

		#endregion
	}
}

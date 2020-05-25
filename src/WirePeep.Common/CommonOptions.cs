#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class CommonOptions
	{
		#region Private Data Members

		private LogFileNameFormat logFileNameFormat = LogFileNameFormat.UtcNow;
		private string logFolder;

		#endregion

		#region Constructors

		public CommonOptions(ISettingsNode settingsNode)
		{
			if (settingsNode != null)
			{
				this.logFileNameFormat = settingsNode.GetValue(nameof(this.LogFileNameFormat), this.LogFileNameFormat);
				this.logFolder = settingsNode.GetValue(nameof(this.LogFolder), string.Empty);
				this.ScrollLockSimulatesFailure = settingsNode.GetValue(nameof(this.ScrollLockSimulatesFailure), this.ScrollLockSimulatesFailure);
			}

			this.ValidateLogFileNameFormat();
			this.ValidateLogFolder();
		}

		#endregion

		#region Public Properties

		public LogFileNameFormat LogFileNameFormat
		{
			get
			{
				return this.logFileNameFormat;
			}

			set
			{
				this.logFileNameFormat = value;
				this.ValidateLogFileNameFormat();
			}
		}

		public string LogFolder
		{
			get
			{
				return this.logFolder;
			}

			set
			{
				this.logFolder = value;
				this.ValidateLogFolder();
			}
		}

		public bool ScrollLockSimulatesFailure { get; set; } = ApplicationInfo.IsDebugBuild;

		#endregion

		#region Public Methods

		public void Save(ISettingsNode settingsNode)
		{
			settingsNode.SetValue(nameof(this.LogFileNameFormat), this.logFileNameFormat);
			settingsNode.SetValue(nameof(this.LogFolder), this.logFolder);
			settingsNode.SetValue(nameof(this.ScrollLockSimulatesFailure), this.ScrollLockSimulatesFailure);
		}

		public string GetFullLogFileName(DateTime utcNow)
		{
			const string Extension = ".txt";
			const string DateTimeFormat = "_yyyy-MM-dd_HH-mm-ss";

			string fileName = nameof(WirePeep);
			switch (this.logFileNameFormat)
			{
				case LogFileNameFormat.Fixed:
					fileName += Extension;
					break;

				case LogFileNameFormat.LocalNow:
					fileName += utcNow.ToLocalTime().ToString(DateTimeFormat) + Extension;
					break;

				default: // LogFileNameFormat.UtcNow:
					fileName += utcNow.ToString(DateTimeFormat) + "Z" + Extension;
					break;
			}

			string result = Path.Combine(this.LogFolder, fileName);
			return result;
		}

		#endregion

		#region Private Methods

		private void ValidateLogFileNameFormat()
		{
			if (!Enum.IsDefined(typeof(LogFileNameFormat), this.logFileNameFormat))
			{
				this.logFileNameFormat = LogFileNameFormat.UtcNow;
			}
		}

		private void ValidateLogFolder()
		{
			static bool Check(string folder) => Directory.Exists(folder);

			static string GetAppFolder(string folder) => Path.Combine(folder, ApplicationInfo.ApplicationName);

			static string GetEnvironmentPath(string variable, string subFolder)
			{
				string variablePath = Environment.GetEnvironmentVariable(variable);
				if (!string.IsNullOrEmpty(variablePath) && !string.IsNullOrEmpty(subFolder))
				{
					variablePath = Path.Combine(variablePath, subFolder);
				}

				return variablePath;
			}

			if (!Check(this.logFolder))
			{
				List<string> candidateFolders = new List<string>();
				candidateFolders.Add(GetEnvironmentPath("OneDriveConsumer", "Documents"));
				candidateFolders.Add(GetEnvironmentPath("OneDriveCommercial", "Documents"));
				candidateFolders.Add(GetEnvironmentPath("OneDrive", "Documents"));
				candidateFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
				candidateFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

				candidateFolders = candidateFolders.Where(folder => Check(folder)).Select(folder => GetAppFolder(folder)).ToList();
				this.logFolder = candidateFolders.FirstOrDefault(folder => Check(folder));
				if (string.IsNullOrEmpty(this.logFolder))
				{
					foreach (string folder in candidateFolders)
					{
						try
						{
							Directory.CreateDirectory(folder);
							this.logFolder = folder;
							break;
						}
						catch (IOException ex)
						{
							Log.Error(this.GetType(), "I/O exception creating log folder.", ex);
						}
						catch (UnauthorizedAccessException ex)
						{
							Log.Error(this.GetType(), "Unauthorized access exception creating log folder.", ex);
						}
					}

					if (string.IsNullOrEmpty(this.logFolder))
					{
						this.logFolder = GetAppFolder(Path.GetTempPath());
						Directory.CreateDirectory(this.logFolder);
					}
				}
			}
		}

		#endregion
	}
}

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
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	public partial class OptionsDialog : ExtendedDialog
	{
		#region Constructors

		public OptionsDialog()
		{
			this.InitializeComponent();
		}

		#endregion

		#region Private Properties

		private string LogFolder => this.logFolder.Text.Trim();

		#endregion

		#region Public Methods

		public bool Execute(Window owner, Options options)
		{
			this.Owner = owner;

			this.logFileNameFormat.SelectedIndex = (int)options.LogFileNameFormat;
			this.logFolder.Text = options.LogFolder;

			bool result = this.ShowDialog() ?? false;
			if (result)
			{
				options.LogFileNameFormat = (LogFileNameFormat)this.logFileNameFormat.SelectedIndex;
				options.LogFolder = this.LogFolder;
			}

			return result;
		}

		#endregion

		#region Private Event Handlers

		private void OKClicked(object sender, RoutedEventArgs e)
		{
			List<string> errors = new List<string>();

			string folder = this.LogFolder;
			if (!Directory.Exists(folder))
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
			string folder = WindowsUtility.SelectFolder(this, "Select Log Folder", this.LogFolder);
			if (!string.IsNullOrEmpty(folder))
			{
				this.logFolder.Text = folder;
			}
		}

		#endregion
	}
}

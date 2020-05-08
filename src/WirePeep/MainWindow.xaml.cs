#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Menees;
using Menees.Shell;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	public partial class MainWindow : Window
	{
		#region Private Data Members

		private readonly WindowSaver saver;
		private Profile configuration;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();

			this.saver = new WindowSaver(this);
			this.saver.LoadSettings += this.Saver_LoadSettings;
			this.saver.SaveSettings += this.Saver_SaveSettings;
		}

		#endregion

		#region Private Event Handlers

		private void Saver_LoadSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;
			this.configuration = new Profile(settings.GetSubNode(nameof(Profile), false));
		}

		private void Saver_SaveSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;
			this.configuration.Save(settings.GetSubNode(nameof(Profile), true));
		}

		private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void ViewOptionsExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish ViewOptionsExecuted. [Bill, 5/7/2020]
			MessageBox.Show(nameof(this.ViewOptionsExecuted));
			this.saver.Save();
		}

		private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			WindowsUtility.ShellExecute(this, "http://www.wirepeep.com");
		}

		private void AddLocationExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish AddLocationExecuted. [Bill, 5/7/2020]
			MessageBox.Show(nameof(this.AddLocationExecuted));
		}

		private void ExportLogExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish ExportLogExecuted. [Bill, 5/7/2020]
			MessageBox.Show(nameof(this.ExportLogExecuted));
		}

		#endregion
	}
}

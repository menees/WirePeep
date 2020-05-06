namespace WirePeep
{
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
	using Menees.Windows.Presentation;

	#endregion

	public partial class MainWindow : Window
	{
		#region Private Data Members

		private readonly WindowSaver saver;

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

			// TODO: Load settings. [Bill, 5/6/2020]
			this.GetHashCode();
			settings.GetHashCode();
		}

		private void Saver_SaveSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;

			// TODO: Save settings. [Bill, 5/6/2020]
			this.GetHashCode();
			settings.GetHashCode();
		}

		#endregion
	}
}

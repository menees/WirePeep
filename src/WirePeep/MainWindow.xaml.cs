#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WirePeep
{
	public sealed partial class MainWindow : ExtendedWindow, IDisposable
	{
		#region Private Data Members

		private readonly WindowSaver saver;
		private readonly StatusRowCollection statusRows;
		private Options options;
		private Profile profile;
		private StateManager stateManager;
		private Timer backgroundTimer;
		private int updatingLock;
		private int closing;
		private Dictionary<string, StatusRow> rowMap;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();

			this.statusRows = (StatusRowCollection)this.Resources["StatusRows"];
			this.rowMap = new Dictionary<string, StatusRow>();

			this.saver = new WindowSaver(this);
			this.saver.LoadSettings += this.SaverLoadSettings;
			this.saver.SaveSettings += this.SaverSaveSettings;
		}

		#endregion

		#region Public Methods

		public void Dispose()
		{
			this.backgroundTimer.Dispose();
		}

		#endregion

		#region Private Methods

		private void UpdateStates(IDictionary<PeerGroupState, IReadOnlyList<LocationState>> states)
		{
			var newRowMap = new Dictionary<string, StatusRow>(this.rowMap.Count);
			foreach (var pair in states)
			{
				PeerGroupState peerGroupState = pair.Key;
				foreach (LocationState locationState in pair.Value)
				{
					string key = $"{peerGroupState.PeerGroup.Name}\0\0{locationState.Location.Name}";
					if (this.rowMap.TryGetValue(key, out StatusRow row))
					{
						row.Update(peerGroupState, locationState);
					}
					else
					{
						row = new StatusRow();
						row.Update(peerGroupState, locationState);
						this.statusRows.Add(row);
					}

					newRowMap.Add(key, row);
				}
			}

			foreach (var pair in this.rowMap.Where(pair => !newRowMap.ContainsKey(pair.Key)))
			{
				this.statusRows.Remove(pair.Value);
			}

			this.rowMap = newRowMap;
		}

		#endregion

		#region Private Event Handlers

		private void SaverLoadSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;
			this.options = new Options(settings.GetSubNode(nameof(Options), false));
			this.profile = new Profile(settings.GetSubNode(nameof(Profile), false));

			this.stateManager = new StateManager(this.profile);
			this.backgroundTimer = new Timer(this.BackgroundTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		private void SaverSaveSettings(object sender, SettingsEventArgs e)
		{
			Interlocked.Increment(ref this.closing);
			this.backgroundTimer.Dispose();

			var settings = e.SettingsNode;
			this.profile?.Save(settings.GetSubNode(nameof(Profile), true));
			this.options?.Save(settings.GetSubNode(nameof(Options), true));
		}

		private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void ViewOptionsExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			OptionsDialog dialog = new OptionsDialog();
			if (dialog.Execute(this, this.options))
			{
				// TODO: Finish ViewOptionsExecuted. [Bill, 5/7/2020]
				this.saver.Save();
			}
		}

		private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			WindowsUtility.ShellExecute(this, "http://www.wirepeep.com");
		}

		private void AddLocationExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish AddLocationExecuted. [Bill, 5/7/2020]
			MessageBox.Show(nameof(this.AddLocationExecuted));
			this.GetHashCode();
		}

		private void ExportLogExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish ExportLogExecuted. [Bill, 5/7/2020]
			MessageBox.Show(nameof(this.ExportLogExecuted));
			this.GetHashCode();
		}

		private void AboutExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			WindowsUtility.ShowAboutBox(this, typeof(MainWindow).Assembly);
		}

		private void BackgroundTimerCallback(object state)
		{
			// Only let one Update run at a time. If the callback takes longer than 1 second, it will be invoked again from another thread.
			if (this.closing == 0 && Interlocked.CompareExchange(ref this.updatingLock, 1, 0) == 0)
			{
				try
				{
					Dictionary<PeerGroupState, IReadOnlyList<LocationState>> states = this.stateManager.Update();
					this.Dispatcher.BeginInvoke(new Action(() => this.UpdateStates(states)));
				}
				finally
				{
					Interlocked.Exchange(ref this.updatingLock, 0);
				}
			}
		}

		#endregion
	}
}

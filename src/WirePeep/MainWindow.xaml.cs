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
		private readonly LogRowCollection logRows;
		private readonly Dictionary<string, LogRow> failedPeerGroupToLogRowMap;

		private Options options;
		private Profile profile;
		private StateManager stateManager;
		private Timer backgroundTimer;
		private int updatingLock;
		private bool closing;
		private bool simulateFailure = Convert.ToBoolean(0);
		private Dictionary<string, StatusRow> statusRowMap;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();

			this.statusRows = (StatusRowCollection)this.Resources["StatusRows"];
			this.statusRowMap = new Dictionary<string, StatusRow>(StringComparer.CurrentCultureIgnoreCase);

			this.logRows = (LogRowCollection)this.Resources["LogRows"];
			this.failedPeerGroupToLogRowMap = new Dictionary<string, LogRow>(this.statusRowMap.Comparer);

			this.saver = new WindowSaver(this);
			this.saver.LoadSettings += this.SaverLoadSettings;
			this.saver.SaveSettings += this.SaverSaveSettings;
		}

		#endregion

		#region Private Properties

		private RowDefinition[] SplitterTargetRows
		{
			get
			{
				int splitterRow = Grid.GetRow(this.splitter);
				var result = new[]
				{
					this.windowLayoutGrid.RowDefinitions[splitterRow - 1],
					this.windowLayoutGrid.RowDefinitions[splitterRow + 1],
				};

				return result;
			}
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
			this.UpdateStatusRows(states);
			this.UpdateLogRows(states.Keys);

			// This isn't a dependency property, so we can't bind to it. We have to manually update it.
			TimeSpan monitored = this.stateManager.Monitored;
			monitored = ConvertUtility.TruncateToSeconds(monitored);
			this.monitoredTime.Text = monitored.ToString();

			// Optionally, simulate a failure when ScrollLock is toggled on.
			this.simulateFailure = this.options.ScrollLockSimulatesFailure && Keyboard.IsKeyToggled(Key.Scroll);
		}

		private void UpdateStatusRows(IDictionary<PeerGroupState, IReadOnlyList<LocationState>> states)
		{
			var newRowMap = new Dictionary<string, StatusRow>(this.statusRowMap.Count, this.statusRowMap.Comparer);
			foreach (var pair in states)
			{
				PeerGroupState peerGroupState = pair.Key;
				foreach (LocationState locationState in pair.Value)
				{
					string key = $"{peerGroupState.PeerGroup.Name}\0\0{locationState.Location.Name}";
					if (this.statusRowMap.TryGetValue(key, out StatusRow row))
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

			foreach (var pair in this.statusRowMap.Where(pair => !newRowMap.ContainsKey(pair.Key)))
			{
				this.statusRows.Remove(pair.Value);
			}

			this.statusRowMap = newRowMap;
		}

		private void UpdateLogRows(ICollection<PeerGroupState> peerGroupStates)
		{
			HashSet<string> currentPeerGroups = new HashSet<string>(this.failedPeerGroupToLogRowMap.Comparer);
			foreach (PeerGroupState peerGroupState in peerGroupStates)
			{
				PeerGroup peerGroup = peerGroupState.PeerGroup;
				string peerGroupName = peerGroup.Name;

				if (this.failedPeerGroupToLogRowMap.TryGetValue(peerGroupName, out LogRow row))
				{
					row.Update(peerGroupState);
					if (!peerGroupState.IsFailed)
					{
						this.failedPeerGroupToLogRowMap.Remove(peerGroupName);
					}
				}
				else if (peerGroupState.IsFailed)
				{
					LogRow previous = this.logRows.FirstOrDefault(r => r.PeerGroupName == peerGroupName);
					row = new LogRow();
					row.Update(peerGroupState, previous);
					this.logRows.Insert(0, row);
					this.failedPeerGroupToLogRowMap.Add(peerGroupName, row);
				}

				currentPeerGroups.Add(peerGroupName);
			}

			foreach (string peerGroupName in this.failedPeerGroupToLogRowMap.Keys.Where(key => !currentPeerGroups.Contains(key)).ToArray())
			{
				this.failedPeerGroupToLogRowMap.Remove(peerGroupName);
			}
		}

		#endregion

		#region Private Event Handlers

		private void SaverLoadSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;
			this.options = new Options(settings.GetSubNode(nameof(Options), false));
			this.profile = new Profile(settings.GetSubNode(nameof(Profile), false));

			ISettingsNode splitterNode = settings.GetSubNode(nameof(GridSplitter), false);
			if (splitterNode != null)
			{
				RowDefinition[] splitterTargetRows = this.SplitterTargetRows;
				for (int i = 0; i < splitterTargetRows.Length; i++)
				{
					ISettingsNode rowNode = splitterNode.GetSubNode($"Row{i}", false);
					if (rowNode != null)
					{
						double value = rowNode.GetValue(nameof(GridLength.Value), 1.0);
						GridUnitType unitType = rowNode.GetValue(nameof(GridLength.GridUnitType), GridUnitType.Star);
						RowDefinition row = splitterTargetRows[i];
						row.Height = new GridLength(value, unitType);
					}
				}
			}

			this.stateManager = new StateManager(this.profile);
			this.backgroundTimer = new Timer(this.BackgroundTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		private void SaverSaveSettings(object sender, SettingsEventArgs e)
		{
			this.closing = true;
			this.backgroundTimer.Dispose();

			var settings = e.SettingsNode;
			this.profile?.Save(settings.GetSubNode(nameof(Profile), true));
			this.options?.Save(settings.GetSubNode(nameof(Options), true));

			settings.DeleteSubNode(nameof(GridSplitter));
			ISettingsNode splitterNode = settings.GetSubNode(nameof(GridSplitter), true);
			RowDefinition[] splitterTargetRows = this.SplitterTargetRows;
			for (int i = 0; i < splitterTargetRows.Length; i++)
			{
				RowDefinition row = splitterTargetRows[i];
				GridLength rowHeight = row.Height;
				ISettingsNode rowNode = splitterNode.GetSubNode($"Row{i}", true);
				rowNode.SetValue(nameof(rowHeight.Value), rowHeight.Value);
				rowNode.SetValue(nameof(rowHeight.GridUnitType), rowHeight.GridUnitType);
			}
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
				// TODO: React to any Options that changed (e.g., LogFolder). [Bill, 5/7/2020]
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
			if (!this.closing && Interlocked.CompareExchange(ref this.updatingLock, 1, 0) == 0)
			{
				try
				{
					Dictionary<PeerGroupState, IReadOnlyList<LocationState>> states = this.stateManager.Update(this.simulateFailure);
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

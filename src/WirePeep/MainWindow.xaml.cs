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
using Microsoft.Win32;

#endregion

namespace WirePeep
{
	public sealed partial class MainWindow : ExtendedWindow, IDisposable
	{
		#region Private Data Members

		private readonly WindowSaver saver;
		private readonly StatusRowCollection statusRows;
		private readonly Dictionary<Guid, StatusRow> statusRowMap;
		private readonly LogRowCollection logRows;
		private readonly Dictionary<Guid, LogRow> failedPeerGroupToLogRowMap;

		private Options options;
		private Profile profile;
		private StateManager stateManager;
		private Timer backgroundTimer;
		private int updatingLock;
		private bool closing;
		private bool simulateFailure;
		private DataGrid selectedGrid;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();

			this.statusRows = (StatusRowCollection)this.Resources["StatusRows"];
			this.statusRowMap = new Dictionary<Guid, StatusRow>();

			this.logRows = (LogRowCollection)this.Resources["LogRows"];
			this.failedPeerGroupToLogRowMap = new Dictionary<Guid, LogRow>(this.statusRowMap.Comparer);

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

		private DataGrid SelectedGrid => this.selectedGrid ?? this.statusGrid;

		private LogRow SelectedLogRow => (LogRow)this.logGrid.SelectedItem;

		private StatusRow SelectedStatusRow => (StatusRow)this.statusGrid.SelectedItem;

		#endregion

		#region Public Methods

		public void Dispose()
		{
			this.backgroundTimer.Dispose();
		}

		#endregion

		#region Private Methods

		private static void CopyToClipboard(DataGrid source, bool copyRow)
		{
			IList<DataGridCellInfo> copyCells = copyRow ? source.SelectedCells : new[] { source.CurrentCell };
			StringBuilder sb = new StringBuilder();
			foreach (DataGridCellInfo cell in copyCells)
			{
				if (sb.Length > 0)
				{
					sb.Append('\t');
				}

				// For non-text columns (e.g., the "Poll" template column), there's no consistent way to get the
				// element's content as text. So we'll just pretend we don't see those columns.
				FrameworkElement element = cell.Column.GetCellContent(cell.Item);
				if (element is TextBlock textBlock)
				{
					sb.Append(textBlock.Text);
				}
			}

			Clipboard.SetText(sb.ToString());
		}

		private void UpdateStates(StateSnapshot states)
		{
			this.UpdateStatusRows(states);
			this.UpdateLogRows(states.AllPeerGroups);

			// This isn't a dependency property, so we can't bind to it. We have to manually update it.
			TimeSpan monitored = this.stateManager.Monitored;
			monitored = ConvertUtility.RoundToSeconds(monitored);
			this.monitoredTime.Text = monitored.ToString();

			// Optionally, simulate a failure when ScrollLock is toggled on.
			this.simulateFailure = this.options.ScrollLockSimulatesFailure && Keyboard.IsKeyToggled(Key.Scroll);
		}

		private void UpdateStatusRows(StateSnapshot states)
		{
			HashSet<Guid> currentLocations = new HashSet<Guid>(this.statusRowMap.Comparer);
			foreach (var pair in states.AllPeerGroupLocations)
			{
				PeerGroupState peerGroupState = pair.Key;
				foreach (LocationState locationState in pair.Value)
				{
					Guid locationId = locationState.Location.Id;
					currentLocations.Add(locationId);

					if (this.statusRowMap.TryGetValue(locationId, out StatusRow row))
					{
						row.Update(peerGroupState, locationState);
					}
					else
					{
						row = new StatusRow();
						row.Update(peerGroupState, locationState);
						this.statusRows.Add(row);
						this.statusRowMap.Add(locationId, row);
					}
				}
			}

			foreach (var pair in this.statusRowMap.Where(pair => !currentLocations.Contains(pair.Key)).ToArray())
			{
				this.statusRows.Remove(pair.Value);
				this.statusRowMap.Remove(pair.Key);
			}
		}

		private void UpdateLogRows(IEnumerable<PeerGroupState> peerGroupStates)
		{
			HashSet<Guid> currentPeerGroups = new HashSet<Guid>(this.failedPeerGroupToLogRowMap.Comparer);
			foreach (PeerGroupState peerGroupState in peerGroupStates)
			{
				PeerGroup peerGroup = peerGroupState.PeerGroup;
				Guid peerGroupId = peerGroup.Id;

				if (this.failedPeerGroupToLogRowMap.TryGetValue(peerGroupId, out LogRow row))
				{
					row.Update(peerGroupState);
					if (!peerGroupState.IsFailed)
					{
						this.failedPeerGroupToLogRowMap.Remove(peerGroupId);
					}
				}
				else if (peerGroupState.IsFailed)
				{
					LogRow previous = this.logRows.FirstOrDefault(r => r.PeerGroupId == peerGroupId);
					row = new LogRow();
					row.Update(peerGroupState, previous);
					this.logRows.Insert(0, row);
					this.failedPeerGroupToLogRowMap.Add(peerGroupId, row);
				}

				currentPeerGroups.Add(peerGroupId);
			}

			foreach (Guid peerGroupId in this.failedPeerGroupToLogRowMap.Keys.Where(key => !currentPeerGroups.Contains(key)).ToArray())
			{
				this.failedPeerGroupToLogRowMap.Remove(peerGroupId);
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

		private void ExportLogCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = this.logRows.Count > 0;

		private void ExportLogExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.logRows.Count > 0)
			{
				SaveFileDialog dialog = new SaveFileDialog
				{
					Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
					DefaultExt = ".csv",
					Title = (e.Command as RoutedUICommand)?.Text,
				};

				if (dialog.ShowDialog(this) ?? false)
				{
					// TODO: Finish ExportLogExecuted. [Bill, 5/7/2020]
					MessageBox.Show(nameof(this.ExportLogExecuted));
					this.GetHashCode();
				}
			}
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
					StateSnapshot states = this.stateManager.Update(this.simulateFailure);
					this.Dispatcher.BeginInvoke(new Action(() => this.UpdateStates(states)));
				}
				finally
				{
					Interlocked.Exchange(ref this.updatingLock, 0);
				}
			}
		}

		private void EditItemCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = this.SelectedGrid?.SelectedItem != null;

		private void EditItemExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Finish EditItemExecuted. [Bill, 5/17/2020]
			if (this.SelectedGrid == this.logGrid)
			{
				MessageBox.Show(nameof(this.EditItemExecuted) + " for " + this.SelectedLogRow.PeerGroupName);
			}
			else
			{
				MessageBox.Show(nameof(this.EditItemExecuted) + " for " + this.SelectedStatusRow.LocationName);
			}
		}

		private void DeleteItemCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = this.SelectedStatusRow != null;

		private void DeleteItemExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			StatusRow statusRow = this.SelectedStatusRow;
			if (statusRow != null)
			{
				Location location = this.profile.Locations.FirstOrDefault(l => l.Id == statusRow.LocationId);
				if (location != null)
				{
					bool deletePeerGroup = this.profile.Locations.Count(l => l.PeerGroup == location.PeerGroup) == 1;

					StringBuilder sb = new StringBuilder("Are you sure you want to delete location \"");
					sb.Append(statusRow.LocationName).Append('"');
					if (deletePeerGroup)
					{
						sb.Append(" (and peer group \"").Append(location.PeerGroup.Name).Append("\")");
					}

					sb.Append('?');

					string message = sb.ToString();
					string caption = ApplicationInfo.ApplicationName;
					if (MessageBox.Show(this, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.profile.Locations.Remove(location);

						// Note: Don't remove from this.statusRowMap here. Let the next Update cycle clean it up.
						this.statusRows.Remove(statusRow);

						if (deletePeerGroup)
						{
							this.profile.PeerGroups.Remove(location.PeerGroup);

							// Note: Don't remove from this.failedPeerGroupToLogRowMap. Let the next Update cycle clean it up.
							LogRow[] removeLogRows = this.logRows.Where(row => row.PeerGroupId == location.PeerGroup.Id).ToArray();
							foreach (LogRow row in removeLogRows)
							{
								this.logRows.Remove(row);
							}
						}
					}
				}
			}
		}

		private void CopyCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = this.SelectedGrid?.SelectedItem != null;

		private void CopyValueExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CopyToClipboard(this.SelectedGrid, false);
		}

		private void CopyRowExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CopyToClipboard(this.SelectedGrid, true);
		}

		private void LogGridSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			object current = e.AddedCells.Select(cell => cell.Item).FirstOrDefault();
			object previous = e.RemovedCells.Select(cell => cell.Item).FirstOrDefault();

			if (!ReferenceEquals(current, previous))
			{
				LogRow selectedLogRow = this.SelectedLogRow;
				foreach (LogRow logRow in this.logRows)
				{
					logRow.IsSelected = ReferenceEquals(logRow, selectedLogRow);
				}
			}
		}

		private void LogGridGotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedGrid = this.logGrid;
		}

		private void StatusGridGotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedGrid = this.statusGrid;
		}

		private void LogGridContextMenuOpening(object sender, ContextMenuEventArgs e) => this.LogGridGotFocus(sender, e);

		private void StatusGridContextMenuOpening(object sender, ContextMenuEventArgs e) => this.StatusGridGotFocus(sender, e);

		#endregion
	}
}

﻿#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
using System.Windows.Shell;
using Menees;
using Menees.Windows.Presentation;
using Microsoft.Win32;
using W = System.Windows.Forms;

#endregion

namespace WirePeep
{
	public sealed partial class MainWindow : ExtendedWindow, IDisposable
	{
		#region Private Data Members

		private readonly WindowSaver windowSaver;
		private readonly StatusRowCollection statusRows;
		private readonly Dictionary<Guid, StatusRow> statusRowMap;
		private readonly LogRowCollection logRows;
		private readonly Dictionary<Guid, LogRow> failedPeerGroupToLogRowMap;
		private readonly W.NotifyIcon notifyIcon;

		private AppOptions appOptions;
		private Profile profile;
		private StateManager stateManager;
		private Timer backgroundTimer;
		private int updatingLock;
		private bool closing;
		private bool simulateFailure;
		private DataGrid selectedGrid;
		private Logger logger;
		private int failureId;
		private MediaPlayer mediaPlayer;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();

			this.statusRows = (StatusRowCollection)this.Resources["StatusRows"];
			this.statusRowMap = new Dictionary<Guid, StatusRow>();

			this.logRows = (LogRowCollection)this.Resources["LogRows"];
			this.failedPeerGroupToLogRowMap = new Dictionary<Guid, LogRow>(this.statusRowMap.Comparer);

			this.windowSaver = new WindowSaver(this) { AutoLoad = false };
			this.windowSaver.LoadSettings += this.WindowSaverLoadSettings;
			this.windowSaver.SaveSettings += this.WindowSaverSaveSettings;

			this.notifyIcon = this.CreateNotifyIcon();
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

		private CommonOptions CommonOptions => this.appOptions?.CommonOptions;

		#endregion

		#region Public Methods

		public void Dispose()
		{
			this.backgroundTimer?.Dispose();
			this.notifyIcon?.Dispose();
			this.notifyIcon.ContextMenuStrip?.Dispose();
		}

		#endregion

		#region Internal Methods

		internal AppOptions LoadNonWindowSettings()
		{
			using (ISettingsStore store = ApplicationInfo.CreateUserSettingsStore())
			{
				ISettingsNode settings = store.RootNode;
				this.appOptions = new AppOptions(settings.GetSubNode(nameof(AppOptions), false));
				this.profile = new Profile(settings.GetSubNode(nameof(Profile), false));
				this.stateManager = new StateManager(this.profile);

				string logFileName = this.GenerateLogFileName();
				this.OpenLogger(logFileName);
				this.appOptions.Apply(this);

				this.backgroundTimer = new Timer(this.BackgroundTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
				return this.appOptions;
			}
		}

		internal void SaveNonWindowSettings()
		{
			using (ISettingsStore store = ApplicationInfo.CreateUserSettingsStore())
			{
				ISettingsNode settings = store.RootNode;
				this.profile?.Save(settings.GetSubNode(nameof(Profile), true));
				this.appOptions?.Save(settings.GetSubNode(nameof(AppOptions), true));
				store.Save();
			}
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
			this.simulateFailure = this.CommonOptions.ScrollLockSimulatesFailure && Keyboard.IsKeyToggled(Key.Scroll);
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
			List<PeerGroupState> failedChanged = new List<PeerGroupState>(0);

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
						this.logger?.AddFailureEnd(row.PeerGroupName, row.FailEnded.Value, row.FailureId, ConvertUtility.RoundToSeconds(row.Length));
						failedChanged.Add(peerGroupState);
					}
				}
				else if (peerGroupState.IsFailed)
				{
					LogRow previous = this.logRows.FirstOrDefault(r => r.PeerGroupId == peerGroupId);
					row = new LogRow(this.failureId++);
					row.Update(peerGroupState, previous);
					this.logRows.Insert(0, row);
					this.failedPeerGroupToLogRowMap.Add(peerGroupId, row);
					TimeSpan? sincePrevious = row.SincePrevious != null ? ConvertUtility.RoundToSeconds(row.SincePrevious.Value) : (TimeSpan?)null;
					this.logger?.AddFailureStart(row.PeerGroupName, row.FailStarted, row.FailureId, sincePrevious);
					failedChanged.Add(peerGroupState);
				}

				currentPeerGroups.Add(peerGroupId);
			}

			foreach (Guid peerGroupId in this.failedPeerGroupToLogRowMap.Keys.Where(key => !currentPeerGroups.Contains(key)).ToArray())
			{
				this.failedPeerGroupToLogRowMap.Remove(peerGroupId);
			}

			foreach (var group in failedChanged.GroupBy(g => g.IsFailed))
			{
				AlertOptions alertOptions = group.Key ? this.appOptions.FailureOptions : this.appOptions.ReconnectOptions;

				StringBuilder sb = new StringBuilder("The following peer groups ");
				sb.Append(group.Key ? "are no longer connected:" : "have reconnected:");
				sb.AppendLine();
				foreach (PeerGroupState peerGroupState in group)
				{
					sb.AppendLine(peerGroupState.PeerGroup.Name);
				}

				alertOptions.Alert(this, sb.ToString(), this.notifyIcon, ref this.mediaPlayer);
			}
		}

		private void EditComment(LogRow logRow)
		{
			string comment = logRow.Comment;
			StringBuilder sb = new StringBuilder();
			sb.Append(string.IsNullOrEmpty(comment) ? "Add a" : "Edit the");
			sb.Append(" comment for the \"").Append(logRow.PeerGroupName).Append("\" failure that started at ");
			sb.Append(logRow.FailStarted).Append(':');

			comment = WindowsUtility.ShowInputBox(this, sb.ToString(), null, comment);
			if (comment != null)
			{
				using (new WaitCursor())
				{
					logRow.Comment = comment;
					this.logger?.AddFailureComment(logRow.PeerGroupName, this.stateManager.LastUpdated, logRow.FailureId, comment);
				}
			}
		}

		private void EditLocation(StatusRow statusRow)
		{
			ObservableCollection<PeerGroup> peerGroups = this.profile.PeerGroups;
			if (peerGroups.Count > 0 || (this.EditPeerGroups() && peerGroups.Count > 0))
			{
				ObservableCollection<Location> locations = this.profile.Locations;

				bool insert = statusRow == null;
				Location location = insert ? null : locations.FirstOrDefault(l => l.Id == statusRow.LocationId);
				int locationIndex = location == null ? -1 : locations.IndexOf(location);

				LocationDialog dialog = new LocationDialog();
				if (dialog.Execute(this, peerGroups, ref location))
				{
					if (location != null && peerGroups.Contains(location.PeerGroup))
					{
						if (locationIndex >= 0 && locationIndex < locations.Count)
						{
							locations[locationIndex] = location;
						}
						else
						{
							locations.Add(location);
						}
					}
				}
			}
		}

		private bool EditPeerGroups()
		{
			PeerGroupDialog dialog = new PeerGroupDialog();
			bool result = dialog.Execute(this, this.profile);
			return result;
		}

		private string GenerateLogFileName()
		{
			DateTime started = this.stateManager.Started;
			string result = this.CommonOptions.GetFullLogFileName(started);
			return result;
		}

		private void OpenLogger(string logFileName)
		{
			this.logger = new Logger(logFileName, false);
			this.logger.AddLogStart(this.stateManager.Started);
		}

		private void UpdateLogger()
		{
			string logFileName = this.GenerateLogFileName();
			if (logFileName != this.logger?.FileName)
			{
				this.CloseLogger();
				this.OpenLogger(logFileName);
			}
		}

		private void CloseLogger()
		{
			if (this.logger != null)
			{
				DateTime utcNow = this.stateManager.LastUpdated;
				TimeSpan monitored = this.stateManager.Monitored;

				using (this.logger.BeginBatch())
				{
					foreach (var group in this.logRows.GroupBy(row => row.PeerGroupId))
					{
						int numFails = group.Count();
						TimeSpan totalFailLength = TimeSpan.FromTicks(group.Sum(row => row.Length.Ticks));
						decimal percentFailTime = Math.Round((decimal)(100 * totalFailLength.TotalSeconds / monitored.TotalSeconds), 2);
						this.logger.AddLogSummary(
							group.First().PeerGroupName,
							utcNow,
							numFails,
							ConvertUtility.RoundToSeconds(totalFailLength),
							percentFailTime,
							ConvertUtility.RoundToSeconds(group.Min(row => row.Length)),
							ConvertUtility.RoundToSeconds(group.Max(row => row.Length)),
							ConvertUtility.RoundToSeconds(TimeSpan.FromMilliseconds(group.Average(row => row.Length.TotalMilliseconds))));
					}

					this.logger.AddLogEnd(utcNow, ConvertUtility.RoundToSeconds(monitored));
				}

				this.logger = null;
			}
		}

		private W.NotifyIcon CreateNotifyIcon()
		{
			W.NotifyIcon notifyIcon = new W.NotifyIcon();

			W.ContextMenuStrip notifyIconMenu = new W.ContextMenuStrip();
#pragma warning disable CC0022 // Should dispose object. The menu items are disposed by the ContextMenuStrip.
			W.ToolStripMenuItem notifyIconViewMenu = new W.ToolStripMenuItem();
			W.ToolStripSeparator notifyIconSeparator = new W.ToolStripSeparator();
			W.ToolStripMenuItem notifyIconExitMenu = new W.ToolStripMenuItem();
#pragma warning restore CC0022 // Should dispose object
			notifyIcon.ContextMenuStrip = notifyIconMenu;

			notifyIcon.Icon = Properties.Resources.WirePeep;
			notifyIcon.Text = ApplicationInfo.ApplicationName;
			notifyIcon.Visible = true;
			notifyIcon.MouseDoubleClick += this.NotifyIconMouseDoubleClick;

			notifyIconMenu.Items.AddRange(new W.ToolStripItem[]
			{
				notifyIconViewMenu,
				notifyIconSeparator,
				notifyIconExitMenu,
			});

			// notifyIconMenu.ShowImageMargin = false;
			notifyIconViewMenu.Font = new Font(notifyIconMenu.Font, System.Drawing.FontStyle.Bold);
			notifyIconViewMenu.Text = "&View";
			notifyIconViewMenu.Click += this.NotifyIconViewMenuClick;

			notifyIconExitMenu.Text = "&Exit";
			notifyIconExitMenu.Click += this.NotifyIconExitMenuClick;

			// The notify icon has to be visible for us to send tooltip notification messages through it.
			notifyIcon.Visible = true;

			return notifyIcon;
		}

		#endregion

		#region Private Event Handlers

		private void WindowSaverLoadSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;

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
		}

		private void WindowSaverSaveSettings(object sender, SettingsEventArgs e)
		{
			var settings = e.SettingsNode;

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

		private void WindowClosing(object sender, CancelEventArgs e)
		{
			this.closing = true;
			this.backgroundTimer.Dispose();
			this.CloseLogger();
		}

		private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void ViewOptionsExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			OptionsDialog dialog = new OptionsDialog();
			if (dialog.Execute(this, this.appOptions))
			{
				this.windowSaver.Save();
				this.UpdateLogger();
				this.appOptions.Apply(this);
			}
		}

		private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			WindowsUtility.ShellExecute(this, "http://www.wirepeep.com");
		}

		private void AddLocationExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this.EditLocation(null);
		}

		private void EditPeerGroupsExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this.EditPeerGroups();
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
					Logger logger = new Logger(dialog.FileName, true);

					using (new WaitCursor())
					using (logger.BeginBatch())
					{
						foreach (LogRow logRow in this.logRows.Reverse())
						{
							logger.AddSimpleEntry(logRow.PeerGroupName, logRow.FailStarted, logRow.FailEnded, logRow.SincePrevious, logRow.Comment);
						}
					}
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
			if (this.SelectedGrid == this.logGrid)
			{
				this.EditComment(this.SelectedLogRow);
			}
			else
			{
				this.EditLocation(this.SelectedStatusRow);
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
					if (WindowsUtility.ShowQuestion(this, message, caption))
					{
						using (new WaitCursor())
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

		private void LogGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			LogRow logRow = this.SelectedLogRow;
			if (logRow != null)
			{
				this.EditComment(logRow);
			}
		}

		private void StatusGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			StatusRow statusRow = this.SelectedStatusRow;
			if (statusRow != null)
			{
				this.EditLocation(statusRow);
			}
		}

		private void WindowActivated(object sender, EventArgs e)
		{
			TaskbarItemInfo taskbarItem = this.TaskbarItemInfo;
			if (taskbarItem != null)
			{
				taskbarItem.ProgressState = TaskbarItemProgressState.None;
				taskbarItem.ProgressValue = 0;
			}
		}

		private void WindowStateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == WindowState.Minimized)
			{
				if (this.appOptions?.MinimizeToTray ?? false)
				{
					this.ShowInTaskbar = false;
				}
			}
			else
			{
				this.ShowInTaskbar = true;
			}
		}

		private void WindowClosed(object sender, EventArgs e)
		{
			this.Dispose();
		}

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			// This will reload the previous window placement but with a normal or maximized state (never minimized).
			this.windowSaver.Load();

			// Setting this WindowState after Load will briefly show the window on the screen (due to Load's call to SetWindowPlacement).
			// However, then it will minimize to the correct monitor's taskbar, and the taskbar's thumbnail will be a correct image.
			// If we did the following code before calling this.windowSaver.Load() above:
			//     if (this.appOptions.StartMinimized) this.windowSaver.LoadStateOverride = WindowState.Minimized;
			// Then we wouldn't see the flicker, but we'd always end up minimized on the primary taskbar and without a thumbnail image.
			// Accepting the brief flicker seems like the best alternative.
			if (this.appOptions.StartMinimized && !this.appOptions.MinimizeToTray)
			{
				this.WindowState = WindowState.Minimized;
			}
		}

		private void NotifyIconViewMenuClick(object sender, EventArgs e)
		{
			if (!this.ShowInTaskbar)
			{
				this.Show();
			}

			WindowsUtility.BringToFront(this);
		}

		private void NotifyIconExitMenuClick(object sender, EventArgs e)
		{
			Commands.Exit.Execute(null, this);
		}

		private void NotifyIconMouseDoubleClick(object sender, W.MouseEventArgs e)
		{
			if (e.Button == W.MouseButtons.Left)
			{
				this.NotifyIconViewMenuClick(sender, e);
			}
		}

		#endregion
	}
}

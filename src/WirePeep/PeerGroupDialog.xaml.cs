#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

#endregion

namespace WirePeep
{
	public partial class PeerGroupDialog : ExtendedDialog
	{
		#region Private Data Members

		private Profile profile;

		#endregion

		#region Constructors

		public PeerGroupDialog()
		{
			this.InitializeComponent();
		}

		#endregion

		#region Public Methods

		public bool Execute(Window owner, Profile profile)
		{
			this.Owner = owner;
			this.profile = profile;

			ObservableCollection<PeerGroup> peerGroups = profile.PeerGroups;
			List<GridRow> rows = peerGroups.OrderBy(g => g.Name).Select(g => new GridRow(g)).ToList();
			this.grid.ItemsSource = rows;

			bool result = false;
			if (this.ShowDialog() ?? false)
			{
				HashSet<PeerGroup> usedGroups = new HashSet<PeerGroup>();
				foreach (GridRow row in rows)
				{
					PeerGroup rowGroup = row.CreateGroup();
					PeerGroup existingGroup = peerGroups.FirstOrDefault(g => g.Id == row.Id);
					if (existingGroup == null)
					{
						peerGroups.Add(rowGroup);
						usedGroups.Add(rowGroup);
					}
					else if (existingGroup.Name != rowGroup.Name
						|| existingGroup.Poll != rowGroup.Poll
						|| existingGroup.Wait != rowGroup.Wait
						|| existingGroup.Fail != rowGroup.Fail)
					{
						int index = peerGroups.IndexOf(existingGroup);
						peerGroups[index] = rowGroup;
						usedGroups.Add(rowGroup);
					}
				}

				foreach (PeerGroup group in peerGroups.Where(g => !usedGroups.Contains(g)).ToArray())
				{
					peerGroups.Remove(group);
				}

				result = true;
			}

			return result;
		}

		#endregion

		#region Private Methods

		private void GridPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			// Idea came from https://stackoverflow.com/a/33187654/1882616.
			if (e.Command == DataGrid.DeleteCommand)
			{
				// The new/insert row is an internal "NamedObject" object not a GridRow.
				if (this.grid.SelectedItem is GridRow row)
				{
					string[] usingLocations = this.profile.Locations.Where(l => l.PeerGroup.Id == row.Id).Select(l => l.Name).ToArray();
					if (usingLocations.Length > 0)
					{
						string suffix = usingLocations.Length == 1 ? string.Empty : "s";
						string locations = string.Join("\n", usingLocations);
						WindowsUtility.ShowInfo(
							this,
							$"Peer group \"{row.Name}\" can't be deleted because it is still in use by the following location{suffix}:\n\n{locations}");
						e.Handled = true;
					}
				}
			}
		}

		private void GridInitializingNewItem(object sender, InitializingNewItemEventArgs e)
		{
			if (e.NewItem is GridRow row && string.IsNullOrEmpty(row.Name))
			{
				// We don't have to add 1 because the count already includes this row.
				row.Name = $"Peer group {this.grid.ItemsSource.Cast<GridRow>().Count()}";
			}
		}

		private void OKClicked(object sender, RoutedEventArgs e)
		{
			// DialogResult = non-zero count, all names non-whitespace, all numbers positive and <= upper bound.
			// TODO: Finish OKClicked. [Bill, 5/21/2020]
		}

		#endregion

		#region Private Types

		private sealed class GridRow
		{
			#region Private Data Members

			private const ushort DefaultPoll = 5;
			private const ushort DefaultWait = 200;
			private const ushort DefaultFail = 10;

			private const ushort MaxPoll = 60 * 60;
			private const ushort MaxWait = 10 * DefaultWait;
			private const ushort MaxFail = 10 * MaxPoll;

			private string name;
			private ushort poll;
			private ushort wait;
			private ushort fail;

			#endregion

			#region Constructors

			public GridRow()
			{
				// This is used by the DataGrid via reflection.
				this.poll = DefaultPoll;
				this.wait = DefaultWait;
				this.fail = DefaultFail;
				this.Id = Guid.NewGuid();
			}

			public GridRow(PeerGroup group)
			{
				this.name = group.Name;
				this.poll = (ushort)group.Poll.TotalSeconds;
				this.wait = (ushort)group.Wait.TotalMilliseconds;
				this.fail = (ushort)group.Fail.TotalSeconds;
				this.Id = group.Id;
			}

			#endregion

			#region Public Properties

			public string Name
			{
				get => this.name;

				set
				{
					string scrubbed = value?.Trim();
					if (string.IsNullOrEmpty(scrubbed))
					{
						// TODO: Why doesn't this put a red box around the cell? [Bill, 5/22/2020]
						throw new FormatException("Please enter a peer group name.");
					}

					this.name = scrubbed;
				}
			}

			public ushort Poll
			{
				get => this.poll;
				set => this.poll = Clamp(value, 1, MaxPoll);
			}

			public ushort Wait
			{
				get => this.wait;
				set => this.wait = Clamp(value, 1, MaxWait);
			}

			public ushort Fail
			{
				get => this.fail;
				set => this.fail = Clamp(value, 1, MaxFail);
			}

			#endregion

			#region Internal Properties

			internal Guid Id { get; }

			#endregion

			#region Public Methods

			public PeerGroup CreateGroup()
			{
				return new PeerGroup(
					this.Name,
					TimeSpan.FromSeconds(this.Poll),
					TimeSpan.FromMilliseconds(this.Wait),
					TimeSpan.FromSeconds(this.Fail),
					this.Id);
			}

			#endregion

			#region Private Methods

			private static ushort Clamp(ushort value, ushort min, ushort max)
				=> value < min ? min : (value > max ? max : value);

			#endregion
		}

		#endregion
	}
}

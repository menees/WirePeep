#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class Profile
	{
		#region Constructors

		public Profile(ISettingsNode settingsNode)
		{
			if (settingsNode == null)
			{
				this.LoadDefaults();
			}
			else
			{
				this.Load(settingsNode);
			}
		}

		#endregion

		#region Public Properties

		public ObservableCollection<PeerGroup> PeerGroups { get; } = new ObservableCollection<PeerGroup>();

		public ObservableCollection<Location> Locations { get; } = new ObservableCollection<Location>();

		#endregion

		#region Public Methods

		public void Save(ISettingsNode settingsNode)
		{
			// TODO: Save configuration. [Bill, 5/7/2020]
		}

		#endregion

		#region Private Methods

		private void LoadDefaults()
		{
			// TODO: Load default options. [Bill, 5/7/2020]
		}

		private void Load(ISettingsNode settingsNode)
		{
			// TODO: Load configuration. [Bill, 5/7/2020]
		}

		#endregion
	}
}

#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Xml.Linq;
using Menees;

#endregion

namespace WirePeep
{
#pragma warning disable CA1724 // I don't care about a conflict with System.Web.Profile.
	public sealed class Profile
#pragma warning restore CA1724
	{
		#region Constructors

		public Profile(ISettingsNode settingsNode)
		{
			if (settingsNode != null)
			{
				this.Load(settingsNode);
			}

			if (this.Locations.Count == 0)
			{
				this.LoadDefaults();
			}

			this.PeerGroups.CollectionChanged += this.PeerGroupsCollectionChanged;
		}

		#endregion

		#region Public Properties

		public ObservableCollection<PeerGroup> PeerGroups { get; } = new ObservableCollection<PeerGroup>();

		public ObservableCollection<Location> Locations { get; } = new ObservableCollection<Location>();

		#endregion

		#region Public Methods

		public void Save(ISettingsNode settingsNode)
		{
			settingsNode.DeleteSubNode(nameof(this.PeerGroups));
			ISettingsNode peerGroupsNode = settingsNode.GetSubNode(nameof(this.PeerGroups), true);
			foreach (PeerGroup peerGroup in this.PeerGroups)
			{
				peerGroup.Save(peerGroupsNode.GetSubNode(peerGroup.Id.ToString(), true));
			}

			settingsNode.DeleteSubNode(nameof(this.Locations));
			ISettingsNode locationsNode = settingsNode.GetSubNode(nameof(this.Locations), true);
			foreach (Location location in this.Locations)
			{
				location.Save(locationsNode.GetSubNode(location.Id.ToString(), true));
			}
		}

		public void RevertToDefaults()
		{
			this.Locations.Clear();
			this.PeerGroups.Clear();
			this.LoadDefaults();
		}

		#endregion

		#region Private Methods

		private void Load(ISettingsNode settingsNode)
		{
			ISettingsNode peerGroupsNode = settingsNode.GetSubNode(nameof(this.PeerGroups), false);
			if (peerGroupsNode != null)
			{
				foreach (string subNodeName in peerGroupsNode.GetSubNodeNames())
				{
					ISettingsNode peerGroupNode = peerGroupsNode.GetSubNode(subNodeName, false);
					PeerGroup peerGroup = PeerGroup.TryLoad(peerGroupNode);
					if (peerGroup != null)
					{
						this.PeerGroups.Add(peerGroup);
					}
				}
			}

			Dictionary<Guid, PeerGroup> idToGroupMap = this.PeerGroups.ToDictionary(group => group.Id);
			ISettingsNode locationsNode = settingsNode.GetSubNode(nameof(this.Locations), false);
			if (locationsNode != null)
			{
				foreach (string subNodeName in locationsNode.GetSubNodeNames())
				{
					ISettingsNode locationNode = locationsNode.GetSubNode(subNodeName, false);
					Location location = Location.TryLoad(locationNode, idToGroupMap);
					if (location != null)
					{
						this.Locations.Add(location);
					}
				}
			}
		}

		private void LoadDefaults()
		{
			void AddLocation(Location location)
			{
				// Don't add a duplicate address (e.g., both router and modem if they're the same).
				if (!this.Locations.Any(l => l.Address == location.Address))
				{
					if (!this.PeerGroups.Contains(location.PeerGroup))
					{
						this.PeerGroups.Add(location.PeerGroup);
					}

					this.Locations.Add(location);
				}
			}

			XElement root = XElement.Parse(Properties.Resources.DefaultProfileXml);
			foreach (XElement groupElement in root.Elements(nameof(PeerGroup)))
			{
				string groupName = groupElement.GetAttributeValue("Name");
#pragma warning disable MEN010 // Avoid magic numbers. Default times are clear in context.
				int failSeconds = groupElement.GetAttributeValue("FailSeconds", 10);
				int pollSeconds = groupElement.GetAttributeValue("PollSeconds", 5);
				int waitMilliseconds = groupElement.GetAttributeValue("WaitMilliseconds", 200);
#pragma warning restore MEN010 // Avoid magic numbers

				PeerGroup group = new PeerGroup(
					groupName,
					TimeSpan.FromSeconds(failSeconds),
					TimeSpan.FromSeconds(pollSeconds),
					TimeSpan.FromMilliseconds(waitMilliseconds));

				foreach (XElement locationElement in groupElement.Elements(nameof(Location)))
				{
					string locationName = locationElement.GetAttributeValue("Name");
					int ordinal = 1;
					foreach (XElement addressElement in locationElement.Elements("Address"))
					{
						if (IPAddress.TryParse(addressElement.Value, out IPAddress address))
						{
							string nameSuffix = ordinal == 1 ? string.Empty : $" #{ordinal}";
							Location location = new Location(group, locationName + nameSuffix, address);
							AddLocation(location);
							ordinal++;
						}
					}
				}

				foreach (XElement findElement in groupElement.Elements("Find"))
				{
					string findName = findElement.GetAttributeValue("Name");
					string findType = findElement.GetAttributeValue("Type");
					IPAddress address;
					switch (findType)
					{
						case "DefaultGateway":
							// The first IP address returned by a traceroute should be the gateway. https://stackoverflow.com/a/29494180/1882616
							// We'll simulate that by doing a ping with TTL = 1. https://stackoverflow.com/a/45565253/1882616
							address = Pinger.GetAddressAtTtl(IPAddress.Parse("8.8.8.8"), 1, group.Wait);
							break;

						case "CableModem":
							using (Pinger pinger = new Pinger(group.Wait))
							{
								address = findElement.Elements("Address").Select(e => IPAddress.Parse(e.Value)).FirstOrDefault(a => pinger.CanPing(a));
							}

							break;

						default:
							throw Exceptions.NewInvalidOperationException($"Unsupported Find Type: {findType}");
					}

					if (address != null)
					{
						Location location = new Location(group, findName, address);
						AddLocation(location);
					}
				}
			}
		}

		#endregion

		#region Private Event Handlers

		private void PeerGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			List<Location> toRemove = null;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Remove:
					// Remove any locations using the removed peer groups. The UI should prevent this from occurring.
					IEnumerable<PeerGroup> oldItems = e.OldItems.Cast<PeerGroup>();
					toRemove = new List<Location>();
					foreach (Location location in this.Locations.Where(l => oldItems.Any(g => g == l.PeerGroup)))
					{
						toRemove.Add(location);
					}

					break;

				case NotifyCollectionChangedAction.Replace:
					// Replace any locations that are using the replaced peer groups.
					oldItems = e.OldItems.Cast<PeerGroup>();
					IEnumerable<PeerGroup> newItems = e.NewItems.Cast<PeerGroup>();
					var pairs = oldItems.Zip(newItems, (o, n) => Tuple.Create(o, n)).ToArray();
					int numLocations = this.Locations.Count;
					for (int i = 0; i < numLocations; i++)
					{
						Location currentLocation = this.Locations[i];
						PeerGroup replacement = pairs.FirstOrDefault(p => p.Item1 == currentLocation.PeerGroup)?.Item2;
						if (replacement != null)
						{
							this.Locations[i] = new Location(replacement, currentLocation.Name, currentLocation.Address, currentLocation.Id);
						}
					}

					break;

				case NotifyCollectionChangedAction.Reset:
					// Update all locations. Use latest peer group with matching Id. Delete non-matches.
					numLocations = this.Locations.Count;
					var idToGroupMap = this.PeerGroups.ToDictionary(g => g.Id);
					toRemove = new List<Location>();
					for (int i = 0; i < numLocations; i++)
					{
						Location currentLocation = this.Locations[i];
						if (idToGroupMap.TryGetValue(currentLocation.PeerGroup.Id, out PeerGroup replacement))
						{
							this.Locations[i] = new Location(replacement, currentLocation.Name, currentLocation.Address, currentLocation.Id);
						}
						else
						{
							toRemove.Add(currentLocation);
						}
					}

					break;
			}

			foreach (Location location in toRemove ?? Enumerable.Empty<Location>())
			{
				this.Locations.Remove(location);
			}
		}

		#endregion
	}
}

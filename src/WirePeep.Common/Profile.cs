#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			this.GetHashCode();
			settingsNode.GetHashCode();
		}

		#endregion

		#region Private Methods

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

		private void Load(ISettingsNode settingsNode)
		{
			// TODO: Load configuration. [Bill, 5/7/2020]
			this.GetHashCode();
			settingsNode.GetHashCode();
		}

		#endregion
	}
}

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	/// <summary>
	/// Provides a friendly name associated with an IPAddress.
	/// </summary>
	public sealed class Location
	{
		#region Constructors

		public Location(PeerGroup peerGroup, string name, IPAddress address, Guid? id = null)
		{
			Conditions.RequireReference(peerGroup, nameof(peerGroup));
			Conditions.RequireString(name, nameof(name));
			Conditions.RequireReference(address, nameof(address));

			this.PeerGroup = peerGroup;
			this.Name = name;
			this.Address = address;
			this.Id = id ?? Guid.NewGuid();
		}

		#endregion

		#region Public Properties

		public PeerGroup PeerGroup { get; }

		public string Name { get; }

		public IPAddress Address { get; }

		public Guid Id { get; }

		#endregion

		#region Public Methods

		public override string ToString() => $"{this.Name} - {this.Address}";

		#endregion

		#region Internal Methods

		internal static Location TryLoad(ISettingsNode settingsNode, IReadOnlyDictionary<Guid, PeerGroup> idToGroupMap)
		{
			Location result = null;

			if (settingsNode != null && Guid.TryParse(settingsNode.NodeName, out Guid id))
			{
				Guid peerGroupId = settingsNode.GetValue("PeerGroupId", Guid.Empty);
				string name = settingsNode.GetValue(nameof(Name), null);
				string addressText = settingsNode.GetValue(nameof(Address), null);

				if (IPAddress.TryParse(addressText, out IPAddress address)
					&& !string.IsNullOrEmpty(name)
					&& idToGroupMap.TryGetValue(peerGroupId, out PeerGroup peerGroup))
				{
					result = new Location(peerGroup, name, address, id);
				}
			}

			return result;
		}

		internal void Save(ISettingsNode settingsNode)
		{
			Debug.Assert(this.Id.ToString() == settingsNode.NodeName, "The Id should be the node name.");

			settingsNode.SetValue("PeerGroupId", this.PeerGroup.Id);
			settingsNode.SetValue(nameof(this.Name), this.Name);
			settingsNode.SetValue(nameof(this.Address), this.Address.ToString());
		}

		#endregion
	}
}

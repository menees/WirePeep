#region Using Directives

using System;
using System.Collections.Generic;
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

		public Location(PeerGroup peerGroup, string name, IPAddress address)
		{
			Conditions.RequireReference(peerGroup, nameof(peerGroup));
			Conditions.RequireString(name, nameof(name));

			this.PeerGroup = peerGroup;
			this.Name = name;
			this.Address = address;
			this.Id = Guid.NewGuid();
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
	}
}

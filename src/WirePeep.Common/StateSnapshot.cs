#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace WirePeep
{
	public sealed class StateSnapshot
	{
		#region Private Data Members

		private readonly Dictionary<PeerGroupState, IEnumerable<LocationState>> states;

		#endregion

		#region Constructors

		internal StateSnapshot(DateTime created, int capacity)
		{
			this.Created = created;
			this.states = new Dictionary<PeerGroupState, IEnumerable<LocationState>>(capacity);
		}

		#endregion

		#region Public Properties

		public DateTime Created { get; }

		public IEnumerable<PeerGroupState> AllPeerGroups => this.states.Keys;

		public IEnumerable<KeyValuePair<PeerGroupState, IEnumerable<LocationState>>> AllPeerGroupLocations => this.states;

		public IEnumerable<PeerGroupState> FailedChangedPeerGroups => this.states.Keys.Where(group => group.IsFailedChanged == this.Created);

		#endregion

		#region Internal Methods

		internal void Add(PeerGroupState snapshotPeerGroup, IEnumerable<LocationState> snapshotLocations)
			=> this.states.Add(snapshotPeerGroup, snapshotLocations);

		#endregion
	}
}

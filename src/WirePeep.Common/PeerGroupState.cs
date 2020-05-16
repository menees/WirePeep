#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class PeerGroupState
	{
		#region Private Data Members

		private DateTime lastUpdated;

		#endregion

		#region Constructors

		internal PeerGroupState(PeerGroup peerGroup)
		{
			this.PeerGroup = peerGroup;
			this.UpdateCounter = -1;
		}

		#endregion

		#region Public Properties

		public PeerGroup PeerGroup { get; }

		public bool IsConnected { get; private set; }

		public long UpdateCounter { get; private set; }

		public DateTime? IsConnectedChanged { get; private set; }

		public bool IsFailed { get; private set; }

		public DateTime? IsFailedChanged { get; private set; }

		#endregion

		#region Public Methods

		public override string ToString() => this.PeerGroup.ToString();

		public void Update(DateTime utcNow, IReadOnlyList<LocationState> locations, bool simulateFailure)
		{
			if (this.PeerGroup.CanPoll(utcNow, this.lastUpdated))
			{
				this.lastUpdated = utcNow;

				// Increment this each time so we can uniquely identify which Update call last
				// updated an item and so we'll round-robin through each location in the list.
				this.UpdateCounter++;

				bool wasConnected = this.IsConnected;

				bool? isPeerGroupConnected = null;
				int numLocations = locations.Count;
				for (int i = 0; i < numLocations; i++)
				{
					int locationIndex = (int)unchecked((this.UpdateCounter + i) % numLocations);
					LocationState locationState = locations[locationIndex];
					bool? wasLocationUpdated = locationState.Update(utcNow, this.UpdateCounter, simulateFailure);

					// A null result means we've polled it too recently.
					if (wasLocationUpdated != null)
					{
						// If we get a connected result, then we can quit early.
						isPeerGroupConnected = locationState.IsConnected;
						if (isPeerGroupConnected ?? false)
						{
							break;
						}
					}
				}

				if (isPeerGroupConnected != null)
				{
					this.IsConnected = isPeerGroupConnected.Value;
				}

				if (this.IsConnected != wasConnected)
				{
					this.IsConnectedChanged = utcNow;
				}

				bool wasFailed = this.IsFailed;
				if (this.IsConnected)
				{
					this.IsFailed = false;
				}
				else if (!this.IsFailed && (this.IsConnectedChanged == null || utcNow >= (this.IsConnectedChanged.Value + this.PeerGroup.Fail)))
				{
					this.IsFailed = true;
				}

				if (this.IsFailed != wasFailed)
				{
					this.IsFailedChanged = utcNow;
				}
			}
		}

		public PeerGroupState ShallowCopy() => (PeerGroupState)this.MemberwiseClone();

		#endregion
	}
}

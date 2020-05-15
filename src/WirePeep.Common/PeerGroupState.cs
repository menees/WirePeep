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
		#region Constructors

		public PeerGroupState(PeerGroup peerGroup)
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

		public bool Update(IReadOnlyList<LocationState> locations)
		{
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
				bool? wasLocationUpdated = locationState.Update(this.UpdateCounter);

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

			bool result = this.IsConnected != wasConnected;
			if (result)
			{
				this.IsConnectedChanged = DateTime.UtcNow;
			}

			bool wasFailed = this.IsFailed;
			if (this.IsConnected)
			{
				this.IsFailed = false;
			}
			else if (!this.IsFailed && (this.IsConnectedChanged == null || DateTime.UtcNow >= (this.IsConnectedChanged.Value + this.PeerGroup.Fail)))
			{
				this.IsFailed = true;
			}

			if (this.IsFailed != wasFailed)
			{
				this.IsFailedChanged = DateTime.UtcNow;
			}

			return result;
		}

		public PeerGroupState ShallowCopy() => (PeerGroupState)this.MemberwiseClone();

		#endregion
	}
}

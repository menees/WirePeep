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

		internal PeerGroupState(PeerGroup peerGroup)
		{
			this.PeerGroup = peerGroup;
			this.UpdateCounter = -1;
		}

		#endregion

		#region Public Properties

		public PeerGroup PeerGroup { get; }

		public ConnectionState Connection { get; private set; }

		public long UpdateCounter { get; private set; }

		public DateTime? ConnectionChanged { get; private set; }

		public bool IsFailed { get; private set; }

		public DateTime? IsFailedChanged { get; private set; }

		public DateTime LastUpdated { get; private set; }

		public DateTime LastUpdateRequest { get; private set; }

		#endregion

		#region Public Methods

		public override string ToString() => this.PeerGroup.ToString();

		public void Update(DateTime utcNow, IReadOnlyList<LocationState> locations, ConnectionState? simulateConnection)
		{
			this.LastUpdateRequest = utcNow;
			if (this.PeerGroup.CanPoll(utcNow, this.LastUpdated))
			{
				this.LastUpdated = utcNow;

				// Increment this each time so we can uniquely identify which Update call last
				// updated an item and so we'll round-robin through each location in the list.
				this.UpdateCounter++;

				ConnectionState priorConnection = this.Connection;

				ConnectionState? peerGroupConnection = null;
				int numLocations = locations.Count;
				for (int i = 0; i < numLocations; i++)
				{
					int locationIndex = (int)unchecked((this.UpdateCounter + i) % numLocations);
					LocationState locationState = locations[locationIndex];
					bool? wasLocationUpdated = locationState.Update(utcNow, this.UpdateCounter, simulateConnection);

					// A null result means we've polled it too recently.
					if (wasLocationUpdated != null)
					{
						// If we get a Connected result, then we can quit early.
						// If we only get Disconnected or Unavailable, then we have to check them all.
						peerGroupConnection = locationState.Connection;
						if (peerGroupConnection == ConnectionState.Connected)
						{
							break;
						}
					}
				}

				// This should only be null when we've polled all the locations too recently, so Connection shouldn't change.
				if (peerGroupConnection != null)
				{
					this.Connection = peerGroupConnection.Value;
				}

				if (this.Connection != priorConnection)
				{
					this.ConnectionChanged = utcNow;
				}

				bool wasFailed = this.IsFailed;
				if (this.Connection == ConnectionState.Connected || this.Connection == ConnectionState.Unavailable)
				{
					this.IsFailed = false;
				}
				else if (!this.IsFailed && (this.ConnectionChanged == null || utcNow >= (this.ConnectionChanged.Value + this.PeerGroup.Fail)))
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

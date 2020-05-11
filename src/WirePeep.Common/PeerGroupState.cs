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

		private long counter;

		#endregion

		#region Constructors

		public PeerGroupState(PeerGroup peerGroup)
		{
			this.PeerGroup = peerGroup;
		}

		#endregion

		#region Public Properties

		public PeerGroup PeerGroup { get; }

		public bool IsConnected { get; private set; }

		#endregion

		#region Public Methods

		public bool Update(IReadOnlyList<LocationState> locations)
		{
			// Increment this each time so we can uniquely identify which Update call last
			// updated an item and so we'll round-robin through each location in the list.
			this.counter = unchecked(this.counter + 1);

			bool wasConnected = this.IsConnected;

			bool? isPeerGroupConnected = null;
			int numLocations = locations.Count;
			for (int i = 0; i < numLocations; i++)
			{
				int locationIndex = (int)unchecked((this.counter + i) % numLocations);
				LocationState locationState = locations[locationIndex];
				bool? isLocationConnected = locationState.Update(this.counter);

				// A null result means we've polled it too recently.
				if (isLocationConnected != null)
				{
					// If we get a connected result, then we can quit early.
					isPeerGroupConnected = isLocationConnected.Value;
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
			return result;
		}

		#endregion
	}
}

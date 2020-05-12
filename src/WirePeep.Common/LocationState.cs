#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace WirePeep
{
	public sealed class LocationState
	{
		#region Private Data Members

		private DateTime lastPolled;

		#endregion

		#region Constructors

		public LocationState(Location location)
		{
			this.Location = location;
		}

		#endregion

		#region Public Properties

		public Location Location { get; }

		public bool IsConnected { get; private set; }

		public TimeSpan RoundtripTime { get; private set; }

		public long UpdateCounter { get; private set; }

		#endregion

		#region Public Methods

		public bool? Update(long counter)
		{
			bool? result = null;

			DateTime utcNow = DateTime.UtcNow;
			PeerGroup peerGroup = this.Location.PeerGroup;
			if (utcNow - this.lastPolled > peerGroup.Poll)
			{
				bool wasConnected = this.IsConnected;

				using (Pinger pinger = new Pinger(peerGroup.Wait))
				{
					this.IsConnected = pinger.TryPing(this.Location.Address, out TimeSpan roundtripTime);
					this.RoundtripTime = roundtripTime;
					this.UpdateCounter = counter;
				}

				result = this.IsConnected != wasConnected;
				this.lastPolled = utcNow;
			}

			return result;
		}

		public LocationState ShallowCopy() => (LocationState)this.MemberwiseClone();

		#endregion
	}
}

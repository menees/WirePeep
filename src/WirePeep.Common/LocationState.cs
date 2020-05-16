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

		internal LocationState(Location location)
		{
			this.Location = location;
		}

		#endregion

		#region Public Properties

		public Location Location { get; }

		public bool? IsConnected { get; private set; }

		public TimeSpan RoundtripTime { get; private set; }

		public long UpdateCounter { get; private set; }

		#endregion

		#region Public Methods

		public override string ToString() => this.Location.ToString();

		public bool? Update(DateTime utcNow, long counter, bool simulateFailure)
		{
			bool? result = null;

			PeerGroup peerGroup = this.Location.PeerGroup;
			if (peerGroup.CanPoll(utcNow, this.lastPolled))
			{
				bool? wasConnected = this.IsConnected;

				using (Pinger pinger = new Pinger(peerGroup.Wait))
				{
					TimeSpan roundtripTime = TimeSpan.Zero;
					this.IsConnected = !simulateFailure && pinger.TryPing(this.Location.Address, out roundtripTime);
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

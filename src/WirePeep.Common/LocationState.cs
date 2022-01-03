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
			this.UpdateCounter = -2;
		}

		#endregion

		#region Public Properties

		public Location Location { get; }

		public ConnectionState Connection { get; private set; }

		public TimeSpan RoundtripTime { get; private set; }

		public long UpdateCounter { get; private set; }

		#endregion

		#region Public Methods

		public override string ToString() => this.Location.ToString();

		public bool? Update(DateTime utcNow, long counter, ConnectionState? simulateConnection)
		{
			bool? result = null;

			PeerGroup peerGroup = this.Location.PeerGroup;
			if (peerGroup.CanPoll(utcNow, this.lastPolled))
			{
				ConnectionState priorConnection = this.Connection;

				using (Pinger pinger = new(peerGroup.Wait))
				{
					TimeSpan roundtripTime = TimeSpan.Zero;
					if (simulateConnection != null)
					{
						this.Connection = simulateConnection.Value;
					}
					else
					{
						bool? ping = pinger.TryPing(this.Location.Address, out roundtripTime);
						switch (ping)
						{
							case true:
								this.Connection = ConnectionState.Connected;
								break;

							case false:
								this.Connection = ConnectionState.Disconnected;
								break;

							default:
								this.Connection = ConnectionState.Unavailable;
								break;
						}
					}

					this.RoundtripTime = roundtripTime;
					this.UpdateCounter = counter;
				}

				result = this.Connection != priorConnection;
				this.lastPolled = utcNow;
			}

			return result;
		}

		public LocationState ShallowCopy() => (LocationState)this.MemberwiseClone();

		#endregion
	}
}

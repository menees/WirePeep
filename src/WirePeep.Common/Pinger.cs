#region Using Directives

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class Pinger : IDisposable
	{
		#region Private Data Members

		private static readonly byte[] BufferContent = Encoding.ASCII.GetBytes($"Ping testing from {nameof(WirePeep)}...");

		private readonly Ping ping;
		private readonly int waitMilliseconds;
		private readonly PingOptions options;

		#endregion

		#region Constructors

		public Pinger(TimeSpan wait)
		{
			this.ping = new Ping();
			this.waitMilliseconds = (int)(wait.Ticks / TimeSpan.TicksPerMillisecond);
		}

		private Pinger(TimeSpan wait, int ttl)
			: this(wait)
		{
			this.options = new PingOptions { Ttl = ttl };
		}

		#endregion

		#region Public Methods

		public static IPAddress GetAddressAtTtl(IPAddress searchAddress, int ttl, TimeSpan wait)
		{
			IPAddress result = null;

			using (Pinger pinger = new Pinger(wait, ttl))
			{
				PingReply reply = pinger.Send(searchAddress);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					result = reply.Address;
				}
			}

			return result;
		}

		public bool CanPing(IPAddress address)
		{
			PingReply reply = this.Send(address);
			bool result = reply.Status == IPStatus.Success;
			return result;
		}

		public bool TryPing(IPAddress address, out TimeSpan roundtripTime)
		{
			PingReply reply = this.Send(address);
			roundtripTime = TimeSpan.FromMilliseconds(reply.RoundtripTime);
			bool result = reply.Status == IPStatus.Success;
			return result;
		}

		public void Dispose()
		{
			this.ping.Dispose();
		}

		#endregion

		#region Private Methods

		private PingReply Send(IPAddress address)
		{
			PingReply result = this.ping.Send(address, this.waitMilliseconds, BufferContent, this.options);
			return result;
		}

		#endregion
	}
}

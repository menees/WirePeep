#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
				PingReply reply = pinger.TrySend(searchAddress);
				if (reply?.Status == IPStatus.Success || reply?.Status == IPStatus.TtlExpired)
				{
					result = reply?.Address;
				}
			}

			return result;
		}

		public bool CanPing(IPAddress address)
		{
			PingReply reply = this.TrySend(address);
			bool result = reply?.Status == IPStatus.Success;
			return result;
		}

		public bool TryPing(IPAddress address, out TimeSpan roundtripTime)
		{
			PingReply reply = this.TrySend(address);
			roundtripTime = TimeSpan.FromMilliseconds(reply?.RoundtripTime ?? 0);
			bool result = reply?.Status == IPStatus.Success;
			return result;
		}

		public void Dispose()
		{
			this.ping.Dispose();
		}

		#endregion

		#region Private Methods

		private PingReply TrySend(IPAddress address)
		{
			PingReply result = null;
			try
			{
				result = this.ping.Send(address, this.waitMilliseconds, BufferContent, this.options);
			}
			catch (PingException ex)
			{
				// MS Help says about PingException:
				//    "An exception was thrown while sending or receiving the ICMP messages.
				//     See the inner exception for the exact exception that was thrown."
				// I've seen a PingException happen once with an inner Win32Exception.
				// That was after a reboot with a quick login, and WirePeep started up a little
				// bit before the network stack was ready. So the internal IcmpSendEcho2 call
				// returned a failure code, but it wasn't captured in the logged exception details.
				// Now, I'll just treat PingExceptions the same as failed pings but without a PingReply.
				IDictionary<string, object> context = null;
				if (ex.InnerException is Win32Exception win32)
				{
					context = new Dictionary<string, object> { { "Win32 Error", win32.NativeErrorCode } };
				}
				else if (ex.InnerException != null)
				{
					context = new Dictionary<string, object> { { "Inner HResult", ex.InnerException.HResult } };
				}

				Log.Error(this.GetType(), "A ping exception occurred.", ex, context);
			}

			return result;
		}

		#endregion
	}
}

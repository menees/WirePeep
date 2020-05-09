#region Using Directives

using System;
using System.Net;
using System.Net.NetworkInformation;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class Pinger
	{
		// TODO: Use https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping?view=netcore-3.1. [Bill, 5/6/2020]
		#region Public Methods

		public static IPAddress GetAddressAtTtl(IPAddress searchAddress, int ttl, int waitMilliseconds)
		{
			IPAddress result = null;

			using (Ping ping = new Ping())
			{
				PingOptions options = new PingOptions { Ttl = ttl };
				PingReply reply = Send(ping, searchAddress, waitMilliseconds, options);
				if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
				{
					result = reply.Address;
				}
			}

			return result;
		}

		public static bool CanPing(IPAddress address, int waitMilliseconds)
		{
			using (Ping ping = new Ping())
			{
				PingReply reply = Send(ping, address, waitMilliseconds);
				bool result = reply.Status == IPStatus.Success;
				return result;
			}
		}

		#endregion

		#region Private Methods

		private static PingReply Send(Ping ping, IPAddress address, int waitMilliseconds, PingOptions options = null)
		{
			ping.Send(address);
			PingReply result = ping.Send(address, waitMilliseconds, CollectionUtility.EmptyArray<byte>(), options);
			return result;
		}

		#endregion
	}
}

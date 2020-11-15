#region Using Directives

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

#endregion

namespace WirePeep
{
	#region public ConnectionState

	public enum ConnectionState
	{
		/// <summary>
		/// Windows's networking stack hasn't started up yet, so we can't make network requests
		/// to determine <see cref="Connected"/> or <see cref="Disconnected"/>.
		/// </summary>
		/// <see cref="NetworkInterface.GetIsNetworkAvailable"/>
		Unavailable,

		/// <summary>
		/// A network connection was made to the resource.
		/// </summary>
		Connected,

		/// <summary>
		/// A network connection could not be made to the resource even though Windows's networking is available.
		/// </summary>
		Disconnected,
	}

	#endregion

	#region public LogFileNameFormat

	public enum LogFileNameFormat
	{
		Fixed,
		LocalNow,
		UtcNow,
	}

	#endregion
}

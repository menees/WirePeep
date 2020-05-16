#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace WirePeep
{
	internal sealed class LogRow
	{
		#region Public Methods

		public void Update(PeerGroupState peerGroupState)
		{
			// TODO: Finish Update. [Bill, 5/16/2020]
			peerGroupState.GetHashCode();
			this.GetHashCode();
		}

		#endregion
	}
}

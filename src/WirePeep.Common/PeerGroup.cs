#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	/// <summary>
	/// If any of the <see cref="Location"/>s associated with this peer group can be contacted,
	/// then we report a connectivity success for this group.
	///
	/// If none of the <see cref="Location"/>s associated with this peer group can be contacted,
	/// then we report a connectivity failure for this group.
	/// </summary>
	public sealed class PeerGroup
	{
		#region Constructors

		public PeerGroup(string name, TimeSpan fail, TimeSpan poll, TimeSpan wait)
		{
			Conditions.RequireString(name, nameof(name));
			this.Name = name;
			this.Fail = fail;
			this.Poll = poll;
			this.Wait = wait;
		}

		#endregion

		#region Public Properties

		public string Name { get; }

		public TimeSpan Fail { get; }

		public TimeSpan Poll { get; }

		public TimeSpan Wait { get; }

		#endregion
	}
}

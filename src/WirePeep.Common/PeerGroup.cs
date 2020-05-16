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
		#region Private Data Members

		private static readonly TimeSpan PollTolerance = TimeSpan.FromMilliseconds(20);

		#endregion

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

		#region Public Methods

		public override string ToString() => this.Name;

		// Timer callbacks may not occur on perfect 1 second intervals, so we'll allow a few milliseconds tolerance.
		public bool CanPoll(DateTime utcNow, DateTime utcLastPolled) => (utcNow - utcLastPolled) > (this.Poll - PollTolerance);

		#endregion
	}
}

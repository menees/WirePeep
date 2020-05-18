#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

		internal PeerGroup(string name, TimeSpan fail, TimeSpan poll, TimeSpan wait, Guid? id = null)
		{
			Conditions.RequireString(name, nameof(name));
			this.Name = name;
			this.Fail = fail;
			this.Poll = poll;
			this.Wait = wait;
			this.Id = id ?? Guid.NewGuid();
		}

		#endregion

		#region Public Properties

		public string Name { get; }

		public TimeSpan Fail { get; }

		public TimeSpan Poll { get; }

		public TimeSpan Wait { get; }

		public Guid Id { get; }

		#endregion

		#region Public Methods

		public override string ToString() => this.Name;

		// Timer callbacks may not occur on perfect 1 second intervals, so we'll allow a few milliseconds tolerance.
		public bool CanPoll(DateTime utcNow, DateTime utcLastPolled) => (utcNow - utcLastPolled) > (this.Poll - PollTolerance);

		#endregion

		#region Internal Methods

		internal static PeerGroup TryLoad(ISettingsNode settingsNode)
		{
			PeerGroup result = null;

			if (settingsNode != null && Guid.TryParse(settingsNode.NodeName, out Guid id))
			{
#pragma warning disable MEN010 // Avoid magic numbers. Default values are clear in context.
				TimeSpan fail = settingsNode.GetValue(nameof(Fail), TimeSpan.FromSeconds(10));
				TimeSpan poll = settingsNode.GetValue(nameof(Poll), TimeSpan.FromSeconds(5));
				TimeSpan wait = settingsNode.GetValue(nameof(Wait), TimeSpan.FromMilliseconds(200));
#pragma warning restore MEN010 // Avoid magic numbers

				string name = settingsNode.GetValue(nameof(Name), null);
				if (!string.IsNullOrEmpty(name))
				{
					result = new PeerGroup(name, fail, poll, wait, id);
				}
			}

			return result;
		}

		internal void Save(ISettingsNode settingsNode)
		{
			Debug.Assert(this.Id.ToString() == settingsNode.NodeName, "The Id should be the node name.");

			settingsNode.SetValue(nameof(this.Name), this.Name);
			settingsNode.SetValue(nameof(this.Fail), this.Fail);
			settingsNode.SetValue(nameof(this.Poll), this.Poll);
			settingsNode.SetValue(nameof(this.Wait), this.Wait);
		}

		#endregion
	}
}

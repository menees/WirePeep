#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace WirePeep
{
	internal sealed class LogRow : PropertyChangeNotifier
	{
		#region Private Data Members

		private string peerGroupName;
		private DateTime failStarted;
		private TimeSpan length;
		private DateTime? failEnded;
		private TimeSpan? sincePrevious;
		private string comment;
		private bool isActive;

		#endregion

		#region Public Properties

		public string PeerGroupName { get => this.peerGroupName; set => this.Update(ref this.peerGroupName, value); }

		public DateTime FailStarted { get => this.failStarted; set => this.Update(ref this.failStarted, value); }

		public TimeSpan Length { get => this.length; set => this.Update(ref this.length, value); }

		public DateTime? FailEnded { get => this.failEnded; set => this.Update(ref this.failEnded, value); }

		public TimeSpan? SincePrevious { get => this.sincePrevious; set => this.Update(ref this.sincePrevious, value); }

		public string Comment { get => this.comment; set => this.Update(ref this.comment, value); }

		public bool IsActive { get => this.isActive; set => this.Update(ref this.isActive, value); }

		#endregion

		#region Public Methods

		public void Update(PeerGroupState peerGroupState, LogRow previousLogRow = null)
		{
			this.PeerGroupName = peerGroupState.PeerGroup.Name;

			if (this.FailStarted == DateTime.MinValue)
			{
				this.FailStarted = peerGroupState.IsFailedChanged.Value;
				this.IsActive = true;
			}

			this.Length = MainWindow.TruncateToSeconds(peerGroupState.LastUpdateRequest - this.FailStarted);

			if (!peerGroupState.IsFailed)
			{
				this.FailEnded = peerGroupState.LastUpdated;
				this.IsActive = false;
			}

			if (previousLogRow?.FailEnded != null)
			{
				this.SincePrevious = MainWindow.TruncateToSeconds(this.FailStarted - previousLogRow.FailEnded.Value);
			}
		}

		#endregion
	}
}

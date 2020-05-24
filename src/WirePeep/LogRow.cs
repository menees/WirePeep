#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using Menees;
using Menees.Windows.Presentation;

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
		private bool isSelected;
		private Guid peerGroupId;

		#endregion

		#region Constructors

		public LogRow(int failureId)
		{
			this.FailureId = failureId;
		}

		#endregion

		#region Public Properties

		public string PeerGroupName { get => this.peerGroupName; set => this.Update(ref this.peerGroupName, value); }

		public DateTime FailStarted
		{
			get => this.failStarted;
			set
			{
				if (this.Update(ref this.failStarted, value))
				{
					this.OnPropertyChanged(nameof(this.FailStartedLocal));
				}
			}
		}

		public DateTime FailStartedLocal => this.failStarted.ToLocalTime();

		public TimeSpan Length { get => this.length; set => this.Update(ref this.length, value); }

		public DateTime? FailEnded
		{
			get => this.failEnded;
			set
			{
				if (this.Update(ref this.failEnded, value))
				{
					this.OnPropertyChanged(nameof(this.FailEndedLocal));
				}
			}
		}

		public DateTime? FailEndedLocal => this.failEnded?.ToLocalTime();

		public TimeSpan? SincePrevious { get => this.sincePrevious; set => this.Update(ref this.sincePrevious, value); }

		public string Comment { get => this.comment; set => this.Update(ref this.comment, value); }

		public bool IsActive { get => this.isActive; set => this.Update(ref this.isActive, value); }

		public bool IsSelected { get => this.isSelected; set => this.Update(ref this.isSelected, value); }

		public Guid PeerGroupId { get => this.peerGroupId; set => this.Update(ref this.peerGroupId, value); }

		public int FailureId { get; }

		#endregion

		#region Public Methods

		public void Update(PeerGroupState peerGroupState, LogRow previousLogRow = null)
		{
			PeerGroup peerGroup = peerGroupState.PeerGroup;
			this.PeerGroupName = peerGroup.Name;
			this.PeerGroupId = peerGroup.Id;

			if (this.FailStarted == DateTime.MinValue)
			{
				this.FailStarted = peerGroupState.IsFailedChanged.Value;
				this.IsActive = true;
			}

			this.Length = ConvertUtility.RoundToSeconds(peerGroupState.LastUpdateRequest - this.FailStarted);

			if (!peerGroupState.IsFailed)
			{
				this.FailEnded = peerGroupState.LastUpdated;
				this.IsActive = false;
			}

			if (previousLogRow?.FailEnded != null)
			{
				this.SincePrevious = ConvertUtility.RoundToSeconds(this.FailStarted - previousLogRow.FailEnded.Value);
			}
		}

		#endregion
	}
}

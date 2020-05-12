#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

#endregion

namespace WirePeep
{
	internal sealed class StatusRow : INotifyPropertyChanged
	{
		#region Private Data Members

		private string groupName;
		private TimeSpan groupFail;
		private TimeSpan groupPoll;
		private TimeSpan groupWait;
		private bool isGroupFailed;
		private string locationName;
		private IPAddress locationAddress;
		private bool? isLocationConnected;
		private TimeSpan locationRoundtripTime;
		private bool isLocationUpToDate;

		#endregion

		#region Public Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Public PeerGroup Properties

		public string GroupName { get => this.groupName; set => this.Update(ref this.groupName, value); }

		public TimeSpan GroupFail { get => this.groupFail; set => this.Update(ref this.groupFail, value); }

		public TimeSpan GroupPoll { get => this.groupPoll; set => this.Update(ref this.groupPoll, value); }

		public TimeSpan GroupWait { get => this.groupWait; set => this.Update(ref this.groupWait, value); }

		public bool IsGroupFailed { get => this.isGroupFailed; set => this.Update(ref this.isGroupFailed, value); }

		#endregion

		#region Public Location Properties

		public string LocationName { get => this.locationName; set => this.Update(ref this.locationName, value); }

		public IPAddress LocationAddress { get => this.locationAddress; set => this.Update(ref this.locationAddress, value); }

		public bool? IsLocationConnected { get => this.isLocationConnected; set => this.Update(ref this.isLocationConnected, value); }

		public TimeSpan LocationRoundtripTime { get => this.locationRoundtripTime; set => this.Update(ref this.locationRoundtripTime, value); }

		public bool IsLocationUpToDate { get => this.isLocationUpToDate; set => this.Update(ref this.isLocationUpToDate, value); }

		#endregion

		#region Public Methods

		public void Update(PeerGroupState peerGroupState, LocationState locationState)
		{
			PeerGroup peerGroup = peerGroupState.PeerGroup;
			this.GroupName = peerGroup.Name;
			this.GroupFail = peerGroup.Fail;
			this.GroupPoll = peerGroup.Poll;
			this.GroupWait = peerGroup.Wait;
			this.IsGroupFailed = peerGroupState.IsFailed;

			Location location = locationState.Location;
			this.LocationName = location.Name;
			this.locationAddress = location.Address;
			this.IsLocationConnected = locationState.IsConnected;
			this.LocationRoundtripTime = locationState.RoundtripTime;
			this.IsLocationUpToDate = locationState.UpdateCounter == peerGroupState.UpdateCounter;
		}

		#endregion

		#region Private Methods

		private void Update<T>(ref T member, T value, [CallerMemberName] string callerMemberName = null)
		{
			if (!EqualityComparer<T>.Default.Equals(member, value))
			{
				member = value;
				PropertyChangedEventHandler handler = this.PropertyChanged;
				if (handler != null)
				{
					PropertyChangedEventArgs args = new PropertyChangedEventArgs(callerMemberName);
					handler(this, args);
				}
			}
		}

		#endregion
	}
}

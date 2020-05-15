#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Menees;

#endregion

namespace WirePeep
{
	internal sealed class StatusRow : INotifyPropertyChanged
	{
		#region Private Data Members

		private string groupName;
		private int groupFailSeconds;
		private int groupPollSeconds;
		private int groupWaitMilliseconds;
		private bool isGroupAccessible;
		private string locationName;
		private IPAddress locationAddress;
		private bool? isLocationConnected;
		private int? locationRoundtripMilliseconds;
		private bool isLocationUpToDate;

		#endregion

		#region Public Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Public PeerGroup Properties

		public string GroupName { get => this.groupName; set => this.Update(ref this.groupName, value); }

		public int GroupFailSeconds { get => this.groupFailSeconds; set => this.Update(ref this.groupFailSeconds, value); }

		public int GroupPollSeconds { get => this.groupPollSeconds; set => this.Update(ref this.groupPollSeconds, value); }

		public int GroupWaitMilliseconds { get => this.groupWaitMilliseconds; set => this.Update(ref this.groupWaitMilliseconds, value); }

		public bool IsGroupAccessible { get => this.isGroupAccessible; set => this.Update(ref this.isGroupAccessible, value); }

		#endregion

		#region Public Location Properties

		public string LocationName { get => this.locationName; set => this.Update(ref this.locationName, value); }

		public IPAddress LocationAddress { get => this.locationAddress; set => this.Update(ref this.locationAddress, value); }

		public bool? IsLocationConnected { get => this.isLocationConnected; set => this.Update(ref this.isLocationConnected, value); }

		public int? LocationRoundtripMilliseconds
		{
			get => this.locationRoundtripMilliseconds;
			set => this.Update(ref this.locationRoundtripMilliseconds, value);
		}

		public bool IsLocationUpToDate { get => this.isLocationUpToDate; set => this.Update(ref this.isLocationUpToDate, value); }

		#endregion

		#region Public Methods

		public void Update(PeerGroupState peerGroupState, LocationState locationState)
		{
			PeerGroup peerGroup = peerGroupState.PeerGroup;
			this.GroupName = peerGroup.Name;
			this.GroupFailSeconds = (int)peerGroup.Fail.TotalSeconds;
			this.GroupPollSeconds = (int)peerGroup.Poll.TotalSeconds;
			this.GroupWaitMilliseconds = (int)peerGroup.Wait.TotalMilliseconds;

			bool isGroupAccessible = !peerGroupState.IsFailed;
#if DEBUG
			// TODO: ApplicationInfo.IsDebugBuild: Assembly.GetEntryAssembly --> [assembly: AssemblyConfiguration("Debug")]. [Bill, 5/15/2020]
			// In debug builds simulate a failure when ScrollLock is toggled on.
			if (isGroupAccessible && Keyboard.IsKeyToggled(Key.Scroll))
			{
				isGroupAccessible = false;
			}
#endif
			this.IsGroupAccessible = isGroupAccessible;

			Location location = locationState.Location;
			this.LocationName = location.Name;
			this.LocationAddress = location.Address;
			this.IsLocationConnected = locationState.IsConnected;
			this.LocationRoundtripMilliseconds = locationState.IsConnected ?? false ? (int)locationState.RoundtripTime.TotalMilliseconds : (int?)null;
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

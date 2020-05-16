#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace WirePeep
{
	public sealed class StateManager
	{
		#region Private Data Members

		private readonly Profile profile;
		private readonly object mapLock = new object();
		private Dictionary<PeerGroupState, List<LocationState>> peerGroupToLocationsMap;

		#endregion

		#region Constructors

		public StateManager(Profile profile)
		{
			this.profile = profile;

			this.profile.Locations.CollectionChanged += this.LocationsCollectionChanged;
			this.profile.PeerGroups.CollectionChanged += this.PeerGroupsCollectionChanged;

			this.UpdatePeerGroups(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			this.UpdateLocations(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Public Properties

		public DateTime Started { get; } = DateTime.UtcNow;

		public TimeSpan Monitored => DateTime.UtcNow - this.Started;

		public ObservableCollection<PeerGroupState> PeerGroups { get; } = new ObservableCollection<PeerGroupState>();

		public ObservableCollection<LocationState> Locations { get; } = new ObservableCollection<LocationState>();

		#endregion

		#region Public Methods

		public Dictionary<PeerGroupState, IReadOnlyList<LocationState>> Update(bool simulateFailure)
		{
			Dictionary<PeerGroupState, List<LocationState>> mapCopy;
			lock (this.mapLock)
			{
				mapCopy = new Dictionary<PeerGroupState, List<LocationState>>(this.peerGroupToLocationsMap);
			}

			ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };
			Parallel.ForEach(mapCopy, options, pair => pair.Key.Update(pair.Value, simulateFailure));

			var result = new Dictionary<PeerGroupState, IReadOnlyList<LocationState>>(mapCopy.Count);
			foreach (var pair in mapCopy)
			{
				PeerGroupState privatePeerGroup = pair.Key;
				PeerGroupState publicPeerGroup = privatePeerGroup.ShallowCopy();

				List<LocationState> privateLocations = pair.Value;
				List<LocationState> publicLocations = privateLocations.Select(l => l.ShallowCopy()).ToList();

				result.Add(publicPeerGroup, publicLocations);
			}

			return result;
		}

		#endregion

		#region Private Methods

		private bool FindPeerGroupState(PeerGroup peerGroup, out PeerGroupState state)
		{
			state = this.PeerGroups.FirstOrDefault(s => s.PeerGroup == peerGroup);
			return state != null;
		}

		private PeerGroupState GetPeerGroupState(PeerGroup peerGroup, bool allowAdd = true)
		{
			if (!this.FindPeerGroupState(peerGroup, out PeerGroupState result))
			{
				result = new PeerGroupState(peerGroup);
				if (allowAdd)
				{
					this.PeerGroups.Add(result);
				}
			}

			return result;
		}

		private void UpdatePeerGroups(NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (PeerGroup peerGroup in e.NewItems)
					{
						this.GetPeerGroupState(peerGroup);
					}

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (PeerGroup peerGroup in e.OldItems)
					{
						if (this.FindPeerGroupState(peerGroup, out PeerGroupState state))
						{
							this.PeerGroups.Remove(state);
						}
					}

					break;

				case NotifyCollectionChangedAction.Replace:
					foreach (var pair in e.OldItems.Cast<PeerGroup>().Zip(e.NewItems.Cast<PeerGroup>(), (o, n) => Tuple.Create(o, n)))
					{
						if (this.FindPeerGroupState(pair.Item1, out PeerGroupState oldState))
						{
							PeerGroupState newState = this.GetPeerGroupState(pair.Item2, allowAdd: false);
							int index = this.PeerGroups.IndexOf(oldState);
							this.PeerGroups[index] = newState;
						}
					}

					break;

				case NotifyCollectionChangedAction.Move:
					// We don't care about the profile's PeerGroup positions.
					break;

				default: // NotifyCollectionChangedAction.Reset
					foreach (PeerGroup peerGroup in this.profile.PeerGroups)
					{
						this.GetPeerGroupState(peerGroup);
					}

					foreach (PeerGroupState state in this.PeerGroups.ToList())
					{
						if (!this.profile.PeerGroups.Contains(state.PeerGroup))
						{
							this.PeerGroups.Remove(state);
						}
					}

					break;
			}
		}

		private void UpdateLocations(NotifyCollectionChangedEventArgs e)
		{
			bool FindLocationState(Location location, out LocationState state)
			{
				state = this.Locations.FirstOrDefault(l => l.Location == location);
				return state != null;
			}

			LocationState GetLocationState(Location location, bool allowAdd = true)
			{
				if (!FindLocationState(location, out LocationState result))
				{
					this.GetPeerGroupState(location.PeerGroup);

					result = new LocationState(location);
					if (allowAdd)
					{
						this.Locations.Add(result);
					}
				}

				return result;
			}

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (Location location in e.NewItems)
					{
						GetLocationState(location);
					}

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (Location location in e.OldItems)
					{
						if (FindLocationState(location, out LocationState state))
						{
							this.Locations.Remove(state);
						}
					}

					break;

				case NotifyCollectionChangedAction.Replace:
					foreach (var pair in e.OldItems.Cast<Location>().Zip(e.NewItems.Cast<Location>(), (o, n) => Tuple.Create(o, n)))
					{
						if (FindLocationState(pair.Item1, out LocationState oldState))
						{
							LocationState newState = GetLocationState(pair.Item2, allowAdd: false);
							int index = this.Locations.IndexOf(oldState);
							this.Locations[index] = newState;
						}
					}

					break;

				case NotifyCollectionChangedAction.Move:
					// We don't care about the profile's Location positions.
					break;

				default: // NotifyCollectionChangedAction.Reset
					foreach (Location location in this.profile.Locations)
					{
						GetLocationState(location);
					}

					foreach (LocationState state in this.Locations.ToList())
					{
						if (!this.profile.Locations.Contains(state.Location))
						{
							this.Locations.Remove(state);
						}
					}

					break;
			}

			lock (this.mapLock)
			{
				this.peerGroupToLocationsMap = this.Locations
				.GroupBy(l => this.GetPeerGroupState(l.Location.PeerGroup))
				.ToDictionary(group => group.Key, group => group.ToList());
			}
		}

		#endregion

		#region Private Event Handlers

		private void PeerGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => this.UpdatePeerGroups(e);

		private void LocationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => this.UpdateLocations(e);

		#endregion
	}
}

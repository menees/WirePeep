namespace WirePeep
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	#endregion

	/// <summary>
	/// A named group of <see cref="Location"/>s where connectivity to any one of them
	/// is sufficient to consider the system in a functional state.
	/// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix. Group is clearer than Collection here.
	public sealed class PeerGroup : IEnumerable<Location>
#pragma warning restore CA1710 // Identifiers should have correct suffix
	{
		#region Private Methods

		private readonly List<Location> locations;

		#endregion

		#region Constructors

		public PeerGroup(IEnumerable<Location> locations)
		{
			this.locations = new List<Location>(locations ?? Enumerable.Empty<Location>());
		}

		#endregion

		#region Public Methods

		public IEnumerator<Location> GetEnumerator() => this.locations.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		#endregion
	}
}

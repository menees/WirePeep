#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

#endregion

namespace WirePeep
{
	// TODO: Move this to Menees.Windows.Presentation. [Bill, 5/16/2020]
	internal abstract class PropertyChangeNotifier : INotifyPropertyChanged
	{
		#region Constructors

		protected PropertyChangeNotifier()
		{
		}

		#endregion

		#region Public Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Protected Methods

		protected void Update<T>(ref T member, T value, [CallerMemberName] string callerMemberName = null)
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

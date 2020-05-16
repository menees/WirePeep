#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

#endregion

namespace WirePeep
{
#pragma warning disable CA1812 // Created via reflection by MainWindow.xaml.
	internal sealed class LogRowCollection : ObservableCollection<LogRow>
#pragma warning restore CA1812
	{
		// This non-generic class allows us to do most of the log grid logic in XAML.
	}
}

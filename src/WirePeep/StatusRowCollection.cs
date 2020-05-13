#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

#endregion

namespace WirePeep
{
#pragma warning disable CA1812 // Created via reflection by MainWindow.xaml.
	internal sealed class StatusRowCollection : ObservableCollection<StatusRow>
#pragma warning restore CA1812
	{
		// This non-generic class allows us to do all the status grid grouping in XAML.
		// https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-group-sort-and-filter-data-in-the-datagrid-control
	}
}

#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

#endregion

namespace WirePeep
{
	internal static class Commands
	{
		#region Public Fields

		public static readonly RoutedUICommand AddLocation = new RoutedUICommand("Add Location", nameof(AddLocation), typeof(Commands));

		public static readonly RoutedUICommand ExportLog = new RoutedUICommand(
			"Export Log",
			nameof(ExportLog),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control), });

		public static readonly RoutedUICommand ViewOptions = new RoutedUICommand("View Options", nameof(ViewOptions), typeof(Commands));

		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			nameof(Exit),
			nameof(Exit),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.F4, ModifierKeys.Alt), });

		#endregion
	}
}

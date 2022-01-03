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

		public static readonly RoutedUICommand AddLocation = new("Add Location", nameof(AddLocation), typeof(Commands));

		public static readonly RoutedUICommand EditPeerGroups = new("Edit Peer Groups", nameof(EditPeerGroups), typeof(Commands));

		public static readonly RoutedUICommand ViewOptions = new("View Options", nameof(ViewOptions), typeof(Commands));

		public static readonly RoutedUICommand About = new(nameof(About), nameof(About), typeof(Commands));

		public static readonly RoutedUICommand SimulateConnection = new("Simulate Connection", nameof(SimulateConnection), typeof(Commands));

		public static readonly RoutedUICommand Unselect = new(nameof(Unselect), nameof(Unselect), typeof(Commands));

		public static readonly RoutedUICommand ExportLog = new(
			"Export Log",
			nameof(ExportLog),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control), });

		public static readonly RoutedUICommand Exit = new(
			nameof(Exit),
			nameof(Exit),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.F4, ModifierKeys.Alt), });

		public static readonly RoutedUICommand EditItem = new(
			"Edit Item",
			nameof(EditItem),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.F4), });

		public static readonly RoutedUICommand DeleteItem = new(
			"Delete Item",
			nameof(DeleteItem),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.Delete), });

		public static readonly RoutedUICommand CopyValue = new(
			"Copy Value",
			nameof(CopyValue),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control), });

		public static readonly RoutedUICommand CopyRow = new(
			"Copy Row",
			nameof(CopyRow),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift), });

		#endregion
	}
}

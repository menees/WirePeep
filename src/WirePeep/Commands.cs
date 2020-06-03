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

		public static readonly RoutedUICommand EditPeerGroups = new RoutedUICommand("Edit Peer Groups", nameof(EditPeerGroups), typeof(Commands));

		public static readonly RoutedUICommand ViewOptions = new RoutedUICommand("View Options", nameof(ViewOptions), typeof(Commands));

		public static readonly RoutedUICommand About = new RoutedUICommand(nameof(About), nameof(About), typeof(Commands));

		public static readonly RoutedUICommand Unselect = new RoutedUICommand(nameof(Unselect), nameof(Unselect), typeof(Commands));

		public static readonly RoutedUICommand ExportLog = new RoutedUICommand(
			"Export Log",
			nameof(ExportLog),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control), });

		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			nameof(Exit),
			nameof(Exit),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.F4, ModifierKeys.Alt), });

		public static readonly RoutedUICommand EditItem = new RoutedUICommand(
			"Edit Item",
			nameof(EditItem),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.F4), });

		public static readonly RoutedUICommand DeleteItem = new RoutedUICommand(
			"Delete Item",
			nameof(DeleteItem),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.Delete), });

		public static readonly RoutedUICommand CopyValue = new RoutedUICommand(
			"Copy Value",
			nameof(CopyValue),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control), });

		public static readonly RoutedUICommand CopyRow = new RoutedUICommand(
			"Copy Row",
			nameof(CopyRow),
			typeof(Commands),
			new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift), });

		#endregion
	}
}

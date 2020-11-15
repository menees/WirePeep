#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Menees;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	public partial class SimulateDialog : ExtendedDialog
	{
		#region Constructors

		public SimulateDialog()
		{
			this.InitializeComponent();
		}

		#endregion

		#region Public Methods

		public bool Execute(Window owner, ref ConnectionState? simulateConnection)
		{
			this.Owner = owner;

			this.none.IsChecked = simulateConnection == null;
			this.unavailable.IsChecked = simulateConnection == ConnectionState.Unavailable;
			this.connected.IsChecked = simulateConnection == ConnectionState.Connected;
			this.disconnected.IsChecked = simulateConnection == ConnectionState.Disconnected;

			bool result = false;
			if (this.ShowDialog() ?? false)
			{
				if (this.unavailable.IsChecked ?? false)
				{
					simulateConnection = ConnectionState.Unavailable;
				}
				else if (this.connected.IsChecked ?? false)
				{
					simulateConnection = ConnectionState.Connected;
				}
				else if (this.disconnected.IsChecked ?? false)
				{
					simulateConnection = ConnectionState.Disconnected;
				}
				else
				{
					simulateConnection = null;
				}

				result = true;
			}

			return result;
		}

		#endregion
	}
}

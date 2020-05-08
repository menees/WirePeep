#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class Options
	{
		#region Constructors

		public Options(ISettingsNode settingsNode)
		{
			// TODO: Try OneDrive/Documents, Documents, TempFile. [Bill, 5/7/2020]
		}

		#endregion

		#region Public Properties

		public string LogFolder { get; private set; }

		#endregion
	}
}

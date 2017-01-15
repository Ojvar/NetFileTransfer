using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Helper.Network.Command
{
	/// <summary>
	/// Command Types
	/// </summary>
	public enum EnumCommandType
	{
		Text				= 1,

		SendFileRequest		= 2,
		ReceiveFileRequest	= 3,
		FileData			= 4,

		Ready				= 100,
		Cancel				= 101,
		Finish				= 102,
		Ok					= 103,
		Error				= 110
	}
}

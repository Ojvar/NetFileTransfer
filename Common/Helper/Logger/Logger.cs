using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Common.Helper.Logger
{
	public class Logger
	{
		public static void log (string data)
		{
			File.AppendAllText ("D:\\log.txt", DateTime.Now.ToString ("yyy/MM/dd HH:mm:SS") + "\t\t" + data);
		}
	}
}

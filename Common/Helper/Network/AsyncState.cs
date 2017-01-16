using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Common.Helper.Network
{
	public class AsyncState
	{
		public TcpClient	client;
		public byte[]		buffer;
		public int			dataSize;
	}
}

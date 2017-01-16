using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Common.Helper.Network
{
	public class FileServer
	{
		public bool	connected;
		public TcpListener	server;


		public FileServer (int port)
		{
			server	= new TcpListener (port);
		}
		public void start ()
		{
			if (!connected)
			{
				server.Start ();
				connected	= true;

				beginAccept ();
			}
		}

		public void stop ()
		{
			if (connected)
			{
				server.Stop ();
			}
		}

		private void beginAccept ()
		{
			if (connected)
				server.BeginAcceptTcpClient (acceptCallback, server);
		}

		private void acceptCallback (IAsyncResult ar)
		{
			try
			{
				TcpClient	newClient	= server.EndAcceptTcpClient (ar);

				if (null != newClient)
				{
					FileServerClient client	= new FileServerClient (newClient);
					client.beginRead ();
				}

				beginAccept ();
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}
}

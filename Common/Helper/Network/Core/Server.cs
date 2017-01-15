using Common.Helper.Network.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common.Helper.Network.Core
{
	public class Server
	{

		#region Delegates
		public delegate void Started (Server sender);
		public delegate void Stopped (Server sender);
		public delegate void AcceptClient (Server sender, Client client);
		public delegate void ClientDataRecieved (Server sender, Client client, NetPacket packet);
		#endregion

		#region Events
		public event Started			onStart;
		public event Stopped			onStop;
		public event AcceptClient		onAcceptClient;
		public event ClientDataRecieved	onClientDataReceived;
		#endregion

		#region Constants
		#endregion

		#region Variables
		private TcpListener		server;
		private object			acceptObj;
		private IAsyncResult	acceptClientAsyncIResult;
		#endregion

		#region Properties
		/// <summary>
		/// Port
		/// </summary>
		public int port
		{
			get;
			private set;
		}
		#endregion

		#region Methods
		/// <summary>
		/// Ctr
		/// </summary>
		public Server (int port)
		{
			this.port	= port;
		}

		/// <summary>
		/// Start
		/// </summary>
		public void start ()
		{
			stop (false);
			this.server	= new TcpListener (IPAddress.Any, port);
			this.server.Start ();

			onStart?.Invoke (this);

			beginAcceptClient ();
		}


		/// <summary>
		/// Stop
		/// </summary>
		public void stop (bool emitEvent)
		{
			if (null != this.server)
				this.server.Stop ();
			this.server	= null;

			if (emitEvent)
				onStop?.Invoke (this);
		}

		/// <summary>
		/// Accept client
		/// </summary>
		private void beginAcceptClient ()
		{
			if (null != this.server)
				acceptClientAsyncIResult = this.server.BeginAcceptTcpClient (acceptCallback, acceptObj);
		}

		/// <summary>
		/// Accept client
		/// </summary>
		/// <param name="ar"></param>
		private void acceptCallback (IAsyncResult ar)
		{
			if (null != this.server)
			{
				//try
				//{
					TcpClient client = this.server.EndAcceptTcpClient (ar);

					if (null != client)
					{
						Client	netClient	= new Client (client);
						netClient.connect (false);
						netClient.beginRead ();
						netClient.onDataReceived	+= (s, d) =>
						{
							onClientDataReceived?.Invoke (this, s, d);
						};
						beginAcceptClient ();
					}
				//}
				//catch (ObjectDisposedException)
				//{
				//}
				//catch (Exception ex)
				//{
				//	throw ex;
				//}
			}
		}
		#endregion
	}
}

using Common.Helper.Network.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Common.Helper.Network.Core
{
	/// <summary>
	/// Net Client
	/// </summary>
	public class Client
	{
		#region Delegates
		public delegate void Connected (Client sender);
		public delegate void Disconnected (Client sender);
		public delegate void DataReceived (Client sender, NetPacket packet);
		public delegate void DataSend (Client sender, NetPacket packet);
		public delegate void Error (Client sender, Exception error);
		public delegate void HintMessage (Client sender, object data);
		#endregion

		#region Events
		public event Connected		onConnect;
		public event Disconnected	onDisconnect;
		public event DataReceived	onDataReceived;
		public event DataReceived	onDataSend;
		public event Error			onError;
		public event HintMessage	onHintMessage;
		#endregion

		#region Constants
		public const int			C_ReadBufferSize	= 5242880;	// 5MB
		#endregion

		#region Variables
		private		ManualResetEvent readBufferResetEvent;
		private		ManualResetEvent writeBufferResetEvent;

		protected	TcpClient		client;
		private		object			connectOjb;
		private		NetworkStream	clientStream;

		private		MemoryStream	readBufferStream;
		private		BinaryReader	readBufferReader;

		private		byte[]			readBuffer;
		private		object			readObj;
		private		IAsyncResult	readAsyncIResult;
		private		IAsyncResult	writeAsyncIResult;

		private		List<NetPacket>	outputList;
		private		NetPacket		writeCurrentObject;
		private		object			writeObj;
		#endregion

		#region Properties
		public String host
		{
			get;
			private set;
		}

		public int port
		{
			get;
			private set;
		}
		#endregion

		#region Methods
		#region Constructor
		/// <summary>
		/// Ctr
		/// </summary>
		public Client (string host, int port)
		{
			this.host   = host;
			this.port   = port;
		}

		/// <summary>
		/// Ctr
		/// </summary>
		public Client (TcpClient client)
		{
			if (null == client)
				throw new NullReferenceException ("Client is null");

			this.client	= client;
			IPEndPoint ipEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
			this.host   = ipEndPoint.Address.ToString ();
			this.port   = ipEndPoint.Port;

			connect (false);
		}
		#endregion

		#region Connect
		/// <summary>
		/// Connect
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public void connect (string host, int port)
		{
			this.host	= host;
			this.port	= port;

			connect (true);
		}

		/// <summary>
		/// Connect
		/// </summary>
		public void connect (bool newConnection)
		{
			if (newConnection)
			{
				disconnect (false);
				client	= new TcpClient ();
			}
			else
				clientStream			= client.GetStream ();

			readBuffer				= new byte[C_ReadBufferSize];
			readBufferResetEvent	= new ManualResetEvent (true);
			writeBufferResetEvent	= new ManualResetEvent (true);
			outputList				= new List<NetPacket> ();
			readBufferStream		= new MemoryStream ();
			readBufferReader		= new BinaryReader (readBufferStream);

			if (newConnection)
				client.BeginConnect (IPAddress.Parse (host), port, connectCallback, connectOjb);
		}

		/// <summary>
		/// Connect
		/// </summary>
		/// <param name="ar"></param>
		private void connectCallback (IAsyncResult ar)
		{
			//try
			//{
				if (null != client)
				{
					//client.EndConnect (ar);
					clientStream	= client.GetStream ();

					onConnect?.Invoke (this);

					beginRead ();
				}
			//}
			//catch (Exception ex)
			//{
			//	onError?.Invoke (this, ex);
			//}
		}
		#endregion

		#region Disconnect
		/// <summary>
		/// Disconnect
		/// </summary>
		public void disconnect (bool emmitEvent)
		{
			stopReading ();
			stopWriting ();

			readBufferReader?.Close ();
			readBufferStream?.Dispose ();
			readBufferReader	= null;
			readBufferStream	= null;
			readBuffer			= null;

			if (true == client?.Client?.Connected)
				client?.Client?.Disconnect (false);
			client	= null;

			if (emmitEvent)
				onDisconnect?.Invoke (this);
		}

		#endregion

		#region Read
		public void stopReading ()
		{
			if (null != readAsyncIResult)
				clientStream?.Close ();
		}

		/// <summary>
		/// Begin read
		/// </summary>
		public void beginRead ()
		{
			if (null != clientStream)
				readAsyncIResult	= clientStream.BeginRead (readBuffer, 0, C_ReadBufferSize, readCallback, readObj);
		}

		/// <summary>
		/// Read callback
		/// </summary>
		/// <param name="ar"></param>
		private void readCallback (IAsyncResult ar)
		{
			if (true == clientStream?.CanRead)
			{
				PropertyInfo pi = clientStream?.GetType ().GetProperty ("Socket", BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance);
				Socket	socket	= pi.GetValue (clientStream, null) as Socket;

				if ((null == socket) || !socket.Connected)
					return;

				int	size	= clientStream.EndRead (ar);

				if ((size > 0))
				{
					// Lock Read operation
					readBufferResetEvent.WaitOne ();
					readBufferResetEvent.Reset ();

					byte[]	newData	= new byte[size];

					Array.Copy (readBuffer, newData, size);
					appendToReadBuffer (newData);

					// Lock Read operation
					readBufferResetEvent.Set ();
				}

				beginRead ();
			}
		}

		/// <summary>
		/// Append to read buffer
		/// </summary>
		/// <param name="readBuffer"></param>
		private void appendToReadBuffer (byte[] readBuffer)
		{
			if (null != readBuffer)
			{
				readBufferStream.Position	= readBufferStream.Length;
				readBufferStream?.Write (readBuffer, 0, readBuffer.Length);
				checkReadBuffer ();
			}
		}

		/// <summary>
		/// Check read buffer
		/// </summary>
		private void checkReadBuffer ()
		{
			if (readBufferStream?.Length > 24)	// Header + Token + Type + DataSize
			{
				//try
				//{
					readBufferStream.Position	= 0;

					if (readBufferReader.ReadChar () == NetPacket.C_PacketHeader)
					{
						// Ignore Token + CommandType
						readBufferReader.ReadBytes (NetPacket.C_TokenLen + NetPacket.C_TypeLen);

						// Get Data size
						int	dataSize = readBufferReader.ReadInt32 ();

						// If have enoght bytes in buffer
						if (readBufferStream.Length >= dataSize + NetPacket.C_HeaderTotalLen)
						{
							#region Extract packet
							byte[]	packData	= new byte[dataSize + NetPacket.C_HeaderTotalLen];
							readBufferStream.Position	= 0;
							readBufferStream.Read (packData, 0, packData.Length);

							onDataReceived?.Invoke (this, NetPacket.toPacket (packData));
							#endregion

							#region Remove from buffer & refresh buffer
							byte[]	data	= readBufferStream.ToArray ().Skip (packData.Length).ToArray ();

							// Dispose old-data
							readBufferReader.Close ();
							readBufferStream.Dispose ();

							// Write new data
							readBufferStream	= new MemoryStream ();
							readBufferReader	= new BinaryReader (readBufferStream); 
							readBufferStream.Write (data, 0, data.Length);
							#endregion
						}
					}
				//}
				//catch (Exception ex)
				//{
				//}
			}
		}
		#endregion

		#region Write
		/// <summary>
		/// Stop Writing
		/// </summary>
		private void stopWriting ()
		{
			writeBufferResetEvent?.WaitOne ();

			outputList?.Clear ();
			try
			{
				if (null != writeAsyncIResult)
					clientStream?.EndWrite (writeAsyncIResult);
			}
			catch (ObjectDisposedException ex)
			{
			}
}

		/// <summary>
		/// Write data
		/// </summary>
		/// <param name="packet"></param>
		public void write (NetPacket packet)
		{
			if (null != packet)
			{
				writeBufferResetEvent.WaitOne ();
				outputList.Add (packet);
				beginWrite ();
			}
		}

		/// <summary>
		/// Begin Write
		/// </summary>
		private void beginWrite ()
		{
			if (0 < outputList.Count)
			{
				// Unlock write event
				writeBufferResetEvent.WaitOne ();

				// Lock write event
				writeBufferResetEvent.Reset ();

				writeCurrentObject	= outputList[0];
				byte[] data	= writeCurrentObject.toBytes ();

				if (true == clientStream?.CanWrite)
					writeAsyncIResult	= clientStream?.BeginWrite (data, 0, data.Length, writeCallback, writeObj);

				// Remove from list
				outputList.Remove (writeCurrentObject);
			}
		}

		/// <summary>
		/// Begin write data
		/// </summary>
		/// <param name="ar"></param>
		private void writeCallback (IAsyncResult ar)
		{
			clientStream?.EndWrite (ar);
			writeBufferResetEvent.Set ();
			onDataSend?.Invoke (this, writeCurrentObject);
		}
		#endregion
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common.Helper.Network
{
	public class FileClient
	{
		/*
			HEADER FLAGS
					O		OK
					F		FAIL
					B		BEGIN (SEND)
					R		BEGIN (RECEIVE)
					E		END
					I		RECEIVED FILE LEN
					D		DATA 
		*/

		public delegate	void ProgressHandler (FileClient sender, long position, long total);
		public delegate	void StartTransferHandler (FileClient sender, long total);
		public delegate	void FinishTransferHandler (FileClient sender);
		public delegate	void TransferErrorHandler (FileClient sender);

		public event ProgressHandler		onProgress;
		public event StartTransferHandler	onStartTransfer;
		public event FinishTransferHandler	onFinishTransfer;
		public event TransferErrorHandler	onTransferError;

		public int				C_BUFFER_SIZE		= Constants.C_BUFFER_SIZE;
		public int				C_WORKMODE_NONE		= 0;
		public int				C_WORKMODE_SEND		= 1;
		public int				C_WORKMODE_RECEIVE	= 2;

		public int				workingMode;
		public long				receivedFileLen		= 0;

		public TcpClient		client;
		public NetworkStream	stream;
		public FileStream		fileStream;

		public FileClient ()
		{
			client	= new TcpClient ();
			workingMode	= C_WORKMODE_NONE;
		}

		public void connect (string host, int port)
		{
			client.Connect (new IPEndPoint (IPAddress.Parse (host), port));
			stream	= client.GetStream ();
			beginRead ();
		}
		public void disconnect ()
		{
			stream.Close ();
			stream.Dispose ();
			client?.Client.Disconnect (false);
			client?.Close ();
		}

		public void beginRead ()
		{
			if (stream?.CanRead == true)
			{
				AsyncState	state	= new Network.AsyncState ();
				state.client	= client;
				state.buffer		= new byte[C_BUFFER_SIZE];

				stream.BeginRead (state.buffer, 0, state.buffer.Length, readCallback, state);
			}
		}

		private void readCallback (IAsyncResult ar)
		{
			AsyncState	state	= ar.AsyncState as AsyncState;

			stream.EndRead (ar);
			processData (state);

			beginRead ();
		}

		public void write (byte[] data)
		{
			if (stream?.CanWrite == true)
				stream.Write (data, 0, data.Length);
		}

		/// <summary>
		/// Process data
		/// </summary>
		/// <param name="state"></param>
		private void processData (AsyncState state)
		{
			if (state.buffer[0] == (byte)'O')
			{
				if (workingMode == C_WORKMODE_SEND)
					doSendOperation ();
			}
			else if (state.buffer[0] == (byte)'I')
			{
				if (workingMode == C_WORKMODE_RECEIVE)
					receivedFileLen	= BitConverter.ToInt32 (state.buffer, 1);
				write (new byte[] { (byte)'O' });
			}
			else if (state.buffer[0] == (byte)'D')
			{
				if (workingMode == C_WORKMODE_RECEIVE)
					doReceieveOperation (state);
			}
			else if (state.buffer[0] == (byte)'F')
			{
				fileStream?.Dispose ();
				onTransferError?.Invoke (this);

				workingMode	= C_WORKMODE_NONE;
			}
			else if (state.buffer[0] == (byte)'E')
			{
				fileStream?.Flush ();
				fileStream?.Dispose ();
				onFinishTransfer?.Invoke (this);

				workingMode	= C_WORKMODE_NONE;
			}
		}

		#region Receive
		private void doReceieveOperation (AsyncState state)
		{
			int		len		= BitConverter.ToInt32 (state.buffer, 1);

			if (len > 0)
			{
				byte[]	data	= new byte[len];
				Array.Copy (state.buffer, 5, data, 0, len);

				fileStream.Write (data, 0, data.Length);
			}

			onProgress?.Invoke (this, fileStream.Length, receivedFileLen);

			write (new byte[] { (byte)'O' });
		}

		public void receiveFile (string filename, string newFileName)
		{
			if (workingMode != C_WORKMODE_NONE)
				return;

			byte[] filenameData = Encoding.UTF8.GetBytes (filename);
			byte[] fileInfoData;
			MemoryStream ms = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (ms);

			bw.Write ('R');                         // Header
			bw.Write ((Int32)filenameData.Length);
			bw.Write (filenameData);

			fileInfoData    = ms.ToArray ();
			bw.Close ();
			ms.Close ();

			// Open file
			if (File.Exists (newFileName))
				File.Delete (newFileName);

			fileStream  = File.Open (newFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);

			// Write to server
			write (fileInfoData);

			workingMode = C_WORKMODE_RECEIVE;
			onStartTransfer?.Invoke (this, fileStream.Length);
		} 
		#endregion

		#region Send
		private void doSendOperation ()
		{
			if (null != fileStream)
			{
				System.Threading.Thread.Sleep (25);

				byte[] data;
				byte[] fileData = new byte[C_BUFFER_SIZE-5];        // flag + len

				int size = fileStream.Read (fileData, 0, fileData.Length);

				if (size > 0)
				{
					data    = null;

					#region Prepare data
					MemoryStream ms = new MemoryStream ();
					BinaryWriter bw = new BinaryWriter (ms);

					bw.Write ((byte)'D');
					bw.Write ((Int32)size);
					bw.Write (fileData);

					data    = ms.ToArray ();
					bw.Close ();
					ms.Dispose (); 
					#endregion

					write (data);

					onProgress?.Invoke (this, fileStream.Position, fileStream.Length);
				}
				else
				{
					onFinishTransfer?.Invoke (this);

					fileStream.Close ();
					write (new byte[] { (byte)'E' });
					workingMode = C_WORKMODE_NONE;
				}
			}
			else
			{
				write (new byte[] { (byte)'F' });
				workingMode	= C_WORKMODE_NONE;
			}
		}

		public void sendFile (string filename)
		{
			if (workingMode != C_WORKMODE_NONE)
				return;

			if (!File.Exists (filename))
				return;

			FileInfo fileInfo = new FileInfo (filename);
			byte[] fileInfoData;
			MemoryStream ms = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (ms);

			byte[] filenameData = Encoding.UTF8.GetBytes (fileInfo.Name);

			bw.Write ('S');                         // Header
			bw.Write ((Int32)fileInfo.Length);
			bw.Write ((Int32)filenameData.Length);
			bw.Write (filenameData);

			fileInfoData    = ms.ToArray ();
			bw.Close ();
			ms.Close ();

			// Open file
			fileStream  = File.Open (filename, FileMode.Open, FileAccess.Read, FileShare.Read);

			// Write to server
			write (fileInfoData);

			workingMode = C_WORKMODE_SEND;
			onStartTransfer?.Invoke (this, fileStream.Length);
		} 
		#endregion
	}
}

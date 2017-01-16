using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Common.Helper.Network
{
	public class FileServerClient
	{
		public delegate	void ProgressHandler (FileServerClient sender, long position, long total);
		public delegate	void StartTransferHandler (FileServerClient sender, long total);
		public delegate	void FinishTransferHandler (FileServerClient sender);
		public delegate	void TransferErrorHandler (FileServerClient sender);

		public event ProgressHandler		onProgress;
		public event StartTransferHandler	onStartTransfer;
		public event FinishTransferHandler	onFinishTransfer;
		public event TransferErrorHandler	onTransferError;

		public int				C_BUFFER_SIZE	= Constants.C_BUFFER_SIZE;

		public int				C_WORKMODE_NONE		= 0;
		public int				C_WORKMODE_SEND		= 1;
		public int				C_WORKMODE_RECEIVE	= 2;

		public int				workingMode;
		public string			basePath			= "D:\\received\\";
		public FileStream		filestream;
		public TcpClient		client;
		public NetworkStream	stream;

		public FileServerClient (TcpClient client)
		{
			this.client	= client;
			this.stream	= client.GetStream ();
			workingMode	= C_WORKMODE_NONE;
		}

		public void disconnect ()
		{
			stream.Close ();
			stream.Dispose ();

			try
			{
				client?.Client?.Disconnect (false);
			}
			catch
			{
			}
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
			try
			{
				AsyncState	state	= ar.AsyncState as AsyncState;

				state.dataSize	= stream.EndRead (ar);
				processData (state);

				beginRead ();
			}
			catch (IOException)
			{
				disconnect ();
			}
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
			byte[]	 data	= new byte[state.dataSize];

			Array.Copy (state.buffer, data, data.Length);

			if (data[0] == 'O')
			{
				if (workingMode == C_WORKMODE_SEND)
					doSendFile (filestream);
			}
			else if (data[0] == 'R')
			{
				#region Send Data
				int		fileNameSize	= BitConverter.ToInt32 (data, 1);
				string	fileName		= "";

				#region Get File name
				byte[] filenameData = new byte[fileNameSize];
				Array.Copy (data, 5, filenameData, 0, fileNameSize);
				fileName    = Encoding.UTF8.GetString (filenameData); 
				#endregion

				if (!File.Exists (basePath + fileName))
				{
					write (new byte[] { (byte)'F' });
					workingMode	= C_WORKMODE_NONE;
				}
				else
					filestream  = File.Open (basePath + fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

				workingMode	= C_WORKMODE_SEND;

				#region Write file length
				data	= new byte[9];
				data[0]	= (byte)'I';
				Array.Copy (BitConverter.GetBytes ((Int64)filestream.Length), 0, data, 1, 8);

				write (data);
				#endregion

				#endregion
			}
			else if (data[0] == 'S')
			{
				#region Recieve Data
				if (workingMode != C_WORKMODE_NONE)
				{
					write (new byte[] { (byte)'F' });
					return;
				}

				int		fileSize		= BitConverter.ToInt32 (data, 1);
				int		fileNameSize	= BitConverter.ToInt32 (data, 5);
				string	fileName		= "";

				byte[] filenameData = new byte[fileNameSize];
				Array.Copy (data, 9, filenameData, 0, fileNameSize);
				fileName    = Encoding.UTF8.GetString (filenameData);

				if (File.Exists (fileName))
					File.Delete (fileName);
				filestream  = File.Open (fileName, FileMode.CreateNew);

				write (new byte[] { (byte)'O' });
				workingMode = C_WORKMODE_RECEIVE; 
				#endregion
			}
			else if (data[0] == 'E')
			{
				#region Finish Recieving
				filestream.Close ();
				filestream.Dispose ();
				filestream  = null;

				workingMode = C_WORKMODE_NONE; 
				#endregion
			}
			else if (data[0] == 'D')
			{
				#region Write Data
				int		len			= BitConverter.ToInt32 (data, 1);
				byte[]	fileData	= new byte[len];
				
				if (len > data.Length-5)
					len	= data.Length-5;

				Array.Copy (data, 5, fileData, 0, len);

				filestream.Write (fileData, 0, fileData.Length);

				write (new byte[] { (byte)'O' });
				#endregion
			}
			else if (data[0] == 'F')
			{
				#region Fail
				filestream?.Dispose ();
				workingMode	= C_WORKMODE_NONE;
				#endregion
			}
		}

		#region Send file
		/// <summary>
		/// Send file 
		/// </summary>
		/// <param name="filestream"></param>
		private void doSendFile (FileStream fileStream)
		{
			if (null != fileStream)
			{
				byte[] data;
				byte[] fileData = new byte[C_BUFFER_SIZE-5];        // 1 = header size

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
				workingMode = C_WORKMODE_NONE;
			}
		} 
		#endregion
	}
}

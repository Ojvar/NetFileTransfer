using Common.Helper.Network.Core;
using Common.Helper.Network.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Common.Helper.Network.Command
{
	/// <summary>
	/// Send Message Command
	/// </summary>
	public class SendFileCommand : BaseCommand
	{

		#region Delegates
		#endregion

		#region Events
		#endregion

		#region Constants
		public const int C_BufferSize	= 40860;
		#endregion

		#region Variables
		private string		filename;
		private FileStream	fileStream;
		#endregion

		#region Properties
		#endregion

		#region Methods
		/// <summary>
		/// Ctr
		/// </summary>
		public SendFileCommand (Client client, string filename) 
			: base (client, EnumCommandType.SendFileRequest)
		{
			if (!File.Exists (filename))
				throw new Exception ("File not found");
			else
			{
				try
				{
					this.filename = filename;

					fileStream	= File.Open (filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				}
				catch (Exception ex)
				{
					hintErrorEvent (ex);
					finish ();
					throw ex;
				}
			}
		}

		/// <summary>
		/// Start
		/// </summary>
		public override void start ()
		{
			base.start ();

			//client.onDataReceived	+= receiveData;

			NetPacket packet	= new NetPacket (token, EnumCommandType.SendFileRequest);
			packet.data	= NetPacket.getBytesUtf8 (Path.GetFileName (filename));
			this.client.write (packet);
		}
		
		/// <summary>
		/// On Data Recieve
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="packet"></param>
		protected override void receiveData (Client sender, NetPacket packet)
		{
			if (packet?.token == token)
				doProcess (packet);
		}

		/// <summary>
		/// Do Process
		/// </summary>
		/// <param name="packet"></param>
		protected override void doProcess (NetPacket packet)
		{
			base.doProcess (packet);

			switch (packet.type)
			{
				case EnumCommandType.Ok:
					sendData (packet);
					break;

				case EnumCommandType.Finish:
					client.onDataReceived	-= receiveData;
					finish ();
					break;

				default: 
					break;
			}
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <param name="packet"></param>
		private void sendData (NetPacket packet)
		{
			//System.Threading.Thread.Sleep (250);

			if (true == fileStream?.CanRead)
			{
				byte[]	data = new byte[C_BufferSize];

				int	size	= fileStream.Read (data, 0, data.Length);

				if (size == 0)
					finish ();
				else
				{
					// Purge buffer
					packet.data	= new byte[size];
					Array.Copy (data, packet.data, size);

					packet.type		= EnumCommandType.FileData;
					writePacket (packet);

					hintDoProgressEvent (string.Format ("Data sent {0} of {1}", fileStream.Position, fileStream.Length));
				}
			}
			else
				finish ();
		}

		/// <summary>
		/// Finish
		/// </summary>
		public override void finish ()
		{
			fileStream?.Close ();
			fileStream?.Dispose ();
			fileStream	= null;
			
			NetPacket	packet	= new NetPacket (this.token, EnumCommandType.Finish);
			writePacket (packet);

			base.finish ();
		}
		#endregion
	}
}

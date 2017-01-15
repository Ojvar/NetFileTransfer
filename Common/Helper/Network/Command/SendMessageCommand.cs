using Common.Helper.Network.Core;
using Common.Helper.Network.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Helper.Network.Command
{
	/// <summary>
	/// Send Message Command
	/// </summary>
	public class SendMessageCommand : BaseCommand
	{

		#region Delegates
		#endregion

		#region Events
		#endregion

		#region Constants
		#endregion

		#region Variables
		private string message;
		#endregion

		#region Properties
		#endregion

		#region Methods
		/// <summary>
		/// Ctr
		/// </summary>
		public SendMessageCommand (Client client, string message) 
			: base (client, EnumCommandType.Text)
		{
			this.message = message;
		}

		/// <summary>
		/// Start
		/// </summary>
		public override void start ()
		{
			base.start ();

			NetPacket packet	= new NetPacket (token, type);
			packet.data	= NetPacket.getBytesUtf8 (message);
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
					client.onDataReceived	-= receiveData;
					finish ();
					break;
			}
		}
		#endregion
	}
}

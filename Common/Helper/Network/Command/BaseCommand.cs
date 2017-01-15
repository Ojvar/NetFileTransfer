using Common.Helper.Network.Core;
using Common.Helper.Network.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Helper.Network.Command
{
	/// <summary>
	/// Base Command Class
	/// </summary>
	public class BaseCommand
	{
		#region Delegates
		public delegate void Start (BaseCommand cmd);
		public delegate void Finish (BaseCommand cmd);
		public delegate void Error (BaseCommand cmd, Exception error);
		public delegate void Progress (BaseCommand cmd, object data);
		public delegate void RecieveData (BaseCommand cmd, NetPacket data);
		public delegate void SendData (BaseCommand cmd, NetPacket data);
		#endregion

		#region Events
		public event Start			onStart;
		public event Finish			onFinish;
		public event Error			onError;
		public event Progress		onProgress;
		public event RecieveData	onRecieveData;
		public event SendData		onSendData;
		#endregion

		#region Constants
		#endregion

		#region Variables
		protected EnumCommandType	type;
		protected Client			client;
		#endregion

		#region Properties
		public Guid token
		{
			get;
			private set;
		}

		public object parent
		{
			get;
			private set;
		}
		#endregion

		#region Methods
		/// <summary>
		/// Ctr
		/// </summary>
		public BaseCommand (Client client, EnumCommandType type, Guid token)
		{
			if (null == client)
				throw new NullReferenceException ("client is null");

			this.token	= token;
			this.client	= client;
			this.type	= type;

			init ();
		}
		/// <summary>
		/// Ctr
		/// </summary>
		public BaseCommand (Client client, EnumCommandType type)
		{
			if (null == client)
				throw new NullReferenceException ("client is null");

			this.token	= Guid.NewGuid ();
			this.client	= client;
			this.type	= type;

			init ();
		}

		/// <summary>
		/// Init
		/// </summary>
		private void init ()
		{
			client.onDataReceived	+= receiveData;
		}

		#region Virtual Methods
		/// <summary>
		/// Start
		/// </summary>
		public virtual void start ()
		{
			hintOnStartEvent (null);
		}

		/// <summary>
		/// Finish
		/// </summary>
		public virtual void finish ()
		{
			hintOnFinishEvent (null);
		}

		/// <summary>
		/// doProcess
		/// </summary>
		protected virtual void doProcess (NetPacket packet)
		{
			hintDoProcessEvent (null);
		}

		
		/// <summary>
		/// Recieve data
		/// </summary>
		protected virtual void receiveData (Client sender, NetPacket packet)
		{
			hintReceiveDataEvent (packet);
		}
		#endregion

		#region Events

		private void hintOnStartEvent (object p)
		{
			onStart?.Invoke (this);
		}

		private void hintOnFinishEvent (object p)
		{
			onFinish?.Invoke (this);
		}

		private void hintDoProcessEvent (object p)
		{
			onStart?.Invoke (this);
		}

		private void hintReceiveDataEvent (NetPacket packet)
		{
			onRecieveData?.Invoke (this, packet);
		}

		private void hintSendDataEvent (NetPacket packet)
		{
			onSendData?.Invoke (this, packet);
		}

		protected void hintErrorEvent (Exception ex)
		{
			onError?.Invoke (this, ex);
		}

		protected void hintDoProgressEvent (object data)
		{
			onProgress?.Invoke (this, data);
		}
		#endregion

		/// <summary>
		/// Write packet
		/// </summary>
		/// <param name="packet"></param>
		protected virtual void writePacket (NetPacket packet)
		{
			if (null != packet)
			{
				client?.write (packet);
				hintSendDataEvent (packet);
			}
		}
		#endregion
	}
}

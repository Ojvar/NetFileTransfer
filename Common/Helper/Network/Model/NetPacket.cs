using Common.Helper.Network.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Common.Helper.Network.Model
{
	/// <summary>
	/// NetPacket Class
	/// </summary>
	public class NetPacket
	{

		#region Delegates
		#endregion

		#region Methods
		#endregion

		#region Constants
		public const char	C_PacketHeader		= (char)1;
		public const int	C_HeaderLen			= 1;
		public const int	C_TokenLen			= 16;
		public const int	C_TypeLen			= 4;
		public const int	C_DataSizeLen		= 4;
		public const int	C_HeaderTotalLen	= C_HeaderLen + C_TokenLen + C_TypeLen + C_DataSizeLen;
		#endregion

		#region Variables
		#endregion

		#region Properties
		public byte[] data
		{
			get;
			set;
		}
		public Guid token
		{
			get;
			set;
		}
		public Command.EnumCommandType type
		{
			get;
			set;
		}
		#endregion

		#region Methods
		/// <summary>
		/// Ctr
		/// </summary>
		public NetPacket (Guid token, EnumCommandType type)
		{
			this.type	= type;
			this.token	= token;

			init ();
		}

		/// <summary>
		/// Ctr
		/// </summary>
		public NetPacket (EnumCommandType type)
		{
			this.type	= type;
			this.token	= Guid.NewGuid ();

			init ();
		}

		/// <summary>
		/// Initialize
		/// </summary>
		private void init ()
		{
		}

		/// <summary>
		/// To Bytes array
		/// </summary>
		/// <returns></returns>
		public byte[] toBytes ()
		{
			return toBytes (this);
		}


		/// <summary>
		/// To Bytes array
		/// </summary>
		/// <param name="netPacket"></param>
		/// <returns></returns>
		public static byte[] toBytes (NetPacket netPacket)
		{
			byte[] result	= null;

			if (null == netPacket)
				throw new NullReferenceException ();
			else
			{
				MemoryStream	ms	= new MemoryStream ();
				BinaryWriter	bw	= new BinaryWriter (ms);

				#region Write data
				bw.Write (C_PacketHeader);
				bw.Write ((Int32)netPacket.type);
				bw.Write (netPacket.token.ToByteArray ());

				if (null != netPacket.data)
				{
					bw.Write (netPacket.data.Length);
					bw.Write (netPacket.data);
				}
				else
					bw.Write (0); 
				#endregion

				result	= ms.ToArray ();

				bw.Close ();
				ms.Close ();
			}

			return  result;
		}

		
		/// <summary>
		/// To Bytes array
		/// </summary>
		/// <param name="netPacket"></param>
		/// <returns></returns>
		public static NetPacket  toPacket (byte[] packetData)
		{
			NetPacket result	= null;

			if (null == packetData)
				throw new NullReferenceException ();
			else
			{
				MemoryStream	ms	= new MemoryStream (packetData);
				BinaryReader	br	= new BinaryReader (ms);

				#region Read data
				if (br.ReadChar () == C_PacketHeader)
				{
					EnumCommandType	type		= (EnumCommandType) br.ReadInt32 ();
					Guid			token		= new Guid (br.ReadBytes (C_TokenLen));
					int				dataSize	= br.ReadInt32 ();
					byte[]			data		= null;

					if (dataSize > 0)
						data	= br.ReadBytes (dataSize);

					result = new NetPacket (token, type);
					result.data	= data;
				}
				else
					throw new Exception ("Wrong data");
				#endregion

				br.Close ();
				ms.Close ();
			}

			return  result;
		}

		/// <summary>
		/// Convert string to bytes
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static byte[] getBytesUtf8 (string text)
		{
			return Encoding.UTF8.GetBytes (text??"");
		}

		/// <summary>
		/// Convert string to bytes
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string getStringUtf8 (byte[] data)
		{
			string	result	= "";

			if (data != null)
				result	= Encoding.UTF8.GetString (data);

			return result;
		}
		#endregion
	}
}

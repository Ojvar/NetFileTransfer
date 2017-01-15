using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Common.Helper.Network.Model;
using Common.Helper.Network.Command;

namespace TestUnitProject
{
	[TestClass]
	public class PacketTest
	{
		[TestMethod]
		public void TestPackUnPack ()
		{
			string	sStr	= "Hello to all and to world";
			NetPacket	packet	= new NetPacket (EnumCommandType.Ok);

			packet.data	= Encoding.UTF8.GetBytes (sStr);

			byte[]	packedData	= packet.toBytes ();
			NetPacket	packet2	= NetPacket.toPacket (packedData);

			String t = Encoding.UTF8.GetString (packet2.data);

			Assert.AreEqual (t, sStr);
		}
	}
}

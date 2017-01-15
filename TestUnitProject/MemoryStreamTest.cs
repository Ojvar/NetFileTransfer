using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestUnitProject
{
	[TestClass]
	public class MemoryStreamTest
	{
		[TestMethod]
		public void testMemoryStreamDataRemove ()
		{
			byte[]	data	= new byte[10];
			MemoryStream	ms	= new MemoryStream ();
			BinaryWriter	bw	= new BinaryWriter (ms);

			ms.Write (data, 0, data.Length);
			bw.Write ("Hello to U, this is a test");
			bw.Close ();

			data	= ms.ToArray ();

			Assert.IsTrue (data.Length > 10);
		}
	}
}

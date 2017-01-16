using Common.Helper.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FileTransfer
{
	public partial class ServerClientForm : Form
	{
		public ServerClientForm ()
		{
			InitializeComponent ();
		}

		FileServer	server;
		FileClient	client;

		private void button1_Click (object sender, EventArgs e)
		{
			server	= new FileServer (11365);
			server.start ();
		}

		private void button2_Click (object sender, EventArgs e)
		{
			server.stop ();
		}


		private void button3_Click (object sender, EventArgs e)
		{
			client	= new FileClient ();
			client.connect ("127.0.0.1", 11365);
			client.onFinishTransfer	+= (s) => { hint (textBox1, "finished"); };
			client.onStartTransfer	+= (s, l) => { hint (textBox1, "started , 0 : " + l); };
			client.onProgress		+= (s, p, l) => { hint (textBox1, "Transfer " + p + " : " + l); };
		}

		private void button4_Click (object sender, EventArgs e)
		{
			client.disconnect ();
		}

		private void button5_Click (object sender, EventArgs e)
		{
			client.sendFile (textBox2.Text);
		}

		public void hint (TextBox t, string data)
		{
			Invoke ((Action) delegate
			{
				t.Text	= data;
			});
		}

		private void button6_Click (object sender, EventArgs e)
		{
			client.receiveFile (textBox3.Text, textBox4.Text);
		}
	}
}

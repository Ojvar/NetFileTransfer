using Common.Helper.Network.Command;
using Common.Helper.Network.Core;
using Common.Helper.Network.Model;
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
	public partial class Form1 : Form
	{
		public Form1 ()
		{
			InitializeComponent ();
		}

		Server server;
		Client	client;

		private void button1_Click (object sender, EventArgs e)
		{
			if (server != null)
				server.stop (true);

			server = new Server (11365);
			server.onAcceptClient   += (s, c) => { addToTextBox ("connected", textBox1); };
			server.onStart	+= (s) => { addToTextBox ("server start", textBox1); };
			server.onStop += (s) => { addToTextBox ("server stopped", textBox1); };
			server.onClientDataReceived	+= (s, c, d) => {
				//string st = NetPacket.getStringUtf8 (d.data);
				//addToTextBox (d.token.ToString () + " Data received " + st, textBox1, true);

				if (d.type == EnumCommandType.Text)
					addToTextBox (NetPacket.getStringUtf8 (d.data), textBox4, false);
				else if (d.type == EnumCommandType.Finish)
				{
					closeFile (d.token);
					return;
				}
				else if (d.type == EnumCommandType.SendFileRequest)
					makeFile (d.token, NetPacket.getStringUtf8 (d.data));
				else if (d.type == EnumCommandType.FileData)
					appendToFile (d.token, d.data);

				d.type	= EnumCommandType.Ok;
				d.data	= null;
				c.write (d);
			};
			server.start ();
		}


		Dictionary<Guid, FileStream>	files	= new Dictionary<Guid, FileStream>  ();
		private void makeFile (Guid token, string filename)
		{
			FileStream	fs	= File.Open ("D:\\received\\" + token.ToString () + "_" + filename, FileMode.CreateNew);
			files.Add (token, fs);
		}

		private void appendToFile (Guid token, byte[] data)
		{
			files[token].Write (data, 0, data.Length);
			files[token].Flush ();
		}

		private void closeFile (Guid token)
		{
			files[token].Close ();
			files.Remove (token);
		}

		private void button2_Click (object sender, EventArgs e)
		{
			server?.stop (true);
		}


		private void button3_Click (object sender, EventArgs e)
		{
			client	= new Client("127.0.0.1", 11365);
			client.onConnect	+=  (c) => { addToTextBox ("Connected", textBox2); };
			client.onDisconnect	+=  (c) => { addToTextBox ("Disconnected", textBox2); };
			//client.onDataSend	+=  (c, d) => {
			//	if (d?.data != null)
			//		addToTextBox ("Data Sent : ", textBox2, true);
			//};
			//client.onDataReceived	+=  (c, d) => {
			//	if (d?.data != null)
			//		addToTextBox ("Data received " , textBox2);
			//};
			client.connect (true);
		}

		private void button4_Click (object sender, EventArgs e)
		{
			client.disconnect (true);
		}


		List<SendFileCommand> cmds = new List<SendFileCommand> ();

		private void button5_Click (object sender, EventArgs e)
		{
			
			foreach (string s in textBox3.Lines)
			{
				SendFileCommand msg	= new SendFileCommand (client, s);
				msg.onProgress  += Msg_onProgress;

				cmds.Add (msg);

				msg.start ();
			}
			//client?.write (new NetPacket (EnumCommandType.Text)
			//{
			//	data	= Encoding.UTF8.GetBytes ("Hello to all")
			//});
		}

		private void Msg_onProgress (BaseCommand cmd, object data)
		{
			addToTextBox (data.ToString (), textBox2, true);
		}

		void addToTextBox (string text, TextBox box, bool clear = true)
		{
			Invoke ((Action)delegate
			{
				if (clear)
					box.Text	= "";
				box.AppendText (text);
			});
		}

		private void button6_Click (object sender, EventArgs e)
		{
			SendMessageCommand msg = new SendMessageCommand (client, "Hello by Packet " + DateTime.Now.ToString ());
			msg.start ();
		}
	}
}

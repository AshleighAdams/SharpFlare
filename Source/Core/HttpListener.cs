using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SharpFlare
{
	public static class HttpListener
	{
		public static void Listen(int port, IPAddress ip = null)
		{
			if(ip == null)
				ip = IPAddress.IPv6Any;
			var listener = new TcpListener(ip, port);
			listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

			listener.Start(100);

			Thread t = new Thread(new ParameterizedThreadStart(_ListenTask));
			t.Start(listener);
			//Task.Run(() => _ListenTaskAsync(listener));
		}

		private static void _ListenTask(object lo)
		{
			TcpListener listener = (TcpListener)lo; // do i have to make this copy like in Lua? i forget if C# needs this...
			while(true)
			{
				//Console.WriteLine("Accept");
				Socket sock = listener.AcceptSocket();

				if(sock == null)
					break;

				Task.Run(() => _HandleSocketTask(sock));
			}
		}

		private static async Task _ListenTaskAsync(TcpListener l)
		{
			TcpListener listener = l; // do i have to make this copy like in Lua? i forget if C# needs this...
			while(true)
			{
				//Console.WriteLine("Accept");
				Socket sock = await listener.AcceptSocketAsync();

				if(sock == null)
					break;

				Task.Run(() => _HandleSocketTask(sock));
			}
		}

		public static async Task<int> ReadHttpHeaders(this SocketStream self, byte[] buff, int pos, int length)
		{
			int at = 0;
			int lf_count = 0;

			return await self.ReadUntil(buff, pos, length, delegate(byte[] peekbuff, int offset, int peeklength)
			{
				for(; at < peeklength; at++)
				{
					if(peekbuff[offset + at] == '\n')
					{
						lf_count++;
						if(lf_count == 2)
							return at;
					}
					else if(peekbuff[offset + at] != '\r')
						lf_count = 0;
				}
				return -1;
			});
		}

		private static async void _HandleSocketTask(Socket socket)
		{
			SocketStream str = new SocketStream(socket);
			byte[] buff = new byte[4096];


			string html = 
@"<html>
	<head>
		<title>SharpFlare test!</title>
	</head>
	<body>
		<p>Hello, world!</p>
	</body>
</html>";
			string response =
@"HTTP/1.1 200 Okay
Connection: keep-alive
Content-Type: text/html
Content-Length: " + html.Length.ToString() + @"

" + html;

			try
			{
				while(str.Connected)
				{
					int len = await str.ReadHttpHeaders(buff, 0, buff.Length);
					string[] lines = Encoding.UTF8.GetString(buff, 0, len).Split('\n');

					Console.WriteLine(lines[0]);

					await str.Write(response);

					Console.WriteLine("wrote {0} bytes", response.Length);
				}
			}
			catch(SocketException s)
			{
			}
		}
	}
} 

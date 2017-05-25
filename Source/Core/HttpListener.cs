using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SharpFlare.Http;

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

				#pragma warning disable 4014
				Task.Run(() => _HandleSocketTask(sock));
				#pragma warning restore 4014
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
			Http1Request  req = new Http1Request();
			Http1Response res = new Http1Response();

			using(SocketStream str = new SocketStream(socket))
			{
				try
				{
					byte[] buff = new byte[4096];
					while(str.Connected)
					{
						int len = await str.ReadHttpHeaders(buff, 0, buff.Length);
						string[] lines = Encoding.UTF8.GetString(buff, 0, len).Split('\n');

						try
						{
							req.Setup(lines, str, socket);
							res.Setup(str, req);

							if(req.Path.EndsWith("jpg"))
							{
								res["Content-Type"] = "image/jpeg";
								res.Content = new FileStream("/home/kobra/Pictures/Wallpapers/tree-road-mountain.jpg", FileMode.Open);
							}
							else
							{
								string html = 
									"<html>\n" +
									"	<head>\n" +
									"	</head>\n" +
									"	<body>\n" +
									"		Hello, world!\n" +
									"	</body>\n" +
									"</html>\n";
								
								res["Content-Type"] = "text/html";
								res.Content = new MemoryStream(Encoding.UTF8.GetBytes(html));
							}


							await res.Finalize();
						}
						catch(NotImplementedException)
						{
							string msg = "";
							await str.Write($"HTTP/1.1 501 Not Implemented\nConnection: keep-alive\nContent-Length: {msg.Length+1}\n\n{msg}\n");
						}
						catch(HttpException ex)
						{
							Logger.GlobalLogger.Message(Logger.Level.Notice, $"Http Error: {ex.Message}");
							// TODO: make this use the hook "Error"
							await str.Write($"HTTP/1.0 {ex.HttpCode.code} {ex.HttpCode.message}\nConnection: close\nContent-Length: {ex.Message.Length+1}\n\n{ex.Message}\n");
							break; // close the connection
						}
					}
				}
				catch(SocketException) { }
				catch(IOException) { }
			}
		}
	}
} 

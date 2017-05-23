using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

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

		static char[] _SplitString = new char[1]{' '};

		private static async void _HandleSocketTask(Socket socket)
		{
			SocketStream str = new SocketStream(socket);
			byte[] buff = new byte[4096];

			while(str.Connected)
			{
				// read headers
				while(true)
				{
					Console.WriteLine(await str.ReadLine());
				}
			}
		}
	}
} 

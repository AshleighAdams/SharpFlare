using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace SharpFlare
{
	public static class HttpListener
	{
		public static Task Listen(int port, IPAddress ip = null)
		{
			if(ip == null)
				ip = IPAddress.IPv6Any;
			var listener = new TcpListener(ip, port);
			listener.Start();

			return TaskPool.Run(() => _ListenTask(listener));
			//return Task.Factory.StartNew(
			//	async delegate()
			//	{
			//		await _ListenTask(listener);
			//	}, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously);
		}

		private static async void _ListenTask(TcpListener l)
		{
			TcpListener listener = l; // do i have to make this copy like in Lua? i forget if C# needs this...
			while(true)
			{
				Console.WriteLine("Accept");
				Socket sock = await listener.AcceptSocketAsync();

				if(sock == null)
					break;

				TaskPool.Run(() => _HandleSocketTask(sock));
				//Task.Factory.StartNew(
				//	async delegate()
				//	{
				//		await _HandleSocketTask(sock);
				//	}, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}

		static char[] _SplitString = new char[1]{' '};

		private static async void _HandleSocketTask(Socket socket)
		{
			// identify method, resource, and version
			NetworkStream    ns = new NetworkStream(socket, true);
			//BufferedStream   bs = new BufferedStream(ns);
			MemoryStream     ms = new MemoryStream();
			StreamWriter     w  = new StreamWriter(ns); // will dispose the stream
			SafeStreamReader r  = new SafeStreamReader(ns);

			while(true)
			{
				string[] payload = (await r.SafeReadLine()).Split(_SplitString, 3);
				if(payload.Length < 3)
				{
					// invalid request
					socket.Close();
					break;
				}

				string method   = payload[0];
				string resource = payload[1];
				string version  = payload[2];

				Console.WriteLine($"{method} {resource} {version}");

				if(version.StartsWith("HTTP/1"))
				{
					string html = "<center>Hello, world.</center>";
					await w.WriteLineAsync("HTTP/1.0 200 Okay");
					await w.WriteLineAsync("Content-Type: text/html");
					await w.WriteLineAsync("Content-Length: " + html.Length.ToString());
					await w.WriteLineAsync();
					await w.WriteLineAsync(html);
					await w.FlushAsync();
				}
				else if(version.StartsWith("HTTP/2"))
				{
					socket.Close();
					break;
				}
				else
				{
					// not supported
					socket.Close();
					break;
				}
			}
			// "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"
			// 0x505249202a20485454502f322e300d0a0d0a534d0d0a0d0a
		}
	}
} 

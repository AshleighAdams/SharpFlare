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
		public static async Task ListenAsync(int port, IPAddress ip)
		{
			if (ip == null)
				throw new ArgumentNullException("ip");
			TcpListener listener = new TcpListener(ip, port);
			listener.Start(100);
			await Task.Run(() => _ListenTaskAsync(listener));
		}

		public static Thread Listen(int port, IPAddress ip)
		{
			if (ip == null)
				throw new ArgumentNullException("ip");
			TcpListener listener = new TcpListener(ip, port);
			listener.Start(100);
			Thread ret = new Thread(new ParameterizedThreadStart(_ListenTask));
			ret.Start();
			return ret;
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

				Task.Run(() => HandleSocket(sock));
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
				Task.Run(() => HandleSocket(sock));
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

		private static async void HandleSocket(Socket socket)
		{
			Http1Request  req = new Http1Request();
			Http1Response res = new Http1Response();

			using(SocketStream str = new SocketStream(socket))
			{
				try
				{
					byte[] buff = new byte[4096];
					bool first = true;

					while(str.Connected && (res.KeepAlive || first))
					{
						if (first) first = false;

						int len = await str.ReadHttpHeaders(buff, 0, buff.Length);
						string[] lines = Encoding.UTF8.GetString(buff, 0, len).Split('\n');

						try
						{
							req.Setup(lines, str, socket);
							res.Setup(str, req);

							await Hooks.Hook.Call("Request", req, res);

							if (!res.Finalized)
								throw new HttpException("Request was not finalized.", Http.Status.InternalServerError);
						}
						///*
						catch (NotImplementedException ex)
						{
							if (res.Finalized)
								break; // a response has already been sent, or it failed during the sending, a 501 internal error
									   // can't be sent anymore, so just close the connection
									   //hook.Call
							res.StatusCode = Status.NotImplemented;
							await Hooks.Hook.Call("Error", req, res, new HttpException(ex, "not imp", Status.NotImplemented));
						}
						catch (HttpException ex)
						{
							Logger.GlobalLogger.Message(Logger.Level.Notice, $"Http Error: {ex.Message}");
							// TODO: make this use the hook "Error"
							//await str.Write($"HTTP/1.0 {ex.HttpStatus.code} {ex.HttpStatus.message}\nConnection: close\nContent-Length: {ex.Message.Length+1}\n\n{ex.Message}\n");

							res.StatusCode = ex.HttpStatus;
							await Hooks.Hook.Call("Error", req, res, ex);

							if (!ex.KeepAlive)
								break;
						}
						catch (Exception ex)
						{
							Logger.GlobalLogger.Message(Logger.Level.Error, $"{req.Method} {req.Path} Exception: {ex}");
							res.StatusCode = Status.InternalServerError;
							await Hooks.Hook.Call("Error", req, res, new HttpException(ex, ex.Message, Status.InternalServerError));
						}
						// */
					}
				}
				catch(SocketException) { }
				catch(IOException) { }
			}
		}
	}
} 

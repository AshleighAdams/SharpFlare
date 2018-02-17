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
	public static class Http1Listener
	{
		public static async Task ListenAsync(int port, IPAddress ip)
		{
			TcpListener listener = new TcpListener(ip, port);
			listener.Start(100);
			listener.Server.NoDelay = true;
			await Task.Run(() => _ListenTaskAsync(listener));
		}

		public static Thread Listen(int port, IPAddress ip)
		{
			TcpListener listener = new TcpListener(ip, port);
			listener.Start(100);
			Thread ret = new Thread(new ParameterizedThreadStart(_ListenTask));
			ret.Start(listener);
			return ret;
		}

		private static void _ListenTask(object lo)
		{
			TcpListener listener = (TcpListener)lo; // do i have to make this copy like in Lua? i forget if C# needs this...
			while (true)
			{
				//Console.WriteLine("Accept");
				Socket sock = listener.AcceptSocket();

				if (sock == null)
					break;

				//TaskPool.Run(async () => await HandleSocket(sock));
				Task.Run(() => HandleSocket(sock)).ConfigureAwait(false);
			}
		}

		private static async Task _ListenTaskAsync(TcpListener l)
		{
			TcpListener listener = l; // do i have to make this copy like in Lua? i forget if C# needs this...
			while (true)
			{
				Socket sock = await listener.AcceptSocketAsync().ConfigureAwait(false);
				if (sock == null)
					break;

#pragma warning disable 4014
				//TaskPool.Run(async () => await HandleSocket(sock));
				Task.Run(() => HandleSocket(sock)).ConfigureAwait(false);
#pragma warning restore 4014
			}
		}

		public static async Task<int> ReadHttpHeaders(this SocketStream self, byte[] buff, int pos, int length)
		{
#if SHARPFLARE_PROFILE
			using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
			{
				int at = 0;
				int lf_count = 0;

				return await self.ReadUntil(buff, pos, length, delegate (byte[] peekbuff, int offset, int peeklength)
				{
					for (; at < peeklength; at++)
					{
						if (peekbuff[offset + at] == '\n')
						{
							lf_count++;
							if (lf_count == 2)
								return at;
						}
						else if (peekbuff[offset + at] != '\r')
							lf_count = 0;
					}
					return -1;
				});
			}
		}

		//static int created = 0;
		//static object created_lock = new object();
		static ObjectPool<Tuple<Http1Request, Http1Response>> RequestResponsePool = new ObjectPool<Tuple<Http1Request, Http1Response>>(
			delegate
			{
				//lock (created_lock)
				//{
				//	created++;
				//	Console.WriteLine($"There are now {created} allocated.");
				//}
				return new Tuple<Http1Request, Http1Response>(new Http1Request(), new Http1Response());
			});
		static int conn = 0;
		private static async void HandleSocket(Socket socket)
		{
#if SHARPFLARE_PROFILE
			using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
			{
				conn++;
				bool should_profile = conn > 0 && false;
				if (should_profile)
					Profiler.Start("request.log");

				//var tuple = RequestResponsePool.Take();
				Http1Request req = new Http1Request();//=  tuple.Item1;
				Http1Response res = new Http1Response();// = tuple.Item2;
				
				using (SocketStream str = new SocketStream(socket, req.ReadBuffer))
				{
					try
					{
						//byte[] buff = new byte[4096];
						bool first = true;

						while (str.Connected && (res.KeepAlive || first))
						{
							if (first) first = false;

							//int len = await str.ReadHttpHeaders(buff, 0, buff.Length).ConfigureAwait(false);
							//string[] lines = Encoding.UTF8.GetString(buff, 0, len).Split('\n');

							try
							{
								// setup the response first, it's less likely to fault, and ensures
								// a fault parsing the request can still send an error response
								res.Setup(str);
								await req.Setup(/*lines, */str, socket);
								res.Setup(req);

								await Hooks.Call("Request", req, res).ConfigureAwait(false);

								if (!res.Finalized)
									throw new HttpException("Request was not finalized.", Http.Status.InternalServerError);
							}
							///*
							catch (SocketException)
							{
								break;
							}
							catch (IOException e) when (e.InnerException is SocketException)
							{
								break;
							}
							catch (NotImplementedException ex)
							{
								if (res.Finalized)
									break; // a response has already been sent, or it failed during the sending, a 501 internal error
										   // can't be sent anymore, so just close the connection
										   //hook.Call
								res.StatusCode = Status.NotImplemented;
								await Hooks.Call("Error", req, res, new HttpException(ex, "Not implemented.", Status.NotImplemented)).ConfigureAwait(false);
							}
							catch (HttpException ex)
							{
								Logger.GlobalLogger.Message(Logger.Level.Notice, $"Http Error: {ex.Message}");
								// TODO: make this use the hook "Error"
								//await str.Write($"HTTP/1.0 {ex.HttpStatus.code} {ex.HttpStatus.message}\nConnection: close\nContent-Length: {ex.Message.Length+1}\n\n{ex.Message}\n");

								res.StatusCode = ex.HttpStatus;
								await Hooks.Call("Error", req, res, ex).ConfigureAwait(false);

								if (!ex.KeepAlive)
									break;
							}
							catch (Exception ex)
							{
								Logger.GlobalLogger.Message(Logger.Level.Error, $"{req.Method} {req.Url.Path} Exception: {ex}");
								try
								{

									res.StatusCode = Status.InternalServerError;
									await Hooks.Call("Error", req, res, new HttpException(ex, ex.Message, res.StatusCode)).ConfigureAwait(false);
								}
								catch (Exception ex2)
								{
									Logger.GlobalLogger.Message(Logger.Level.Error, $"Exception in exception handler: {ex2}");
								}
							}
							// */
						}
					}
					catch (SocketException) { }
					catch (IOException) { }
				}

				//RequestResponsePool.Return(tuple);

				if (should_profile)
					Profiler.Stop();
			}
		}
	}
} 

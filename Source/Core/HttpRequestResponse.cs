using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Text;

namespace SharpFlare
{
	namespace Http
	{
		public class Http1Request : Request
		{
			public byte[] ReadBuffer = new byte[4096];
			public class Http1RequestUrl : RequestUrl // http://user:pass@google.com/search?q=hi
			{
				public Http1RequestUrl() { }
				public string Scheme { get; set; }   // http
				public string Username { get; set; } // user
				public string Password { get; set; } // pass
				public string Host { get; set; }     // google.com
				public int Port { get; set; }     // 80
				public string OriginalPath { get; set; }
				public string Path { get; set; }     // /search
				public string Query { get; set; }    // q=hi
			}

			public Http1Request() { }

			// setup vars put outside to ease the garbage collector strain
			LinkedList<string> _setup_path_stack = new LinkedList<string>();
			static char[] _setup_split_space = new char[] { ' ' };
			public async Task Setup(SocketStream stream, Socket sock)
			{
#if SHARPFLARE_PROFILE
			using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					this._Url.Scheme = this._Url.Username = this._Url.Password = this._Url.Host = this._Url.OriginalPath = this._Url.Path = this._Url.Query = "";
					this._Url.Port = 80;

					this.Content = stream; // so on an early exception, we can still deliver the error
					this.IP = (sock.RemoteEndPoint as IPEndPoint).Address;
					headers.Clear();

					// read the data
					int len = await stream.ReadHttpHeaders(ReadBuffer, 0, ReadBuffer.Length).ConfigureAwait(false);

					string path;
					double version;

					using (var memreadstr = new MemoryStream(ReadBuffer))  // Encoding.UTF8.GetString(ReadBuffer, 0, len).Split('\n');
					using (var linereader = new StreamReader(memreadstr))
					{
						if(linereader.EndOfStream)
							throw new HttpException("No data present.", status: Status.BadRequest, keepalive: false);
						//if (lines.Length < 1)
						//	throw new HttpException("No data present.", keepalive: false);

						
						{
							string[] split = linereader.ReadLine().Split(_setup_split_space, 3);
							if (split.Length != 3)
								throw new HttpException("Invalid request line.", status: Status.BadRequest, keepalive: false);
							this.Method = split[0];
							path = split[1];
							this.Protocol = split[2];
							if (!double.TryParse(split[2].Replace("HTTP/", ""), out version))
								throw new HttpException("Invalid version string.", Status.BadRequest);
						}

						string lastheader = ""; // for continuations
						int i = 0;
						while(!linereader.EndOfStream)
						{
							string line = linereader.ReadLine();
							if (string.IsNullOrEmpty(line))
								break;

							if (line[0] == ' ' || line[0] == '\t')
							{
								if (string.IsNullOrEmpty(lastheader))
									throw new HttpException("No previous header to append to.", status: Http.Status.BadRequest, keepalive: false);
								this[lastheader] += ' ' + line.TrimEnd();
							}
							else
							{
								i++;
								int index = line.IndexOf(':');

								if (index == 0)
									throw new HttpException($"The {i}{Util.Nth(i)} header key is empty.", status: Http.Status.BadRequest, keepalive: false);
								else if (index < 0)
									throw new HttpException($"The {i}{Util.Nth(i)} header value is non existant.", status: Http.Status.BadRequest, keepalive: false);

								string key = line.Substring(0, index);
								string value = line.Substring(index + 1).Trim();

								headers[key] = value;
								lastheader = key;
							}
						}
					}
					
					string host = this["Host"];

					if (version >= 1.2 && path[0] != '/')
					{
						// extract the host, port, and scheme
						throw new NotImplementedException();
					}

					{ // parse the url
						int portpos = host.IndexOf(':');
						if (portpos == -1)
							this._Url.Port = 80; // TODO: make this 443 for https
						else
						{
							int port;
							if (int.TryParse(host.Substring(portpos + 1), out port))
								this._Url.Port = port;
							else
								throw new HttpException("Host's port is in an invalid format.", Status.BadRequest);
							host = host.Substring(0, portpos);
						}

						this._Url.Host = host;

						int query = path.IndexOf('?');
						if (query != -1)
						{
							this._Url.Query = path.Substring(query + 1);
							path = path.Substring(0, query);
						}

						path = Unescape.Url(path).Replace('\\', '/');


						// canonicalize the path /a/b/c/../z -> /a/b/z
						int i = 0;
						while (i < path.Length)
						{
							int start = i;
							int end;
							for (end = i; end < path.Length && path[end] != '/'; end++)
								;
							i = end + 1;
							int bitlen = end - start; // either it is a slash, or it went out of bounds by 1 anyway

							if (bitlen == 0) // is it an empty node? continue
								continue;
							else if (bitlen == 1 && path[start] == '.')
								continue;
							else if (bitlen == 2 && path[start] == '.' && path[start + 1] == '.')
								_setup_path_stack.RemoveLast();
							else
								_setup_path_stack.AddLast(path.Substring(start, bitlen));
						}
						this._Url.Path = path = $"/{string.Join("/", _setup_path_stack)}";
						_setup_path_stack.Clear();
					}
					
					long content_length = 0;
					string strcontent_length = this["Content-Length"];
					if (strcontent_length != "")
						if (!long.TryParse(strcontent_length, out content_length))
							throw new HttpException("Failed to parse Content-Length.", Status.BadRequest);
					this.ContentLength = content_length;
				}
			}

			public string Protocol { get; private set; }
			public string Method { get; private set; }
			public Http1RequestUrl _Url = new Http1RequestUrl();
			public RequestUrl Url { get { return _Url; } }
			public SocketStream Content { get; private set; }
			public long ContentLength { get; private set; }

			public IPAddress IP { get; private set; }

			Dictionary<string, string> headers = new Dictionary<string, string>();
			public string this[string key]
			{
				get
				{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
					{
						string v;
						if (!headers.TryGetValue(key, out v))
							return "";
						return v;
					}
				}
				private set
				{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
					{
						headers[key] = value;
					}
				}
			}

			public string GetCookie(string name)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					throw new NotImplementedException();
				}
			}
		}

		public class Http1Response : Response
		{
			public bool Finalized { get; private set; }
			List<Tuple<string, string>> headers = new List<Tuple<string, string>>();
			SocketStream stream;
			public bool KeepAlive { get; private set; }

			public Stream Content { get; set; }

			public Http1Response() { }

			public void Setup(SocketStream str)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					stream = str;
					Content = null;
					headers.Clear();
					Finalized = false;
					StatusCode = Http.Status.Okay;
				}
			}

			public void Setup(Http1Request req)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					KeepAlive = req["Connection"].ToLower().Contains("keep-alive");
				}
			}

			public Status StatusCode { set; get; }

			byte[] sendbuff = new byte[8192];
			public async Task Finalize()
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					if (Finalized)
						return;
					Finalized = true;

					if (KeepAlive)
						this["Connection"] = "keep-alive";
					else
						this["Connection"] = "close";

					this["Server"] = "SharpFlare";
					this["Date"] = DateTime.Now.ToHttpDate();

					if (Content != null)
						this["Content-Length"] = Content.Length.ToString();

					StringBuilder sb = new StringBuilder();
					sb.Append($"HTTP/1.1 {StatusCode.code} {StatusCode.message}\n");
					foreach (Tuple<string, string> tup in headers) // TODO: escape/encode these
						sb.Append($"{tup.Item1}: {tup.Item2}\r\n");
					sb.Append($"\r\n");

					string s = sb.ToString();
					int count = Encoding.UTF8.GetBytes(s, 0, s.Length, sendbuff, 0);

					await stream.Write(sendbuff, 0, count).ConfigureAwait(false);

					if (Content != null)
					{
						while (true)
						{
							count = await Content.ReadAsync(sendbuff, 0, sendbuff.Length).ConfigureAwait(false);
							if (count == 0)
								break;
							await stream.Write(sendbuff, 0, count).ConfigureAwait(false);
						}

						Content.Dispose();
						Content = null;
					}

					await stream.Flush().ConfigureAwait(false);
				}
			}

			// set Content-Type
			public string this[string index]
			{
				set
				{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
					{
						headers.Add(new Tuple<string, string>(index, value));
					}
				}
			}

			public void SetCookie(                   // Set-Cookie:
				string name, string value,           // name=value;
				DateTime? expires = null,            // Expires= or Max-Age=;
				string domain = null,                // Domain=;
				string path = null,                  // Path=;
				bool secure = false,                 // Secure;
				bool httponly = false,               // HttpOnly
				string samesite = null               // SameSite=
			)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					throw new NotImplementedException();
				}
			}
		}
	}
}

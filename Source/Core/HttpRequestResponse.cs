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

			public void Setup(string[] lines, SocketStream stream, Socket sock)
			{
				this._Url.Scheme = this._Url.Username = this._Url.Password = this._Url.Host = this._Url.OriginalPath = this._Url.Path = this._Url.Query = "";
				this._Url.Port = 80;

				this.Content = stream; // so on an early exception, we can still deliver the error
				this.IP = (sock.RemoteEndPoint as IPEndPoint).Address;
				headers.Clear();
				

				if (lines.Length < 1)
					throw new HttpException("No data present.", keepalive: false); 

				string[] split = lines[0].Split(' ');

				if(split.Length != 3)
					throw new HttpException("Invalid request line.", keepalive: false); 
				

				string lastheader = ""; // for continuations
				for(int i = 1; i < lines.Length; i++)
				{
					string line = lines[i];
					if(string.IsNullOrWhiteSpace(line))
						break;


					if(line[0] == ' ' || line[0] == '\t')
					{
						if(string.IsNullOrEmpty(lastheader))
							throw new HttpException("No previous header to append to.", status: Http.Status.BadRequest, keepalive: false);
						this[lastheader] += ' ' + line.TrimEnd();
					}
					else
					{
						int index = line.IndexOf(':');

						if(index == 0)
							throw new HttpException($"The {i}{Util.Nth(i)} header key is empty.", status: Http.Status.BadRequest, keepalive: false);
						else if(index < 0)
							throw new HttpException($"The {i}{Util.Nth(i)} header value is non existant.", status: Http.Status.BadRequest, keepalive: false);

						string key = line.Substring(0, index);
						string value = line.Substring(index + 1).Trim();

						headers[key] = value;
						Console.WriteLine($"'{key}' = '{value}'");

						lastheader = key;
					}
				}

				string path = split[1];
				string host = this["Host"];
				double version;
				if (!double.TryParse(split[2].Replace("HTTP/", ""), out version))
					throw new HttpException("Invalid version string.", Status.BadRequest);

				string pathbit = split[1];
				if (version >= 1.2 && path[0] != '/')
				{
					// extract the host, port, and scheme
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

					int query = pathbit.IndexOf('?');
					if (query != -1)
					{
						this._Url.Query = pathbit.Substring(query + 1);
						pathbit = pathbit.Substring(0, query);
					}

					pathbit = Unescape.Url(pathbit);
					this._Url.Path = pathbit;
				}

				this.Method        = split[0];
				this.Protocol      = split[2];
				
				long content_length = 0;
				string strcontent_length = this["Content-Length"];
				if (strcontent_length != "")
					if (!long.TryParse(strcontent_length, out content_length))
						throw new HttpException("Failed to parse Content-Length.", Status.BadRequest);
				this.ContentLength = content_length;
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
					string v;
					if(!headers.TryGetValue(key, out v))
						return "";
					return v;
				}
				private set
				{
					headers[key] = value;
				}
			}

			public string GetCookie(string name)
			{
				throw new NotImplementedException();
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

			public void Setup(SocketStream str, Http1Request req)
			{
				stream = str;
				Content = null;
				headers.Clear();
				Finalized = false;
				StatusCode = Http.Status.Okay;
				KeepAlive = req["Connection"].ToLower().Contains("keep-alive");
			}

			public Status StatusCode { set; get; }

			byte[] sendbuff = new byte[8192];
			public async Task Finalize()
			{
				if(Finalized)
					return;
				Finalized = true;

				if (KeepAlive)
					this["Connection"] = "keep-alive";
				else
					this["Connection"] = "close";

				this["Server"] = "SharpFlare";
				this["Date"] = DateTime.Now.ToHttpDate();

				if(Content != null)
					this["Content-Length"] = Content.Length.ToString();

				StringBuilder sb = new StringBuilder();
				sb.Append($"HTTP/1.1 {StatusCode.code} {StatusCode.message}\n");
				foreach(Tuple<string, string> tup in headers) // TODO: escape/encode these
					sb.Append($"{tup.Item1}: {tup.Item2}\n");
				sb.Append($"\n");

				string s = sb.ToString();
				int count = Encoding.UTF8.GetBytes(s, 0, s.Length, sendbuff, 0);
				await stream.Write(sendbuff, 0, count);

				if(Content != null)
				{
					while(true)
					{
						count = await Content.ReadAsync(sendbuff, 0, sendbuff.Length);
						if(count == 0)
							break;
						await stream.Write(sendbuff, 0, count);
					}

					Content.Dispose();
					Content = null;
				}
			}

			// set Content-Type
			public string this[string index]
			{
				set
				{
					headers.Add(new Tuple<string, string>(index, value));
				}
			}

			public void SetCookie(                   // Set-Cookie:
				string name, string value,    // name=value;
				DateTime? expires  = null,   // Expires= or Max-Age=;
				string    domain    = null,   // Domain=;
				string    path      = null,   // Path=;
				bool      secure    = false,  // Secure;
				bool      httponly  = false,  // HttpOnly
				string    samesite  = null    // SameSite=
			)
			{
			}
		}
	}
}

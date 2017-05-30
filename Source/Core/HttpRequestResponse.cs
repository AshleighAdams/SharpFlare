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
			public Http1Request() { }

			public void Setup(string[] lines, SocketStream stream, Socket sock)
			{
				headers.Clear();

				if(lines.Length < 1)
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

				this.Method        = split[0];
				this.Path          = split[1];
				this.Protocol      = split[2];
				this.Authority     = "todo";
				this.Scheme        = "todo";
				this.Host          = this["Host"];
				this.Content       = stream;
				this.IP            = (sock.RemoteEndPoint as IPEndPoint).Address;
				this.ContentLength = 0;
				// parse headers
			}

			public string Protocol { get; private set; }
			public string Method { get; private set; }
			public string Path { get; private set; }
			public string Authority { get; private set; }
			public string Scheme { get; private set; }
			public string Host { get; private set; }
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
				this["Date"] = DateTime.UtcNow.ToString();

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
				DateTime?  expires  = null,   // Expires= or Max-Age=;
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

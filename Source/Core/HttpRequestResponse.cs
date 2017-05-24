using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;

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
					throw new HttpException("No data present."); 

				string[] split = lines[0].Split(' ');

				if(split.Length != 3)
					throw new HttpException("Invalid request line."); 
				

				string lastheader = ""; // for continuations
				for(int i = 1; i < lines.Length; i++)
				{
					string line = lines[i];
					if(string.IsNullOrWhiteSpace(line))
						break;

					int index = line.IndexOf(':');
					if(index < 0)
						throw new NotImplementedException();

					string key = line.Substring(0, index);
					string value = line.Substring(index + 1).Trim();

					headers[key] = value;
					Console.WriteLine($"'{key}' = '{value}'");

					lastheader = key;
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
			}
		}
	}
}

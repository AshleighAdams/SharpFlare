using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

namespace SharpFlare
{
	namespace Http
	{
		public class Http1Request : Request
		{
			public Http1Request() { }

			public void Setup(string[] lines)
			{
				if(lines.Length < 1)
					throw new HttpException("No data present."); 

				string[] split = lines[0].Split(' ');

				if(split.Length != 3)
					throw new HttpException("Invalid request line."); 
				
				this.Method    = split[0];
				this.Path      = split[1];
				this.Protocol  = split[2];
				this.Authority = "todo";
				this.Scheme    = "todo";
				this.Host      = this["Host"];

			}

			public string Protocol { get; private set; }
			public string Method { get; private set; }
			public string Path { get; private set; }
			public string Authority { get; private set; }
			public string Scheme { get; private set; }
			public string Host { get; private set; }
			public SocketStream Content { get; private set; }

			Dictionary<string, string> headers;
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

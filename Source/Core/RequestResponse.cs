// https://http2.github.io/http2-spec/#HttpRequest
// http://http2.github.io/http2-spec/compression.html
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace SharpFlare
{
	namespace Http
	{
		public class HttpException : Exception
		{
			public readonly int HttpCode;
			public HttpException(string message, int code = 401) : base(message)
			{
				HttpCode = code;
			}
		}

		public interface Request
		{
			string Protocol { get; }
			string Method { get; }
			string Host { get; }
			string Path { get; }
			string Authority { get; }
			string Scheme { get; }
			string this[string index] { get; }
			SocketStream Content { get; }
			long ContentLength { get; }
			IPAddress IP { get; }
		}
		
		public interface Response
		{
			Status StatusCode { get; set; }
			string this[string index] { get; set; }
			Stream Content { get; set; } // please set content type
		}
	}
}

// https://http2.github.io/http2-spec/#HttpRequest
// http://http2.github.io/http2-spec/compression.html
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SharpFlare
{
	namespace Http
	{
		public class HttpException : Exception
		{
			public readonly Status HttpCode;
			public HttpException(string message, Status code = null) : base(message)
			{
				if(code == null)
					code = Http.Status.BadRequest;
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
			SocketStream Content { get; }
			long ContentLength { get; }
			IPAddress IP { get; }
			string this[string index] { get; }
			string GetCookie(string name);
		}

		namespace CookieAttribute
		{
			enum SameSite
			{
				None, Lax, Strict
			}
		}

		public interface Response
		{
			Status StatusCode { set; }
			Task Finalize();
			Stream Content { set; }
			// set Content-Type
			string this[string index] { set; }
			void SetCookie(                   // Set-Cookie:
				string name, string value,    // name=value;
				DateTime?  expires  = null,   // Expires= or Max-Age=;
				string    domain    = null,   // Domain=;
				string    path      = null,   // Path=;
				bool      secure    = false,  // Secure;
				bool      httponly  = false,  // HttpOnly
				string    samesite  = null    // SameSite=
			);
		}
	}
}

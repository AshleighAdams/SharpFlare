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
			public readonly Status HttpStatus;
			public readonly bool KeepAlive;
			public HttpException(string message, Status status = null, bool keepalive = true) : base(message)
			{
				if(status == null)
					status = Http.Status.BadRequest;
				HttpStatus = status;
				KeepAlive = keepalive;
			}
			public HttpException(Exception inner, string message, Status status = null, bool keepalive = true) : base(message, inner)
			{
				if (status == null)
					status = Http.Status.BadRequest;
				HttpStatus = status;
				KeepAlive = keepalive;
			}
		}

		public interface RequestUrl // http://user:pass@google.com/./dir/../search?q=hi
		{
			string Scheme { get; }       // http
			string Username { get; }     // user
			string Password { get; }     // pass
			string Host { get; }         // google.com
			int    Port { get; }         // 80
			string OriginalPath { get; } // /./dir/../search
			string Path { get; }         // /search
			string Query { get; }        // q=hi
		}

		public interface Request
		{
			string Protocol { get; }
			string Method { get; }
			RequestUrl Url { get; }
			SocketStream Content { get; }
			long ContentLength { get; }
			IPAddress IP { get; }
			string this[string index] { get; }
			bool HeaderPresent(string index);
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
			DateTime? LastModified { set; } // will try to pull from the stream if it's supported too
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

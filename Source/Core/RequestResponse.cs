// https://http2.github.io/http2-spec/#HttpRequest
// http://http2.github.io/http2-spec/compression.html
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

namespace SharpFlare
{
	namespace Http
	{
		public enum ResponseCode : uint
		{
			Okay = 200,
			FileNotFound = 404
		}
		
		public interface Request
		{
			string Protocol { get; }
			string Method { get; }
			string Path { get; }
			string Authority { get; }
			string Scheme { get; }
			Dictionary<string, string> Headers { get; }
			Stream Content { get; }
		}
		
		public interface Response
		{
			ResponseCode Response { get; set; }
			Dictionary<string, string> Headers { get; set; }
			Stream Content { get; set; } // please set content type
			void Transmit(); // only call once, subsequent calls ignored
			NetworkStream DisownSocket(); // take control of the NetworkSocket, used for upgrades, won't be disposed
		}
	}
}

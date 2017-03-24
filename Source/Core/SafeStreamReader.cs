using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpFlare
{
	class SafeStreamReader : StreamReader
	{
		public SafeStreamReader(Stream s) : base(s)
		{
		}
		public SafeStreamReader(Stream s, Encoding e) : base(s, e)
		{
		}

		public async Task<string> SafeReadLine(int max_length = 4096)
		{
			StringBuilder sb = new StringBuilder();
			char[] buf = new char[1];

			while(true)
			{
				int read = await ReadAsync(buf, 0, 1);
				if(!(read > 0))
					break;
				sb.Append(buf[0]);

				if(buf[0] == '\n')
					break;

				if(sb.Length > max_length)
				{
					// https://msdn.microsoft.com/en-us/library/ms229007(v=vs.110).aspx
					throw new InvalidOperationException("read line is too long");
				}
			}

			// handle cases for \r\n
			if(sb.Length > 0)
				if(sb[sb.Length - 1] == '\r')
					sb.Length--;
			return sb.ToString();
		}
	}
}

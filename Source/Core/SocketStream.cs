using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;

namespace SharpFlare
{
	public class SocketStream // buffers the reader
	{
		Socket sock;
		NetworkStream stream;

		public SocketStream(Socket s)
		{
			sock = s;
			stream = new NetworkStream(sock, true);
		}

		public bool Connected { get { return sock.Connected; } }
		public bool DataAvailable { get { return stream.DataAvailable; } }

		const int  PeekBufferSize = 4096;
		byte[]     PeekBuffer = new byte[PeekBufferSize];
		int        PeekLength = 0;     // where the peek buffer has been filled to
		int        PeekPos = 0; // where we read out from
		//int        PeekBufferLen = 0;

		private async Task<int> Peek(int max_length) // returns the number of new bytes this peek
		{
			PeekCompact(false);
			// will fill the buffers with any data availible, and hang if there is no new data
			int read = await stream.ReadAsync(PeekBuffer, PeekLength, max_length - PeekLength);
			PeekLength += read;
			return read;
		}

		private void PeekCompact(bool move = true)
		{
			if(PeekPos == PeekLength) // we're equal to read stuff, reset
			{
				PeekLength = 0;
				PeekPos = 0;
			}
			else if(PeekPos > 0 && move)
			{
				// compact it all down to 0 index
				Buffer.BlockCopy(PeekBuffer, PeekPos, PeekBuffer, 0, PeekLength - PeekPos);
				PeekLength = PeekLength - PeekPos;
				PeekPos = 0;
			}
		}

		public async Task<int> Read(byte[] buffer, int pos, int length)
		{
			bool block = true; // read from real socket only only if data waiting

			int read = 0;

			if(PeekPos < PeekLength) // we can read some on the peek buffer
			{
				// peak the buffer pos
				int readfrombuf = Math.Min(length, PeekLength - PeekPos);
				Buffer.BlockCopy(PeekBuffer, PeekPos, buffer, pos, readfrombuf);
				PeekPos  += readfrombuf;
				pos      += readfrombuf;
				read     += readfrombuf;
				length   -= readfrombuf;
				block = false;
			}

			if(block || sock.Available > 0)
				read += await stream.ReadAsync(buffer, pos, length);
			return read;
		}

		public async Task<int> ReadUntil(byte[] buffer, int pos, int max_length, Func<byte[], int, int> Tester)
		{
			PeekCompact(move: true);
			Debug.Assert(PeekPos == 0);
			Debug.Assert(max_length <= PeekBufferSize);

			// we don't have any lined up, fetch some more
			if(PeekLength == PeekPos)
				await Peek(max_length);

			while(true)
			{
				int index = Tester(PeekBuffer, PeekLength);
				if(index >= 0)
				{
					index++; // 0th place = 1 length
					PeekPos = index;

					if(buffer != null)
						Buffer.BlockCopy(PeekBuffer, 0, buffer, pos, index);
					return index;
				}

				// peek is here because the condition might be satisfied by the already existing buffer...
				await Peek(max_length);
			}
		}

		public async Task<string> ReadLine(int max_length = 1024)
		{
			StringBuilder sb = new StringBuilder();

			int at = 0;
			int lf_count = 0;
			await this.ReadUntil(null, 0, max_length, delegate(byte[] data, int length)
			{
				for(; at < length; at++)
				{
					if(data[at] == (byte)'\n')
						return at;
					else if(data[at] != (byte)'\r')
						sb.Append((char)data[at]);
				}
				return -1;
			});

			return sb.ToString();
		}
	}
}

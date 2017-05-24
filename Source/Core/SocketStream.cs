using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;

namespace SharpFlare
{
	public class SocketStream : IDisposable
	{
		Socket sock;
		NetworkStream stream;

		public SocketStream(Socket s)
		{
			sock = s;
			stream = new NetworkStream(sock, true);
		}
		~SocketStream()
		{
			Dispose(false);
		}

		private bool disposed = false;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				if (disposing)
				{
					// free managed
					stream.Dispose();
				}
				// free unmanaged
				PeekBuffer = null;
			}
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
			if(read <= 0)
				throw new SocketException();
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
			{
				int newread = await stream.ReadAsync(buffer, pos, length);
				if(block && newread <= 0)
					throw new SocketException();
				read += newread;
			}
			return read;
		}

		public async Task<int> ReadUntil(byte[] buffer, int pos, int max_length, Func<byte[], int, int, int> Tester)
		{
			if(max_length > PeekBufferSize)
				throw new ArgumentOutOfRangeException("ReadUntil(): max_length is longer than peek buffer size");
			else if(PeekPos + max_length > PeekBufferSize)
			{
				PeekCompact(move: true);
				Debug.Assert(PeekPos == 0);
				Debug.Assert(max_length <= PeekBufferSize);
			}

			// we don't have any lined up, fetch some more
			if(PeekLength == PeekPos)
				await Peek(max_length);

			while(true)
			{
				int index = Tester(PeekBuffer, PeekPos, PeekLength);
				if(index >= 0)
				{
					index++; // 0th place = 1 length
					PeekPos += index;

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
			byte[] buff = new byte[max_length];

			int frompos = 0;
			int topos = 0;

			await this.ReadUntil(null, 0, max_length, delegate(byte[] data, int offset, int length)
			{
				for(; frompos < length; frompos++)
				{
					if(data[offset + frompos] == (byte)'\n')
						return frompos;
					else if(data[offset + frompos] != (byte)'\r')
					{
						buff[topos] = data[offset + frompos];
						topos++;
					}
				}
				return -1;
			});

			return Encoding.UTF8.GetString(buff, 0, topos);
		}

		public async Task Write(byte[] buffer, int pos, int length)
		{
			await stream.WriteAsync(buffer, pos, length);
		}

		public async Task Write(string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			await this.Write(bytes, 0, bytes.Length);
		}
	}
}

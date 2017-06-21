using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SharpFlare;
using SharpFlare.CLI;
using System.Net;

public class Http1
{
	[Option("Default HTTP/1 port to listen on.", "--default-http1-port")]
	public static int DefaultPort = 80;

	Dictionary<int, Task> IPv4Listeners = new Dictionary<int, Task>();
	Dictionary<int, Task> IPv6Listeners = new Dictionary<int, Task>();

	void HostAdded(string host)
	{
		// TODO: get the port numbers from the hosts.
	}

	public void Load()
	{
		IPv6Listeners[DefaultPort] = Http1Listener.ListenAsync(DefaultPort, IPAddress.IPv6Any);
		IPv4Listeners[DefaultPort] = Http1Listener.ListenAsync(DefaultPort, IPAddress.Any);

		Hooks.Add("NewHost", "HTTP/1.X Listener", async delegate (object[] args)
		{
			HostAdded((string)args[0]);
			return false;
		});
	}

	public void Unload()
	{
	}
}
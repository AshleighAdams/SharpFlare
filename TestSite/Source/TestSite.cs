using SharpFlare;
using SharpFlare.Http;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//[Host("*://*.blah.com/*")]
public class Test
{
	/* Warnings?
	   If you do not need an async call (your page generates synchrnously), the compiler will
	   warn you there is no await present. If you REALLY want to get rid of those warnings, 
	   then remove the async keyword off of the function signuture, and at the end of your
	   function, add: return Task.CompletedTask;
	*/
	[Route("/test_bad")]
	public async Task TestBad(Request req, Response res, string[] args)
	{
		res.Content = await FileAsync.OpenAsync(@"/etc/hosts", FileMode.Open);
	}
	[Route("/test_good")]
	public async Task TestGood(Request req, Response res, string[] args)
	{
		res.Content = new MemoryStream(Encoding.UTF8.GetBytes("Plugin says hello"));
	}

	[Route("/test_file.bin")]
	public async Task TestFile(Request req, Response res, string[] args)
	{
		res["Content-Type"] = "binary/octet-stream";
		res.Content = await FileAsync.OpenReadAsync(@"D:\Ashleigh\Downloads\Long Term\A.Ghost.Story.2017.1080p.BRRip.6CH.MkvCage.mkv");
	}

	[Route("/test/([a-zA-Z0-9 ]+)")]
	public async Task TestPattern(Request req, Response res, string[] args)
	{
		StringBuilder sb = new StringBuilder();

		sb.Append("<html>\n");
		foreach(string arg in args)
			sb.Append($"<div>{arg}</div>\n");
		sb.Append("</html>\n");

		res["Content-Type"] = "text/html";
		res.Content = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
	}

	public void Load()
	{
		SharpFlare.Logger.GlobalLogger.Message(SharpFlare.Logger.Level.Normal, "Hello plugin");
	}

	public void Unload()
	{
		SharpFlare.Logger.GlobalLogger.Message(SharpFlare.Logger.Level.Normal, "Bye plugin");
	}

	// very simple proxy, doesn't handle anything other than GET, and doesn't rewrite urls
	[Route(@"/highlight/(.+)")]
	public async Task TestHighlight(Request reqds, Response resds, string[] args)
	{
		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(args[1]);
		
		req.UserAgent = reqds["User-Agent"];
		WebResponse res = await req.GetResponseAsync();

		using (var x = new StreamReader(res.GetResponseStream()))
		{
			string str = await x.ReadToEndAsync();

			str = Regex.Replace(str, "", "");

			resds["Content-Type"] = res.ContentType;
			resds.Content = new MemoryStream(Encoding.UTF8.GetBytes(str));
		}
	}

}
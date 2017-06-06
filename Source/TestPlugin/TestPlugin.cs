using SharpFlare;
using SharpFlare.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

//[Host("*://*.blah.com/*")]
public class TestPlugin
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

	public void Load()
	{
		SharpFlare.Logger.GlobalLogger.Message(SharpFlare.Logger.Level.Normal, "Hello plugin");
	}

	public void Unload()
	{
		SharpFlare.Logger.GlobalLogger.Message(SharpFlare.Logger.Level.Normal, "Bye plugin");
	}
}
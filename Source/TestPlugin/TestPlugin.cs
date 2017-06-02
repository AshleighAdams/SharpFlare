using SharpFlare;
using SharpFlare.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

//[Host("*://*.blah.com/*")]
public class TestPlugin
{
	[Route("/test_bad")]
	public async Task TestBad(Request req, Response res, string[] args)
	{
		res.Content = new FileStream(@"C:\Windows\System32\drivers\etc\hosts", FileMode.Open);
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
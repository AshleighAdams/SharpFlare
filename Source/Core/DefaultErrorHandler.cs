using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using SharpFlare.Http;

namespace SharpFlare
{
	public static partial class DefaultErrorHandler
	{
		static Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>
		{
			// todo: when escaping path, %20 -> space
			["/sharpflare/Proxima%20Nova-Regular.otf"] = Convert.FromBase64String(ProximaNovaRegular_otf_base64),
			["/sharpflare/space.png"] = Convert.FromBase64String(space_png_base64),
			["/sharpflare/moon.png"] = Convert.FromBase64String(moon_png_base64),
			["/sharpflare/flare.png"] = Convert.FromBase64String(flare_png_base64),
			["/sharpflare/mountain-silhouette.png"] = Convert.FromBase64String(mountain_silhouette_png_base64)
		};

		static Dictionary<string, string> ContentType = new Dictionary<string, string>
		{
			["/sharpflare/Proxima%20Nova-Regular.otf"] = "application/font-sfnt",
			["/sharpflare/space.png"] = "image/png",
			["/sharpflare/moon.png"] = "image/png",
			["/sharpflare/flare.png"] = "image/png",
			["/sharpflare/mountain-silhouette.png"] = "image/png"
		};

		public static async Task SendErrorFile(Request req, Response r, string[] args)
		{
			r["Content-Type"] = ContentType[req.Path];
			r.Content = new MemoryStream(Files[req.Path]);
			await r.Finalize();
		}

		public static void Setup()
		{
			Hooks.Hook.Add("Error", "Default Error", HandleError);
			Router.Host.Any.Route("/sharpflare/Proxima%20Nova-Regular.otf", SendErrorFile);
			Router.Host.Any.Route("/sharpflare/space.png", SendErrorFile);
			Router.Host.Any.Route("/sharpflare/moon.png", SendErrorFile);
			Router.Host.Any.Route("/sharpflare/flare.png", SendErrorFile);
			Router.Host.Any.Route("/sharpflare/mountain-silhouette.png", SendErrorFile);
		}
		
		public static async Task<bool> HandleError(params object[] args)
		{
			Request req = (Request)args[0];
			Response res = (Response)args[1];
			HttpException ex = (HttpException)args[2];

			string title, msg, stack;
			title = $"{ex.HttpStatus.code} {ex.HttpStatus.message}";
			msg = ex.Message;
			stack = "";

			int atm_r = 140, atm_g = 0, atm_b = 255;
			int sky_r = 0,   sky_g = 0,   sky_b = 50;

			if (ex.HttpStatus.code >= 500 && ex.HttpStatus.code <= 599) // only show a stack trace for server errors
			{
				atm_r = 255; atm_g = 0; atm_b = 255;
				sky_r = 0; sky_g = 0; sky_b = 0;
			}
			if(ex.InnerException != null) // this was an unhandled exception that was caught, show the stack trace
			{
				Exception inner = ex;
				while (inner.InnerException != null)
					inner = inner.InnerException;

				msg = (inner.GetType().Name + ": " + inner.Message);

				if (inner.StackTrace != null)
					stack = Util.CleanAsyncStackTrace(inner.StackTrace).Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br />");
				else
					stack = "<i>Stacktrace not found.</i>";
			}
			

			string html =
$@"<html>
	<head>
		<title>{title}</title>
		<meta name=""viewport"" content=""width=820"">
		<style>
			@font-face
			{{
				font-family: ProximaNovaCond;
				src: url(""/sharpflare/Proxima Nova-Regular.otf"") format(""opentype"");
			}}
			body
			{{
				margin: 0;
				background: black; /* what to show after the mountains, should always be black */
				color: white;
				font-family: ""ProximaNovaCond"", sans-serif;
				position: relative;
				min-width: 800px;
				overflow-wrap: break-word;
				-webkit-text-size-adjust: none;
			}}
			html
			{{
				font-size: 100%;
			}}
			@media(max-width:60em)
			{{
				html
				{{
					font-size: 200%;
				}}
			}}
			div.wrapper
			{{
				width: 800px;
				margin: auto auto;
				padding-top: 10vh;
			}}
			h1
			{{
				font-size: 150%;
			}}
			p.code
			{{
				font-size: 100%;
				font-family: monospace;
			}}
			div.mountains
			{{
				width: 100%;
				min-height: 100px;
				max-height: 344px;
				overflow: hidden;
				background-image: url(""/sharpflare/mountain-silhouette.png"");
				background-repeat: repeat-x;
				background-position: center top 0px;
			}}
			div.atmosphere
			{{
				width: 100%; /* rgba(150,50,0,1) */
				background: radial-gradient(1000px 800px at bottom, rgba({atm_r},{atm_g},{atm_b},0.5), rgba({sky_r},{sky_g},{sky_b},0.10) );
				z-index:-1;
			}}
			div.flarecont
			{{
				max-height: 100vh;
				height: 800px;
				width: 100%;
				position: absolute;
				bottom: 0;
				overflow: hidden;
				pointer-events: none;
			}}
			@keyframes spin
			{{
				from {{ transform: rotate(0deg);   }}
				to   {{ transform: rotate(360deg); }}
			}}
			@media(min-width:60em)
			{{
				div.flare
				{{
					animation: spin 60s infinite linear;
				}}
			}}
			div.flare
			{{
				background: url(/sharpflare/flare.png);
				background-size: cover;
				width: 1600px;
				height: 1600px;
				position: absolute;
				left: calc(50% - 1600px/2);
				/*top: calc(1600px/8);*/
				filter: blur(20px);
				opacity: 0.2;
				z-index: -99;
			}}
			div.space
			{{
				background-image: url(""/sharpflare/space.png"");
				z-index: -2;
			}}
			div.background
			{{
				width: 100%;
				height: 100%;
				position: absolute;
				background: transparent;
				z-index:-1;
			}}
			div.moon
			{{
				width: 128px;
				height: 128px;
				top: 40px;
				left: 50%;
				position: relative;
				background-image: url(""/sharpflare/moon.png"");
				background-size: 100% 100%;
				z-index:-1;

				opacity: 0;
			}}
			div.flex
			{{
				display: flex;
				flex-direction: column;
				min-height: 100vh;
			}}
			div.flexgrow
			{{
				flex-grow: 1;
			}}
		</style>
	</head>
	<body>
		<div class=space>
			<div class=flarecont>
				<div class=flare id=flare>
				</div>
			</div>
			<div class=atmosphere>
				<div class=flex>
					<div class=wrapper>
						<center>
							<h1>{title}</h1>
							<p>{msg}</p>
						</center>
						<p class=code style=""display: table; margin: 0 auto;"">
							{stack}
						</p>
					</div>
					<div class=""mountains flexgrow"">
						<div class=moon id = moon onload=""rotate_moon();"">
						</div>
					</div>
				</div>
			</div>
		</div>
	</body>
</html>
";
			res["Content-Type"] = "text/html";
			res.Content = new MemoryStream(Encoding.UTF8.GetBytes(html));
			await res.Finalize();

			return false;
		}
	}
}
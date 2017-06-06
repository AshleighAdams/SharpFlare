using System.Collections.Generic;


namespace SharpFlare
{
	namespace Http
	{
		public class Status
		{
			public readonly int code;
			public readonly string message;

			static Dictionary<int, Status> Statuses = new Dictionary<int, Status>();

			public Status(int c, string m)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					code = c;
					message = m;
					Statuses[c] = this;
				}
			}

			public static implicit operator Status(int code)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					Status ret;
					if (Statuses.TryGetValue(code, out ret))
						return ret;

					Logger.GlobalLogger.Message(SharpFlare.Logger.Level.Warning, $"Unknown HTTP status code {code} used");
					return new Status(code, "");
				}
			}

			public static Status
				Continue                      = new Status(100, "Continue"),
				SwitchingProtocols            = new Status(101, "Switching Protocols"),
				Processing                    = new Status(102, "Processing"),

				Okay                          = new Status(200, "Okay"),
				Created                       = new Status(201, "Created"),
				Accepted                      = new Status(202, "Accepted"),
				NonAuthoritativeInformation   = new Status(203, "Non-Authoritative Information"),
				NoContent                     = new Status(204, "No Content"),
				ResetContent                  = new Status(205, "Reset Content"),
				PartialContent                = new Status(206, "Partial Content"),
				MultiStatus                   = new Status(207, "Multi-Status"),
				AlreadyReported               = new Status(208, "Already Reported"),

				IMUsed                        = new Status(226, "IM Used"),

				MultipipleChoices             = new Status(300, "Multipiple Choices"),
				MovedPermanently              = new Status(301, "Moved Permanently"),
				Found                         = new Status(302, "Found"),
				SeeOther                      = new Status(303, "See Other"),
				NotModified                   = new Status(304, "Not Modified"),
				UseProxy                      = new Status(305, "Use Proxy"),
				TemporaryRedirect             = new Status(307, "Temporary Redirect"),
				PermanentRedirect             = new Status(308, "Permanent Redirect"),

				BadRequest                    = new Status(400, "Bad Request"),
				Unauthorized                  = new Status(401, "Unauthorized"),
				PaymentRequired               = new Status(402, "Payment Required"),
				Forbidden                     = new Status(403, "Forbidden"),
				NotFound                      = new Status(404, "Not Found"),
				MethodNotAllowed              = new Status(405, "Method Not Allowed"),
				NotAcceptable                 = new Status(406, "Not Acceptable"),
				ProxyAuthenticationRequred    = new Status(407, "Proxy Authentication Requred"),
				RequestTimeout                = new Status(408, "Request Timeout"),
				Conflict                      = new Status(409, "Conflict"),
				Gone                          = new Status(410, "Gone"),
				LengthRequired                = new Status(411, "Length Required"),
				PreconditionFailed            = new Status(412, "Precondition Failed"),
				RequestEntityTooLarge         = new Status(413, "Request Entity Too Large"),
				RequestUriTooLong             = new Status(414, "Request-URI Too Long"),
				UnsupportedMediaType          = new Status(415, "Unsupported Media Type"),
				RequestedRangeNotSatisfiable  = new Status(416, "Requested Range Not Satisfiable"),
				ExpectationFailed             = new Status(417, "Expectation Failed"),
				ImATeapot                     = new Status(418, "I'm A Teapot"),

				EnhanceYourCalm               = new Status(420, "Enhance Your Calm"),

				UnprocessableEntity           = new Status(422, "Unprocessable Entity"),
				Locked                        = new Status(423, "Locked"),
				FailedDependency              = new Status(424, "Failed Dependency"),

				UpgradeRequred                = new Status(426, "Upgrade Requred"),

				PreconditionRequred           = new Status(428, "Precondition Requred"),
				TooManyRequests               = new Status(429, "Too Many Requests"),

				RequestHeaderFieldsTooLarge   = new Status(431, "Request Header Fields Too Large"),

				UnavailableForLegalReasons    = new Status(451, "Unavailable For Legal Reasons"),

				InternalServerError           = new Status(500, "Internal Server Error"),
				NotImplemented                = new Status(501, "Not Implemented"),
				BadGateway                    = new Status(502, "Bad Gateway"),
				ServiceUnavailable            = new Status(503, "Service Unavailable"),
				GatewayTimeout                = new Status(504, "Gateway Timeout"),
				HttpVersionNotSupported       = new Status(505, "HTTP Version Not Supported"),
				VariantAlsoNegotiates         = new Status(506, "Variant Also Negotiates"),
				InsufficentStorage            = new Status(507, "Insufficent Storage"),
				LoopDetected                  = new Status(508, "Loop Detected"),
				NotExtended                   = new Status(510, "Not Extended"),
				NetworkAuthenticationRequred  = new Status(511, "Network Authentication Requred");
		}
	}
}
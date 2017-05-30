using System;

namespace SharpFlare
{
	public static class Util
	{
		//                       0th   1st   2nd   3rd   4th   5th   6th   7th   8th   9th
		static string[] nths = { "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th" };
		public static string Nth(int n)
		{
			int mod100 = n % 100;

			if(mod100 >= 11 && mod100 <= 13)
				return nths[0];
			return nths[mod100 % 10];
		}

		public static string ToHttpDate(this DateTime when)
		{
			//  Sun, 06 Nov 1994 08:49:37 GMT
			DateTime utc = when.ToUniversalTime();
			return utc.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
		}
	}
}


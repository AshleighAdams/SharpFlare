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
	}
}


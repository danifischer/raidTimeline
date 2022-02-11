using System.Collections.Generic;

namespace raidTimeline.Logic.Helper
{
	internal static class FluentMethods
	{
		internal static int GetOrDefault(this Dictionary<string, int> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : 0;
		}
	}
}
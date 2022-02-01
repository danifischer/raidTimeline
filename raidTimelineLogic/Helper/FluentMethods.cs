using System.Collections.Generic;

namespace raidTimelineLogic.Helper
{
	internal static class FluentMethods
	{
		internal static int GetOrDefault(this Dictionary<string, int> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : 0;
		}
	}
}
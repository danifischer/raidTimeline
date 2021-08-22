using System.Collections.Generic;

namespace raidTimelineLogic.Helper
{
	public static class FluentMethods
	{
		public static int GetOrDefault(this Dictionary<string, int> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : 0;
		}
	}
}
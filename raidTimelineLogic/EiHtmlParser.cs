using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace raidTimelineLogic
{
	internal class EiHtmlParser
	{
		internal RaidModel ParseLog(string path, string file)
		{
			var model = new RaidModel(file, path);
			var encounter = File.ReadAllText(model.LogPath);

			dynamic logData = GetLogData(encounter);

			SetRemainingHealth(model, logData);
			SetGeneralInformation(model, logData);
			SetTime(i => model.OccurenceStart = i, encounter, "Time Start: ");
			SetTime(i => model.OccurenceEnd = i, encounter, "Time End: ");
			
			return model;
		}

		private static void SetTime(Action<DateTime> setAction, string encounter, string startString)
		{
			var indexStart = encounter.IndexOf(startString);
			var indexEnd = encounter.IndexOf(" +", indexStart);
			var encounterStart = encounter.Substring(indexStart + startString.Length, indexEnd - (indexStart + startString.Length));

			setAction(DateTime.ParseExact(encounterStart
					, "yyyy-MM-dd HH:mm:ss"
					, CultureInfo.InvariantCulture));
		}

		private static void SetGeneralInformation(RaidModel model, dynamic logData)
		{
			model.EncounterTime = logData.encounterDuration.Value;
			model.EncounterIcon = logData.fightIcon.Value;
			model.EncounterName = logData.fightName.Value;
			model.Killed = logData.success.Value;
		}

		private static void SetRemainingHealth(RaidModel model, dynamic logData)
		{
			for (int i = 0; i < logData.phases[0].targets.Count; i++)
			{
				var targetData = logData.targets[(int)logData.phases[0].targets[i]];
				model.HpLeft.Add(targetData.hpLeft.Value);
			}
		}

		private static dynamic GetLogData(string encounter)
		{
			var indexStart = encounter.IndexOf("var logData = ");
			var indexEnd = encounter.IndexOf("</script>", indexStart);
			var json = encounter.Substring(indexStart + 14, indexEnd - (indexStart + 14));
			var semiColon = json.LastIndexOf(";");
			json = json.Substring(0, semiColon);
			return (dynamic)JsonConvert.DeserializeObject(json);
		}
	}
}


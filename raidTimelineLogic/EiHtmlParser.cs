using Newtonsoft.Json;
using raidTimelineLogic.Models;
using System;
using System.Globalization;
using System.IO;

namespace raidTimelineLogic
{
	internal class EiHtmlParser
	{
		internal RaidModel ParseLog(string filePath)
		{
			var model = new RaidModel(filePath);
			var encounter = File.ReadAllText(model.LogPath);

			dynamic logData = GetLogData(encounter);

			SetRemainingHealth(model, logData);
			SetGeneralInformation(model, logData);
			SetTime(i => model.OccurenceStart = i, encounter, "Time Start: ");
			SetTime(i => model.OccurenceEnd = i, encounter, "Time End: ");

			var fightDuration = (long)logData.phases[0].duration.Value;
	
			foreach(var player in logData.players)
			{
				var playerModel = new PlayerModel();
				var targets = player.details.dmgDistributionsTargets[0];
				playerModel.Damage = (long)targets[0].totalDamage.Value;
				playerModel.AccountName = player.acc;
				playerModel.Dps = playerModel.Damage * 1000 / fightDuration;
				model.Players.Add(playerModel);
			}

			return model;
		}

		private static void SetTime(Action<DateTime> setAction, string encounter, string startString)
		{
			var indexStart = encounter.IndexOf(startString);
			var indexEnd = encounter.IndexOf(" ", indexStart + startString.Length + 19);
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
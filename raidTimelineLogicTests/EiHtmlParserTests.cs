using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using raidTimelineLogic;
using raidTimelineLogic.Models;
using System;
using System.Globalization;
using System.Linq;

namespace raidTimelineLogicTests
{
	[TestClass]
	public class EiHtmlParserTests
	{
		private const string ExpectedEncounterName = "Vale Guardian";
		private const string ExpectedEncounterTime = "03m 59s 857ms";
		private const string ExpectedEncounterIcon = "https://wiki.guildwars2.com/images/f/fb/Mini_Vale_Guardian.png";
		private const bool ExpectedKilled = true;
		private const int ExpectedNumberOfHpLeft = 1;
		private const double ExpectedHpLeft = 0;
		private const int ExpectedPlayers = 10;
		private readonly DateTime ExpectedOccurenceStart = DateTime.ParseExact("2020-08-01 21:15:19",
			"yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		private readonly DateTime ExpectedOccurenceEnd = DateTime.ParseExact("2020-08-01 21:19:21",
			"yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

		[DataTestMethod]
		[DataRow(@"Files\version_2_25.html", DisplayName = "Version 2.25.")]
		[DataRow(@"Files\version_2_26.html", DisplayName = "Version 2.26.")]
		public void ParseLog_Default_HasExpectedValues(string path)
		{
			var parser = new EiHtmlParser();

			var log = parser.ParseLog(path);
			
			AssertExpectations(log);
		}

		private void AssertExpectations(RaidModel log)
		{
			log.EncounterName.Should().Be(ExpectedEncounterName);
			log.EncounterTime.Should().Be(ExpectedEncounterTime);
			log.EncounterIcon.Should().Be(ExpectedEncounterIcon);
			log.Killed.Should().Be(ExpectedKilled);
			log.HpLeft.Count().Should().Be(ExpectedNumberOfHpLeft);
			log.HpLeft[0].Should().Be(ExpectedHpLeft);
			log.Players.Count().Should().Be(ExpectedPlayers);
			log.OccurenceStart.Should().Be(ExpectedOccurenceStart);
			log.OccurenceEnd.Should().Be(ExpectedOccurenceEnd);
		}
	}
}

using FluentAssertions;
using raidTimeline.Logic.Models;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace raidTimeline.Logic.Tests
{
	public class EiHtmlParserTests
	{
		private const string ExpectedEncounterName = "Soulless Horror";
		private const string ExpectedEncounterTime = "03m 57s 362ms";
		private const string ExpectedEncounterIcon = "https://wiki.guildwars2.com/images/d/d4/Mini_Desmina.png";
		private const bool ExpectedKilled = true;
		private const int ExpectedNumberOfHpLeft = 1;
		private const double ExpectedHpLeft = 0;
		private const int ExpectedPlayers = 10;
		private readonly DateTime _expectedOccurenceStart = DateTime.ParseExact("2021-07-15 20:14:12 +02:00",
			"yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
		private readonly DateTime _expectedOccurenceEnd = DateTime.ParseExact("2021-07-15 20:18:22 +02:00",
			"yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

		[Theory]
		[InlineData(@"Files\parsed_test_log.html")]
		public void ParseLog_Default_HasExpectedValues(string path)
		
		{
			var log = Logic.EiHtmlParser.ParseLog(path);
			
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
			log.OccurenceStart.Should().Be(_expectedOccurenceStart);
			log.OccurenceEnd.Should().Be(_expectedOccurenceEnd);
		}
	}
}

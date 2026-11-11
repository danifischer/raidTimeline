#nullable enable
using System.Collections.Generic;

namespace raidTimeline.Logic.EiJson
{
    // Minimal read model of the Elite Insights JSON output (https://baaron4.github.io/GW2-Elite-Insights-Parser/Json/).
    // Only the properties raidTimeline needs are mapped; everything else in the (large) file is skipped by the
    // deserializer. Property names are matched case-insensitively against the camelCase json.

    internal sealed class EiLog
    {
        public string EliteInsightsVersion { get; set; } = string.Empty;
        public int TriggerID { get; set; }
        public string FightName { get; set; } = string.Empty;
        public string FightIcon { get; set; } = string.Empty;
        public string TimeStartStd { get; set; } = string.Empty;
        public string TimeEndStd { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public long DurationMS { get; set; }
        public bool Success { get; set; }
        public List<EiTarget> Targets { get; set; } = new();
        public List<EiPlayer> Players { get; set; } = new();
        public List<EiPhase> Phases { get; set; } = new();
        public List<EiMechanic> Mechanics { get; set; } = new();
        public Dictionary<string, EiBuffDescription> BuffMap { get; set; } = new();
        public List<string> UploadLinks { get; set; } = new();
    }

    internal sealed class EiTarget
    {
        public string Name { get; set; } = string.Empty;
        public double HealthPercentBurned { get; set; }
    }

    internal sealed class EiPhase
    {
        public List<int> Targets { get; set; } = new();
        public Dictionary<string, string> TargetPriorities { get; set; } = new();
    }

    internal sealed class EiPlayer
    {
        /// <summary>Account name, e.g. "Name.1234".</summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>Character name. Mechanic events reference players by this name.</summary>
        public string Name { get; set; } = string.Empty;

        public int Group { get; set; }
        public string Profession { get; set; } = string.Empty;
        public bool NotInSquad { get; set; }

        /// <summary>Per target, per phase.</summary>
        public List<List<EiDps>> DpsTargets { get; set; } = new();

        /// <summary>Per phase.</summary>
        public List<EiDps> DpsAll { get; set; } = new();

        /// <summary>Per phase.</summary>
        public List<EiSupport> Support { get; set; } = new();

        /// <summary>Per phase.</summary>
        public List<EiDefenses> Defenses { get; set; } = new();

        public List<EiBuffUptime> BuffUptimes { get; set; } = new();
    }

    internal sealed class EiDps
    {
        public long Damage { get; set; }
        public double BreakbarDamage { get; set; }
    }

    internal sealed class EiSupport
    {
        public int Resurrects { get; set; }
        public double ResurrectTime { get; set; }
    }

    internal sealed class EiDefenses
    {
        public int DownCount { get; set; }
        public int DeadCount { get; set; }
    }

    internal sealed class EiBuffUptime
    {
        public int Id { get; set; }

        /// <summary>Per phase.</summary>
        public List<EiBuffData> BuffData { get; set; } = new();
    }

    internal sealed class EiBuffData
    {
        /// <summary>Uptime in percent, or the average stack count for stacking buffs.</summary>
        public double Uptime { get; set; }

        /// <summary>Percentage of time at least one stack was present (stacking buffs only).</summary>
        public double Presence { get; set; }
    }

    internal sealed class EiBuffDescription
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public bool Stacking { get; set; }
    }

    internal sealed class EiMechanic
    {
        /// <summary>Short name, e.g. "Donut In".</summary>
        public string Name { get; set; } = string.Empty;

        public List<EiMechanicEvent> MechanicsData { get; set; } = new();
    }

    internal sealed class EiMechanicEvent
    {
        /// <summary>Character name of the actor the mechanic was triggered on.</summary>
        public string Actor { get; set; } = string.Empty;
    }
}

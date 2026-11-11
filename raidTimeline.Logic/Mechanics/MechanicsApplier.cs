#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using raidTimeline.Logic.EiJson;
using raidTimeline.Models.Models;

namespace raidTimeline.Logic.Mechanics
{
    /// <summary>
    /// Fills <see cref="PlayerModel.Mechanics"/> (how often every Elite Insights mechanic hit the player) and the
    /// columns of the mechanics table (<see cref="RaidModel.MechanicColumns"/>, <see cref="PlayerModel.CombinedMechanics"/>).
    /// </summary>
    internal static class MechanicsApplier
    {
        private const int GenericColumns = 3;

        internal static void Apply(RaidModel model, EiLog log, MechanicsCatalog catalog)
        {
            // mechanic name -> character name -> number of times
            var counts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            foreach (var mechanic in log.Mechanics)
            {
                if (!counts.TryGetValue(mechanic.Name, out var perActor))
                {
                    counts[mechanic.Name] = perActor = new Dictionary<string, int>(StringComparer.Ordinal);
                }

                foreach (var mechanicEvent in mechanic.MechanicsData)
                {
                    perActor[mechanicEvent.Actor] = perActor.GetValueOrDefault(mechanicEvent.Actor) + 1;
                }
            }

            int Count(PlayerModel player, string mechanic) =>
                counts.TryGetValue(mechanic, out var perActor)
                    ? perActor.GetValueOrDefault(log.Players[player.Index].Name)
                    : 0;

            foreach (var player in model.Players)
            {
                foreach (var name in counts.Keys)
                {
                    player.Mechanics[name] = Count(player, name);
                }
            }

            var definition = catalog.Find(log.TriggerID);
            var columns = definition != null
                ? definition.Columns.Select(c => (Column: new MechanicColumn(c.Key, c.Label, c.Title, c.Rank), Definition: c))
                    .ToList()
                : GenericColumnsFor(model, log, catalog, counts);

            foreach (var (column, source) in columns)
            {
                model.MechanicColumns.Add(column);
                foreach (var player in model.Players)
                {
                    player.CombinedMechanics[column.Key] =
                        source.Mechanics.Sum(m => Count(player, m)) - source.Subtract.Sum(m => Count(player, m));
                }
            }
        }

        /// <summary>
        /// Encounters without a definition (strikes, fractals, new content) show the mechanics players triggered most.
        /// </summary>
        private static List<(MechanicColumn Column, MechanicsCatalog.ColumnDefinition Definition)> GenericColumnsFor(
            RaidModel model, EiLog log, MechanicsCatalog catalog, Dictionary<string, Dictionary<string, int>> counts)
        {
            var characters = model.Players.Select(p => log.Players[p.Index].Name).ToHashSet(StringComparer.Ordinal);

            return log.Mechanics
                .GroupBy(m => m.Name, StringComparer.Ordinal)
                .Where(g => !catalog.IgnoredMechanics.Contains(g.Key))
                .Select(g => (Mechanic: g.First(), Total: g.Sum(m => m.MechanicsData.Count(e => characters.Contains(e.Actor)))))
                .Where(x => x.Total > 0)
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Mechanic.Name, StringComparer.Ordinal)
                .Take(GenericColumns)
                .Select(x =>
                {
                    var title = string.IsNullOrWhiteSpace(x.Mechanic.Description)
                        ? x.Mechanic.FullName
                        : x.Mechanic.Description;
                    var definition = new MechanicsCatalog.ColumnDefinition
                    {
                        Key = "gen_" + x.Mechanic.Name,
                        Label = x.Mechanic.Name,
                        Title = title,
                        Mechanics = { x.Mechanic.Name }
                    };
                    return (new MechanicColumn(definition.Key, definition.Label, definition.Title), definition);
                })
                .ToList();
        }
    }
}

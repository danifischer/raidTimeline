#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace raidTimeline.Logic.Mechanics
{
    /// <summary>
    /// The boss specific columns of the mechanics table, read from <c>Mechanics/mechanics.json</c>
    /// (embedded resource). Adding an encounter or renaming a mechanic only needs a change in that file.
    /// </summary>
    internal sealed class MechanicsCatalog
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Lazy<MechanicsCatalog> DefaultCatalog = new(LoadEmbedded);

        private readonly Dictionary<int, EncounterDefinition> _byId = new();

        private MechanicsCatalog(Document document)
        {
            Encounters = document.Encounters;
            IgnoredMechanics = new HashSet<string>(document.IgnoredMechanics, StringComparer.Ordinal);

            foreach (var encounter in Encounters)
            {
                foreach (var id in encounter.Ids)
                {
                    if (!_byId.TryAdd(id, encounter))
                    {
                        throw new InvalidDataException(
                            $"Encounter id {id} is used by '{_byId[id].Name}' and '{encounter.Name}'.");
                    }
                }
            }
        }

        /// <summary>The catalog shipped with the library.</summary>
        internal static MechanicsCatalog Default => DefaultCatalog.Value;

        internal IReadOnlyList<EncounterDefinition> Encounters { get; }

        /// <summary>Mechanics every encounter has (downed, missed quickness, ...), not used for unknown encounters.</summary>
        internal IReadOnlySet<string> IgnoredMechanics { get; }

        internal EncounterDefinition? Find(int encounterId) => _byId.GetValueOrDefault(encounterId);

        /// <exception cref="InvalidDataException">The json is not a valid catalog.</exception>
        internal static MechanicsCatalog Parse(string json)
        {
            Document? document;
            try
            {
                document = JsonSerializer.Deserialize<Document>(json, Options);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException("The mechanics definition is not valid json: " + e.Message, e);
            }

            if (document == null)
            {
                throw new InvalidDataException("The mechanics definition is empty.");
            }

            Validate(document);
            return new MechanicsCatalog(document);
        }

        private static MechanicsCatalog LoadEmbedded()
        {
            var assembly = typeof(MechanicsCatalog).Assembly;
            var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith("mechanics.json"));
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            return Parse(reader.ReadToEnd());
        }

        private static void Validate(Document document)
        {
            foreach (var encounter in document.Encounters)
            {
                var name = string.IsNullOrWhiteSpace(encounter.Name) ? "(no name)" : encounter.Name;

                if (encounter.Ids.Count == 0)
                    throw new InvalidDataException($"Encounter '{name}' has no ids.");
                if (encounter.Columns.Count == 0)
                    throw new InvalidDataException($"Encounter '{name}' has no columns.");
                if (!encounter.Columns.Any(c => c.Rank))
                    throw new InvalidDataException($"Encounter '{name}' has no column which is used for the ranking.");

                var keys = new HashSet<string>();
                foreach (var column in encounter.Columns)
                {
                    if (string.IsNullOrWhiteSpace(column.Key) || !keys.Add(column.Key))
                        throw new InvalidDataException($"Encounter '{name}' has an empty or duplicate column key '{column.Key}'.");
                    if (string.IsNullOrWhiteSpace(column.Label))
                        throw new InvalidDataException($"Column '{column.Key}' of '{name}' has no label.");
                    if (column.Mechanics.Count == 0)
                        throw new InvalidDataException($"Column '{column.Key}' of '{name}' has no mechanics.");
                }
            }
        }

        internal sealed class Document
        {
            public List<string> IgnoredMechanics { get; set; } = new();
            public List<EncounterDefinition> Encounters { get; set; } = new();
        }

        internal sealed class EncounterDefinition
        {
            public string Name { get; set; } = string.Empty;

            /// <summary>Elite Insights trigger ids of the encounter.</summary>
            public List<int> Ids { get; set; } = new();

            public List<ColumnDefinition> Columns { get; set; } = new();
        }

        internal sealed class ColumnDefinition
        {
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;

            /// <summary>Short names of the Elite Insights mechanics which are added up.</summary>
            public List<string> Mechanics { get; set; } = new();

            /// <summary>Short names of mechanics which are subtracted (e.g. hits that were stabilized).</summary>
            public List<string> Subtract { get; set; } = new();

            /// <summary>False for columns that are no fails (e.g. collected tears), they are shown but not ranked.</summary>
            public bool Rank { get; set; } = true;
        }
    }
}

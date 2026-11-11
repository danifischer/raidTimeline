#nullable enable
using System.IO;
using raidTimeline.Logic.Mechanics;

namespace raidTimeline.Logic
{
    /// <summary>
    /// Personal additions to the boss definitions of <c>mechanics.json</c>, e.g. for a new encounter or after
    /// Elite Insights renamed a mechanic. The file has the same format as the shipped one; an encounter in it replaces
    /// the shipped encounter with the same id.
    /// </summary>
    public static class CustomMechanics
    {
        /// <summary>The content of a new file.</summary>
        public const string Template = """
            {
              // Your own boss definitions. They have the format of raidTimeline.Logic/Mechanics/mechanics.json
              // (see "Boss mechanics" in the README). An encounter here replaces the shipped one with the same id.
              // Example:
              // "encounters": [
              //   {
              //     "name": "My Boss",
              //     "ids": [12345],
              //     "columns": [
              //       { "key": "my_slam", "label": "Slam", "title": "Hit by the slam", "mechanics": ["Slam"] }
              //     ]
              //   }
              // ]
              "ignoredMechanics": [],
              "encounters": []
            }
            """;

        /// <summary>
        /// Uses the definitions of the file for everything that is parsed from now on.
        /// </summary>
        /// <returns>False if the file does not exist (nothing changes).</returns>
        /// <exception cref="InvalidDataException">The file is not a valid definition.</exception>
        public static bool Load(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            MechanicsCatalog.Custom = MechanicsCatalog.Shipped.WithOverrides(MechanicsCatalog.Parse(File.ReadAllText(path)));
            return true;
        }
    }
}

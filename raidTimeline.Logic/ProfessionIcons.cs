#nullable enable
using System.Collections.Generic;

namespace raidTimeline.Logic
{
    /// <summary>Profession and elite specialization icons (English names as used by Elite Insights).</summary>
    /// <remarks>Generated from https://api.guildwars2.com/v2/specializations and /v2/professions. Add new elite specializations here.</remarks>
    internal static class ProfessionIcons
    {
        private static readonly Dictionary<string, string> Icons = new()
        {
            ["Amalgam"] = "https://render.guildwars2.com/file/0AF363EC11D1A4B6E507F8D92FA116619B1B3B0E/3680065.png",
            ["Antiquary"] = "https://render.guildwars2.com/file/411B80D69C4324E84D9C050955B0040F66D7DFA1/3680089.png",
            ["Berserker"] = "https://render.guildwars2.com/file/3111C4ACA223F8B654B3453A42F7539D64EE953A/1128567.png",
            ["Bladesworn"] = "https://render.guildwars2.com/file/F73D52559DEA5FC92CD4FAF2557E050BC1516A7C/2491565.png",
            ["Catalyst"] = "https://render.guildwars2.com/file/220F40016450052CAEB4E5BC7A5A9A6078F0F124/2491557.png",
            ["Chronomancer"] = "https://render.guildwars2.com/file/B0A3C5B4097705AE21E9CB78E53045E327F1C7C9/1128569.png",
            ["Conduit"] = "https://render.guildwars2.com/file/A0449B6C2253DB48E0B60E3AA9D55AF130ED2528/3680085.png",
            ["Daredevil"] = "https://render.guildwars2.com/file/CEA6755F350FAAE9A7D8796CF8CC10FA1E33081D/1128571.png",
            ["Deadeye"] = "https://render.guildwars2.com/file/E4CDA974AF47D2336E02211E4667FE5C9579774F/1770213.png",
            ["Dragonhunter"] = "https://render.guildwars2.com/file/4161C630AA50667FDCBF01042D07021CE44451C9/1128573.png",
            ["Druid"] = "https://render.guildwars2.com/file/033CFD3270277F38215AE60574BCC2000873BDFF/1128575.png",
            ["Elementalist"] = "https://render.guildwars2.com/file/77B793123251931AFF9FCA24C07E0F704BC4DA49/156630.png",
            ["Engineer"] = "https://render.guildwars2.com/file/5CCB361F44CCC7256132405D31E3A24DACCF440A/156632.png",
            ["Evoker"] = "https://render.guildwars2.com/file/B4132092D803E7BC12A6E0DC756CED4F945197D2/3680061.png",
            ["Firebrand"] = "https://render.guildwars2.com/file/A1287D0FD1159CAC3A58C212C94A4BD0AB32A8D3/1770211.png",
            ["Galeshot"] = "https://render.guildwars2.com/file/229C6E937EEDFF36AACF360F37059FC1D401D7F6/3680081.png",
            ["Guardian"] = "https://render.guildwars2.com/file/C32BE61FC55C962524624F643897ECF1A9C80462/156634.png",
            ["Harbinger"] = "https://render.guildwars2.com/file/14D0900962F99600047D73242F0AD0D3AC986EB7/2479361.png",
            ["Herald"] = "https://render.guildwars2.com/file/0D29C2DC29FFD156082572B41310D00914029E6C/1128577.png",
            ["Holosmith"] = "https://render.guildwars2.com/file/5DCFFC66A63466DFEBD29ACFA605CF00C708CBF3/1770225.png",
            ["Luminary"] = "https://render.guildwars2.com/file/F6BDFD499FD0DAE1A436AAC36E5A970A3A782509/3680069.png",
            ["Mechanist"] = "https://render.guildwars2.com/file/CA303ADC0BBE9F9591C5C4A2C4740FD59E6EEE55/2503658.png",
            ["Mesmer"] = "https://render.guildwars2.com/file/E43730AD49A903C3A1B4F27E41DE04EA51A775EC/156636.png",
            ["Mirage"] = "https://render.guildwars2.com/file/52285431289AE4FE39E8A21D4333E137A5EF0921/1770217.png",
            ["Necromancer"] = "https://render.guildwars2.com/file/AE56F8670807B87CF6EEE3FC7E6CB9710959E004/156638.png",
            ["Paragon"] = "https://render.guildwars2.com/file/C9CB4FD2DA9B7B3DA3A706C4B991E15E9D6B0CB8/3680093.png",
            ["Ranger"] = "https://render.guildwars2.com/file/49B10316B424F4E20139EB5E51ADCF24A8724E9B/156640.png",
            ["Reaper"] = "https://render.guildwars2.com/file/6E9C230241E83DFCAA0B0C11ED32ED0CB605C0EC/1128579.png",
            ["Renegade"] = "https://render.guildwars2.com/file/A347E09ED9C6537C7D13EC212229468078C0F0E9/1770219.png",
            ["Revenant"] = "https://render.guildwars2.com/file/7C9309BE7A2A48C6A9FBCC70CC1EBEBFD7593C05/961390.png",
            ["Ritualist"] = "https://render.guildwars2.com/file/0B54AE3EC9D45BA3010803A308F5B67F30BC34FF/3680077.png",
            ["Scourge"] = "https://render.guildwars2.com/file/B10D1AB5AA0B0CFE7D26A142E99414C26244359C/1770221.png",
            ["Scrapper"] = "https://render.guildwars2.com/file/5749382E61E622D8E9E00FA60D4CEAADA3E12715/1128581.png",
            ["Soulbeast"] = "https://render.guildwars2.com/file/F80AC2AFAAF55C0F7F012C7EBECC90FD01F27FE9/1770215.png",
            ["Specter"] = "https://render.guildwars2.com/file/DD5E09D55662BEBFE1CF4FA32C28E4023A2D09EB/2503666.png",
            ["Spellbreaker"] = "https://render.guildwars2.com/file/C105F67C30BBD0FE0A26CB97E90B481419927D50/1770223.png",
            ["Tempest"] = "https://render.guildwars2.com/file/BC1D0645C1304238F6154C9F366DEBD014FD7AD4/1128583.png",
            ["Thief"] = "https://render.guildwars2.com/file/F9EC00E23F630D6DB20CDA985592EC010E2A5705/156641.png",
            ["Troubadour"] = "https://render.guildwars2.com/file/1ACAEDF680C0FAF9081780D8F925B5E854DFA9F4/3680073.png",
            ["Untamed"] = "https://render.guildwars2.com/file/E208D91FD7A1D1035364D252F24D75F9FCC706C6/2503662.png",
            ["Vindicator"] = "https://render.guildwars2.com/file/D9549F115AF045C846E9EFC4D51BF527B1F7F2F7/2491561.png",
            ["Virtuoso"] = "https://render.guildwars2.com/file/C6480FD498E66EE5AEFBAF01EDF944631947DBA5/2479357.png",
            ["Warrior"] = "https://render.guildwars2.com/file/0A97E13F29B3597A447EEC04A09BE5BD699A2250/156643.png",
            ["Weaver"] = "https://render.guildwars2.com/file/C4B549D62A43E1BF490197E212B21879C4214008/1670506.png",
            ["Willbender"] = "https://render.guildwars2.com/file/0AF79DC205A1E14422720079BBAC05E35BCDB07E/2479353.png",
        };

        internal static string For(string profession) =>
            Icons.TryGetValue(profession, out var icon) ? icon : string.Empty;
    }
}

"""Shrinks an Elite Insights json log to the parts raidTimeline reads, so it can be used as a test fixture.

Usage: python trim_ei_json.py <input.json> <output.json>

Generate the input with the Elite Insights CLI (GW2EICLI.zip from
https://github.com/baaron4/GW2-Elite-Insights-Parser/releases) and a config containing
SaveOutJSON=true, e.g.:  GuildWars2EliteInsights-CLI.exe -c ei.conf test_log.zevtc

Only the heavy time series / distribution data is removed; the structure of everything that remains is untouched.
"""
import json
import sys

DROP_ANYWHERE = {
    "damage1S", "powerDamage1S", "conditionDamage1S", "targetDamage1S", "targetPowerDamage1S",
    "targetConditionDamage1S", "targetBreakbarDamage1S", "damageTaken1S", "powerDamageTaken1S",
    "conditionDamageTaken1S", "breakbarDamageTaken1S", "rotation", "totalDamageDist", "targetDamageDist",
    "totalDamageTaken", "conditionsStates", "boonsStates", "healthPercents", "barrierPercents",
    "breakbarPercents", "activeCombatMinions", "minions", "damageModifiers", "damageModifiersTarget",
    "incomingDamageModifiers", "incomingDamageModifiersTarget", "weaponSets", "statsTargets", "statsAll",
    "activeTimes", "consumables", "commanderTagStates", "buffs", "buffVolumes",
}
DROP_TOP_LEVEL = {"skillMap", "damageModMap", "personalBuffs", "personalDamageMods", "logErrors"}
BUFF_GROUPS = ("selfBuffs", "groupBuffs", "offGroupBuffs", "squadBuffs", "buffVolumes", "selfBuffVolumes",
               "groupBuffVolumes", "offGroupBuffVolumes", "squadBuffVolumes")


def trim(node):
    if isinstance(node, dict):
        return {k: trim(v) for k, v in node.items()
                if k not in DROP_ANYWHERE and not k.startswith(BUFF_GROUPS)}
    if isinstance(node, list):
        return [trim(v) for v in node]
    return node


def main(source, target):
    with open(source, encoding="utf-8") as f:
        log = json.load(f)
    for key in DROP_TOP_LEVEL:
        log.pop(key, None)
    # Every buff uptime entry carries large per-source dictionaries we do not read.
    for player in log["players"]:
        for uptime in player.get("buffUptimes", []):
            for data in uptime["buffData"]:
                for k in list(data):
                    if k not in ("uptime", "presence"):
                        del data[k]
        for k in ("buffUptimesActive",):
            player.pop(k, None)
    with open(target, "w", encoding="utf-8") as f:
        json.dump(trim(log), f, separators=(",", ":"), ensure_ascii=False)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

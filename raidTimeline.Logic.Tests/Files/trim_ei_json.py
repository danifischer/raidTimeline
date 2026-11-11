"""Shrinks an Elite Insights json log to the parts raidTimeline reads, so it can be used as a test fixture.

Usage: python trim_ei_json.py <input.json> <output.json> [--players N] [--rank a,b,c] [--anonymize] [--minimal]

Generate the input with the Elite Insights CLI (GW2EICLI.zip from
https://github.com/baaron4/GW2-Elite-Insights-Parser/releases) and a config containing
SaveOutJSON=true, e.g.:  GuildWars2EliteInsights-CLI.exe -c ei.conf test_log.zevtc
(Elite Insights writes the json next to the log file.)

Only the heavy time series / distribution data is removed; the structure of everything that remains is untouched.

--players N   keep only the N players with the most mechanic events (fewer players = smaller file)
--rank a,b,c  count only these mechanics (short names) when choosing the players to keep
--minimal     keep only the properties raidTimeline reads (tiny files for mechanics tests)
--anonymize   replace account names, character names and upload links, so no real player data is committed
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
DROP_TOP_LEVEL = {"skillMap", "damageModMap", "personalBuffs", "personalDamageMods", "logErrors",
                  "recordedBy", "recordedAccountBy"}
BUFF_GROUPS = ("selfBuffs", "groupBuffs", "offGroupBuffs", "squadBuffs", "buffVolumes", "selfBuffVolumes",
               "groupBuffVolumes", "offGroupBuffVolumes", "squadBuffVolumes")


def trim(node):
    if isinstance(node, dict):
        return {k: trim(v) for k, v in node.items()
                if k not in DROP_ANYWHERE and not k.startswith(BUFF_GROUPS)}
    if isinstance(node, list):
        return [trim(v) for v in node]
    return node


def keep_players(log, count, rank=None):
    """Keeps the players with the most mechanic events. Target/phase indexes are unaffected."""
    events = {}
    for mechanic in log["mechanics"]:
        if rank and mechanic["name"] not in rank:
            continue
        for event in mechanic["mechanicsData"]:
            events[event["actor"]] = events.get(event["actor"], 0) + 1
    ranked = sorted(log["players"], key=lambda p: -events.get(p["name"], 0))[:count]
    kept = [p for p in log["players"] if p in ranked]
    names = {p["name"] for p in kept}
    log["players"] = kept
    for mechanic in log["mechanics"]:
        mechanic["mechanicsData"] = [e for e in mechanic["mechanicsData"] if e["actor"] in names]


PLAYER_KEYS = {"account", "name", "group", "profession", "notInSquad", "friendlyNPC", "dpsTargets", "dpsAll",
               "support", "defenses", "buffUptimes"}
TARGET_KEYS = {"name", "healthPercentBurned"}
TOP_KEYS = {"eliteInsightsVersion", "triggerID", "fightName", "fightIcon", "timeStartStd", "timeEndStd", "duration",
            "durationMS", "success", "targets", "players", "phases", "mechanics", "uploadLinks", "buffMap"}


def minimal(log):
    """Reduces the log to the properties raidTimeline reads (see EiLog.cs)."""
    for key in list(log):
        if key not in TOP_KEYS:
            del log[key]
    log["players"] = [{k: v for k, v in p.items() if k in PLAYER_KEYS} for p in log["players"]]
    log["targets"] = [{k: v for k, v in t.items() if k in TARGET_KEYS} for t in log["targets"]]
    log["phases"] = [{k: v for k, v in log["phases"][0].items() if k in ("start", "end", "name", "targets", "targetPriorities")}]
    log["buffMap"] = {k: {kk: vv for kk, vv in v.items() if kk in ("name", "icon", "classification", "stacking")}
                      for k, v in log["buffMap"].items() if v.get("classification") == "Boon"}
    for player in log["players"]:
        player["dpsTargets"] = [[{k: v for k, v in phase.items() if k in ("damage", "breakbarDamage")}
                                 for phase in target[:1]] for target in player["dpsTargets"]]
        player["dpsAll"] = [{k: v for k, v in p.items() if k in ("damage", "breakbarDamage")} for p in player["dpsAll"][:1]]
        player["support"] = [{k: v for k, v in p.items() if k in ("resurrects", "resurrectTime")} for p in player["support"][:1]]
        player["defenses"] = [{k: v for k, v in p.items() if k in ("downCount", "deadCount")} for p in player["defenses"][:1]]
        player["buffUptimes"] = [{"id": b["id"], "buffData": b["buffData"][:1]} for b in player["buffUptimes"]
                                 if "b%d" % b["id"] in log["buffMap"]]
    for mechanic in log["mechanics"]:
        for key in list(mechanic):
            if key not in ("name", "mechanicsData"):
                del mechanic[key]
        mechanic["mechanicsData"] = [{"actor": e["actor"]} for e in mechanic["mechanicsData"]]


def anonymize(log):
    names = {}
    for i, player in enumerate(log["players"], 1):
        names[player["name"]] = "Character %d" % i
        player["name"] = names[player["name"]]
        player["account"] = "Player%d.%04d" % (i, 1000 + i)
        player.pop("guildID", None)
    for mechanic in log["mechanics"]:
        for event in mechanic["mechanicsData"]:
            # actors that are not players (npcs) keep their name
            event["actor"] = names.get(event["actor"], event["actor"])
    # targets such as "Sacrificed 1 <character name>" (Matthias) contain player names as well
    for target in log["targets"]:
        if target["name"].startswith("Sacrificed "):
            target["name"] = " ".join(target["name"].split(" ")[:2]) + " Character"
    log["uploadLinks"] = []


def main(source, target, players=None, anon=False, rank=None, small=False):
    with open(source, encoding="utf-8") as f:
        log = json.load(f)
    for key in DROP_TOP_LEVEL:
        log.pop(key, None)
    if players:
        keep_players(log, players, rank)
    # Every buff uptime entry carries large per-source dictionaries we do not read.
    for player in log["players"]:
        for uptime in player.get("buffUptimes", []):
            for k in list(uptime):
                if k not in ("id", "buffData"):  # e.g. per source state lists, which contain character names
                    del uptime[k]
            for data in uptime["buffData"]:
                for k in list(data):
                    if k not in ("uptime", "presence"):
                        del data[k]
        player.pop("buffUptimesActive", None)
    if anon:
        anonymize(log)
    if small:
        minimal(log)
    with open(target, "w", encoding="utf-8") as f:
        json.dump(trim(log), f, separators=(",", ":"), ensure_ascii=False)


if __name__ == "__main__":
    args = sys.argv[1:]
    count = int(args[args.index("--players") + 1]) if "--players" in args else None
    rank = set(args[args.index("--rank") + 1].split(",")) if "--rank" in args else None
    main(args[0], args[1], count, "--anonymize" in args, rank, "--minimal" in args)

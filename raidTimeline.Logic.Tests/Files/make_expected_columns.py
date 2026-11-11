"""Calculates the expected values of every mechanics table column for every fixture in this folder.

Usage: python make_expected_columns.py

The values are computed here straight from the json of each fixture and the definition in
raidTimeline.Logic/Mechanics/mechanics.json (a second, independent implementation of the counting), and written to
expected_columns.json. BossMechanicsTests compares them with what the parser produces.
Run it again after adding a fixture or changing a definition, and review the diff.
"""
import glob
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
CATALOG = os.path.join(HERE, "..", "..", "raidTimeline.Logic", "Mechanics", "mechanics.json")

catalog = json.load(open(CATALOG, encoding="utf-8"))
encounters = {i: e for e in catalog["encounters"] for i in e["ids"]}

result = {}
for path in sorted(glob.glob(os.path.join(HERE, "*.json"))):
    name = os.path.splitext(os.path.basename(path))[0]
    if name == "expected_columns":
        continue
    log = json.load(open(path, encoding="utf-8"))
    encounter = encounters.get(log["triggerID"])
    if encounter is None:
        raise SystemExit("%s: no definition for encounter %s" % (name, log["triggerID"]))

    players = [p for p in log["players"] if not p.get("friendlyNPC")]  # friendly npcs are left out of the model
    events = {}
    for mechanic in log["mechanics"]:
        per_actor = events.setdefault(mechanic["name"], {})
        for event in mechanic["mechanicsData"]:
            per_actor[event["actor"]] = per_actor.get(event["actor"], 0) + 1

    def count(player, mechanic):
        return events.get(mechanic, {}).get(player["name"], 0)

    columns = {}
    for column in encounter["columns"]:
        columns[column["key"]] = [
            sum(count(p, m) for m in column["mechanics"]) - sum(count(p, m) for m in column.get("subtract", []))
            for p in players]
    result[name] = {"encounterId": log["triggerID"], "players": len(players), "columns": columns}

with open(os.path.join(HERE, "expected_columns.json"), "w", encoding="utf-8", newline="\n") as f:
    f.write("{\n")
    items = list(result.items())
    for i, (name, value) in enumerate(items):
        f.write("  %s: {\n    \"encounterId\": %d, \"players\": %d,\n    \"columns\": {\n" % (
            json.dumps(name), value["encounterId"], value["players"]))
        cols = list(value["columns"].items())
        for j, (key, values) in enumerate(cols):
            f.write("      %s: %s%s\n" % (json.dumps(key), json.dumps(values), "," if j < len(cols) - 1 else ""))
        f.write("    }\n  }%s\n" % ("," if i < len(items) - 1 else ""))
    f.write("}\n")

empty = [(n, k) for n, v in result.items() for k, vals in v["columns"].items() if not any(vals)]
print("fixtures:", len(result), "| columns that are 0 for every player:", empty or "none")

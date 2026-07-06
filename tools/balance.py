#!/usr/bin/env python3
# Анализ кривой сложности кампании RiotGalaxy II.
# Читает Content/Config/enemies.yaml, Content/Missions/*.yaml, Content/Levels/*.yaml
# и считает "threat" каждого боя/миссии из статов врагов, плотности спавна, sortie и боссов.
# Это ОЦЕНКА для поиска спайков/провалов внутри актов, НЕ точная модель (боссы недооценены:
# их сложность — в паттернах/фазах, а не только в HP). Прогонять после правок состава волн/статов.
#
# Запуск (нужен PyYAML):  python3 tools/balance.py
# Модель threat на врага: hp*0.5 + dps*7 + damage*0.3 (dps=damage/shootInterval; нестреляющие — контакт).
# На бой: сумма * фактор_плотности(spawnInterval) * фактор_sortie. Правь веса вверху функций при желании.
import os, glob, yaml, sys

ROOT = os.path.join(os.path.dirname(__file__), "..", "RiotGalaxy.Content")
def load(p):
    with open(p, encoding="utf-8") as f: return yaml.safe_load(f)

enemies = load(os.path.join(ROOT, "Config", "enemies.yaml"))

def stat(t, k, d=0.0):
    e = enemies.get(t, {}) or {}
    if k in e: return e[k]
    lo, hi = e.get(k+"Min"), e.get(k+"Max")
    if lo is not None and hi is not None: return (lo+hi)/2.0
    return d

def is_boss(t): return (enemies.get(t, {}) or {}).get("ai") == "boss"

def per_enemy_threat(t):
    hp = stat(t, "hp", 10)
    dmg = stat(t, "damage", 10)
    si = stat(t, "shootInterval", 0)
    dps = (dmg / si) if si and si > 0 else dmg * 0.35   # нестреляющие — контактный урон
    # живучесть (время на убийство) + огневая мощь
    return hp * 0.5 + dps * 7.0 + dmg * 0.3

def battle_threat(path):
    b = load(path) or {}
    spawn = b.get("spawnInterval", 0.8) or 0.8
    sortie = bool(b.get("sortie", False))
    scount = b.get("sortieCount", 1) or 1
    total, n_enemies, sumhp, sumdps, boss = 0.0, 0, 0.0, 0.0, False
    def add(ev):
        nonlocal total, n_enemies, sumhp, sumdps, boss
        if not isinstance(ev, dict): return
        # блок parallel: список веток, каждая ветка — список событий
        if "parallel" in ev:
            for branch in (ev["parallel"] or []):
                for sub in (branch or []): add(sub)
            return
        if "enemy" not in ev: return
        t = ev["enemy"]; c = ev.get("count", 1) or 1
        if is_boss(t): boss = True
        total += per_enemy_threat(t) * c
        n_enemies += c
        sumhp += stat(t, "hp", 10) * c
        si = stat(t, "shootInterval", 0)
        sumdps += ((stat(t, "damage", 10) / si) if si and si > 0 else 0) * c
    for ev in (b.get("events") or []):
        add(ev)
    density = max(0.8, min(2.0, 0.9 / spawn))           # чаще спавн → плотнее → тяжелее
    sf = 1.0 + (0.12 * scount if sortie else 0.0)       # активные пике добавляют давления
    score = total * density * sf
    return dict(score=score, enemies=n_enemies, hp=sumhp, dps=sumdps,
                boss=boss, spawn=spawn, sortie=sortie, name=b.get("description", ""))

# кампания → миссии → шаги-бои (в порядке)
camp = load(os.path.join(ROOT, "Missions", "campaign.yaml"))["missions"]
print(f"{'Миссия':<22}{'бой':<10}{'threat':>8}{'враги':>7}{'HP':>7}{'DPS':>7}  фичи")
print("-"*80)
mission_scores = []
for mid in camp:
    m = load(os.path.join(ROOT, "Missions", f"{mid}.yaml"))
    title = m.get("title", mid)
    msum = 0.0; battles = []
    for step in (m.get("steps") or []):
        bn = step.get("battle") or step.get("boss")
        if not bn: continue
        p = os.path.join(ROOT, "Levels", f"{bn}.yaml")
        if not os.path.exists(p): continue
        r = battle_threat(p)
        battles.append((bn, r)); msum += r["score"]
    mission_scores.append((mid, title, msum))
    for i,(bn,r) in enumerate(battles):
        feats = []
        if r["boss"]: feats.append("BOSS")
        if r["sortie"]: feats.append("sortie")
        feats.append(f"spawn{r['spawn']}")
        head = f"{mid} {title}" if i==0 else ""
        print(f"{head:<22}{bn.split('_',1)[-1]:<10}{r['score']:>8.0f}{r['enemies']:>7}{r['hp']:>7.0f}{r['dps']:>7.1f}  {' '.join(feats)}")
    print(f"{'':<22}{'ИТОГО':<10}{msum:>8.0f}")
    print()

print("="*50)
print("КРИВАЯ ПО МИССИЯМ (сумма threat):")
print("  (передышка на открытии акта и скачок на финале — НАМЕРЕННЫЕ; ⚠ = аномалия внутри акта)")
OPENERS = {"m6", "m10"}      # первые миссии Актов II/III — намеренная передышка
FINALES = {"m5", "m9", "m13"} # финалы актов — намеренный скачок
prev=None
for mid,title,s in mission_scores:
    n = int(mid[1:]); act = 1 if n<=5 else 2 if n<=9 else 3
    arrow = ""
    if prev is not None:
        d = s-prev
        tag = ""
        if mid in FINALES:   tag = "  (финал акта ↑)"
        elif mid in OPENERS: tag = "  (открытие акта — передышка)"
        elif d < -400:       tag = "  ⚠ ПРОВАЛ внутри акта"
        elif d > 1500:       tag = "  ⚠ РЕЗКИЙ СКАЧОК"
        arrow = f"  {'+' if d>=0 else ''}{d:.0f}{tag}"
    print(f"  Акт{act} {mid:<4}{title:<26}{s:>8.0f}{arrow}")
    prev=s

# EitRForWotr — Elephant in the Room (baked) for Pathfinder: WOTR

A UnityModManager mod that bakes the **original 2012 blog-post** version of
[*Elephant in the Room*](https://michaeliantorno.com/feat-taxes-in-pathfinder/)
(12 combat-feat changes — not the expanded PDF) into Pathfinder: Wrath of the Righteous.
Applies globally to player, companions, summons, and NPCs/enemies.

Companion-mod recommendation for smarter enemy AI on the new free combat options:
[Wrath Tactics](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1005).

## Status

All 12 changes from the original 2012 blog post are baked. Known gaps:

- **Change #1 (weapon feats apply to fighter weapon groups)** — not
  implemented. Install [Weapon Focus Plus](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/7)
  alongside this mod for the same effect.
- **Change #12 (Dodge merge) minor deviation** — the blog specifies "+1
  dodge AC, increasing to +4 against attacks of opportunity from movement";
  WOTR's stock Mobility component gives "+4 vs all AoOs" with no built-in
  movement-trigger condition. Result is +5 vs all AoOs (instead of +1
  always / +4 vs movement-AoOs only). Slightly broader than spec; would
  need a custom AC-bonus component to fix exactly.
- **TWF — Improved Two-Weapon Fighting as a prerequisite.** The blog says
  "Gone. Merged with Greater Two-Weapon Fighting" but is silent on what
  to do with features that previously *required* ITWF. This mod
  **redirects** any `PrerequisiteFeature(ITWF)` to `PrerequisiteFeature(TWF)`
  rather than deleting it. Rationale: it matches how the blog handles
  other consolidations (Mobility prereqs become Dodge; Improved-X prereqs
  become Deft/Powerful Maneuvers) and the mod's own pattern for
  changes #5/#6/#8/#9/#12. In stock WOTR only Greater TWF appears to
  list ITWF as a hard prereq, so this is usually a no-op in practice.

Everything else is faithful to the blog's literal text. Notably, the
"finesse" weapon-attribute change (#2) is already covered without extra
work — WOTR's stock `WeaponCategoryExtension.Data` table already tags
the rapier (the blog's only named non-light example), as well as estoc,
elven curved blade, starknife, dueling sword, and sawtooth sabre, with
the `Finessable` subcategory.

## Prerequisites

| | Required | Where |
|---|---|---|
| Game | Pathfinder: Wrath of the Righteous (Steam, appid 1184370) | `~/.local/share/Steam/steamapps/common/Pathfinder Second Adventure/` |
| Build | .NET SDK 8 or 9+ | `pacman -S dotnet-sdk` |
| Runtime | UnityModManager 0.27+ installed into the game | See "Install UMM" below |

`net472` reference assemblies, `WW-Blueprint-Core`, and the UMM API are pulled via NuGet — no extra Linux setup needed.

## Install UMM (one-time)

UMM doesn't ship a Linux binary, but its installer runs fine under plain `wine` — wine's bundled Mono runtime handles the .NET Framework app, no `mono` package needed. Headless install:

```bash
mkdir -p ~/Tools/UnityModManager
cd ~/Tools/UnityModManager
unzip ~/Downloads/UnityModManager-*.zip
cd UnityModManagerInstaller
WINEDEBUG=-all wine Console.exe <<EOF
64
Z:\home\$USER\.local\share\Steam\steamapps\common\Pathfinder Second Adventure
I
EOF
```

`64` is the WOTR entry in UMM's hardcoded game list (current at v0.32.4 — re-check if the version changes). `I` selects Install. UMM installs as DoorstopProxy (drops `winhttp.dll` + `doorstop_config.ini` into the game folder, plus its own runtime under `Wrath_Data/Managed/UnityModManager/`).

> ⚠️ **If the game crashes on launch under Proton**, the design doc's note about Doorstop being flaky is hitting you. Re-run the installer with `D` to delete, then install via UMM's **Assembly** install method (patches `Assembly-CSharp.dll` directly via `dnlib` — more invasive but more reliable under Proton). To pick Assembly mode you'll need the GUI: `wine UnityModManager.exe`.

Verify mods folder afterward (created by UMM):

```
~/.local/share/Steam/steamapps/common/Pathfinder Second Adventure/Mods/
```

## Build & deploy

```bash
dotnet build
```

The MSBuild `Deploy` target copies `EitRForWotr.dll` + `Info.json` + `Localization/` into the UMM mods folder automatically. Override the install paths if yours differ:

```bash
dotnet build \
  -p:WrathDir="/path/to/Pathfinder Second Adventure" \
  -p:ModDir="/path/to/UMM/EitRForWotr"
```

To produce a distributable zip (DLL + dependencies + Info.json, structured for UMM / ModFinder install), run:

```bash
./build.sh
# → EitRForWotr-v<version>.zip in the repo root
```

## Scope

Original 2012 blog post only:

1. Martial Mastery (weapon-group feats)
2. Weapon Finesse → automatic on finesse weapons
3. Agile Maneuvers → better of Str or Dex on CMB
4. Combat Expertise → free at BAB +1
5. Improved Trip/Disarm/Dirty Trick/Feint/Reposition/Steal → Deft Maneuvers
6. Deft Maneuvers (new)
7. Power Attack → free at BAB +1
8. Improved Bull Rush/Drag/Overrun/Sunder → Powerful Maneuvers
9. Powerful Maneuvers (new)
10. Point-Blank Shot dropped as a prereq for Precise Shot
11. Deadly Aim → free at BAB +1
12. Mobility merged into Dodge

**PDF expansions (Spell Focus, skill feat scaling, mounted combat, etc.) are out of scope.**

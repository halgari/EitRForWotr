# EitRForWotr — Elephant in the Room (baked) for Pathfinder: WOTR

A UnityModManager mod that bakes the **original 2012 blog-post** version of
[*Elephant in the Room*](https://michaeliantorno.com/feat-taxes-in-pathfinder/)
(12 combat-feat changes — not the expanded PDF) into Pathfinder: Wrath of the Righteous.
Applies globally to player, companions, summons, and NPCs/enemies.

Companion-mod recommendation for smarter enemy AI on the new free combat options:
[Wrath Tactics](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/1005).

Design doc: `~/Downloads/EitR-WOTR-Mod-Guide.md` (local).
Implementation plan: `~/.claude/plans/read-the-doc-in-humble-lemon.md`.

## Status

All 11 of the original blog post's combat-feat changes are baked. Two
known scope gaps for v0.1:

- **Change #1 (weapon feats apply to fighter weapon groups)** — not
  implemented. Install [Weapon Focus Plus](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/7)
  alongside this mod for the same effect.
- **Change #2 partial coverage** — Weapon Finesse and Agile Maneuvers are
  granted globally, but the explicit weapon re-tagging (rapier / whip /
  spiked-chain / elven-curve-blade / estoc / starknife → Finessable) is
  deferred. Stock-Finessable weapons (most "light" weapons) work for
  everyone; the listed exceptions need a tag sweep over `BlueprintItemWeapon`.

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

## Iterate loop

```bash
# Tail the game log
tail -F "$HOME/.local/share/Steam/steamapps/compatdata/1184370/pfx/drive_c/users/steamuser/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/Player.log" \
  | grep -i eitr
```

In WOTR, `Ctrl-F10` opens the UMM overlay → Mods tab → confirm `EitRForWotr` is Active. Hit **Reload** there to re-load the DLL after a rebuild. Patches that hook `BlueprintsCache.Init` only re-fire on a save reload.

## Project layout

```
.
├── EitRForWotr.csproj          MSBuild project
├── Info.json                    UMM manifest
├── Main.cs                      Entry point + Harmony bootstrap
├── Patches/
│   └── BlueprintsCache_Init_Patch.cs   Postfix; mutators dispatched here
├── Mutators/                    One file per EitR change family (added per phase)
├── NewBlueprints/               Deft Maneuvers, Powerful Maneuvers (Phase 4c)
└── Localization/enGB.json       Display strings
```

## Scope

Original 2012 blog post only:

1. Martial Mastery (weapon-group feats)
2. Weapon Finesse → automatic on finesse weapons
3. Agile Maneuvers → Dex on CMB w/ finesse weapons
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

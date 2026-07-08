# VoidClash

*Pre-Alpha v0.16.0 - Unity 6.3 (6000.3.16f1)*

VoidClash is a real-time strategy game built entirely in Unity/C#, in the spirit of StarCraft. The
player commands prototype Terran, Bubble, and Dots factions with worker economy, structure-driven
economy, mobile power-core economy, base building, lift-off buildings, fog of war, and a rule-based
AI opponent that can play any of the three races.

The game is playable as Free Play (choose player race, AI race, and difficulty), or through
race-specific campaign fronts against Zerg, Protoss, and Terran enemies.

Everything ships from code: 3D art is assembled from Unity primitives, materials and textures are
generated in project code, sound effects and voice chirps are synthesized, and both scenes build
themselves from bootstrap objects. No imported models, paid assets, or third-party packages beyond
stock Unity/URP.

## Requirements And Running

- Unity 6.3 LTS, exactly `6000.3.16f1`.
- Clone the repo and open it in Unity Hub.
- First open only: `VoidClash -> Setup Project` runs automatically if generated assets are missing.
- Open `Assets/Scenes/MainMenu.unity` and press Play.

To build the Windows player from Unity, use `VoidClash -> Build Windows EXE`.

Headless build:

```powershell
Unity.exe -batchmode -quit -projectPath <path> -executeMethod VoidClash.Editor.BuildGame.Run
```

The player lands at `Build/VoidClash.exe`. Release zips are stored separately from `Build/` so the
repo stays source-first.

## v0.16.0 Full Visual Identity Update

- First impression: v0.16 menu badge, Free Play matchup preview, campaign threat cards.
- Battlefield identity: sci-fi tactical plates, base pads, center beacon, lane/cliff glow, and
  faction atmosphere for Terran, Bubble, Dots, Zerg, and Protoss fronts.
- Motion and combat: idle motion, attack recoil, build-completion flashes, lift/landing rings,
  Dots formation bursts, poison readability, stronger commander-power spectacle, and boss shockwaves.
- UI polish: race-tinted minimap frame, command-card accent, power-ready flash colors, objective strip,
  stronger briefing/end presentation, and screenshot-smoke support.

## Modes

- Free Play: choose your race, the enemy race (Terran / Bubble / Dots / Random) and a
  difficulty (Easy / Normal / Hard), then fight. The AI plays whichever race you pick for it —
  any matchup, including mirrors.
- Campaign: thirteen missions, unlocked in order per race arc, with local progress:
  1. First Contact: Zerg swarm pressure.
  2. The Golden Armada: Protoss armor and slower power waves.
  3. The Overmind: Zerg boss mission.
  4. Steel Mirror: rebel Terran expansion pressure.
  5. Shattered Gate: Protoss center-control pressure.
  6. Brood Eclipse: final Zerg boss mission with earlier pressure.
  7. Bubble 1 - First Foam: Bubble Nexus and Spring tutorial.
  8. Bubble 2 - Toxic Pop: Poison Pool tutorial.
  9. Bubble 3 - Pressure Dome: Bubble pressure and defense mission.
  10. Dots 1 - Hidden Core: Core Dot, Printer, Shape Matrix, and Giant tutorial.
  11. Bubble 4 - Glass Undertow: Bubble poison tactics against Protoss armor.
  12. Dots 2 - Needle Orbit: Kite and Spike range-control mission.
  13. Dots 3 - Giant Relay: Giant/Core protection mission against Protoss pressure.

## Controls

| Input | Action |
|---|---|
| Left-click | Select unit/building |
| Left-drag | Box-select multiple units |
| Shift + click/drag | Add/remove from selection |
| Double-click | Select all units of that type currently on screen |
| Right-click | Move, attack, harvest, set rally, or fly an airborne building |
| A, then left-click | Attack-move |
| S / H | Stop / Hold position |
| L | Lift off or choose a landing zone for lift-capable buildings |
| F1 | Select and jump to an idle Worker |
| F2 | Select all combat army units |
| F3 / F4 / F5 | Commander powers (target a spot): Airstrike / Heal Wave / Freeze |
| F6 | Race Overdrive: boost the selected combat units |
| Ctrl + 1-9 | Assign control group |
| 1-9 | Recall control group; double-tap to jump camera |
| Q / W / E / R / T / Y | Build hotkeys, or train hotkeys on production buildings |
| C / V / B / Z | Dots shape commands: Core Dot, Dot Kite, Dot Spike, Dot Giant |
| W A S D / arrows / screen edge | Pan camera |
| Mouse wheel | Zoom |
| Shift while placing | Keep placing the same building |
| Esc | Cancel current mode or open pause |
| Minimap click/drag | Move the camera |

## Economy And Roster

Start: 1 Command Center, 4 Workers, 50 minerals in Free Play, and mission-specific campaign starts.
Workers auto-harvest nearby crystals, carry 5 minerals per trip, and deposit at dropoff buildings.

| Terran Unit | Cost | Supply | HP | Damage | Notes |
|---|---:|---:|---:|---|---|
| Worker | 50 | 1 | 60 | 5 melee | Harvests and constructs |
| Soldier | 50 | 1 | 120 | 8 normal | Frontline infantry |
| Ranger | 75 | 1 | 90 | 12 piercing | Long range, strong vs light units |
| Heavy | 150 | 3 | 350 | 25 siege | Slow armor/building breaker |

| Building | Cost / Build Time | HP | Function |
|---|---:|---:|---|
| Command Center | 400 / 30s | 1500 | Trains Workers, dropoff, +10 supply, can lift off |
| Supply Depot | 100 / 15s | 500 | +8 supply |
| Barracks | 150 / 20s | 900 | Trains Soldiers and Rangers, can lift off |
| Factory | 200 / 25s | 1100 | Trains Heavies, can lift off |
| Turret | 125 / 15s | 750 | Static defense |
| Sensor Tower | 125 / 18s | 450 | Optional scouting building with large vision |

Campaign enemy units use the same armor and damage systems: Zerglings, Hydralisks, Zealots,
Stalkers, and the Overlord boss.

## Bubble And Dots Prototypes

Bubble is a structure-driven swarm race. The Bubble Nexus automatically makes one-hit bubbles,
Bubble Springs earn a slow crystal-linked mineral trickle, Poison Pools morph basic bubbles into
gas-bursting poison bubbles, and Aerators upgrade Bubble production speed.

Dots are a shape-droid prototype race. They do not mine with workers. Dot Printers create loose
Dots on their own; loose Dots are the race's raw material and are spent to create shapes. A Shape
Matrix unlocks shaping commands: Core Dot, flying Dot Kite, long-range Dot Spike, and Dot Giant.
Core Dots trickle minerals. A Dot Giant costs loose Dots and swallows a Core Dot; when the Giant
dies, the Core Dot escapes.

## v0.14.0 Highlights

- Campaign unlocks are now per race arc: Terran, Bubble, and Dots each start with their first
  mission unlocked instead of Bubble/Dots being gated behind Terran progress.
- Added three missions: Bubble 4, Dots 2, and Dots 3.
- Skirmish is now presented as Free Play: choose player race, AI race, and difficulty.
- Premium-feel pass: closer default camera, clearer commander-power area markers, and upgraded
  mission briefings with objective/intel structure and race accenting.

## v0.13.0 Highlights

- Commander Powers (player, F3/F4/F5, shared recharge): Airstrike (delayed AoE barrage), Heal Wave
  (restores nearby units), and Freeze (locks enemy units in place for a few seconds).
- Race Overdrive (F6, own recharge): temporarily boosts the selected combat units' move + attack
  speed. Flavored per race — Terran Stim, Bubble Froth, Dots Overclock.
- New HUD powers panel shows each ability's live recharge.

## v0.12.0 Highlights

- New Skirmish screen: pick your race, the enemy race (+Random), and difficulty, then START BATTLE.
- The enemy AI now plays all three races. Bubble and Dots enemies are structure-driven — they build,
  earn minerals, and send their auto-produced swarms at you; Terran uses the full economy/army logic.
- Difficulty scaling: Easy = a slow, small, late army; Hard = a mineral head start with faster
  building and bigger, earlier waves.
- Fixed Dots income so an enemy Dots faction actually earns minerals from its Core Dots.
- Any-race starts: the race base spawner is generalized so either faction can begin as any race.

## v0.11.0 Highlights

- Campaign menu split by race: Terran, Bubble, and Dots no longer share one all-missions list.
- Episode cards now open the correct race campaign list.
- Each race campaign panel shows its own mission count and cleared progress.
- Main menu bottom text updated to v0.11.0.
- Dots Lab intro hint now shows shape hotkeys: C Core, V Kite, B Spike, Z Giant.

## v0.10.1 Highlights

- Bubble balance pass: faster Nexus production, faster Spring income, and tougher basic/poison bubbles.
- Dots now work as a spendable raw-material race: Printers produce loose Dots without needing power.
- Core Dot is formed from Dots and provides passive mineral income.
- Dot Giant now consumes a Core Dot and releases it on death.
- Dot Kite added as a flying Dots shape.
- Dot Spike added as a fragile long-range Dots shape.
- Select Army UI now shows army count.
- v0.8-v0.10 release zips are available locally when packaged.

## v0.8.0-v0.10.0 Highlights

- Dots Lab added as a playable prototype mode.
- Mobile Core Dot replaces the old Power Core building idea.
- Core Dot generates passive minerals, powers Dot Printers, and shows a visible power range when selected.
- Dot Giant added as the first major shape.
- Dots tutorial campaign mission added.
- Bubble campaign expanded to three missions.
- Worker harvest path recovery improved to recover from blocked or bad mineral approaches.
- v0.4-v0.7 features remain included: Sensor Tower, mission AI personalities, story beats, generated
  voice acknowledgements, Bubble Nexus/Spring/Poison/Aerator loop, Select Army UI, and release builds.

## Architecture

All game code lives under `Assets/Scripts`.

- Data: `DataDefs.cs` is the source of truth for unit/building stats. `Campaign.cs` defines missions
  with player race, enemy race, army mix, wave timing, story beat, boss, and AI personality.
- Bootstraps: `GameBootstrap` builds the game scene at runtime. `MenuBootstrap` builds the main
  menu. `StoryDirector` sends timed campaign flavor beats.
- Entities: `Unit`, `WorkerUnit`, and `Building` cover movement, combat, harvesting, construction,
  training, turrets, rally points, and lift-off/fly/land.
- Systems: `SelectionManager`, `InputController`, `BuildingPlacer`, `FogOfWar`, `Minimap`,
  `EnemyAI`, `BubbleSystem`, `DotsSystem`, and `GameManager` run the RTS loop.
- Presentation: `VisualFactory`, `MaterialLibrary`, `TextureFactory`, `EffectsManager`,
  `SynthLib`, and `AudioManager` generate all visuals, effects, sound, voice, and music.

## Automated Verification

`Assets/Tests/SmokeTests.cs` contains PlayMode smoke tests for:

1. Economy, all 6 buildings, all Terran units, fog, and minimap.
2. Worker harvest reliability and construction cancel refunds.
3. Boss mission setup, lift-off/fly/land, and boss-kill victory.
4. Campaign missions loading objectives, story beats, personalities, race visuals, and bosses.
5. Sensor Tower data and generated voice clips.
6. Bubble Lab economy, production upgrade, self-building, and poison morphing.
7. Dots Lab Dot printing, Shape Matrix self-building, Core/Kite/Spike/Giant forming, and Core Dot
   escape on Giant death.
8. An unattended AI match reaching a conclusion without console errors.

Note: during an earlier v0.4.0 packaging pass, Unity compiled and built successfully, but the PlayMode test
runner did not emit an XML results file, so that run is not counted as passed.

## Release Artifacts

- `Build/` is the local compiled player folder and is not committed.
- `VoidClash-v0.16.0-prealpha-win64.zip` is the latest packaged Windows release archive once the local build step completes.
- Older release zips are kept as historical artifacts when already tracked.

## Known Rough Edges

- Balance and campaign pacing still need real player-facing tuning.
- Dots still need more major shapes and active abilities beyond Giant/Kite/Spike.
- Bubble and Dots campaign arcs are early prototypes.
- Fog is radius-based; cliffs do not block line of sight.
- Worker mining has stronger recovery logic, but still needs longer playtesting before it can be
  called fully solved.
- The tactical map is flat by design for now, with grid and landmark readability taking priority.

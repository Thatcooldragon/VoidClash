# VoidClash

*Pre-Alpha v0.8.0 - Unity 6.3 (6000.3.16f1)*

VoidClash is a real-time strategy game built entirely in Unity/C#, in the spirit of StarCraft. The
player commands prototype Terran, Bubble, and Dots factions with worker economy, structure-driven
economy, mobile power-core economy, base building, lift-off buildings, fog of war, and a rule-based
AI opponent.

The game is playable as Free Play, Bubble Lab, Dots Lab, or through a 10-mission campaign against
Zerg, Protoss, and Terran enemies. v0.8.0 adds Dots tutorial content, the corrected mobile Core Dot
economy, Dot Giant shaping, a third Bubble mission, and stronger worker harvest recovery.

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

## Modes

- Free Play: a skirmish against the Terran AI.
- Bubble Tide: a self-building Bubble lab where a Nexus produces fragile bubbles.
- Dots Lab: a prototype race lab with a mobile Core Dot, Dot Printers, Shape Matrix, and Giant shape.
- Campaign: ten missions, unlocked in order, with local progress:
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
| Ctrl + 1-9 | Assign control group |
| 1-9 | Recall control group; double-tap to jump camera |
| Q / W / E / R / T / Y | Build hotkeys, or train hotkeys on production buildings |
| Z | Form a Dot Giant when 20 Dots are selected near a Core Dot and a Shape Matrix exists |
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

Dots are a shape-droid prototype race. They do not mine with workers. A mobile Core Dot provides
slow passive income, powers nearby Dot Printers, and hides inside major shapes. Dot Printers create
loose Dots while powered by the Core Dot. A Shape Matrix unlocks the Dot Giant: 20 loose Dots plus a
nearby Core Dot combine into a powerful walking shape. When the Giant dies, the Core Dot escapes.

## v0.8.0 Highlights

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
7. Dots Lab Core Dot income, powered printing, Shape Matrix self-building, Giant forming, and Core
   Dot escape on Giant death.
8. An unattended AI match reaching a conclusion without console errors.

Note: during an earlier v0.4.0 packaging pass, Unity compiled and built successfully, but the PlayMode test
runner did not emit an XML results file, so that run is not counted as passed.

## Release Artifacts

- `Build/` is the local compiled player folder and is not committed.
- `VoidClash-v0.8.0-prealpha-win64.zip` is the v0.8.0 Windows release archive when packaged.
- Older release zips are kept as historical artifacts when already tracked.

## Known Rough Edges

- Balance and campaign pacing still need real player-facing tuning.
- Dots still need more major shapes and abilities beyond the Giant.
- Bubble and Dots campaign arcs are early prototypes.
- Fog is radius-based; cliffs do not block line of sight.
- Worker mining has stronger recovery logic, but still needs longer playtesting before it can be
  called fully solved.
- The tactical map is flat by design for now, with grid and landmark readability taking priority.

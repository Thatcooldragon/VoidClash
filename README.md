# VoidClash

*Pre-Alpha — Unity 6.3 (6000.3.16f1)*

A real-time strategy game built entirely in Unity/C#, in the spirit of StarCraft. One faction
(Terran) fights across a handcrafted map with worker economy, base building, Terran-style
building lift-off, fog of war, and a rule-based AI opponent — playable either as a **Free Play**
skirmish or through a **3-mission campaign** against the Zerg and Protoss, capped by a boss fight.

Everything ships from code: all 3D art is Unity primitives assembled procedurally with URP
materials (emissive sci-fi accents), every sound effect and the ambient music track are
synthesized at runtime, and both scenes (`MainMenu`, `Game`) build themselves from a single
bootstrap object. No imported models, no paid assets, no third-party packages beyond stock
Unity/URP.

## Requirements & How to Run

- **Unity 6.3 LTS — exactly `6000.3.16f1`** (other 6000.3.x patch versions will likely work too).
- Clone the repo and open it as a project in Unity Hub.
- **First open only:** an editor script (`VoidClash → Setup Project`, also runs automatically via
  `[InitializeOnLoad]`) bakes all generated assets — ScriptableObject unit/building stats,
  procedural materials, synthesized `.wav` clips, visual prefabs, both scenes, build settings, and
  URP tuning (SSAO renderer feature, soft shadows, HDR). Nothing to do manually.
- Open `Assets/Scenes/MainMenu.unity` and press **Play**.

**Building a standalone player:** use the menu `VoidClash → Build Windows EXE`, or headlessly:

```
Unity.exe -batchmode -quit -projectPath <path> -executeMethod VoidClash.Editor.BuildGame.Run
```

The build lands in `Build/VoidClash.exe`. It isn't checked into git (see [Repo layout](#repo-layout-a-note-on-git-lfs) below) — grab it from a Release, or build it yourself in a couple of minutes.

## Modes

- **Free Play** — the original skirmish: you vs. a red Terran AI, straight economy race to
  Victory/Defeat.
- **Campaign — *Terran Front*** — three missions, unlocked in order, progress saved locally:
  1. **First Contact** *(vs Zerg)* — cheap, fast Zergling/Hydralisk swarms. Burn out their base.
  2. **The Golden Armada** *(vs Protoss)* — fewer but much tougher Zealots/Stalkers behind turrets.
  3. **The Overmind** *(vs Zerg + BOSS)* — the **Overlord** (4500 HP, siege damage) marches on your
     base once the clock hits ~7 minutes. Victory requires killing it specifically — razing the
     Zerg base alone won't end the mission. Bring Heavies.

## Controls

| Input | Action |
|---|---|
| **Left-click** | Select unit/building |
| **Left-drag** | Box-select multiple units |
| **Shift + click/drag** | Add/remove from selection |
| **Double-click** | Select all units of that type currently on screen |
| **Right-click** | Move / attack enemy / harvest mineral / set rally (production building selected) / fly-to (airborne building selected) |
| **A**, then left-click | Attack-move (combat units selected) |
| **S** / **H** | Stop / Hold position |
| **L** | Lift Off (grounded Command Center / Barracks / Factory) or choose a landing zone (while airborne) |
| **Ctrl + 1–9** | Assign control group |
| **1–9** | Recall control group (press twice quickly to also snap the camera there) |
| **Q / W / E / R / T** | Build hotkeys (worker selected) or train hotkeys (production building selected) |
| **W A S D / arrows / screen edge** | Pan camera (A/S pan only when nothing is selected — otherwise they're unit commands) |
| **Mouse wheel** | Zoom |
| **Shift while placing a building** | Queue multiple of the same building without reopening the hotbar |
| **Esc** | Cancel current order/placement, else open the Pause menu |
| **Minimap click/drag** | Move the camera |

## Economy & Roster

Start: 1 Command Center, 4 Workers, 50 minerals (campaign missions vary these), supply 4/10.
Workers auto-harvest the nearest crystal node — carry 5 per ~2s trip, visible crystal riding on
their back, deposit at any dropoff building. Supply: Command Center +10, Supply Depot +8, cap 200.

| Terran Unit | Cost | Supply | HP | Damage | Notes |
|---|---|---|---|---|---|
| Worker | 50 | 1 | 60 | 5 melee | Harvest + construct |
| Soldier | 50 | 1 | 120 | 8 normal, short range | Frontline |
| Ranger | 75 | 1 | 90 | 12 piercing, long range | Shreds light units, weak vs armor |
| Heavy | 150 | 3 | 350 | 25 siege, medium range, slow | Crushes armor & buildings |

Armor classes (Light / Armored / Structure) crossed with damage classes (Normal / Piercing /
Siege) form a rock-paper-scissors: Rangers beat infantry, Heavies beat armor/buildings, massed
Soldiers are the even matchup. Campaign enemies (Zergling, Hydralisk, Zealot, Stalker, and the
Overlord boss) plug into the same table with their own stats.

| Building | Cost / Build time | HP | Function |
|---|---|---|---|
| Command Center | 400 / 30s | 1500 | Trains Workers, mineral dropoff, +10 supply, **can lift off** |
| Supply Depot | 100 / 15s | 500 | +8 supply |
| Barracks | 150 / 20s | 900 | Trains Soldiers & Rangers, **can lift off** |
| Factory | 200 / 25s | 1100 | Trains Heavies, **can lift off** |
| Turret | 125 / 15s | 750 | Auto-attacking static defense, long range |

**Lift-off:** completed Command Centers, Barracks, and Factories can lift into the air (`L`), fly
anywhere (right-click while airborne), and land on any clear ground (`L` again, then click a
spot — rechecked for validity right before touchdown). Airborne buildings pause training and stop
accepting deliveries or melee attacks against them.

The AI opponent (Free Play or campaign) runs the same rules you do: real economy, worker-built
structures paid for in minerals, supply management, turret defense, and escalating attack waves
that scale with the mission. It's tuned to be beatable by a new player who actually plays well.

## Architecture

All game code lives under `Assets/Scripts` (asmdef `VoidClash.Runtime`), with the editor-only asset
generator in `Scripts/Editor` (asmdef `VoidClash.Editor`) and play-mode tests in `Assets/Tests`.

- **Data** — `UnitData` / `BuildingData` ScriptableObjects, baked into `Assets/ScriptableObjects`
  from a single source of truth (`DataDefs.cs`) and indexed by one `GameDatabase` asset in
  `Resources`. `Campaign.cs` holds the 3 `MissionDef`s (enemy race, army mix, wave timing, boss).
- **Bootstraps** — each scene is exactly one GameObject. `GameBootstrap` builds the entire match at
  runtime: lighting/skybox/fog, a URP post-processing Volume (bloom, ACES tonemapping, vignette,
  color grading, plus an SSAO renderer feature baked in by setup), the tactical map (`MapBuilder`:
  ground plane + grid overlay, border cliffs, ridge lanes, rock clusters, mineral fields), a
  runtime-baked NavMesh, starting bases, all managers, and the code-built UGUI HUD. `MenuBootstrap`
  does the same for the main menu (Campaign mission select, Free Play, Options).
- **Entities** — `Entity` (registry, fog visibility, selection ring/health bar) → `Unit`
  (NavMeshAgent state machine: Idle/Move/AttackMove/Attack/Hold), `WorkerUnit` (harvest cycle +
  construction), `Building` (construction progress, training queue, rally points, turret AI, and
  the lift-off/fly/land flight state machine). `Weapon`/`Projectile` handle combat; `Health` applies
  the armor/damage-class table.
- **Systems** — `SelectionManager` (selection + control groups), `InputController` (mouse/keyboard,
  drag-box, command routing, lift-off/land targeting mode), `BuildingPlacer` (ghost preview +
  validity via physics overlap and NavMesh probes — also used headlessly by the AI), `FogOfWar`
  (grid → overlay texture, per-cell enemy visibility), `Minimap` (CPU-composited texture: terrain,
  fog, unit dots, camera box, click-to-move), `EnemyAI` (economy/production/wave state machine,
  parametrized per mission), `GameManager` (win/lose including boss-kill victory, pause, scene
  flow, campaign progression).
- **Presentation** — `VisualFactory` assembles every unit/building from primitives (with a
  Zerg/Protoss material override for campaign enemies), `MaterialLibrary` / `TextureFactory`
  generate all URP materials/textures, `EffectsManager` builds every particle system in code
  (muzzle flashes, impacts, tracers, explosions with light flash + camera shake, construction dust,
  harvest sparkle, move/attack markers), `SynthLib` / `AudioManager` synthesize all SFX and the
  ambient music loop.

## Automated verification

`Assets/Tests/SmokeTests.cs` (play-mode; run via the Test Runner or
`-runTests -testPlatform PlayMode`):

1. **PlayerSystems_EconomyBuildingsUnitsFog** — mineral deposits and supply accounting; all 5
   buildings worker-constructed; all 4 Terran unit types trained; fog hides the enemy base and
   reveals with scouting; minimap texture stays alive throughout.
2. **FullMatch_AIReachesConclusion_NoConsoleErrors** — an unattended Free Play match at high speed:
   asserts the AI builds production, fields an army, and the match concludes — while failing on
   any console `Error`/`Exception`/`Assert` logged during the entire run.

## Repo layout — a note on Git LFS

`Build/` (the compiled player) is intentionally **not** committed. Git LFS was tried and reverted:
without a local git-lfs install, a plain `git clone` (or GitHub's "Download ZIP") only fetches tiny
pointer stubs for LFS-tracked files, silently producing a broken exe for anyone who doesn't have
LFS set up. Keeping the repo source-only means `git clone` just works for everyone; the playable
build will ship separately as a GitHub Release asset (a plain HTTP download, no LFS involved).

## Known rough edges (pre-alpha)

- The tactical map is currently flat with a grid overlay rather than sculpted terrain — a rolling
  heightfield was prototyped but reverted for stability; the map still reads clearly for gameplay.
- No line-of-sight blocking behind cliffs — vision is radius-based fog of war only.
- Balance and the campaign's pacing haven't had a real player-facing tuning pass yet.

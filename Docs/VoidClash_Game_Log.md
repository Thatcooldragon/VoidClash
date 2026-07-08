# VoidClash Game Log

## 2026-07-08 - v0.14.0 Pre-Alpha State

Latest commit:

- `a328f6e Add v0.14 campaign and free play polish`

Latest package:

- `VoidClash-v0.14.0-prealpha-win64.zip`
- Size: about 40.3 MB

Verification:

- Unity PlayMode tests passed: `16/16`
- Test file: `test_v14.xml`
- Windows build succeeded: `Build/VoidClash.exe`

v0.14 shipped:

- Bubble and Dots campaigns unlock independently from Terran.
- Free Play now clearly lets the player choose player race, AI race, and difficulty.
- Added three missions:
  - Bubble 4 - Glass Undertow
  - Dots 2 - Needle Orbit
  - Dots 3 - Giant Relay
- Premium-feel pass:
  - Closer default camera.
  - Commander power area markers.
  - Better mission briefing layout.
  - Updated menu/README version text.

Current state:

- The game is still pre-alpha, but it is now much closer to alpha-candidate shape.
- Core systems are broad: campaigns, Free Play, three playable race prototypes, AI, powers, fog, minimap, tests, builds.
- Biggest weakness is now visual presentation and screenshot quality.

## 2026-07-08 - v0.15.0 Battlefield Identity Update

Working title:

- **Battlefield Identity Update**

Implemented:

- Replaced the flat checker ground with sci-fi tactical floor plates.
- Added player/enemy base identity pads with faction glow.
- Added center beacon, lane guide lines, visible cliff-edge glow, and animated signal arrays.
- Added unit idle motion, building accent pulsing, melee swipe arcs, and damage-class impact sparks.
- Added race-accent HUD top edge and a cleaner objective strip.
- Added smoke-test coverage for `PlayerBasePad`, `EnemyBasePad`, and `CenterBeacon`.

Still pre-alpha:

- This is the first implemented slice of the big visual mind map, not a full alpha gate.
- Next visual targets are menu screenshots, briefing presentation, and richer race-specific FX.

Design rule:

- Do not add a fourth race or giant new mechanic before the current game looks presentable.

Main pillars:

- Battlefield material overhaul.
- Faction silhouette and animation pass.
- UI/HUD/menu reskin.
- Combat feedback and ability spectacle.
- Campaign presentation upgrade.
- Screenshot smoke tests.

Success sentence:

> VoidClash is still early, but now it looks like a real strategy game with a clear identity.

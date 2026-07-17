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

Release artifact:

- Target package: `VoidClash-v0.15.0-prealpha-win64.zip`

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

## 2026-07-08 - v0.16.0 Full Visual Identity Update

Working title:

- **Full Visual Identity Update**

Implemented across the mind map:

- First impression: v0.16 menu badge, Free Play matchup preview, campaign threat cards.
- Battlefield: faction atmosphere overlays, base pads, center beacon, lane/cliff glow, tactical plate ground.
- Factions: shared `FactionPalette`, race-tinted HUD, Zerg creep tint, Protoss field shine, Bubble foam, Dots orbit language.
- Animation/motion: idle motion, attack recoil, build-completion flashes, lift-off and landing rings, signal motion.
- Combat spectacle: damage-type impacts, projectile impact upgrade, melee arcs, Dots shape bursts, poison clouds, boss shockwave.
- Commander powers: airstrike aircraft shadow, stronger heal ring, race-colored ready states.
- UI/campaign: minimap frame, command-card accent, objective strip, briefing enemy threat panel, victory/defeat accent.
- Tech/verification: shared palette/helper components, v0.16 smoke assertions, existing screenshot smoke tool remains the image gate.

Release target:

- `VoidClash-v0.16.0-prealpha-win64.zip`

## 2026-07-17 - PC-Only Focus

Goal:

- Remove the temporary browser/mobile path and keep VoidClash focused on the PC version.

Implemented:

- Removed the WebGL build menu/command.
- Removed the touch-screen two-finger command shortcut.
- Removed browser hosting notes from the repo.
- Restored WebGL compression/decompression settings to their previous project values.

Verification:

- PC build path remains `VoidClash -> Build Windows EXE` and `BuildGame.Run`.

Current direction:

- Keep improving the PC RTS version we were already working on.

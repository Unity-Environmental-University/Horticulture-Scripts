# Horticulture Scripts

> Script-only slice of the Horticulture project — a Unity 6 experience that teaches Integrated Pest Management through hands-on plant care challenges.

## Snapshot

| Detail | Notes |
| --- | --- |
| Unity | 6 (6000.x series) |
| Platforms | Windows, macOS, Linux, Android, iOS |
| Repository Scope | Code-only mirror of the gameplay logic; assets and content ship from the main game repo |
| Status | Active development — expect experimental branches and in-flight systems |

## Highlights

- 🌱 Scenario-driven lessons that reinforce sustainable growing practices.
- 🪲 Dynamic pest identification and treatment workflows built on reusable card systems.
- 🧠 Modular architecture: MonoBehaviours orchestrate plain-C# subsystems for easier testing and iteration.

## Documentation Hub

| System | Reference |
| --- | --- |
| Classes | `docs/classes-system-documentation.md` |
| Cinematics | `docs/cinematics-system-documentation.md` |
| Audio | `docs/audio-system-documentation.md` |
| Game State | `docs/game-state-system-documentation.md` |
| Mod Loading | `docs/mod-loading-system-documentation.md` |
| Modding Guide | `docs/modding-guide.md` |

> Tip: Docs live alongside the scripts they describe. If you touch a system, audit its entry here before shipping.

## Build Access

- Desktop & Android builds land on the [GitHub Releases](https://github.com/Unity-Environmental-University/Horticulture-Scripts/releases) page.
- iOS players can join the public [TestFlight](https://testflight.apple.com/join/1f84McMq) to preview the latest validated drop.

## Testing Cheat Sheet

- Headless Play Mode (macOS): `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity -projectPath <Unity-project> -runTests -testPlatform PlayMode -logFile - -testResults results.xml`
- Headless Play Mode (Windows): `"C:\\Program Files\\Unity\\Hub\\Editor\\<version>\\Editor\\Unity.exe" -projectPath <path> -runTests -testPlatform PlayMode -logFile - -testResults results.xml`
- In-Editor: `Window > General > Test Runner` (PlayMode). Re-run focused suites whenever gameplay logic changes.

## Gameplay Radar

- **Card Holder Visibility**
  - Holders remain visible while hosting any card (location or persistent) even off-plant or between rounds.
  - Once the card expires or returns to the deck, holders revert: visible with a plant, hidden otherwise.
  - Anchoring code paths: `Card Core/DeckManager.cs` (`ClearAllPlants`, `ClearPlant`) and `Card Core/PlacedCardHolder.cs` (`ClearLocationCardByExpiry`, `ClearHolder`, `GiveBackCard`).
  - Guardrails: `PlayModeTest/CardHolderVisibilityTests.cs`.

## House Rules

- Treat this repo as the scripting source of truth; Unity assets stay in the primary game workspace.
- Keep private fields serialized, follow the `_camelCase` convention, and let helper classes shoulder complex logic.
- Feature work ships with matching Play Mode coverage and doc updates tracked in the table above.

# Unity Horticulture

**Project Description:** A game built with Unity 6 where players learn about and practice Integrated Pest Management.

**Key Features:**

*   Educational game focused on IPM techniques.
*   Players diagnose plant problems and implement sustainable solutions.

**Technical Details:**

*   **Unity Version:** 6
*   **Platform:** Windows, Mac, Linux, Android, & IOS

**Looking to for the Builds?**

*   **Windows, Mac, Linux, & Android:** Builds can be found on the [Releases](https://github.com/Unity-Environmental-University/Horticulture-Scripts/releases) tab in GitHub
*   **IOS:** For now, You can Join the Public IOS [TestFlight](https://testflight.apple.com/join/1f84McMq)

**Note:** This is a development project and may contain unfinished features. Additionally, this is a secondary repository and will not always reflect the current status of the project (I.E., Not all pushes will be reflected here)

## Documentation

- Classes System: `docs/ClassesSystemDocumentation.md`
- Cinematics System: `docs/CinematicsSystemDocumentation.md`
- Audio System: `docs/AudioSystemDocumentation.md`
- Game State System: `docs/GameStateSystemDocumentation.md`
- Mod Loading System: `docs/ModLoadingSystemDocumentation.md`
- Modding Guide: `docs/ModdingGuide.md`

## Testing

- Headless Play Mode (macOS):
  - `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity -projectPath <Unity-project> -runTests -testPlatform PlayMode -logFile - -testResults results.xml`
- Headless Play Mode (Windows):
  - `"C:\\Program Files\\Unity\\Hub\\Editor\\<version>\\Editor\\Unity.exe" -projectPath <path> -runTests -testPlatform PlayMode -logFile - -testResults results.xml`
- In-Editor: Window > General > Test Runner (PlayMode). Re-run relevant suites after every change.

## Gameplay Behavior

- Card Holder Visibility
  - Placed card holders stay visible if they currently hold a card (including location/persistent cards), even when no plant is present and between rounds.
  - When a location card expires or a card is removed, holders return to normal visibility: visible if a plant is present, hidden if not.
  - Relevant code paths:
    - `Card Core/DeckManager.cs`: `ClearAllPlants`, `ClearPlant` (respect `HoldingCard` when hiding holders)
    - `Card Core/PlacedCardHolder.cs`: `ClearLocationCardByExpiry`, `ClearHolder`, `GiveBackCard` (normalize visibility by plant presence)
  - Tests: `PlayModeTest/CardHolderVisibilityTests.cs`

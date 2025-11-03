# Cinematics System Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Core Components](#core-components)
- [API Reference](#api-reference)
- [Integration Patterns](#integration-patterns)
- [Configuration Guide](#configuration-guide)
- [Usage Examples](#usage-examples)
- [Dependencies](#dependencies)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Overview

The Cinematics System in Horticulture is a Unity Timeline-based cutscene and sequencing system that orchestrates story elements, UI animations, and game flow control. The system seamlessly integrates with the card game mechanics, robot AI, and player interaction systems to provide a cohesive narrative experience.

See also: `CLASSES_SYSTEM_DOCUMENTATION.md` for card, affliction, and shop type definitions used by cinematics.

**Key Features:**
- Unity Timeline integration for complex cutscenes
- Robot character sequencing and movement coordination
- UI input management during cinematics
- Card game flow synchronization
- Tutorial sequence orchestration
- Flexible scene skipping functionality

## Architecture

The cinematics system follows a modular architecture with three main components:

```
CinematicDirector (Core Controller)
├── Manages Unity Timeline playback
├── Integrates with CardGameMaster
└── Provides scene skipping functionality

RobotCardGameSequencer (Specialized Sequencer)
├── Coordinates robot movement and behavior
├── Manages UI animations during sequences
└── Synchronizes with card game flow

CutsceneUIController (Input Manager)
├── Controls UI input during cinematics
└── Integrates with CardGameMaster UI system
```

## Core Components

### CinematicDirector

The primary controller for all cinematic operations in the game.

**Namespace:** `_project.Scripts.Cinematics`

**Responsibilities:**
- Static management of Unity's PlayableDirector
- Timeline asset playback control
- Integration with card game ready states
- Scene skipping functionality

### RobotCardGameSequencer

Specialized sequencer for coordinating robot character interactions with the card game system.

**Namespace:** `_project.Scripts.Cinematics`

**Responsibilities:**
- Robot movement coordination
- UI animation synchronization
- Card game sequence timing
- Player interaction management

### CutsceneUIController

Lightweight controller for managing UI input states during cutscenes.

**Namespace:** `_project.Scripts.Cinematics`

**Responsibilities:**
- UI input module state management
- Integration with CardGameMaster UI system

## API Reference

### CinematicDirector

#### Properties

##### `director` (static)
```csharp
public static PlayableDirector director
```
Static reference to the Unity PlayableDirector component used throughout the game.

##### Timeline Assets
```csharp
public PlayableAsset introTimeline        // Introduction sequence
public PlayableAsset aphidsTimeline       // Aphids tutorial cinematic  
public PlayableAsset postAphidsTimeline   // Post-aphids sequence
```

#### Methods

##### `PlayScene(PlayableAsset timeline)` (static)
```csharp
public static void PlayScene(PlayableAsset timeline)
```
**Description:** Plays the specified timeline asset using the static director.

**Parameters:**
- `timeline` (PlayableAsset): The timeline asset to play

**Usage:**
```csharp
CinematicDirector.PlayScene(CardGameMaster.Instance.cinematicDirector.introTimeline);
```

##### `SkipScene()`
```csharp
public void SkipScene()
```
**Description:** Stops the currently playing timeline if one is active. Logs a skip confirmation message.

**Usage:**
```csharp
cinematicDirector.SkipScene();
```

### RobotCardGameSequencer

#### Serialized Fields

```csharp
[SerializeField] private RobotController robotController;  // Robot movement controller
[SerializeField] private GameObject player;                // Player reference object
[SerializeField] private GameObject frontOfPlayer;         // Target position for robot
[SerializeField] private Animation uiAnimator;             // UI animation controller
[SerializeField] private GameObject robotCameraObj;        // Robot's look target
```

#### Core Methods

##### `BeginCardGameSequence()` (private)
```csharp
private IEnumerator BeginCardGameSequence()
```
**Description:** Orchestrates the complete card game introduction sequence, including robot movement, UI animations, and timeline synchronization.

**Sequence Flow:**
1. Check if sequencing is enabled
2. Start UI animation and pause it
3. Move robot to front of player
4. Wait for timeline completion
5. Resume UI animation
6. Update robot look target

### CutsceneUIController

#### Methods

##### `OnEnable()` / `OnDisable()` (private)
```csharp
private void OnEnable()  // Enables CardGameMaster UI input
private void OnDisable() // Disables CardGameMaster UI input
```
**Description:** Automatically manages UI input module states based on GameObject active state.

## Integration Patterns

### 1. Card Game System Integration

The cinematics system integrates deeply with the card game through the `CardGameMaster`:

```csharp
// Setting up ready state synchronization
CardGameMaster.Instance.turnController.ReadyToPlay = () => director.state != PlayState.Playing;
```

This pattern ensures that:
- Card game actions wait for cinematics to complete
- Turn progression is synchronized with story beats
- UI states are properly managed during transitions

### 2. Robot AI Coordination

The robot sequencer coordinates with the `RobotController` for seamless character integration:

```csharp
robotController.currentLookTarget = robotCameraObj;
robotController.GoToNewLocation(frontOfPlayer.transform.position);
yield return new WaitUntil(robotController.HasReachedDestination);
```

### 3. Tutorial System Integration

The system integrates with tutorial progression through conditional checks:

```csharp
if (CardGameMaster.IsSequencingEnabled)
    PlayScene(introTimeline);
```

## Configuration Guide

### Setting Up CinematicDirector

1. **Create Timeline Assets**
   - Create PlayableAsset instances in Unity Timeline
   - Assign to appropriate timeline fields in the inspector

2. **Configure CardGameMaster Integration**
   - Ensure CinematicDirector reference is set in CardGameMaster
   - Verify timeline assets are properly assigned

3. **Scene Setup**
   - Place CinematicDirector on a persistent GameObject
   - Ensure PlayableDirector component exists in the scene

### Setting Up RobotCardGameSequencer

1. **Robot Dependencies**
   - Assign RobotController reference
   - Set player and frontOfPlayer GameObjects
   - Configure robotCameraObj for look targeting

2. **UI Animation Setup**
   - Assign Animation component for UI transitions
   - Ensure animation clips are properly configured

### Setting Up CutsceneUIController

1. **Simple Integration**
   - Attach to any GameObject that should control UI input during its lifetime
   - No additional configuration required

## Usage Examples

### Basic Timeline Playback

```csharp
// Play the introduction timeline
CinematicDirector.PlayScene(cinematicDirector.introTimeline);
```

### Conditional Cinematic Sequences

```csharp
// Tutorial-specific cinematic integration
if (level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn == 0)
{
    CinematicDirector.PlayScene(CardGameMaster.Instance.cinematicDirector.aphidsTimeline);
    yield return new WaitUntil(ReadyToPlay);
}
```

### Robot Sequence Coordination

```csharp
// Custom robot sequence (similar to RobotCardGameSequencer implementation)
private IEnumerator CustomRobotSequence()
{
    robotController.GoToNewLocation(targetPosition);
    yield return new WaitUntil(robotController.HasReachedDestination);
    
    // Play associated timeline
    CinematicDirector.PlayScene(customTimeline);
    yield return new WaitUntil(() => CinematicDirector.director.state != PlayState.Playing);
    
    // Continue with post-cinematic actions
    robotController.currentLookTarget = newTarget;
}
```

### UI Input Management

#### Why UIInputManager?

Prior to UIInputManager, direct manipulation of `uiInputModule.enabled` caused race conditions:

**The Problem:**
1. Cinematic enables UIInput (OnEnable)
2. Player skips cinematic → Popup appears
3. Popup enables UIInput (ownership transfer)
4. Cinematic OnDisable fires (Unity lifecycle quirk)
5. **BUG:** UIInput disabled despite popup being active

**The Solution:**
UIInputManager tracks ownership, ensuring only the current owner can disable UIInput.

**Usage Pattern:**

```csharp
// Proper UI input control for cinematics
public class CustomCinematicController : MonoBehaviour
{
    private void OnEnable()
    {
        UIInputManager.RequestEnable("CustomCinematic"); // Take ownership
    }

    private void OnDisable()
    {
        UIInputManager.RequestDisable("CustomCinematic"); // Release ownership
    }
}
```

#### Common UI Input Pitfalls

❌ **DON'T:** Directly access `CardGameMaster.Instance.uiInputModule.enabled`
✅ **DO:** Use `UIInputManager.RequestEnable("YourSystemName")`

❌ **DON'T:** Assume your OnDisable will execute before other systems take ownership
✅ **DO:** Use ownership-based requests that respect the current owner

❌ **DON'T:** Use generic owner names like "UI" or "Controller"
✅ **DO:** Use specific, traceable names like "PopUpController" or "CutsceneUI"

## Dependencies

### Unity Packages
- **Unity Timeline** - Core timeline functionality
- **Unity Playables** - PlayableDirector and PlayableAsset support

### Internal Systems
- **Card Core System** (`_project.Scripts.Card_Core`)
  - `CardGameMaster` - Central game controller
  - `TurnController` - Game flow management
- **Core System** (`_project.Scripts.Core`) 
  - `RobotController` - Robot AI and movement
- **UI System** (`_project.Scripts.UI`)
  - UI input management components

### External Dependencies
- **DOTween** (implied) - For smooth animations and transitions
- **Unity Input System** - UI input module management

## Testing

The cinematics system is tested through the `TurnTester` class which includes:

### Test Coverage Areas
- Timeline integration verification
- Robot sequencing coordination  
- UI state management validation
- Card game flow synchronization

### Key Test Patterns

```csharp
// Testing timeline integration
[Test]
public void TestCinematicIntegration()
{
    // Verify director setup
    Assert.IsNotNull(CinematicDirector.director);
    
    // Test ready state integration
    var readyState = CardGameMaster.Instance.turnController.ReadyToPlay;
    Assert.IsNotNull(readyState);
}
```

## Troubleshooting

### Common Issues

#### Timeline Not Playing
**Symptom:** Timeline assets don't play when `PlayScene` is called
**Solutions:**
- Verify PlayableDirector component exists in scene
- Check that timeline assets are properly assigned
- Ensure director reference is correctly initialized in Awake()

#### Robot Not Moving During Sequences
**Symptom:** Robot doesn't move to target positions during cinematics
**Solutions:**
- Verify RobotController reference is set
- Check NavMeshAgent configuration on robot
- Ensure target positions are on valid NavMesh areas
- Verify `HasReachedDestination()` logic is working correctly

#### UI Input Issues During Cinematics
**Symptom:** UI remains interactive during cutscenes or becomes unresponsive
**Solutions:**
- Check CutsceneUIController is properly attached and enabled
- Verify CardGameMaster.Instance.uiInputModule is accessible
- Ensure GameObject activation states match expected UI input states

#### Sequencing Timing Issues  
**Symptom:** Cinematics and gameplay elements are out of sync
**Solutions:**
- Verify `ReadyToPlay` delegate is properly set
- Check `WaitUntil` conditions in coroutines
- Ensure `CardGameMaster.IsSequencingEnabled` is configured correctly
- Review timeline lengths and transition timing

### Debug Logging

Enable debug logging by checking the debugging flags in relevant controllers:

```csharp
// In TurnController, set debugging = true for detailed logs
if (debugging)
    Debug.Log("[TurnController] Tutorial: PlaceTutorialPlants");
```

### Performance Considerations

- Timeline assets are loaded into memory - consider asset management for large cinematics
- Coroutines are used extensively - monitor for memory leaks in complex sequences
- Robot movement uses NavMeshAgent - ensure NavMesh performance is optimized
- UI input toggling happens frequently - monitor for input system overhead

---

**Version:** 1.0  
**Last Updated:** September 2025  
**Namespace:** `_project.Scripts.Cinematics`  
**Unity Version:** 6000.2.0f1+

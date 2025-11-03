# Developer Onboarding Guide

Welcome to the Horticulture Unity project! This guide will help you get up to speed with the codebase, development practices, and project structure.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Architecture](#project-architecture)
3. [Development Environment](#development-environment)
4. [Code Standards](#code-standards)
5. [Testing Approach](#testing-approach)
6. [Common Workflows](#common-workflows)
7. [Debugging Guide](#debugging-guide)
8. [Resources](#resources)

## Getting Started

### Prerequisites

Before you begin, ensure you have:
- **Unity 6000.2.10f1** or later installed
- **Git** for version control
- **IDE** (Visual Studio, JetBrains Rider, or VS Code with C# extension)
- **Basic Unity knowledge** (GameObjects, Components, Scripting)

### First Steps

1. **Clone and Setup**
   ```bash
   git clone [repository-url]
   cd Horticulture
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Open Project" and select the project folder
   - Wait for Unity to import all packages and compile scripts

3. **Verify Setup**
   - Open `Assets/_project/Scenes/MainScene`
   - Press Play - the game should load without errors
   - Try the tutorial to understand game mechanics

4. **Run Tests**
   - Open `Window > General > Test Runner`
   - Run all tests to ensure everything passes

## Project Architecture

### High-Level Overview

```
Horticulture (Educational Game)
│
├── Card Game System (Strategic gameplay)
│   ├── Deck management and card operations
│   ├── Turn-based progression system
│   └── Scoring and economy mechanics
│
├── Plant System (Core simulation)
│   ├── Plant lifecycle and health
│   ├── Affliction system (pests/diseases)
│   └── Treatment application logic
│
├── First-Person System (Exploration)
│   ├── Player movement and camera control
│   ├── Plant inspection mechanics
│   └── 3D interaction system
│
└── Game State System (Persistence)
    ├── Save/load functionality
    └── Progress tracking
```

### Key Namespaces

All project code lives under `_project.Scripts` with these main areas:

- **`_project.Scripts.Card_Core`** - Card game mechanics
- **`_project.Scripts.Core`** - Plant system and FPS controller
- **`_project.Scripts.GameState`** - Save/load system
- **`_project.Scripts.UI`** - User interface components
- **`_project.Scripts.Classes`** - Data structures and interfaces
- **`_project.Scripts.Audio`** - Sound management
- **`_project.Scripts.Stickers`** - Card modification system

### Core Components Deep Dive

#### CardGameMaster (Singleton)
The central orchestrator that coordinates all systems:
```csharp
public class CardGameMaster : MonoBehaviour
{
    public static CardGameMaster Instance { get; private set; }
    
    public DeckManager deckManager;      // Card operations
    public ScoreManager scoreManager;    // Economy & scoring
    public TurnController turnController; // Game flow
    public ShopManager shopManager;      // Card purchasing
}
```

#### DeckManager
Handles all card-related operations:
- Drawing and managing card hands
- Plant placement and removal
- Affliction application
- Card shuffling and deck management

#### PlantController
Manages individual plant behavior:
- Health and infection tracking
- Visual effects (particles, shaders)
- Treatment application
- Death/removal logic

#### TurnController
Controls game progression:
- Turn and round advancement
- Win/lose condition checking
- Tutorial sequence management
- Plant effect queuing

### Data Flow

1. **Game Start**: CardGameMaster initializes all systems
2. **Round Start**: DeckManager places plants on board
3. **Affliction Phase**: Random afflictions applied to plants
4. **Player Turn**: Cards drawn, player makes strategic decisions
5. **Turn End**: Treatments applied, afflictions processed
6. **Round End**: Score calculated, shop opened
7. **Save**: GameStateManager persists all data

## Development Environment

### Unity Configuration

**Required Packages** (automatically imported):
- DOTween Pro - Animation system
- Unity Input System - Modern input handling
- Universal Render Pipeline - Graphics rendering
- Unity Test Framework - Automated testing
- Unity Analytics - User data collection

**Editor Settings**:
- Script Compilation: All platforms enabled
- API Compatibility: .NET Framework
- Code Optimization: Development builds use debug mode

### IDE Setup

**Recommended Extensions**:
- Unity Tools (for Visual Studio)
- C# language support
- Git integration
- XML documentation preview

**Code Style**:
- 4 spaces for indentation
- Opening braces on new lines
- XML documentation for all public members
- Use `var` for local variables when type is obvious

### Version Control

**Branch Strategy**:
- `main` - Production-ready code
- `develop` - Integration branch
- `feature/*` - Individual features
- `fix/*` - Bug fixes

**Commit Guidelines**:
- Use descriptive commit messages
- Reference issue numbers when applicable
- Follow conventional commit format when possible

## Code Standards

### Naming Conventions

```csharp
// Classes - PascalCase
public class PlantController : MonoBehaviour

// Public members - PascalCase
public int CurrentHealth { get; set; }

// Private fields - camelCase with underscore prefix
private int _maxHealth;

// Methods - PascalCase
public void ApplyTreatment(ITreatment treatment)

// Constants - UPPER_SNAKE_CASE
private const int MAX_PLANTS_PER_LOCATION = 1;

// Local variables - camelCase
var plantController = GetComponent<PlantController>();
```

### Documentation Requirements

**XML Documentation** (Required for all public APIs):
```csharp
/// <summary>
/// Applies a treatment to the plant, potentially removing afflictions.
/// </summary>
/// <param name="treatment">The treatment to apply</param>
/// <returns>True if treatment was successful, false otherwise</returns>
public bool ApplyTreatment(ITreatment treatment)
{
    // Implementation
}
```

**Inline Comments** (For complex logic):
```csharp
// Calculate affliction spread chance based on adjacency
// Adjacent plants have 50% base chance, modified by plant type
var spreadChance = 0.5f * GetAdjacencyMultiplier(targetPlant.Type);
```

### Error Handling

**Defensive Programming**:
```csharp
public void ProcessAfflictions(List<PlantController> plants)
{
    if (plants == null)
    {
        Debug.LogWarning("Plants list is null, cannot process afflictions");
        return;
    }

    foreach (var plant in plants)
    {
        if (plant == null)
        {
            Debug.LogWarning("Found null plant in list, skipping");
            continue;
        }
        
        // Process plant afflictions
    }
}
```

**Unity Logging**:
- `Debug.Log()` - General information
- `Debug.LogWarning()` - Non-critical issues
- `Debug.LogError()` - Critical errors
- `Debug.LogException()` - Exception details

## Testing Approach

### Unit Testing Structure

Tests are located in `PlayModeTest/` assembly with NUnit framework:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class DeckManagerTests
{
    [Test]
    public void DrawActionHand_WithEmptyDeck_ShouldNotCrash()
    {
        // Arrange
        var deckManager = CreateTestDeckManager();
        
        // Act & Assert
        Assert.DoesNotThrow(() => deckManager.DrawActionHand());
    }
}
```

### Testing Guidelines

**What to Test**:
- Core game logic (card operations, scoring)
- Edge cases (empty decks, null values)
- Data serialization/deserialization
- Mathematical calculations

**What Not to Test**:
- Unity engine behavior
- Third-party library functionality
- Simple property getters/setters
- UI event handlers (test manually)

**Test Organization**:
- One test class per system component
- Descriptive test method names
- Arrange-Act-Assert pattern
- Setup/teardown for common test data

### Manual Testing

**Critical Test Areas**:
1. **Card Operations**: Selection, placement, removal
2. **Game Flow**: Turn progression, round advancement
3. **Save/Load**: Complete game state persistence
4. **Audio/Visual**: Feedback systems and animations
5. **Platform Compatibility**: Build and run on target platforms

## Common Workflows

### Adding a New Card Type

1. **Create Card Class**:
   ```csharp
   public class NewTreatmentCard : ICard
   {
       public string Name => "New Treatment";
       public PlantAfflictions.ITreatment Treatment => new NewTreatment();
       // Implement other interface members
   }
   ```

2. **Add Treatment Logic**:
   ```csharp
   public class NewTreatment : PlantAfflictions.ITreatment
   {
       public void ApplyTreatment(PlantController plant)
       {
           // Treatment implementation
       }
   }
   ```

3. **Update Card Lists**:
   - Add to appropriate deck in `DeckManager`
   - Include in shop inventory if purchasable

4. **Create Material**:
   - Add material to `Resources/Materials/Cards/`
   - Update card's `Material` property

5. **Test Implementation**:
   - Unit tests for treatment logic
   - Manual testing in-game

### Implementing New Game Features

1. **Design Phase**:
   - Document feature requirements
   - Identify affected systems
   - Plan data structure changes

2. **Implementation**:
   - Create necessary classes/components
   - Update existing systems for integration
   - Add appropriate error handling

3. **Testing**:
   - Unit tests for new functionality
   - Integration testing with existing systems
   - Manual verification of edge cases

4. **Documentation**:
   - Update XML documentation
   - Add to relevant documentation files
   - Include usage examples

### Debugging Complex Issues

1. **Unity Console**: Check for errors, warnings, and logs
2. **Visual Debugging**: Use gizmos and debug drawing
3. **Breakpoints**: Step through code execution
4. **Unity Profiler**: Performance analysis
5. **Test Isolation**: Create minimal reproduction cases

## Debugging Guide

### Common Issues and Solutions

**Cards Not Responding to Clicks**:
- Check `Click3D` component is enabled
- Verify `click3DGloballyDisabled` is false
- Ensure colliders are properly sized
- Confirm camera raycast layers

**Visual Issues**:
- Check material assignments in card classes
- Verify Resources.Load paths are correct
- Ensure shaders are compatible with URP
- Review particle system settings

**Save/Load Problems**:
- Verify all serializable data is marked [Serializable]
- Check file permissions in save directory
- Ensure backward compatibility with existing saves
- Test with various game states

**Performance Issues**:
- Use Unity Profiler to identify bottlenecks
- Check for memory leaks in card creation/destruction
- Optimize frequent Update() calls
- Consider object pooling for cards

### Debug Features

**CardGameMaster Debug Options**:
```csharp
public bool debuggingCardClass = true; // Enable card operation logging
```

**Plant System Debug**:
- Visual infection level indicators
- Particle system previews
- Treatment application logging

**Performance Monitoring**:
- Frame rate display
- Memory usage tracking
- Card operation metrics

## Resources

### Documentation

- **[README.md](../README.md)** - Project overview
- **[Card Core System](card-core-system.md)** - Complete card system documentation
- **[CLAUDE.md](../CLAUDE.md)** - AI development guidelines
- **Unity Documentation** - https://docs.unity3d.com/

### Learning Resources

- **Unity Learn** - https://learn.unity.com/
- **C# Programming Guide** - https://docs.microsoft.com/en-us/dotnet/csharp/
- **Game Architecture Patterns** - http://gameprogrammingpatterns.com/
- **Unity Best Practices** - https://unity.com/how-to/programming-best-practices-unity

### Tools and Extensions

- **DOTween Documentation** - http://dotween.demigiant.com/documentation.php
- **Unity Test Framework** - https://docs.unity3d.com/Packages/com.unity.test-framework@latest
- **Qodana Code Quality** - https://www.jetbrains.com/qodana/

### Community

- **Unity Forums** - https://forum.unity.com/
- **Unity Discord** - https://discord.gg/unity
- **Stack Overflow** - Unity3D tag

---

## Next Steps

Once you've completed this onboarding:

1. **Explore the Codebase**: Read through key scripts in Card Core/ and Core/
2. **Run Through Tutorial**: Play the game to understand mechanics
3. **Make a Small Change**: Try adding a simple feature or fixing a minor bug
4. **Review Process**: Follow the code review workflow in CLAUDE.md
5. **Ask Questions**: Don't hesitate to reach out to team members

Welcome to the team! The Horticulture project combines education, sustainability, and engaging gameplay. Your contributions will help teach players about Integrated Pest Management while creating an enjoyable gaming experience.

For immediate questions or clarifications, consult the team or refer to the detailed documentation in the `docs/` directory.

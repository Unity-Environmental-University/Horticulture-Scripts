# Horticulture - Testing Guide

This guide covers testing strategies, frameworks, and best practices for the Horticulture Unity project. It provides guidance for writing effective tests and maintaining code quality.

## Table of Contents

1. [Testing Strategy](#testing-strategy)
2. [Testing Framework Setup](#testing-framework-setup)
3. [Unit Testing](#unit-testing)
4. [Integration Testing](#integration-testing)
5. [Manual Testing](#manual-testing)
6. [Performance Testing](#performance-testing)
7. [Test Data Management](#test-data-management)
8. [Continuous Testing](#continuous-testing)
9. [Best Practices](#best-practices)

## Testing Strategy

### Testing Pyramid

Horticulture follows a traditional testing pyramid approach:

```
        /\
       /  \
      /    \  ← Manual Testing (Exploratory, Usability)
     /______\
    /        \
   /   E2E    \ ← Integration Testing (System flows)
  /____________\
 /              \
/   Unit Tests   \ ← Unit Testing (Business logic)
/________________\
```

### Test Categories

#### Unit Tests (70% of tests)
- **Scope**: Individual classes and methods
- **Focus**: Business logic, calculations, state management
- **Tools**: NUnit framework within Unity
- **Location**: `PlayModeTest/` assembly

#### Integration Tests (20% of tests)
- **Scope**: Component interactions and system workflows
- **Focus**: Card game flows, save/load operations, turn progression
- **Tools**: Unity Test Framework with scene testing
- **Location**: `PlayModeTest/` assembly with scene setup

#### Manual Tests (10% of tests)
- **Scope**: User experience, visual/audio feedback, platform compatibility
- **Focus**: Gameplay feel, accessibility, edge cases
- **Tools**: Manual test scripts and checklists
- **Location**: Documentation and test plans

### Testing Philosophy

1. **Test Behavior, Not Implementation**: Focus on what the code should do, not how it does it
2. **Fast Feedback**: Unit tests run quickly to support rapid development
3. **Reliable Tests**: Tests should be deterministic and not flaky
4. **Maintainable Tests**: Test code quality is as important as production code
5. **Coverage Goals**: Aim for 80% code coverage on business logic

## Testing Framework Setup

### Unity Test Framework

The project uses Unity's built-in testing framework based on NUnit.

#### Test Assembly Configuration

**PlayModeTest Assembly**:
```json
{
    "name": "PlayModeTest",
    "references": [
        "GUID:27619889b8ba8c24980f49ee34dbb44a", // Main Scripts assembly
        "GUID:0acc523941302664db1f4e527237feb3"  // Unity Test Framework
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false
}
```

#### Running Tests

**Via Unity Editor**:
1. Open `Window > General > Test Runner`
2. Select PlayMode or EditMode tab
3. Click "Run All" or select specific tests

**Via Command Line**:
```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode
```

### Test Organization

```
PlayModeTest/
├── CardSystem/
│   ├── DeckManagerTests.cs
│   ├── TurnControllerTests.cs
│   └── ScoreManagerTests.cs
├── PlantSystem/
│   ├── PlantControllerTests.cs
│   ├── PlantInfectLevelTest.cs
│   └── PlantManagerTests.cs
├── GameState/
│   ├── SaveLoadTests.cs
│   └── SerializationTests.cs
├── Integration/
│   ├── FullGameFlowTests.cs
│   └── TurnProgressionTests.cs
└── Utilities/
    ├── TestHelpers.cs
    └── MockObjects.cs
```

## Unit Testing

### Test Structure

Follow the **Arrange-Act-Assert** pattern:

```csharp
[Test]
public void DrawActionHand_WithEmptyDeck_ShouldNotThrow()
{
    // Arrange
    var deckManager = CreateTestDeckManager();
    deckManager.actionDeck.Clear(); // Empty deck
    
    // Act & Assert
    Assert.DoesNotThrow(() => deckManager.DrawActionHand());
}
```

### Core Components Testing

#### Card System Tests

**DeckManager Tests**:
```csharp
[TestFixture]
public class DeckManagerTests
{
    private DeckManager _deckManager;
    private CardGameMaster _cardGameMaster;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject();
        _cardGameMaster = go.AddComponent<CardGameMaster>();
        _deckManager = go.AddComponent<DeckManager>();
        // Initialize test deck
        _deckManager.actionDeck = CreateTestActionDeck();
    }

    [Test]
    public void DrawActionHand_ShouldDrawCorrectNumberOfCards()
    {
        // Arrange
        var expectedCount = _deckManager.cardsDrawnPerTurn;
        
        // Act
        _deckManager.DrawActionHand();
        
        // Assert
        Assert.AreEqual(expectedCount, _deckManager.actionHand.Count);
    }

    [Test]
    public void ShuffleDeck_ShouldChangeCardOrder()
    {
        // Arrange
        var originalOrder = _deckManager.actionDeck.ToList();
        
        // Act
        _deckManager.ShuffleDeck(_deckManager.actionDeck);
        
        // Assert
        CollectionAssert.AreNotEqual(originalOrder, _deckManager.actionDeck);
        CollectionAssert.AreEquivalent(originalOrder, _deckManager.actionDeck);
    }

    [TearDown]
    public void TearDown()
    {
        if (_cardGameMaster != null)
            Object.DestroyImmediate(_cardGameMaster.gameObject);
    }
}
```

#### Plant System Tests

**PlantController Tests**:
```csharp
[TestFixture]
public class PlantControllerTests
{
    private PlantController _plantController;
    private GameObject _plantObject;

    [SetUp]
    public void Setup()
    {
        _plantObject = new GameObject();
        _plantController = _plantObject.AddComponent<PlantController>();
        _plantController.PlantCard = new ColeusCard();
    }

    [Test]
    public void AddAffliction_ShouldIncreaseInfectLevel()
    {
        // Arrange
        var initialInfectLevel = _plantController.GetInfectLevel();
        var affliction = new PlantAfflictions.AphidsAffliction();
        
        // Act
        _plantController.AddAffliction(affliction);
        
        // Assert
        Assert.Greater(_plantController.GetInfectLevel(), initialInfectLevel);
        Assert.IsTrue(_plantController.HasAffliction(affliction));
    }

    [Test]
    public void RemoveAffliction_ShouldDecreaseInfectLevel()
    {
        // Arrange
        var affliction = new PlantAfflictions.AphidsAffliction();
        _plantController.AddAffliction(affliction);
        var infectLevelWithAffliction = _plantController.GetInfectLevel();
        
        // Act
        _plantController.RemoveAffliction(affliction);
        
        // Assert
        Assert.Less(_plantController.GetInfectLevel(), infectLevelWithAffliction);
        Assert.IsFalse(_plantController.HasAffliction(affliction));
    }

    [TearDown]
    public void TearDown()
    {
        if (_plantObject != null)
            Object.DestroyImmediate(_plantObject);
    }
}
```

#### Scoring System Tests

**ScoreManager Tests**:
```csharp
[TestFixture]
public class ScoreManagerTests
{
    private ScoreManager _scoreManager;
    private CardGameMaster _cardGameMaster;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject();
        _cardGameMaster = go.AddComponent<CardGameMaster>();
        _scoreManager = go.AddComponent<ScoreManager>();
        ScoreManager.SetMoneys(100); // Start with known amount
    }

    [Test]
    public void CalculateScore_WithHealthyPlants_ShouldReturnPositiveScore()
    {
        // Arrange
        SetupHealthyPlants();
        
        // Act
        var score = _scoreManager.CalculateScore();
        
        // Assert
        Assert.Greater(score, 0);
    }

    [Test]
    public void SubtractMoneys_ShouldDecreaseTotal()
    {
        // Arrange
        var initialMoney = ScoreManager.GetMoneys();
        var subtractAmount = 25;
        
        // Act
        ScoreManager.SubtractMoneys(subtractAmount);
        
        // Assert
        Assert.AreEqual(initialMoney - subtractAmount, ScoreManager.GetMoneys());
    }

    private void SetupHealthyPlants()
    {
        // Implementation to create test plants
    }
}
```

### Card Testing Patterns

#### Testing Card Behavior

```csharp
[TestFixture]
public class CardTests
{
    [Test]
    public void ColeusCard_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var originalCard = new ColeusCard();
        originalCard.ModifyValue(10);
        
        // Act
        var clonedCard = originalCard.Clone();
        clonedCard.ModifyValue(5);
        
        // Assert
        Assert.AreNotEqual(originalCard.Value, clonedCard.Value);
        Assert.AreNotSame(originalCard, clonedCard);
    }

    [Test]
    public void HorticulturalOilBasic_ApplyTreatment_ShouldRemoveAphids()
    {
        // Arrange
        var plant = CreateTestPlant();
        var affliction = new PlantAfflictions.AphidsAffliction();
        plant.AddAffliction(affliction);
        var treatment = new PlantAfflictions.HorticulturalOilTreatment();
        
        // Act
        treatment.ApplyTreatment(plant);
        
        // Assert
        Assert.IsFalse(plant.HasAffliction(affliction));
    }
}
```

### Mock Objects and Test Helpers

#### Test Helper Utilities

```csharp
public static class TestHelpers
{
    public static DeckManager CreateTestDeckManager()
    {
        var go = new GameObject();
        var deckManager = go.AddComponent<DeckManager>();
        deckManager.actionDeck = CreateTestActionDeck();
        deckManager.plantDeck = CreateTestPlantDeck();
        return deckManager;
    }

    public static List<ICard> CreateTestActionDeck()
    {
        return new List<ICard>
        {
            new HorticulturalOilBasic(),
            new SoapyWaterBasic(),
            new InsecticideBasic(),
            new FungicideBasic()
        };
    }

    public static PlantController CreateTestPlant()
    {
        var go = new GameObject();
        var plant = go.AddComponent<PlantController>();
        plant.PlantCard = new ColeusCard();
        return plant;
    }

    public static void CleanupGameObject(GameObject go)
    {
        if (go != null)
            Object.DestroyImmediate(go);
    }
}
```

#### Mock Card Implementation

```csharp
public class MockCard : ICard
{
    public string Name { get; set; } = "Mock Card";
    public string Description { get; set; } = "Test card";
    public int? Value { get; set; } = 0;
    public PlantAfflictions.IAffliction Affliction => null;
    public PlantAfflictions.ITreatment Treatment => null;
    public GameObject Prefab => null;
    public Material Material => null;
    public List<ISticker> Stickers { get; } = new();

    public ICard Clone() => new MockCard 
    { 
        Name = this.Name,
        Value = this.Value
    };

    public void Selected() { }
    public void ApplySticker(ISticker sticker) => Stickers.Add(sticker);
    public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
}
```

## Integration Testing

### Full Game Flow Tests

```csharp
[TestFixture]
public class GameFlowIntegrationTests
{
    private CardGameMaster _gamemaster;
    private GameObject _testScene;

    [SetUp]
    public void Setup()
    {
        _testScene = new GameObject("TestScene");
        _gamemaster = _testScene.AddComponent<CardGameMaster>();
        SetupCompleteGameSystem();
    }

    [UnityTest]
    public IEnumerator CompleteTurn_ShouldProgressGameState()
    {
        // Arrange
        var initialTurn = _gamemaster.turnController.currentTurn;
        
        // Act
        yield return _gamemaster.turnController.BeginTurnSequence();
        _gamemaster.turnController.EndTurn();
        
        // Assert
        Assert.AreEqual(initialTurn + 1, _gamemaster.turnController.currentTurn);
    }

    [UnityTest]
    public IEnumerator PlantTreatmentWorkflow_ShouldApplyTreatmentCorrectly()
    {
        // Arrange
        var plant = CreateAndPlacePlant();
        var affliction = new PlantAfflictions.AphidsAffliction();
        plant.AddAffliction(affliction);
        
        var treatmentCard = new HorticulturalOilBasic();
        _gamemaster.deckManager.actionHand.Add(treatmentCard);
        
        // Act
        _gamemaster.deckManager.SelectedACard = treatmentCard;
        var cardHolder = GetCardHolderForPlant(plant);
        cardHolder.TakeSelectedCard();
        
        yield return new WaitForSeconds(0.1f); // Allow treatment to process
        
        // Assert
        Assert.IsFalse(plant.HasAffliction(affliction));
        Assert.IsTrue(plant.CurrentTreatments.Any(t => t is PlantAfflictions.HorticulturalOilTreatment));
    }
}
```

### Save/Load Integration Tests

```csharp
[TestFixture]
public class SaveLoadIntegrationTests
{
    [Test]
    public void SaveAndLoad_ShouldPreserveGameState()
    {
        // Arrange
        var gamemaster = SetupGameWithKnownState();
        var originalTurn = gamemaster.turnController.currentTurn;
        var originalMoney = ScoreManager.GetMoneys();
        
        // Act
        gamemaster.Save();
        
        // Modify state to ensure load is actually restoring
        gamemaster.turnController.currentTurn = 999;
        ScoreManager.SetMoneys(0);
        
        gamemaster.Load();
        
        // Assert
        Assert.AreEqual(originalTurn, gamemaster.turnController.currentTurn);
        Assert.AreEqual(originalMoney, ScoreManager.GetMoneys());
    }
}
```

## Manual Testing

### Manual Test Checklists

#### Core Gameplay Checklist

**Card Operations**:
- [ ] Cards can be selected by clicking
- [ ] Selected card highlights appropriately
- [ ] Cards can be deselected by clicking again
- [ ] Cards can be placed on valid plant locations
- [ ] Invalid placements show appropriate feedback
- [ ] Cards can be picked up from placed locations
- [ ] Card swapping works correctly
- [ ] Redraw functionality works and costs money appropriately

**Plant Management**:
- [ ] Plants appear correctly when placed
- [ ] Health bars update when afflictions are applied
- [ ] Visual effects play when treatments are applied
- [ ] Plants die when health reaches zero
- [ ] Plant death triggers removal sequence correctly

**Turn Progression**:
- [ ] Turns advance correctly when "End Turn" is clicked
- [ ] Rounds advance after maximum turns
- [ ] Score calculation occurs at round end
- [ ] Shop opens appropriately between rounds
- [ ] Win/lose conditions trigger correctly

#### User Interface Checklist

**Interaction Feedback**:
- [ ] Hover effects work on interactive elements
- [ ] Click feedback is immediate and clear
- [ ] Audio cues play at appropriate times
- [ ] Visual animations are smooth and informative
- [ ] Text is readable at all supported resolutions

**Accessibility**:
- [ ] Color blind friendly color schemes
- [ ] Sufficient contrast ratios
- [ ] Keyboard navigation works where applicable
- [ ] Text scaling doesn't break layout

#### Performance Checklist

**Frame Rate**:
- [ ] Maintains 60fps during normal gameplay
- [ ] No significant frame drops during turn transitions
- [ ] Particle effects don't cause performance issues
- [ ] UI animations are smooth

**Memory Usage**:
- [ ] Memory usage remains stable over extended play
- [ ] No memory leaks during card creation/destruction
- [ ] Garbage collection doesn't cause noticeable hitches

### Platform-Specific Testing

#### Desktop Testing (Windows/Mac/Linux)

**Input Methods**:
- [ ] Mouse input works correctly
- [ ] Keyboard shortcuts function
- [ ] Window resizing maintains proper aspect ratio
- [ ] Alt-tab doesn't break game state

**File System**:
- [ ] Save files are created in correct location
- [ ] Save/load works with various file permissions
- [ ] Game handles missing save files gracefully

#### Mobile Testing (Future)

**Touch Input**:
- [ ] Touch gestures work appropriately
- [ ] Multi-touch doesn't cause issues
- [ ] Touch accuracy is sufficient for card selection

**Performance**:
- [ ] Maintains acceptable frame rate on target devices
- [ ] Battery usage is reasonable
- [ ] Doesn't cause device overheating

### Exploratory Testing

#### Edge Cases

**Unusual Player Behavior**:
- Rapidly clicking multiple cards
- Attempting to place cards on invalid locations
- Alt-tabbing during critical operations
- Closing game during save operations

**Boundary Conditions**:
- Maximum number of afflictions on single plant
- Zero money situations
- Full hand of expensive cards
- Empty decks in various scenarios

**Error Recovery**:
- Game behavior when save file is corrupted
- Recovery from network interruptions (future)
- Handling of missing asset files

## Performance Testing

### Performance Metrics

#### Target Performance

**Frame Rate**:
- Minimum: 30fps on minimum spec hardware
- Target: 60fps on recommended hardware
- Maximum: Unlimited with V-sync disabled option

**Memory Usage**:
- Maximum: 2GB RAM on desktop
- Mobile target: 512MB RAM (future)

**Load Times**:
- Game startup: < 10 seconds
- Scene transitions: < 2 seconds
- Save/load operations: < 1 second

### Performance Test Scripts

#### Frame Rate Monitoring

```csharp
[TestFixture]
public class PerformanceTests
{
    [UnityTest]
    public IEnumerator FrameRate_DuringNormalGameplay_ShouldMaintainTarget()
    {
        // Arrange
        var frameRateCounter = SetupFrameRateMonitoring();
        var gamemaster = SetupFullGameScene();
        
        // Act - Simulate typical gameplay
        for (int i = 0; i < 100; i++) // 100 frames
        {
            SimulatePlayerAction();
            yield return null;
        }
        
        // Assert
        var averageFrameRate = frameRateCounter.GetAverageFrameRate();
        Assert.GreaterOrEqual(averageFrameRate, 30f);
    }

    [UnityTest]
    public IEnumerator MemoryUsage_AfterExtendedPlay_ShouldNotGrowUnbounded()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act - Simulate extended gameplay
        for (int round = 0; round < 10; round++)
        {
            yield return SimulateCompleteRound();
        }
        
        // Force garbage collection
        GC.Collect();
        yield return null;
        
        // Assert
        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;
        
        Assert.Less(memoryGrowth, 100 * 1024 * 1024); // Less than 100MB growth
    }
}
```

## Test Data Management

### Test Data Organization

#### Static Test Data

```csharp
public static class TestData
{
    public static readonly List<ICard> StandardActionDeck = new()
    {
        new HorticulturalOilBasic(),
        new SoapyWaterBasic(),
        new InsecticideBasic(),
        new FungicideBasic(),
        new Panacea()
    };

    public static readonly List<ICard> StandardPlantDeck = new()
    {
        new ColeusCard(),
        new ChrysanthemumCard(),
        new PepperCard(),
        new CucumberCard()
    };

    public static GameStateData CreateTestGameState()
    {
        return new GameStateData
        {
            turnData = new TurnData { currentTurn = 1, currentRound = 1 },
            scoreData = new ScoreData { money = 100 },
            deckData = CreateTestDeckData(),
            plants = new List<PlantData>()
        };
    }
}
```

#### Dynamic Test Data Generation

```csharp
public static class TestDataGenerator
{
    public static List<PlantController> GenerateRandomPlants(int count)
    {
        var plants = new List<PlantController>();
        var plantTypes = Enum.GetValues(typeof(PlantType)).Cast<PlantType>()
            .Where(t => t != PlantType.NotYetSelected).ToArray();

        for (int i = 0; i < count; i++)
        {
            var plant = TestHelpers.CreateTestPlant();
            plant.type = plantTypes[UnityEngine.Random.Range(0, plantTypes.Length)];
            plants.Add(plant);
        }

        return plants;
    }

    public static void ApplyRandomAfflictions(PlantController plant, int maxAfflictions = 3)
    {
        var afflictionTypes = new Type[]
        {
            typeof(PlantAfflictions.AphidsAffliction),
            typeof(PlantAfflictions.MealyBugsAffliction),
            typeof(PlantAfflictions.ThripsAffliction),
            typeof(PlantAfflictions.MildewAffliction)
        };

        var afflictionCount = UnityEngine.Random.Range(1, maxAfflictions + 1);
        for (int i = 0; i < afflictionCount; i++)
        {
            var afflictionType = afflictionTypes[UnityEngine.Random.Range(0, afflictionTypes.Length)];
            var affliction = (PlantAfflictions.IAffliction)Activator.CreateInstance(afflictionType);
            plant.AddAffliction(affliction);
        }
    }
}
```

## Continuous Testing

### Automated Test Execution

#### Git Hooks

**Pre-commit Hook**:
```bash
#!/bin/sh
# Run unit tests before allowing commits
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode
if [ $? -ne 0 ]; then
    echo "Unit tests failed. Commit aborted."
    exit 1
fi
```

**Pre-push Hook**:
```bash
#!/bin/sh
# Run full test suite before pushing
Unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode
if [ $? -ne 0 ]; then
    echo "Integration tests failed. Push aborted."
    exit 1
fi
```

#### CI/CD Integration (Future)

**Unity Cloud Build Configuration**:
```yaml
# cloudbuild.yml
build:
  steps:
    - name: "Run Tests"
      command: "Unity -batchmode -quit -runTests -testResults results.xml"
    - name: "Parse Results"
      command: "parse-test-results results.xml"
```

### Test Reporting

#### Coverage Reports

```csharp
// Example coverage tracking
public static class TestCoverage
{
    private static readonly HashSet<string> _coveredMethods = new();
    
    public static void TrackMethodCall(string methodName)
    {
        _coveredMethods.Add(methodName);
    }
    
    public static void GenerateReport()
    {
        var report = new StringBuilder();
        report.AppendLine($"Methods covered: {_coveredMethods.Count}");
        foreach (var method in _coveredMethods.OrderBy(m => m))
        {
            report.AppendLine($"  - {method}");
        }
        
        File.WriteAllText("coverage-report.txt", report.ToString());
    }
}
```

## Best Practices

### Test Writing Guidelines

1. **One Assertion Per Test**: Each test should verify one specific behavior
2. **Descriptive Test Names**: Names should describe the scenario and expected outcome
3. **Independent Tests**: Tests should not depend on each other's execution order
4. **Fast Tests**: Unit tests should complete in milliseconds, not seconds
5. **Deterministic Tests**: Tests should produce the same result every time

### Test Maintenance

#### Keeping Tests Up to Date

```csharp
// Good: Test behavior that should remain stable
[Test]
public void DrawActionHand_ShouldMaintainDeckSize()
{
    var initialDeckSize = deckManager.actionDeck.Count;
    deckManager.DrawActionHand();
    Assert.AreEqual(initialDeckSize - deckManager.cardsDrawnPerTurn, 
                    deckManager.actionDeck.Count);
}

// Avoid: Testing implementation details that might change
[Test]
public void DrawActionHand_ShouldCallSpecificInternalMethods() // Don't do this
{
    // Testing internal implementation details
}
```

#### Refactoring Test Code

- **Extract Common Setup**: Use [SetUp] and helper methods
- **Remove Duplication**: Create reusable test utilities
- **Update Tests with Code Changes**: Maintain tests alongside production code
- **Delete Obsolete Tests**: Remove tests for removed functionality

### Common Testing Pitfalls

#### Unity-Specific Issues

1. **GameObject Lifecycle**: Always clean up created GameObjects in [TearDown]
2. **Coroutines in Tests**: Use [UnityTest] for coroutine-based testing
3. **Scene Dependencies**: Keep tests isolated from specific scenes
4. **Component Dependencies**: Mock Unity components when possible

#### General Testing Issues

1. **Flaky Tests**: Avoid timing dependencies and random behavior
2. **Overmocking**: Don't mock everything; integration tests are valuable
3. **Test Data Pollution**: Clean up test data between tests
4. **Assertion Overload**: Too many assertions make tests hard to debug

### Testing Documentation

Keep test documentation current with:
- Test strategy updates
- New testing patterns
- Framework upgrades
- Performance benchmarks

---

## Conclusion

Effective testing ensures the Horticulture game remains stable, performant, and maintainable as it evolves. The combination of unit tests, integration tests, and manual testing provides comprehensive coverage while supporting rapid development cycles.

Key testing principles:
- **Automate what you can**: Reduce manual testing burden with good automation
- **Test early and often**: Catch issues before they become expensive to fix
- **Maintain test quality**: Treat test code with the same care as production code
- **Focus on value**: Test the most important and risky parts of the system first

Regular review and improvement of testing practices ensures the test suite continues to provide value throughout the project lifecycle.
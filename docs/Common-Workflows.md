# Common Development Workflows

**Step-by-step guides for common development tasks in Horticulture.**

## 🎴 Adding a New Card Type

### Step 1: Create the Card Class

```csharp
// Assets/_project/Scripts/Classes/Cards/MyNewCard.cs
public class MyNewTreatmentCard : ICard
{
    public string Name => "My New Treatment";
    public string Description => "Treats specific afflictions";
    
    private int _value = -2; // Cost
    public int? Value 
    { 
        get => _value; 
        set => _value = value ?? 0; 
    }
    
    public PlantAfflictions.ITreatment Treatment => new MyNewTreatment();
    
    public Material Material => 
        Resources.Load<Material>("Materials/Cards/MyNewCard");
    
    public List<ISticker> Stickers { get; } = new();
    
    public ICard Clone() => new MyNewTreatmentCard { Value = this.Value };
    
    public void ModifyValue(int delta) => _value += delta;
    
    // Other ICard interface members...
}
```

### Step 2: Implement the Treatment Logic

```csharp
// In PlantAfflictions namespace
public class MyNewTreatment : ITreatment
{
    public void ApplyTreatment(PlantController plant)
    {
        // Remove specific afflictions
        var toRemove = plant.CurrentAfflictions
            .Where(a => a is MyTargetAffliction)
            .ToList();
            
        foreach (var affliction in toRemove)
        {
            plant.RemoveAffliction(affliction);
        }
    }
    
    public string Description => "Removes my target affliction";
}
```

### Step 3: Create the Card Material

1. In Unity: `Assets/Resources/Materials/Cards/`
2. Create new material: Right-click > Create > Material
3. Name it `MyNewCard`
4. Assign shader and textures
5. Configure properties

### Step 4: Add to Deck

```csharp
// In DeckManager.cs, add to appropriate deck
public void InitializeActionDeck()
{
    actionDeck = new List<ICard>
    {
        // Existing cards...
        new MyNewTreatmentCard(),
        new MyNewTreatmentCard() // Add multiple copies
    };
}
```

### Step 5: Test the Card

```csharp
[TestFixture]
public class MyNewCardTests
{
    [Test]
    public void MyNewTreatment_RemovesTargetAffliction()
    {
        // Arrange
        var plant = TestHelpers.CreateTestPlant();
        var affliction = new MyTargetAffliction();
        plant.AddAffliction(affliction);
        var treatment = new MyNewTreatment();
        
        // Act
        treatment.ApplyTreatment(plant);
        
        // Assert
        Assert.IsFalse(plant.HasAffliction(affliction));
    }
}
```

**Related**: [[card-core-system|Card System]], [[testing-guide|Testing Guide]]

---

## 🌱 Creating a New Plant Type

### Step 1: Add to PlantType Enum

```csharp
[Flags]
public enum PlantType
{
    NotYetSelected = 0,
    Coleus = 1 << 0,
    Pepper = 1 << 1,
    Cucumber = 1 << 2,
    Chrysanthemum = 1 << 3,
    MyNewPlant = 1 << 4  // Add here
}
```

### Step 2: Create Plant Card Class

```csharp
public class MyNewPlantCard : IPlantCard
{
    public string Name => "My New Plant";
    public PlantCardCategory Category => PlantCardCategory.Decorative;
    private int _value = 6;
    public int? Value { get; set; } = 6;
    public InfectLevel Infect { get; } = new();
    public int EggLevel { get; set; }
    
    public ICard Clone() => new MyNewPlantCard { Value = this.Value };
    
    // Other interface members...
}
```

### Step 3: Create Plant Prefab

1. Create new GameObject in scene
2. Add required components:
   - `PlantController`
   - `PlantCardFunctions`
   - `PlantHealthBarHandler`
   - Mesh renderer with your plant model
3. Save as prefab in `Assets/Prefabs/Plants/`

### Step 4: Add to DeckManager

```csharp
[SerializeField] private GameObject myNewPlantPrefab;

public void InitializePlantDeck()
{
    plantDeck = new List<ICard>
    {
        new MyNewPlantCard(),
        // Other plants...
    };
}
```

### Step 5: Configure Plant Behavior

```csharp
// If plant needs special behavior
public class MyNewPlantBehavior : MonoBehaviour
{
    private PlantController _controller;
    
    void Start()
    {
        _controller = GetComponent<PlantController>();
        // Custom initialization
    }
    
    public void ProcessCustomEffect()
    {
        // Custom plant-specific logic
    }
}
```

**Related**: [[Plant-System|Plant System]], [[plant-spots|Plant Spots]]

---

## 🐛 Bug Fixing Workflow

### Step 1: Reproduce the Issue

1. **Document the bug**:
   - What were you trying to do?
   - What happened instead?
   - How can you reproduce it?

2. **Isolate the issue**:
   - Does it happen every time?
   - In what scenarios does it occur?
   - Can you reproduce in a test scene?

### Step 2: Find the Root Cause

```csharp
// Add debug logging
Debug.Log($"[BUG DEBUG] Card selected: {SelectedACard?.Name ?? "null"}");
Debug.Log($"[BUG DEBUG] Holder state: {cardHolder.HoldingCard}");

// Add breakpoints in IDE
// Step through code to find where behavior diverges
```

### Step 3: Write a Failing Test

```csharp
[Test]
public void BugFix_CardSelection_ShouldNotCrashWithNullCard()
{
    // Arrange - Reproduce bug conditions
    deckManager.SelectedACard = null;
    
    // Act & Assert - Should not throw
    Assert.DoesNotThrow(() => {
        cardHolder.TakeSelectedCard();
    });
}
```

### Step 4: Implement the Fix

```csharp
// Before (buggy)
public void TakeSelectedCard()
{
    var card = deckManager.SelectedACard;
    PlaceCard(card); // Crashes if card is null!
}

// After (fixed)
public void TakeSelectedCard()
{
    var card = deckManager.SelectedACard;
    if (card == null)
    {
        Debug.LogWarning("No card selected");
        return;
    }
    PlaceCard(card);
}
```

### Step 5: Verify the Fix

1. Run the test (should now pass)
2. Manually test the original bug scenario
3. Test related functionality
4. Run full test suite to ensure no regressions

### Step 6: Document and Commit

```bash
git add .
git commit -m "Fix: Card placement crash with null selection

- Added null check in TakeSelectedCard
- Added defensive logging
- Added unit test to prevent regression

Fixes issue where clicking card holder with no card selected
would crash the game."
```

**Related**: [[testing-guide|Testing Guide]], [[Troubleshooting|Troubleshooting]]

---

## 🎨 Adding a New Visual Effect

### Step 1: Create the Particle System

1. In Unity Hierarchy: Right-click > Effects > Particle System
2. Configure particle properties:
   - Shape, emission, size over lifetime, color
3. Add to plant prefab or create standalone effect

### Step 2: Reference in Code

```csharp
public class MyEffectController : MonoBehaviour
{
    [SerializeField] private ParticleSystem myEffect;
    
    public void PlayEffect()
    {
        if (myEffect != null)
        {
            myEffect.Play();
        }
    }
}
```

### Step 3: Queue the Effect

```csharp
// Queue effect through TurnController
public void ApplyTreatmentWithEffect(PlantController plant)
{
    // Apply treatment logic
    treatment.ApplyTreatment(plant);
    
    // Queue visual feedback
    TurnController.QueuePlantEffect(
        plant,
        healParticles,
        healSound,
        0.3f // delay
    );
}
```

### Step 4: Test the Effect

- Play in editor and verify effect triggers correctly
- Check timing and duration
- Verify effect works on all plant types
- Test in builds, not just editor

**Related**: [[Plant-System|Plant System]], [[audio-system-documentation|Audio System]]

---

## 💾 Implementing Save/Load for New Features

### Step 1: Add Data to Save Structure

```csharp
// In GameStateData.cs
[Serializable]
public class GameStateData
{
    // Existing fields...
    public MyNewFeatureData myNewFeature; // Add this
}

[Serializable]
public class MyNewFeatureData
{
    public int someValue;
    public string someString;
    public List<string> someList;
}
```

### Step 2: Implement Save Logic

```csharp
// In your feature's manager class
public MyNewFeatureData SaveData()
{
    return new MyNewFeatureData
    {
        someValue = this.currentValue,
        someString = this.currentState,
        someList = this.items.Select(i => i.name).ToList()
    };
}
```

### Step 3: Implement Load Logic

```csharp
public void LoadData(MyNewFeatureData data)
{
    if (data == null) return;
    
    this.currentValue = data.someValue;
    this.currentState = data.someString;
    this.items = data.someList
        .Select(name => FindItemByName(name))
        .ToList();
}
```

### Step 4: Integrate with GameStateManager

```csharp
// In CardGameMaster.Save()
gameState.myNewFeature = myFeatureManager.SaveData();

// In CardGameMaster.Load()
myFeatureManager.LoadData(gameState.myNewFeature);
```

### Step 5: Test Save/Load

```csharp
[Test]
public void SaveLoad_PreservesMyFeatureState()
{
    // Arrange
    var manager = new MyFeatureManager();
    manager.currentValue = 42;
    
    // Act - Save
    var saveData = manager.SaveData();
    
    // Reset
    manager.currentValue = 0;
    
    // Act - Load
    manager.LoadData(saveData);
    
    // Assert
    Assert.AreEqual(42, manager.currentValue);
}
```

**Related**: [[game-state-system-documentation|Game State System]]

---

## 🧪 Writing Tests for New Features

### Step 1: Create Test File

```csharp
// In PlayModeTest/MyFeature/MyFeatureTests.cs
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class MyFeatureTests
{
    private MyFeatureManager _manager;
    
    [SetUp]
    public void Setup()
    {
        _manager = new GameObject().AddComponent<MyFeatureManager>();
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_manager.gameObject);
    }
}
```

### Step 2: Write Unit Tests

```csharp
[Test]
public void MyFeature_WhenActivated_ShouldChangeState()
{
    // Arrange
    Assert.IsFalse(_manager.IsActive);
    
    // Act
    _manager.Activate();
    
    // Assert
    Assert.IsTrue(_manager.IsActive);
}
```

### Step 3: Write Integration Tests

```csharp
[UnityTest]
public IEnumerator MyFeature_IntegrationWithCards_WorksCorrectly()
{
    // Arrange
    var cardSystem = SetupCardSystem();
    var myFeature = SetupMyFeature();
    
    // Act
    yield return cardSystem.ExecuteAction();
    yield return new WaitForSeconds(0.5f);
    
    // Assert
    Assert.IsTrue(myFeature.WasTriggered);
}
```

### Step 4: Run Tests

1. Open Test Runner (`Window > General > Test Runner`)
2. Click "Run All"
3. Fix any failures
4. Ensure all tests pass before committing

**Related**: [[testing-guide|Testing Guide]]

---

## 📝 Documenting New Features

### Step 1: Update Relevant Docs

1. **API Reference**: Add new public APIs
2. **System Docs**: Explain how feature works
3. **User Guide**: Document user-facing changes

### Step 2: Add Code Documentation

```csharp
/// <summary>
/// Manages the new feature's state and behavior.
/// </summary>
/// <remarks>
/// This manager coordinates between the card system and plant system
/// to provide enhanced gameplay functionality.
/// </remarks>
public class MyFeatureManager : MonoBehaviour
{
    /// <summary>
    /// Activates the feature for the specified target.
    /// </summary>
    /// <param name="target">The target to activate the feature on</param>
    /// <returns>True if activation was successful</returns>
    public bool Activate(GameObject target)
    {
        // Implementation
    }
}
```

### Step 3: Update Wiki Pages

- Add feature to relevant category pages
- Create feature-specific page if complex
- Update [[Quick-Reference]] with common APIs
- Add to [[Troubleshooting]] if needed

**Related**: [[Code-Standards|Coding Standards]]

---

## 🔗 Related Pages

- [[Quick-Reference|Quick Reference]]
- [[testing-guide|Testing Guide]]
- [[Troubleshooting|Troubleshooting]]
- [[Code-Standards|Coding Standards]]
- [[api-reference|API Reference]]

---

*Add your own workflows here as you discover efficient patterns!*

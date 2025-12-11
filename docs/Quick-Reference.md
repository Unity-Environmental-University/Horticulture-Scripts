# Quick Reference

**Quick access to common APIs, patterns, and workflows for Horticulture development.**

## 🎴 Card System Quick Ref

### Creating a New Card

```csharp
public class MyTreatmentCard : ICard
{
    public string Name => "My Treatment";
    public int? Value { get; set; } = -2; // Cost
    public PlantAfflictions.ITreatment Treatment => new MyTreatment();
    public ICard Clone() => new MyTreatmentCard();
}
```

### Common Card Operations

```csharp
// Get card game master
var cgm = CardGameMaster.Instance;

// Select a card
cgm.deckManager.SelectedACard = myCard;

// Draw hand
cgm.deckManager.DrawActionHand();

// Discard card
cgm.deckManager.DiscardActionCard(myCard);
```

See [[card-core-system|Card System Docs]]

## 🌱 Plant System Quick Ref

### Working with Plants

```csharp
// Add affliction
plant.AddAffliction(new PlantAfflictions.AphidsAffliction());

// Remove affliction
plant.RemoveAffliction(affliction);

// Check affliction
if (plant.HasAffliction(affliction)) { }

// Get infection level
int infectLevel = plant.GetInfectLevel();

// Apply treatment
treatment.ApplyTreatment(plant);
```

See [[Plant-System|Plant System Docs]]

## 💾 Save/Load Quick Ref

```csharp
// Save game
CardGameMaster.Instance.Save();

// Load game
CardGameMaster.Instance.Load();

// Check if save exists
if (GameStateManager.SaveExists()) { }
```

See [[game-state-system-documentation|Game State Docs]]

## 🎮 Game Flow Quick Ref

### Turn Progression

```csharp
// End turn
turnController.EndTurn();

// Start new turn
yield return turnController.BeginTurnSequence();

// Queue plant effect
TurnController.QueuePlantEffect(plant, particle, sound, 0.5f);
```

### Scoring

```csharp
// Calculate score
int score = scoreManager.CalculateScore();

// Get money
int money = ScoreManager.GetMoneys();

// Subtract money
ScoreManager.SubtractMoneys(amount);
```

## 🧪 Testing Quick Ref

### Unit Test Template

```csharp
[TestFixture]
public class MyComponentTests
{
    private MyComponent _component;

    [SetUp]
    public void Setup()
    {
        _component = new GameObject().AddComponent<MyComponent>();
    }

    [Test]
    public void MyMethod_WithCondition_ShouldDoExpected()
    {
        // Arrange
        var expected = 10;
        
        // Act
        var result = _component.MyMethod();
        
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_component.gameObject);
    }
}
```

See [[testing-guide|Testing Guide]]

## 🎨 UI Quick Ref

### Click3D Component

```csharp
// Setup click handler
click3D.OnClick.AddListener(() => {
    Debug.Log("Clicked!");
});

// Disable clicking globally
Click3D.click3DGloballyDisabled = true;
```

### CardView Setup

```csharp
var cardView = cardObject.GetComponent<CardView>();
cardView.Setup(myCard);
cardView.UpdateDisplay();
```

## 🔊 Audio Quick Ref

```csharp
// Get sound system
var soundSystem = CardGameMaster.Instance.soundSystem;

// Play sound
soundSystem.PlaySound(soundSystem.selectCard);

// Get insect sound
AudioClip sound = soundSystem.GetInsectSound(affliction);
```

## 🏷️ Common Event Patterns

```csharp
// Subscribe to event
void OnEnable()
{
    TurnController.OnTurnEnd += HandleTurnEnd;
}

// Unsubscribe
void OnDisable()
{
    TurnController.OnTurnEnd -= HandleTurnEnd;
}

// Handle event
void HandleTurnEnd(int turnNumber)
{
    Debug.Log($"Turn {turnNumber} ended");
}
```

## 📐 Common Patterns

### Singleton Access

```csharp
var gamemaster = CardGameMaster.Instance;
```

### Component Getting

```csharp
// Get component
var plantCtrl = GetComponent<PlantController>();

// Get in children
var cardView = GetComponentInChildren<CardView>();
```

### Coroutines

```csharp
// Start coroutine
StartCoroutine(MyCoroutine());

// Coroutine with delay
IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1f);
    // Do something
}
```

## 🐛 Debug Commands

```csharp
// Enable card debugging
CardGameMaster.Instance.debuggingCardClass = true;

// Log current game state
Debug.Log($"Turn: {turnController.currentTurn}, Money: {ScoreManager.GetMoneys()}");
```

## 📊 Common Values

### Default Game Values

- **Cards per turn**: 4
- **Turns per round**: 4
- **Redraw cost**: 3
- **Starting money**: 100
- **Money goal**: 100 (per level)

### Affliction Spread Rates

- **Adjacent spread**: 50% chance
- **Thrips global spread**: Can spread to any plant
- **Panacea protection**: Prevents spreading

## 🔗 Related Pages

- [[api-reference|Full API Reference]]
- [[Common-Workflows|Common Workflows]]
- [[Troubleshooting|Troubleshooting]]
- [[Code-Standards|Coding Standards]]

---

*For detailed documentation, see the respective system pages*

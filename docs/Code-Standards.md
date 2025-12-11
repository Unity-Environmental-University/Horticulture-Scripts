# Code Standards

**Coding conventions and best practices for Horticulture development.**

## 🎯 Core Principles

1. **Readability Over Cleverness** - Code is read more than written
2. **Consistency** - Follow established patterns
3. **Simplicity** - Simple solutions are better than complex ones
4. **Documentation** - Document intent, not just implementation
5. **Testability** - Write code that's easy to test

---

## 📝 Naming Conventions

### Classes

```csharp
// PascalCase for class names
public class PlantController : MonoBehaviour
public class CardGameMaster : MonoBehaviour
public interface ICard
public struct PlantData
```

### Methods

```csharp
// PascalCase for public methods
public void DrawActionHand()
public int CalculateScore()
public bool HasAffliction(IAffliction affliction)

// PascalCase for private methods too
private void InitializeDeck()
private IEnumerator PlacePlants()
```

### Fields & Properties

```csharp
// PascalCase for public properties
public int CurrentTurn { get; set; }
public string Name { get; }
public bool IsActive { get; private set; }

// _camelCase with underscore for private fields
private int _maxHealth;
private string _cardName;
private List<ICard> _actionDeck;

// camelCase for parameters and local variables
public void ApplyTreatment(PlantController plant, ITreatment treatment)
{
    var affliction = plant.CurrentAfflictions.FirstOrDefault();
    int treatmentCost = treatment.Cost;
}
```

### Constants

```csharp
// UPPER_SNAKE_CASE for constants
private const int MAX_CARDS_IN_HAND = 10;
private const float DEFAULT_CARD_SPACING = 1.5f;
public const string SAVE_FILE_NAME = "game_save.json";
```

### Unity Specific

```csharp
// SerializeField private fields
[SerializeField] private GameObject cardPrefab;
[SerializeField] private int cardsDrawnPerTurn = 4;

// Public fields for Unity Inspector (when necessary)
public AudioClip selectSound;
public Material defaultMaterial;
```

---

## 📄 File Organization

### File Structure

```
Assets/_project/Scripts/
├── Card Core/           # Card game mechanics
├── Core/               # Plant system, FPS controller
├── GameState/          # Save/load system
├── UI/                 # User interface
├── Classes/            # Data structures, interfaces
├── Audio/              # Sound management
└── Stickers/           # Card modification system
```

### One Class Per File

```csharp
// ✅ Good - PlantController.cs
public class PlantController : MonoBehaviour
{
    // Implementation
}

// ❌ Bad - Multiple unrelated classes in one file
public class PlantController { }
public class CardManager { }  // Should be in separate file
```

### File Naming

- Match filename to class name: `CardGameMaster.cs`
- Use PascalCase: `PlantHealthBarHandler.cs`
- Interfaces: `ICard.cs`, `IAffliction.cs`

---

## 🏗️ Code Structure

### Method Length

```csharp
// ✅ Good - Methods under 30 lines
public void DrawActionHand()
{
    ClearCurrentHand();
    int cardsToDraw = cardsDrawnPerTurn;
    
    for (int i = 0; i < cardsToDraw; i++)
    {
        if (actionDeck.Count == 0)
            ShuffleDiscardIntoDeck();
            
        DrawNextCard();
    }
}

// ❌ Bad - 100+ line methods
public void ProcessTurn()
{
    // ...100 lines of mixed concerns...
}
```

### Single Responsibility

```csharp
// ✅ Good - Each method does one thing
public void ApplyTreatment(PlantController plant)
{
    RemoveAffliction(plant);
    PlayTreatmentEffect(plant);
    UpdatePlantHealth(plant);
}

// ❌ Bad - Method does too much
public void ApplyTreatment(PlantController plant)
{
    // Removes affliction
    // Plays effect
    // Updates health
    // Saves game
    // Updates UI
    // Calculates score
    // ...
}
```

### Early Returns

```csharp
// ✅ Good - Early returns for clarity
public void TakeSelectedCard()
{
    if (SelectedACard == null)
        return;
        
    if (HoldingCard)
        return;
        
    if (!CanTakeCard())
        return;
        
    PlaceCard(SelectedACard);
}

// ❌ Bad - Nested conditionals
public void TakeSelectedCard()
{
    if (SelectedACard != null)
    {
        if (!HoldingCard)
        {
            if (CanTakeCard())
            {
                PlaceCard(SelectedACard);
            }
        }
    }
}
```

---

## 📚 Documentation

### XML Documentation

**Required for all public APIs:**

```csharp
/// <summary>
/// Applies a treatment to the specified plant, removing afflictions.
/// </summary>
/// <param name="plant">The plant to treat</param>
/// <param name="treatment">The treatment to apply</param>
/// <returns>True if treatment was successful</returns>
/// <exception cref="ArgumentNullException">
/// Thrown when plant or treatment is null
/// </exception>
public bool ApplyTreatment(PlantController plant, ITreatment treatment)
{
    // Implementation
}
```

### Inline Comments

```csharp
// ✅ Good - Explain WHY, not WHAT
// Calculate spread chance based on adjacent plants (50% base rate)
// to balance difficulty while maintaining challenge
var spreadChance = 0.5f * adjacencyMultiplier;

// ❌ Bad - Obvious comments
// Set spread chance to 0.5
var spreadChance = 0.5f;
```

### Complex Logic

```csharp
// ✅ Good - Explain complex algorithms
/// <summary>
/// Uses Fisher-Yates algorithm to shuffle deck in-place.
/// O(n) time complexity, ensures uniform distribution.
/// </summary>
public void ShuffleDeck(List<ICard> deck)
{
    for (int i = deck.Count - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);
        (deck[i], deck[j]) = (deck[j], deck[i]);
    }
}
```

### TODOs and FIXMEs

```csharp
// TODO: Implement affliction spreading for new plant types
// FIXME: Rare crash when plant dies during animation
// HACK: Temporary workaround until Unity fixes coroutine issue
```

---

## 🛡️ Error Handling

### Defensive Programming

```csharp
// ✅ Good - Validate inputs
public void ProcessPlants(List<PlantController> plants)
{
    if (plants == null)
    {
        Debug.LogWarning("Plants list is null");
        return;
    }
    
    foreach (var plant in plants)
    {
        if (plant == null)
        {
            Debug.LogWarning("Found null plant in list");
            continue;
        }
        
        ProcessPlant(plant);
    }
}
```

### Logging Levels

```csharp
// Information
Debug.Log("Game saved successfully");

// Warnings (recoverable)
Debug.LogWarning("Save file not found, creating new one");

// Errors (serious issues)
Debug.LogError("Failed to deserialize save data");

// Exceptions
try 
{
    LoadGame();
}
catch (Exception e)
{
    Debug.LogException(e);
}
```

### Avoid Silent Failures

```csharp
// ✅ Good - Report problems
public bool LoadGame()
{
    if (!File.Exists(savePath))
    {
        Debug.LogWarning($"Save file not found: {savePath}");
        return false;
    }
    
    try
    {
        // Load logic
        return true;
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to load game: {e.Message}");
        return false;
    }
}

// ❌ Bad - Silent failure
public void LoadGame()
{
    if (!File.Exists(savePath))
        return; // User has no idea why nothing happened
}
```

---

## 🎨 Unity Best Practices

### Component References

```csharp
// ✅ Good - Cache component references
private PlantController _plantController;

void Awake()
{
    _plantController = GetComponent<PlantController>();
}

void Update()
{
    if (_plantController.IsHealthy)
    {
        // Use cached reference
    }
}

// ❌ Bad - Get component every frame
void Update()
{
    var plant = GetComponent<PlantController>(); // Expensive!
    if (plant.IsHealthy) { }
}
```

### Serialization

```csharp
// ✅ Good - Use SerializeField for private fields
[SerializeField] private int cardsDrawnPerTurn = 4;
[SerializeField] private GameObject cardPrefab;

// Use properties for additional logic
public int CardsInHand => actionHand.Count;

// ❌ Avoid - Public fields without [SerializeField]
public int cardsDrawnPerTurn = 4; // Use only when necessary
```

### Coroutines

```csharp
// ✅ Good - Store coroutine reference for stopping
private Coroutine _plantEffectCoroutine;

public void StartPlantEffect()
{
    StopPlantEffect(); // Stop previous if running
    _plantEffectCoroutine = StartCoroutine(PlantEffectSequence());
}

public void StopPlantEffect()
{
    if (_plantEffectCoroutine != null)
    {
        StopCoroutine(_plantEffectCoroutine);
        _plantEffectCoroutine = null;
    }
}
```

### GameObject Lifecycle

```csharp
// ✅ Good - Cleanup in OnDestroy
private List<GameObject> _spawnedObjects = new();

void OnDestroy()
{
    foreach (var obj in _spawnedObjects)
    {
        if (obj != null)
            Destroy(obj);
    }
    _spawnedObjects.Clear();
}
```

---

## 🧪 Testing Standards

### Test Naming

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Test]
public void DrawActionHand_WithEmptyDeck_ShouldShuffleDiscard()

[Test]
public void ApplyTreatment_WithAfflictedPlant_ShouldRemoveAffliction()

[Test]
public void CalculateScore_WithHealthyPlants_ShouldReturnPositiveValue()
```

### Test Structure

```csharp
[Test]
public void MyTest()
{
    // Arrange - Setup test conditions
    var plant = CreateTestPlant();
    var treatment = new TestTreatment();
    
    // Act - Execute the behavior
    treatment.ApplyTreatment(plant);
    
    // Assert - Verify results
    Assert.IsFalse(plant.HasAffliction);
}
```

### Test Isolation

```csharp
// ✅ Good - Independent tests
[SetUp]
public void Setup()
{
    _gameObject = new GameObject();
    _component = _gameObject.AddComponent<MyComponent>();
}

[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(_gameObject);
}
```

**Related**: [[testing-guide|Testing Guide]]

---

## 🚫 Common Anti-Patterns to Avoid

### Avoid Magic Numbers

```csharp
// ❌ Bad
if (health < 50) { }
for (int i = 0; i < 4; i++) { }

// ✅ Good
private const int LOW_HEALTH_THRESHOLD = 50;
private const int MAX_CARDS_PER_TURN = 4;

if (health < LOW_HEALTH_THRESHOLD) { }
for (int i = 0; i < MAX_CARDS_PER_TURN; i++) { }
```

### Avoid String Literals

```csharp
// ❌ Bad
var material = Resources.Load<Material>("Materials/Cards/Coleus");

// ✅ Good
private const string CARD_MATERIALS_PATH = "Materials/Cards";
var material = Resources.Load<Material>($"{CARD_MATERIALS_PATH}/Coleus");
```

### Avoid Excessive Nesting

```csharp
// ❌ Bad - Too deeply nested
if (condition1)
{
    if (condition2)
    {
        if (condition3)
        {
            // Logic here
        }
    }
}

// ✅ Good - Early returns
if (!condition1) return;
if (!condition2) return;
if (!condition3) return;

// Logic here
```

### Avoid God Objects

```csharp
// ❌ Bad - Class does everything
public class GameManager
{
    public void HandleCards() { }
    public void HandlePlants() { }
    public void HandleUI() { }
    public void HandleAudio() { }
    public void HandleSaveLoad() { }
    // ... 50 more responsibilities
}

// ✅ Good - Single responsibility
public class CardManager { }
public class PlantManager { }
public class UIManager { }
// Each class has one clear purpose
```

---

## 📊 Performance Guidelines

### Avoid Allocations in Update

```csharp
// ❌ Bad
void Update()
{
    var list = new List<int>(); // Allocation every frame!
    string message = "Health: " + health; // String allocation!
}

// ✅ Good
private List<int> _reusableList = new();

void Update()
{
    _reusableList.Clear();
    // Reuse list
}
```

### Cache Frequently Used Values

```csharp
// ✅ Good
private Transform _transform;

void Awake()
{
    _transform = transform; // Cache it
}

void Update()
{
    _transform.position = newPosition;
}
```

### Use Object Pooling

```csharp
// ✅ Good - For frequently created/destroyed objects
public class CardPool
{
    private Queue<GameObject> _pool = new();
    
    public GameObject Get()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();
            
        return CreateNew();
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

---

## 🔄 Version Control

### Commit Messages

```bash
# Format: Type: Brief description
# 
# Longer explanation if needed

# Examples:
git commit -m "Fix: Card placement crash with null selection"
git commit -m "Feature: Add location card system"
git commit -m "Refactor: Extract plant effect logic to separate class"
git commit -m "Docs: Update API reference for new card types"
git commit -m "Test: Add integration tests for save/load"
```

### Branch Naming

```bash
feature/location-cards
fix/card-placement-crash
refactor/plant-system-cleanup
docs/update-api-reference
```

---

## 🔗 Related Resources

- [[Quick-Reference|Quick Reference]]
- [[Common-Workflows|Common Workflows]]
- [[testing-guide|Testing Guide]]
- [[developer-onboarding|Developer Onboarding]]
- [[ARCHITECTURE|Architecture Overview]]

---

*These standards evolve - suggest improvements when you find better patterns!*

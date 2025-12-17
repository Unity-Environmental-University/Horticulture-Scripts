# PlantHolder System

**Last Updated:** 2024-12-17
**Location:** `Assets/_project/Scripts/Card Core/PlantHolder.cs`

## Overview

The `PlantHolder` class is a wrapper for plant location Transforms that provides component caching and a convenient API for managing plant locations and their associated card holders. It was introduced to replace the direct use of `List<Transform>` in `DeckManager.plantLocations`.

## Purpose

**Before PlantHolder:**
```csharp
// Old approach: Direct Transform list
public List<Transform> plantLocations;

// Required frequent GetComponentsInChildren calls
var cardHolders = plantLocation.GetComponentsInChildren<PlacedCardHolder>();
```

**After PlantHolder:**
```csharp
// New approach: PlantHolder wrapper with caching
public List<PlantHolder> plantLocations;

// CardHolders are pre-cached, no GetComponent call needed
var cardHolders = plantLocation.CardHolders; // O(1) access
```

## Key Benefits

1. **Performance**: Pre-caches child `PlacedCardHolder` components, eliminating repeated `GetComponentsInChildren` calls
2. **Convenience**: Clean API for accessing position, rotation, transform, and card holders
3. **Backward Compatibility**: Implicit operators allow seamless integration with existing code
4. **Type Safety**: Encapsulates plant location concept vs. generic Transform

## Architecture

### Class Structure

```csharp
[Serializable]
public class PlantHolder
{
    [SerializeField] private Transform plantLocation;
    [SerializeField] private List<PlacedCardHolder> placedCardHolders;

    // Properties
    public Transform Transform { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public IReadOnlyList<PlacedCardHolder> CardHolders { get; }

    // Methods
    public void InitializeCardHolders()
}
```

### Component Caching

Card holders are discovered and cached during initialization:

```csharp
public void InitializeCardHolders()
{
    if (!plantLocation) return;
    placedCardHolders = plantLocation.GetComponentsInChildren<PlacedCardHolder>(true).ToList();
}
```

**When Initialization Occurs:**
- `DeckManager.Start()` calls `InitializePlantHolders()` for all locations
- Manual construction via `new PlantHolder(transform, initializeCardHolders: true)`
- OnValidate during Editor migration from legacy Transform list

## API Reference

### Properties

#### Transform
```csharp
public Transform Transform => plantLocation;
```
Returns the underlying Transform for direct access when needed.

#### Position
```csharp
public Vector3 Position => plantLocation ? plantLocation.position : Vector3.zero;
```
Convenient access to world position without null checks.

#### Rotation
```csharp
public Quaternion Rotation => plantLocation ? plantLocation.rotation : Quaternion.identity;
```
Convenient access to world rotation without null checks.

#### CardHolders
```csharp
public IReadOnlyList<PlacedCardHolder> CardHolders => placedCardHolders;
```
Pre-cached list of child PlacedCardHolder components. Returns cached data, not a live query.

### Methods

#### InitializeCardHolders()
```csharp
public void InitializeCardHolders()
```
Discovers and caches child PlacedCardHolder components. Called during scene initialization.

**When to call:**
- After hierarchy changes that add/remove PlacedCardHolder components
- After scene loading or prefab instantiation
- Automatically called by DeckManager.Start()

### Constructors

#### Default Constructor
```csharp
public PlantHolder()
```
Required for Unity serialization. Creates empty PlantHolder.

#### Transform Constructor
```csharp
public PlantHolder(Transform location, bool initializeCardHolders = false)
```
Creates PlantHolder from Transform reference.

**Parameters:**
- `location`: The Transform representing the plant location
- `initializeCardHolders`: If true, immediately discovers and caches child card holders

**Example:**
```csharp
var holder = new PlantHolder(myTransform, initializeCardHolders: true);
```

### Implicit Operators

#### Transform Conversion
```csharp
public static implicit operator Transform(PlantHolder holder)
```
Allows PlantHolder to be used where Transform is expected.

**Example:**
```csharp
PlantHolder holder = plantLocations[0];
Transform t = holder; // Implicit conversion
var component = holder.GetComponent<PlantController>(); // Works!
```

**Note:** For explicit GetComponent calls, use `.Transform` property for clarity:
```csharp
var component = holder.Transform.GetComponent<PlantController>(); // Clearer
```

#### Boolean Conversion
```csharp
public static implicit operator bool(PlantHolder holder)
```
Enables null/validity checks without verbose syntax.

**Example:**
```csharp
if (plantHolder) // Implicitly checks if holder and transform are valid
{
    // Safe to use
}

// Equivalent to:
if (plantHolder != null && plantHolder.Transform != null)
```

## Usage Patterns

### Basic Access Pattern
```csharp
// Access in DeckManager or TurnController
foreach (var holder in plantLocations)
{
    if (!holder) continue; // Null check via implicit operator

    var plantTransform = holder.Transform;
    var position = holder.Position;
    var cardHolders = holder.CardHolders; // No GetComponent call!

    // Work with cached card holders
    foreach (var cardHolder in cardHolders)
    {
        // ...
    }
}
```

### Plant Placement
```csharp
var location = plantLocations[i];
var plant = Instantiate(prefab, location.Position, location.Rotation);
plant.transform.SetParent(location.Transform);
```

### Card Holder Operations
```csharp
foreach (var location in plantLocations)
{
    if (!location) continue;

    // Access pre-cached card holders (performance optimized)
    foreach (var cardHolder in location.CardHolders)
    {
        if (cardHolder && !cardHolder.HoldingCard)
            cardHolder.ToggleCardHolder(hasPlant);
    }
}
```

### Where Clause Pattern
```csharp
var validLocations = plantLocations
    .Where(location => location) // Implicit bool conversion
    .ToList();
```

## Integration with DeckManager

### Initialization
```csharp
private void Start()
{
    InitializePlantHolders();
    // ...
}

private void InitializePlantHolders()
{
    if (plantLocations == null) return;
    foreach (var holder in plantLocations)
        holder?.InitializeCardHolders();
}
```

### Plant Management
```csharp
public IEnumerator PlacePlants()
{
    for (var i = 0; i < plantLocations.Count; i++)
    {
        var location = plantLocations[i];
        if (!location) continue;

        var plant = Instantiate(prefab, location.Position, location.Rotation);
        plant.transform.SetParent(location.Transform);

        // Access cached card holders without GetComponent
        foreach (var cardHolder in location.CardHolders)
        {
            cardHolder.ToggleCardHolder(true);
        }
    }
}
```

## Migration from Legacy Transform List

### Automatic Migration

The system includes automatic migration for scenes using the old `List<Transform>` format.

**OnValidate Migration (Automatic in Editor):**
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    if (_legacyPlantLocations != null && _legacyPlantLocations.Count > 0)
    {
        if (plantLocations == null || plantLocations.Count == 0)
        {
            plantLocations = new List<PlantHolder>();
            foreach (var transform in _legacyPlantLocations)
            {
                if (transform)
                    plantLocations.Add(new PlantHolder(transform, initializeCardHolders: false));
            }
            _legacyPlantLocations.Clear();
        }
    }
}
#endif
```

### Editor Migration Tool

For batch migration of multiple scenes:

1. Open Unity Editor
2. Navigate to: `Tools > Migration > Migrate DeckManager Plant Locations (Transform -> PlantHolder)`
3. Choose "Migrate Current Scene" or "Migrate All Project Scenes"

See [plant-holder-migration-solution.md](plant-holder-migration-solution.md) for detailed migration guide.

### Manual Setup for New Scenes

When creating new scenes:

1. Add plant location GameObjects to scene
2. Add DeckManager component to appropriate GameObject
3. In Inspector, populate `plantLocations` list:
   - Click "+" to add element
   - Drag plant location GameObject to the Transform field
   - PlantHolder is created automatically
4. CardHolders initialize automatically at runtime via `InitializePlantHolders()`

## Performance Considerations

### Component Caching Benefits

**Before PlantHolder:**
```csharp
// Called every frame or multiple times per operation
var cardHolders = plantLocation.GetComponentsInChildren<PlacedCardHolder>();
// O(n) cost where n = child count, involves hierarchy traversal
```

**After PlantHolder:**
```csharp
// One-time initialization cost
holder.InitializeCardHolders(); // O(n) once at startup

// Subsequent accesses are O(1)
var cardHolders = holder.CardHolders; // Cached list access
```

### When to Re-initialize

Card holder cache is only valid while the hierarchy remains unchanged. Re-initialize after:
- Adding/removing PlacedCardHolder components
- Changing scene hierarchy
- Instantiating new card holder prefabs

**Note:** The current implementation assumes card holders remain static after initialization. Dynamic hierarchy changes are not automatically detected.

## Testing

The PlantHolder system is tested through integration tests:

**Test Coverage:**
- `CardHolderVisibilityTests.cs` - Card holder visibility with PlantHolder
- `PrepareNextRoundTests.cs` - Round preparation with PlantHolder
- `PlantDeathCardPlacementTests.cs` - Plant removal and card holder cleanup
- `IsolationCardTests.cs` - Location card persistence through PlantHolder

## Design Rationale

### Why Not Use Direct Transform?

**Problem with Direct Transform:**
- No semantic meaning (is this Transform a plant location, UI element, camera?)
- Repeated GetComponentsInChildren calls caused performance overhead
- No centralized place for plant location logic

**Benefits of PlantHolder Wrapper:**
- Clear semantic meaning: This is a plant location, not just any Transform
- Performance optimization through caching
- Extensible: Can add plant location-specific logic in future
- Type safety: Compiler enforces correct usage

### Backward Compatibility Design

Implicit operators were added to minimize refactoring burden:

```csharp
// These all work without code changes:
Transform t = plantLocation; // Works via implicit operator
if (plantLocation) { } // Works via implicit bool operator
var component = plantLocation.GetComponent<T>(); // Works via implicit Transform conversion
```

**Trade-off:** Implicit operators can hide the wrapper abstraction. For new code, prefer explicit property access:
```csharp
// Preferred for clarity
var transform = holder.Transform;
var position = holder.Position;
```

## Future Enhancements

Potential improvements for future versions:

1. **Automatic Cache Invalidation**: Detect hierarchy changes and auto-reinitialize
2. **Editor Visualization**: Custom scene view gizmos for plant locations
3. **Validation**: Editor warnings if card holders not initialized
4. **Runtime Creation**: Helper methods for procedural plant location generation
5. **Event System**: Notify listeners when card holders are added/removed

## Related Documentation

- [plant-holder-migration-solution.md](plant-holder-migration-solution.md) - Migration guide for legacy scenes
- [card-core-system.md](card-core-system.md) - Card Core system architecture
- [architecture.md](architecture.md) - Overall game architecture

---

**Key Takeaway:** PlantHolder is a performance-optimized wrapper that provides semantic clarity and caching for plant location management. Use `.Transform` for explicit Transform access and `.CardHolders` for cached component access.
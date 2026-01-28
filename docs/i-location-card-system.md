# ILocationCard System Documentation

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architecture Overview](#architecture-overview)  
3. [Core Components](#core-components)
4. [API Reference](#api-reference)
5. [Implementation Guide](#implementation-guide)
6. [Performance Considerations](#performance-considerations)
7. [Error Handling](#error-handling)
8. [Testing Guidelines](#testing-guidelines)
9. [Integration Examples](#integration-examples)
10. [Troubleshooting](#troubleshooting)

## Executive Summary

The ILocationCard system provides persistent location-based effects for plant spots in the Horticulture game. Unlike regular action cards, location cards remain active across multiple turns, automatically applying their effects to plants placed at specific locations. The system features robust error handling, performance optimization through caching, and seamless integration with the existing card game architecture.

**Key Features:**
- Immediate placement effects plus turn-based processing
- Automatic effect expiration and cleanup
- Plant cache optimization with dirty flags  
- Comprehensive error handling and recovery
- Race condition protection
- Support for both permanent and temporary effects

## Architecture Overview

### System Components

The ILocationCard system consists of three main components working together:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   ILocationCard │◄──►│  SpotDataHolder │◄──►│PlacedCardHolder │
│    Interface    │    │   (Controller)  │    │  (Integration)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         ▲                       ▲                       ▲
         │                       │                       │
         │                       │                       │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ FertilizerBasic │    │ Plant Caching   │    │Turn Integration │
│(Implementation) │    │ Error Recovery  │    │Card Restrictions│
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Data Flow

1. **Card Placement**: Player places location card → `PlacedCardHolder.TakeSelectedCard()`
2. **Effect Activation**: `SpotDataHolder.OnLocationCardPlaced()` called
3. **Turn Processing**: `TurnController` calls `SpotDataHolder.ProcessTurn()`
4. **Effect Application**: Location card effects applied to associated plant
5. **Expiry Handling**: `ProcessTurn()` marks expiry, `FinalizeLocationCardTurn()` performs cleanup

## Core Components

### ILocationCard Interface

```csharp
/// <summary>
/// Interface for location-based cards that provide persistent effects to plant spots.
/// Location cards remain active across turns and automatically apply effects.
/// </summary>
public interface ILocationCard : ICard
{
    /// <summary>
    /// Duration of the effect in turns. For permanent effects, return a high value like 999.
    /// </summary>
    int EffectDuration { get; }
    
    /// <summary>
    /// Whether this is a permanent effect (never expires naturally).
    /// </summary>
    bool IsPermanent { get; }
    
    /// <summary>
    /// The type of location effect this card provides.
    /// </summary>
    LocationEffectType EffectType { get; }

    /// <summary>
    /// Called when the location card is first placed at a location.
    /// Use for immediate, one-time setup effects.
    /// </summary>
    /// <param name="plant">The plant at this location (may be null)</param>
    void ApplyLocationEffect(PlantController plant);
    
    /// <summary>
    /// Called when the location card is removed from a location.
    /// Use for cleanup and effect removal.
    /// </summary>
    /// <param name="plant">The plant at this location (may be null)</param>
    void RemoveLocationEffect(PlantController plant);
    
    /// <summary>
    /// Called each turn while the location card is active.
    /// This is where the main persistent effects should be implemented.
    /// </summary>
    /// <param name="plant">The plant at this location (may be null)</param>
    void ApplyTurnEffect(PlantController plant);
}
```

### SpotDataHolder

The `SpotDataHolder` class manages location-based effects for individual plant spots. It handles the lifecycle of location cards and their effects.

**Key Features:**
- **Plant Cache Optimization**: Caches plant references with dirty flag system
- **Race Condition Protection**: Prevents concurrent modification issues
- **Error Recovery**: Comprehensive try-catch blocks with safe fallbacks
- **Automatic Cleanup**: Handles expired card removal automatically

**Core Methods:**

```csharp
/// <summary>
/// Refreshes the cached plant reference. Only performs expensive searches
/// when the cache is dirty or invalid.
/// </summary>
public void RefreshAssociatedPlant()

/// <summary>
/// Invalidates the plant cache, forcing a refresh on next access.
/// Call this when plant hierarchy may have changed.
/// </summary>
public void InvalidatePlantCache()

/// <summary>
/// Called when a location card is placed at this spot.
/// Activates the effect for processing on subsequent turns.
/// </summary>
/// <param name="locationCard">The location card being placed</param>
public void OnLocationCardPlaced(ILocationCard locationCard)

/// <summary>
/// Called when a location card is removed from this spot.
/// Deactivates the effect immediately.
/// </summary>
public void OnLocationCardRemoved()

/// <summary>
/// Processes the location effect for the current turn.
/// Called by TurnController during turn processing.
/// </summary>
public void ProcessTurn()
```

### PlacedCardHolder Integration

The `PlacedCardHolder` class has been enhanced to support location cards with specific restrictions and behaviors:

**Location Card Restrictions:**
- Location cards cannot be manually clicked/removed (line 69: `if (placedCard is ILocationCard) return;`)
- Expire via `ProcessTurn()` + `FinalizeLocationCardTurn()` and clear through `ClearLocationCardByExpiry()`
- Early removal can occur through `SpotDataHolder.OnLocationCardRemoved()` (e.g., specific treatments)
- Placed location cards are removed from the player's hand immediately and only return to the discard pile after expiry
- Field spells that also implement `ILocationCard` follow the same lifecycle and expiry rules
- Integration with card holder type restrictions

**Key Integration Points:**

```csharp
/// <summary>
/// Clears an expired location card from this holder.
/// Called automatically by SpotDataHolder when effects expire.
/// </summary>
public void ClearLocationCardByExpiry()

/// <summary>
/// Notifies associated SpotDataHolder of location card placement.
/// Called during card placement workflow.
/// </summary>
private void NotifySpotDataHolder()

/// <summary>
/// Notifies associated SpotDataHolder of location card removal.
/// Called during card removal workflow.
/// </summary>
private void NotifySpotDataHolderRemoval()
```

## API Reference

### UreaBasic Example Implementation

For the production implementation, see `UreaBasic` in `Assets/_project/Scripts/Classes/CardClasses.cs`. It demonstrates a non-trivial location card with diminishing returns, duration tracking, and save/load-safe application counts.

### CardHolderType Enumeration

```csharp
/// <summary>
/// Defines what types of cards a PlacedCardHolder can accept.
/// </summary>
public enum CardHolderType
{
    Any,           // Accepts both action and location cards
    ActionOnly,    // Only accepts action cards (not ILocationCard)
    LocationOnly   // Only accepts location cards (ILocationCard)
}
```

## Implementation Guide

### Creating a New Location Card

Follow these steps to implement a custom location card:

#### Step 1: Create the Card Class

```csharp
public class MyLocationCard : ILocationCard
{
    public string Name => "My Location Card";
    public string Description => "Custom location effect description";
    
    // Implement required ICard properties
    private int _value = -2; // Cost to play
    public int? Value
    {
        get => _value;
        set => _value = value ?? 0;
    }
    
    public List<ISticker> Stickers { get; } = new();
    public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
    public Material Material => Resources.Load<Material>("Materials/Cards/MyCard");
    
    // Location Card specific properties
    public int EffectDuration => 5; // Lasts 5 turns
    public bool IsPermanent => false; // Temporary effect
    public LocationEffectType EffectType => null; // Optional categorization
    
    // Required methods
    public ICard Clone()
    {
        var clone = new MyLocationCard { Value = this.Value };
        foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
        return clone;
    }
    
    public void ApplyLocationEffect(PlantController plant)
    {
        // One-time effect when card is placed
        Debug.Log($"Applied {Name} to location");
    }
    
    public void RemoveLocationEffect(PlantController plant)
    {
        // Cleanup when card expires or is removed
        Debug.Log($"Removed {Name} from location");
    }
    
    public void ApplyTurnEffect(PlantController plant)
    {
        // Main effect applied each turn
        if (plant?.PlantCard == null) return;
        
        // Your custom effect logic here
        // Example: Reduce disease spread
        var infectLevel = plant.PlantCard.Infect;
        foreach (var kvp in infectLevel.All.ToList())
        {
            if (kvp.Value.infect > 0)
            {
                infectLevel.SetInfect(kvp.Key, kvp.Value.infect - 1);
            }
        }
    }
}
```

#### Step 2: Add to Game Systems

1. **Add to card creation system** (usually in `CardGameMaster` or deck initialization)
2. **Include in prototype decks** if it should appear in the shop
3. **Test with different card holder configurations**

#### Step 3: Configure Card Holders

Set up plant locations to accept location cards:

```csharp
// In scene setup or plant location initialization
var cardHolder = plantLocation.GetComponent<PlacedCardHolder>();
cardHolder.SetCardHolderType(CardHolderType.LocationOnly); // Only location cards
// or
cardHolder.SetCardHolderType(CardHolderType.Any); // Both types allowed
```

### Integration with Existing Systems

#### Turn Controller Integration

`TurnController.EndTurn()` iterates all `SpotDataHolder` instances and calls `ProcessTurn()` to apply location effects during turn advancement.

#### Save/Load System Integration

Location effects that rely on `PlantController.uLocationCards` persist across save/load (e.g., Urea application counts). Placed location cards themselves are not serialized yet.

## Performance Considerations

### Plant Cache Optimization

The system uses a sophisticated caching strategy to minimize expensive GameObject searches:

```csharp
private bool _plantCacheDirty = true;
private PlantController _associatedPlant;

public void RefreshAssociatedPlant()
{
    // Only refresh if cache is dirty or plant reference is invalid
    if (!_plantCacheDirty && _associatedPlant != null && _associatedPlant.PlantCard != null)
        return;

    // Optimized search strategy - try most likely locations first
    _associatedPlant = GetComponentInChildren<PlantController>();
    if (_associatedPlant == null)
        _associatedPlant = GetComponentInParent<PlantController>();
    if (_associatedPlant == null && transform.parent != null)
        _associatedPlant = transform.parent.GetComponentInChildren<PlantController>();

    _plantCacheDirty = false;
}
```

**Performance Features:**
- **Lazy Evaluation**: Plant references are only refreshed when necessary
- **Hierarchical Search**: Searches in most likely locations first
- **Cache Invalidation**: Manual cache invalidation for known changes
- **Null Safety**: Comprehensive null checking prevents exceptions

### Memory Management

- **Effect State**: Minimal memory footprint with efficient state tracking
- **Reference Management**: Proper cleanup prevents memory leaks
- **Garbage Collection**: Minimizes GC pressure through object reuse

### Computational Efficiency

- **Conditional Processing**: Effects only process when plants are present
- **Early Returns**: Quick validation prevents unnecessary work
- **Batch Operations**: Multiple effects can be processed efficiently

## Error Handling

The ILocationCard system implements comprehensive error handling with multiple layers of protection:

### Exception Safety

```csharp
public void OnLocationCardPlaced(ILocationCard locationCard)
{
    try
    {
        // Main placement logic
        if (cLocationCard != null && _effectActive) _effectActive = false;
        cLocationCard = locationCard;
        // ... rest of placement logic
    }
    catch (Exception e)
    {
        Debug.LogError($"Error placing location card {locationCard?.Name}: {e.Message}");
        // Reset to safe state on error
        _effectActive = false;
        cLocationCard = null;
    }
}
```

### Error Recovery Strategies

1. **Safe State Reset**: On any error, the system resets to a known safe state
2. **Graceful Degradation**: Effects are disabled rather than crashing the game
3. **Logging**: Detailed error messages help with debugging
4. **Null Safety**: Comprehensive null checking prevents null reference exceptions

### Race Condition Protection

The system protects against race conditions through:
- **State Validation**: Multiple checks before state changes
- **Atomic Operations**: Grouped operations that complete together
- **Defensive Programming**: Assumes other systems may modify state unexpectedly

### Validation Patterns

```csharp
// Comprehensive validation before processing
public void ProcessTurn()
{
    if (cLocationCard == null || !_effectActive) return;

    RefreshAssociatedPlant();

    // Validate plant exists and is valid
    if (_associatedPlant == null)
    {
        if (!cLocationCard.IsPermanent) _remainingDuration--;
        return;
    }

    if (_associatedPlant.PlantCard == null)
    {
        if (!cLocationCard.IsPermanent) _remainingDuration--;
        return;
    }

    // Safe to process effect
    // ...
}
```

## Testing Guidelines

### Unit Testing Location Cards

```csharp
[Test]
public void LocationCard_AppliesTurnEffect_IncreasesPlantValue()
{
    // Arrange
    var fertilizer = new FertilizerBasic();
    var plant = CreateTestPlant(initialValue: 5);
    
    // Act
    fertilizer.ApplyTurnEffect(plant);
    
    // Assert
    Assert.AreEqual(6, plant.PlantCard.Value);
}

[Test]
public void LocationCard_HandlesMissingPlant_DoesNotThrow()
{
    // Arrange
    var fertilizer = new FertilizerBasic();
    
    // Act & Assert
    Assert.DoesNotThrow(() => fertilizer.ApplyTurnEffect(null));
}
```

### Integration Testing

```csharp
[Test]
public void SpotDataHolder_ProcessTurn_AppliesEffectToPlant()
{
    // Arrange
    var spotDataHolder = CreateSpotDataHolder();
    var locationCard = new FertilizerBasic();
    var plant = CreateTestPlant(initialValue: 3);
    
    // Act
    spotDataHolder.OnLocationCardPlaced(locationCard);
    spotDataHolder.ProcessTurn();
    
    // Assert
    Assert.AreEqual(4, plant.PlantCard.Value);
}

[Test]
public void SpotDataHolder_ExpiryHandling_RemovesCardAutomatically()
{
    // Arrange
    var spotDataHolder = CreateSpotDataHolder();
    var shortDurationCard = new TestLocationCard(duration: 1);
    
    // Act
    spotDataHolder.OnLocationCardPlaced(shortDurationCard);
    spotDataHolder.ProcessTurn(); // Should expire after this turn
    
    // Assert
    Assert.IsFalse(spotDataHolder.HasActiveLocationEffect());
}
```

### Performance Testing

```csharp
[Test]
public void SpotDataHolder_PlantCaching_OptimizesPerformance()
{
    // Test that plant references are cached and not searched repeatedly
    var spotDataHolder = CreateSpotDataHolder();
    
    // Measure performance of multiple ProcessTurn calls
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    for (int i = 0; i < 100; i++)
    {
        spotDataHolder.ProcessTurn();
    }
    
    stopwatch.Stop();
    
    // Assert performance is within acceptable bounds
    Assert.Less(stopwatch.ElapsedMilliseconds, 10);
}
```

### Error Handling Testing

```csharp
[Test]
public void SpotDataHolder_ErrorInLocationCard_DoesNotCrashSystem()
{
    // Test that exceptions in location card effects are handled gracefully
    var spotDataHolder = CreateSpotDataHolder();
    var faultyCard = new FaultyLocationCard(); // Throws exceptions
    
    Assert.DoesNotThrow(() => spotDataHolder.OnLocationCardPlaced(faultyCard));
    Assert.DoesNotThrow(() => spotDataHolder.ProcessTurn());
    
    // System should recover to safe state
    Assert.IsFalse(spotDataHolder.HasActiveLocationEffect());
}
```

## Integration Examples

### Example 1: Disease Prevention Card

```csharp
public class DiseasePreventionCard : ILocationCard
{
    public string Name => "Disease Prevention";
    public string Description => "Prevents new diseases from affecting plants at this location";
    
    public int EffectDuration => 999; // Permanent until removed
    public bool IsPermanent => true;
    
    public void ApplyTurnEffect(PlantController plant)
    {
        if (plant?.PlantCard?.Infect == null) return;
        
        // Prevent new infections by capping existing levels
        var infectLevel = plant.PlantCard.Infect;
        foreach (var kvp in infectLevel.All.ToList())
        {
            if (kvp.Value.infect > 3) // Cap at level 3
            {
                infectLevel.SetInfect(kvp.Key, 3);
            }
        }
    }
}
```

### Example 2: Growth Accelerator Card

```csharp
public class GrowthAcceleratorCard : ILocationCard
{
    public string Name => "Growth Accelerator";
    public string Description => "Doubles plant value growth for 4 turns";
    
    public int EffectDuration => 4;
    public bool IsPermanent => false;
    
    private int _lastAppliedValue = 0;
    
    public void ApplyTurnEffect(PlantController plant)
    {
        if (plant?.PlantCard?.Value == null) return;
        
        int currentValue = plant.PlantCard.Value.Value;
        int growth = currentValue - _lastAppliedValue;
        
        if (growth > 0)
        {
            // Double the growth
            plant.PlantCard.Value += growth;
            plant.UpdatePriceFlag(plant.PlantCard.Value.Value);
        }
        
        _lastAppliedValue = plant.PlantCard.Value.Value;
    }
}
```

### Example 3: Multi-Effect Location Card

```csharp
public class ComprehensiveCareCard : ILocationCard
{
    public string Name => "Comprehensive Care";
    public string Description => "Provides multiple benefits: growth boost, disease resistance, and value protection";
    
    public int EffectDuration => 6;
    public bool IsPermanent => false;
    
    public void ApplyLocationEffect(PlantController plant)
    {
        Debug.Log("Comprehensive Care package activated!");
    }
    
    public void ApplyTurnEffect(PlantController plant)
    {
        if (plant?.PlantCard == null) return;
        
        // Growth boost
        if (plant.PlantCard.Value.HasValue)
        {
            plant.PlantCard.Value += 1;
        }
        
        // Disease resistance
        var infectLevel = plant.PlantCard.Infect;
        foreach (var kvp in infectLevel.All.ToList())
        {
            if (kvp.Value.infect > 0)
            {
                infectLevel.SetInfect(kvp.Key, kvp.Value.infect - 1);
            }
        }
        
        // Value protection (minimum value)
        if (plant.PlantCard.Value < 2)
        {
            plant.PlantCard.Value = 2;
        }
        
        plant.UpdatePriceFlag(plant.PlantCard.Value ?? 0);
    }
    
    public void RemoveLocationEffect(PlantController plant)
    {
        Debug.Log("Comprehensive Care package ended.");
    }
}
```

## Troubleshooting

### Common Issues

#### Location Cards Not Applying Effects

**Symptoms:** Location card is placed but no effects are visible

**Causes & Solutions:**
1. **SpotDataHolder not found**
   - Ensure plant locations have `SpotDataHolder` component
   - Check component hierarchy (parent/child relationships)

2. **Plant reference not cached**
   - Call `InvalidatePlantCache()` after plant placement
   - Verify plant has valid `PlantCard` component

3. **Effect not active**
   - Check if `_effectActive` flag is true in `SpotDataHolder`
   - Verify `OnLocationCardPlaced()` was called successfully

**Debug Code:**
```csharp
// Add to SpotDataHolder.ProcessTurn() for debugging
Debug.Log($"Processing turn: Card={cLocationCard?.Name}, Active={_effectActive}, Plant={_associatedPlant?.name}");
```

#### Location Cards Disappearing Unexpectedly

**Symptoms:** Location cards are removed before their duration expires

**Causes & Solutions:**
1. **Exception in effect processing**
   - Check Unity console for error messages
   - Add error handling to custom `ApplyTurnEffect()` implementations

2. **Manual card removal**
   - Location cards should only be removed via `ClearLocationCardByExpiry()`
   - Verify no other systems are calling card removal methods

3. **Save/load issues**
   - Check if location card state persists across save/load cycles
   - Verify serialization includes location card data

#### Performance Issues

**Symptoms:** Frame rate drops when location cards are active

**Causes & Solutions:**
1. **Expensive operations in `ApplyTurnEffect()`**
   - Profile custom location card implementations
   - Cache expensive calculations

2. **Plant cache misses**
   - Monitor cache hit rate with debug logging
   - Ensure proper cache invalidation timing

3. **Memory allocation in effects**
   - Avoid creating objects in `ApplyTurnEffect()`
   - Reuse collections and objects where possible

### Debug Utilities

#### Location Card Debug Component

```csharp
public class LocationCardDebugger : MonoBehaviour
{
    private SpotDataHolder _spotDataHolder;
    
    void Start()
    {
        _spotDataHolder = GetComponent<SpotDataHolder>();
    }
    
    void OnGUI()
    {
        if (_spotDataHolder == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Active Card: {_spotDataHolder.GetActiveLocationCard()?.Name ?? "None"}");
        GUILayout.Label($"Effect Active: {_spotDataHolder.HasActiveLocationEffect()}");
        GUILayout.Label($"Remaining Duration: {_spotDataHolder.GetRemainingDuration()}");
        
        if (GUILayout.Button("Invalidate Plant Cache"))
        {
            _spotDataHolder.InvalidatePlantCache();
        }
        
        if (GUILayout.Button("Process Turn"))
        {
            _spotDataHolder.ProcessTurn();
        }
        
        GUILayout.EndArea();
    }
}
```

#### Console Commands

Add these debug commands to your console system:

```csharp
[ConsoleCommand("location_card_status")]
public static void DebugLocationCardStatus()
{
    var holders = FindObjectsOfType<SpotDataHolder>();
    foreach (var holder in holders)
    {
        var card = holder.GetActiveLocationCard();
        Debug.Log($"Spot {holder.name}: {card?.Name ?? "No Card"} ({holder.GetRemainingDuration()} turns remaining)");
    }
}

[ConsoleCommand("force_location_turn")]
public static void ForceLocationTurn()
{
    var holders = FindObjectsOfType<SpotDataHolder>();
    foreach (var holder in holders)
    {
        holder.ProcessTurn();
    }
    Debug.Log($"Processed {holders.Length} location card effects");
}
```

---

*This documentation covers the production-ready ILocationCard system as implemented in Horticulture. For additional support or questions about implementation details, refer to the source files: `SpotDataHolder.cs`, `CardClasses.cs` (ILocationCard interface), and `PlacedCardHolder.cs`.*

# Self-Replicating Card Feature Documentation

**Last Updated:** 2025-12-11
**Feature Status:** Planned
**Related Systems:** Card Core, Deck Management, Plant Management

## Overview

This document describes the design and implementation approach for a special card type that automatically places copies of itself on card holders throughout the play area when the original is placed.

## Feature Description

When a self-replicating card is placed on any card holder, it should:

1. Place itself on the selected card holder (normal placement behavior)
2. Automatically create and place copies on **one card holder from each plant slot pair**

### Card Holder Pairing System

Each plant location in the game has **two card holders** that form a logical pair:

- Both holders are associated with the same plant slot
- They are retrieved via `location.GetComponentsInChildren<PlacedCardHolder>(true)`
- The pairing is implicit in the scene hierarchy structure

## Architecture Understanding

### Key Components

#### PlacedCardHolder (`Card Core/PlacedCardHolder.cs`)

- Manages individual card placements
- Key method: `TakeSelectedCard()` - handles placing a card from the player's hand
- Supports card type restrictions via `CardHolderType` enum (Any, ActionOnly, LocationOnly)
- Has validation: `CanAcceptCard(ICard card)` checks if a card can be placed

#### SpotDataHolder (`Card Core/SpotDataHolder.cs`)

- Maintains a list of associated `PlacedCardHolder` components
- Tracks location card effects
- Manages the relationship between card holders and plant slots

#### DeckManager (`Card Core/DeckManager.cs`)

- Contains list of all plant locations: `plantLocations` (`List<Transform>`)
- Each location has multiple `PlacedCardHolder` components as children
- Manages card placement workflow and deck state

### Card Holder Discovery

```csharp
// Example from DeckManager showing how to access all card holders
foreach (var location in plantLocations)
{
    var cardHolders = location.GetComponentsInChildren<PlacedCardHolder>(true);
    // cardHolders contains all holders for this plant slot (typically 2 per slot)
}
```

## Implementation Design

### Option 1: Card Interface Extension (Recommended)

Create a new interface that extends `ICard` to support replication behavior:

```csharp
namespace _project.Scripts.Classes
{
    /// <summary>
    /// Interface for cards that replicate themselves on placement
    /// </summary>
    public interface IReplicatingCard : ICard
    {
        /// <summary>
        /// Determines which card holder in each pair should receive the replica.
        /// Return 0 for first holder, 1 for second holder in each pair.
        /// </summary>
        int GetTargetHolderIndexInPair();

        /// <summary>
        /// Called after successful replication to all holders
        /// </summary>
        void OnReplicationComplete(List<PlacedCardHolder> targetHolders);
    }
}
```

### Option 2: Custom Card Property

Add a boolean or enum property to specific card implementations:

```csharp
public class SelfReplicatingActionCard : ICard
{
    public bool AutoReplicateOnPlacement => true;
    public ReplicationStrategy Strategy => ReplicationStrategy.FirstInPair;

    // Standard ICard implementation...
}

public enum ReplicationStrategy
{
    FirstInPair,   // Place on index 0 of each pair
    SecondInPair,  // Place on index 1 of each pair
    Random         // Randomly choose one from each pair
}
```

### Placement Logic Location

The replication logic should be added to `PlacedCardHolder.TakeSelectedCard()` after the initial card is successfully placed:

**File:** `Card Core/PlacedCardHolder.cs`
**Method:** `TakeSelectedCard()` (line 350)
**Insertion Point:** After line 472 (after `RefreshEfficacyDisplay()`)

```csharp
// In PlacedCardHolder.TakeSelectedCard(), after successful placement:

// Check if this card should replicate
if (placedCard is IReplicatingCard replicatingCard)
{
    ReplicateCardToOtherHolders(replicatingCard);
}
```

### Replication Implementation

Add a new method to `PlacedCardHolder`:

```csharp
/// <summary>
/// Creates and places copies of a replicating card on one holder from each plant slot pair.
/// Skips the holder where the original card was placed.
/// </summary>
private void ReplicateCardToOtherHolders(IReplicatingCard replicatingCard)
{
    var deckManager = CardGameMaster.Instance.deckManager;
    if (deckManager == null) return;

    var targetHolderIndex = replicatingCard.GetTargetHolderIndexInPair();
    var successfulPlacements = new List<PlacedCardHolder>();

    foreach (var location in deckManager.plantLocations)
    {
        var cardHolders = location.GetComponentsInChildren<PlacedCardHolder>(true);
        if (cardHolders.Length < 2) continue; // Skip if not a proper pair

        // Determine which holder in the pair to target
        var targetHolder = cardHolders[Mathf.Clamp(targetHolderIndex, 0, cardHolders.Length - 1)];

        // Skip if this is the holder where we just placed the original
        if (targetHolder == this) continue;

        // Skip if holder already has a card
        if (targetHolder.HoldingCard) continue;

        // Skip if holder can't accept this card type
        if (!targetHolder.CanAcceptCard(replicatingCard))
        {
            Debug.LogWarning($"Card holder at {location.name} cannot accept {replicatingCard.Name}");
            continue;
        }

        // Validate plant exists and is alive
        var plant = targetHolder.ResolvePlantForDisplay();
        if (plant == null || plant.PlantCard == null || plant.PlantCard.Value <= 0)
        {
            continue; // Skip dead/missing plants
        }

        // Create a copy of the card
        var cardCopy = replicatingCard.Clone(); // Assumes ICard.Clone() creates proper copy

        // Place the copy on the target holder
        PlaceReplicaCard(targetHolder, cardCopy);
        successfulPlacements.Add(targetHolder);
    }

    // Notify the card that replication is complete
    replicatingCard.OnReplicationComplete(successfulPlacements);
}

/// <summary>
/// Programmatically places a card on a specific holder without user selection.
/// Similar to TakeSelectedCard() but for automated placement.
/// </summary>
private void PlaceReplicaCard(PlacedCardHolder targetHolder, ICard card)
{
    // This would need access to the card prefab and placement logic
    // Implementation details depend on how card visuals are created
    // May need to be a public static method or extension method on PlacedCardHolder

    // Pseudo-code:
    // 1. Get card prefab from DeckManager
    // 2. Instantiate card visual
    // 3. Set up CardView with the card data
    // 4. Call placement logic similar to TakeSelectedCard()
    // 5. Update costs if applicable
    // 6. NO audio feedback (to avoid sound spam)
}
```

## Key Considerations

### 1. Card Costs

- Should replicated cards cost money?
  - **Option A:** Only the original card costs money (recommended for game balance)
  - **Option B:** Each replica also adds to treatment cost
  - **Implementation:** Check `placedCard?.Value != null` logic in `TakeSelectedCard()` around line 459

### 2. Turn Tracking

- Should replicas track their placement turn separately?
  - Current system tracks `PlacementTurn` to prevent redraw exploits
  - Replicas should probably share the same placement turn as the original
  - **Implementation:** Set `PlacementTurn` to match original card's placement turn

### 3. Card Holder Selection Strategy

- How to choose which holder in each pair?
  - **Fixed index:** Always first or always second in the pair
  - **Alternating:** First for odd locations, second for even
  - **Random:** Randomly choose for variety
  - **Smart selection:** Choose empty holder, or one matching certain criteria

### 4. Visual/Audio Feedback

- Replicated placements should probably NOT play audio to avoid sound spam
- Could use visual effects (particle system) to indicate replication
- Consider adding a brief animation or highlight effect

### 5. Card Limits

- Should replication respect hand limits or deck availability?
  - Cards are being created, not drawn from deck
  - Replicas could be "phantom" cards that don't count against deck
  - Or, could fail to replicate if deck doesn't have enough copies

### 6. Interaction with Existing Cards

- What happens if a holder already has a card?
  - **Skip:** Don't place replica on occupied holders (recommended)
  - **Replace:** Swap the existing card (potentially disruptive)
  - **Stack:** Allow multiple cards (requires major system changes)

### 7. Removal Behavior

- When original card is removed, should replicas also be removed?
  - **Independent:** Replicas persist independently (recommended)
  - **Linked:** Removing original removes all replicas
  - **Requires:** Additional tracking of replica relationships

## Testing Strategy

### Unit Tests (PlayModeTest Assembly)

Create `ReplicatingCardTest.cs`:

```csharp
[UnityTest]
public IEnumerator ReplicatingCard_PlacesOnOnePairMember()
{
    // Setup: Create 3 plant locations with 2 holders each
    // Place a replicating card on one holder
    // Assert: Exactly one holder from each of the other 2 locations has the card
    // Assert: Original placement location only has original card
}

[UnityTest]
public IEnumerator ReplicatingCard_SkipsOccupiedHolders()
{
    // Setup: Pre-fill some card holders
    // Place replicating card
    // Assert: Only empty holders received replicas
}

[UnityTest]
public IEnumerator ReplicatingCard_RespectsCardHolderType()
{
    // Setup: Mark some holders as LocationOnly
    // Place replicating ActionCard
    // Assert: LocationOnly holders were skipped
}

[UnityTest]
public IEnumerator ReplicatingCard_SkipsDeadPlants()
{
    // Setup: Create plant with Value <= 0
    // Place replicating card
    // Assert: Dead plant's holders don't receive replicas
}
```

### Integration Tests

1. **Full Round Test:** Place replicating card, advance turn, verify all replicas behave correctly
2. **Cost Calculation:** Verify total treatment cost is correct based on chosen cost strategy
3. **Save/Load:** Ensure replicas persist correctly through save/load cycle

### Manual Testing

1. Place replicating card in tutorial mode
2. Verify visual clarity of replica placements
3. Test interaction with shop, retained card holder
4. Test with various plant configurations (some dead, some missing)

## Example Implementation: Simple Spray Card

```csharp
using _project.Scripts.Classes;
using _project.Scripts.Core;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    /// Example replicating card that places copies on all plant slots
    /// </summary>
    public class MultiSprayCard : IReplicatingCard, ICard
    {
        public string Name => "Multi-Spray Treatment";
        public string Description => "Applies to all plants simultaneously";
        public int? Value => 15; // Cost for the original card only
        public Material Material { get; set; }
        public List<ISticker> Stickers { get; } = new();

        // IReplicatingCard implementation
        public int GetTargetHolderIndexInPair() => 0; // Always use first holder in pair

        public void OnReplicationComplete(List<PlacedCardHolder> targetHolders)
        {
            Debug.Log($"Multi-Spray replicated to {targetHolders.Count} locations");
        }

        // ICard implementation
        public ICard Clone()
        {
            return new MultiSprayCard
            {
                Material = this.Material,
                // Don't copy stickers to replicas
            };
        }
    }
}
```

## Migration Path

### Phase 1: Core Infrastructure

1. Create `IReplicatingCard` interface
2. Add replication detection in `PlacedCardHolder.TakeSelectedCard()`
3. Implement basic replication logic (skip occupied holders)
4. Add unit tests for core functionality

### Phase 2: Refinement

1. Implement cost strategy configuration
2. Add visual feedback for replications
3. Handle edge cases (dead plants, wrong holder types)
4. Comprehensive integration testing

### Phase 3: Content Creation

1. Design and implement first replicating card
2. Balance testing
3. Tutorial/documentation for players
4. Mod API documentation for custom replicating cards

## Related Files

- `Assets/_project/Scripts/Card Core/PlacedCardHolder.cs` - Primary placement logic
- `Assets/_project/Scripts/Card Core/DeckManager.cs` - Plant location management
- `Assets/_project/Scripts/Card Core/SpotDataHolder.cs` - Holder registration
- `Assets/_project/Scripts/Classes/ICard.cs` - Base card interface
- `Assets/_project/Scripts/PlayModeTest/` - Test implementations

## Open Questions

1. Should the feature be card-specific or configurable via a flag/interface?
2. How should this interact with the sticker system? Should replicas copy stickers?
3. What happens if a player removes the original card - do replicas persist?
4. Should there be a maximum number of replications (for balance)?
5. How does this interact with save/load - are replicas restored?
6. Should replication be animated/sequenced or instantaneous?

## Decision Log

### This section should be updated as implementation decisions are made

| Date | Decision | Rationale |
|------|----------|-----------|
| TBD | Interface vs Property | |
| TBD | Cost Strategy | |
| TBD | Replica Persistence | |
| TBD | Visual Feedback Approach | |

---

**Next Steps:**

1. Review this design with stakeholders
2. Answer open questions above
3. Begin Phase 1 implementation
4. Create prototype replicating card for testing

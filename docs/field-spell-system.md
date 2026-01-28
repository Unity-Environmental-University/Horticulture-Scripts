# Field Spell System Documentation

**Status**: Placement Implemented (hand removal + multi-plant placement)
**Last Updated**: 2026-01-28
**Related Systems**: Card Core, PlantHolder, Treatment System

---

## Overview

The Field Spell system introduces a new card type that can affect multiple plant locations simultaneously, enabling gameplay mechanics like area-of-effect treatments, persistent field effects, and visual preview systems for multi-target interactions.

**Key Features**:
- Multi-plant treatment application
- Ghost preview system for affected targets (planned)
- Persistent effects that remain until conditions are met (via ILocationCard)
- Integration with PlantHolder for efficient plant iteration

---

## IFieldSpell Interface

**Location**: `Assets/_project/Scripts/Classes/CardClasses.cs` (lines 74-79)

```csharp
public interface IFieldSpell : ICard
{
    bool AffectsAllPlants { get; set; }
    bool ShowsGhosts { get; set; }
    bool TillDeath { get; set; }
}
```

### Properties

#### AffectsAllPlants
**Type**: `bool`
**Purpose**: Determines whether the field spell applies its treatment to all plants simultaneously or requires individual targeting.

**Behavior**:
- `true`: Single card placement triggers treatment application on all plants
- `false`: Card must be placed on each plant location individually

**Current Status**: This flag is not used in runtime logic yet. Field spells always multi-place to all valid plant locations.

**Use Cases**:
- Beneficial insects that spread naturally (e.g., Lady Bugs)
- Environmental effects affecting entire greenhouse
- Area-of-effect treatments

#### ShowsGhosts
**Type**: `bool`
**Purpose**: Controls whether ghost preview cards are displayed on affected plant locations when the field spell is selected.

**Behavior**:
- `true`: Visual previews appear on all target plant locations when card is selected
- `false`: No preview system (silent application)

**Use Cases**:
- Player feedback for multi-target cards
- Visual indication of which plants will be affected
- Tutorial/educational purposes

**Current Status**: Ghost previews are not implemented for field spells yet.

#### TillDeath
**Type**: `bool`
**Purpose**: Determines whether the field spell persists on the board until specific conditions are met.

**Behavior**:
- `true`: Effect remains active until all target afflictions are cleared or plants die
- `false`: One-time application that doesn't persist

**Use Cases**:
- Ongoing beneficial insect populations
- Persistent environmental buffs
- Long-term integrated pest management strategies

**Current Status**: Persistence is controlled by `ILocationCard.EffectDuration`/`IsPermanent`. `TillDeath` is not used in runtime logic yet.

---

## Example Implementation: LadyBugsCard

**Location**: `Assets/_project/Scripts/Classes/CardClasses.cs` (lines 1058-1088)

```csharp
public class LadyBugsCard : IFieldSpell, ILocationCard
{
    public string Description => "Lady Bugs be lady bugs bro...";
    public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.LadyBugs();

    // Field Spell Properties
    public bool AffectsAllPlants { get; set; } = true;
    public bool ShowsGhosts { get; set; } = true;
    public bool TillDeath { get; set; } = true;

    // Location Card Properties (expiry handled via ILocationCard)
    public int EffectDuration => IsPermanent ? 999 : 4;
    public bool IsPermanent => false;
    public LocationEffectType EffectType => null;

    public string Name => "Lady Bugs";
    private int _value = -15;

    public int? Value
    {
        get => _value;
        set => _value = value ?? 0;
    }

    public ICard Clone() => new LadyBugsCard { Value = _value };
    public void ModifyValue(int amount) => _value += amount;
}
```

### LadyBugs Treatment

**Location**: `Assets/_project/Scripts/Classes/PlantAfflictions.cs` (LadyBugs treatment)

```csharp
public class LadyBugs : ITreatment
{
    public string Name => "LadyBugs";
    public string Description => "Lady Bugs affect all plants and are a beneficial insect";

    public int InfectCureValue => 2;
    public int EggCureValue => 1;
    public int Efficacy => DefaultEfficacy;
    public int BeeValue => 5;           // Beneficial pollinator

    // ... other treatment properties
}
```

**Design Intent**:
- Lady bugs are beneficial insects that control pest populations
- Affect all plants simultaneously (natural spread behavior)
- High cure values eliminate pest problems completely
- Persistent effect uses `ILocationCard.EffectDuration` (expires after 4 turns by default)
- Shows ghost previews for educational feedback

**Early Removal Behavior**:
- Applying Permethrin or Imidacloprid removes the LadyBugs location card and treatment early.

---

## Integration Points

### PlantHolder System

The Field Spell system leverages the PlantHolder architecture for efficient iteration over all plant locations and their associated card holders.

**Location**: `Assets/_project/Scripts/Card Core/PlacedCardHolder.cs` (PlaceFieldSpell)

```csharp
if (selectedCard is IFieldSpell fieldSpell)
{
    // Find first empty, compatible holder per plant location
    foreach (var plantHolder in _deckManager.plantLocations)
    {
        var emptyHolder = plantHolder.CardHolders
            ?.FirstOrDefault(holder => holder is not null && !holder.HoldingCard && holder.CanAcceptCard(fieldSpell));
        if (emptyHolder != null)
            targetHolders.Add(emptyHolder);
    }

    // Clone the selected card into each target holder
    foreach (var targetHolder in targetHolders)
    {
        // Instantiate clone, configure transforms, and assign placedCard
    }
}
```

**Available Data Structures**:
- `_deckManager.plantLocations`: `List<PlantHolder>` - all plant locations in scene
- `plantHolder.CardHolders`: `IReadOnlyList<PlacedCardHolder>` - pre-cached card holders for each plant
- `plantHolder.Transform`: Direct access to plant location Transform

**Performance Benefits**:
- No expensive `GetComponentsInChildren` calls during field spell application
- Pre-cached references eliminate per-frame lookups
- Efficient iteration pattern for multi-plant operations

### Treatment System

Field spells integrate with the existing treatment system (`PlantAfflictions.ITreatment`) to apply their effects:

```csharp
// Field spell provides treatment
IFieldSpell fieldSpell = selectedCard as IFieldSpell;
ITreatment treatment = fieldSpell.Treatment;

// Apply treatment to affected plants
// (implementation details depend on AffectsAllPlants flag)
```

---

## Implementation Status

### ✅ Complete
- [x] `IFieldSpell` interface definition
- [x] Property specifications (AffectsAllPlants, ShowsGhosts, TillDeath)
- [x] Example card implementation (LadyBugs)
- [x] Treatment integration (PlantAfflictions.LadyBugs)
- [x] PlantHolder system support for efficient iteration
- [x] Placement logic in `PlacedCardHolder.PlaceFieldSpell()`

### ⚠️ In Progress
- [ ] Ghost preview system integration for field spells
- [ ] Multi-plant treatment application logic beyond placement

### 🔮 Future Development
- [ ] Persistent effect tracking in TurnController
- [ ] `TillDeath` condition handling (when to remove field spell)
- [ ] Visual feedback for active field spells
- [ ] Test coverage for field spell placement
- [ ] Test coverage for multi-plant treatment application
- [ ] Integration tests for ghost preview visibility
- [ ] Save/load support for active field spells

---

## Usage Guide (Future)

### Creating a New Field Spell Card

```csharp
public class MyFieldSpell : IFieldSpell
{
    public string Name => "My Field Spell";
    public string Description => "Affects plants in interesting ways";

    // Define treatment behavior
    public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.MyTreatment();

    // Configure field spell behavior
    public bool AffectsAllPlants { get; set; } = true;   // Multi-target
    public bool ShowsGhosts { get; set; } = true;        // Show previews
    public bool TillDeath { get; set; } = false;         // One-time application

    private int _value = -10;
    public int? Value
    {
        get => _value;
        set => _value = value ?? 0;
    }

    // Required ICard methods
    public ICard Clone() => new MyFieldSpell { Value = _value };
    public void Selected() { }
    public void ModifyValue(int amount) => _value += amount;
}
```

### Implementing Field Spell Logic (Planned)

```csharp
// Pseudocode for future implementation
if (selectedCard is IFieldSpell fieldSpell)
{
    if (fieldSpell.AffectsAllPlants)
    {
        // Apply to all plants
        foreach (var plantHolder in _deckManager.plantLocations)
        {
            ApplyTreatmentToPlant(plantHolder, fieldSpell.Treatment);
        }
    }

    if (fieldSpell.ShowsGhosts)
    {
        // Display ghost previews on affected locations
        ShowGhostPreviews(GetAffectedLocations(fieldSpell));
    }

    if (fieldSpell.TillDeath)
    {
        // Track persistent effect
        RegisterPersistentEffect(fieldSpell);
    }
}
```

---

## Design Rationale

### Why Field Spells?

Field spells address gameplay needs that traditional single-target cards cannot:

1. **Educational Value**: Teach concepts like beneficial insect introduction
2. **Strategic Depth**: Trade card efficiency for broader coverage
3. **Integrated Pest Management**: Mirror real-world IPM strategies where beneficial organisms spread naturally
4. **Gameplay Variety**: Different decision-making compared to single-target treatments

### Property Design Decisions

**AffectsAllPlants**:
- Balances power vs. targeting control
- Enables both AoE and multi-cast patterns
- Supports different card balance strategies

**ShowsGhosts**:
- Educational feedback for new players
- Prevents confusion about multi-target effects
- Optional for experienced players who understand mechanics

**TillDeath**:
- Mirrors persistent biological controls in real IPM
- Creates strategic timing decisions
- Allows for resource-efficient long-term solutions

---

## Technical Considerations

### Performance
- PlantHolder caching eliminates expensive component lookups
- Field spell checks are conditional (only when card is selected)
- Ghost preview generation should use object pooling

### Memory
- Persistent field spells need tracking in game state
- Save/load system requires field spell serialization
- Ghost previews should be destroyed when no longer needed
- Field spell placements reuse a single card instance across holders; implementations should avoid per-location mutable state

### Platform Compatibility
- Ghost previews should respect hover system platform limitations
- Touch devices may need alternative visual feedback
- Performance considerations for mobile platforms

---

## Integration with Existing Systems

### Card Core System
- Field spells follow standard `ICard` interface
- Deck management treats them like any other card
- Selection and deselection work with existing UI

### DeckManager
- `plantLocations` provides iteration structure
- Field spells are removed from the hand on placement
- Cards return to discard only when they also implement `ILocationCard`

### TurnController
- May need to track persistent field spells
- End-of-turn processing for `TillDeath` effects
- Cleanup logic when conditions are met

### Game State System
- Serialize active field spells for save/load
- Restore persistent effects on game load
- Track field spell conditions across saves

---

## Testing Strategy (Planned)

### Unit Tests
```csharp
[Test]
public void FieldSpell_AffectsAllPlants_ShouldApplyToAllLocations()
{
    // Arrange
    var fieldSpell = new LadyBugs();
    var plantLocations = GetAllPlantLocations();

    // Act
    ApplyFieldSpell(fieldSpell);

    // Assert
    foreach (var location in plantLocations)
    {
        Assert.IsTrue(HasTreatment(location, fieldSpell.Treatment));
    }
}

[Test]
public void FieldSpell_ShowsGhosts_ShouldDisplayPreviews()
{
    // Arrange
    var fieldSpell = new LadyBugs();
    var deckManager = GetDeckManager();

    // Act
    deckManager.SelectCard(fieldSpell);

    // Assert
    var ghostPreviews = GetActiveGhostPreviews();
    Assert.AreEqual(plantLocations.Count, ghostPreviews.Count);
}

[Test]
public void FieldSpell_TillDeath_ShouldPersistUntilConditionsMet()
{
    // Arrange
    var fieldSpell = new LadyBugs();
    var turnController = GetTurnController();

    // Act
    ApplyFieldSpell(fieldSpell);
    turnController.ProcessTurn();

    // Assert
    Assert.IsTrue(IsFieldSpellActive(fieldSpell));

    // Clear all pests
    ClearAllPests();
    turnController.ProcessTurn();

    // Assert
    Assert.IsFalse(IsFieldSpellActive(fieldSpell));
}
```

---

## Related Documentation

- [Card Core System](card-core-system.md) - Main card mechanics
- [PlantHolder System](plant-holder-migration-solution.md) - Plant location management
- [Treatment System](../Classes/PlantAfflictions.cs) - Treatment definitions
- [Architecture](ARCHITECTURE.md) - Overall system design

---

## FAQ

**Q: Can a field spell affect only some plants?**
A: Currently, `AffectsAllPlants` is boolean. Future development could add targeting filters or range-based effects.

**Q: How do field spells interact with isolation cards?**
A: Integration with isolation system needs definition. Isolated plants may or may not be affected depending on game design.

**Q: What happens if I place multiple field spells?**
A: Behavior for multiple active field spells needs specification. Should they stack, override, or conflict?

**Q: Do field spells consume a card slot?**
A: Implementation detail pending. They may occupy a special field slot or consume regular card placements.

---

**Contributors**: Documentation based on code exploration
**Implementation Timeline**: Interface defined in v1.2.0, full implementation pending
**Contact**: See project README for contributor information

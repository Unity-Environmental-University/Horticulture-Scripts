# Plant Location Card Slots - Technical Design Document

## Executive Summary

This document outlines the implementation plan for persistent plant location card slots in the Horticulture game. Each plant location will have a dedicated card slot that accepts special "Location Cards" providing ongoing effects to whatever plant occupies that location. These effects persist between rounds and are saved with the game state.

## Feature Requirements

### Core Functionality
- Each plant location has a dedicated card slot for Location Cards
- Location Cards are a special card type distinct from regular Action Cards
- Cards placed in location slots persist between rounds (not cleared by turn controller)
- Location Card effects apply to whatever plant is placed at that location
- Effects persist over time and influence plant behavior/stats
- Full integration with existing save/load system

### User Experience
- Visual card slots at each plant location
- Drag-and-drop interaction to place Location Cards
- Clear visual indication of which location cards are active
- Location Cards can be removed/replaced by players
- Tooltips showing active location effects

## Current Architecture Analysis

### Existing Systems
- **Card System**: Interface `ICard` with concrete implementations for plants, diseases, and treatments
- **Plant System**: `PlantController` manages individual plants, `PlantManager` oversees all plants
- **Location System**: `plantLocations` list in `DeckManager` defines plant placement positions
- **Save System**: `GameStateManager` handles persistence with `PlantData` structures
- **Card Management**: `DeckManager` handles card deck operations and visual presentation

### Key Integration Points
- Plant locations are managed in `DeckManager.plantLocations`
- Plant state is saved/loaded via `GameStateData.PlantData`
- Card serialization system exists in `GameStateManager`
- Visual card representation handled by `CardView` components

## Technical Implementation Plan

### Phase 1: Core Location Card System

#### 1.1 Location Card Interface & Classes
```csharp
// New interface for location-specific cards
public interface ILocationCard : ICard
{
    LocationEffect Effect { get; }
    bool IsStackable { get; }
    int MaxStackSize { get; }
}

// Base location effect interface
public interface ILocationEffect
{
    string Name { get; }
    string Description { get; }
    void ApplyToPlant(PlantController plant);
    void RemoveFromPlant(PlantController plant);
    void UpdateEffect(PlantController plant); // Called each turn
}
```

#### 1.2 Sample Location Cards
```csharp
public class FertilizerCard : ILocationCard
{
    public string Name => "Organic Fertilizer";
    public LocationEffect Effect => new FertilizerEffect();
    public bool IsStackable => false;
    public int MaxStackSize => 1;
}

public class FertilizerEffect : ILocationEffect
{
    public void ApplyToPlant(PlantController plant)
    {
        // Increase plant value by 2
        plant.PlantCard.ModifyValue(2);
    }
    
    public void UpdateEffect(PlantController plant)
    {
        // Additional per-turn bonuses could go here
    }
}
```

#### 1.3 Location Slot Component
```csharp
public class LocationCardSlot : MonoBehaviour
{
    public ILocationCard HeldCard { get; private set; }
    public Transform cardVisualParent;
    public bool CanAcceptCard(ILocationCard card);
    public void PlaceCard(ILocationCard card);
    public ILocationCard RemoveCard();
    public void ApplyEffectsToPlant(PlantController plant);
}
```

### Phase 2: Plant Location Integration

#### 2.1 Enhanced Plant Location System
- Add `LocationCardSlot` component to each plant location
- Modify `DeckManager.plantLocations` initialization to set up card slots
- Update plant placement logic to apply location card effects

#### 2.2 Plant Controller Integration
```csharp
// Add to PlantController
public List<ILocationEffect> LocationEffects { get; } = new();

public void AddLocationEffect(ILocationEffect effect)
{
    LocationEffects.Add(effect);
    effect.ApplyToPlant(this);
}

public void RemoveLocationEffect(ILocationEffect effect)
{
    if (LocationEffects.Remove(effect))
        effect.RemoveFromPlant(this);
}
```

### Phase 3: Save/Load System Integration

#### 3.1 Data Structures
```csharp
// Add to GameStateData.cs
[Serializable]
public class LocationCardData
{
    public int locationIndex;
    public CardData locationCard;
}

// Add to GameStateData class
public List<LocationCardData> locationCards;
```

#### 3.2 Serialization Methods
```csharp
// Add to GameStateManager
private static List<LocationCardData> SerializeLocationCards(DeckManager dm)
{
    var locationCards = new List<LocationCardData>();
    for (int i = 0; i < dm.plantLocations.Count; i++)
    {
        var slot = dm.plantLocations[i].GetComponent<LocationCardSlot>();
        if (slot?.HeldCard != null)
        {
            locationCards.Add(new LocationCardData
            {
                locationIndex = i,
                locationCard = SerializeCard(slot.HeldCard)
            });
        }
    }
    return locationCards;
}
```

### Phase 4: User Interface & Interaction

#### 4.1 Visual Card Slots
- Create UI prefab for location card slots
- Position slots visually near plant locations
- Implement hover/highlight states

#### 4.2 Drag & Drop System
- Extend existing card drag system to support Location Cards
- Add validation for location card placement
- Visual feedback for valid/invalid drop targets

#### 4.3 Location Card Deck Management
- Create separate deck for Location Cards
- Add Location Cards to shop system
- Implement drawing/acquisition mechanics

### Phase 5: Game Balance Integration

#### 5.1 Turn Processing
- Update `TurnController` to process location effects
- Ensure location effects are applied after plant placement
- Handle location effect updates during day processing

#### 5.2 Scoring Integration
- Update `ScoreManager` to account for location card effects
- Modify profit calculations to include location bonuses

## Implementation Timeline

### Sprint 1 (Week 1-2)
**Goal: Foundation Systems**
- [ ] Implement ILocationCard interface and base classes
- [ ] Create 3-4 basic location cards (Fertilizer, Pesticide Depot, Growth Enhancer)
- [ ] Implement LocationCardSlot component
- [ ] Add location slots to plant locations
- [ ] Basic visual representation

### Sprint 2 (Week 3-4)  
**Goal: Core Integration**
- [ ] Integrate location effects with PlantController
- [ ] Implement location card placement logic
- [ ] Update plant placement to apply location effects
- [ ] Basic save/load support for location cards
- [ ] Comprehensive unit tests

### Sprint 3 (Week 5-6)
**Goal: User Experience**
- [ ] Implement drag & drop for location cards
- [ ] Create location card acquisition system (shop integration)
- [ ] Polish visual design and animations
- [ ] Add tooltips and UI feedback
- [ ] Performance optimization

### Sprint 4 (Week 7)
**Goal: Balance & Polish**
- [ ] Game balance testing and tuning
- [ ] Edge case handling and bug fixes
- [ ] Documentation updates
- [ ] Performance profiling and optimization
- [ ] Final QA testing

## Risk Assessment

### High Priority Risks
1. **Save/Load Compatibility**: New data structures must not break existing saves
   - *Mitigation*: Implement backwards-compatible serialization with version checking

2. **Performance Impact**: Additional processing for location effects each turn
   - *Mitigation*: Optimize effect processing, lazy evaluation where possible

3. **UI Complexity**: Drag & drop system integration complexity
   - *Mitigation*: Reuse existing card interaction patterns, prototype early

### Medium Priority Risks
1. **Game Balance**: Location cards may create overpowered strategies
   - *Mitigation*: Extensive playtesting, configurable effect values

2. **Visual Clutter**: Additional UI elements may overwhelm the interface  
   - *Mitigation*: Minimalist design, collapsible/contextual UI elements

## Testing Strategy

### Unit Tests
- Location card effect application/removal
- Save/load serialization for location cards
- Plant controller location effect integration
- Card slot validation logic

### Integration Tests
- End-to-end location card placement workflow
- Turn processing with location effects active
- Save/load round-trip testing
- Performance benchmarks

### Manual QA Focus Areas
- Drag & drop interaction responsiveness
- Visual feedback and animations
- Edge cases (removing cards, plant death, etc.)
- Game balance verification

## Dependencies

### External Dependencies
- DOTween (existing) - for card animations
- Unity Input System (existing) - for drag interactions

### Internal Dependencies
- Existing card system refactoring may be needed
- Plant placement workflow modifications required
- Save system version upgrade necessary

## Backwards Compatibility

- Existing save games must continue to work
- Location card data will be optional in save format
- Graceful degradation if location card data is missing
- Version migration strategy for future updates

## Performance Considerations

- Location effects processed only when plants are present
- Effect caching to avoid redundant calculations  
- Minimal garbage collection from effect updates
- Efficient serialization format for save/load

## Future Enhancements

### Potential Phase 2 Features
- Stackable location cards for cumulative effects
- Location card synergies and combinations
- Temporary vs. permanent location effects
- Location-specific card restrictions
- Advanced location card crafting system

## Conclusion

This implementation plan provides a robust foundation for the plant location card slot system while maintaining compatibility with existing game systems. The phased approach allows for iterative development and testing, ensuring each component is solid before building upon it.

The modular design will support future expansion while keeping the core implementation manageable and maintainable.
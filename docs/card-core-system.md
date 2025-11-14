# Card Core System Documentation

## Overview

The Card Core system is the central game mechanic powering the Horticulture Unity game. It manages card-based gameplay where players diagnose plant problems and implement sustainable Integrated Pest Management (IPM) solutions through strategic card play. The system orchestrates the interaction between plants, afflictions, treatments, and game progression.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Core Components](#core-components)
3. [API Reference](#api-reference)
4. [Game Mechanics](#game-mechanics)
5. [Usage Examples](#usage-examples)
6. [Integration Patterns](#integration-patterns)
7. [Configuration Guide](#configuration-guide)
8. [Testing](#testing)

## System Architecture

The Card Core system follows a centralized singleton architecture with modular components:

```
CardGameMaster (Singleton)
├── DeckManager (Card Operations)
├── TurnController (Game Flow)
├── ScoreManager (Scoring & Economy)
├── ShopManager (Card Purchasing)
└── Supporting Components
    ├── CardView (Visual Representation)
    ├── PlacedCardHolder (Card Placement)
    ├── RetainedCardHolder (Card Retention)
    └── Click3D (3D Interaction)
```

### Key Design Patterns

- **Singleton Pattern**: `CardGameMaster` provides global access to the card game state
- **Component Architecture**: Unity's component-based system with modular scripts
- **Event-Driven Communication**: Components communicate through Unity events and direct references
- **Data-Driven Design**: Game configurations stored in ScriptableObjects and serialized fields

## Core Components

### CardGameMaster

The central controller that orchestrates all card game functionality.

**Location**: `Assets/_project/Scripts/Card Core/CardGameMaster.cs`

**Responsibilities**:
- Singleton management for global access
- Integration between deck, score, turn, and shop systems
- Audio and visual system coordination
- Save/load game state operations

**Key Properties**:
```csharp
public static CardGameMaster Instance { get; private set; }
public DeckManager deckManager;
public ScoreManager scoreManager;
public TurnController turnController;
public ShopManager shopManager;
public List<PlacedCardHolder> cardHolders;
```

### DeckManager

Manages all card operations including deck management, card drawing, and plant placement.

**Location**: `Assets/_project/Scripts/Card Core/DeckManager.cs`

**Responsibilities**:
- Action deck management (draw, discard, shuffle)
- Plant placement and management
- Affliction application and management
- Sticker system integration
- Tutorial mode support

**Key Methods**:
```csharp
public void DrawActionHand()
public IEnumerator PlacePlants()
public void DrawAfflictions()
public void ApplyAfflictionDeck()
public void ShuffleDeck(List<ICard> deck)
```

### TurnController

Controls game flow, turn progression, and game state transitions.

**Location**: `Assets/_project/Scripts/Card Core/TurnController.cs`

**Responsibilities**:
- Turn and round management
- Game progression logic
- Tutorial sequence control
- Win/lose conditions
- Plant effect queuing system

**Key Methods**:
```csharp
public void EndTurn()
public IEnumerator BeginTurnSequence()
public static void QueuePlantEffect(PlantController plant, ParticleSystem particle, AudioClip sound, float delay)
```

### ScoreManager

Handles scoring, economy, and cost calculations.

**Location**: `Assets/_project/Scripts/Card Core/ScoreManager.cs`

**Responsibilities**:
- Money management and display
- Treatment cost calculations
- Potential profit calculations
- Score calculation at round end

**Key Methods**:
```csharp
public int CalculateScore()
public void CalculateTreatmentCost()
public void CalculatePotentialProfit()
public static void SubtractMoneys(int amount)
```

### ShopManager

Manages the in-game shop system for purchasing cards.

**Location**: `Assets/_project/Scripts/Card Core/ShopManager.cs`

**Responsibilities**:
- Shop inventory generation
- Card purchasing logic
- Shop UI management

**Key Methods**:
```csharp
public void OpenShop()
public void CloseShop()
private void GenerateShopInventory()
```

## API Reference

### CardGameMaster API

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Instance` | `CardGameMaster` | Singleton instance (static, read-only) |
| `deckManager` | `DeckManager` | Reference to deck management component |
| `scoreManager` | `ScoreManager` | Reference to scoring component |
| `turnController` | `TurnController` | Reference to turn control component |
| `cardHolders` | `List<PlacedCardHolder>` | All card placement locations in scene |
| `isInspecting` | `bool` | Whether player is currently inspecting objects |

#### Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `Save()` | None | `void` | Saves current game state |
| `Load()` | None | `void` | Loads saved game state |
| `SelfDestruct()` | None | `void` | Destroys the CardGameMaster GameObject |

### DeckManager API

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `SelectedACard` | `ICard` | Currently selected action card |
| `selectedACardClick3D` | `Click3D` | Click3D component of selected card |
| `cardsDrawnPerTurn` | `int` | Number of cards drawn each turn (default: 4) |
| `redrawCost` | `int` | Cost to redraw hand (default: 3) |

#### Deck Management Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `DrawActionHand()` | None | `void` | Draws new action hand, discarding current |
| `DiscardActionCard(ICard, bool)` | `card`: Card to discard<br>`addToDiscard`: Add to discard pile | `void` | Discards specified action card |
| `RedrawCards()` | None | `void` | Redraws current hand for cost |
| `ShuffleDeck(List<ICard>)` | `deck`: Deck to shuffle | `void` | Shuffles specified deck using Fisher-Yates |

#### Plant Management Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `PlacePlants()` | None | `IEnumerator` | Coroutine to place plants on board |
| `ClearAllPlants()` | None | `void` | Removes all plants from board |
| `ClearPlant(PlantController, bool skipDeathSequence = false)` | `plant`: Plant to remove<br>`skipDeathSequence`: true to skip calling `KillPlant` (when already running) | `IEnumerator` | Removes specific plant with optional death animation |

#### Affliction Management Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `DrawAfflictions()` | None | `void` | Draws and applies afflictions to plants |
| `ApplyAfflictionDeck()` | None | `void` | Applies drawn afflictions to random plants |

### TurnController API

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `currentTurn` | `int` | Current turn number within round |
| `currentRound` | `int` | Current round number |
| `turnCount` | `int` | Maximum turns per round (default: 4) |
| `moneyGoal` | `int` | Money required to advance level |
| `canClickEnd` | `bool` | Whether end turn button is clickable |

#### Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `EndTurn()` | None | `void` | Processes end of turn logic |
| `BeginTurnSequence()` | None | `IEnumerator` | Initializes new turn/round |
| `QueuePlantEffect(PlantController, ParticleSystem, AudioClip, float)` | `plant`: Target plant<br>`particle`: Effect particle<br>`sound`: Effect audio<br>`delay`: Effect delay | `void` | Queues visual/audio plant effect |

### ScoreManager API

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `treatmentCost` | `int` | Current total treatment cost |

#### Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `CalculateScore()` | None | `int` | Calculates and applies end-of-round score |
| `CalculateTreatmentCost()` | None | `void` | Updates current treatment cost display |
| `CalculatePotentialProfit()` | None | `void` | Updates potential profit display |
| `GetMoneys()` | None | `int` | Returns current money amount (static) |
| `SubtractMoneys(int)` | `amount`: Money to subtract | `void` | Subtracts money and updates UI (static) |

### Card System API

#### ICard Interface

```csharp
public interface ICard
{
    string Name { get; }
    string Description { get; }
    int? Value { get; set; }
    PlantAfflictions.IAffliction Affliction { get; }
    PlantAfflictions.ITreatment Treatment { get; }
    GameObject Prefab { get; }
    Material Material { get; }
    List<ISticker> Stickers { get; }
    ICard Clone();
    void Selected();
    void ApplySticker(ISticker sticker);
    void ModifyValue(int delta);
}
```

## Game Mechanics

### Turn Structure

Each game consists of multiple rounds, with each round having up to 4 turns:

1. **Round Start**: Plants are placed on the board
2. **Affliction Phase**: Random afflictions are applied to plants
3. **Player Turn**: Player draws action cards and places treatments
4. **Turn End**: Treatments are applied, afflictions may spread
5. **Round End**: Score is calculated based on plant health vs. treatment costs

### Card Types

#### Plant Cards
- **Purpose**: Represent plants that can be afflicted and treated
- **Value**: Positive monetary value when healthy
- **Examples**: Coleus ($5), Chrysanthemum ($8), Pepper ($4), Cucumber ($3)

#### Affliction Cards
- **Purpose**: Represent plant diseases/pests
- **Value**: Negative impact on plant value
- **Examples**: Aphids (-$2), Mealy Bugs (-$4), Thrips (-$5), Mildew (-$4)

#### Treatment Cards
- **Purpose**: Remove or prevent afflictions
- **Cost**: Negative monetary cost to use
- **Examples**: Horticultural Oil (-$1), Insecticide (-$3), Fungicide (-$2), Panacea (-$5)

### Scoring System

**Score Calculation**:
```
Final Score = Plant Values + Affliction Penalties + Treatment Costs + Current Money
```

- **Plant Values**: Sum of healthy plant values
- **Affliction Penalties**: Sum of affliction damage on plants
- **Treatment Costs**: Sum of treatment cards used
- **Current Money**: Player's remaining money

### Tutorial System

The game includes a structured 5-turn tutorial:

- **Turn 0**: Single Coleus with Aphids - Learn basic treatment
- **Turn 1**: Two plants - Learn multiple plant management  
- **Turn 2**: Three plants with two afflictions - Learn prioritization
- **Turn 3**: Four plants with three afflictions - Full complexity
- **Turn 4**: All affliction types introduced

### Special Mechanics

#### Affliction Spreading
- **Adjacent Spread**: Most afflictions spread to neighboring plants (50% chance)
- **Global Spread**: Thrips can spread to any plant without the affliction
- **Prevention**: Panacea treatment prevents spreading to/from treated plants

#### Card Retention
Players can retain one card between rounds using the `RetainedCardHolder` system.

#### Sticker System
Stickers can be applied to cards to modify their properties (costs, effects).

#### Plant Death and Card Placement

When a plant's value drops to 0 or below:
- **Immediate Response**: Cardholders are disabled immediately in `PlantController.Update()` to prevent new card placements
- **Treatment Card Cleanup**: Treatment cards already placed on the plant are destroyed (not returned to deck) when `DeckManager.ClearPlant()` is called after the death animation
- **Location Card Persistence**: Location cards (ILocationCard) remain visible after plant death as they're tied to the location, not the plant
- **Validation**: `PlacedCardHolder.TakeSelectedCard()` validates plant health before accepting cards
- **Death Sequence**: Plant death triggers animation, cardholder disable, and treatment card cleanup in that order

**Design Rationale**: Treatment cards placed on dying plants represent wasted resources. This mechanic teaches players to:
- Monitor plant health proactively
- Avoid investing in plants beyond recovery
- Understand the economic cost of late interventions

Location cards persist because they affect the growing location itself, independent of which plant occupies it.

## Usage Examples

### Basic Card Game Setup

```csharp
// Get the card game master instance
var cardGameMaster = CardGameMaster.Instance;

// Start a new game
cardGameMaster.turnController.ResetGame();

// Access deck manager for card operations  
var deckManager = cardGameMaster.deckManager;

// Draw initial action hand
deckManager.DrawActionHand();
```

### Handling Card Selection and Placement

```csharp
// In a UI button click handler
public void OnCardHolderClicked()
{
    var cardHolder = GetComponent<PlacedCardHolder>();
    
    // Place selected card if one is selected
    if (deckManager.SelectedACard != null && !cardHolder.HoldingCard)
    {
        cardHolder.TakeSelectedCard();
    }
    // Pick up card if holder has one
    else if (cardHolder.HoldingCard)
    {
        cardHolder.OnPlacedCardClicked();
    }
}
```

### Custom Card Implementation

```csharp
public class CustomTreatmentCard : ICard
{
    public string Name => "Custom Treatment";
    public string Description => "A custom treatment for specific afflictions";
    
    private int _value = -2;
    public int? Value 
    { 
        get => _value; 
        set => _value = value ?? 0; 
    }
    
    public PlantAfflictions.ITreatment Treatment => new CustomTreatment();
    public Material Material => Resources.Load<Material>("Materials/Cards/CustomCard");
    public List<ISticker> Stickers => new();
    
    public ICard Clone() => new CustomTreatmentCard();
    public void ModifyValue(int delta) => _value += delta;
}
```

### Queuing Plant Effects

```csharp
// Queue a visual effect when applying treatment
public void ApplyTreatmentWithEffects(PlantController plant, ParticleSystem healEffect, AudioClip healSound)
{
    // Apply the treatment logic
    plant.RemoveAffliction(someAffliction);
    
    // Queue visual/audio feedback
    TurnController.QueuePlantEffect(plant, healEffect, healSound, 0.5f);
}
```

### Shop Integration

```csharp
// Creating a custom shop item
public class CustomShopItem : IShopItem
{
    public ICard Card { get; private set; }
    public string DisplayName => $"{Card.Name} (Upgraded)";
    public int Cost => (Card.Value ?? 0) * 2; // Double the base cost
    
    public void Purchase()
    {
        // Custom purchase logic
        var upgradedCard = Card.Clone();
        upgradedCard.ModifyValue(-1); // Make it cheaper to use
        
        CardGameMaster.Instance.deckManager.AddActionCard(upgradedCard);
        ScoreManager.SubtractMoneys(Cost);
    }
}
```

## Integration Patterns

### Save/Load System Integration

The Card Core system integrates with the `GameStateManager` for persistence:

```csharp
// Saving game state
CardGameMaster.Instance.Save(); // Saves all card game state

// Loading game state
CardGameMaster.Instance.Load(); // Restores all card game state
```

**Saved Data Includes**:
- Current deck composition
- Hand state
- Placed cards
- Plant states and afflictions
- Score and money
- Turn/round progress

### Plant System Integration

Card treatments interact with the plant system through the `PlantController`:

```csharp
// Applying treatment from card to plant
public class TreatmentCard : ICard
{
    public PlantAfflictions.ITreatment Treatment { get; set; }
    
    public void ApplyToPlant(PlantController plant)
    {
        Treatment.ApplyTreatment(plant);
        plant.UsedTreatments.Add(Treatment);
    }
}
```

### Audio System Integration

The Card Core system integrates with the audio system for feedback:

```csharp
// Playing card-related sounds
CardGameMaster.Instance.playerHandAudioSource.PlayOneShot(
    CardGameMaster.Instance.soundSystem.selectCard);

// Queuing plant effect sounds
TurnController.QueuePlantEffect(plant, null, healSound, 0.3f);
```

### UI System Integration

Card visuals are managed through the UI integration:

```csharp
// Setting up card view
var cardView = cardObject.GetComponent<CardView>();
cardView.Setup(cardData);

// Updating UI text
CardGameMaster.Instance.moneysText.text = $"Money: ${currentMoney}";
```

## Configuration Guide

### Required Components

To set up the Card Core system, ensure these components are properly configured:

#### CardGameMaster Setup

```csharp
[RequireComponent(typeof(DeckManager))]
[RequireComponent(typeof(ScoreManager))]  
[RequireComponent(typeof(TurnController))]
public class CardGameMaster : MonoBehaviour
```

**Required References**:
- `DeckManager` component
- `ScoreManager` component
- `TurnController` component
- `ShopManager` reference
- `SoundSystemMaster` reference
- `CinematicDirector` reference
- UI text components for display

#### DeckManager Configuration

**Required Prefabs**:
- `cardPrefab`: Base card prefab with `CardView` and `Click3D` components
- Plant prefabs: `coleusPrefab`, `chrysanthemumPrefab`, `cucumberPrefab`, `pepperPrefab`

**Required Transforms**:
- `actionCardParent`: Parent transform for action cards in hand
- `plantLocations`: List of transforms where plants can be placed
- `stickerPackParent`: Parent for sticker visuals

**Configuration Values**:
```csharp
public int cardsDrawnPerTurn = 4;    // Cards drawn each turn
public int redrawCost = 3;           // Cost to redraw hand
public float cardSpacing = 1f;       // Spacing between cards in hand
```

#### TurnController Configuration

**Game Balance Settings**:
```csharp
public int turnCount = 4;      // Turns per round
public int moneyGoal = 100;    // Money needed to advance level
```

**UI References**:
- Win/lose screen GameObjects
- Ready-to-play delegate function

### Scene Setup Requirements

1. **Camera Setup**: Main camera tagged as "MainCamera"
2. **Plant Locations**: Empty GameObjects positioned where plants should appear
3. **Card Holders**: `PlacedCardHolder` components at each plant location
4. **UI Canvas**: Canvas with UI input module for shop interface
5. **Audio Sources**: Audio sources for player hand and robot feedback

### Card Prefab Requirements

Card prefabs must include:
- `CardView` component with UI text references
- `Click3D` component for interaction
- `Renderer` component for material changes
- `Collider` component for raycasting

### Material Setup

Cards use materials loaded from Resources:
- Path: `Resources/Materials/Cards/[CardName]`
- Materials should support color property changes for hover effects

## Testing

### Unit Testing Approach

The Card Core system can be tested using Unity's Test Framework:

```csharp
[Test]
public void DrawActionHand_ShouldDrawCorrectNumberOfCards()
{
    // Arrange
    var deckManager = CardGameMaster.Instance.deckManager;
    var expectedCardCount = deckManager.cardsDrawnPerTurn;
    
    // Act
    deckManager.DrawActionHand();
    
    // Assert
    Assert.AreEqual(expectedCardCount, deckManager.GetActionHand().Count);
}

[Test]  
public void CalculateScore_WithHealthyPlants_ShouldReturnPositiveScore()
{
    // Arrange
    var scoreManager = CardGameMaster.Instance.scoreManager;
    // Place healthy plants, no afflictions
    
    // Act
    var score = scoreManager.CalculateScore();
    
    // Assert
    Assert.Greater(score, 0);
}
```

### Integration Testing

Test complete game flows:

```csharp
[UnityTest]
public IEnumerator CompleteTurn_ShouldProgressGameState()
{
    // Arrange
    var turnController = CardGameMaster.Instance.turnController;
    var initialTurn = turnController.currentTurn;
    
    // Act
    yield return turnController.BeginTurnSequence();
    turnController.EndTurn();
    
    // Assert
    Assert.AreEqual(initialTurn + 1, turnController.currentTurn);
}
```

### Manual Testing Checklist

**Card Operations**:
- [ ] Cards can be selected and deselected
- [ ] Cards can be placed on card holders
- [ ] Cards can be picked up from holders
- [ ] Card swapping works correctly
- [ ] Redraw functionality works and costs money

**Game Flow**:
- [ ] Turns progress correctly
- [ ] Rounds advance when appropriate
- [ ] Win/lose conditions trigger correctly
- [ ] Tutorial sequence flows properly

**Visual/Audio**:
- [ ] Card animations play smoothly
- [ ] Audio cues play at appropriate times
- [ ] UI updates correctly reflect game state
- [ ] Plant effects queue and play in sequence

**Persistence**:
- [ ] Game state saves correctly
- [ ] Game state loads correctly
- [ ] All card states are preserved
- [ ] Plant states are restored properly

---

## Troubleshooting

### Common Issues

**Cards not responding to clicks**:
- Ensure `Click3D` component is enabled
- Check that `click3DGloballyDisabled` is false
- Verify colliders are present and properly sized

**Hand cards not displaying**:
- Check `actionCardParent` reference is set
- Ensure card prefab has `CardView` component
- Verify materials are loaded correctly from Resources

**Afflictions not applying**:
- Ensure plants have `PlantController` components
- Check that affliction cards implement `ICard.Affliction` property
- Verify plant locations are properly configured

**Save/load not working**:
- Check `GameStateManager` is properly referenced
- Ensure all serializable data is properly marked
- Verify file permissions in save directory

### Performance Considerations

**Card Display Optimization**:
- Use object pooling for frequently created/destroyed cards
- Limit the number of simultaneous card animations
- Cache material property blocks to avoid material instance creation

**Plant Effect Queue**:
- Limit queued effects to prevent memory buildup
- Consider using coroutine pooling for frequent effects
- Clear queue when changing scenes or restarting game

### Debug Features

Enable debugging in CardGameMaster:
```csharp
public bool debuggingCardClass = true;
```

This will log detailed card operation information to help diagnose issues.

---

*This documentation covers the Card Core system as of the current implementation. For the most up-to-date information, refer to the actual source code in the `Assets/_project/Scripts/Card Core/` directory.*

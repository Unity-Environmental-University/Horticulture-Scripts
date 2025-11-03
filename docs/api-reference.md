# Horticulture API Reference

This document provides a comprehensive API reference for the Horticulture Unity project, covering all major public classes, interfaces, and methods available to developers.

## Table of Contents

1. [Card System](#card-system)
2. [Plant Management](#plant-management)
3. [Game State](#game-state)
4. [Core Components](#core-components)
5. [User Interface](#user-interface)
6. [Audio System](#audio-system)
7. [Sticker System](#sticker-system)

## Card System

### ICard Interface

The base interface for all cards in the game.

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

#### Properties

- **Name** - Display name of the card
- **Description** - Text description of card effects
- **Value** - Monetary value (positive for plants, negative for treatments)
- **Affliction** - Associated affliction (for affliction cards)
- **Treatment** - Associated treatment (for treatment cards)
- **Prefab** - Unity prefab for visual representation
- **Material** - Material for card appearance
- **Stickers** - List of applied modification stickers

#### Methods

- **Clone()** - Creates a deep copy of the card
- **Selected()** - Called when the card is selected by player
- **ApplySticker(ISticker)** - Applies a modification sticker
- **ModifyValue(int)** - Changes the card's value

### IPlantCard Interface

Extends ICard for plant-specific functionality.

```csharp
public interface IPlantCard : ICard
{
    InfectLevel Infect { get; }
    int EggLevel { get; set; }
}
```

#### Properties

- **Infect** - Infection tracking system for all affliction sources
- **EggLevel** - Total egg count across all afflictions

### IAfflictionCard Interface

Extends ICard for affliction-specific functionality.

```csharp
public interface IAfflictionCard : ICard
{
    int BaseInfectLevel { get; set; }
    int BaseEggLevel { get; set; }
}
```

#### Properties

- **BaseInfectLevel** - Initial infection level when applied
- **BaseEggLevel** - Initial egg count when applied

### ILocationCard Interface

For persistent location-based cards (new feature).

```csharp
public interface ILocationCard : ICard
{
    int EffectDuration { get; }
    bool IsPermanent { get; }
    LocationEffectType EffectType { get; }
    
    void ApplyLocationEffect(PlantController plant);
    void RemoveLocationEffect(PlantController plant);
}
```

#### Properties

- **EffectDuration** - Number of turns the effect lasts
- **IsPermanent** - Whether the effect persists indefinitely
- **EffectType** - Category of location effect

#### Methods

- **ApplyLocationEffect(PlantController)** - Applies effect to plant at location
- **RemoveLocationEffect(PlantController)** - Removes effect from plant

## Plant Management

### PlantController Class

Controls individual plant behavior and state.

```csharp
public class PlantController : MonoBehaviour
```

#### Public Properties

```csharp
public List<PlantAfflictions.IAffliction> CurrentAfflictions { get; }
public List<PlantAfflictions.ITreatment> CurrentTreatments { get; }
public List<PlantAfflictions.IAffliction> PriorAfflictions { get; }
public List<PlantAfflictions.ITreatment> UsedTreatments { get; }
public int EggLevel { get; set; }
public PlantType type;
public ICard PlantCard;
```

#### Key Methods

```csharp
// Affliction Management
public void AddAffliction(PlantAfflictions.IAffliction affliction)
public void RemoveAffliction(PlantAfflictions.IAffliction affliction)
public bool HasAffliction(PlantAfflictions.IAffliction affliction)
public bool HasHadAffliction(PlantAfflictions.IAffliction affliction)

// Infection Tracking
public int GetInfectLevel()
public void SetInfectLevel(int infectLevel)
public int GetInfectFrom(PlantAfflictions.IAffliction affliction)
public int GetEggsFrom(PlantAfflictions.IAffliction affliction)

// Visual Updates
public void UpdatePriceFlag(int newValue)
public void SetMoldIntensity(float value)
public void FlagShadersUpdate()

// Daily Processing
public void ProcessDay()
```

### PlantManager Class

Manages collections of plants and batch operations.

```csharp
public class PlantManager : MonoBehaviour
```

#### Properties

```csharp
public readonly List<GameObject> CachedPlants
```

#### Methods

```csharp
public void TriggerPlantTreatments()
```

Processes daily activities for all managed plants.

### PlantType Enumeration

```csharp
[Flags]
public enum PlantType
{
    NotYetSelected = 0,
    Coleus = 1 << 0,
    Pepper = 1 << 1,
    Cucumber = 1 << 2,
    Chrysanthemum = 1 << 3
}
```

## Game State

### GameStateManager Class

Handles save and load operations for complete game persistence.

```csharp
public static class GameStateManager
```

#### Key Methods

```csharp
public static void SaveGame()
public static void LoadGame()
public static bool SaveExists()
public static void DeleteSave()
```

### GameStateData Classes

#### GameStateData

Root container for all save data.

```csharp
public class GameStateData
{
    public TurnData turnData;
    public ScoreData scoreData;
    public DeckData deckData;
    public List<PlantData> plants;
    public RetainedCardData retainedCard;
}
```

#### TurnData

Contains game progression data.

```csharp
public class TurnData
{
    public int turnCount;
    public int level;
    public int moneyGoal;
    public int currentTurn;
    public int currentTutorialTurn;
    public int totalTurns;
    public int currentRound;
    public bool canClickEnd;
    public bool newRoundReady;
    public bool shopQueued;
    public bool tutorialCompleted;
}
```

#### PlantData

Individual plant state data.

```csharp
public class PlantData
{
    public PlantType plantType;
    public CardData plantCard;
    public int locationIndex;
    public List<string> currentAfflictions;
    public List<string> priorAfflictions;
    public List<string> currentTreatments;
    public List<string> usedTreatments;
    public float moldIntensity;
}
```

## Core Components

### CardGameMaster Class

Central singleton controlling the entire card game system.

```csharp
public class CardGameMaster : MonoBehaviour
```

#### Singleton Access

```csharp
public static CardGameMaster Instance { get; private set; }
```

#### Component References

```csharp
public DeckManager deckManager;
public ScoreManager scoreManager;
public TurnController turnController;
public ShopManager shopManager;
public SoundSystemMaster soundSystem;
public CinematicDirector cinematicDirector;
```

#### Key Methods

```csharp
public void Save()
public void Load()
public void SelfDestruct()
```

### DeckManager Class

Manages card operations and deck state.

```csharp
public class DeckManager : MonoBehaviour
```

#### Properties

```csharp
public ICard SelectedACard { get; set; }
public int cardsDrawnPerTurn = 4;
public int redrawCost = 3;
```

#### Key Methods

```csharp
// Hand Management
public void DrawActionHand()
public void DiscardActionCard(ICard card, bool addToDiscard = true)
public void RedrawCards()

// Plant Operations
public IEnumerator PlacePlants()
public void ClearAllPlants()
public IEnumerator ClearPlant(PlantController plant)

// Affliction Management
public void DrawAfflictions()
public void ApplyAfflictionDeck()

// Deck Operations
public void ShuffleDeck(List<ICard> deck)
```

### TurnController Class

Controls game flow and turn progression.

```csharp
public class TurnController : MonoBehaviour
```

#### Properties

```csharp
public int currentTurn;
public int currentRound;
public int turnCount = 4;
public int moneyGoal = 100;
public bool canClickEnd;
```

#### Key Methods

```csharp
public void EndTurn()
public IEnumerator BeginTurnSequence()
public static void QueuePlantEffect(PlantController plant, ParticleSystem particle, AudioClip sound, float delay)
```

### ScoreManager Class

Handles scoring, economy, and cost calculations.

```csharp
public class ScoreManager : MonoBehaviour
```

#### Properties

```csharp
public int treatmentCost;
```

#### Key Methods

```csharp
public int CalculateScore()
public void CalculateTreatmentCost()
public void CalculatePotentialProfit()
public static int GetMoneys()
public static void SubtractMoneys(int amount)
```

## User Interface

### CardView Class

Manages visual representation of cards.

```csharp
public class CardView : MonoBehaviour
```

#### Key Methods

```csharp
public void Setup(ICard card)
public void UpdateDisplay()
```

### Click3D Class

Handles 3D object interaction and clicking.

```csharp
public class Click3D : MonoBehaviour
```

#### Properties

```csharp
public bool click3DGloballyDisabled;
```

#### Events

```csharp
public UnityEvent OnClick;
public UnityEvent OnClickDown;
public UnityEvent OnClickUp;
public UnityEvent OnHoverEnter;
public UnityEvent OnHoverExit;
```

### PlacedCardHolder Class

Manages card placement locations on the board.

```csharp
public class PlacedCardHolder : MonoBehaviour
```

#### Properties

```csharp
public bool HoldingCard { get; }
public ICard HeldCard { get; }
```

#### Key Methods

```csharp
public void TakeSelectedCard()
public void OnPlacedCardClicked()
public bool CanTakeCard()
```

## Audio System

### SoundSystemMaster Class

Manages all audio playback in the game.

```csharp
public class SoundSystemMaster : MonoBehaviour
```

#### Audio Clips

```csharp
public AudioClip selectCard;
public AudioClip placeCard;
public AudioClip plantHeal;
public AudioClip plantDamage;
```

#### Methods

```csharp
public AudioClip GetInsectSound(PlantAfflictions.IAffliction affliction)
public void PlaySound(AudioClip clip)
```

## Sticker System

### ISticker Interface

Base interface for card modification stickers.

```csharp
public interface ISticker
{
    string Name { get; }
    string Description { get; }
    int? Value { get; set; }
    
    ISticker Clone();
    void ApplyToCard(ICard card);
}
```

### Common Sticker Classes

#### ValueReducerSticker

Reduces the cost of treatment cards.

```csharp
public class ValueReducerSticker : ISticker
{
    public string Name => "Cost Reducer";
    public string Description => "Reduces card cost by 1";
    public int? Value { get; set; } = -1;
    
    public void ApplyToCard(ICard card)
    {
        card.ModifyValue(Value ?? 0);
    }
}
```

#### CopyCardSticker

Creates duplicate cards when applied.

```csharp
public class CopyCardSticker : ISticker
{
    public string Name => "Card Duplicator";
    public string Description => "Creates a copy of this card";
    
    public void ApplyToCard(ICard card)
    {
        // Implementation creates card copy
    }
}
```

## Usage Examples

### Creating a New Treatment Card

```csharp
public class CustomTreatment : ICard
{
    public string Name => "Custom Treatment";
    public string Description => "Removes custom afflictions";
    
    private int _value = -3;
    public int? Value 
    { 
        get => _value; 
        set => _value = value ?? 0; 
    }
    
    public PlantAfflictions.ITreatment Treatment => new CustomTreatmentLogic();
    public Material Material => Resources.Load<Material>("Materials/Cards/CustomTreatment");
    public List<ISticker> Stickers { get; } = new();
    
    public ICard Clone() => new CustomTreatment { Value = this.Value };
    public void ModifyValue(int delta) => _value += delta;
}
```

### Applying Treatment to Plant

```csharp
public void ApplyTreatmentToPlant(PlantController plant, PlantAfflictions.ITreatment treatment)
{
    // Apply the treatment
    treatment.ApplyTreatment(plant);
    
    // Add to plant's treatment history
    plant.CurrentTreatments.Add(treatment);
    
    // Queue visual effect
    TurnController.QueuePlantEffect(
        plant, 
        healParticles, 
        healSound, 
        0.3f
    );
}
```

### Saving Game State

```csharp
public void SaveCurrentGame()
{
    try
    {
        CardGameMaster.Instance.Save();
        Debug.Log("Game saved successfully");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to save game: {e.Message}");
    }
}
```

### Processing Plant Afflictions

```csharp
public void ProcessAfflictionSpreading(List<PlantController> plants)
{
    foreach (var plant in plants)
    {
        foreach (var affliction in plant.CurrentAfflictions.ToList())
        {
            // Check for spreading to adjacent plants
            if (UnityEngine.Random.value < 0.5f) // 50% spread chance
            {
                var adjacentPlant = GetAdjacentPlant(plant);
                if (adjacentPlant && !adjacentPlant.HasAffliction(affliction))
                {
                    adjacentPlant.AddAffliction(affliction);
                }
            }
        }
    }
}
```

## Event System

### Common Events

The game uses Unity Events for decoupled communication:

```csharp
// Card selection events
public static event System.Action<ICard> OnCardSelected;
public static event System.Action<ICard> OnCardDeselected;

// Turn progression events  
public static event System.Action<int> OnTurnStart;
public static event System.Action<int> OnTurnEnd;
public static event System.Action<int> OnRoundStart;

// Plant events
public static event System.Action<PlantController> OnPlantPlaced;
public static event System.Action<PlantController> OnPlantRemoved;
public static event System.Action<PlantController, PlantAfflictions.IAffliction> OnAfflictionAdded;
```

## Error Handling

### Common Exception Types

The API uses standard C# exceptions:

- **ArgumentNullException** - When required parameters are null
- **InvalidOperationException** - When operations are called in invalid states
- **NotImplementedException** - For incomplete features

### Debugging Support

Enable debug logging:

```csharp
CardGameMaster.Instance.debuggingCardClass = true;
```

This enables detailed logging for:
- Card selection and placement
- Affliction application and removal
- Treatment processing
- Save/load operations

---

## Version Information

- **API Version**: 1.0.0
- **Unity Version**: 6000.1.11f1+
- **Last Updated**: Current as of latest project state

For implementation details and examples, see the source code in `Assets/_project/Scripts/` and the comprehensive documentation in the `docs/` directory.
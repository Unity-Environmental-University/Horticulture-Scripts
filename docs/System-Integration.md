# System Integration Map

**Visual guide to how Horticulture's systems interconnect and communicate.**

## 🗺️ High-Level System Map

```
┌──────────────────────────────────────────────────────┐
│                  CardGameMaster                      │
│              (Central Coordinator)                   │
│                                                      │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ DeckManager │  │ TurnController│  │ScoreManager││
│  └─────────────┘  └──────────────┘  └────────────┘ │
└──────────────────────────────────────────────────────┘
         │                  │                  │
         ↓                  ↓                  ↓
┌─────────────────┐ ┌──────────────┐ ┌────────────────┐
│  Plant System   │ │  Game State  │ │  UI System     │
│                 │ │              │ │                │
│ PlantController │ │ SaveLoad     │ │ CardView       │
│ PlantManager    │ │ Serialization│ │ Click3D        │
│ HealthBars      │ │              │ │ Menus          │
└─────────────────┘ └──────────────┘ └────────────────┘
         │                  │                  │
         ↓                  ↓                  ↓
┌─────────────────┐ ┌──────────────┐ ┌────────────────┐
│ Visual Effects  │ │  Analytics   │ │  Audio System  │
│                 │ │              │ │                │
│ Particles       │ │ Tracking     │ │ SoundSystem    │
│ Shaders         │ │ Metrics      │ │ AudioSources   │
│ Animations      │ │ Reporting    │ │                │
└─────────────────┘ └──────────────┘ └────────────────┘
```

## 🔄 Data Flow Diagrams

### Card Selection & Placement Flow

```
User Click
    ↓
Click3D Component
    ↓
CardGameMaster.deckManager.SelectedACard = card
    ↓
User Clicks Plant Location
    ↓
PlacedCardHolder.TakeSelectedCard()
    ↓
┌─────────────────────────────────────┐
│ If Treatment Card:                  │
│   → PlantController.ApplyTreatment()│
│   → Remove Afflictions              │
│   → Update Health Display           │
│   → Queue Visual Effects            │
│   → Play Audio                      │
└─────────────────────────────────────┘
    ↓
DeckManager.DiscardActionCard(card)
    ↓
ScoreManager.CalculateTreatmentCost()
    ↓
UI Updates
```

### Turn Progression Flow

```
User Clicks "End Turn"
    ↓
TurnController.EndTurn()
    ↓
┌──────────────────────────┐
│ Process Plant Effects:   │
│ 1. Execute queued effects│
│ 2. Spread afflictions    │
│ 3. Check plant deaths    │
└──────────────────────────┘
    ↓
currentTurn++
    ↓
┌────────────────────────────┐
│ If Turn < MaxTurns:        │
│   → BeginTurnSequence()    │
│   → DrawActionHand()       │
│   → Resume gameplay        │
│                            │
│ If Turn == MaxTurns:       │
│   → CalculateScore()       │
│   → OpenShop()             │
│   → New Round or Game Over │
└────────────────────────────┘
```

### Save/Load Flow

```
User Triggers Save
    ↓
CardGameMaster.Save()
    ↓
┌─────────────────────────────────┐
│ Collect State:                  │
│ • TurnController → TurnData     │
│ • ScoreManager → ScoreData      │
│ • DeckManager → DeckData        │
│ • PlantControllers → PlantData[]│
└─────────────────────────────────┘
    ↓
GameStateData object created
    ↓
JsonUtility.ToJson(gameState)
    ↓
File.WriteAllText(savePath, json)
    ↓
Save Complete


User Triggers Load
    ↓
CardGameMaster.Load()
    ↓
json = File.ReadAllText(savePath)
    ↓
gameState = JsonUtility.FromJson(json)
    ↓
┌─────────────────────────────────┐
│ Restore State:                  │
│ • TurnData → TurnController     │
│ • ScoreData → ScoreManager      │
│ • DeckData → DeckManager        │
│ • PlantData[] → Recreate Plants │
└─────────────────────────────────┘
    ↓
UI Updates
    ↓
Load Complete
```

## 🔌 System Dependencies

### CardGameMaster Dependencies

```
CardGameMaster
├── DeckManager (required)
├── ScoreManager (required)
├── TurnController (required)
├── ShopManager (required)
├── SoundSystemMaster (optional)
├── CinematicDirector (optional)
└── UI Components (required)
    ├── Text displays
    ├── Card holders
    └── Menus
```

### DeckManager Dependencies

```
DeckManager
├── CardGameMaster (parent)
├── PlantController (many)
├── PlacedCardHolder (many)
├── CardView (many)
├── Click3D (many)
└── Plant Prefabs
    ├── coleusPrefab
    ├── chrysanthemumPrefab
    ├── cucumberPrefab
    └── pepperPrefab
```

### PlantController Dependencies

```
PlantController
├── PlantCard (ICard)
├── PlantHealthBarHandler
├── PlantCardFunctions
├── Renderer (for materials)
├── ParticleSystems
└── AudioSource (optional)
```

## 📡 Event Communication

### Publisher-Subscriber Patterns

```csharp
// Card Events
CardGameMaster
    → OnCardSelected
        → CardView subscribes (visual feedback)
        → Click3D subscribes (disable other cards)
        → UI subscribes (highlight valid targets)

// Turn Events
TurnController
    → OnTurnEnd
        → ScoreManager subscribes (calculate costs)
        → PlantManager subscribes (process afflictions)
        → Analytics subscribes (track performance)
        → UI subscribes (update displays)
    
    → OnRoundEnd
        → ScoreManager subscribes (calculate final score)
        → ShopManager subscribes (open shop)
        → Analytics subscribes (record round data)

// Plant Events
PlantController
    → OnAfflictionAdded
        → HealthBar subscribes (update display)
        → ParticleSystem subscribes (play effect)
        → Analytics subscribes (track afflictions)
    
    → OnPlantDeath
        → DeckManager subscribes (clear plant)
        → ScoreManager subscribes (update value)
        → PlantManager subscribes (remove from list)
```

## 🔀 Cross-System Operations

### Apply Treatment Example

Shows how multiple systems coordinate:

```
1. USER ACTION
   └─ Click treatment card
      └─ Click3D → CardGameMaster

2. CARD SYSTEM
   └─ DeckManager.SelectedACard = treatment
      └─ CardView updates visual state

3. PLACEMENT
   └─ Click plant location
      └─ PlacedCardHolder validates
         └─ TakeSelectedCard()

4. PLANT SYSTEM
   └─ PlantController.ApplyTreatment()
      ├─ Remove afflictions
      ├─ Update health value
      └─ Add to treatment history

5. VISUAL FEEDBACK
   └─ TurnController.QueuePlantEffect()
      ├─ Particle system plays
      └─ Shader updates

6. AUDIO FEEDBACK
   └─ SoundSystemMaster.PlaySound()
      └─ Treatment sound plays

7. SCORING
   └─ ScoreManager.CalculateTreatmentCost()
      └─ Update money display

8. ANALYTICS
   └─ Track treatment application
      ├─ Treatment type
      ├─ Target plant
      └─ Turn number

9. GAME STATE
   └─ Update serializable state
      ├─ Treatment in plant history
      └─ Card moved to discard
```

### Save Game Example

```
1. USER TRIGGERS SAVE
   └─ Menu button or auto-save

2. CARDGAMEMASTER COORDINATES
   └─ CardGameMaster.Save()

3. DATA COLLECTION
   ├─ TurnController
   │  └─ Provides TurnData
   ├─ ScoreManager
   │  └─ Provides ScoreData
   ├─ DeckManager
   │  ├─ Serializes all decks
   │  ├─ Serializes hand
   │  └─ Provides retained card
   └─ PlantControllers
      └─ Each provides PlantData

4. SERIALIZATION
   ├─ Convert ICard to CardData
   ├─ Store type names for polymorphism
   └─ Create GameStateData object

5. FILE SYSTEM
   ├─ JsonUtility.ToJson()
   ├─ File.WriteAllText()
   └─ Success/failure feedback

6. UI FEEDBACK
   └─ Show "Game Saved" message
```

## 🎯 System Responsibilities Matrix

| System | Creates | Modifies | Reads | Notifies |
|--------|---------|----------|-------|----------|
| **CardGameMaster** | Game instance | All systems | N/A | Initialization |
| **DeckManager** | Cards, Plants | Decks, Hand | Game state | Card events |
| **TurnController** | N/A | Turn/Round | All systems | Turn events |
| **ScoreManager** | N/A | Money | Cards, Plants | Score changes |
| **PlantController** | Effects | Own state | Treatments | Health events |
| **GameStateManager** | Save files | N/A | All systems | N/A (static) |
| **UI Systems** | Displays | UI elements | Game state | User input |
| **Audio System** | N/A | Audio | System events | N/A |
| **Analytics** | Metrics | Tracking data | All systems | N/A |

## 🛣️ Integration Points

### Adding New Card Type

**Systems Affected:**
1. **Card System** - New ICard implementation
2. **DeckManager** - Add to deck initialization
3. **UI System** - CardView displays new card
4. **Save System** - CardData handles serialization
5. **Analytics** - Track new card usage

### Adding New Plant Type

**Systems Affected:**
1. **Plant System** - New PlantType enum value
2. **Card System** - New IPlantCard implementation
3. **DeckManager** - Add plant prefab reference
4. **Visual System** - Plant model and materials
5. **Save System** - PlantData handles new type

### Adding New Game Mechanic

**Systems Affected:**
1. **Turn System** - May need new phase
2. **Game State** - New data structures
3. **UI System** - New displays/controls
4. **Save System** - Serialize new state
5. **Analytics** - Track new mechanic usage

## 🔗 Related Documentation

- [[ARCHITECTURE|Architecture Overview]]
- [[Core-Systems|Core Systems]]
- [[api-reference|API Reference]]
- [[Common-Workflows|Common Workflows]]

---

*Use this map to understand system boundaries and integration points when making changes.*

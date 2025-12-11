# Core Systems Overview

**Documentation for Horticulture's core game systems and architectures.**

## 🎴 Card Game System

The heart of Horticulture's gameplay mechanics.

### Key Components
- **[[card-core-system|Card Core System]]** - Complete card mechanics documentation
- **DeckManager** - Card operations, drawing, shuffling
- **CardGameMaster** - Central game coordinator
- **ShopManager** - Card purchasing system

### Card Types
- **Plant Cards** - Revenue-generating plants with values
- **Treatment Cards** - Remove afflictions, cost money to use
- **Affliction Cards** - Damage plants, reduce values
- **Location Cards** - Persistent location-based effects

### Key Features
- Turn-based card play
- Strategic decision making
- Economic resource management
- Tutorial system for onboarding

**📚 Deep Dive**: [[card-core-system|Card System Documentation]]

---

## 🌱 Plant Management System

Individual plant behavior and lifecycle management.

### Components
- **PlantController** - Individual plant state and behavior
- **PlantManager** - Collection management and batch operations
- **PlantHealthBarHandler** - Visual health representation
- **PlantCardFunctions** - Card-specific plant functionality

### Core Mechanics
- **Health Tracking** - Plant value affected by afflictions
- **Infection System** - Multi-source infection tracking
- **Treatment Application** - Treatment effects on plants
- **Visual Effects** - Particles, shaders, health bars
- **Death System** - Plant removal when value ≤ 0

### Affliction System
- **Types**: Aphids, Mealy Bugs, Thrips, Mildew
- **Spreading**: Adjacent spread (50% chance), global spread (Thrips)
- **Immunity**: Plants remember previous afflictions
- **Egg Tracking**: Separate egg level system

**📚 Deep Dive**: [[Plant-System|Plant System Documentation]]

---

## 💾 Game State & Persistence

Complete game state serialization and save/load functionality.

### Components
- **GameStateManager** - Save/load coordinator (static)
- **GameStateData** - Data structure definitions
- **Serialization System** - JSON-based serialization

### What Gets Saved
- Turn and round progress
- Current money and score
- All decks (action, plant, affliction, discard)
- Current hand state
- All plant states and afflictions
- Retained card between rounds
- Tutorial progress

### Features
- Human-readable JSON format
- Complete game state preservation
- Version compatibility management
- Graceful handling of missing data

**📚 Deep Dive**: [[game-state-system-documentation|Game State Documentation]]

---

## 🎮 Game Flow & Turn System

Controls game progression and turn-based mechanics.

### Components
- **TurnController** - Turn and round management
- **ScoreManager** - Economy and scoring calculations
- **Level System** - Difficulty progression

### Turn Structure
1. **Round Start** - Place plants, apply random afflictions
2. **Player Turns** (4 per round):
   - Draw action cards
   - Place treatment cards
   - Apply treatments
   - Process affliction spreading
3. **Round End** - Calculate score, open shop
4. **Continue/End** - Check win/lose conditions

### Scoring
```
Final Score = Plant Values + Affliction Penalties + Treatment Costs + Current Money
```

### Difficulty Scaling
- Rent increases with level (+$50 from level 3)
- Money goal scales with difficulty
- Tutorial mode for first 5 turns

**📚 Deep Dive**: [[card-core-system#game-mechanics|Game Mechanics]]

---

## 📊 Analytics System

Player data tracking and performance measurement.

### Tracked Metrics
- Round performance and progression
- Treatment effectiveness
- Player behavior and decisions
- Victory conditions and profitability
- Educational outcomes

### Data Collection
- Turn-by-turn actions
- Card usage patterns
- Economic decisions
- Affliction management strategies

### Use Cases
- Game balance analysis
- Educational assessment
- Player progression tracking
- Feature usage statistics

**📚 Deep Dive**: [[analytics-system|Analytics Documentation]]

---

## 🔊 Audio System

Sound effects and audio management.

### Components
- **SoundSystemMaster** - Central audio coordinator
- **Audio Sources** - Per-component audio playback
- **Sound Library** - Organized audio clips

### Audio Types
- Card interactions (select, place, discard)
- Plant effects (heal, damage, death)
- Affliction sounds (insect noises)
- UI feedback sounds

### Features
- Context-sensitive audio selection
- Audio source pooling
- Volume management
- Sound variation system

**📚 Deep Dive**: [[audio-system-documentation|Audio Documentation]]

---

## 🎨 UI & Input System

User interface and player input handling.

### Components
- **CardView** - Visual card representation
- **Click3D** - 3D object interaction
- **PlacedCardHolder** - Card placement management
- **UI Controllers** - Menu and display systems

### Input Handling
- Mouse/touch input
- Raycast-based 3D interaction
- Click and drag system
- Hover effects

### UI Features
- Dynamic card display
- Health bars and indicators
- Money and score displays
- Shop interface
- Win/lose screens

**📚 Deep Dive**: [[ui-input-management|UI & Input Documentation]]

---

## 🎬 Cinematics System

Camera control and cinematic sequences.

### Features
- Camera transitions
- Zoom and focus effects
- Cinematic sequences
- Inspection mode

### Components
- **CinematicDirector** - Sequence coordinator
- Camera controller
- Animation system

**📚 Deep Dive**: [[cinematics-system-documentation|Cinematics Documentation]]

---

## 🎯 Animation System

Animation hooks and visual feedback.

### Components
- DOTween integration
- Animation sequences
- Visual transitions
- Card movement animations

### Key Features
- Smooth card animations
- Plant effect sequences
- UI transitions
- Camera movements

**📚 Deep Dive**: [[animation-hooks|Animation Documentation]]

---

## 🏷️ Sticker System

Card modification and enhancement system.

### Sticker Types
- **Value Modifiers** - Change card costs/values
- **Card Duplicators** - Create card copies
- **Special Effects** - Unique card enhancements

### Application
- Apply to cards in shop or gameplay
- Persistent across save/load
- Stack multiple stickers
- Visual indicators on cards

---

## 🎓 Classes System

Player progression and unlockable content.

### Features
- Character classes
- Unique abilities
- Progression system
- Unlock mechanics

**📚 Deep Dive**: [[classes-system-documentation|Classes Documentation]]

---

## 🔧 Mod Support

Modding capabilities and mod loading system.

### Features
- Mod loading infrastructure
- Custom card support
- Configuration system
- Community content

**📚 Resources**:
- [[modding-guide|Modding Guide]]
- [[mod-loading-system-documentation|Mod Loading System]]

---

## 🔗 System Integration

### How Systems Connect

```
CardGameMaster (Hub)
├── Card System → Plants → Visual Effects
├── Turn System → Game State → Save/Load
├── Scoring → Analytics → UI Display
└── Audio System → All Systems
```

### Event Flow
1. Player action triggers card system
2. Card system affects plant system
3. Plant changes trigger visual/audio
4. State changes propagate to UI
5. Analytics track all interactions
6. Game state serializes everything

---

## 📚 Related Documentation

- [[ARCHITECTURE|Architecture Overview]]
- [[api-reference|API Reference]]
- [[Quick-Reference|Quick Reference]]
- [[Common-Workflows|Common Workflows]]

---

*This is the foundation of Horticulture - understanding these systems is key to effective development.*

# ModLoader System

This directory contains the complete ModLoader system for the Horticulture game.

## Files Overview

### Core System Files
- **`ModLoader.cs`** - Main mod loading system (~130 lines)
  - Loads JSON card files and AssetBundle stickers
  - Entry point: `ModLoader.TryLoadMods(CardGameMaster)`
  - Scans both user and game mod directories

- **`ModAssets.cs`** - AssetBundle registry (~35 lines) 
  - `RegisterBundle(key, bundle)` - Store loaded bundles
  - `LoadFromBundle<T>(key, assetName)` - Retrieve assets

- **`ModInfo.cs`** - Simple mod metadata (~25 lines)
  - Basic mod information: name, version, author, description
  - Safe JSON parsing with `FromJson(string)`

## Integration Points

### Usage in Game
- Called from `CardGameMaster.Awake()` via `ModLoader.TryLoadMods(this)`
- Registers cards with `DeckManager.RegisterModActionPrototype()`
- Registers stickers with `DeckManager.RegisterModSticker()`

### External Dependencies
- Uses `RuntimeCard` class for card creation
- Integrates with plant treatment system (`PlantAfflictions.*`)
- References `StickerDefinition` for bundle assets

## Mod Directory Structure
```
Application.persistentDataPath/Mods/     (User mods)
├── card1.card.json
├── card2.card.json  
└── stickers.bundle

Application.streamingAssetsPath/Mods/    (Shipped mods)
├── default_cards.card.json
└── default_stickers.bundle
```

## File Formats

### Card JSON (*.card.json)
```json
{
    "name": "Card Name",
    "description": "Card description",
    "value": -3,
    "treatment": "SoapyWater", 
    "weight": 3,
    "rarity": "common"
}
```

### AssetBundle (*.bundle)
- Contains `StickerDefinition` assets
- Automatically registered with filename as key
- Assets accessible via `ModAssets.LoadFromBundle<T>()`

## Namespace
All files use: `_project.Scripts.ModLoader`

## Total System Size
~190 lines of core functionality + tests + documentation
# Horticulture Mod Creation Guide

Simple guide for creating mods for the Horticulture Unity game.

## System Information

- **Namespace**: `_project.Scripts.ModLoading`
- **System Location**: `/Assets/_project/Scripts/ModLoading/`
- **Loads**: JSON cards (`*.card.json`) and AssetBundle stickers (`*.bundle`)

## Mod Directory Structure

Place mod files in one of these locations:
- **User Mods**: `Application.persistentDataPath/Mods/` (recommended)
- **Game Mods**: `Application.streamingAssetsPath/Mods/`

On different platforms:
- **Windows**: `%USERPROFILE%/AppData/LocalLow/[CompanyName]/Horticulture/Mods/`
- **macOS**: `~/Library/Application Support/[CompanyName]/Horticulture/Mods/`
- **Linux**: `~/.config/unity3d/[CompanyName]/Horticulture/Mods/`

## Card Mods

Create `.card.json` files to add custom cards to the game.

### Basic Card JSON Format

```json
{
    "name": "My Custom Card",
    "description": "A custom treatment card",
    "value": -3,
    "treatment": "SoapyWater",
    "weight": 3,
    "rarity": "common"
}
```

### Required Fields
- `name` - Card display name
- `description` - Card description text
- `value` - Card cost (negative numbers for costs)

### Optional Fields
- `treatment` - Treatment type: `SoapyWater`, `Fungicide`, `Insecticide`, `HorticulturalOil`, `Spinosad`, `Imidacloprid`, `Panacea`
- `infectCure` - Custom effectiveness against adult pests (overrides default treatment values)
- `eggCure` - Custom effectiveness against egg/larval stages (overrides default treatment values)
- `weight` - How often card appears (1-10, higher = more common) - default: 1
- `rarity` - Alternative to weight: `common` (5), `uncommon` (3), `rare` (2), `epic` (1)
- `prefabResource` - Unity Resources path for card prefab
- `materialResource` - Unity Resources path for card material

### Advanced: Bundle Assets
For mods with custom 3D models/materials:
- `bundleKey` - Name of AssetBundle (without .bundle extension)
- `prefab` - Asset name in bundle for card prefab
- `material` - Asset name in bundle for card material

**Note**: Bundle assets take precedence over Resource assets if both are specified.

## Sticker Mods

Create `.bundle` AssetBundle files containing `StickerDefinition` assets.

## Templates & Examples

Ready-to-use template files are available in `/Assets/_project/Scripts/ModLoading/Templates/`:

- **`basic_treatment.card.json`** - Simple, affordable treatment card
- **`expensive_treatment.card.json`** - High-cost premium card
- **`custom_visual_card.card.json`** - Card with Resources-based visuals
- **`bundle_card.card.json`** - Card with AssetBundle-based visuals
- **`no_treatment_card.card.json`** - Utility card without treatment

Copy any template, rename it, and customize the values for your mod.

### Quick Example
**my_fungicide.card.json**
```json
{
    "name": "Super Fungicide",
    "description": "An effective anti-fungal treatment with enhanced potency",
    "value": -4,
    "treatment": "Fungicide",
    "rarity": "uncommon",
    "infectCure": 2,
    "eggCure": 3
}
```

## Testing Your Mods

1. Place `.card.json` files in your Mods folder
2. Launch Horticulture 
3. Check the Unity console for mod loading messages
4. Look for your cards in the shop or deck

## System Architecture

The ModLoading system consists of:
- **`ModLoader.cs`** - Main loading system (`TryLoadMods()` entry point)
- **`ModAssets.cs`** - AssetBundle registry for custom visuals
- **`RuntimeCard.cs`** - Data-driven card implementation
- **`ModInfo.cs`** - Optional mod metadata support

Integration: Called from `CardGameMaster.Awake()` before deck initialization.

## Troubleshooting

- **Cards not appearing**: Check Unity console for JSON parsing errors
- **Invalid treatment**: Use exact treatment names from the list above  
- **File not found**: Ensure files are in the correct Mods directory
- **Namespace errors**: System uses `_project.Scripts.ModLoading`
- **Weight not working**: Use integer values 1-10, or rarity strings
- **Bundle assets not loading**: Ensure `.bundle` file is in same directory as `.card.json`

## Treatment System & Pest Management

The game uses a sophisticated infect/egg system where each treatment has specific effectiveness:

### Treatment Types & Their Effects
- **HorticulturalOil**: InfectCure=1, EggCure=1 (targets both adults and larvae)
- **Fungicide**: InfectCure=1, EggCure=1 (broad spectrum anti-fungal)
- **Insecticide**: InfectCure=1, EggCure=0 (adults only, leaves eggs)
- **SoapyWater**: InfectCure=1, EggCure=1 (gentle, effective on soft-bodied pests)
- **Spinosad**: InfectCure=1, EggCure=1 (organic, both life stages)
- **Imidacloprid**: InfectCure=1, EggCure=1 (systemic, persistent)
- **Panacea**: InfectCure=999, EggCure=999 (complete elimination)

### Strategic Card Design
- **Adult-focused cards**: Use `Insecticide` for quick knockdown but require follow-up
- **Egg-focused cards**: Use `HorticulturalOil` for preventive control
- **Balanced cards**: Use `SoapyWater` or `Spinosad` for complete pest management
- **Emergency cards**: Use `Panacea` for desperate situations (high cost)

### Affliction-Specific Effectiveness (Recommended)

The most powerful feature for mod cards is **affliction-specific effectiveness**. Instead of generic cure values, define exactly which pests your treatment targets:

```json
{
    "name": "Aphid Specialist",
    "description": "Targeted treatment for aphid infestations",
    "value": -4,
    "effectiveness": [
        {
            "affliction": "Aphids",
            "infectCure": 5,
            "eggCure": 3
        },
        {
            "affliction": "MealyBugs", 
            "infectCure": 1,
            "eggCure": 0
        }
    ]
}
```

**Available Afflictions:**
- `"Aphids"` - Soft-bodied sucking insects
- `"SpiderMites"` - Tiny web-spinning mites
- `"Thrips"` - Small flying/jumping insects
- `"MealyBugs"` - White cotton-like insects
- `"FungusGnats"` - Small flying insects in soil
- `"Mildew"` - Fungal disease

**Key Benefits:**
- **Complete Independence**: No reliance on built-in treatments
- **Surgical Precision**: Target specific pests while leaving others unaffected
- **Realistic IPM**: Matches how real-world treatments work
- **Strategic Depth**: Players need different cards for different problems
- **Future-Proof**: Works with any new afflictions added to the game or through mods

**Design Examples:**
- **Specialist**: High effectiveness against one pest, none against others
- **Broad Spectrum**: Moderate effectiveness against multiple pests  
- **Selective**: Strong against harmful pests, weak/none against beneficial ones
- **Experimental**: Unusual effectiveness patterns for unique gameplay

### Legacy System (Backward Compatible)

For simpler cards, you can still use the legacy `infectCure`/`eggCure` system:

```json
{
    "name": "Simple Treatment",
    "description": "Basic treatment with generic effectiveness",
    "value": -3,
    "treatment": "SoapyWater",
    "infectCure": 2,
    "eggCure": 1
}
```

**Note**: The affliction-specific `effectiveness` array takes priority over legacy fields if both are present.

### Custom Afflictions (Advanced)

Mods can define completely new pest/disease types that don't exist in the base game:

```json
{
    "name": "RustDisease",
    "description": "Orange-brown fungal disease that creates rusty spots on leaves",
    "color": [0.8, 0.4, 0.1, 1.0],
    "shader": "Shader Graphs/Rust",
    "vulnerableToTreatments": ["Fungicide", "Copper"]
}
```

**File Format**: Save as `*.affliction.json` in your Mods directory.

**Fields:**
- `name` - Unique identifier for this affliction (used by treatment cards)
- `description` - Description of the affliction
- `color` - RGBA color values (0-1 range) for visual representation
- `shader` - Optional shader name for visual effects
- `vulnerableToTreatments` - Array of legacy treatment types that can affect this affliction

**Using Custom Afflictions:**
Once defined, custom afflictions can be targeted by treatment cards using their `name`:

```json
{
    "name": "Anti-Rust Treatment",
    "description": "Specialized treatment for rust diseases",
    "value": -4,
    "effectiveness": [
        {
            "affliction": "RustDisease",
            "infectCure": 5,
            "eggCure": 3
        }
    ]
}
```

**Benefits:**
- **Complete Independence**: Create entirely new pest/disease types
- **Visual Customization**: Custom colors and shader effects
- **Strategic Depth**: New afflictions require new treatment strategies
- **Unlimited Expansion**: No limit to the number of custom afflictions

## Advanced Topics

### AssetBundle Creation
1. Create AssetBundle in Unity Editor
2. Name assets appropriately for JSON references
3. Export bundle with platform-specific build
4. Place `.bundle` file in Mods directory

### Custom Treatment Types
Currently, only built-in treatments are supported. Custom treatments require code changes to the `CreateTreatment()` method in `ModLoader.cs`.

## Version Compatibility
- **Unity Version**: 6000.2.0f1 or later
- **System Namespace**: `_project.Scripts.ModLoading`
- **File Format**: JSON for cards, AssetBundle for stickers

That's it! Keep it simple and your mods will work great.
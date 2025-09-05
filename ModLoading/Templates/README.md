# Mod Templates

This directory contains example template files for creating Horticulture mods.

## Template Files

### Basic Card Templates
- **`basic_treatment.card.json`** - Simple treatment card with rarity
- **`expensive_treatment.card.json`** - High-cost card with custom weight
- **`custom_visual_card.card.json`** - Card using Resources for custom visuals
- **`bundle_card.card.json`** - Card using AssetBundle for custom visuals
- **`no_treatment_card.card.json`** - Utility card without treatment effect

### Legacy Treatment Cards (Backward Compatible)
- **`egg_buster.card.json`** - HorticulturalOil card targeting egg stages (InfectCure=1, EggCure=1)
- **`adult_killer.card.json`** - Insecticide card targeting adults only (InfectCure=1, EggCure=0)
- **`broad_spectrum.card.json`** - SoapyWater card effective against both stages (InfectCure=1, EggCure=1)
- **`emergency_cure.card.json`** - Panacea card for complete pest elimination (InfectCure=999, EggCure=999)
- **`targeted_systemic.card.json`** - Imidacloprid card for persistent pests (InfectCure=1, EggCure=1)

### Modern Affliction-Specific Cards (Recommended)
- **`aphid_specialist.card.json`** - Targeted treatment highly effective against aphids with minor effects on related pests
- **`fungal_fighter.card.json`** - Anti-fungal treatment that only affects Mildew (safe for beneficial insects)
- **`broad_spectrum_mod.card.json`** - Universal treatment with different effectiveness against each pest type
- **`completely_custom_treatment.card.json`** - Experimental treatment with unusual effectiveness patterns

### Custom Afflictions (Advanced)
- **`custom_rust_disease.affliction.json`** - Example custom fungal disease with orange-brown coloration
- **`custom_scale_insect.affliction.json`** - Example custom insect pest with hard shell protection
- **`treatment_for_custom_affliction.card.json`** - Treatment card targeting the custom afflictions above

### Metadata Template
- **`mod_info_template.json`** - Optional mod information file (not currently used by system but good for organization)

## Usage

1. Copy the template file that matches your needs
2. Rename it to something descriptive (keep the `.card.json` extension)
3. Edit the values to match your card design
4. Place in your `Mods/` directory

## File Naming

- Use descriptive names: `healing_potion.card.json` instead of `card1.card.json`
- Keep `.card.json` extension for cards
- Keep `.bundle` extension for AssetBundle files

## Testing

After creating your mod files:
1. Launch Horticulture
2. Check the Unity console for loading messages
3. Look for your cards in the game's deck or shop
4. Debug any errors shown in the console

## Advanced Modding

For AssetBundle creation and advanced modding techniques, see the Unity documentation on AssetBundles and the main MOD_GUIDE.md file.
# Modding Guide (Early Preview)

This build supports a simple `Mods` folder for user content:

- Location (both are scanned):
  - `Application.persistentDataPath/Mods` (user-writable)
  - `Application.streamingAssetsPath/Mods` (ship default mods here)

Three kinds of content are recognized:

1) JSON Action Cards (`*.card.json`)
2) AssetBundles containing `StickerDefinition` assets (`*.bundle`)
3) JSON Afflictions (`*.affliction.json`)

The loader runs at startup before decks initialize, so added cards become part of the action deck pool, and stickers appear in the sticker pack area.

## 1) JSON Action Cards

Drop files named like `MyCard.card.json` into `Mods/`.

Minimum viable (white-box card):
```
{
  "name": "Custom Treatment",
  "value": -1
}
```

Notes:
- With only `name` and `value`, the card appears using the default card prefab and a white material.
- It is selectable and playable, but has no gameplay effect (uses a built-in no-op treatment). Add `treatment` to apply real effects.
- The title shows a [MOD] badge to make mod cards explicit in-game.

Two ways to optionally reference visuals:

Option A — from Resources (simple):
```
{
  "name": "Neem Oil Deluxe",
  "description": "More effective horticultural oil",
  "value": -2,
  "prefabResource": "Prefabs/Cards/ActionCard",
  "materialResource": "Materials/Cards/NeemOil",
  "treatment": "HorticulturalOil"
}
```

Option B — from an AssetBundle in Mods (advanced):
```
{
  "name": "Neem Oil Deluxe",
  "description": "More effective horticultural oil",
  "value": -2,
  "bundleKey": "nature_pack",          // bundle filename without extension, e.g., nature_pack.bundle
  "prefab": "ActionCardPrefab",       // asset name inside the bundle
  "material": "NeemOilMat",           // asset name inside the bundle
  "treatment": "HorticulturalOil"
}
```

Notes:
- `prefabResource` and `materialResource` load from `Resources/` in the shipped build. Omit if you want defaults.
- `value` follows existing convention (negative means a cost to play).
- `treatment` wires the card into built-in treatment logic; leave blank for cosmetic-only cards.
- `cost` is accepted as an alias for `value`.
- `weight` controls how many copies of the card prototype are added to the action deck (default 1).
- `rarity` can also set weight if `weight` isn’t provided: Common≈6, Uncommon≈3, Rare≈2, Epic≈1, Legendary≈1.

### Affliction-specific effectiveness

Modern treatments can target individual afflictions with precise cure values. Supply an `effectiveness`
array that lists each affliction the card should help against:

```
{
  "name": "Broad Spectrum Spray",
  "value": -3,
  "effectiveness": [
    { "affliction": "Aphids", "infectCure": 2, "eggCure": 1 },
    { "affliction": "Thrips", "infectCure": 1 }
  ]
}
```

- When `effectiveness` is present, it takes priority over the legacy `treatment`, `infectCure`, and `eggCure`
  fields.
- Omit `eggCure` when a treatment should only clear adult stages.
- If you skip the array entirely, the loader falls back to the legacy behaviour using the optional
  `treatment`, `infectCure`, and `eggCure` values.

## 2) Sticker AssetBundles

Build an AssetBundle for the target platform containing one or more `StickerDefinition` ScriptableObjects.
Name the bundle file with `.bundle` and place in `Mods/`.

At startup, all `StickerDefinition` assets inside are loaded and added to the sticker pack. If the sticker has a `prefab`, it is instantiated and displayed with the other stickers.

### Building Bundles (summary)
- Create your `StickerDefinition` assets in your Unity project.
- Assign them to an AssetBundle name in the Inspector.
- Use a platform-matching build pipeline to produce the bundle file.
- Copy the resulting `*.bundle` into `Mods/`.

Bundle key: the loader uses the bundle filename (without extension) as `bundleKey`. For a file named `nature_pack.bundle`, use `"bundleKey": "nature_pack"` in JSON.

## 3) JSON Afflictions

Drop files named like `RustBlight.affliction.json` into `Mods/` to add new afflictions. Minimum viable file:

```
{
  "name": "Rust Blight",
  "description": "Orange pustules inch across foliage overnight.",
  "color": "#c36b1f",
  "shader": "Shader Graphs/CustomLit",
  "vulnerableToTreatments": ["Fungicide"]
}
```

- `name` should match whatever you reference from card `effectiveness` entries.
- `color` accepts HTML colour strings (e.g., `#RRGGBB`); defaults to white if omitted.
- `shader` is optional; when supplied the loader calls `Shader.Find` with the provided name.
- `vulnerableToTreatments` keeps legacy cards compatible by listing treatment class name prefixes (without
  the `Treatment` suffix).

Custom afflictions are registered at startup and cloned for each plant, so state is not shared between
instances. Pair them with mod cards that declare matching `effectiveness` entries for best results.

## What’s Not Yet Supported
- Mod-defined plant prefabs or entirely new plant card types.
- Code-level mods (custom C# behavior). If needed later, we can add a safe plugin API or dynamic assembly loading.

## Troubleshooting
- Nothing loads: verify the platform-specific folder is correct and that files use the expected extensions.
- Card not visible: the action deck is randomized; use the Test Runner or logs to confirm registration.
- Sticker visuals missing: ensure your `StickerDefinition.prefab` is assigned and the bundle built successfully.

Version: Preview 1
Last Updated: September 2025

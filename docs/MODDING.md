# Modding Guide (Early Preview)

This build supports a simple `Mods` folder for user content:

- Location (both are scanned):
  - `Application.persistentDataPath/Mods` (user-writable)
  - `Application.streamingAssetsPath/Mods` (ship default mods here)

Two kinds of content are recognized:

1) JSON Action Cards (`*.card.json`)
2) AssetBundles containing `StickerDefinition` assets (`*.bundle`)

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

## What’s Not Yet Supported
- Mod-defined afflictions or plant types.
- Code-level mods (custom C# behavior). If needed later, we can add a safe plugin API or dynamic assembly loading.

## Troubleshooting
- Nothing loads: verify the platform-specific folder is correct and that files use the expected extensions.
- Card not visible: the action deck is randomized; use the Test Runner or logs to confirm registration.
- Sticker visuals missing: ensure your `StickerDefinition.prefab` is assigned and the bundle built successfully.

Version: Preview 1
Last Updated: August 2025

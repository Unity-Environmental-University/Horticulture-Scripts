# Mod Loading System

## Overview
- Location: `_project/Scripts/ModLoading/`
- Purpose: ingest user-provided content (action cards, stickers, afflictions) at runtime and register it with gameplay systems before decks initialize.
- Entry point: `ModLoader.TryLoadMods(CardGameMaster master)` (invoked from `CardGameMaster.Awake`).

## File Inventory
- `ModLoader.cs` — Orchestrates directory scanning, JSON parsing, AssetBundle loading, and registration.
- `RuntimeCard.cs` — Implements a data-driven `ICard` with pluggable visuals and treatment factory delegates.
- `ModAssets.cs` — AssetBundle registry with deterministic unload/reload behaviour.
- `ModAffliction.cs` — Runtime implementation of `PlantAfflictions.IAffliction` backed entirely by JSON.
- `ModAfflictionRegistry.cs` — In-memory catalogue of mod-supplied afflictions with cloning safeguards.
- `Templates/` — Authoring samples for cards and afflictions used by player modders.

## Load Pipeline
1. `TryLoadMods` clears previously cached bundles via `ModAssets.UnloadAll` (supports hot reload in editor).
2. `LoadFromFolder` executes three passes over `Mods/` in both persistent- and streaming-assets roots:
   - **Cards** (`*.card.json`): deserialise `CardJson`, build a `RuntimeCard`, register via `DeckManager.RegisterModActionPrototype`.
   - **Stickers** (`*.bundle`): load `AssetBundle`, store in `ModAssets`, spawn any `StickerDefinition` prefabs through `DeckManager.RegisterModSticker`.
   - **Afflictions** (`*.affliction.json`): build `ModAffliction` instances and cache with `ModAfflictionRegistry`.
3. Deck setup runs later (`DeckManager.InitializeActionDeck`) and instantiates the new prototypes; sticker registration immediately spawns prefabs if the sticker fan is present.

## Runtime Cards
- Constructed either from Resources (`prefabResource`/`materialResource`) or AssetBundles (`bundleKey`, `prefab`, `material`).
- `Weight` controls how many copies enter the prototype deck; derived from JSON `weight` or `rarity`.
- `Treatment` is produced lazily via `_treatmentFactory` to minimise allocation until the card is played.
- Cloning copies stickers and bundle identifiers so duplicates behave consistently.

### Treatment Resolution
- `CreateTreatment(CardJson def)` prefers modern `effectiveness` data:
  - Builds a `ModTreatment` that maps affliction name → `(infectCure, eggCure)`.
  - `ModTreatment.ApplyTreatment` walks the target `PlantController.CurrentAfflictions`, wraps itself in a `CustomTreatmentWrapper`, and delegates to `IAffliction.TreatWith` so core systems remain unchanged.
- When `effectiveness` is absent, `CreateLegacyTreatment` instantiates a built-in treatment (`Fungicide`, `SoapyWater`, etc.) and optionally overrides cure values via `CustomTreatmentWrapper`.
- Debug output respects `CardGameMaster.Instance.debuggingCardClass`.

## Mod Afflictions
- JSON fields map directly to `ModAffliction` constructor arguments (`name`, `description`, `color[]`, `shader`, `vulnerableToTreatments`).
- `ModAffliction.TreatWith` checks both modern (`ModTreatment`) and legacy treatment compatibility, removes itself once larvae/adult stages reach zero, and logs debug output when ineffective.
- Instances hold internal state (adult/larvae flags); always clone before reuse (`ModAfflictionRegistry.GetAffliction`).

## Asset Management
- AssetBundles are keyed by filename without extension; registering a new bundle automatically unloads any previous bundle with the same key to avoid stale references.
- `ModAssets.LoadFromBundle<T>` wraps `LoadAsset<T>` and returns `null` on failure rather than throwing; callers must handle fallback paths (e.g., `RuntimeCard.Prefab` returning the default action-card prefab).
- `ModAssets.UnloadAll` is safe to call multiple times and clears the registry to reduce memory pressure when returning to the title screen.

## Failure Modes & Logging
- JSON parse or asset load exceptions are caught and surfaced as `[ModLoader]` warnings; remaining files continue processing.
- Missing treatments or afflictions degrade gracefully (card loads but has no effect) rather than blocking the entire mod batch.
- Affliction registration overwrites by name; mods should choose unique identifiers to avoid conflicts.

## Extensibility Guidelines
- When adding new mod-capable content types, follow the pattern in `LoadFromFolder`: guard missing directories, catch exceptions, and keep each pass isolated.
- New treatment verbs should extend `CreateLegacyTreatment` (for legacy compatibility) or enhance `ModTreatment`.
- Consider updating `docs/MODDING.md` whenever input schema changes, and add authoring templates to `Templates/`.

## Testing Hooks
- `PlayModeTest/ModTreatmentTest.cs` and `PlayModeTest/CustomCureValueTest.cs` exercise `ModTreatment` behaviour, including fallback paths and per-affliction curing.
- To test card ingestion manually, drop template files into `Application.persistentDataPath/Mods` inside the Editor and watch the console for `[ModLoader]` output.
- Use `ModAfflictionRegistry.Clear()` when writing isolation tests to ensure registry state does not leak between cases.

## Dependencies
- Depends heavily on `DeckManager` for registration and sticker spawning.
- Uses `PlantAfflictions` and `PlantController` from `_project.Scripts.Classes` and `_project.Scripts.Core` to integrate with the treatment pipeline.
- Requires `CardGameMaster` singleton to be initialised before `TryLoadMods` executes.

---
Last Updated: September 2025

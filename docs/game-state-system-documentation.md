# Game State System

## Overview
- Location: `_project/Scripts/GameState/`
- Responsibility: serialise the in-progress run (turn state, decks, plants, retained card) to `PlayerPrefs` and restore it on demand.
- Key entry points: `GameStateManager.SaveGame()` and `GameStateManager.LoadGame()`.

## Data Model (`GameStateData.cs`)
- `GameStateData` aggregates `TurnData`, `ScoreData`, `DeckData`, list of `PlantData`, and optional `RetainedCardData`.
- `DeckData` captures **only action deck/hand/discard** plus player sticker inventory; affliction/plant decks remain for future extension.
- `PlantData` stores the serialised card (`CardData`), its socket index, string-backed affliction/treatment history, and current `moldIntensity`.
- `CardData` serialises by runtime type name, stickers, and nullable value override. `StickerData` keeps the sticker type, display name, and optional value payload.

## Save Flow (`SaveGame`)
1. Fetch singletons: `CardGameMaster.Instance`, its `TurnController`, `ScoreManager`, and `DeckManager`.
2. Populate DTOs:
   - **TurnData** mirrors the turn controller's counters/flags.
   - **ScoreData** pulls current money via `ScoreManager.GetMoneys()`.
   - **DeckData** serialises action deck/hand/discard by cloning to `CardData` and records active stickers.
   - **PlantData** enumerates `DeckManager.plantLocations`, serialises the planted card and associated string lists from each `PlantController` (current/prior afflictions, treatments, used treatments), and snapshots `moldIntensity`.
   - **RetainedCardData** captures the optional retained card slot state.
3. Convert to JSON using `JsonUtility.ToJson` and persist into `PlayerPrefs` under the `GameState` key.

## Load Flow (`LoadGame`)
1. Validate `PlayerPrefs` contains data; guard against empty or oversized payloads (>1 MB) before parsing.
2. Deserialise `GameStateData` with `JsonUtility.FromJson` inside a try/catch to surface malformed saves.
3. Verify `CardGameMaster.Instance`, `TurnController`, and `DeckManager` all exist before mutating state.
4. Rehydrate components:
   - Turn/score fields are copied directly when DTOs are non-null.
   - Decks are restored via `DeckManager.RestoreActionDeck / RestoreDiscardPile / RestoreActionHand` and the display refreshed.
   - Stickers are passed back through `DeckManager.RestorePlayerStickers`.
   - `SuppressQueuedEffects` is set, then `RestorePlantsSequentially` coroutine is launched to rebuild plant slots without triggering queued effects.
   - Retained-card visuals are recreated via `RetainedCardHolder.RestoreCardVisual` if the slot existed in the save.
5. After plant reconstruction the coroutine clears the effect queue, re-enables shader updates, and forces a one-frame delay so visuals settle.

## Serialization Helpers
- `SerializeCard(ICard)` and `SerializeSticker(ISticker)` produce DTOs using runtime type names; value overrides only populate when nullable `Value` has data.
- `DeserializeCard(CardData)` resolves the type by name across all loaded assemblies, instantiates it, reapplies value overrides, and replays stickers using `DeserializeSticker`.
- `DeserializeSticker(StickerData)` handles both ScriptableObject-based and POCO stickers, defaulting to cloning existing definitions when available.
- All helper methods throw or log descriptive errors when resolution fails so corrupted saves do not crash the game loop unexpectedly.

## Plant Restore Sequence
- `DeckManager.RestorePlantsSequentially` (invoked via coroutine) reconstructs plant GameObjects at each socket:
  - Instantiates the prefab based on the serialised `ICard`.
  - Replays afflictions/treatments via the string lists, leveraging the core `PlantAfflictions` factory helpers.
  - Restores historical collections (`PriorAfflictions`, `UsedTreatments`) without triggering new effects.
  - Sets `moldIntensity`, updates price flags, and yields between spawns for pacing.
- After all plants spawn, `TurnController.ClearEffectQueue` runs and each `PlantController.FlagShadersUpdate()` ensures the renderer pipeline reflects current status.

## Failure Handling
- Each stage logs warnings instead of aborting wholesale (e.g., missing decks or turn data). Only deserialisation or singleton lookups will hard-abort the load.
- Unknown cards or stickers raise explicit exceptions/warnings. Consider extending the resolver if future content uses namespaces or assembly-qualified names.
- `SuppressQueuedEffects` prevents visual/audio queues from replaying during load, reducing flicker or duplicate sounds.

## Extensibility & Guidance
- Add new save fields by augmenting the DTO classes; `JsonUtility` handles missing fields during load by using default values, which preserves backward compatibility.
- Maintain DTO types with only serialisable fields (no behaviours) so upgrades remain painless.
- When introducing new gameplay systems, prefer serialising identifiers rather than full objects and reconstruct behaviour via existing managers.
- Update this documentation and the save/load tests whenever the schema changes.

## Testing & Validation
- Play Mode tests: `PlayModeTest/CardSerializationTest.cs` covers card round-trip serialisation for both mutable and read-only cards, value overrides, and sticker application edge cases.
- Manual workflow: capture a save mid-run, alter the deck/plant composition, and ensure load faithfully reconstructs state without leaking references or duplicating afflictions.
- Regression check: verify loading older saves after DTO changes; missing fields should default gracefully (e.g., the recently removed `PlantData.plantType`).

---
Last Updated: September 2025

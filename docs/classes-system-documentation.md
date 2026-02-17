# Classes System Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Core Components](#core-components)
- [API Reference](#api-reference)
- [Integration Patterns](#integration-patterns)
- [Usage Examples](#usage-examples)
- [Dependencies](#dependencies)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Overview

Classes contain the domain model for cards, plant afflictions/treatments, shop items, and simple effect requests. These types back the card game loop and integrate with Core, Card Core, and UI.

Location: `Assets/_project/Scripts/Classes/`

## Architecture

```
CardClasses
├── ICard (base contract)
├── FoilCard (decorator — adds holographic shimmer to any ICard)
├── IPlantCard (plant-specific contract)
├── IAfflictionCard (affliction-specific contract)
├── ILocationCard (location-specific contract)
├── CardHand (deck/hand management)
├── Plant Cards (ColeusCard, ChrysanthemumCard, PepperCard, CucumberCard)
├── Affliction Cards (AphidsCard, MealyBugsCard, ThripsCard, MildewCard, SpiderMitesCard, FungusGnatsCard)
├── Treatment Cards (HorticulturalOilBasic, InsecticideBasic, FungicideBasic, SoapyWaterBasic, SpinosadTreatment, ImidaclopridTreatment, Panacea)
└── Location Cards (UreaBasic)

PlantAfflictions : MonoBehaviour
├── IAffliction (contract) + implementations
└── ITreatment (contract) + implementations

ShopClasses
└── IShopItem, CardShopItem

PlantEffectClasses
└── PlantEffectRequest DTO
```

## Core Components

### ICard and Card Types (CardClasses.cs)
- ICard: Base contract with `Name`, optional `Description`, nullable `Value`, optional `Affliction`/`Treatment`, `Prefab`, `Material`, `List<ISticker> Stickers`, `bool IsFoil`; `Clone()`, `Selected()`, `ApplySticker()`, `ModifyValue(int)`. All members except `Name`, `Stickers`, and `Clone()` have default interface implementations, so implementing types only need to override what they use.
- FoilCard: Decorator that wraps any `ICard` and returns `true` for `IsFoil`. All other members delegate to the inner card. Use `DeckManager.ApplyFoilToCard(target)` to wrap an existing card in-place across all decks rather than constructing `FoilCard` directly.
- IPlantCard: Extends ICard for plant-specific cards with `InfectLevel`, `EggLevel`, and a `PlantCardCategory` so cards can report if they are Fruiting, Decorative, or another class.
- IAfflictionCard: Extends ICard for affliction cards with `BaseInfectLevel` and `BaseEggLevel` properties.
- ILocationCard: Extends ICard for location effects with `EffectDuration`, `IsPermanent`, `EffectType`, and location effect methods.
- CardHand: Manages `Deck` and `PrototypeDeck`; `DrawCards(int)`, `DeckRandomDraw()` duplicates each prototype 1–4x.
- Cards: Plant cards, affliction cards, treatment cards, and location cards map to gameplay entities and UI materials.

### Afflictions & Treatments (PlantAfflictions.cs)
- IAffliction: `Name`, `Description`, `Color`, optional `Shader`, `TreatWith(ITreatment, PlantController)`, `TickDay(PlantController)`, `GetCard()`, `Clone()`.
- Implementations reduce plant value each day; some maintain internal state (e.g., Thrips larvae/adults).
- ITreatment: `Name`, `Description`; default `ApplyTreatment(PlantController)` iterates current afflictions and calls `TreatWith`.

### Shop Items (ShopClasses.cs)
- IShopItem: `Card`, `DisplayName`, `Cost`, `Purchase()`.
- CardShopItem: clones card into deck, subtracts currency, removes shop UI entry.

### Effects DTO (PlantEffectClasses.cs)
- PlantEffectRequest: aggregates `PlantController`, `ParticleSystem`, `AudioClip`, and `Delay` for queued effects.

## API Reference

### CardHand
```csharp
public void DrawCards(int number)
public void DeckRandomDraw()
```

### IAffliction
```csharp
void TreatWith(ITreatment treatment, PlantController plant)
void TickDay(PlantController plant)
IAffliction Clone()
ICard GetCard()
```

### ITreatment
```csharp
void ApplyTreatment(PlantController plant) // default implementation
```

### ICard.IsFoil
`bool IsFoil` defaults to `false` on the interface. `FoilCard` returns `true`. `CardView.Setup` reads this property to create or toggle the holographic overlay quad; no additional wiring is needed when cards are displayed through the normal pipeline.

### FoilCard
```csharp
// Wrap an existing card to make it foil
var foil = new FoilCard(existingCard);

// Apply foil across all decks via DeckManager (preferred)
deckManager.ApplyFoilToCard(target);

// Unwrap to access the underlying card
ICard inner = ((FoilCard)foilCard).Inner;
```

## Integration Patterns

- Card Core: action cards expose `Treatment` that `TurnController` can apply to the selected plant via `ApplyTreatment`.
- Core: afflictions call `plant.UpdatePriceFlag(newValue)` after `TickDay` to keep UI/state in sync.
- Resources: action cards bind `Material` via `Resources.Load<Material>("Materials/Cards/<Name>")` for card rendering.
- Shop: `CardShopItem.Purchase()` updates deck (`DeckManager.AddActionCard`), currency (`ScoreManager.SubtractMoneys`), and UI (`ShopManager.RemoveShopItem`).
- Foil rendering: `CardView.SetFoilOverlay` creates a child quad using the material at `Resources/Materials/Cards/FoilOverlay` (using the `Custom/FoilCard` URP shader). The quad is cached as a child named `FoilOverlay` and toggled on subsequent calls.

## Usage Examples

```csharp
// Build a randomized starter hand
var prototypes = new List<ICard> { new ColeusCard(), new SoapyWaterBasic(), new AphidsCard() };
var hand = new CardHand("Starter", new List<ICard>(), prototypes);
hand.DeckRandomDraw();
hand.DrawCards(5);

// Apply a treatment to a plant
var treatment = new PlantAfflictions.SoapyWaterTreatment();
treatment.ApplyTreatment(plantController);

// Purchase a shop item
var item = new CardShopItem(new FungicideBasic(), deckManager, shopEntryGO);
item.Purchase();

// Make a card foil (preferred: use DeckManager so all deck lists stay consistent)
deckManager.ApplyFoilToCard(selectedCard);
```

## Dependencies

- Namespaces: `_project.Scripts.Card_Core`, `_project.Scripts.Core`, `_project.Scripts.Stickers`.
- Unity: `Resources`, `Shader`, `Material`, `GameObject`.

## Testing

- Framework: Unity Test Framework (NUnit), Play Mode tests in `PlayModeTest/`.
- Behavior-first: assert via public API; use reflection for private members when necessary.
```csharp
// Example: seed a private field for setup
var f = typeof(ColeusCard).GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
f.SetValue(card, 7);
```
- Centralize helpers (e.g., `ReflectionUtil`) to reduce brittle `BindingFlags` usage.

## Troubleshooting

- Action card seems ineffective: ensure `Treatment.Name` matches `TreatWith` switch/conditions and plant actually has matching afflictions.
- Afflictions not reducing value: confirm `TickDay` is invoked and `PlantController.PlantCard.Value` is nullable-int with a value.
- Resources material missing: verify `Materials/Cards/<Name>` exists and is included in build.
- Foil overlay not appearing: ensure `Resources/Materials/Cards/FoilOverlay` exists and uses the `Custom/FoilCard` shader. `CardView` logs a warning if the material is missing.

---

Version: 1.2
Last Updated: 2026-02-17
Namespaces: `_project.Scripts.Classes`

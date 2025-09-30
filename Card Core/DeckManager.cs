using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using _project.Scripts.ModLoading;
using _project.Scripts.Stickers;
using DG.Tweening;
using Unity.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Card_Core
{
    public class DeckManager : MonoBehaviour
    {
        #region Stickers

        private void InitializeStickerDeck()
        {
            if (stickerDefinitions == null || stickerPackParent == null) return;
            foreach (var def in stickerDefinitions)
            {
                _playerStickers.Add(def);
                var go = Instantiate(def.Prefab,
                    stickerPackParent.position,
                    stickerPackParent.rotation,
                    stickerPackParent);
                var view = go.AddComponent<StickerView>();
                view.definition = def;
            }
            
            ArrangeStickersInFan();
        }

        /// <summary>
        /// Sets up stickers in a simple stacked layout.
        /// For single stickers, keep them centered.
        /// For multiple stickers, stack them like cards.
        /// </summary>
        private void ArrangeStickersInFan()
        {
            if (stickerPackParent == null) return;
            
            var stickerViews = stickerPackParent.GetComponentsInChildren<StickerView>();
            var stickerCount = stickerViews.Length;
            
            if (stickerCount <= 1) 
            {
                if (stickerCount != 1) return;
                stickerViews[0].transform.localPosition = Vector3.zero;
                stickerViews[0].transform.localRotation = Quaternion.identity;
                return;
            }
            
            SetupStickerStack(stickerViews);
        }
        
        /// <summary>
        /// Positions stickers with consistent spacing regardless of quantity
        /// </summary>
        private static void SetupStickerStack(StickerView[] stickerViews)
        {
            var stickerCount = stickerViews.Length;
            if (stickerCount == 0) return;
            
            const float arcRadius = 0.4f;
            const float anglePerSticker = 15f;
            const float stickerSpacing = 0.08f;
            
            for (var i = 0; i < stickerCount; i++)
            {
                var stickerView = stickerViews[i];
                
                var totalSpan = (stickerCount - 1) * anglePerSticker;
                var currentAngle = i * anglePerSticker - totalSpan / 2f;
                var angleInRadians = currentAngle * Mathf.Deg2Rad;
                
                var xOffset = Mathf.Sin(angleInRadians) * arcRadius;
                var yOffset = (1f - Mathf.Cos(angleInRadians)) * arcRadius;
                yOffset += i * stickerSpacing;
                
                var stackOffset = new Vector3(xOffset, yOffset, -i * 0.01f);
                var stackRotation = Quaternion.Euler(0, 0, currentAngle * 0.5f);
                
                stickerView.transform.localPosition = stackOffset;
                stickerView.transform.localRotation = stackRotation;
            }
        }

        #endregion

        #region Constants

        private const float MinMoldIntensity = 0.8f;
        private const float MaxMoldIntensity = 1.0f;

        #endregion

        #region Prototype Decks

        private static readonly List<ICard> PrototypeAfflictionsDeck = new()
        {
            new AphidsCard(),
            new MealyBugsCard(),
            new ThripsCard(),
            new MildewCard()
        };

        private static readonly List<ICard> PrototypePlantsDeck = new()
        {
            new ColeusCard(),
            new ChrysanthemumCard(),
            new CucumberCard(),
            new PepperCard()
        };

        private static readonly List<ICard> PrototypeActionDeck = new()
        {
            new HorticulturalOilBasic(),
            new HorticulturalOilBasic(),
            new FungicideBasic(),
            new FungicideBasic(),
            new InsecticideBasic(),
            new InsecticideBasic(),
            new SoapyWaterBasic(),
            new SoapyWaterBasic(),
            new Panacea(),
            new Panacea(),
            new FertilizerBasic(),
            new FertilizerBasic()
        };

        private readonly List<ICard> _tutorialActionDeck = new()
        {
            new HorticulturalOilBasic(),
            new FungicideBasic(),
            new InsecticideBasic(),
            new SoapyWaterBasic(),
            new Panacea(),

            new HorticulturalOilBasic(),
            new FungicideBasic(),
            new InsecticideBasic(),
            new SoapyWaterBasic(),
            new Panacea()
        };

        private readonly List<ICard> _tutorialPlantDeck = new();

        private readonly List<ICard> _tutorialAfflictionDeck = new();

        #endregion

        #region Declare Decks

        private readonly List<ICard> _actionDeck = new();
        private readonly List<ICard> _actionDiscardPile = new();
        private readonly List<ISticker> _playerStickers = new();
        private static readonly List<ICard> PlantDeck = new();
        private static readonly List<ICard> AfflictionsDeck = new();

        #endregion

        #region Class Variables

        [DontSerialize] public bool updatingActionDisplay;

        // DOTween sequence management for memory leak prevention
        private Sequence _currentHandSequence;
        private Sequence _currentDisplaySequence;

        private readonly CardHand _afflictionHand = new("Afflictions Hand", AfflictionsDeck, PrototypeAfflictionsDeck);
        private readonly CardHand _plantHand = new("Plants Hand", PlantDeck, PrototypePlantsDeck);
        private readonly List<ICard> _actionHand = new();

        public List<Transform> plantLocations;
        public Transform actionCardParent;
        public Transform stickerPackParent;

        [Tooltip("Author your sticker assets here")]
        public List<StickerDefinition> stickerDefinitions;

        /// <summary>
        ///     Currently selected sticker (click to apply on the next card click).
        /// </summary>
        public StickerView SelectedSticker { get; private set; }

        public Click3D selectedACardClick3D;
        public ICard selectedACard;

        public event Action<ICard> SelectedCardChanged;

        public ICard SelectedCard => selectedACard;
        public Click3D SelectedCardClick3D => selectedACardClick3D;

        public void SetSelectedCard(Click3D source, ICard card, bool notify = true)
        {
            selectedACardClick3D = source;
            selectedACard = card;
            if (notify) SelectedCardChanged?.Invoke(card);
        }

        public void ClearSelectedCard(bool notify = true)
        {
            var hadSelection = selectedACardClick3D != null || selectedACard != null;
            selectedACardClick3D = null;
            selectedACard = null;
            if (notify && hadSelection) SelectedCardChanged?.Invoke(null);
        }
        public GameObject coleusPrefab;
        public GameObject chrysanthemumPrefab;
        public GameObject cucumberPrefab;
        public GameObject pepperPrefab;
        public float cardSpacing = 1f;
        public int cardsDrawnPerTurn = 4;
        public int redrawCost = 3;
        public bool debug = true;

        #endregion

        #region Initialization

        private void Start()
        {
            InitializeActionDeck();
            InitializeStickerDeck();
            _plantHand.DeckRandomDraw();
            _afflictionHand.DeckRandomDraw();
            if (debug) Debug.Log("Initial Deck Order: " + string.Join(", ", PlantDeck.ConvertAll(card => card.Name)));
            if (debug)
                Debug.Log("Initial Deck Order: " + string.Join(", ", AfflictionsDeck.ConvertAll(card => card.Name)));
        }

        /// Initializes the action deck by populating it with clones of prototype action cards.
        /// For each card in the prototype action deck, creates a randomized number (between 1 and 4) of duplicates
        /// and adds them to the action deck. The action deck is then shuffled to randomize the order.
        /// Optionally logs the initialized deck order if debugging is enabled.
        private void InitializeActionDeck()
        {
            foreach (var prototype in PrototypeActionDeck)
            {
                var copies = 1;
                if (prototype is RuntimeCard rc)
                    copies = Mathf.Clamp(rc.Weight, 1, 50);

                for (var i = 0; i < copies; i++)
                    _actionDeck.Add(prototype.Clone());
            }

            ShuffleDeck(_actionDeck);
            if (debug)
                Debug.Log("Initialized ActionDeck Order: " +
                          string.Join(", ", _actionDeck.ConvertAll(card => card.Name)));
        }

        /// <summary>
        /// Register a new action card prototype from a mod before deck initialization.
        /// </summary>
        /// <param name="prototype">Card to add to the action prototype pool</param>
        public void RegisterModActionPrototype(ICard prototype)
        {
            if (prototype == null) return;
            PrototypeActionDeck.Add(prototype);
            if (debug) Debug.Log($"[Mods] Added action prototype: {prototype.Name}");
        }

        /// <summary>
        /// Register a StickerDefinition from a mod and spawn it in the sticker pack area if present.
        /// </summary>
        public void RegisterModSticker(StickerDefinition def)
        {
            if (def == null) return;
            stickerDefinitions ??= new List<StickerDefinition>();
            stickerDefinitions.Add(def);

            if (stickerPackParent == null || !def.Prefab) return;
            var go = Instantiate(def.Prefab, stickerPackParent.position, stickerPackParent.rotation, stickerPackParent);
            var view = go.GetComponent<StickerView>() ?? go.AddComponent<StickerView>();
            view.definition = def;
            ArrangeStickersInFan();
        }

        #endregion

        #region Deck Utilities

        /// Shuffles the specified deck of cards into a random order.
        /// This method uses the Fisher-Yates shuffle algorithm to rearrange the elements
        /// in the provided list randomly. The shuffle is done in place, modifying the
        /// original list.
        /// <param name="deck">
        ///     A list of cards implementing the ICard interface to be shuffled.
        /// </param>
        private void ShuffleDeck(List<ICard> deck)
        {
            var n = deck.Count;
            while (n > 1)
            {
                var k = Random.Range(0, n--);
                (deck[n], deck[k]) = (deck[k], deck[n]);
            }

            if (debug)
                Debug.Log("Shuffled deck order: " + string.Join(", ", deck.ConvertAll(card => card.Name)));
        }

        /// <summary>
        ///     Generates a weighted random integer within a specified range.
        ///     The weight increases with the round number, favoring higher numbers towards later rounds.
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random range.</param>
        /// <param name="maxExclusive">The exclusive upper bound of the random range.</param>
        /// <returns>A weighted random integer within the specified range.</returns>
        private static int RoundWeightedRandom(int min, int maxExclusive)
        {
            // 0 at round 1, 1 at round 7+
            var round01 = Mathf.Clamp01((CardGameMaster.Instance.turnController.currentRound - 1f) / 4f);

            // Get a random t in 0-1 and build two *opposite* biases ----
            var r = Random.value;

            // lowBias  →   r²   (strongly favours 0)
            // highBias → 1-(1-r)²  (strongly favours 1)
            var lowBias = r * r;
            var highBias = 1f - (1f - r) * (1f - r);

            // Blend between those two curves based on how far into the game we are ----
            // Exaggerate round01 to make early rounds more biased, and late rounds scale up faster.
            var blended = Mathf.Lerp(lowBias, highBias, round01 * 2f);

            // Convert to an integer in [min, maxExclusive) ----
            var result = min + Mathf.FloorToInt(blended * (maxExclusive - min));

            // Safety clamp (handles edge-cases where maxExclusive == min + 1)
            return Mathf.Clamp(result, min, maxExclusive - 1);
        }

        public List<ICard> GetActionDeck() => new(_actionDeck);
        public List<ICard> GetDiscardPile() => new(_actionDiscardPile);
        public List<ICard> GetActionHand() => new(_actionHand);
        public List<ISticker> GetPlayerStickers() => new(_playerStickers);

        public void RestoreActionDeck(List<CardData> cards)
        {
            _actionDeck.Clear();
            foreach (var card in cards)
                // Reconstruct each card from serialized data
                _actionDeck.Add(GameStateManager.DeserializeCard(card));
        }

        public void RestoreDiscardPile(List<CardData> cards)
        {
            _actionDiscardPile.Clear();
            foreach (var card in cards) _actionDiscardPile.Add(GameStateManager.DeserializeCard(card));
        }

        public void RestoreActionHand(List<CardData> cards)
        {
            _actionHand.Clear();
            foreach (var card in cards) _actionHand.Add(GameStateManager.DeserializeCard(card));
        }

        public void RestorePlayerStickers(List<StickerData> stickers)
        {
            // Clear existing sticker visuals
            if (stickerPackParent != null)
                foreach (Transform child in stickerPackParent)
                    Destroy(child.gameObject);

            _playerStickers.Clear();
            foreach (var sticker in stickers.Select(GameStateManager.DeserializeSticker)
                         .Where(sticker => sticker != null))
            {
                _playerStickers.Add(sticker);

                // Recreate visual representation
                if (!sticker.Prefab || !stickerPackParent) continue;
                var go = Instantiate(sticker.Prefab,
                    stickerPackParent.position,
                    stickerPackParent.rotation,
                    stickerPackParent);
                var view = go.GetComponent<StickerView>() ?? go.AddComponent<StickerView>();
                if (sticker is StickerDefinition definition)
                    view.definition = definition;
            }
            
            // Arrange restored stickers in a fan layout
            ArrangeStickersInFan();
        }

        #endregion

        #region Sticker Drag & Drop

        /// <summary>
        ///     Selects a sticker so it can be applied to the next clicked card.
        /// </summary>
        public void SelectSticker(StickerView sticker)
        {
            // Toggle off if clicking the already selected sticker
            if (SelectedSticker == sticker)
            {
                sticker.GetComponent<Click3D>().selected = false;
                SelectedSticker = null;
                return;
            }

            // un-highlight previous selection if any
            if (SelectedSticker != null)
                SelectedSticker.GetComponent<Click3D>().selected = false;

            SelectedSticker = sticker;
            // highlight a new selection
            sticker.GetComponent<Click3D>().selected = true;
        }

        /// <summary>
        ///     Applies the selected sticker to the given card if it matches, then clears selection.
        /// </summary>
        public void TryDropStickerOn(ICard card, StickerView sticker)
        {
            if (SelectedSticker != sticker) return;
            sticker.definition.Apply(card);
            // remove the sticker from the tray
            Destroy(sticker.gameObject);
            SelectedSticker = null;
        }

        #endregion

        #region Plant Management

        /// Places plants from the PlantDeck into designated locations on the game board.
        /// This method first clears all existing plants and then randomly draws a specified number of cards from the PlantDeck.
        /// The drawn cards are then placed sequentially in the available plant locations, with a delay between each placement.
        /// If there are no plant locations available or if the PlantDeck is empty, this method does nothing.
        public IEnumerator PlacePlants()
        {
            if (plantLocations == null || plantLocations.Count == 0) yield break;

            ClearAllPlants();
            _plantHand.DeckRandomDraw();
            ShuffleDeck(_plantHand.Deck);
            _plantHand.Clear();

            var max = Mathf.Min(plantLocations.Count, PlantDeck.Count);
            var cardsToDraw = RoundWeightedRandom(1, max + 1);
            _plantHand.DrawCards(cardsToDraw);

            yield return StartCoroutine(PlacePlantsSequentially());
        }

        private IEnumerator PlacePlantsSequentially(float delay = 0.3f)
        {
            //delay = CardGameMaster.Instance.soundSystem.plantSpawn.length;
            for (var i = 0; i < _plantHand.Count && i < plantLocations.Count; i++)
            {
                var prefab = GetPrefabForCard(_plantHand[i]);
                if (!prefab) continue;

                var plantLocation = plantLocations[i];

                // Play the sound before placing
                var clip = CardGameMaster.Instance.soundSystem.plantSpawn;
                if (clip) AudioSource.PlayClipAtPoint(clip, plantLocation.position);

                // Instantiate and assign
                var plant = Instantiate(prefab, plantLocation.position, plantLocation.rotation);
                plant.transform.SetParent(plantLocation);

                var plantController = plant.GetComponent<PlantController>();
                plantController.PlantCard = _plantHand[i];

                if (plantController.priceFlag && plantController.priceFlagText)
                    plantController.priceFlagText!.text = "$" + plantController.PlantCard.Value;

                // Notify SpotDataHolder that a plant was added
                var spotDataHolder = plantLocation.GetComponentInChildren<SpotDataHolder>();
                if (spotDataHolder != null)
                {
                    spotDataHolder.InvalidatePlantCache();
                    spotDataHolder.RefreshAssociatedPlant();
                }

                yield return new WaitForSeconds(delay);
            }

            StartCoroutine(UpdateCardHolderRenders());
            CardGameMaster.Instance.scoreManager.CalculatePotentialProfit();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public IEnumerator UpdateCardHolderRenders()
        {
            yield return null;

            foreach (var location in plantLocations)
            {
                var plantController = location.GetComponentInChildren<PlantController>(true);
                var cardHolders = location.GetComponentsInChildren<PlacedCardHolder>(true);
                
                foreach (var cardHolder in cardHolders)
                    if (cardHolder && !cardHolder.HoldingCard)
                        cardHolder.ToggleCardHolder(plantController != null);
            }
        }

        /// Clears all plant objects from their designated locations by destroying all
        /// associated GameObjects with `PlantController` components at each location.
        /// Ensures the randomness of later plant arrangements by invoking a
        /// random draw on the plant deck. Outputs debug messages if enabled.
        public void ClearAllPlants()
        {
            foreach (var child in plantLocations
                         .Select(slot => slot.GetComponentsInChildren<PlantController>(true))
                         .SelectMany(children => children)) Destroy(child.gameObject);

            // Notify SpotDataHolders that plants were removed
            foreach (var spotDataHolder in plantLocations
                         .Select(location => location.GetComponentInChildren<SpotDataHolder>())
                         .Where(holder => holder != null))
            {
                spotDataHolder.InvalidatePlantCache();
                spotDataHolder.RefreshAssociatedPlant();
            }

            // hide all cardholders
            foreach (var holder in plantLocations
                         .Select(location => location.GetComponentsInChildren<PlacedCardHolder>(true))
                         .SelectMany(cardHolders => cardHolders)) holder.ToggleCardHolder(false);

            _plantHand.DeckRandomDraw();

            if (debug) Debug.Log("All plants cleared");
        }

        public IEnumerator ClearPlant(PlantController plant)
        {
            if (!plant) yield break;

            plant.deathFX.Play();
            yield return new WaitForSeconds(plant.deathFX.main.duration + 0.5f);
            
            var location = plantLocations.FirstOrDefault(slot =>
                slot.GetComponentsInChildren<PlantController>(true).Contains(plant));

            Destroy(plant.gameObject);

            if (!location) yield break;

            // Notify SpotDataHolder that the plant was removed
            var spotDataHolder = location.GetComponentInChildren<SpotDataHolder>();
            if (spotDataHolder != null)
            {
                spotDataHolder.InvalidatePlantCache();
                spotDataHolder.RefreshAssociatedPlant();
            }

            var cardHolders = location.GetComponentsInChildren<PlacedCardHolder>(true);
            foreach (var holder in cardHolders)
                holder.ToggleCardHolder(false);
        }

        /// Determines the appropriate prefab GameObject to instantiate for a given card.
        /// This method maps specific card types (e.g., ColeusCard, ChrysanthemumCard) to their
        /// corresponding prefab GameObjects used to represent them visually within the game.
        /// If the card type does not have a defined prefab, it returns null.
        /// <param name="card">The card of type ICard for which the prefab needs to be retrieved.</param>
        /// <returns>The GameObject prefab corresponding to the provided card, or null if none is defined.</returns>
        private GameObject GetPrefabForCard(ICard card)
        {
            return card switch
            {
                ColeusCard => coleusPrefab,
                ChrysanthemumCard => chrysanthemumPrefab,
                CucumberCard => cucumberPrefab,
                PepperCard => pepperPrefab,
                _ => null
            };
        }

        #endregion

        #region Tutorial

        public IEnumerator PlaceTutorialPlants()
        {
            if (plantLocations == null || plantLocations.Count == 0) yield break;
            var cgm = CardGameMaster.Instance;

            ClearAllPlants();
            Debug.Log("tut turn" + cgm.turnController.currentTutorialTurn);
            switch (cgm.turnController.currentTutorialTurn)
            {
                case 0:
                    _tutorialPlantDeck.Clear();
                    _tutorialAfflictionDeck.Clear();

                    _tutorialPlantDeck.Add(new ColeusCard());

                    _tutorialAfflictionDeck.Add(new AphidsCard());
                    break;
                case 1:
                    _tutorialPlantDeck.Clear();
                    _tutorialAfflictionDeck.Clear();

                    _tutorialPlantDeck.Add(new ColeusCard());
                    _tutorialPlantDeck.Add(new ChrysanthemumCard());

                    _tutorialAfflictionDeck.Add(new AphidsCard());
                    break;
                case 2:
                    _tutorialPlantDeck.Clear();
                    _tutorialAfflictionDeck.Clear();

                    _tutorialPlantDeck.Add(new ColeusCard());
                    _tutorialPlantDeck.Add(new ChrysanthemumCard());
                    _tutorialPlantDeck.Add(new CucumberCard());

                    _tutorialAfflictionDeck.Add(new AphidsCard());
                    _tutorialAfflictionDeck.Add(new MealyBugsCard());
                    break;
                case 3:
                    _tutorialPlantDeck.Clear();
                    _tutorialAfflictionDeck.Clear();

                    _tutorialPlantDeck.Add(new ColeusCard());
                    _tutorialPlantDeck.Add(new ChrysanthemumCard());
                    _tutorialPlantDeck.Add(new CucumberCard());
                    _tutorialPlantDeck.Add(new PepperCard());

                    _tutorialAfflictionDeck.Add(new AphidsCard());
                    _tutorialAfflictionDeck.Add(new MealyBugsCard());
                    _tutorialAfflictionDeck.Add(new MildewCard());
                    break;
                case 4:
                    _tutorialAfflictionDeck.Clear();

                    _tutorialAfflictionDeck.Add(new AphidsCard());
                    _tutorialAfflictionDeck.Add(new MealyBugsCard());
                    _tutorialAfflictionDeck.Add(new MildewCard());
                    _tutorialAfflictionDeck.Add(new ThripsCard());
                    break;
            }

            _plantHand.Clear();
            foreach (var card in _tutorialPlantDeck)
                _plantHand.Add(card.Clone());

            Debug.Log("PlantHand: " + string.Join(", ", _plantHand.ConvertAll(card => card.Name)));
            Debug.Log("TUT PLANT HAND: " + string.Join(", ", _tutorialPlantDeck.ConvertAll(card => card.Name)));
            yield return StartCoroutine(PlacePlantsSequentially());
        }

        /// <summary>
        ///     Restores and places plants from saved game state sequentially, preserving transforms and afflictions.
        /// </summary>
        public IEnumerator RestorePlantsSequentially(List<PlantData> plantDataList, float delay = 0.4f)
        {
            if (plantDataList == null || plantDataList.Count == 0) yield break;

            _plantHand.Clear();
            ClearAllPlants();
            foreach (var pd in plantDataList.OrderBy(pd => pd.locationIndex))
            {
                // Reconstruct card and prefab
                var cardProto = GameStateManager.DeserializeCard(pd.plantCard);
                var prefab = GetPrefabForCard(cardProto);
                if (!prefab) continue;

                var location = plantLocations[pd.locationIndex];
                // Play spawn sound
                var clip = CardGameMaster.Instance.soundSystem.plantSpawn;
                if (clip) AudioSource.PlayClipAtPoint(clip, location.position);

                // Instantiate and set parent
                var plantObj = Instantiate(prefab, location.position, location.rotation);
                plantObj.transform.SetParent(location);

                // Restore plant controller state
                var plant = plantObj.GetComponent<PlantController>();
                plant.PlantCard = cardProto;
                if (plant.priceFlag && plant.priceFlagText)
                    plant.priceFlagText.text = "$" + plant.PlantCard.Value;

                // Restore history without queueing or duplicating afflictions
                plant.PriorAfflictions.Clear();
                if (pd.priorAfflictions != null)
                    foreach (var aff in pd.priorAfflictions.Select(GetAfflictionFromString).Where(aff => aff != null))
                        plant.PriorAfflictions.Add(aff);

                // Restore current afflictions (effects suppressed during the load process)
                plant.CurrentAfflictions.Clear();
                if (pd.currentAfflictions != null)
                    foreach (var aff in pd.currentAfflictions.Select(GetAfflictionFromString).Where(aff => aff != null))
                        plant.AddAffliction(aff);

                if (pd.usedTreatments != null)
                    foreach (var tr in pd.usedTreatments)
                        plant.UsedTreatments.Add(GetTreatmentFromString(tr));
                if (pd.currentTreatments != null)
                    foreach (var tr in pd.currentTreatments)
                        plant.CurrentTreatments.Add(GetTreatmentFromString(tr));
                plant.SetMoldIntensity(pd.moldIntensity);

                yield return new WaitForSeconds(delay);
            }

            StartCoroutine(UpdateCardHolderRenders());
            CardGameMaster.Instance.scoreManager.CalculatePotentialProfit();
        }

        private static PlantAfflictions.IAffliction GetAfflictionFromString(string afSting)
        {
            return afSting switch
            {
                "Aphids" => new PlantAfflictions.AphidsAffliction(),
                "MealyBugs" => new PlantAfflictions.MealyBugsAffliction(),
                "Mildew" => new PlantAfflictions.MildewAffliction(),
                "Thrips" => new PlantAfflictions.ThripsAffliction(),
                "Spider Mites" => new PlantAfflictions.SpiderMitesAffliction(),
                "Fungus Gnats" => new PlantAfflictions.FungusGnatsAffliction(),
                _ => null
            };
        }

        private static PlantAfflictions.ITreatment GetTreatmentFromString(string trSting)
        {
            return trSting switch
            {
                "Horticultural Oil" => new PlantAfflictions.HorticulturalOilTreatment(),
                "Fungicide" => new PlantAfflictions.FungicideTreatment(),
                "Insecticide" => new PlantAfflictions.InsecticideTreatment(),
                "SoapyWater" => new PlantAfflictions.SoapyWaterTreatment(),
                "Spinosad" => new PlantAfflictions.SpinosadTreatment(),
                "Imidacloprid" => new PlantAfflictions.ImidaclopridTreatment(),
                "Panacea" => new PlantAfflictions.Panacea(),
                _ => null
            };
        }

        public void DrawTutorialAfflictions()
        {
            _afflictionHand.Clear();
            foreach (var card in _tutorialAfflictionDeck)
                _afflictionHand.Add(card.Clone());

            if (debug)
                Debug.Log("Afflictions Drawn: " + string.Join(", ", _afflictionHand.ConvertAll(card => card.Name)));
            ApplyAfflictionDeck();
            CardGameMaster.Instance.scoreManager.CalculateTreatmentCost();
        }

        public void DrawTutorialActionHand()
        {
            if (updatingActionDisplay) return;

            _actionHand.Clear();

            for (var i = 0; i < cardsDrawnPerTurn; i++)
                _actionHand.Add(_tutorialActionDeck[i % _tutorialActionDeck.Count].Clone());

            // Clear all existing visualized cards
            ClearActionCardVisuals();

            DisplayActionCardsSequence();

            if (debug)
                Debug.Log($"Tutorial Action Hand: {string.Join(", ", _actionHand.ConvertAll(card => card.Name))}");
        }

        #endregion

        #region Afflictions Management

        /// Draws a random number of affliction cards from the affliction deck, with the number
        /// of cards to be drawn determined using a weighted random value based on the current plant hand size.
        /// The method first shuffles the affliction deck, clears any existing cards in the affliction hand,
        /// and then draws a specified number of cards while updating the affliction hand.
        /// Log information about the drawn cards if debugging is enabled
        /// and applies the newly drawn affliction deck to the game.
        public void DrawAfflictions()
        {
            _afflictionHand.DeckRandomDraw();
            ShuffleDeck(_afflictionHand.Deck);
            _afflictionHand.Clear();

            var max = _plantHand.Count;
            //var cardsToDraw = MinWeightedRandom(1, max);
            var cardsToDraw = RoundWeightedRandom(1, max + 1);

            _afflictionHand.DrawCards(cardsToDraw);

            if (debug)
                Debug.Log("Afflictions Drawn: " + string.Join(", ", _afflictionHand.ConvertAll(card => card.Name)));
            ApplyAfflictionDeck();
            CardGameMaster.Instance.scoreManager.CalculateTreatmentCost();
        }

        /// Applies afflictions from the affliction deck to available plants in the scene.
        /// For each card in the affliction hand, an affliction is applied to a randomly chosen
        /// plant that is not already afflicted, up to the minimum number of either available
        /// plants or affliction cards.
        /// If the affliction is a mildew type, an intensity value is generated and set for the plant.
        /// Each plant's shaders are flagged for updates after the affliction is applied.
        /// Optionally, log Debug messages, including details about the applied afflictions.
        /// Afflictions that fail to assign due to missing related data will log a warning.
        private void ApplyAfflictionDeck()
        {
            var availablePlants = plantLocations
                .Select(location => location.GetComponentInChildren<PlantController>(true))
                .Where(controller => controller)
                .ToList();

            var numAfflictions = _afflictionHand.Count;
            var numToApply = Mathf.Min(numAfflictions, availablePlants.Count);


            for (var i = 0; i < numToApply; i++)
            {
                var randomIndex = Random.Range(0, availablePlants.Count);
                var plantController = availablePlants[randomIndex];
                availablePlants.RemoveAt(randomIndex);

                var card = _afflictionHand[i];
                var affliction = card.Affliction;
                if (affliction != null)
                {
                    // Check if the plant already has the affliction, Skip if it does.
                    if (plantController.HasAffliction(affliction)) continue;

                    plantController.AddAffliction(affliction);

                    if (affliction is PlantAfflictions.MildewAffliction)
                    {
                        var intensity = Random.Range(MinMoldIntensity, MaxMoldIntensity);
                        plantController.SetMoldIntensity(intensity);
                    }

                    plantController.FlagShadersUpdate();
                    if (debug) Debug.Log($"Applied {affliction.Name} to {plantController.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning("Card does not have an affliction: " + card.Name);
                }
            }
        }

        #endregion

        #region Hand Layout Helpers

        /// <summary>
        /// Calculates position and rotation for a card at a specific index in the hand layout
        /// </summary>
        /// <param name="cardIndex">Index of the card in the hand (0-based)</param>
        /// <param name="totalCards">Total number of cards in hand</param>
        /// <param name="effectiveSpacing">Spacing between cards</param>
        /// <param name="useOverlapLayout">Whether to use overlap layout or fan layout</param>
        /// <returns>Target position and rotation for the card</returns>
        private static (Vector3 position, Quaternion rotation) CalculateCardTransform(int cardIndex, int totalCards,
            float effectiveSpacing, bool useOverlapLayout)
        {
            if (useOverlapLayout)
            {
                // Overlap layout: cards arranged horizontally with minimal spacing, no fan angle
                var startX = -(totalCards - 1) * effectiveSpacing * 0.5f;
                var xOffset = startX + cardIndex * effectiveSpacing;
                var zOffset = cardIndex * 0.01f; // Small Z offset for proper layering (rightmost card on top)
                var position = new Vector3(xOffset, 0f, -zOffset);
                var rotation = Quaternion.identity; // No rotation in overlap mode
                return (position, rotation);
            }
            else
            {
                // Fan layout: calculate fan offsets with adjusted spacing
                const float totalFanAngle = -30f;
                var angleOffset = totalCards > 1
                    ? Mathf.Lerp(-totalFanAngle / 2, totalFanAngle / 2, (float)cardIndex / (totalCards - 1))
                    : 0f;
                var xOffset = totalCards > 1
                    ? Mathf.Lerp(-effectiveSpacing, effectiveSpacing, (float)cardIndex / (totalCards - 1))
                    : 0f;

                var position = new Vector3(xOffset, 0f, 0f);
                var rotation = Quaternion.Euler(0, 0, angleOffset);
                return (position, rotation);
            }
        }

        /// <summary>
        /// Updates Click3D component's original transform references for proper hover behavior
        /// </summary>
        /// <param name="click3D">The Click3D component to update</param>
        /// <param name="scale">The card's actual scale</param>
        /// <param name="position">The card's actual position</param>
        private static void UpdateClick3DFields(Click3D click3D, Vector3 scale, Vector3 position)
        {
            click3D?.UpdateOriginalTransform(scale, position);
        }


        /// <summary>
        /// Calculates optimal hand layout parameters based on the total number of cards.
        /// </summary>
        /// <param name="totalCards">The current number of cards in the hand.</param>
        /// <returns>A tuple containing:
        /// <c>effectiveSpacing</c> – The spacing to apply between adjacent cards after any adjustments.<br/>
        /// <c>cardScale</c> – The uniform scale to apply to each card, preserving the aspect ratio.<br/>
        /// <c>useOverlapLayout</c> – A flag indicating whether the hand should switch to an overlap layout for large hands.</returns>
        /// <remarks>
        /// The method first attempts a scaling approach for up to six cards. If more than six cards are present,
        /// it falls back to an overlapping layout (logic omitted in this snippet). For small hands, spacing
        /// may be reduced proportionally to the number of cards drawn per turn, and card size is scaled
        /// down only if the required width exceeds a maximum hand width. The returned values are used
        /// by animation routines to position and scale card transforms smoothly.
        /// </remarks>
        private (float effectiveSpacing, Vector3 cardScale, bool useOverlapLayout) CalculateHandLayout(int totalCards)
        {
            const float maxHandWidth = 8f; // Maximum width for card hand spread
            const int maxScalingCards = 6; // Switch to overlap after this many cards
            
            var effectiveSpacing = cardSpacing;
            
            // Get the prefab's original scale as a baseline
            var cgm = CardGameMaster.Instance;
            var prefabScale = cgm.actionCardPrefab ? cgm.actionCardPrefab.transform.localScale : Vector3.one;
            var cardScale = prefabScale;
            var useOverlapLayout = false;
            
            // Hybrid approach: scaling up to 6 cards, overlap for 7+
            if (totalCards <= maxScalingCards)
            {
                // Use a scaling approach for smaller hands (up to 6 cards)
                if (totalCards <= cardsDrawnPerTurn) return (effectiveSpacing, cardScale, false);
                // Reduce spacing dynamically based on card count
                var overflowFactor = (float)cardsDrawnPerTurn / totalCards;
                effectiveSpacing = cardSpacing * overflowFactor;
                    
                // Calculate if we need to also scale down cards
                var requiredWidth = totalCards > 1 ? (totalCards - 1) * effectiveSpacing * 2 : 0f;
                if (!(requiredWidth > maxHandWidth)) return (effectiveSpacing, cardScale, false);
                var scaleFactor = Mathf.Clamp(maxHandWidth / requiredWidth, 0.7f, 1f);
                effectiveSpacing *= scaleFactor;
                // Apply a scale factor to the prefab's original scale
                cardScale = prefabScale * Mathf.Max(scaleFactor, 0.85f); // Don't scale below 85% of the original
            }
            else
            {
                // Use overlap layout for 7+ cards
                useOverlapLayout = true;
                cardScale = prefabScale; // Keep cards at full size in overlap mode
                
                // Calculate overlap spacing - much smaller for overlap layout
                // We want cards to overlap significantly, showing just enough to be selectable
                effectiveSpacing = 0.1f; // Extremely tight overlap
                
                // Ensure the total width doesn't exceed our limit
                var totalWidth = (totalCards - 1) * effectiveSpacing;
                if (totalWidth > maxHandWidth)
                {
                    effectiveSpacing = maxHandWidth / (totalCards - 1);
                }
            }
            
            return (effectiveSpacing, cardScale, useOverlapLayout);
        }

        #endregion

        #region Action Card Management

        /// <summary>
        /// Replaces the current action hand with a fresh set of cards drawn from the action deck.
        /// </summary>
        /// <remarks>
        /// The operation follows these steps in order:
        /// <para>1. If the action display is currently updating, the method exits immediately to avoid interference.</para>
        /// <para>2. All cards presently held in the action hand are moved to the discard pile via <c>DiscardActionCard</c>, then the hand list is cleared.</para>
        /// <para>3. If the number of cards that were previously in the hand exceeds the maximum allowed per turn, excess cards are removed and a warning is logged.</para>
        /// <para>4. Any visual representations of action cards currently displayed are removed by calling <c>ClearActionCardVisuals</c>.</para>
        /// <para>5. Cards are drawn from the top of the action deck until the hand contains the configured number of cards per turn.
        /// If the deck is exhausted, it may be replenished from the discard pile (details omitted for brevity).</para>
        /// <para>6. A coroutine is started to animate or otherwise display the newly drawn cards.</para>
        /// <para>7. When debugging is enabled, the method logs the contents of the hand, deck, and discard pile for diagnostic purposes.</para>
        /// </remarks>
        public void DrawActionHand()
        {
            if (updatingActionDisplay) return;

            // Discard current hand cards to discard pile
            var cardsToDiscard = new List<ICard>(_actionHand);
            foreach (var card in cardsToDiscard)
                DiscardActionCard(card, true);

            _actionHand.Clear();

            if (_actionHand.Count > cardsDrawnPerTurn)
            {
                Debug.LogWarning("Hand overflow detected. Trimming hand.");
                _actionHand.RemoveRange(cardsDrawnPerTurn, _actionHand.Count - cardsDrawnPerTurn);
            }

            // Clear all existing visualized cards
            ClearActionCardVisuals();

            var cardsNeeded = cardsDrawnPerTurn;

            while (cardsNeeded > 0)
            {
                // Recycle the discard pile only if deck empty and the discard pile has cards
                if (_actionDeck.Count == 0 && _actionDiscardPile.Count > 0)
                {
                    if (debug)
                        Debug.Log(
                            $"Recycling {_actionDiscardPile.Count} cards from discard pile into action deck.");
                    _actionDeck.AddRange(_actionDiscardPile);
                    _actionDiscardPile.Clear();
                    ShuffleDeck(_actionDeck);
                    if (debug) Debug.Log("Recycled discard pile into action deck.");
                }

                if (_actionDeck.Count == 0)
                {
                    if (debug) Debug.Log("No cards left in action deck to draw.");
                    break; // No more cards to draw
                }

                var drawnCard = _actionDeck[0];
                _actionDeck.RemoveAt(0);
                _actionHand.Add(drawnCard);
                cardsNeeded--;
            }

            DisplayActionCardsSequence();

            if (debug)
                Debug.Log(
                    $"Action Hand ({_actionHand.Count}): {string.Join(", ", _actionHand.ConvertAll(card => card.Name))}");
            if (debug)
                Debug.Log(
                    $"Action Deck ({_actionDeck.Count}): {string.Join(", ", _actionDeck.ConvertAll(card => card.Name))}");
            if (debug)
                Debug.Log(
                    $"Discard Pile ({_actionDiscardPile.Count}): {string.Join(", ", _actionDiscardPile.ConvertAll(card => card.Name))}");
        }

        /// Discards the specified action card by removing it from the action hand.
        /// Optionally adds the card to the discard pile if the parameter is set to true.
        /// Reset the currently selected action card if the discarded card matches the selected one.
        /// <param name="card">The action card to be discarded.</param>
        /// <param name="addToDiscard">Whether to add the discarded card to the discard pile.</param>
        public void DiscardActionCard(ICard card, bool addToDiscard)
        {
            _actionHand.Remove(card);
            if (addToDiscard) AddCardToDiscard(card);
            if (card != selectedACard) return;
            ClearSelectedCard();
        }

        /// Discards the currently selected action card from the action hand.
        /// This method removes the selected card from the action hand, adds it to the discard pile,
        /// and clears the reference to the selected card and its associated Click3D component.
        /// If no card is selected, the method does nothing.
        public void DiscardSelectedCard()
        {
            if (selectedACard == null) return;
            var selectedView = selectedACardClick3D;
            _actionHand.Remove(selectedACard);
            AddCardToDiscard(selectedACard);
            ClearSelectedCard();

            if (selectedView)
                Destroy(selectedView.gameObject);
        }

        private void AddCardToDiscard(ICard card)
        {
            _actionDiscardPile.Add(card);
        }

        public void AddCardToHand(ICard card)
        {
            _actionHand.Add(card);
        }


        /// <summary>
        /// Adds the specified card to the internal action hand list and, if visual assets are available,
        /// creates a new card view instance, configures it with the supplied card data, and initiates
        /// an animation that reflows the entire hand layout.
        /// </summary>
        /// <param name="card">The card object to add to the hand. The object is stored in the internal list and
        /// used to configure the visual representation.</param>
        /// <param name="animDuration">
        /// Duration of the hand‑reflow animation, expressed in seconds. A default value of 0.3f is provided,
        /// but callers may override it to create faster or slower transitions.
        /// </param>
        public void AddCardToHandWithAnimation(ICard card, float animDuration = 0.3f)
        {
            if (card == null)
            {
                Debug.LogError("Cannot add null card to hand");
                return;
            }
            
            // Prevent concurrent animation state issues
            if (updatingActionDisplay)
            {
                Debug.LogWarning("Animation already in progress, queuing card addition after completion");
                // For now, add the card immediately but skip animation to prevent race conditions
                _actionHand.Add(card);
                return;
            }
            
            _actionHand.Add(card);

            if (!actionCardParent || !card.Prefab)
            {
                // No visuals available; exit early.
                return;
            }

            // Log hand size status if debugging is enabled
            if (debug && _actionHand.Count > cardsDrawnPerTurn)
            {
                var layoutMode = _actionHand.Count <= 6 ? "scaling" : "overlap";
                Debug.Log($"Hand overflow: {_actionHand.Count} cards (normal: {cardsDrawnPerTurn}). Using {layoutMode} layout.");
            }

            // Create the visual for the new card
            var newCardObj = Instantiate(card.Prefab, actionCardParent);
            var cardView = newCardObj.GetComponent<CardView>();
            if (cardView)
                cardView.Setup(card);
            else
                Debug.LogWarning("Action Card Prefab is missing a Card View...");

            // Start from a hidden / centered state with the correct scale
            var t = newCardObj.transform;
            t.localScale = Vector3.zero; // Start at zero, will animate to the final scale
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
            var sfx = CardGameMaster.Instance.soundSystem?.drawCard;
            if (playerAudio && sfx) playerAudio.PlayOneShot(sfx);

            // Animate the entire hand to its new layout including this card
            AnimateHandReflow(animDuration);
        }

        private void AnimateHandReflow(float duration)
        {
            // Kill any existing hand animation sequence to prevent memory leaks
            SafeKillSequence(ref _currentHandSequence);
            
            updatingActionDisplay = true;

            // Capture current children as the visuals we will reflow
            var childCount = actionCardParent.childCount;
            if (childCount == 0)
            {
                updatingActionDisplay = false;
                _currentHandSequence = null;
                return;
            }

            // Calculate optimal layout for hand size
            var (effectiveSpacing, cardScale, useOverlapLayout) = CalculateHandLayout(childCount);

            // Temporarily disable Click3D interactions during animation to prevent conflicts
            var click3DComponents = new Click3D[childCount];
            for (var i = 0; i < childCount; i++)
            {
                var tf = actionCardParent.GetChild(i);
                var click3D = tf.GetComponent<Click3D>();
                if (!click3D) continue;
                click3DComponents[i] = click3D;
                click3D.StopAllCoroutines(); // Stop any ongoing hover animations
                click3D.enabled = false; // Temporarily disable to prevent conflicts
            }

            // Create a DOTween sequence for all card animations
            _currentHandSequence = DOTween.Sequence();

            for (var i = 0; i < childCount; i++)
            {
                var tf = actionCardParent.GetChild(i);
                var (targetPos, targetRot) = CalculateCardTransform(i, childCount, effectiveSpacing, useOverlapLayout);

                // Add simultaneous animations for position, rotation, and scale
                _currentHandSequence.Join(
                    tf.DOLocalMove(targetPos, duration)
                        .SetEase(Ease.OutQuart)
                        .SetLink(tf.gameObject, LinkBehaviour.KillOnDisable)
                );
                _currentHandSequence.Join(
                    tf.DOLocalRotateQuaternion(targetRot, duration)
                        .SetEase(Ease.OutQuart)
                        .SetLink(tf.gameObject, LinkBehaviour.KillOnDisable)
                );
                _currentHandSequence.Join(
                    tf.DOScale(cardScale, duration)
                        .SetEase(Ease.OutQuart)
                        .SetLink(tf.gameObject, LinkBehaviour.KillOnDisable)
                );
            }

            // Set up a completion callback
            _currentHandSequence.OnComplete(() =>
            {
                try
                {
                    // Fix Click3D original scale and position for proper hover behavior, then re-enable
                    for (var i = 0; i < childCount; i++)
                    {
                        if (i >= actionCardParent.childCount || i >= click3DComponents.Length) continue; // Safety check for destroyed children
                        actionCardParent.GetChild(i);
                        var (targetPos, _) = CalculateCardTransform(i, childCount, effectiveSpacing, useOverlapLayout);
                        var click3D = click3DComponents[i];
                        if (!click3D) continue;
                        // Update the Click3D original transform references
                        UpdateClick3DFields(click3D, cardScale, targetPos);
                        // Re-enable the Click3D component now that our animation is complete
                        click3D.enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error in hand reflow completion: {ex.Message}");
                    
                    // Re-enable all Click3D components in case of error
                    foreach (var t in click3DComponents)
                    {
                        if (t) t.enabled = true;
                    }
                }
                finally
                {
                    // Always reset state, even if there was an error
                    updatingActionDisplay = false;
                    _currentHandSequence = null;
                }
            });

            // Start the animation sequence
            _currentHandSequence.Play();
        }

        /// Clears the action hand, deck, and discard the pile by removing all cards from these lists.
        /// Additionally, destroys all child objects under the `actionCardParent` transform.
        /// After clearing, it reinitializes the action deck and logs the operation if debugging is enabled.
        public void ClearActionHand()
        {
            var cardsToDiscard = new List<ICard>(_actionHand); // Make a copy

            foreach (var card in cardsToDiscard)
                DiscardActionCard(card, true);

            _actionHand.Clear();

            ClearActionCardVisuals();

            if (debug) Debug.Log("Cleared action hand and discarded cards.");
        }

        public void AddActionCard(ICard card)
        {
            _actionHand.Add(card);
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionHand.ConvertAll(input => input.Name)));
        }

        /// <summary>
        /// Displays action cards in a fanned-out sequence within the scene using DOTween animations.
        /// This method creates GameObjects for each card in the action hand and positions them
        /// within a parent transform, arranging them in a visually pleasing fanned layout.
        /// Each card GameObject is initialized with its corresponding data through the CardView component.
        /// Cards appear sequentially with a staggered animation and scale up with a bounce effect.
        /// </summary>
        private void DisplayActionCardsSequence()
        {
            // Kill any existing display animation sequence to prevent memory leaks
            SafeKillSequence(ref _currentDisplaySequence);
            
            updatingActionDisplay = true;

            var cardsToDisplay = new List<ICard>(_actionHand);
            var totalCards = _actionHand.Count;
            
            // Calculate optimal layout for hand size
            var (effectiveSpacing, cardScale, useOverlapLayout) = CalculateHandLayout(totalCards);

            // Create a DOTween sequence for staggered card appearances
            _currentDisplaySequence = DOTween.Sequence();
            // Scale delay so total fan-in animation stays quick even for large hands
            var cardDelay = Mathf.Min(0.1f, totalCards > 0 ? 0.6f / totalCards : 0.1f);

            for (var i = 0; i < totalCards; i++)
            {
                var card = cardsToDisplay[i];
                var cardIndex = i; // Capture for closure
                
                _currentDisplaySequence.AppendCallback(() =>
                {
                    try
                    {
                        var cardObj = Instantiate(card.Prefab, actionCardParent);
                        var cardView = cardObj.GetComponent<CardView>();
                        if (cardView)
                            cardView.Setup(card);
                        else
                            Debug.LogWarning("Action Card Prefab is missing a Card View...");

                        var (targetPos, targetRot) = 
                            CalculateCardTransform(cardIndex, totalCards, effectiveSpacing, useOverlapLayout);

                        // Start from zero scales and animate in
                        cardObj.transform.localPosition = targetPos;
                        cardObj.transform.localRotation = targetRot;
                        cardObj.transform.localScale = Vector3.zero;

                        // Temporarily disable Click3D to prevent conflicts during scale animation
                        var click3D = cardObj.GetComponent<Click3D>();
                        if (click3D)
                        {
                            click3D.enabled = false;
                        }

                        // Animate the card scaling up with a bounce effect
                        cardObj.transform
                            .DOScale(cardScale, 0.3f)
                            .SetLink(cardObj, LinkBehaviour.KillOnDisable)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() =>
                            {
                                // Re-enable Click3D and set the proper original transform after animation completes
                                if (!click3D) return;
                                UpdateClick3DFields(click3D, cardScale, targetPos);
                                click3D.enabled = true;
                            });

                        var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
                        var sfx = CardGameMaster.Instance.soundSystem?.drawCard;
                        if (playerAudio && sfx) playerAudio.PlayOneShot(sfx);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error creating card in display sequence: {ex.Message}");
                    }
                });
                
                if (i < totalCards - 1) // Don't add delay after the last card
                {
                    _currentDisplaySequence.AppendInterval(cardDelay);
                }
            }

            // Set completion callback
            _currentDisplaySequence.OnComplete(() =>
            {
                updatingActionDisplay = false;
                _currentDisplaySequence = null;
            });

            _currentDisplaySequence.Play();
        }

        public void RedrawCards()
        {
            if (updatingActionDisplay) return;

            if (CardGameMaster.Instance.cardHolders.Any(holder => holder && holder.HoldingCard))
            {
                Debug.LogError("Cards In CardHolder!");
                return;
            }

            // Create a temporary list to avoid modifying _actionHand while iterating
            var cardsToDiscard = new List<ICard>(_actionHand);
            foreach (var card in cardsToDiscard) DiscardActionCard(card, true);

            _actionHand.Clear();

            if (_actionHand.Count > cardsDrawnPerTurn)
            {
                Debug.LogWarning("Hand overflow detected. Trimming hand.");
                _actionHand.RemoveRange(cardsDrawnPerTurn, _actionHand.Count - cardsDrawnPerTurn);
            }

            // Clear all existing visualized cards in the action card parent
            ClearActionCardVisuals();

            for (var i = 0; i < cardsDrawnPerTurn; i++)
            {
                // Handle a case when the action deck or discard pile is empty
                if (_actionDeck.Count == 0 && _actionDiscardPile.Count > 0)
                {
                    _actionDeck.AddRange(_actionDiscardPile);
                    _actionDiscardPile.Clear();
                    ShuffleDeck(_actionDeck);
                    if (debug) Debug.Log("Recycled discard pile into action deck.");
                }

                // Ensure we don't try to access an empty action deck
                if (_actionDeck.Count <= 0) continue;

                var drawnCard = _actionDeck[0];
                _actionDeck.RemoveAt(0);
                _actionHand.Add(drawnCard);
            }

            DisplayActionCardsSequence();
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionHand.ConvertAll(card => card.Name)));
            ScoreManager.SubtractMoneys(redrawCost);
            ScoreManager.UpdateMoneysText();
        }

        /// <summary>
        ///     Destroys all GameObjects under actionCardParent.
        /// </summary>
        private void ClearActionCardVisuals()
        {
            foreach (Transform child in actionCardParent)
                Destroy(child.gameObject);
        }

        /// <summary>
        ///     Refreshes the action hand display to match the current _actionHand list.
        ///     Clears existing visuals and plays the display sequence for all cards.
        /// </summary>
        public void RefreshActionHandDisplay()
        {
            if (updatingActionDisplay) return;
            ClearActionCardVisuals();
            DisplayActionCardsSequence();
        }

        #endregion

        #region DOTween Sequence Management

        /// <summary>
        /// Safely kills a DOTween sequence with proper error handling and memory cleanup.
        /// </summary>
        /// <param name="sequence">The sequence to kill (passed by reference and set to null)</param>
        private static void SafeKillSequence(ref Sequence sequence)
        {
            if (sequence == null) return;

            try
            {
                sequence.Kill(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error killing DOTween sequence: {ex.Message}");
            }
            finally
            {
                sequence = null;
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Cleanup DOTween sequences on component destruction to prevent memory leaks
        /// </summary>
        private void OnDestroy()
        {
            SafeKillSequence(ref _currentHandSequence);
            SafeKillSequence(ref _currentDisplaySequence);
        }

        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Analytics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using _project.Scripts.ModLoading;
using _project.Scripts.Stickers;
using Unity.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Card_Core
{
    [RequireComponent(typeof(DeckActionHandAnimator))]
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
        ///     Sets up stickers in a simple stacked layout.
        ///     For single stickers, keep them centered.
        ///     For multiple stickers, stack them like cards.
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
        ///     Positions stickers with consistent spacing regardless of quantity
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
            new MildewCard(),

            // cards w/o shaders
            new FungusGnatsCard(),
            new SpiderMitesCard(),

            // non-spreadable condition cards
            new DehydratedCard(),
            new NeedsLightCard()
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
            new SoapyWaterBasic(),
            new SoapyWaterBasic(), 
            new UreaBasic(),
            new UreaBasic(),
            new IsolateBasic(),
            new IsolateBasic(),
            new ImidaclopridTreatment(),
            new SpinosadTreatment(),
            new HydrationBasic(),
            new SunlightBasic(),
            new PermethrinBasic(),
            new PermethrinBasic(),
            new LadyBugsCard(),
            new LadyBugsCard(),

            // TODO cards w/o new materials
            new Panacea(),
        };

        private readonly List<ICard> _tutorialActionDeck = new()
        {
            new HorticulturalOilBasic(),
            new FungicideBasic(),
            new PermethrinBasic(),
            new SoapyWaterBasic(),
            new Panacea(),

            new HorticulturalOilBasic(),
            new FungicideBasic(),
            new PermethrinBasic(),
            new SoapyWaterBasic(),
            new Panacea()
        };

        private readonly List<ICard> _tutorialPlantDeck = new();

        private readonly List<ICard> _tutorialAfflictionDeck = new();

        #endregion

        #region Declare Decks

        private readonly List<ICard> _actionDeck = new();
        private readonly List<ICard> _actionDiscardPile = new();
        private readonly List<ICard> _sideDeck = new();
        private readonly List<ISticker> _playerStickers = new();
        private static readonly List<ICard> PlantDeck = new();
        private static readonly List<ICard> AfflictionsDeck = new();

        // Price boost tracking for current level
        private static PlantCardCategory? _boostedCategory;
        private static int _priceBoostAmount;

        #endregion

        #region Class Variables

        [DontSerialize] private bool _updatingActionDisplay;

        public bool UpdatingActionDisplay
        {
            get => _updatingActionDisplay;
            private set
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_updatingActionDisplay != value && debug)
                {
                    Debug.Log($"[DeckManager] updatingActionDisplay: {_updatingActionDisplay} -> {value}");

                    // Only log stack trace for suspicious transitions (setting true when already true)
                    if (value && _updatingActionDisplay)
                    {
                        Debug.LogWarning($"[DeckManager] Suspicious flag transition!\n{Environment.StackTrace}");
                    }
                }
                #endif
                _updatingActionDisplay = value;
            }
        }

        [SerializeField] private DeckActionHandAnimator actionHandAnimator;

        internal void SetUpdatingActionDisplay(bool value)
        {
            UpdatingActionDisplay = value;
        }

        internal void ForceClearUpdatingActionDisplay()
        {
            _updatingActionDisplay = false;
        }

        private DeckActionHandAnimator ActionHandAnimator
        {
            get
            {
                if (actionHandAnimator) return actionHandAnimator;
                actionHandAnimator = GetComponent<DeckActionHandAnimator>();
                return actionHandAnimator;
            }
        }

        private readonly CardHand _afflictionHand = new("Afflictions Hand", AfflictionsDeck, PrototypeAfflictionsDeck);
        private readonly CardHand _plantHand = new("Plants Hand", PlantDeck, PrototypePlantsDeck);
        private readonly List<ICard> _actionHand = new();
        private bool _usingTutorialActionDeck;

        public List<PlantHolder> plantLocations = new();
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
            var hadSelection = selectedACardClick3D is not null || selectedACard != null;
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
        private int _redrawCount;
        public int RedrawCost => GetFibonacci(4 + _redrawCount);
        public bool debug = true;

        public void ResetRedrawCount()
        {
            _redrawCount = 0;
        }

        private static int GetFibonacci(int n)
        {
            if (n <= 1) return n;
            var a = 0;
            var b = 1;
            for (var i = 2; i <= n; i++)
            {
                var temp = a;
                a = b;
                b = temp + b;
            }

            return b;
        }

        #endregion

        #region Initialization

        private void Awake()
        {
            if (ActionHandAnimator) return;
            actionHandAnimator = gameObject.AddComponent<DeckActionHandAnimator>();
        }

        private void Start()
        {
            // Defensive reset: ensure the animation flag starts clean
            UpdatingActionDisplay = false;

            WarnIfPlantLocationsLikelyMissing();

            InitializePlantHolders();
            InitializeActionDeck();
            InitializeStickerDeck();
            _plantHand.DeckRandomDraw();
            _afflictionHand.DeckRandomDraw();
            if (debug) Debug.Log("Initial Deck Order: " + string.Join(", ", PlantDeck.ConvertAll(card => card.Name)));
            if (debug)
                Debug.Log("Initial Deck Order: " + string.Join(", ", AfflictionsDeck.ConvertAll(card => card.Name)));
        }

        /// <summary>
        /// Initializes each PlantHolder by discovering its child PlacedCardHolder components.
        /// </summary>
        private void InitializePlantHolders()
        {
            if (plantLocations == null) return;
            foreach (var holder in plantLocations)
                holder?.InitializeCardHolders();
        }

        private void WarnIfPlantLocationsLikelyMissing()
        {
            if (plantLocations is { Count: > 0 }) return;

            // Avoid noisy logs in tests/minimal setups where no board exists yet.
            if (FindObjectsByType<SpotDataHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length ==
                0) return;

            Debug.LogError(
                "[DeckManager] plantLocations is empty. If you upgraded from a version where this was a List<Transform>, " +
                "run Tools > Migration > Migrate DeckManager Plant Locations (Transform -> PlantHolder) to update scenes/prefabs.");
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
        ///     Register a new action card prototype from a mod before deck initialization.
        /// </summary>
        /// <param name="prototype">Card to add to the action prototype pool</param>
        public void RegisterModActionPrototype(ICard prototype)
        {
            if (prototype == null) return;
            PrototypeActionDeck.Add(prototype);
            if (debug) Debug.Log($"[Mods] Added action prototype: {prototype.Name}");
        }

        /// <summary>
        ///     Register a StickerDefinition from a mod and spawn it in the sticker pack area if present.
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
        public List<ICard> GetSideDeck() => new(_sideDeck);
        public List<ICard> GetDiscardPile() => new(_actionDiscardPile);
        public List<ICard> GetActionHand() => new(_actionHand);
        public List<ISticker> GetPlayerStickers() => new(_playerStickers);

        public void RestoreActionDeck(List<CardData> cards)
        {
            _actionDeck.Clear();
            foreach (var restored in cards.Select(GameStateManager.DeserializeCard)
                         .Where(restored => restored != null)) _actionDeck.Add(restored);
        }

        public void RestoreSideDeck(List<CardData> cards)
        {
            _sideDeck.Clear();
            foreach (var restored in cards.Select(GameStateManager.DeserializeCard)
                         .Where(restored => restored != null)) _sideDeck.Add(restored);
        }

        public void RestoreDiscardPile(List<CardData> cards)
        {
            _actionDiscardPile.Clear();
            foreach (var restored in cards.Select(GameStateManager.DeserializeCard)
                         .Where(restored => restored != null)) _actionDiscardPile.Add(restored);
        }

        public void RestoreActionHand(List<CardData> cards)
        {
            _actionHand.Clear();
            foreach (var restored in cards.Select(GameStateManager.DeserializeCard)
                         .Where(restored => restored != null)) _actionHand.Add(restored);
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

        // DO NOT CALL THIS. THIS KINDA SUCKS -- I will unfortunately be calling this
        public void ApplyActionDeckOverride(List<ICard> deck)
        {
            if (deck == null) return;

            _usingTutorialActionDeck = false;
            ClearSelectedCard();
            _actionHand.Clear();

            if (actionCardParent)
                ClearActionCardVisuals();

            _actionDiscardPile.Clear();
            _actionDeck.Clear();

            foreach (var card in deck)
                _actionDeck.Add(card);
        }

        // AGAIN - DO. NOT. CALL. THIS. -- I will be calling this... :(
        public void ApplySideDeckOverride(List<ICard> deck)
        {
            if (deck == null) return;

            _sideDeck.Clear();

            foreach (var card in deck)
                _sideDeck.Add(card);
        }

        public void AddDiscardToActionDeck()
        {
            foreach (var card in _actionDiscardPile.ToList())
            {
                _actionDeck.Add(card);
                _actionDiscardPile.Remove(card);
            }
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
            ApplyStoredPriceBoost();
            ShuffleDeck(_plantHand.Deck);
            _plantHand.Clear();

            var max = Mathf.Min(plantLocations.Count, PlantDeck.Count);
            var cardsToDraw = RoundWeightedRandom(1, max + 1);
            _plantHand.DrawCards(cardsToDraw);

            yield return StartCoroutine(PlacePlantsSequentially());
        }

        private IEnumerator PlacePlantsSequentially(float delay = 0.3f)
        {
            if (plantLocations == null)
                yield break;

            //delay = CardGameMaster.Instance.soundSystem.plantSpawn.length;
            for (var i = 0; i < _plantHand.Count && i < plantLocations.Count; i++)
            {
                var prefab = GetPrefabForCard(_plantHand[i]);
                if (!prefab) continue;

                var plantLocation = plantLocations[i];
                if (!plantLocation) continue;

                var plantLocationTransform = plantLocation.Transform;
                if (!plantLocationTransform) continue;

                // Play the sound before placing
                var clip = CardGameMaster.Instance.soundSystem.plantSpawn;
                if (clip) AudioSource.PlayClipAtPoint(clip, plantLocation.Position);

                // Instantiate and assign
                var plant = Instantiate(prefab, plantLocation.Position, plantLocation.Rotation);
                plant.transform.SetParent(plantLocationTransform);

                var plantController = plant.GetComponent<PlantController>();
                plantController.PlantCard = _plantHand[i];

                if (plantController.priceFlag && plantController.priceFlagText)
                    plantController.priceFlagText!.text = "$" + plantController.PlantCard.Value;

                // Notify SpotDataHolder that a plant was added
                var spotDataHolder = plantLocationTransform.GetComponentInChildren<SpotDataHolder>();
                if (spotDataHolder)
                {
                    spotDataHolder.InvalidatePlantCache();
                    spotDataHolder.RefreshAssociatedPlant();
                }

                yield return new WaitForSeconds(delay);
            }

            StartCoroutine(UpdateCardHolderRenders());
            CardGameMaster.Instance.scoreManager.CalculatePotentialProfit();
        }

        /// <summary>
        ///     Updates cardholder visibility and refreshes efficacy displays for all placed cards.
        ///     Called after plants spawn or when card placements change.
        /// </summary>
        // ReSharper disable Unity.PerformanceAnalysis - Coroutine called via StartCoroutine
        public IEnumerator UpdateCardHolderRenders()
        {
            // Wait one frame to ensure newly spawned plants are fully initialized
            // before querying their components in RefreshEfficacyDisplay
            yield return null;

            if (plantLocations == null)
                yield break;

            foreach (var location in plantLocations)
            {
                if (!location) continue;

                var plantTransform = location.Transform;
                if (!plantTransform) continue;

                var plantController = plantTransform.GetComponentInChildren<PlantController>(true);

                // Use cached CardHolders instead of GetComponentsInChildren (performance optimization)
                // Note: CardHolders cache is populated by InitializePlantHolders() in Start()
                // but dynamic locations may be added at runtime, so initialize on-demand.
                var cardHolders = location.CardHolders;
                if (cardHolders == null || cardHolders.Count == 0)
                {
                    location.InitializeCardHolders();
                    cardHolders = location.CardHolders;
                }
                if (cardHolders == null || cardHolders.Count == 0) continue;

                foreach (var cardHolder in cardHolders)
                {
                    if (!cardHolder) continue;

                    // Toggle visibility for empty holders (existing behavior)
                    if (!cardHolder.HoldingCard)
                        cardHolder.ToggleCardHolder(plantController != null);
                    // Refresh efficacy displays for placed treatment cards (bug fix)
                    else
                        cardHolder.RefreshEfficacyDisplay();
                }
            }
        }

        /// Clears all plant objects from their designated locations by destroying all
        /// associated GameObjects with `PlantController` components at each location.
        /// Ensures the randomness of later plant arrangements by invoking a
        /// random draw on the plant deck. Outputs debug messages if enabled.
        public void ClearAllPlants()
        {
            if (plantLocations == null) return;

            foreach (var slot in plantLocations)
            {
                if (!slot) continue;

                var slotTransform = slot.Transform;
                if (!slotTransform) continue;

                var plants = slotTransform.GetComponentsInChildren<PlantController>(true);
                foreach (var plant in plants)
                {
                    if (!plant) continue;
                    Destroy(plant.gameObject);
                }
            }

            // Notify SpotDataHolders that plants were removed
            foreach (var location in plantLocations)
            {
                if (!location) continue;

                var locationTransform = location.Transform;
                if (!locationTransform) continue;

                var spotDataHolder = locationTransform.GetComponentInChildren<SpotDataHolder>();
                if (!spotDataHolder) continue;

                spotDataHolder.InvalidatePlantCache();
                spotDataHolder.RefreshAssociatedPlant();
            }

            // Hide cardholders only if they are NOT currently holding a card
            foreach (var location in plantLocations)
            {
                if (!location) continue;

                var locationTransform = location.Transform;
                if (!locationTransform) continue;

                var holders = locationTransform.GetComponentsInChildren<PlacedCardHolder>(true);
                foreach (var holder in holders)
                {
                    if (!holder) continue;
                    if (holder.HoldingCard) continue; // Keep visible if a persistent card is present
                    holder.ToggleCardHolder(false);
                }
            }

            _plantHand.DeckRandomDraw();

            if (debug) Debug.Log("All plants cleared");
        }

        /// <summary>
        ///     Removes a plant from the board and cleans up associated cards and UI elements.
        /// </summary>
        /// <param name="plant">The PlantController to remove from the game board.</param>
        /// <param name="skipDeathSequence">
        ///     If true, skips the death animation and immediately destroys the plant.
        ///     Use this when the death animation is already running or when immediate cleanup is needed (e.g., tests).
        /// </param>
        /// <returns>Coroutine that completes when plant cleanup is finished.</returns>
        /// <remarks>
        ///     <para>Cleanup process:</para>
        ///     <list type="number">
        ///         <item>Plays death animation (unless skipDeathSequence=true)</item>
        ///         <item>Destroys plant GameObject</item>
        ///         <item>Clears treatment cards from PlacedCardHolders (cards are DESTROYED, not recycled to deck)</item>
        ///         <item>Preserves location cards (ILocationCard) - they remain visible as they're tied to location, not plant</item>
        ///         <item>Disables cardholders without location cards</item>
        ///         <item>Notifies SpotDataHolder of plant removal</item>
        ///     </list>
        ///     <para>
        ///         Design Note: Treatment cards on dying plants are destroyed as a penalty for poor plant management,
        ///         teaching players the importance of early intervention. Location cards persist because they affect
        ///         the growing location itself, independent of which plant occupies it.
        ///     </para>
        /// </remarks>
        public IEnumerator ClearPlant(PlantController plant, bool skipDeathSequence = false)
        {
            if (!plant) yield break;

            if (!skipDeathSequence)
                yield return plant.KillPlant(false);

            PlantHolder location = null;
            if (plantLocations != null)
            {
                foreach (var slot in plantLocations)
                {
                    if (!slot) continue;

                    var slotTransform = slot.Transform;
                    if (!slotTransform) continue;

                    if (!slotTransform.GetComponentsInChildren<PlantController>(true).Contains(plant)) continue;
                    location = slot;
                    break;
                }
            }

            var master = CardGameMaster.Instance;
            if (master?.isInspecting == true && master.inspectedObj)
            {
                var inspectComponent = plant.GetComponentInChildren<InspectFromClick>(true);
                if (inspectComponent && master.inspectedObj == inspectComponent)
                    inspectComponent.ToggleInspect();
            }

            Destroy(plant.gameObject);

            if (!location) yield break;

            var locationTransform = location!.Transform;
            if (!locationTransform) yield break;

            // Notify SpotDataHolder that the plant was removed
            var spotDataHolder = locationTransform.GetComponentInChildren<SpotDataHolder>();
            if (spotDataHolder)
            {
                spotDataHolder.InvalidatePlantCache();
                spotDataHolder.RefreshAssociatedPlant();
            }

            var cardHolders = locationTransform.GetComponentsInChildren<PlacedCardHolder>(true);
            foreach (var holder in cardHolders)
            {
                if (!holder) continue;

                switch (holder.HoldingCard)
                {
                    // Location cards persist after plant death (tied to location, not plant)
                    case true when holder.placedCard is ILocationCard:
                        // Keep the location card and holder visible
                        continue;
                    // Clear treatment cards - they're destroyed when the plant dies (not returned to deck)
                    case true:
                        holder.ClearHolder();
                        break;
                }

                // Hide the cardholder (already disabled by PlantController on death detection)
                holder.ToggleCardHolder(false);
            }
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

        /// <summary>
        ///     Calculates the price boost multiplier based on difficulty level.
        ///     Scales with rent increases to keep the game challenging but playable.
        /// </summary>
        /// <param name="level">Current difficulty level (defaults to reading from TurnController)</param>
        /// <returns>Multiplier for price boost (1x at early levels, scaling up with difficulty)</returns>
        /// <remarks>
        ///     Formula: 1 + (level - 1) / 2, capped at 20x for extreme levels
        ///     - Level 1-2: 1x multiplier
        ///     - Level 3-4: 2x multiplier
        ///     - Level 5-6: 3x multiplier
        ///     - Level 10: 5x multiplier
        ///     - Level 39+: 20x multiplier (capped)
        ///     This scaling keeps pace with rent increases (+$50/level starting at level 3).
        /// </remarks>
        private static int CalculatePriceBoostModifier(int? level = null)
        {
            int currentLevel;
            if (level.HasValue)
            {
                currentLevel = level.Value;
            }
            else if (CardGameMaster.Instance?.turnController)
            {
                currentLevel = CardGameMaster.Instance.turnController.level;
            }
            else
            {
                Debug.LogWarning("[DeckManager] CardGameMaster not initialized, using level 1 modifier");
                return 1;
            }

            var modifier = 1 + (currentLevel - 1) / 2;

            // Cap at 20x to prevent extreme scaling at very high levels
            return Mathf.Min(modifier, 20);
        }

        /// <summary>
        /// Generates a random plant price boost for the current level.
        /// Randomly selects one plant category (Fruiting or Decorative) and a boost amount (2-4),
        /// scaled by the current difficulty level, which will be applied to all matching plants
        /// when PrepareNextRound is called.
        /// </summary>
        /// <param name="level">Optional level override for testing (defaults to reading from TurnController)</param>
        /// <remarks>
        /// This method should be called once per level during level progression.
        /// The selected category and boost amount are stored statically and applied
        /// automatically by PrepareNextRound via ApplyStoredPriceBoost.
        /// Difficulty modifier multiplies base boost range (2-5) for higher levels.
        /// </remarks>
        public static void GeneratePlantPrices(int? level = null)
        {
            var modifier = CalculatePriceBoostModifier(level);
            _boostedCategory = (PlantCardCategory)Random.Range(0, 2);
            _priceBoostAmount = Random.Range(2, 5) * modifier;

            var currentLevel = level ?? CardGameMaster.Instance?.turnController?.level ?? 1;
            if (CardGameMaster.Instance?.debuggingCardClass == true)
                Debug.Log($"Level {currentLevel}: " +
                          $"Price boost of +${_priceBoostAmount} (modifier: {modifier}x) " +
                          $"will be applied to category: {_boostedCategory}");
        }

        private void ApplyStoredPriceBoost()
        {
            if (!_boostedCategory.HasValue) return;

            foreach (var card in PlantDeck)
            {
                if (card is not IPlantCard plantCard) continue;
                if (plantCard.Category != _boostedCategory.Value) continue;
                if (card.Value.HasValue)
                {
                    card.Value = card.Value.Value + _priceBoostAmount;
                }
            }

            if (!debug) return;
            var boostedCount = PlantDeck.Count(c => c is IPlantCard pc && pc.Category == _boostedCategory.Value);
            Debug.Log($"Applied +${_priceBoostAmount} boost to {boostedCount} {_boostedCategory} cards in deck");
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
                if (cardProto == null) continue;
                var prefab = GetPrefabForCard(cardProto);
                if (!prefab) continue;

                var location = plantLocations[pd.locationIndex];
                // Play spawn sound
                var clip = CardGameMaster.Instance.soundSystem.plantSpawn;
                if (clip) AudioSource.PlayClipAtPoint(clip, location.Position);

                // Instantiate and set parent
                var plantObj = Instantiate(prefab, location.Position, location.Rotation);
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

                // Restore location card tracking
                if (pd.uLocationCards != null)
                {
                    plant.uLocationCards.Clear();
                    plant.uLocationCards.AddRange(pd.uLocationCards);
                }

                // Restore infection/egg levels (convert the list back to dictionary)
                if (pd.infectData != null && plant.PlantCard is IPlantCard plantCardInterface)
                {
                    foreach (var entry in pd.infectData)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.source))
                        {
                            Debug.LogWarning("Skipping invalid InfectDataEntry during restoration");
                            continue;
                        }

                        if (entry.infect > 0)
                            plantCardInterface.Infect.SetInfect(entry.source, entry.infect);
                        if (entry.eggs > 0)
                            plantCardInterface.Infect.SetEggs(entry.source, entry.eggs);
                    }
                }

                // Restore isolation flags (backwards compatibility: detect the old save format)
                var isOldSaveFormat = pd.uLocationCards == null;
                if (isOldSaveFormat)
                {
                    // Old save: default to allowing spread/receive
                    plant.canSpreadAfflictions = true;
                    plant.canReceiveAfflictions = true;
                }
                else
                {
                    // New save: use saved values
                    plant.canSpreadAfflictions = pd.canSpreadAfflictions;
                    plant.canReceiveAfflictions = pd.canReceiveAfflictions;
                }

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
                "Dehydrated" => new PlantAfflictions.DehydratedAffliction(),
                "Needs Light" => new PlantAfflictions.NeedsLightAffliction(),
                _ => null
            };
        }

        private static PlantAfflictions.ITreatment GetTreatmentFromString(string trSting)
        {
            return trSting switch
            {
                "Horticultural Oil" => new PlantAfflictions.HorticulturalOilTreatment(),
                "Fungicide" => new PlantAfflictions.FungicideTreatment(),
                "Permethrin" => new PlantAfflictions.PermethrinTreatment(),
                "SoapyWater" => new PlantAfflictions.SoapyWaterTreatment(),
                "Spinosad" => new PlantAfflictions.SpinosadTreatment(),
                "Imidacloprid" => new PlantAfflictions.ImidaclopridTreatment(),
                "Hydration" => new PlantAfflictions.HydrationTreatmentBasic(),
                "Sunlight" => new PlantAfflictions.SunlightTreatmentBasic(),
                "LadyBugs" => new PlantAfflictions.LadyBugs(),
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

            // Refresh efficacy displays after afflictions are applied to new plants
            StartCoroutine(UpdateCardHolderRenders());

            CardGameMaster.Instance.scoreManager.CalculateTreatmentCost();
        }

        private void EnsureTutorialActionDeck()
        {
            if (_usingTutorialActionDeck) return;

            _usingTutorialActionDeck = true;

            ClearSelectedCard();
            _actionHand.Clear();
            _actionDeck.Clear();
            _actionDiscardPile.Clear();

            if (actionCardParent)
                ClearActionCardVisuals();

            foreach (var card in _tutorialActionDeck)
                _actionDeck.Add(card.Clone());
        }

        public void DrawTutorialActionHand()
        {
            if (debug) Debug.Log($"[DeckManager] DrawTutorialActionHand called. updatingActionDisplay={UpdatingActionDisplay}");

            if (UpdatingActionDisplay)
            {
                if (debug) Debug.LogWarning("[DeckManager] Cannot draw tutorial hand - updatingActionDisplay is true!");
                return;
            }

            if (debug) Debug.Log("[DeckManager] Ensuring tutorial action deck...");
            EnsureTutorialActionDeck();
            if (debug) Debug.Log("[DeckManager] Calling DrawActionHand...");
            DrawActionHand();

            if (debug)
                Debug.Log($"[DeckManager] Tutorial Action Hand: {string.Join(", ", _actionHand.ConvertAll(card => card.Name))}");
        }

        #endregion

        #region Afflictions Management

        /// Draws a random number of affliction cards from the affliction deck, with the number
        /// of cards to be drawn determined to use a weighted random value based on the current plant hand size.
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

            // Refresh efficacy displays after afflictions are applied to new plants
            StartCoroutine(UpdateCardHolderRenders());

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
            var validLocations = plantLocations?
                .Where(location => location)
                .ToList();

            if (validLocations == null || validLocations.Count == 0)
            {
                if (debug) Debug.LogWarning("[DeckManager] No plant locations available; skipping affliction application.");
                return;
            }

            var availablePlants = validLocations
                .Select(location => location.Transform.GetComponentInChildren<PlantController>(true))
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

                    // Record affliction applied analytics
                    try
                    {
                        var cgm = CardGameMaster.Instance;
                        if (cgm?.turnController != null)
                        {
                            AnalyticsFunctions.RecordAffliction(
                                plantController.PlantCard?.Name ?? plantController.name,
                                affliction.Name,
                                cgm.turnController.currentRound,
                                cgm.turnController.currentTurn
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Analytics error in RecordAffliction: {ex.Message}");
                    }

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

        #region Action Card Management

        /// <summary>
        ///     Replaces the current action hand with a fresh set of cards drawn from the action deck.
        /// </summary>
        /// <remarks>
        ///     The operation follows these steps in order:
        ///     <para>1. If the action display is currently updating, the method exits immediately to avoid interference.</para>
        ///     <para>
        ///         2. All cards presently held in the action hand are moved to the discard pile via <c>DiscardActionCard</c>,
        ///         then the hand list is cleared.
        ///     </para>
        ///     <para>
        ///         3. If the number of cards that were previously in the hand exceeds the maximum allowed per turn, excess cards
        ///         are removed and a warning is logged.
        ///     </para>
        ///     <para>
        ///         4. Any visual representations of action cards currently displayed are removed by calling
        ///         <c>ClearActionCardVisuals</c>.
        ///     </para>
        ///     <para>
        ///         5. Cards are drawn from the top of the action deck until the hand contains the configured number of cards per
        ///         turn.
        ///         If the deck is exhausted, it may be replenished from the discard pile (details omitted for brevity).
        ///     </para>
        ///     <para>6. A coroutine is started to animate or otherwise display the newly drawn cards.</para>
        ///     <para>
        ///         7. When debugging is enabled, the method logs the contents of the hand, deck, and discard pile for diagnostic
        ///         purposes.
        ///     </para>
        /// </remarks>
        public void DrawActionHand()
        {
            if (UpdatingActionDisplay) return;

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

            ActionHandAnimator.DisplayActionCardsSequence(_actionHand);

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
            if (_usingTutorialActionDeck)
            {
                _actionDeck.Add(card);
                return;
            }

            _actionDiscardPile.Add(card);
        }

        /// <summary>
        ///     Moves a card from the source deck to the side deck.
        ///     Only adds the card if it exists in the source deck to prevent duplication.
        /// </summary>
        private void AddCardToSideDeck(List<ICard> sourceDeck, ICard card)
        {
            if (!sourceDeck.Remove(card))
            {
                if (debug)
                    Debug.LogWarning(
                        $"[DeckManager] Attempted to move card '{card?.Name}' to side-deck, but it was not found in source deck.");
                return;
            }

            _sideDeck.Add(card);
        }

        /// <summary>
        ///     Moves a card from the source deck (typically side-deck) to the action deck.
        ///     Only adds the card if it exists in the source deck to prevent duplication.
        /// </summary>
        private void AddCardToActionDeck(List<ICard> sourceDeck, ICard card)
        {
            if (!sourceDeck.Remove(card))
            {
                if (debug)
                    Debug.LogWarning(
                        $"[DeckManager] Attempted to move card '{card?.Name}' to action deck, but it was not found in source deck.");
                return;
            }

            _actionDeck.Add(card);
        }

        public void AddCardToHand(ICard card)
        {
            _actionHand.Add(card);
        }

        /// <summary>
        ///     Resets the action deck to its standard configuration after tutorial play.
        ///     Clears tutorial-specific state, rebuilds the prototype action deck, and removes lingering visuals.
        /// </summary>
        public void ResetActionDeckAfterTutorial()
        {
            if (!_usingTutorialActionDeck) return;

            _usingTutorialActionDeck = false;

            ClearSelectedCard();
            _actionHand.Clear();

            if (actionCardParent)
                ClearActionCardVisuals();

            _actionDeck.Clear();
            _actionDiscardPile.Clear();

            InitializeActionDeck();
        }

        /// <summary>
        ///     Adds the specified card to the internal action hand list and, if visual assets are available,
        ///     creates a new card view instance, configures it with the supplied card data, and initiates
        ///     an animation that reflows the entire hand layout.
        /// </summary>
        /// <param name="card">
        ///     The card object to add to the hand. The object is stored in the internal list and
        ///     used to configure the visual representation.
        /// </param>
        /// <param name="animDuration">
        ///     Duration of the hand‑reflow animation, expressed in seconds. A default value of 0.3f is provided,
        ///     but callers may override it to create faster or slower transitions.
        /// </param>
        public void AddCardToHandWithAnimation(ICard card, float animDuration = 0.3f)
        {
            if (card == null)
            {
                Debug.LogError("Cannot add null card to hand");
                return;
            }
            
            // Prevent concurrent animation state issues
            if (UpdatingActionDisplay)
            {
                Debug.LogWarning("Animation already in progress, queuing card addition after completion");
                // For now, add the card immediately but skip animation to prevent race conditions
                _actionHand.Add(card);
                return;
            }

            _actionHand.Add(card);
            ActionHandAnimator.AddCardVisualAndAnimate(card, animDuration, _actionHand.Count);
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

        public void RedrawCards()
        {
            var cgm = CardGameMaster.Instance;
            if (cgm?.turnController is null)
            {
                Debug.LogWarning(
                    "[DeckManager] Cannot record redraw: CardGameMaster or TurnController not initialized");
                return;
            }

            var currentScore = ScoreManager.GetMoneys();
            var currentRoundNum = cgm.turnController.currentRound;
            var currentTurnNum = cgm.turnController.currentTurn;

            if (UpdatingActionDisplay)
            {
                AnalyticsFunctions.RecordRedraw("N/A", "N/A", currentScore, currentRoundNum, currentTurnNum,
                    false, "Animation in progress");
                return;
            }

            // Only block redraw if cards were placed THIS turn
            // NOTE: This check occurs BEFORE TurnController.EndTurn() increments currentTurn,
            // ensuring cards placed in the current turn block redraw until the turn ends.
            if (cgm.cardHolders.Any(holder => holder && holder.HoldingCard && holder.PlacementTurn == currentTurnNum))
            {
                Debug.LogError("Cards placed this turn are in CardHolder! Cannot redraw.");
                AnalyticsFunctions.RecordRedraw("N/A", "N/A", currentScore, currentRoundNum, currentTurnNum,
                    false, "Cards placed this turn");
                return;
            }

            // Create a temporary list to avoid modifying _actionHand while iterating
            var cardsToDiscard = new List<ICard>(_actionHand);

            // Capture discarded card names before clearing
            var cardsDiscarded = string.Join(",", cardsToDiscard.Select(card => card.Name));

            foreach (var card in cardsToDiscard)
            {
                DiscardActionCard(card, true);
            }

            _actionHand.Clear();

            if (_actionHand.Count > cardsDrawnPerTurn)
            {
                Debug.LogWarning("Hand overflow detected. Trimming hand.");
                _actionHand.RemoveRange(cardsDrawnPerTurn, _actionHand.Count - cardsDrawnPerTurn);
            }

            // Clear all existing visualized cards in the action card parent
            ClearActionCardVisuals();

            var newlyDrawnCards = new List<ICard>();

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
                newlyDrawnCards.Add(drawnCard);
            }

            var cardsDrawn = string.Join(",", newlyDrawnCards.Select(card => card.Name));

            ActionHandAnimator.DisplayActionCardsSequence(_actionHand);
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionHand.ConvertAll(card => card.Name)));

            var cost = RedrawCost;
            _redrawCount++;

            ScoreManager.SubtractMoneys(cost);
            ScoreManager.UpdateMoneysText();

            // Record successful redraw with actual card data
            AnalyticsFunctions.RecordRedraw(cardsDiscarded, cardsDrawn, ScoreManager.GetMoneys(),
                currentRoundNum, currentTurnNum);
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
            if (UpdatingActionDisplay) return;
            ClearActionCardVisuals();
            ActionHandAnimator.DisplayActionCardsSequence(_actionHand);
        }

        #endregion

        #region Animation Recovery

        /// <summary>
        ///     Force-resets the animation flag and kills all running sequences.
        ///     Called by TurnController when animation timeout is detected.
        /// </summary>
        public void ForceResetAnimationFlag()
        {
            Debug.LogWarning("[DeckManager] ForceResetAnimationFlag called - clearing stuck animation state.");
            ActionHandAnimator.ForceResetAnimationFlag();
        }

        #endregion
    }
}

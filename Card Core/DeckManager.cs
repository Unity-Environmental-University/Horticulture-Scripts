using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using Unity.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Card_Core
{
    public class DeckManager : MonoBehaviour
    {
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
            new Panacea()
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

        private readonly List<ICard> _tutorialAfflictionDeck = new()
        {
            new AphidsCard()
        };

        #endregion

        #region Declare Decks

        private readonly List<ICard> _actionDeck = new();
        private readonly List<ICard> _actionDiscardPile = new();
        private static readonly List<ICard> PlantDeck = new();
        private static readonly List<ICard> AfflictionsDeck = new();

        #endregion

        #region Class Variables

        [DontSerialize] public bool updatingActionDisplay;

        private readonly CardHand _afflictionHand = new("Afflictions Hand", AfflictionsDeck, PrototypeAfflictionsDeck);
        private readonly CardHand _plantHand = new("Plants Hand", PlantDeck, PrototypePlantsDeck);
        private readonly List<ICard> _actionHand = new();
        private static DeckManager Instance { get; set; }
        public List<Transform> plantLocations;
        public Transform actionCardParent;
        public Click3D selectedACardClick3D;
        public ICard SelectedACard;
        public GameObject cardPrefab;
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

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeActionDeck();
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
            foreach (var card in PrototypeActionDeck)
                //var duplicates = Random.Range(1, 5);
                //for (var i = 0; i < duplicates; i++) _actionDeck.Add(card.Clone());
                _actionDeck.Add(card.Clone());

            ShuffleDeck(_actionDeck);
            if (debug)
                Debug.Log("Initialized ActionDeck Order: " +
                          string.Join(", ", _actionDeck.ConvertAll(card => card.Name)));
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

        public List<ICard> GetActionDeck() => new List<ICard>(_actionDeck);
        public List<ICard> GetDiscardPile() => new List<ICard>(_actionDiscardPile);
        public List<ICard> GetActionHand() => new List<ICard>(_actionHand);

        public void RestoreActionDeck(List<CardData> cards)
        {
            _actionDeck.Clear();
            foreach (var card in cards)
            {
                // Reconstruct each card from serialized data
                _actionDeck.Add(GameStateManager.DeserializeCard(card));
            }
        }
        
        public void RestoreDiscardPile(List<CardData> cards)
        {
            _actionDiscardPile.Clear();
            foreach (var card in cards)
            {
                _actionDiscardPile.Add(GameStateManager.DeserializeCard(card));
            }
        }
        
        public void RestoreActionHand(List<CardData> cards)
        {
            _actionHand.Clear();
            foreach (var card in cards)
            {
                _actionHand.Add(GameStateManager.DeserializeCard(card));
            }
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

        private IEnumerator PlacePlantsSequentially(float delay = 0.4f)
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
                {
                    if (cardHolder)
                        cardHolder.ToggleCardHolder(plantController != null);
                }
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
            Destroy(plant.gameObject);

            var location = plantLocations.FirstOrDefault(slot =>
                slot.GetComponentsInChildren<PlantController>(true).Contains(plant));

            if (!location) yield break;
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
        /// Restores and places plants from saved game state sequentially, preserving transforms and afflictions.
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
                {
                    foreach (var aff in pd.priorAfflictions.Select(GetAfflictionFromString).Where(aff => aff != null))
                    {
                        plant.PriorAfflictions.Add(aff);
                    }
                }

                // Restore current afflictions (effects suppressed during the load process)
                plant.CurrentAfflictions.Clear();
                if (pd.currentAfflictions != null)
                {
                    foreach (var aff in pd.currentAfflictions.Select(GetAfflictionFromString).Where(aff => aff != null))
                    {
                        plant.AddAffliction(aff);
                    }
                }
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
                "Mildew"  => new PlantAfflictions.MildewAffliction(),
                "Thrips" => new PlantAfflictions.ThripsAffliction(),
                "Spider Mites" => new PlantAfflictions.SpiderMitesAffliction(),
                "Fungus Gnats" => new PlantAfflictions.FungusGnatsAffliction(),
                _ => null,
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
            {
                _actionHand.Add(_tutorialActionDeck[i % _tutorialActionDeck.Count].Clone());
            }

            // Clear all existing visualized cards
            ClearActionCardVisuals();

            StartCoroutine(DisplayActionCardsSequence());

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
                PlantAfflictions.IAffliction affliction = card.Affliction;
                if (affliction != null)
                {
                    // Check if the plant already has the affliction, Skip if it does.
                    if (plantController.HasAffliction(affliction)) continue;

                    plantController.AddAffliction(affliction);

                    if (affliction is PlantAfflictions.MildewAffliction)
                    {
                        var intensity = Random.Range(0.8f, 01f);
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

        /// Draws a new action hand by discarding the current hand and drawing the specified number of cards from the action deck.
        /// If the action deck is empty, it recycles the discard pile into the action deck. The drawn cards are then displayed in sequence.
        /// Optionally, log the state of the action hand, action deck, and discard the pile if debugging is enabled.
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

            StartCoroutine(DisplayActionCardsSequence());

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
            if (card != SelectedACard) return;
            SelectedACard = null;
            selectedACardClick3D = null;
        }

        /// Discards the currently selected action card from the action hand.
        /// This method removes the selected card from the action hand, adds it to the discard pile,
        /// and clears the reference to the selected card and its associated Click3D component.
        /// If no card is selected, the method does nothing.
        public void DiscardSelectedCard()
        {
            if (SelectedACard == null) return;
            _actionHand.Remove(SelectedACard);
            AddCardToDiscard(SelectedACard);
            SelectedACard = null;

            Destroy(selectedACardClick3D.gameObject);
            selectedACardClick3D = null;
        }

        private void AddCardToDiscard(ICard card)
        {
            _actionDiscardPile.Add(card);
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

        /// Displays action cards in a fanned-out sequence within the scene.
        /// This method creates GameObjects for each card in the action hand and positions them
        /// within a parent transform, arranging them in a visually pleasing fanned layout.
        /// Each card GameObject is initialized with its corresponding data through the CardView component.
        /// The sequence incorporates a delay between visual updates to allow for smooth animations.
        /// <returns>
        ///     An IEnumerator to enable the sequence to be executed as a coroutine, supporting time delays
        ///     for smooth visualization in Unity's coroutine system.
        /// </returns>
        private IEnumerator DisplayActionCardsSequence()
        {
            updatingActionDisplay = true;

            var cardsToDisplay = new List<ICard>(_actionHand);
            var totalCards = _actionHand.Count;
            const float totalFanAngle = -30f; // Total fan angle in degrees

            for (var i = 0; i < totalCards; i++)
            {
                var card = cardsToDisplay[i];
                var cardObj = Instantiate(cardPrefab, actionCardParent);
                var cardView = cardObj.GetComponent<CardView>();
                if (cardView)
                    cardView.Setup(card);
                else
                    Debug.LogWarning("Action Card Prefab is missing a Card View...");

                // Calculate a fanning rotation offset.
                var angleOffset = totalCards > 1
                    ? Mathf.Lerp(-totalFanAngle / 2, totalFanAngle / 2, (float)i / (totalCards - 1))
                    : 0f;

                // Use cardSpacing to determine how far apart they are.
                var xOffset = totalCards > 1
                    ? Mathf.Lerp(-cardSpacing, cardSpacing, (float)i / (totalCards - 1))
                    : 0f;

                // Set the local position and rotation.
                cardObj.transform.localPosition = new Vector3(xOffset, 0f, 0f);
                cardObj.transform.localRotation = Quaternion.Euler(0, 0, angleOffset);
                
                var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
                playerAudio.PlayOneShot(CardGameMaster.Instance.soundSystem.drawCard);

                yield return new WaitForSeconds(0.5f);
            }
            
            updatingActionDisplay = false;
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

            StartCoroutine(DisplayActionCardsSequence());
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionHand.ConvertAll(card => card.Name)));
            ScoreManager.SubtractMoneys(redrawCost);
            ScoreManager.UpdateMoneysText();
        }

        /// <summary>
        /// Destroys all GameObjects under actionCardParent.
        /// </summary>
        private void ClearActionCardVisuals()
        {
            foreach (Transform child in actionCardParent)
                Destroy(child.gameObject);
        }

        /// <summary>
        /// Refreshes the action hand display to match the current _actionHand list.
        /// Clears existing visuals and plays the display sequence for all cards.
        /// </summary>
        public void RefreshActionHandDisplay()
        {
            if (updatingActionDisplay) return;
            ClearActionCardVisuals();
            StartCoroutine(DisplayActionCardsSequence());
        }

        #endregion
    }
}

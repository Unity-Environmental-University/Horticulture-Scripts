using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
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
            new NeemOilBasic(),
            new FungicideBasic(),
            new InsecticideBasic(),
            new SoapyWaterBasic(),
            new Panacea()
        };

        #endregion

        #region Declare Decks
        
        private readonly List<ICard> _actionDeck = new();
        private readonly List<ICard> _actionDiscardPile = new();
        private static readonly List<ICard> PlantDeck = new();
        private static readonly List<ICard> AfflictionsDeck = new();

        #endregion

        #region Class Variables
        
        private readonly CardHand _afflictionHand = new("Afflictions Hand", AfflictionsDeck, PrototypeAfflictionsDeck);
        private readonly CardHand _plantHand = new("Plants Hand", PlantDeck, PrototypePlantsDeck);
        private readonly List<ICard> _actionHand = new();
        private TurnController _turnController;
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
        public int cardsDrawnPerTurn = 3;
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
            DontDestroyOnLoad(gameObject);
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
            {
                var duplicates = Random.Range(1, 5);
                for (var i = 0; i < duplicates; i++) _actionDeck.Add(card.Clone());
            }

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

        /// Generates a weighted random integer between the specified minimum and maximum values,
        /// with a bias towards the minimum value. The bias is achieved by squaring a randomly
        /// generated floating-point value, which increases the likelihood of selecting values
        /// closer to the minimum.
        /// <param name="min">The lower bound (inclusive) of the random number range.</param>
        /// <param name="max">The upper bound (exclusive) of the random number range.</param>
        /// <returns>
        ///     A random integer between the specified minimum and maximum bounds,
        ///     with a higher probability of being closer to the minimum.
        /// </returns>
        private static int WeightedRandom(int min, int max)
        {
            var t = Random.value; // rand float (between 0.0 - 1.0)
            t *= t; // Squares the float => closer to 0 (or the minimum)
            return min + Mathf.FloorToInt(t * (max - min)); // * this value can be 0 *
        }

        #endregion

        #region Plant Management

        /// Places plant cards at predefined locations within the scene.
        /// This method clears any existing plant objects, shuffles the plant deck, and
        /// determines a random number of plant cards to draw and place. Each drawn plant is associated
        /// with a location and instantiated using its corresponding prefab, which is retrieved according
        /// to the card type.
        /// The instantiated plant GameObjects are parented to their respective location transforms, and
        /// their PlantController components are assigned the corresponding card data.
        /// Does nothing if no plant locations are set or if the list of plant locations is empty.
        public void PlacePlants()
        {
            if (plantLocations == null || plantLocations.Count == 0) return;

            ClearAllPlants();
            _plantHand.DeckRandomDraw();
            ShuffleDeck(_plantHand.Deck);

            _plantHand.Clear();

            var max = Mathf.Min(plantLocations.Count, PlantDeck.Count);
            var cardsToDraw = Random.Range(1, max + 1);
            _plantHand.DrawCards(cardsToDraw);

            for (var i = 0; i < _plantHand.Count && i < plantLocations.Count; i++)
            {
                var prefab = GetPrefabForCard(_plantHand[i]);
                if (!prefab) continue;

                var plantLocation = plantLocations[i];

                // instantiate plant and get some components
                var plant = Instantiate(prefab, plantLocation.position, plantLocation.rotation);
                plant.transform.SetParent(plantLocation);
                plant.GetComponent<PlantController>().PlantCard = _plantHand[i];
            }
        }

        /// Clears all plant objects from their designated locations by destroying all
        /// associated GameObjects with `PlantController` components at each location.
        /// Ensures the randomness of subsequent plant arrangements by invoking a
        /// random draw on the plant deck. Outputs debug messages if enabled.
        private void ClearAllPlants()
        {
            foreach (var child in plantLocations
                         .Select(slot => slot.GetComponentsInChildren<PlantController>(true))
                         .SelectMany(children => children)) Destroy(child.gameObject);

            _plantHand.DeckRandomDraw();

            if (debug) Debug.Log("All plants cleared");
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

        #region Afflictions Management

        /// Draws a random number of affliction cards from the afflictions deck, with the number
        /// of cards to be drawn determined using a weighted random value based on the current plant hand size.
        /// The method first shuffles the affliction deck, clears any existing cards in the afflictions hand,
        /// and then draws a specified number of cards while updating the affliction hand.
        /// Logs information about the drawn cards if debugging is enabled,
        /// and applies the newly drawn affliction deck to the game.
        public void DrawAfflictions()
        {
            _afflictionHand.DeckRandomDraw();
            ShuffleDeck(_afflictionHand.Deck);
            _afflictionHand.Clear();

            var max = _plantHand.Count;
            var cardsToDraw = WeightedRandom(1, max);

            _afflictionHand.DrawCards(cardsToDraw);

            if (debug)
                Debug.Log("Afflictions Drawn: " + string.Join(", ", _afflictionHand.ConvertAll(card => card.Name)));
            ApplyAfflictionDeck();
        }

        /// Applies afflictions from the affliction deck to available plants in the scene.
        /// For each card in the affliction hand, an affliction is applied to a randomly chosen
        /// plant that is not already afflicted, up to the minimum number of either available
        /// plants or affliction cards.
        /// If the affliction is a mildew type, an intensity value is generated and set for the plant.
        /// Each plant's shaders are flagged for updates after the affliction is applied.
        /// Optionally log Debug messages, including details about the applied afflictions.
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

        /// Draws the player's action hand by clearing the current hand, recycling cards
        /// from the discard pile into the deck if necessary, and then drawing a specified
        /// number of cards from the action deck. Repositions and visualizes the drawn cards
        /// within the scene using the DisplayActionCardsSequence method.
        /// The discard pile is shuffled back into the action deck if the deck does not
        /// contain enough cards to complete the draw, and debug messages are logged if enabled.
        public void DrawActionHand()
        {
            // Create a temporary list to avoid modifying _actionHand while iterating
            var cardsToDiscard = new List<ICard>(_actionHand);
            foreach (var card in cardsToDiscard)
            {
                DiscardActionCard(card, true);
            }

            _actionHand.Clear();

            // Clear all existing visualized cards in the action card parent
            foreach (Transform child in actionCardParent)
            {
                Destroy(child.gameObject);
            }

            for (var i = 0; i < cardsDrawnPerTurn; i++)
            {
                // Handle case when action deck or discard pile is empty
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
        }

        /// Discards the specified action card by removing it from the action hand.
        /// Optionally adds the card to the discard pile if the parameter is set to true.
        /// Resets the currently selected action card if the discarded card matches the selected one.
        /// <param name="card">The action card to be discarded.</param>
        /// <param name="addToDiscard">Whether to add the discarded card to the discard pile.</param>
        public void DiscardActionCard(ICard card, bool addToDiscard)
        {
            _actionHand.Remove(card);
            if (addToDiscard) _actionDiscardPile.Add(card);
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
            _actionDiscardPile.Add(SelectedACard);
            SelectedACard = null;

            Destroy(selectedACardClick3D.gameObject);
            selectedACardClick3D = null;
        }

        /// Clears the action hand, action deck, and discard pile, and resets the action deck for the next sequence.
        /// This method removes all current action cards from the action hand and discard pile,
        /// destroys any card GameObjects under the action card parent transform, and reinitializes
        /// the action deck to its original shuffled state.
        /// Logs the state of the action hand, action deck, and discard pile if debugging is enabled.
        public void ClearActionHand()
        {
            _actionHand.Clear();
            _actionDeck.Clear();
            _actionDiscardPile.Clear();

            foreach (Transform child in actionCardParent) Destroy(child.gameObject);

            InitializeActionDeck();

            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionHand.ConvertAll(card => card.Name)));
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionDeck.ConvertAll(card => card.Name)));
            if (debug) Debug.Log("Action Hand: " + string.Join(", ", _actionDiscardPile.ConvertAll(card => card.Name)));
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
            var totalCards = _actionHand.Count;
            const float totalFanAngle = -30f; // Total fan angle in degrees

            for (var i = 0; i < totalCards; i++)
            {
                var card = _actionHand[i];
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

                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion
    }
}
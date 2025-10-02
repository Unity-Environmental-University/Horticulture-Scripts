using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Rendering;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Manages outline visual feedback for cardholders based on card selection state.
    ///     Automatically highlights valid placement targets when a card is selected.
    /// </summary>
    public class CardSelectionOutlineController : MonoBehaviour
    {
        private readonly List<CardHolderOutlineBinding> _placedCardHolderOutlines = new();

        /// <summary>
        ///     Cached reference to the RetainedCardHolder to avoid repeated scene searches.
        /// </summary>
        private RetainedCardHolder _cachedRetainedHolder;

        private CardGameMaster _cardGameMaster;
        private DeckManager _deckManager;

        /// <summary>
        ///     Tracks the outline controller for the singleton RetainedCardHolder instance.
        /// </summary>
        private RetainedCardHolderOutlineBinding _retainedCardHolderOutline;

        private void Awake()
        {
            TryCacheDependencies();
        }

        private void Start()
        {
            if (!TryCacheDependencies())
            {
                Debug.LogWarning(
                    $"CardSelectionOutlineController missing required components on {gameObject.name}. Disabling outline updates.");
                enabled = false;
                return;
            }

            CacheCardHolderOutlines();
            _deckManager.SelectedCardChanged += HandleCardSelectionChanged;
            HandleCardSelectionChanged(_deckManager.SelectedCard);
        }

        private void OnDestroy()
        {
            if (_deckManager)
                _deckManager.SelectedCardChanged -= HandleCardSelectionChanged;
        }

        /// <summary>
        ///     Scans for all PlacedCardHolder instances and the singleton RetainedCardHolder,
        ///     caching their OutlineController components.
        ///     Ensures each OutlineController only affects its local cardholder's renderers.
        /// </summary>
        private void CacheCardHolderOutlines()
        {
            _placedCardHolderOutlines.Clear();
            if (_cardGameMaster == null || _cardGameMaster.cardHolders == null) return;

            foreach (var holder in _cardGameMaster.cardHolders)
                TryAddPlacedCardHolderOutline(holder);

            // Cache the RetainedCardHolder reference to avoid repeated scene searches
            if (_cachedRetainedHolder == null)
                _cachedRetainedHolder = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);

            if (_cachedRetainedHolder == null) return;
            var outline = FindOutlineController(_cachedRetainedHolder);
            if (outline == null) return;
            EnsureLocalOutlineScope(outline);
            outline.SetOutline(false);
            _retainedCardHolderOutline = new RetainedCardHolderOutlineBinding(_cachedRetainedHolder, outline);
        }

        /// <summary>
        ///     Handles card selection changes by updating outline visibility based on card compatibility.
        /// </summary>
        private void HandleCardSelectionChanged(ICard card)
        {
            EnsureCardHolderOutlines();

            if (card == null)
            {
                DisableAllOutlines();
                return;
            }

            UpdatePlacedCardHolderOutlines(card);
            UpdateRetainedCardHolderOutline(card);
        }

        /// <summary>
        ///     Disables all outlines when no card is selected.
        /// </summary>
        private void DisableAllOutlines()
        {
            // Iterate in reverse to safely remove items during iteration
            for (var i = _placedCardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _placedCardHolderOutlines[i];
                if (!binding.Holder || !binding.Outline)
                {
                    _placedCardHolderOutlines.RemoveAt(i);
                    continue;
                }

                binding.Outline.SetOutline(false);
            }

            var retainedOutline = _retainedCardHolderOutline.Outline;
            if (retainedOutline)
                retainedOutline.SetOutline(false);
        }

        /// <summary>
        ///     Updates PlacedCardHolder outlines based on card compatibility.
        /// </summary>
        private void UpdatePlacedCardHolderOutlines(ICard card)
        {
            if (card == null) return;

            // Iterate in reverse to safely remove items during iteration
            for (var i = _placedCardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _placedCardHolderOutlines[i];
                var holder = binding.Holder;
                var outline = binding.Outline;

                if (!holder || !outline)
                {
                    _placedCardHolderOutlines.RemoveAt(i);
                    continue;
                }

                var enable = holder.CanAcceptCard(card);
                outline.SetOutline(enable);
            }
        }

        /// <summary>
        ///     Updates RetainedCardHolder outline based on card compatibility and availability.
        /// </summary>
        private void UpdateRetainedCardHolderOutline(ICard card)
        {
            if (card == null) return;

            var holder = _retainedCardHolderOutline.Holder;
            var outline = _retainedCardHolderOutline.Outline;

            if (holder && outline)
            {
                var enable = holder.HeldCard == null && holder.CanAcceptCard(card);
                outline.SetOutline(enable);
            }
            else if (holder || outline)
            {
                // Partial destruction detected - clear the binding
                _retainedCardHolderOutline = default;
                _cachedRetainedHolder = null;
            }
        }

        /// <summary>
        ///     Attempts to cache the CardGameMaster and DeckManager dependencies; returns true when both are present.
        /// </summary>
        private bool TryCacheDependencies()
        {
            if (!_cardGameMaster)
                TryGetComponent(out _cardGameMaster);

            if (!_deckManager)
                TryGetComponent(out _deckManager);

            return _cardGameMaster && _deckManager;
        }

        /// <summary>
        ///     Ensures the outline cache is up-to-date by removing stale entries and adding new holders.
        /// </summary>
        private void EnsureCardHolderOutlines()
        {
            if (!_cardGameMaster || _cardGameMaster.cardHolders == null) return;

            // Iterate in reverse to safely remove items during iteration
            for (var i = _placedCardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _placedCardHolderOutlines[i];
                if (!binding.Holder || !binding.Outline)
                    _placedCardHolderOutlines.RemoveAt(i);
            }

            // Add new PlacedCardHolders (optimized to avoid LINQ allocations)
            foreach (var holder in _cardGameMaster.cardHolders)
            {
                if (!holder) continue;

                var alreadyTracked = false;
                foreach (var binding in _placedCardHolderOutlines)
                    if (binding.Holder == holder)
                    {
                        alreadyTracked = true;
                        break;
                    }

                if (!alreadyTracked)
                    TryAddPlacedCardHolderOutline(holder);
            }

            // Ensure RetainedCardHolder is tracked
            if (_retainedCardHolderOutline.Holder && _retainedCardHolderOutline.Outline) return;

            // Handle partial destruction - clear binding if one component is null
            if (_retainedCardHolderOutline.Holder || _retainedCardHolderOutline.Outline)
            {
                _retainedCardHolderOutline = default;
                _cachedRetainedHolder = null;
            }

            // Use cached reference to avoid repeated scene searches
            if (!_cachedRetainedHolder)
                _cachedRetainedHolder = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);

            if (!_cachedRetainedHolder) return;
            var outline = FindOutlineController(_cachedRetainedHolder);
            if (!outline) return;
            EnsureLocalOutlineScope(outline);
            outline.SetOutline(false);
            _retainedCardHolderOutline = new RetainedCardHolderOutlineBinding(_cachedRetainedHolder, outline);
        }

        /// <summary>
        ///     Attempts to add a PlacedCardHolder to the outline cache if it has an OutlineController.
        /// </summary>
        private void TryAddPlacedCardHolderOutline(PlacedCardHolder holder)
        {
            if (!holder) return;

            var outline = FindOutlineController(holder);
            if (!outline) return;

            EnsureLocalOutlineScope(outline);
            outline.SetOutline(false);
            _placedCardHolderOutlines.Add(new CardHolderOutlineBinding(holder, outline));
        }

        /// <summary>
        ///     Finds an OutlineController on the given component or its children.
        /// </summary>
        private static OutlineController FindOutlineController(Component component)
        {
            if (!component) return null;

            return component.GetComponent<OutlineController>() ??
                   component.GetComponentInChildren<OutlineController>(true);
        }

        /// <summary>
        ///     Configures an OutlineController to only affect local renderers, not the entire scene.
        /// </summary>
        private static void EnsureLocalOutlineScope(OutlineController outline)
        {
            if (!outline) return;
            outline.SetLocalScope();
        }

        private readonly struct CardHolderOutlineBinding
        {
            public CardHolderOutlineBinding(PlacedCardHolder holder, OutlineController outline)
            {
                Holder = holder;
                Outline = outline;
            }

            public PlacedCardHolder Holder { get; }
            public OutlineController Outline { get; }
        }

        /// <summary>
        ///     Binds a RetainedCardHolder component to its associated OutlineController.
        /// </summary>
        private readonly struct RetainedCardHolderOutlineBinding
        {
            public RetainedCardHolderOutlineBinding(RetainedCardHolder holder, OutlineController outline)
            {
                Holder = holder;
                Outline = outline;
            }

            public RetainedCardHolder Holder { get; }
            public OutlineController Outline { get; }
        }
    }
}

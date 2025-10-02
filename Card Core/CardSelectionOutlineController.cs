using System.Collections.Generic;
using System.Linq;
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
        private readonly List<CardHolderOutlineBinding> _cardHolderOutlines = new();
        private CardGameMaster _cardGameMaster;
        private DeckManager _deckManager;

        private void Awake()
        {
            _cardGameMaster = GetComponent<CardGameMaster>();
            _deckManager = GetComponent<DeckManager>();

            if (_cardGameMaster && _deckManager) return;
            Debug.LogError(
                $"CardSelectionOutlineController requires both CardGameMaster and DeckManager components on {gameObject.name}");
            enabled = false;
        }

        private void Start()
        {
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
        ///     Scans for all PlacedCardHolder instances and caches their OutlineController components.
        ///     Ensures each OutlineController only affects its local cardholder's renderers.
        /// </summary>
        private void CacheCardHolderOutlines()
        {
            _cardHolderOutlines.Clear();
            if (_cardGameMaster == null || _cardGameMaster.cardHolders == null) return;

            foreach (var holder in _cardGameMaster.cardHolders)
                TryAddCardHolderOutline(holder);
        }

        /// <summary>
        ///     Handles card selection changes by updating outline visibility based on card compatibility.
        /// </summary>
        private void HandleCardSelectionChanged(ICard card)
        {
            EnsureCardHolderOutlines();

            var hasSelection = card != null;
            for (var i = _cardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _cardHolderOutlines[i];
                if (!binding.Holder || !binding.Outline)
                {
                    _cardHolderOutlines.RemoveAt(i);
                    continue;
                }

                var enable = hasSelection && binding.Holder.CanAcceptCard(card);
                binding.Outline.SetOutline(enable);
            }
        }

        /// <summary>
        ///     Ensures the outline cache is up-to-date by removing stale entries and adding new holders.
        /// </summary>
        private void EnsureCardHolderOutlines()
        {
            if (!_cardGameMaster || _cardGameMaster.cardHolders == null) return;

            // Remove stale entries
            for (var i = _cardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _cardHolderOutlines[i];
                if (!binding.Holder || !binding.Outline)
                    _cardHolderOutlines.RemoveAt(i);
            }

            // Add new holders
            foreach (var holder in from holder in _cardGameMaster.cardHolders
                     where holder
                     let alreadyTracked = _cardHolderOutlines.Exists(b => b.Holder == holder)
                     where !alreadyTracked
                     select holder)
                TryAddCardHolderOutline(holder);
        }

        /// <summary>
        ///     Attempts to add a cardholder to the outline cache if it has an OutlineController.
        /// </summary>
        private void TryAddCardHolderOutline(PlacedCardHolder holder)
        {
            if (!holder) return;

            var outline = holder.GetComponent<OutlineController>() ??
                          holder.GetComponentInChildren<OutlineController>(true);
            if (!outline) return;

            EnsureLocalOutlineScope(outline);
            outline.SetOutline(false);
            _cardHolderOutlines.Add(new CardHolderOutlineBinding(holder, outline));
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
    }
}
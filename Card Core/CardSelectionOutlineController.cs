using System;
using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Rendering;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    /// Manages outline visual feedback for card holders based on card selection state.
    /// Automatically highlights valid placement targets when a card is selected.
    /// </summary>
    public class CardSelectionOutlineController : MonoBehaviour
    {
        private CardGameMaster _cardGameMaster;
        private DeckManager _deckManager;
        private readonly List<CardHolderOutlineBinding> _cardHolderOutlines = new();

        private void Awake()
        {
            _cardGameMaster = GetComponent<CardGameMaster>();
            _deckManager = GetComponent<DeckManager>();

            if (!_cardGameMaster || !_deckManager)
            {
                Debug.LogError($"CardSelectionOutlineController requires both CardGameMaster and DeckManager components on {gameObject.name}");
                enabled = false;
            }
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
        /// Scans for all PlacedCardHolder instances and caches their OutlineController components.
        /// </summary>
        private void CacheCardHolderOutlines()
        {
            _cardHolderOutlines.Clear();
            if (_cardGameMaster?.cardHolders == null) return;

            foreach (var holder in _cardGameMaster.cardHolders)
            {
                if (!holder) continue;

                var outline = holder.GetComponent<OutlineController>() ??
                              holder.GetComponentInChildren<OutlineController>(true);
                if (!outline) continue;

                outline.SetOutline(false);
                _cardHolderOutlines.Add(new CardHolderOutlineBinding(holder, outline));
            }
        }

        /// <summary>
        /// Handles card selection changes by updating outline visibility based on card compatibility.
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
        /// Ensures the outline cache is up-to-date by removing stale entries and adding new holders.
        /// </summary>
        private void EnsureCardHolderOutlines()
        {
            if (_cardGameMaster?.cardHolders == null) return;

            for (var i = _cardHolderOutlines.Count - 1; i >= 0; i--)
            {
                var binding = _cardHolderOutlines[i];
                if (binding.Holder && binding.Outline) continue;
                _cardHolderOutlines.RemoveAt(i);
            }

            foreach (var holder in _cardGameMaster.cardHolders)
            {
                if (!holder) continue;
                var alreadyTracked = _cardHolderOutlines.Exists(binding => binding.Holder == holder);
                if (alreadyTracked) continue;

                var outline = holder.GetComponent<OutlineController>() ??
                              holder.GetComponentInChildren<OutlineController>(true);
                if (!outline) continue;

                outline.SetOutline(false);
                _cardHolderOutlines.Add(new CardHolderOutlineBinding(holder, outline));
            }
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

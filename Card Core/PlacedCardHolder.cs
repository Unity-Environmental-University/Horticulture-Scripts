using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlacedCardHolder : MonoBehaviour
    {
        private DeckManager _deckManager;
        private readonly List<Transform> _cardSlotsList = new();
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<ICard> _placedCardsList = new();

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;

            foreach (Transform child in transform)
            {
                if (child.GetComponent<Click3D>() != null)
                {
                    _cardSlotsList.Add(child);
                }
            }
        }

        /// <summary>
        /// Places a specified card object onto the first available child slot in a defined list of card slots.
        /// </summary>
        /// <param name="card">The card object to be placed in a card slot. Must implement the ICard interface.</param>
        public void PlaceCardOnChild(ICard card)
        {
            if (!_cardSlotsList.Any() || card == null || _deckManager.selectedACardClick3D == null) return;

            foreach (var slot in _cardSlotsList.Take(_cardSlotsList.Count))
            {
                var newCardObj = CreateCardObject(_deckManager.selectedACardClick3D.gameObject, slot, card);
                _placedCardsList.Add(card);
            }
        }

        /// <summary>
        /// Creates a new card GameObject based on a specified template, assigns it specified properties,
        /// and places it under the specified parent transform.
        /// </summary>
        /// <param name="template">The GameObject template to instantiate the new card from.</param>
        /// <param name="parent">The parent transform under which the new card GameObject will be placed.</param>
        /// <param name="card">The card data implementing the ICard interface to associate with the new GameObject.</param>
        /// <returns>Returns the newly instantiated card GameObject.</returns>
        private static GameObject CreateCardObject(GameObject template, Transform parent, ICard card)
        {
            var cardObj = Instantiate(template, parent, true);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localRotation = Quaternion.identity;
            cardObj.GetComponent<Click3D>().Card = card;
            return cardObj;
        }
    }
}
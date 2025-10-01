using System.Collections;
using _project.Scripts.Classes;
using DG.Tweening;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class RetainedCardHolder : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [Header("Card Type Restrictions")]
        [SerializeField] private CardHolderType acceptedCardType = CardHolderType.ActionOnly;
        private DeckManager _deckManager;
        private GameObject cardGoClone;
        private ICard _heldCard;
        public ICard HeldCard
        {
            get => _heldCard;
            set
            {
                if (value == null)
                {
                    _heldCard = null;
                    return;
                }

                if (!CanAcceptCard(value))
                {
                    Debug.LogWarning(
                        $"RetainedCardHolder of type {acceptedCardType} rejected card assignment: {value?.Name ?? "Unknown"}.");
                    return;
                }

                _heldCard = value;
            }
        }
        public bool hasPaidForCard;
        public bool isCardLocked;
        private MeshRenderer _buttonRenderer;

        private bool HasCard => HeldCard != null;

        public bool CanAcceptCard(ICard card)
        {
            if (card == null) return true;

            return acceptedCardType switch
            {
                CardHolderType.ActionOnly => card is not ILocationCard,
                CardHolderType.LocationOnly => card is ILocationCard,
                _ => true
            };
        }

        public void SetCardHolderType(CardHolderType cardType)
        {
            acceptedCardType = cardType;
        }

        public CardHolderType GetCardHolderType()
        {
            return acceptedCardType;
        }

        private void Start()
        {
            _buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        public void HoldSelectedCard()
        {
            var cgm = CardGameMaster.Instance;
            var selectedCard = _deckManager.selectedACard;
            var selectedClick3D = _deckManager.selectedACardClick3D;

            if (selectedCard == null || selectedClick3D == null)
            {
                if (HasCard)
                    SelectHeldCard();
                return;
            }

            if (!CanAcceptCard(selectedCard))
            {
                Debug.LogWarning(
                    $"RetainedCardHolder of type {acceptedCardType} cannot accept card: {selectedCard?.Name ?? "Unknown"}.");
                return;
            }

            if (HasCard && HeldCard != selectedCard)
                return;

            if (HeldCard == selectedCard)
            {
                SelectHeldCard();
                return;
            }

            HeldCard = selectedCard;

            cardGoClone = Instantiate(cardPrefab, transform);
            cardGoClone.name = cardPrefab.name;

            var cardView = cardGoClone.GetComponent<CardView>();
            if (cardView)
                cardView.Setup(HeldCard);

            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.cardView = cardView;
                click3D.isEnabled = true;
                click3D.selected = false;
                click3D.isRetainedItem = true;
                click3D.RefreshState();
            }
            
            cgm.playerHandAudioSource.PlayOneShot(cgm.soundSystem.placeCard);

            cardGoClone.transform.SetParent(transform, false);
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.8f;

            foreach (var r in selectedClick3D.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            selectedClick3D.isEnabled = false;

            _deckManager.ClearSelectedCard();

            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = false;
            var scoreManager = cgm.scoreManager;
            if (!hasPaidForCard && HeldCard.Value != null)
            {
                ScoreManager.SubtractMoneys(Mathf.Abs(HeldCard.Value.Value));
                hasPaidForCard = true;
            }
            scoreManager.CalculateTreatmentCost();
            isCardLocked = true;
        }

        private void SelectHeldCard()
        {
            if (isCardLocked) return;
            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D == null) return;

            if (_deckManager.selectedACard == HeldCard)
            {
                _deckManager.ClearSelectedCard();
                click3D.selected = false;
                StartCoroutine(click3D.AnimateCardBack());
            }
            else
            {
                if (_deckManager.selectedACardClick3D)
                {
                    _deckManager.selectedACardClick3D.selected = false;
                    StartCoroutine(_deckManager.selectedACardClick3D.AnimateCardBack());
                }

                ShowCard();

                click3D.selected = true;
                click3D.RefreshState();

                _deckManager.SetSelectedCard(click3D, HeldCard);
                CardGameMaster.Instance.playerHandAudioSource.PlayOneShot(
                    CardGameMaster.Instance.soundSystem.selectCard);

                StartCoroutine(AnimateCard());
            }
        }

        private IEnumerator AnimateCard(float animationDuration = 0.2f)
        {
            cardGoClone.transform
                .DOScale(Vector3.one, animationDuration)
                .SetLink(cardGoClone, LinkBehaviour.KillOnDisable);
            yield return new WaitForSeconds(animationDuration);
        }

        private void ShowCard()
        {
            if (cardGoClone == null) return;
            foreach (var renderer1 in cardGoClone.GetComponentsInChildren<Renderer>())
                renderer1.enabled = true;
        }

        public void ReclaimCard(GameObject returnedCardGo)
        {
            // Safely destroy the old retained visual if it exists
            if (cardGoClone != null)
            {
                Destroy(cardGoClone);
                cardGoClone = null;
            }

            var returnedCardView = returnedCardGo.GetComponent<CardView>();
            var returnedCard = returnedCardView?.GetCard();

            if (returnedCard == null)
            {
                Debug.LogWarning("RetainedCardHolder received a null card during reclaim; clearing held state.");
                Destroy(returnedCardGo);
                ClearHeldCard();
                return;
            }

            if (!CanAcceptCard(returnedCard))
            {
                Debug.LogWarning(
                    $"RetainedCardHolder of type {acceptedCardType} cannot accept returned card: {returnedCard?.Name ?? "Unknown"}.");
                Destroy(returnedCardGo);
                ClearHeldCard();
                return;
            }

            HeldCard = returnedCard;

            if (cardPrefab == null)
            {
                Debug.LogWarning("Held card has no valid prefab.");
                return;
            }

            cardGoClone = Instantiate(cardPrefab, transform);
            cardGoClone.name = cardPrefab.name;

            var cardView = cardGoClone.GetComponent<CardView>();
            if (cardView) cardView.Setup(HeldCard);

            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.cardView = cardView;
                click3D.isEnabled = true;
                click3D.selected = false;
                click3D.handItem = false;
                click3D.RefreshState();
            }

            cardGoClone.transform.SetParent(transform, false);
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.9f;

            Destroy(returnedCardGo);
        }

        public void ClearHeldCard()
        {
            HeldCard = null;
            hasPaidForCard = false;

            if (cardGoClone)
            {
                Destroy(cardGoClone);
                cardGoClone = null;
            }

            if (_buttonRenderer)
                _buttonRenderer.enabled = true;
        }
        
        /// <summary>
        /// Restores the visual representation of a retained card after loading from save data
        /// </summary>
        public void RestoreCardVisual()
        {
            if (HeldCard == null) return;

            if (!CanAcceptCard(HeldCard))
            {
                Debug.LogWarning(
                    $"RetainedCardHolder of type {acceptedCardType} cannot restore card: {HeldCard?.Name ?? "Unknown"}.");
                ClearHeldCard();
                return;
            }

            // Clear any existing visual first
            if (cardGoClone != null)
            {
                Destroy(cardGoClone);
                cardGoClone = null;
            }
            
            // Create visual representation similar to HoldSelectedCard()
            var cgm = CardGameMaster.Instance;
            cardGoClone = Instantiate(cgm.actionCardPrefab, transform);
            
            var cardView = cardGoClone.GetComponent<CardView>();
            if (cardView)
                cardView.Setup(HeldCard);
            
            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.onClick3D.RemoveAllListeners();
                click3D.onClick3D.AddListener(SelectHeldCard);
                // Disable pop-up/resize on hover for retained cards
                click3D.handItem = false;
                click3D.isEnabled = !isCardLocked;
                click3D.RefreshState();
            }
            
            // Position and scale the card
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.8f;
            
            // Hide the retained slot button
            if (_buttonRenderer)
                _buttonRenderer.enabled = false;
            
            // Update cost display
            cgm.scoreManager.CalculateTreatmentCost();
        }

        private void OnDestroy()
        {
            if (cardGoClone)
            {
                cardGoClone.transform.DOKill();
            }
            transform.DOKill();
        }
    }
}

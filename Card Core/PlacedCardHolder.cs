using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Handlers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace _project.Scripts.Card_Core
{
    public enum CardHolderType
    {
        Any,
        ActionOnly,
        LocationOnly
    }

    public class PlacedCardHolder : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int Surface = Shader.PropertyToID("_Surface");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        [Header("Card Type Restrictions")]
        [SerializeField] private CardHolderType acceptedCardType = CardHolderType.Any;
        
        public Click3D placedCardClick3D;
        public CardView placedCardView;
        private DeckManager _deckManager;
        [SerializeField] private SpotDataHolder spotDataHolder;
        private float _lastClickTime = -1f;
        private EfficacyDisplayHandler _efficacyDisplay;
        [Header("Placement Visuals")]
        [SerializeField] private Vector3 placedCardScaleMultiplier = Vector3.one;
        [Tooltip("Local-space rotation offset to apply after the default lay-flat rotation (-90,0,0)")]
        [SerializeField] private Vector3 placedCardRotationOffsetEuler = Vector3.zero;
        [Tooltip("Local-space offset applied to the placed clone to avoid clipping into the holder surface.")]
        [SerializeField] private Vector3 placedCardPositionOffsetLocal = new Vector3(0f, 0.002f, 0f);
        [Header("Hover Behavior")]
        [Tooltip("Disable hover pop animation for cloned cards placed in this holder.")]
        [SerializeField] private bool disableHoverOnPlacedCard = true;
        
        [Header("Hover Preview")]
        [Tooltip("When a compatible card is selected, hovering this holder shows a ghost preview.")]
        [SerializeField] private bool enableHoverPreview = true;

        [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.35f;
        
        private Click3D _holderClick3D;
        private GameObject _previewCardClone;
        private bool _isHovered;
        private List<Material> _previewMaterials;

        private int _lastPlacementFrame = -1;
        private float _lastPlacementTime = -1f;
        private ScoreManager _scoreManager;
        public ICard placedCard;
        public bool HoldingCard => placedCardClick3D;

        /// <summary>
        /// The turn number when the current card was placed. -1 if no card is held.
        /// Used to determine if redraw should be blocked (can't redraw if cards placed this turn).
        /// NOTE: Not currently persisted in save/load system - will reset to -1 on a game load.
        /// </summary>
        [FormerlySerializedAs("_placementTurn")] [SerializeField, Tooltip("The turn number when the current card was placed. -1 if no card is held.")]
        private int placementTurn = -1;
        public int PlacementTurn { get => placementTurn; private set => placementTurn = value; }

        private static CardGameMaster Cgm => CardGameMaster.Instance;

        private void Start()
        {
            if (CardGameMaster.Instance == null)
            {
                Debug.LogError("[PlacedCardHolder] CardGameMaster.Instance is null in Start. " +
                              "Ensure CardGameMaster exists in scene and initializes first.", this);
                return;
            }

            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;

            if (_deckManager == null)
            {
                Debug.LogError("[PlacedCardHolder] DeckManager is null after CardGameMaster initialization.", this);
            }

            if (spotDataHolder)
                spotDataHolder.RegisterCardHolder(this);

            SubscribeHoverPreview();
        }

        private void SubscribeHoverPreview()
        {
            if (!enableHoverPreview || _deckManager == null) return;

            _deckManager.SelectedCardChanged += OnSelectedCardChanged;
            _holderClick3D = ResolveHolderClick3D();
            if (!_holderClick3D) return;
            _holderClick3D.HoverEntered += OnHolderHoverEnter;
            _holderClick3D.HoverExited += OnHolderHoverExit;
        }

        private Click3D ResolveHolderClick3D()
        {
            var click3Ds = GetComponentsInChildren<Click3D>(true);
            foreach (var candidate in click3Ds)
            {
                if (candidate == null) continue;
                if (candidate.handItem || candidate.isSticker) continue;
                if (candidate.GetComponent<CardView>() != null) continue;
                return candidate;
            }

            if (click3Ds.Length > 0)
            {
                Debug.LogWarning($"[PlacedCardHolder] No valid holder Click3D found for '{name}'. " +
                                "Hover preview may not work correctly.", this);
            }

            return null;
        }

        private void OnHolderHoverEnter(Click3D click3D)
        {
            _isHovered = true;
            UpdatePreview();
        }

        private void OnHolderHoverExit(Click3D click3D)
        {
            _isHovered = false;
            ClearPreview();
        }

        private void OnSelectedCardChanged(ICard card)
        {
            if (!_isHovered)
            {
                ClearPreview();
                return;
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            ClearPreview();

            if (!_deckManager) return;

            var selectedCardClick3D = _deckManager.selectedACardClick3D;
            var selectedCard = _deckManager.selectedACard;

            if (!selectedCardClick3D || selectedCard == null) return;
            if (HoldingCard) return;

            var plant = ResolvePlantForDisplay();
            if (!plant || plant.PlantCard == null || plant.PlantCard.Value <= 0) return;

            if (!CanAcceptCard(selectedCard)) return;

            var sourceLocalScale = selectedCardClick3D.transform.localScale;
            var sourceLossyScale = selectedCardClick3D.transform.lossyScale;
            var parentLossyScale = transform.lossyScale;
            var resolvedLocalScale = new Vector3(
                !Mathf.Approximately(parentLossyScale.x, 0f)
                    ? sourceLossyScale.x / parentLossyScale.x
                    : sourceLocalScale.x,
                !Mathf.Approximately(parentLossyScale.y, 0f)
                    ? sourceLossyScale.y / parentLossyScale.y
                    : sourceLocalScale.y,
                !Mathf.Approximately(parentLossyScale.z, 0f)
                    ? sourceLossyScale.z / parentLossyScale.z
                    : sourceLocalScale.z
            );

            var resolvedLocalRotation =
                Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(placedCardRotationOffsetEuler);

            var cardClone = Instantiate(selectedCardClick3D.gameObject, transform);

            var cardViewClone = cardClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(selectedCard);

            cardClone.transform.SetParent(transform, false);
            cardClone.transform.localPosition = placedCardPositionOffsetLocal;
            cardClone.transform.localRotation = resolvedLocalRotation;
            cardClone.transform.localScale = Vector3.Scale(resolvedLocalScale, placedCardScaleMultiplier);

            DisablePreviewInteraction(cardClone);
            ApplyPreviewVisuals(cardClone);

            _previewCardClone = cardClone;
        }

        private static void DisablePreviewInteraction(GameObject previewClone)
        {
            var click3D = previewClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.isEnabled = false;
                click3D.enabled = false;
                click3D.onClick3D?.RemoveAllListeners();
            }

            var colliders = previewClone.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
                collider.enabled = false;
        }

        private void ApplyPreviewVisuals(GameObject previewClone)
        {
            _previewMaterials = new List<Material>();
            var renderers = previewClone.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;

                var material = r.material;
                if (!material) continue;
                _previewMaterials.Add(material);
                MakeMaterialTransparent(material, previewAlpha);
            }
        }

        private static void MakeMaterialTransparent(Material material, float alpha)
        {
            if (material.HasProperty(BaseColor))
            {
                var baseColor = material.GetColor(BaseColor);
                baseColor.a = alpha;
                material.SetColor(BaseColor, baseColor);
            }

            if (material.HasProperty(Color1))
            {
                var color = material.GetColor(Color1);
                color.a = alpha;
                material.SetColor(Color1, color);
            }

            if (material.HasProperty(Surface))
            {
                material.SetFloat(Surface, 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            }

            if (material.HasProperty(Blend))
                material.SetFloat(Blend, 0f);

            if (material.HasProperty(AlphaClip))
                material.SetFloat(AlphaClip, 0f);

            if (material.HasProperty(ZWrite))
                material.SetFloat(ZWrite, 0f);

            if (material.HasProperty(SrcBlend))
                material.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);

            if (material.HasProperty(DstBlend))
                material.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);

            material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private void ClearPreview()
        {
            if (!_previewCardClone) return;

            Destroy(_previewCardClone);
            _previewCardClone = null;

            if (_previewMaterials == null) return;
            foreach (var mat in _previewMaterials.Where(mat => mat)) Destroy(mat);
            _previewMaterials.Clear();
            _previewMaterials = null;
        }

        private EfficacyDisplayHandler GetEfficacyDisplay()
        {
            if (_efficacyDisplay) return _efficacyDisplay;

            _efficacyDisplay = GetComponentInChildren<EfficacyDisplayHandler>(true);
            if (_efficacyDisplay) return _efficacyDisplay;

            if (transform.parent)
                _efficacyDisplay = transform.parent.GetComponentInChildren<EfficacyDisplayHandler>(true);

            return _efficacyDisplay;
        }

        private PlantController ResolvePlantForDisplay()
        {
            var root = transform.parent ? transform.parent : transform;
            var plant = root.GetComponentInChildren<PlantController>(true);
            return plant ? plant : root.GetComponentInParent<PlantController>();
        }

        public void RefreshEfficacyDisplay()
        {
            var handler = GetEfficacyDisplay();
            if (!handler)
                return;

            handler.Clear();

            if (placedCard?.Treatment == null)
            {
                handler.SetPlant(null);
                handler.SetTreatment(null);
                return;
            }

            var plant = ResolvePlantForDisplay();
            handler.SetPlant(plant);
            handler.SetTreatment(placedCard.Treatment);
            handler.UpdateInfo();
        }

        public bool CanAcceptCard(ICard card)
        {
            if (card == null) return false;
            
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


        /// <summary>
        ///     Handles the click event for a placed card, determining whether to pick it up,
        ///     swap it with a selected card in the hand, or treat it as a normal placement.
        /// </summary>
        private void OnPlacedCardClicked()
        {
            if (placedCard is null or ILocationCard) return;
            if (CardGameMaster.Instance?.isInspecting == true) return;
            if (Time.time - _lastClickTime < 0.1f) return;
            _lastClickTime = Time.time;
            if (Time.frameCount == _lastPlacementFrame) return;
            if (Time.time - _lastPlacementTime < 0.5f) return;

            if (!HoldingCard)
            {
                TakeSelectedCard();
                return;
            }

            if (_deckManager.selectedACard == null || _deckManager.selectedACard == placedCard)
            {
                PickUpPlacedCard();
                return;
            }

            SwapWithSelectedCard();
        }

        private void PickUpPlacedCard()
        {
            if (!HoldingCard) return;
            // Safety check: prevent picking up expired/null cards or Location Cards
            if (placedCard is null or ILocationCard) return;

            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.unplaceCard);

            ClearAllSelections();

            if (placedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == placedCard;

                if (!isFromRetained)
                    _scoreManager.treatmentCost -= placedCard.Value.Value;
            }

            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != placedCard) continue;

                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false;
                }

                break;
            }

            ClearHolder();

            _scoreManager.CalculateTreatmentCost();
        }

        /// <summary>
        /// Swaps the currently held card with the selected card from the hand.
        /// NOTE: PlacementTurn is intentionally NOT updated during swaps, preserving the original
        /// placement turn to prevent redraw exploits via card swapping.
        /// </summary>
        private void SwapWithSelectedCard()
        {
            if (!HoldingCard || _deckManager.selectedACard == null) return;
            // Safety check: prevent swapping with expired/null cards
            if (placedCard == null) return;

            if (!CanAcceptCard(_deckManager.selectedACard))
            {
                Debug.Log($"Card holder of type {acceptedCardType} cannot accept card: {_deckManager.selectedACard.Name}");
                return;
            }

            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);

            var currentPlacedCard = placedCard;
            var currentCardClone = placedCardClick3D.gameObject;

            if (currentPlacedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == currentPlacedCard;
                if (!isFromRetained)
                    _scoreManager.treatmentCost -= currentPlacedCard.Value.Value;
            }

            placedCard = null;
            placedCardClick3D = null;
            placedCardView = null;

            RefreshEfficacyDisplay();

            if (currentCardClone != null)
                Destroy(currentCardClone);

            TakeSelectedCard();

            RestoreCardToHandWithoutSelection(currentPlacedCard);

            var allHandCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in allHandCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D == null || !click3D.selected) continue;
                click3D.selected = false;
                click3D.StartCoroutine(click3D.AnimateCardBack());
            }

            _scoreManager.CalculateTreatmentCost();
            
            NotifySpotDataHolder();

            RefreshEfficacyDisplay();
        }

        private void ClearAllSelections()
        {
            _deckManager.ClearSelectedCard();
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D == null || !click3D.selected) continue;
                click3D.selected = false;
                click3D.StartCoroutine(click3D.AnimateCardBack());
            }
        }

        private void RestoreCardToHandWithoutSelection(ICard card)
        {
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != card) continue;

                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false;
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }

                break;
            }
        }

        public void ClearHolder()
        {
            if (placedCard is ILocationCard)
                NotifySpotDataHolderRemoval();

            if (placedCardClick3D != null)
                Destroy(placedCardClick3D.gameObject);

            placedCard = null;
            placedCardClick3D = null;
            placedCardView = null;
            PlacementTurn = -1;

            // Normalize visibility based on plant presence when no card is held
            var plant = ResolvePlantForDisplay();
            ToggleCardHolder(plant != null);

            RefreshEfficacyDisplay();
        }

        public void ClearLocationCardByExpiry()
        {
            if (placedCard is not ILocationCard) return;

            if (placedCardClick3D)
            {
                placedCardClick3D.enabled = false;
                placedCardClick3D.isEnabled = false;
                placedCardClick3D.onClick3D?.RemoveAllListeners();
                Destroy(placedCardClick3D.gameObject);
            }

            placedCardView = null;
            placedCardClick3D = null;
            placedCard = null;
            PlacementTurn = -1;

            // When a location card expires, return the holder to its normal visibility:
            // show if a plant is present; hide if not.
            var plant = ResolvePlantForDisplay();
            ToggleCardHolder(plant != null);

            RefreshEfficacyDisplay();
        }

        /// <summary>
        /// Places the currently selected card from DeckManager onto this holder.
        /// Validates that the associated plant exists and has positive value before placement.
        /// </summary>
        /// <remarks>
        /// This method prevents placing cards on dead or dying plants (Value &lt;= 0).
        /// Cards placed on plants that subsequently die are destroyed, not returned to the deck,
        /// so this validation prevents wasted resources.
        /// </remarks>
        public void TakeSelectedCard()
        {
            ClearPreview();

            // Prevent interaction when holding a Location Card (same check as OnPlacedCardClicked)
            if (HoldingCard && placedCard is ILocationCard) return;

            if (HoldingCard) GiveBackCard();

            if (_deckManager.selectedACardClick3D is null || _deckManager.selectedACard is null) return;

            // Prevent placing cards on dead or dying plants
            var plant = ResolvePlantForDisplay();
            if (plant == null)
            {
                Debug.LogWarning($"[PlacedCardHolder] Cannot place card: No plant found for holder '{name}'", this);
                return;
            }

            if (plant.PlantCard == null || plant.PlantCard.Value <= 0)
            {
                Debug.Log($"[PlacedCardHolder] Cannot place card '{_deckManager.selectedACard.Name}' on dead/dying plant '{plant.name}' (Value: {plant.PlantCard?.Value ?? 0})", this);
                return;
            }

            if (!CanAcceptCard(_deckManager.selectedACard))
            {
                Debug.Log($"Card holder of type {acceptedCardType} cannot accept card: {_deckManager.selectedACard.Name}");
                return;
            }

            var selectedCard = _deckManager.selectedACardClick3D;

            if (_deckManager.selectedACard is IFieldSpell)
            {
                // TODO: Implement field spell logic - can now iterate card holders via plantHolder.CardHolders
                foreach (var plantHolder in _deckManager.plantLocations)
                {
                    if (!plantHolder) continue;
                    foreach (var holder in plantHolder.CardHolders)
                    {
                        // Field spell effect placeholder
                    }
                }
            }

            // Properly disable the original card's Click3D component to prevent duplicate clicks
            var sourceLocalScale = selectedCard.transform.localScale;
            var sourceLossyScale = selectedCard.transform.lossyScale;
            var parentForPlacement = transform;
            var parentLossyScale = parentForPlacement.lossyScale;
            var resolvedLocalScale = new Vector3(
                !Mathf.Approximately(parentLossyScale.x, 0f) ? sourceLossyScale.x / parentLossyScale.x : sourceLocalScale.x,
                !Mathf.Approximately(parentLossyScale.y, 0f) ? sourceLossyScale.y / parentLossyScale.y : sourceLocalScale.y,
                !Mathf.Approximately(parentLossyScale.z, 0f) ? sourceLossyScale.z / parentLossyScale.z : sourceLocalScale.z
            );

            // Default lay-flat rotation with an optional tweak
            var resolvedLocalRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(placedCardRotationOffsetEuler);

            selectedCard.DisableClick3D();
            selectedCard.enabled = false;
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);

            var cardClone = Instantiate(selectedCard.gameObject, parentForPlacement);

            var cardViewClone = cardClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(_deckManager.selectedACard);

            cardClone.transform.SetParent(parentForPlacement, false);
            // Place relative to the holder using configured offsets
            cardClone.transform.localPosition = placedCardPositionOffsetLocal;
            cardClone.transform.localRotation = resolvedLocalRotation;
            cardClone.transform.localScale = Vector3.Scale(resolvedLocalScale, placedCardScaleMultiplier);
            placedCard = _deckManager.selectedACard;
            placedCardClick3D = cardClone.GetComponent<Click3D>();
            placedCardView = cardClone.GetComponent<CardView>();

            if (placedCardClick3D != null)
            {
                placedCardClick3D.handItem = true;
                if (disableHoverOnPlacedCard)
                {
                    placedCardClick3D.scaleUp = 1f;
                    placedCardClick3D.popHeight = 0f;
                }
                placedCardClick3D.StopAllCoroutines();
                placedCardClick3D.UpdateOriginalTransform(
                    cardClone.transform.localScale,
                    cardClone.transform.localPosition);
                placedCardClick3D.RefreshState();
                placedCardClick3D.onClick3D.RemoveAllListeners();
                placedCardClick3D.onClick3D.AddListener(OnPlacedCardClicked);
                placedCardClick3D.enabled = false;
                StartCoroutine(ReenablePlacedCardClickWithInputActionFix());
            }

            _lastPlacementFrame = Time.frameCount;
            _lastPlacementTime = Time.time;

            // Track which turn the card was placed
            var cgm = CardGameMaster.Instance;
            if (cgm?.turnController != null)
            {
                PlacementTurn = cgm.turnController.currentTurn;
            }
            else
            {
                Debug.LogWarning("[PlacedCardHolder] Cannot track placement turn: CardGameMaster or TurnController not initialized", this);
                PlacementTurn = -1;
            }

            if (placedCardView != null) placedCardView.enabled = false;

            _deckManager.ClearSelectedCard();

            // var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            // if (buttonRenderer)
            //     buttonRenderer.enabled = false;
            var cardRenderers = selectedCard.GetComponentsInChildren<Renderer>();
            if (cardRenderers == null) return;
            foreach (var renderer1 in cardRenderers) renderer1.enabled = false;

            if (placedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == placedCard;

                if (!isFromRetained)
                    _scoreManager.treatmentCost += placedCard.Value.Value;
            }

            _scoreManager.CalculateTreatmentCost();
            
            NotifySpotDataHolder();

            RefreshEfficacyDisplay();
        }

        // WORKAROUND: Uses reflection to dispose of internal InputAction and prevent Input System errors.
        // Multiple frame delays allow Unity lifecycle and Input System to stabilize after card placement.
        private IEnumerator ReenablePlacedCardClickWithInputActionFix()
        {
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (!placedCardClick3D)
            {
                Debug.LogWarning("PlacedCardHolder: placedCardClick3D is null during re-enable", this);
                yield break;
            }

            placedCardClick3D.enabled = true;
            placedCardClick3D.isEnabled = true;

            try
            {
                var reflection = placedCardClick3D.GetType();
                var inputActionField = reflection.GetField("_mouseClickAction",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (inputActionField == null)
                {
                    Debug.LogWarning("PlacedCardHolder: Could not find _mouseClickAction field via reflection. " +
                                     "Input System API may have changed.", this);
                    yield break;
                }

                if (inputActionField.GetValue(placedCardClick3D) is not InputAction inputAction) yield break;

                inputAction.Disable();
                inputAction.Dispose();
                inputActionField.SetValue(placedCardClick3D, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlacedCardHolder: Reflection-based input action cleanup failed: {ex.Message}", this);
            }
        }

        /// <summary>
        ///     Returns the currently held card from the cardholder to its appropriate location.
        /// </summary>
        /// <remarks>
        ///     This method ensures proper restoration of the held card by either returning it to the player's hand or
        ///     moving it back to the retained cardholder, if applicable. If the card cannot be returned to either,
        ///     its clone is destroyed. The method also updates the game state by recalculating treatment costs, clearing
        ///     this holder's state, and enabling the visibility of the necessary UI elements. Additionally, it plays an
        ///     audio cue to signify the card's removal from the holder.
        /// </remarks>
        private void GiveBackCard()
        {
            if (!HoldingCard) return;

            var returnedToHand = false;
            var returnedToRetained = false;

            // Check for returning to the original hand
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != placedCard) continue;
                // Found the original card in the hand, re-enable it
                foreach (var renderer1 in cardView.GetComponentsInChildren<Renderer>(true))
                    renderer1.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false;
                    click3D.RefreshState();
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }

                returnedToHand = true;
                break;
            }

            // Check for returning to RetainedCardHolder
            var retainedSlot = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
            if (retainedSlot != null)
                if (retainedSlot.HeldCard == placedCard)
                {
                    if (!returnedToHand)
                    {
                        // The Card belongs to the retained slot, give it back
                        retainedSlot.ReclaimCard(placedCardClick3D.gameObject);
                        returnedToRetained = true;
                    }
                    else
                    {
                        // The Card was from the retained slot but returned to hand or lost — cleanup retained slot
                        retainedSlot.ClearHeldCard();
                    }
                }

            // If we didn't return the clone to retain, destroy it
            if (!returnedToRetained && placedCardClick3D) Destroy(placedCardClick3D.gameObject);

            // Update cost
            if (placedCard?.Value != null)
            {
                var retainedSlot1 = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                if (!(retainedSlot1 != null && retainedSlot1.HeldCard == placedCard))
                    _scoreManager.treatmentCost -= placedCard.Value.Value;
            }

            _scoreManager.CalculateTreatmentCost();
            
            NotifySpotDataHolderRemoval();

            // Clear this holder's state
            placedCardView = null;
            placedCardClick3D = null;
            placedCard = null;
            PlacementTurn = -1;

            var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
            playerAudio.PlayOneShot(CardGameMaster.Instance.soundSystem.unplaceCard);

            // Normalize visibility based on plant presence when no card is held
            var plant = ResolvePlantForDisplay();
            ToggleCardHolder(plant != null);

            RefreshEfficacyDisplay();
        }

        public void ToggleCardHolder(bool state)
        {
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            var click3D = gameObject.GetComponentInChildren<Click3D>(true);
            if (click3D)
            {
                click3D.isEnabled = state;
                click3D.enabled = state;
            }
            if (buttonRenderer)
                buttonRenderer.enabled = state;
        }

        private SpotDataHolder ResolveSpotDataHolder()
        {
            if (spotDataHolder) return spotDataHolder;

            spotDataHolder = GetComponent<SpotDataHolder>();
            if (spotDataHolder)
            {
                spotDataHolder.RegisterCardHolder(this);
                return spotDataHolder;
            }

            spotDataHolder = GetComponentInChildren<SpotDataHolder>();
            if (!spotDataHolder) return EnsureSpotDataHolder();
            spotDataHolder.RegisterCardHolder(this);
            return spotDataHolder;

        }

        private SpotDataHolder EnsureSpotDataHolder()
        {
            if (spotDataHolder)
            {
                spotDataHolder.RegisterCardHolder(this);
                return spotDataHolder;
            }

            var candidate = GetComponent<SpotDataHolder>();

            if (!candidate)
                candidate = gameObject.AddComponent<SpotDataHolder>();

            if (candidate)
            {
                candidate.RegisterCardHolder(this);
                candidate.InvalidatePlantCache();
                candidate.RefreshAssociatedPlant();
                spotDataHolder = candidate;
            }
            else
            {
                Debug.LogWarning($"PlacedCardHolder {name} could not create a SpotDataHolder.", this);
            }

            return spotDataHolder;
        }

        private void NotifySpotDataHolder()
        {
            if (placedCard is not ILocationCard locationCard) return;

            try
            {
                var target = ResolveSpotDataHolder();
                if (target != null)
                    target.OnLocationCardPlaced(locationCard);
                else
                    Debug.LogWarning(
                        $"PlacedCardHolder {name} could not find a SpotDataHolder for location card placement.", this);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error notifying SpotDataHolder of card placement: {e.Message}");
            }
        }

        private void NotifySpotDataHolderRemoval()
        {
            if (placedCard is not ILocationCard) return;

            try
            {
                var target = ResolveSpotDataHolder();
                if (target != null) target.OnLocationCardRemoved();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error notifying SpotDataHolder of card removal: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            ClearPreview();

            if (_deckManager != null)
                _deckManager.SelectedCardChanged -= OnSelectedCardChanged;

            if (_holderClick3D != null)
            {
                _holderClick3D.HoverEntered -= OnHolderHoverEnter;
                _holderClick3D.HoverExited -= OnHolderHoverExit;
            }

            if (spotDataHolder)
                spotDataHolder.UnregisterCardHolder(this);
        }
    }
}

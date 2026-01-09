using System;
using System.Collections;
using System.Collections.Generic;
using _project.Scripts.Classes;
using DG.Tweening;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    [DisallowMultipleComponent]
    public class DeckActionHandAnimator : MonoBehaviour
    {
        private Sequence _currentDisplaySequence;

        private Sequence _currentHandSequence;
        private DeckManager _deckManager;

        private DeckManager DeckManager
        {
            get
            {
                if (_deckManager) return _deckManager;
                _deckManager = GetComponent<DeckManager>();
                if (!_deckManager)
                {
                    Debug.LogError("[DeckActionHandAnimator] Missing DeckManager component. " +
                                  "Ensure DeckManager has [RequireComponent(typeof(DeckActionHandAnimator))].", this);
                }
                return _deckManager;
            }
        }

        private void OnDestroy()
        {
            SafeKillSequence(ref _currentHandSequence);
            SafeKillSequence(ref _currentDisplaySequence);
        }

        private void AnimateHandReflow(float duration)
        {
            if (!DeckManager || !DeckManager.actionCardParent) return;

            SafeKillSequence(ref _currentHandSequence);

            DeckManager.SetUpdatingActionDisplay(true);

            StartCoroutine(AnimationTimeoutWatchdog(duration + 2f));

            try
            {
                var parent = DeckManager.actionCardParent;
                var childCount = parent.childCount;
                if (childCount == 0)
                {
                    DeckManager.SetUpdatingActionDisplay(false);
                    _currentHandSequence = null;
                    return;
                }

                var (effectiveSpacing, cardScale, useOverlapLayout) = CalculateHandLayout(childCount);

                var click3DComponents = new Click3D[childCount];
                for (var i = 0; i < childCount; i++)
                {
                    var tf = parent.GetChild(i);
                    var click3D = tf.GetComponent<Click3D>();
                    if (!click3D) continue;
                    click3DComponents[i] = click3D;
                    click3D.StopAllCoroutines();
                    click3D.enabled = false;
                }

                _currentHandSequence = DOTween.Sequence();

                for (var i = 0; i < childCount; i++)
                {
                    var tf = parent.GetChild(i);
                    var (targetPos, targetRot) =
                        CalculateCardTransform(i, childCount, effectiveSpacing, useOverlapLayout);

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

                _currentHandSequence.OnComplete(() =>
                {
                    try
                    {
                        for (var i = 0; i < childCount; i++)
                        {
                            if (i >= parent.childCount || i >= click3DComponents.Length) continue;
                            var (targetPos, _) =
                                CalculateCardTransform(i, childCount, effectiveSpacing, useOverlapLayout);
                            var click3D = click3DComponents[i];
                            if (!click3D) continue;
                            UpdateClick3DFields(click3D, cardScale, targetPos);
                            click3D.enabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error in hand reflow completion: {ex.Message}");

                        foreach (var t in click3DComponents)
                            if (t)
                                t.enabled = true;
                    }
                    finally
                    {
                        DeckManager.SetUpdatingActionDisplay(false);
                        _currentHandSequence = null;
                    }
                });

                _currentHandSequence.Play();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating hand reflow animation: {ex.Message}");
                SafeKillSequence(ref _currentHandSequence);
                DeckManager.SetUpdatingActionDisplay(false);
            }
        }

        public void AddCardVisualAndAnimate(ICard card, float animDuration, int totalCardsInHand)
        {
            if (!DeckManager || !DeckManager.actionCardParent || card == null || !card.Prefab) return;

            if (DeckManager.debug && totalCardsInHand > DeckManager.cardsDrawnPerTurn)
            {
                var layoutMode = totalCardsInHand <= 6 ? "scaling" : "overlap";
                Debug.Log(
                    $"Hand overflow: {totalCardsInHand} cards (normal: {DeckManager.cardsDrawnPerTurn}). Using {layoutMode} layout.");
            }

            var newCardObj = Instantiate(card.Prefab, DeckManager.actionCardParent);
            var cardView = newCardObj.GetComponent<CardView>();
            if (cardView)
                cardView.Setup(card);
            else
                Debug.LogWarning("Action Card Prefab is missing a Card View...");

            var t = newCardObj.transform;
            t.localScale = Vector3.zero;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            var cgm = CardGameMaster.Instance;
            var playerAudio = cgm ? cgm.playerHandAudioSource : null;
            var sfx = cgm ? cgm.soundSystem?.drawCard : null;
            if (playerAudio && sfx) playerAudio.PlayOneShot(sfx);

            AnimateHandReflow(animDuration);
        }

        public void DisplayActionCardsSequence(IReadOnlyList<ICard> cardsToDisplay)
        {
            if (!DeckManager || !DeckManager.actionCardParent) return;

            SafeKillSequence(ref _currentDisplaySequence);

            DeckManager.SetUpdatingActionDisplay(true);

            StartCoroutine(AnimationTimeoutWatchdog(5f));

            try
            {
                if (cardsToDisplay == null || cardsToDisplay.Count == 0)
                {
                    DeckManager.SetUpdatingActionDisplay(false);
                    return;
                }

                var totalCards = cardsToDisplay.Count;
                var (effectiveSpacing, cardScale, useOverlapLayout) = CalculateHandLayout(totalCards);

                var preInstantiatedCards = new PreInstantiatedCard[totalCards];

                for (var i = 0; i < totalCards; i++)
                {
                    var card = cardsToDisplay[i];
                    var (targetPos, targetRot) =
                        CalculateCardTransform(i, totalCards, effectiveSpacing, useOverlapLayout);

                    var cardObj = Instantiate(card.Prefab, DeckManager.actionCardParent);
                    var cardView = cardObj.GetComponent<CardView>();
                    if (cardView)
                        cardView.Setup(card);
                    else
                        Debug.LogWarning("Action Card Prefab is missing a Card View...");

                    cardObj.transform.localPosition = targetPos;
                    cardObj.transform.localRotation = targetRot;
                    cardObj.transform.localScale = Vector3.zero;

                    var click3D = cardObj.GetComponent<Click3D>();
                    if (click3D) click3D.enabled = false;

                    preInstantiatedCards[i] = new PreInstantiatedCard
                    {
                        cardObject = cardObj,
                        click3D = click3D,
                        targetScale = cardScale
                    };
                }

                _currentDisplaySequence = DOTween.Sequence().Pause();
                var cardDelay = Mathf.Min(0.1f, totalCards > 0 ? 0.6f / totalCards : 0.1f);

                for (var i = 0; i < totalCards; i++)
                {
                    var cardData = preInstantiatedCards[i];
                    var cardIndex = i;
                    var (targetPos, _) =
                        CalculateCardTransform(cardIndex, totalCards, effectiveSpacing, useOverlapLayout);

                    _currentDisplaySequence.AppendCallback(() =>
                    {
                        var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
                        var sfx = CardGameMaster.Instance.soundSystem?.drawCard;
                        if (playerAudio && sfx) playerAudio.PlayOneShot(sfx);

                        cardData.cardObject.transform
                            .DOScale(cardData.targetScale, 0.3f)
                            .SetLink(cardData.cardObject, LinkBehaviour.KillOnDisable)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() =>
                            {
                                if (cardData.click3D == null) return;
                                UpdateClick3DFields(cardData.click3D, cardData.targetScale, targetPos);
                                cardData.click3D.enabled = true;
                            });
                    });

                    if (i < totalCards - 1) _currentDisplaySequence.AppendInterval(cardDelay);
                }

                _currentDisplaySequence.OnKill(() => { FinalizePreInstantiatedCards(preInstantiatedCards); });

                _currentDisplaySequence.OnComplete(() =>
                {
                    DeckManager.SetUpdatingActionDisplay(false);
                    _currentDisplaySequence = null;
                });

                _currentDisplaySequence.Play();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating display card sequence: {ex.Message}");
                SafeKillSequence(ref _currentDisplaySequence);
                DeckManager.SetUpdatingActionDisplay(false);
            }
        }

        public void ForceResetAnimationFlag()
        {
            SafeKillSequence(ref _currentHandSequence);
            SafeKillSequence(ref _currentDisplaySequence);

            DeckManager.ForceClearUpdatingActionDisplay();

            if (!DeckManager.actionCardParent) return;

            var parent = DeckManager.actionCardParent;
            var childCount = parent.childCount;
            if (childCount == 0) return;

            var (_, cardScale, _) = CalculateHandLayout(childCount);

            foreach (Transform child in parent)
            {
                if (child.localScale == Vector3.zero) child.localScale = cardScale;

                var click3D = child.GetComponent<Click3D>();
                if (!click3D) continue;

                UpdateClick3DFields(click3D, child.localScale, child.localPosition);
                click3D.enabled = true;
            }
        }

        private (float effectiveSpacing, Vector3 cardScale, bool useOverlapLayout) CalculateHandLayout(int totalCards)
        {
            const float maxHandWidth = 8f;
            const int maxScalingCards = 6;

            var effectiveSpacing = DeckManager.cardSpacing;

            var cgm = CardGameMaster.Instance;
            var prefabScale = cgm.actionCardPrefab ? cgm.actionCardPrefab.transform.localScale : Vector3.one;
            var cardScale = prefabScale;
            var useOverlapLayout = false;

            if (totalCards <= maxScalingCards)
            {
                if (totalCards <= DeckManager.cardsDrawnPerTurn) return (effectiveSpacing, cardScale, false);
                var overflowFactor = (float)DeckManager.cardsDrawnPerTurn / totalCards;
                effectiveSpacing = DeckManager.cardSpacing * overflowFactor;

                var requiredWidth = totalCards > 1 ? (totalCards - 1) * effectiveSpacing * 2 : 0f;
                if (!(requiredWidth > maxHandWidth)) return (effectiveSpacing, cardScale, false);
                var scaleFactor = Mathf.Clamp(maxHandWidth / requiredWidth, 0.7f, 1f);
                effectiveSpacing *= scaleFactor;
                cardScale = prefabScale * Mathf.Max(scaleFactor, 0.85f);
            }
            else
            {
                useOverlapLayout = true;
                cardScale = prefabScale;
                effectiveSpacing = 0.1f;

                var totalWidth = (totalCards - 1) * effectiveSpacing;
                if (totalWidth > maxHandWidth) effectiveSpacing = maxHandWidth / (totalCards - 1);
            }

            return (effectiveSpacing, cardScale, useOverlapLayout);
        }

        private static (Vector3 position, Quaternion rotation) CalculateCardTransform(int cardIndex, int totalCards,
            float effectiveSpacing, bool useOverlapLayout)
        {
            if (useOverlapLayout)
            {
                var startX = -(totalCards - 1) * effectiveSpacing * 0.5f;
                var overlapXOffset = startX + cardIndex * effectiveSpacing;
                var zOffset = cardIndex * 0.01f;
                var position = new Vector3(overlapXOffset, 0f, -zOffset);
                var rotation = Quaternion.identity;
                return (position, rotation);
            }

            const float totalFanAngle = -30f;
            var angleOffset = totalCards > 1
                ? Mathf.Lerp(-totalFanAngle / 2, totalFanAngle / 2, (float)cardIndex / (totalCards - 1))
                : 0f;
            var xOffset = totalCards > 1
                ? Mathf.Lerp(-effectiveSpacing, effectiveSpacing, (float)cardIndex / (totalCards - 1))
                : 0f;

            var fanPosition = new Vector3(xOffset, 0f, 0f);
            var fanRotation = Quaternion.Euler(0, 0, angleOffset);
            return (fanPosition, fanRotation);
        }

        private static void UpdateClick3DFields(Click3D click3D, Vector3 scale, Vector3 position)
        {
            click3D?.UpdateOriginalTransform(scale, position);
        }

        private static void FinalizePreInstantiatedCards(PreInstantiatedCard[] cards)
        {
            if (cards == null) return;

            foreach (var cardData in cards)
            {
                if (cardData.cardObject is null) continue;

                var cardTransform = cardData.cardObject.transform;

                if (cardTransform.localScale != cardData.targetScale) cardTransform.localScale = cardData.targetScale;

                if (cardData.click3D is null) continue;
                UpdateClick3DFields(cardData.click3D, cardData.targetScale, cardTransform.localPosition);
                cardData.click3D.enabled = true;
            }
        }

        private IEnumerator AnimationTimeoutWatchdog(float maxDuration)
        {
            yield return new WaitForSeconds(maxDuration);

            if (!DeckManager.UpdatingActionDisplay) yield break;
            Debug.LogError("[DeckManager] Animation watchdog timeout detected! Force-clearing flag.");
            ForceResetAnimationFlag();
        }

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

        /// <summary>
        ///     Holds pre-instantiated card data for animation sequencing.
        ///     Decouples card creation from animation timing to prevent cards being lost
        ///     when the animation watchdog kills the sequence mid-instantiation.
        /// </summary>
        private struct PreInstantiatedCard
        {
            public GameObject cardObject;
            public Click3D click3D;
            public Vector3 targetScale;
        }
    }
}
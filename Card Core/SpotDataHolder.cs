using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class SpotDataHolder : MonoBehaviour
    {
        [SerializeField] private List<PlacedCardHolder> associatedCardHolders = new();
        [SerializeField] private GameObject cardHolder;
        private PlantController _associatedPlant;
        private ILocationCard cLocationCard;
        private bool _effectActive;
        private bool _pendingExpiry;
        private bool _plantCacheDirty = true;
        private int _remainingDuration;
        
        private void Start() => RefreshAssociatedPlant();

        public void RefreshAssociatedPlant()
        {
            // Only refresh if the cache is dirty or the plant reference is invalid
            if (!_plantCacheDirty && _associatedPlant && _associatedPlant.PlantCard != null)
                return;

            _associatedPlant = GetComponentInChildren<PlantController>();
            if (!_associatedPlant)
                _associatedPlant = GetComponentInParent<PlantController>();
            if (!_associatedPlant && transform.parent)
                _associatedPlant = transform.parent.GetComponentInChildren<PlantController>();

            _plantCacheDirty = false;
        }

        public void InvalidatePlantCache() => _plantCacheDirty = true;

        public void RegisterCardHolder(PlacedCardHolder holder)
        {
            if (!holder) return;
            if (associatedCardHolders.Contains(holder)) return;
            associatedCardHolders.Add(holder);
        }

        public void UnregisterCardHolder(PlacedCardHolder holder)
        {
            if (!holder) return;
            associatedCardHolders.Remove(holder);
        }

        public void OnLocationCardPlaced(ILocationCard locationCard)
        {
            try
            {
                if (locationCard == null)
                {
                    Debug.LogWarning("SpotDataHolder received a null location card during placement.");
                    return;
                }

                // Clear previous effect before applying the new one
                if (cLocationCard != null && _effectActive)
                {
                    TryRemoveLocationEffect(cLocationCard);
                    _effectActive = false;
                }

                cLocationCard = locationCard;

                InvalidatePlantCache();

                // Apply immediate effect
                TryApplyLocationEffect(locationCard);

                // Activate the effect - it will apply on next ProcessTurn()
                _remainingDuration = locationCard.EffectDuration;
                _effectActive = true;
                _pendingExpiry = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error placing location card {locationCard?.Name}: {e.Message}");
                // Reset to safe state on error
                _effectActive = false;
                cLocationCard = null;
            }
        }

        public void OnLocationCardRemoved()
        {
            try
            {
                if (cLocationCard == null) return;

                TryRemoveLocationEffect(cLocationCard);

                _effectActive = false;
                cLocationCard = null;
                _pendingExpiry = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error removing location card: {e.Message}");
                // Force clean state on error
                _effectActive = false;
                cLocationCard = null;
            }
        }


        public void ProcessTurn()
        {
            if (cLocationCard == null || !_effectActive) return;

            // Validate and refresh plant reference before applying effects
            RefreshAssociatedPlant();

            var hasPlant = _associatedPlant != null && _associatedPlant.PlantCard != null;
            
            if (hasPlant)
            {
                try
                {
                    cLocationCard.ApplyTurnEffect(_associatedPlant);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error applying location effect {cLocationCard.Name}: {e.Message}");
                    // Deactivate on error to prevent repeated failures
                    _effectActive = false;
                    cLocationCard = null;
                    return;
                }
            }
            
            if (cLocationCard.IsPermanent) return;

            _remainingDuration--;
            if (_remainingDuration > 0) return;

            _pendingExpiry = true;
        }

        public void FinalizeLocationCardTurn()
        {
            if (!_pendingExpiry)
                return;

            var expired = cLocationCard;
            _pendingExpiry = false;

            if (expired == null)
            {
                _effectActive = false;
                return;
            }

            _effectActive = false;
            cLocationCard = null;

            TryRemoveLocationEffect(expired);

            var holders = BuildHolderSearchList();

            foreach (var holder in holders.Where(holder => holder && holder.placedCard == expired))
            {
                holder.ClearLocationCardByExpiry();
                break;
            }
        }

        private List<PlacedCardHolder> BuildHolderSearchList()
        {
            var results = new List<PlacedCardHolder>();

            for (var i = associatedCardHolders.Count - 1; i >= 0; i--)
            {
                var holder = associatedCardHolders[i];
                if (!holder)
                {
                    associatedCardHolders.RemoveAt(i);
                    continue;
                }

                if (!results.Contains(holder))
                    results.Add(holder);
            }

            var localHolders = GetComponentsInChildren<PlacedCardHolder>(true);
            foreach (var holder in localHolders)
            {
                if (!holder) continue;
                if (!results.Contains(holder))
                    results.Add(holder);
            }

            if (transform.parent == null) return results;
            {
                var parentHolders = transform.parent.GetComponentsInChildren<PlacedCardHolder>(true);
                foreach (var holder in parentHolders)
                {
                    if (!holder) continue;
                    if (!results.Contains(holder))
                        results.Add(holder);
                }
            }

            return results;
        }

        private void TryApplyLocationEffect(ILocationCard locationCard)
        {
            if (locationCard == null) return;

            RefreshAssociatedPlant();
            if (_associatedPlant == null)
            {
                Debug.LogWarning($"SpotDataHolder {name}: No plant available to apply location effect {locationCard.Name}.");
                return;
            }

            try
            {
                locationCard.ApplyLocationEffect(_associatedPlant);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error applying location effect {locationCard.Name}: {e.Message}");
            }
        }

        private void TryRemoveLocationEffect(ILocationCard locationCard)
        {
            if (locationCard == null) return;

            RefreshAssociatedPlant();
            if (!_associatedPlant) return;

            try
            {
                locationCard.RemoveLocationEffect(_associatedPlant);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error removing location effect {locationCard.Name}: {e.Message}");
            }
        }
    }
}

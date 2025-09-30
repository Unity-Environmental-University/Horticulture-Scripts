using System;
using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class SpotDataHolder : MonoBehaviour
    {
        [SerializeField] private GameObject cardHolder;
        private PlantController _associatedPlant;
        private bool _effectActive;
        private bool _plantCacheDirty = true;
        private int _remainingDuration;

        private ILocationCard cLocationCard;
        [SerializeField] private List<PlacedCardHolder> associatedCardHolders = new();

        private void Start()
        {
            RefreshAssociatedPlant();
        }

        public void RefreshAssociatedPlant()
        {
            // Only refresh if cache is dirty or plant reference is invalid
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
                // In turn-based system, clear previous effect and set new card
                if (cLocationCard != null && _effectActive) _effectActive = false;

                cLocationCard = locationCard;
                InvalidatePlantCache();
                RefreshAssociatedPlant();

                // Activate the effect - it will apply on next ProcessTurn()
                if (locationCard == null) return;
                _remainingDuration = locationCard.EffectDuration;
                _effectActive = true;
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
                // In turn-based system, deactivate the effect
                if (cLocationCard != null && _effectActive) _effectActive = false;

                cLocationCard = null;
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
            
            if (_associatedPlant == null || _associatedPlant.PlantCard == null)
            {
                if (!cLocationCard.IsPermanent) _remainingDuration--;
                return;
            }
            
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
            
            if (cLocationCard.IsPermanent) return;
            
            _remainingDuration--;
            if (_remainingDuration > 0) return;

            var expired = cLocationCard;
            _effectActive = false;
            cLocationCard = null;

            var holders = BuildHolderSearchList();

            foreach (var holder in holders)
            {
                if (!holder || holder.placedCard != expired) continue;
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

            if (transform.parent != null)
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
    }
}

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

            var holders = GetComponentsInChildren<PlacedCardHolder>(true);
            if (transform.parent != null)
            {
                var parentHolders = transform.parent.GetComponentsInChildren<PlacedCardHolder>(true);
                if (parentHolders is { Length: > 0 })
                {
                    var list = new List<PlacedCardHolder>(holders);
                    foreach (var h in parentHolders)
                        if (!list.Contains(h))
                            list.Add(h);
                    holders = list.ToArray();
                }
            }

            foreach (var holder in holders)
            {
                if (!holder || holder.placedCard != expired) continue;
                holder.ClearLocationCardByExpiry();
                break;
            }
        }
    }
}
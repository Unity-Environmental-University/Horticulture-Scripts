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
        private int _remainingDuration;

        private ILocationCard cLocationCard;

        private void Start()
        {
            RefreshAssociatedPlant();
        }

        public void RefreshAssociatedPlant()
        {
            var previousPlant = _associatedPlant;
            _associatedPlant = GetComponentInParent<PlantController>();
            if (!_associatedPlant)
                _associatedPlant = GetComponentInChildren<PlantController>();
            if (!_associatedPlant && transform.parent != null)
                _associatedPlant = transform.parent.GetComponentInChildren<PlantController>();

            // In turn-based system, effects are applied during ProcessTurn()
            // No immediate effect application needed here
        }

        public void OnLocationCardPlaced(ILocationCard locationCard)
        {
            // In turn-based system, simply clear previous effect and set new card
            if (cLocationCard != null && _effectActive) _effectActive = false;

            cLocationCard = locationCard;
            RefreshAssociatedPlant();

            // Activate the effect - it will apply on next ProcessTurn()
            if (locationCard == null) return;
            _remainingDuration = locationCard.EffectDuration;
            _effectActive = true;
        }

        public void OnLocationCardRemoved()
        {
            // In turn-based system, simply deactivate the effect
            if (cLocationCard != null && _effectActive)
            {
                _effectActive = false;
            }

            cLocationCard = null;
        }


        public void ProcessTurn()
        {
            if (cLocationCard == null || !_effectActive) return;

            // Handle permanent effects separately
            if (cLocationCard.IsPermanent)
            {
                RefreshAssociatedPlant();
                if (_associatedPlant == null) return;
                cLocationCard.ApplyTurnEffect(_associatedPlant);
                return;
            }

            // Process temporary effects with duration
            RefreshAssociatedPlant();
            if (_associatedPlant != null) cLocationCard.ApplyTurnEffect(_associatedPlant);

            _remainingDuration--;

            // Check expiration and handle cleanup properly
            if (_remainingDuration > 0) return;
            
            // Properly deactivate effect
            _effectActive = false;
            cLocationCard = null;
        }


        public bool HasActiveLocationEffect()
        {
            return cLocationCard != null && _effectActive;
        }

        public ILocationCard GetActiveLocationCard()
        {
            return _effectActive ? cLocationCard : null;
        }

        public int GetRemainingDuration()
        {
            return _remainingDuration;
        }
    }
}
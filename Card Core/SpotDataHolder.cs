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

            if (previousPlant == _associatedPlant || cLocationCard == null || !_effectActive) return;
            if (previousPlant) cLocationCard.RemoveLocationEffect(previousPlant);

            if (_associatedPlant) cLocationCard.ApplyLocationEffect(_associatedPlant);
        }

        public void OnLocationCardPlaced(ILocationCard locationCard)
        {
            Debug.LogWarning(
                $"[SpotDataHolder] Location card placed: {locationCard?.Name ?? "null"} at {transform.name}");

            if (cLocationCard != null && _effectActive) RemoveLocationEffect();

            cLocationCard = locationCard;
            RefreshAssociatedPlant();
            if (locationCard != null) ApplyLocationEffect();
        }

        public void OnLocationCardRemoved()
        {
            Debug.LogWarning(
                $"[SpotDataHolder] Location card removed: {cLocationCard?.Name ?? "none"} from {transform.name}");

            if (cLocationCard != null && _effectActive) RemoveLocationEffect();
            cLocationCard = null;
        }

        private void ApplyLocationEffect()
        {
            if (cLocationCard == null) return;

            _remainingDuration = cLocationCard.EffectDuration;
            _effectActive = true;

            Debug.LogWarning($"[SpotDataHolder] ApplyLocationEffect - Plant found: {_associatedPlant != null}, Plant: {_associatedPlant?.name}");
            
            if (_associatedPlant != null) 
            {
                Debug.LogWarning($"[SpotDataHolder] Calling ApplyLocationEffect on plant {_associatedPlant.name}");
                cLocationCard.ApplyLocationEffect(_associatedPlant);
            }

            if (CardGameMaster.Instance != null && CardGameMaster.Instance.debuggingCardClass)
            {
                var plantStatus = _associatedPlant != null ? $"to plant at {transform.name}" : "to empty spot";
                Debug.Log($"Applied location effect {cLocationCard.Name} {plantStatus}");
            }
        }

        private void RemoveLocationEffect()
        {
            if (cLocationCard == null || !_effectActive) return;

            if (_associatedPlant != null) cLocationCard.RemoveLocationEffect(_associatedPlant);

            _effectActive = false;

            if (CardGameMaster.Instance == null || !CardGameMaster.Instance.debuggingCardClass) return;
            var plantStatus = _associatedPlant != null ? $"from plant at {transform.name}" : "from empty spot";
            Debug.Log($"Removed location effect {cLocationCard.Name} {plantStatus}");
        }

        public void ProcessTurn()
        {
            if (cLocationCard == null || !_effectActive || cLocationCard.IsPermanent) return;

            RefreshAssociatedPlant();
            if (_associatedPlant != null)
            {
                cLocationCard.ApplyTurnEffect(_associatedPlant);
            }

            _remainingDuration--;
            Debug.LogWarning($"[SpotDataHolder] Processing turn for {cLocationCard.Name} at {transform.name}, remaining duration: {_remainingDuration}");

            if (_remainingDuration <= 0)
            {
                Debug.LogWarning($"[SpotDataHolder] Location effect {cLocationCard.Name} expired at {transform.name}");
                RemoveLocationEffect();
                cLocationCard = null;
            }
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

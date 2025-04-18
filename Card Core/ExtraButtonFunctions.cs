using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ExtraButtonFunctions : MonoBehaviour
    {
        private DeckManager _deckManager;
        private TurnController _turnController;
        private Click3D _click3D;
        private TextMeshPro _buttonText;

        private void Awake() { _click3D = GetComponent<Click3D>(); }

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _turnController = CardGameMaster.Instance.turnController;
            _buttonText = GetComponentInChildren<TextMeshPro>();
        }

        private void Update()
        {
            _buttonText.text = _turnController.newRoundReady ? "Next Round" : "End";

            if (_click3D.isEnabled == _turnController.canClickEnd && !_deckManager.updatingActionDisplay) return;
            _click3D.isEnabled = !_deckManager.updatingActionDisplay;
            _click3D.RefreshState();
        }
    }
}
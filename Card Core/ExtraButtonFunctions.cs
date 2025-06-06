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
        
        public bool isApplyButton = false;
        public bool isRedrawButton = false;

        private void Awake() { _click3D = GetComponent<Click3D>(); }

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _turnController = CardGameMaster.Instance.turnController;
            _buttonText = GetComponentInChildren<TextMeshPro>();
        }

        private void Update()
        {
            if (isApplyButton)
                _buttonText.text = _turnController.newRoundReady ? "Next Round" : "Apply";
            else if (isRedrawButton)
                _buttonText.text = _turnController.newRoundReady ? "" : "Redraw -$3";
            else 
                _buttonText.text = "PLEASE SELECT BUTTON TYPE";

            if (!_turnController.canClickEnd || _deckManager.updatingActionDisplay) 
            {
                _click3D.isEnabled = false;
                _click3D.RefreshState();
                return;
            }
            _click3D.isEnabled = !_deckManager.updatingActionDisplay;
            _click3D.RefreshState();
        }
    }
}
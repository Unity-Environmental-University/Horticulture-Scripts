using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _project.Scripts.Card_Core
{
    public class ExtraButtonFunctions : MonoBehaviour
    {
        private DeckManager _deckManager;
        private TurnController _turnController;
        private Click3D _click3D;
        private TextMeshPro _buttonText;
        private Camera _camera;
        
        public bool isApplyButton = false;
        public bool isRedrawButton = false;

        private void Awake() { _click3D = GetComponent<Click3D>(); }

        private void Start()
        {
            _camera = Camera.main;
            _deckManager = CardGameMaster.Instance.deckManager;
            _turnController = CardGameMaster.Instance.turnController;
            _buttonText = GetComponentInChildren<TextMeshPro>();
        }

        private void Update()
        {
            if (!_click3D) return;
            if (isApplyButton)
            {
                _buttonText.text = _turnController.newRoundReady ? "Next Round" : "Apply";
            }
            else if (isRedrawButton)
            {
                _buttonText.text = _turnController.newRoundReady ? "" : "Redraw -$3";

                var canAffordRedraw = _deckManager.redrawCost <= ScoreManager.GetMoneys();
                var shouldEnable = canAffordRedraw && !_turnController.newRoundReady;

                _click3D.isEnabled = shouldEnable;
                if (!shouldEnable) _click3D.mouseOver = false; // force hover off

                _click3D.RefreshState();
                return;
            }
            else
            {
                _buttonText.text = "PLEASE SELECT BUTTON TYPE";
                return;
            }

            // Apply button logic
            var shouldDisableApply = !_turnController.canClickEnd || _deckManager.updatingActionDisplay;
            var wasEnabled = _click3D.isEnabled;
            var nowEnabled = !shouldDisableApply;

            _click3D.isEnabled = nowEnabled;

            // If re-enabled this frame, and mouse is currently over the button, set hover
            if (!wasEnabled && nowEnabled)
            {
                if (_camera)
                {
                    var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (Physics.Raycast(ray, out var hit))
                    {
                        _click3D.mouseOver = hit.transform == _click3D.transform || hit.transform.IsChildOf(_click3D.transform);
                    }
                    else
                    {
                        _click3D.mouseOver = false;
                    }
                }
            }
            else if (!nowEnabled)
            {
                _click3D.mouseOver = false;
            }

            _click3D.RefreshState();
        }
    }
}
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ExtraButtonFunctions : MonoBehaviour
    {
        [SerializeField] private GameObject button;
        private DeckManager _deckManager;
        private Click3D click3D;

        private void Awake()
        {
            click3D = GetComponent<Click3D>();
        }

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        private void Update()
        {
            if (click3D.isEnabled == !_deckManager.updatingActionDisplay) return;
            click3D.isEnabled = !_deckManager.updatingActionDisplay;
            click3D.RefreshState();
        }
    }
}
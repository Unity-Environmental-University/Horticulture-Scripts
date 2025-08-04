using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    [RequireComponent(typeof(Click3D))]
    public class StickerView : MonoBehaviour
    {
        public StickerDefinition definition;
        private Click3D _click3d;

        private void Awake()
        {
            _click3d = GetComponent<Click3D>();
            _click3d.handItem = true;
            _click3d.onClick3D.AddListener(OnStickerClicked);
        }

        private void OnDestroy()
        {
            _click3d.onClick3D.RemoveListener(OnStickerClicked);
        }

        private void OnStickerClicked()
        {
            CardGameMaster.Instance.deckManager.SelectSticker(this);
        }
    }
}

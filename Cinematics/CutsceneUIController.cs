using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Cinematics
{
    public class CutsceneUIController : MonoBehaviour
    {
        private void OnEnable()
        {
            var module = CardGameMaster.Instance?.uiInputModule;
            if (module != null) module.enabled = true;
        }

        private void OnDisable()
        {
            var module = CardGameMaster.Instance?.uiInputModule;
            if (module != null) module.enabled = false;
        }
    }
}
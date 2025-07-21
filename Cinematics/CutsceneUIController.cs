using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Cinematics
{
    public class CutsceneUIController : MonoBehaviour
    {
        private void OnEnable() => CardGameMaster.Instance.uiInputModule.enabled = true;
        private void OnDisable() => CardGameMaster.Instance.uiInputModule.enabled = false;
    }
}

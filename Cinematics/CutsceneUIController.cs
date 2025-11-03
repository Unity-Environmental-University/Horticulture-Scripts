using _project.Scripts.UI;
using UnityEngine;

namespace _project.Scripts.Cinematics
{
    public class CutsceneUIController : MonoBehaviour
    {
        private void OnEnable() => UIInputManager.RequestEnable("CutsceneUI");

        private void OnDisable() => UIInputManager.RequestDisable("CutsceneUI");
    }
}
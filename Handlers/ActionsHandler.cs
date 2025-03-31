using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Handlers
{
    public class ActionsHandler : MonoBehaviour
    {
        public void ToggleNeemOil(ScriptedCollider spreader)
        {
            spreader.ToggleRole(SpreaderRole.NeemOil);
        }

        public void ToggleFungicide(ScriptedCollider spreader)
        {
            spreader.ToggleRole(SpreaderRole.Fungicide);
        }

        public void ToggleInsecticide(ScriptedCollider spreader)
        {
            spreader.ToggleRole(SpreaderRole.Insecticide);
        }

        public void ToggleSoapyWater(ScriptedCollider spreader)
        { 
            spreader.ToggleRole(SpreaderRole.SoapyWater);
        }
    }
}
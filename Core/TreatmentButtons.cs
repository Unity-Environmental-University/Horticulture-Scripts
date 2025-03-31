using _project.Scripts.UI;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class TreatmentButtons : MonoBehaviour
    {
        [SerializeField] private NotebookController notebookController;
        private SpreaderRole _currentTreatment;
        private Texture2D _currentIcon;

        public void SetIcon(Texture2D icon) => notebookController.SetCursor(_currentIcon = icon);

        public void SetTreatment(string treatmentName)
        {
            if (System.Enum.TryParse(treatmentName, out SpreaderRole treatment))
            {
                _currentTreatment = treatment;
            }
            else
            {
                Debug.LogError($"Invalid treatment name: {treatmentName}");
            }
        }

        private void ClearTreatment()
        {
            _currentTreatment = SpreaderRole.None;
            notebookController.ClearCursor();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void AddTreatmentToTable(TreatmentTable table)
        {
            if (_currentTreatment == SpreaderRole.None) return;
            table.AddTreatment(_currentTreatment, _currentIcon);
            ClearTreatment(); // Optionally clear cursor after applying
        }
    }
}
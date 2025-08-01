using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.Core
{
    public class TreatmentTable : MonoBehaviour
    {
        public Transform iconContainer;
        public GameObject iconPrefab;
        public GameObject scriptedCollider;

        private readonly List<Texture2D> _treatmentIcons = new();
        private readonly HashSet<SpreaderRole> _treatments = new();

        public void AddTreatment(SpreaderRole treatment, Texture2D icon)
        {
            if (!_treatments.Add(treatment)) return;
            _treatmentIcons.Add(icon);
            UpdateUI();
            UpdateColliders();
        }

        public void RemoveAllTreatments()
        {
            _treatments.Clear();
            _treatmentIcons.Clear();
            UpdateUI();
            UpdateColliders();
        }

        private void UpdateUI()
        {
            foreach (Transform child in iconContainer) Destroy(child.gameObject);

            foreach (var icon in _treatmentIcons)
            {
                var iconObj = Instantiate(iconPrefab, iconContainer);
                iconObj.GetComponent<Image>().sprite
                    = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
            }
        }

        private void UpdateColliders()
        {
            if (!scriptedCollider) return;
            var scComp = scriptedCollider.GetComponent<ScriptedCollider>();
            scComp.roles = SpreaderRole.None;

            foreach (var treatment in _treatments) scComp.AddRole(treatment);
        }
    }
}
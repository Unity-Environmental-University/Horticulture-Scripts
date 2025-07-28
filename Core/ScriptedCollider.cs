using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Core
{
    [Flags]
    public enum SpreaderRole
    {
        None = 0,
        Aphid = 1 << 0,        // 1
        MealyBug = 1 << 1,     // 2
        Mold = 1 << 2,         // 4
        Thrips = 1 << 3,       // 8
        HorticulturalOil = 1 << 4,      // 16
        Fungicide = 1 << 5,    // 32
        Insecticide = 1 << 6,  // 64
        SoapyWater = 1 << 7    // 128
    }

    public class ScriptedCollider : MonoBehaviour
    {
        [SerializeField] private PlantManager plantManager;
        private static readonly SpreaderRole[] AllRoles = 
            Enum.GetValues(typeof(SpreaderRole)).Cast<SpreaderRole>().ToArray();
        private SpreaderRole _previousRoles;
    
        public List<GameObject> localPlants = new();
        public SpreaderRole roles;
        public bool debugging;

        private void Awake()
        {
#if !UNITY_EDITOR
        debugging = false;
#endif
            _previousRoles = roles;
            ProcessPlants();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Plant")) return;

            if (localPlants.Contains(other.gameObject)) return;
            localPlants.Add(other.gameObject);
            if (plantManager && !plantManager.CachedPlants.Contains(other.GameObject()))
                plantManager.CachedPlants.Add(other.gameObject);
            ProcessPlants();
        }

        private void ProcessPlants()
        {
            localPlants.RemoveAll(plant => !plant);
            if (plantManager) plantManager.CachedPlants.RemoveAll(plant => !plant);

            foreach (var controller in localPlants
                         .Select(plant => plant.GetComponent<PlantController>()))
            {
                if (!controller) continue;
                ApplyRoles(controller);
            }
        }

        private void ApplyRoles(PlantController controller)
        {
            foreach (var role in AllRoles)
            {
                if (role == SpreaderRole.None)
                    continue;
                if (HasRole(role))
                    Spread(controller, role);
            }
        }

        private void Spread(PlantController controller, SpreaderRole role)
        {
            var (affliction, treatment) = ConvertRole(role);

            if (affliction != null)
            {
                if (!controller.CurrentAfflictions.Exists(a => a.Name == affliction.Name))
                {
                    controller.CurrentAfflictions.Add(affliction);
                    if (role == SpreaderRole.Mold) controller.SetMoldIntensity(Random.Range(0f, 1f));
                }
            }

            if (treatment != null)
            {
                if (!controller.CurrentTreatments.Exists(t => t.Name == treatment.Name))
                {
                    controller.CurrentTreatments.Add(treatment);
                }
            }
            controller.FlagShadersUpdate();
        }

        private bool HasRole(SpreaderRole role) => (roles & role) == role;

        public void AddRole(SpreaderRole role)
        {
            roles |= role;
            if (roles == _previousRoles) return;
            ProcessPlants();
            _previousRoles = roles;
        }

        public void ToggleRole(SpreaderRole role)
        {
            roles ^= role;
            if (roles == _previousRoles) return;
            ProcessPlants();
            _previousRoles = roles;
        }

        private (PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment) ConvertRole(
            SpreaderRole role)
        {
            if (debugging) Debug.Log($"Converting {role} to corresponding Affliction and Treatment.");

            return role switch
            {
                SpreaderRole.Aphid => (new PlantAfflictions.AphidsAffliction(), null),
                SpreaderRole.Thrips => (new PlantAfflictions.ThripsAffliction(), null),
                SpreaderRole.MealyBug => (new PlantAfflictions.MealyBugsAffliction(), null),
                SpreaderRole.Mold => (new PlantAfflictions.MildewAffliction(), null),

                SpreaderRole.HorticulturalOil => (null, new PlantAfflictions.HorticulturalOilTreatment()),
                SpreaderRole.Fungicide => (null, new PlantAfflictions.FungicideTreatment()),
                SpreaderRole.Insecticide => (null, new PlantAfflictions.InsecticideTreatment()),
                SpreaderRole.SoapyWater => (null, new PlantAfflictions.SoapyWaterTreatment()),

                _ => (null, null)
            };
        }
    }
}
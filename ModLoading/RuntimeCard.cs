using System;
using System.Collections.Generic;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using UnityEngine;

namespace _project.Scripts.ModLoading
{
    /// <summary>
    /// Data-driven action card loaded from JSON. Supports optional prefab/material from Resources
    /// and optional built-in treatment assignment.
    /// </summary>
    public class RuntimeCard : ICard
    {
        private int _value;
        private readonly string _prefabResourcePath;
        private readonly string _materialResourcePath;
        private string _bundleKey;
        private string _bundlePrefabName;
        private string _bundleMaterialName;
        private readonly Func<PlantAfflictions.ITreatment> _treatmentFactory;

        public int Weight { get; set; } = 1;

        public RuntimeCard(string name,
                           string description,
                           int value,
                           string prefabResourcePath,
                           string materialResourcePath,
                            Func<PlantAfflictions.ITreatment> treatmentFactory = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "ModCard" : name;
            Description = description;
            _value = value;
            _prefabResourcePath = prefabResourcePath;
            _materialResourcePath = materialResourcePath;
            _treatmentFactory = treatmentFactory ?? (() => new NoopTreatment());
        }

        /// <summary>
        /// Factory for bundle-backed visuals.
        /// </summary>
        public static RuntimeCard FromBundle(string name,
                                             string description,
                                             int value,
                                             string bundleKey,
                                             string prefabAssetName,
                                             string materialAssetName,
                                             Func<PlantAfflictions.ITreatment> treatmentFactory = null)
        {
            var rc = new RuntimeCard(name, description, value, null, null, treatmentFactory)
            {
                _bundleKey = bundleKey,
                _bundlePrefabName = prefabAssetName,
                _bundleMaterialName = materialAssetName
            };
            return rc;
        }

        public string Name { get; }

        public string Description { get; }

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public GameObject Prefab
        {
            get
            {
                // Prefer bundle asset if specified
                if (!string.IsNullOrWhiteSpace(_bundleKey) && !string.IsNullOrWhiteSpace(_bundlePrefabName))
                {
                    var fromBundle = ModAssets.LoadFromBundle<GameObject>(_bundleKey, _bundlePrefabName);
                    if (fromBundle) return fromBundle;
                }

                if (!string.IsNullOrWhiteSpace(_prefabResourcePath))
                {
                    var fromResources = Resources.Load<GameObject>(_prefabResourcePath);
                    if (fromResources) return fromResources;
                }

                return CardGameMaster.Instance?.actionCardPrefab;
            }
        }

        public Material Material
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_bundleKey) && !string.IsNullOrWhiteSpace(_bundleMaterialName))
                {
                    var fromBundle = ModAssets.LoadFromBundle<Material>(_bundleKey, _bundleMaterialName);
                    if (fromBundle) return fromBundle;
                }

                return !string.IsNullOrWhiteSpace(_materialResourcePath)
                    ? Resources.Load<Material>(_materialResourcePath)
                    : null;
            }
        }

        public PlantAfflictions.ITreatment Treatment => _treatmentFactory?.Invoke();
        public PlantAfflictions.IAffliction Affliction => null;

        public List<ISticker> Stickers { get; } = new();

        public void Selected()
        {
            if (CardGameMaster.Instance != null && CardGameMaster.Instance.debuggingCardClass)
                Debug.Log("Selected mod card: " + Name);
        }

        public void ModifyValue(int delta)
        {
            _value += delta;
        }

        public ICard Clone()
        {
            var clone = new RuntimeCard(Name, Description, _value, _prefabResourcePath, _materialResourcePath, _treatmentFactory)
            {
                Weight = Weight,
                _bundleKey = _bundleKey,
                _bundlePrefabName = _bundlePrefabName,
                _bundleMaterialName = _bundleMaterialName
            };
            foreach (var sticker in Stickers)
                clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }
}

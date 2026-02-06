using System;
using System.Collections.Generic;
using System.IO;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using UnityEngine;

namespace _project.Scripts.ModLoading
{
    /// <summary>
    ///     Simple mod loader for JSON cards and AssetBundle stickers.
    ///     Loads from Application.persistentDataPath/Mods and Application.streamingAssetsPath/Mods
    /// </summary>
    public static class ModLoader
    {
        /// <summary>
        ///     Load mods from both user and game directories
        /// </summary>
        public static void TryLoadMods(CardGameMaster master)
        {
            if (master?.deckManager == null) return;

            // Ensure any previously registered bundles are unloaded (domain reload off / hot-reload)
            try
            {
                ModAssets.UnloadAll();
            }
            catch
            {
                /* ignore */
            }

            LoadFromFolder(Path.Combine(Application.persistentDataPath, "Mods"), master);
            LoadFromFolder(Path.Combine(Application.streamingAssetsPath, "Mods"), master);
        }

        private static void LoadFromFolder(string folder, CardGameMaster master)
        {
            if (!Directory.Exists(folder)) return;

            LoadCards(folder, master);
            LoadStickers(folder, master);
            LoadAfflictions(folder);
        }

        /// <summary>
        ///     Load *.card.json files and register as RuntimeCards
        /// </summary>
        private static void LoadCards(string folder, CardGameMaster master)
        {
            foreach (var json in Directory.GetFiles(folder, "*.card.json", SearchOption.AllDirectories))
                try
                {
                    var text = File.ReadAllText(json);
                    var def = JsonUtility.FromJson<CardJson>(text);
                    if (string.IsNullOrEmpty(def?.name)) continue;

                    var card = !string.IsNullOrEmpty(def.bundleKey)
                        ? RuntimeCard.FromBundle(def.name, def.description, def.value, def.bundleKey, def.prefab,
                            def.material, () => CreateTreatment(def))
                        : new RuntimeCard(def.name, def.description, def.value, def.prefabResource,
                            def.materialResource, () => CreateTreatment(def));

                    card.Weight = GetWeight(def.weight, def.rarity);
                    master.deckManager.RegisterModActionPrototype(card);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModLoader] Failed to load {json}: {e.Message}");
                }
        }

        /// <summary>
        ///     Load *.bundle files and register StickerDefinition assets
        /// </summary>
        private static void LoadStickers(string folder, CardGameMaster master)
        {
            foreach (var bundlePath in Directory.GetFiles(folder, "*.bundle", SearchOption.AllDirectories))
                try
                {
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle == null) continue;

                    var key = Path.GetFileNameWithoutExtension(bundlePath);
                    ModAssets.RegisterBundle(key, bundle);

                    var stickers = bundle.LoadAllAssets<StickerDefinition>();
                    foreach (var def in stickers) master.deckManager.RegisterModSticker(def);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModLoader] Failed to load bundle {bundlePath}: {e.Message}");
                }
        }

        /// <summary>
        ///     Load *.affliction.json files and register as ModAfflictions
        /// </summary>
        private static void LoadAfflictions(string folder)
        {
            foreach (var json in Directory.GetFiles(folder, "*.affliction.json", SearchOption.AllDirectories))
                try
                {
                    var text = File.ReadAllText(json);
                    var def = JsonUtility.FromJson<AfflictionJson>(text);
                    if (string.IsNullOrEmpty(def?.name)) continue;

                    var color = def.color is { Length: >= 3 }
                        ? new Color(def.color[0], def.color[1], def.color[2], def.color.Length > 3 ? def.color[3] : 1f)
                        : Color.red;

                    var affliction = new ModAffliction(def.name, def.description, color, def.shader,
                        def.vulnerableToTreatments);

                    // Register with the mod registry for later use
                    ModAfflictionRegistry.Register(def.name, affliction);

                    Debug.Log($"[ModLoader] Loaded custom affliction: {def.name}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModLoader] Failed to load affliction {json}: {e.Message}");
                }
        }

        private static int GetWeight(int weight, string rarity)
        {
            if (weight > 0) return Mathf.Clamp(weight, 1, 10);

            // ReSharper disable once RedundantSwitchExpressionArms
            return rarity?.ToLower() switch
            {
                "common" => 5,
                "uncommon" => 3,
                "rare" => 2,
                "epic" => 1,
                _ => 1
            };
        }

        private static PlantAfflictions.ITreatment CreateTreatment(CardJson def)
        {
            // A new affliction-specific effectiveness system takes priority
            if (def.effectiveness is not { Length: > 0 })
                return CreateLegacyTreatment(def.treatment, def.infectCure, def.eggCure);
            var treatmentName = !string.IsNullOrEmpty(def.treatment) ? def.treatment : def.name;
            return new ModTreatment(treatmentName, def.description, def.effectiveness, def.isSynthetic);
        }

        private static PlantAfflictions.ITreatment CreateLegacyTreatment(string name, int? infectCure = null,
            int? eggCure = null)
        {
            if (string.IsNullOrEmpty(name)) return null;

            PlantAfflictions.ITreatment baseTreatment = name.Replace(" ", "").ToLower() switch
            {
                "horticulturaloil" => new PlantAfflictions.HorticulturalOilTreatment(),
                "fungicide" => new PlantAfflictions.FungicideTreatment(),
                "permethrin" => new PlantAfflictions.PermethrinTreatment(),
                "soapywater" => new PlantAfflictions.SoapyWaterTreatment(),
                "spinosad" => new PlantAfflictions.SpinosadTreatment(),
                "imidacloprid" => new PlantAfflictions.ImidaclopridTreatment(),
                "panacea" => new PlantAfflictions.Panacea(),
                _ => null
            };

            if (baseTreatment == null) return null;

            // If no custom values specified, return the base treatment
            if (!infectCure.HasValue && !eggCure.HasValue)
                return baseTreatment;

            // Otherwise, wrap with custom values
            return new CustomTreatmentWrapper(baseTreatment, baseTreatment.IsSynthetic, infectCure, eggCure);
        }

        [Serializable]
        private class CardJson
        {
            public string name;
            public string description;
            public int value;
            public string prefabResource;
            public string materialResource;
            public string bundleKey;
            public string prefab;
            public string material;
            public string treatment;
            public int weight = 1;
            public string rarity;
            public AfflictionEffectiveness[] effectiveness;
            public bool isSynthetic = true;
            public int? eggCure;
            public int? infectCure;
        }

        [Serializable]
        public class AfflictionEffectiveness
        {
            public string affliction;
            public int infectCure;
            public int eggCure;
        }

        [Serializable]
        private class AfflictionJson
        {
            public string name;
            public string description;
            public float[] color = { 1f, 0f, 0f, 1f }; // Default to red
            public string shader;
            public string[] vulnerableToTreatments;
        }

        /// <summary>
        ///     Custom treatment wrapper that allows overriding infect/egg cure values
        /// </summary>
        private class CustomTreatmentWrapper : PlantAfflictions.ITreatment
        {
            private readonly PlantAfflictions.ITreatment _baseTreatment;

            public CustomTreatmentWrapper(PlantAfflictions.ITreatment baseTreatment, bool isSynthetic,
                int? infectCure = null, int? eggCure = null)
            {
                _baseTreatment = baseTreatment;
                IsSynthetic = isSynthetic;
                InfectCureValue = infectCure ?? baseTreatment.InfectCureValue;
                EggCureValue = eggCure ?? baseTreatment.EggCureValue;
                Efficacy = baseTreatment.Efficacy ?? 100;
            }

            public string Name => _baseTreatment.Name;
            public string Description => _baseTreatment.Description;
            public bool IsSynthetic { get; }
            public int? InfectCureValue { get; set; }
            public int? EggCureValue { get; set; }
            public int? Efficacy { get; set; }
        }

        /// <summary>
        ///     Fully modular treatment that uses affliction-specific effectiveness instead of hardcoded type checking
        /// </summary>
        public class ModTreatment : PlantAfflictions.ITreatment
        {
            private readonly Dictionary<string, AfflictionEffectiveness> _effectiveness;

            public ModTreatment(string name, string description, AfflictionEffectiveness[] effectiveness,
                bool isSynthetic)
            {
                Name = name;
                Description = description;
                IsSynthetic = isSynthetic;
                _effectiveness = new Dictionary<string, AfflictionEffectiveness>();

                if (effectiveness != null)
                    foreach (var eff in effectiveness)
                        if (!string.IsNullOrEmpty(eff.affliction))
                            _effectiveness[eff.affliction] = eff;

                // Default fallback values
                InfectCureValue = 0;
                EggCureValue = 0;
                Efficacy = 100; // Assume fully effective by default
            }

            public string Name { get; }
            public string Description { get; }
            public bool IsSynthetic { get; }
            public int? InfectCureValue { get; set; }
            public int? EggCureValue { get; set; }
            public int? Efficacy { get; set; }

            /// <summary>
            ///     Override ApplyTreatment to use affliction-specific effectiveness
            /// </summary>
            public void ApplyTreatment(PlantController plant)
            {
                if (!plant)
                {
                    Debug.LogWarning("PlantController is null, cannot apply treatment.");
                    return;
                }

                var afflictions = plant.CurrentAfflictions != null
                    ? new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions)
                    : new List<PlantAfflictions.IAffliction>();

                if (afflictions.Count == 0) Debug.LogWarning("No afflictions found on the plant.");

                foreach (var affliction in afflictions)
                {
                    var (infectCure, eggCure) = GetEffectivenessFor(affliction.Name);
                    if (infectCure > 0 || eggCure > 0)
                    {
                        // Create a temporary treatment wrapper with specific effectiveness for this affliction
                        var afflictionSpecificTreatment = new CustomTreatmentWrapper(this, IsSynthetic, infectCure, eggCure);
                        affliction.TreatWith(afflictionSpecificTreatment, plant);

                        if (CardGameMaster.Instance?.debuggingCardClass == true)
                            Debug.Log(
                                $"Applied {Name} to {affliction.Name}: infectCure={infectCure}, eggCure={eggCure}");
                    }
                    else if (CardGameMaster.Instance?.debuggingCardClass == true)
                    {
                        Debug.Log($"{Name} has no effect on {affliction.Name}");
                    }
                }
            }

            /// <summary>
            ///     Get effectiveness for a specific affliction by name
            /// </summary>
            public (int infectCure, int eggCure) GetEffectivenessFor(string afflictionName)
            {
                return _effectiveness.TryGetValue(afflictionName, out var eff)
                    ? (eff.infectCure, eff.eggCure)
                    : (0, 0); // No effect on unknown afflictions
            }
        }
    }
}
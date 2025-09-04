using System;
using System.IO;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using UnityEngine;

namespace _project.Scripts.ModLoader
{
    /// <summary>
    /// Simple mod loader for JSON cards and AssetBundle stickers.
    /// Loads from Application.persistentDataPath/Mods and Application.streamingAssetsPath/Mods
    /// </summary>
    public static class ModLoader
    {
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
        }

        /// <summary>
        /// Load mods from both user and game directories
        /// </summary>
        public static void TryLoadMods(CardGameMaster master)
        {
            if (master?.deckManager == null) return;

            LoadFromFolder(Path.Combine(Application.persistentDataPath, "Mods"), master);
            LoadFromFolder(Path.Combine(Application.streamingAssetsPath, "Mods"), master);
        }

        private static void LoadFromFolder(string folder, CardGameMaster master)
        {
            if (!Directory.Exists(folder)) return;

            LoadCards(folder, master);
            LoadStickers(folder, master);
        }

        /// <summary>
        /// Load *.card.json files and register as RuntimeCards
        /// </summary>
        private static void LoadCards(string folder, CardGameMaster master)
        {
            foreach (var json in Directory.GetFiles(folder, "*.card.json", SearchOption.AllDirectories))
            {
                try
                {
                    var text = File.ReadAllText(json);
                    var def = JsonUtility.FromJson<CardJson>(text);
                    if (string.IsNullOrEmpty(def?.name)) continue;

                    var card = !string.IsNullOrEmpty(def.bundleKey) 
                        ? RuntimeCard.FromBundle(def.name, def.description, def.value, def.bundleKey, def.prefab, def.material, () => CreateTreatment(def.treatment))
                        : new RuntimeCard(def.name, def.description, def.value, def.prefabResource, def.materialResource, () => CreateTreatment(def.treatment));

                    card.Weight = GetWeight(def.weight, def.rarity);
                    master.deckManager.RegisterModActionPrototype(card);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModLoader] Failed to load {json}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Load *.bundle files and register StickerDefinition assets
        /// </summary>
        private static void LoadStickers(string folder, CardGameMaster master)
        {
            foreach (var bundlePath in Directory.GetFiles(folder, "*.bundle", SearchOption.AllDirectories))
            {
                try
                {
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle == null) continue;

                    var key = Path.GetFileNameWithoutExtension(bundlePath);
                    ModAssets.RegisterBundle(key, bundle);

                    var stickers = bundle.LoadAllAssets<StickerDefinition>();
                    foreach (var def in stickers)
                    {
                        master.deckManager.RegisterModSticker(def);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModLoader] Failed to load bundle {bundlePath}: {e.Message}");
                }
            }
        }

        private static int GetWeight(int weight, string rarity)
        {
            if (weight > 0) return Mathf.Clamp(weight, 1, 10);
            
            return rarity?.ToLower() switch
            {
                "common" => 5,
                "uncommon" => 3,
                "rare" => 2,
                "epic" => 1,
                _ => 1
            };
        }

        private static PlantAfflictions.ITreatment CreateTreatment(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            return name.Replace(" ", "").ToLower() switch
            {
                "horticulturaloil" => new PlantAfflictions.HorticulturalOilTreatment(),
                "fungicide" => new PlantAfflictions.FungicideTreatment(),
                "insecticide" => new PlantAfflictions.InsecticideTreatment(),
                "soapywater" => new PlantAfflictions.SoapyWaterTreatment(),
                "spinosad" => new PlantAfflictions.SpinosadTreatment(),
                "imidacloprid" => new PlantAfflictions.ImidaclopridTreatment(),
                "panacea" => new PlantAfflictions.Panacea(),
                _ => null
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using UnityEngine;

namespace _project.Scripts.Core
{
    /// <summary>
    /// Lightweight mod loader that discovers runtime cards (JSON) and stickers (AssetBundle)
    /// from a Mods folder and registers them into the live DeckManager before decks initialize.
    /// </summary>
    public static class ModLoader
    {
        [Serializable]
        private class CardJson
        {
            public string name;
            public string description;
            // Cost to play (alias: cost); negative is cost as per game convention
            public int value;
            public int? cost;            // optional alias for value
            // Option A: load visuals from Resources
            public string prefabResource;   // Optional Resources path to a prefab
            public string materialResource; // Optional Resources path to a material
            // Option B: load visuals from an AssetBundle previously discovered in Mods
            public string bundleKey;        // Bundle key (defaults to bundle file name without extension)
            public string prefab;           // Asset name inside the bundle
            public string material;         // Asset name inside the bundle
            // Gameplay
            public string treatment;        // Optional: built-in treatment name (e.g., "SoapyWater", "Fungicide")
            // Appearance probability
            public int? weight;             // Optional multiplicity weight (>=1)
            public string rarity;           // Optional shorthand: Common/Uncommon/Rare/Epic/Legendary
        }

        public static void TryLoadMods(CardGameMaster master)
        {
            if (master == null || master.deckManager == null)
            {
                Debug.LogWarning("[ModLoader] CardGameMaster or DeckManager not present; skipping mod load.");
                return;
            }

            // Prefer user-writable location, but also scan StreamingAssets so shipped mods work out of the box
            var roots = new List<string>();
            if (!string.IsNullOrEmpty(Application.persistentDataPath))
                roots.Add(Path.Combine(Application.persistentDataPath, "Mods"));
            if (!string.IsNullOrEmpty(Application.streamingAssetsPath))
                roots.Add(Path.Combine(Application.streamingAssetsPath, "Mods"));

            foreach (var root in roots)
            {
                LoadFromFolder(root, master);
            }
        }

        private static void LoadFromFolder(string folder, CardGameMaster master)
        {
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
                Debug.Log($"[ModLoader] Scanning mods at {folder}");

                // 1) JSON Runtime Cards
                foreach (var json in Directory.GetFiles(folder, "*.card.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        var text = File.ReadAllText(json);
                        var def = JsonUtility.FromJson<CardJson>(text);
                        if (def == null || string.IsNullOrWhiteSpace(def.name))
                        {
                            Debug.LogWarning($"[ModLoader] Invalid card JSON: {json}");
                            continue;
                        }

                        // Prefer explicit cost if provided
                        var cost = def.cost.HasValue ? def.cost.Value : def.value;

                        RuntimeCard card;
                        if (!string.IsNullOrWhiteSpace(def.bundleKey))
                        {
                            // Bundle-backed visuals
                            card = RuntimeCard.FromBundle(
                                def.name,
                                def.description,
                                cost,
                                def.bundleKey,
                                def.prefab,
                                def.material,
                                () => CreateTreatmentByName(def.treatment)
                            );
                        }
                        else
                        {
                            // Resources-backed visuals
                            card = new RuntimeCard(
                                def.name,
                                def.description,
                                cost,
                                def.prefabResource,
                                def.materialResource,
                                () => CreateTreatmentByName(def.treatment)
                            );
                        }

                        // Apply weight/rarity
                        card.Weight = ResolveWeight(def.weight, def.rarity);

                        master.deckManager.RegisterModActionPrototype(card);
                        Debug.Log($"[ModLoader] Registered mod card: {def.name}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModLoader] Failed to load card JSON '{json}': {e.Message}");
                    }
                }

                // 2) AssetBundles with StickerDefinition assets
                foreach (var bundlePath in Directory.GetFiles(folder, "*.bundle", SearchOption.AllDirectories))
                {
                    AssetBundle bundle = null;
                    try
                    {
                        bundle = AssetBundle.LoadFromFile(bundlePath);
                        if (bundle == null)
                        {
                            Debug.LogWarning($"[ModLoader] Failed to load bundle: {bundlePath}");
                            continue;
                        }

                        // Register bundle in the mod asset registry for later card visual lookup
                        var key = Path.GetFileNameWithoutExtension(bundlePath);
                        ModAssets.RegisterBundle(key, bundle);

                        var stickers = bundle.LoadAllAssets<StickerDefinition>();
                        foreach (var def in stickers)
                        {
                            master.deckManager.RegisterModSticker(def);
                            Debug.Log($"[ModLoader] Registered mod sticker: {def.name}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModLoader] Error reading bundle '{bundlePath}': {e.Message}");
                    }
                    // IMPORTANT: keep the bundle loaded so cards can resolve assets at runtime
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ModLoader] Error scanning folder '{folder}': {e.Message}");
            }
        }

        private static int ResolveWeight(int? weight, string rarity)
        {
            if (weight.HasValue && weight.Value > 0) return Mathf.Clamp(weight.Value, 1, 50);
            if (string.IsNullOrWhiteSpace(rarity)) return 1;

            switch (rarity.Trim().ToLowerInvariant())
            {
                case "common": return 6;
                case "uncommon": return 3;
                case "rare": return 2;
                case "epic": return 1;
                case "legendary": return 1;
                default: return 1;
            }
        }

        private static PlantAfflictions.ITreatment CreateTreatmentByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var key = name.Replace(" ", string.Empty).Trim().ToLowerInvariant();
            return key switch
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

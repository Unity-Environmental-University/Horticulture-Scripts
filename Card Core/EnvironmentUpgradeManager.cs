using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class EnvironmentUpgradeManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> upgradeSpawnPoints = new();

        private readonly List<IEnvironmentUpgrade> _activeUpgrades = new();
        private readonly Dictionary<IEnvironmentUpgrade, GameObject> _spawnedPrefabs = new();
        private int _nextSpawnPointIndex;

        public GameObject beeBoxPrefab;
        public IReadOnlyList<IEnvironmentUpgrade> ActiveUpgrades => _activeUpgrades;


        /// <summary>
        ///     Checks if an upgrade can be purchased.
        /// </summary>
        public bool CanPurchaseUpgrade(IEnvironmentUpgrade upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogError("[EnvironmentUpgradeManager] Cannot purchase null upgrade");
                return false;
            }

            if (_activeUpgrades.Any(u => u.GetType() == upgrade.GetType()))
            {
                Debug.LogWarning($"[EnvironmentUpgradeManager] {upgrade.DisplayName} already purchased");
                return false;
            }

            if (upgradeSpawnPoints.Count <= 0 || _nextSpawnPointIndex < upgradeSpawnPoints.Count) return true;
            Debug.LogWarning("[EnvironmentUpgradeManager] Cannot purchase upgrade: all spawn points are occupied");
            return false;
        }

        /// <summary>
        ///     Purchases an upgrade, adds it to active upgrades, and spawns its prefab.
        /// </summary>
        public bool PurchaseUpgrade(IEnvironmentUpgrade upgrade)
        {
            if (!CanPurchaseUpgrade(upgrade))
                return false;

            _activeUpgrades.Add(upgrade);
            SpawnUpgradePrefab(upgrade);

            Debug.Log($"[EnvironmentUpgradeManager] Purchased: {upgrade.DisplayName}");
            return true;
        }

        /// <summary>
        ///     Injects bonuses from all active upgrades into the ScoreManager.
        /// </summary>
        public void InjectBonuses(ScoreManager scoreManager, int healthyPlants, int totalPlants)
        {
            if (!scoreManager)
            {
                Debug.LogError("[EnvironmentUpgradeManager] Cannot inject bonuses: ScoreManager is null");
                return;
            }

            foreach (var upgrade in _activeUpgrades)
            {
                var bonus = upgrade.CalculateRoundBonus(healthyPlants, totalPlants);
                if (bonus == null) continue;
                scoreManager.bonuses.Add(bonus);
                Debug.Log($"[EnvironmentUpgradeManager] Added bonus: {bonus.Name} = ${bonus.BonusValue}");
            }
        }

        /// <summary>
        ///     Clears upgrades that last only one round.
        ///     Called at the end of each round.
        /// </summary>
        public void ClearRoundUpgrades()
        {
            var roundUpgrades = _activeUpgrades.Where
                (u => u.Duration == UpgradeDuration.OneRound).ToList();

            foreach (var upgrade in roundUpgrades)
            {
                // Destroy spawned prefab
                if (_spawnedPrefabs.TryGetValue(upgrade, out var prefab) && prefab)
                {
                    Destroy(prefab);
                    _spawnedPrefabs.Remove(upgrade);
                }

                // Remove from the active list
                _activeUpgrades.Remove(upgrade);
            }

            if (roundUpgrades.Count > 0)
                Debug.Log($"[EnvironmentUpgradeManager] Cleared {roundUpgrades.Count} per-round upgrade(s)");
        }

        /// <summary>
        ///     Clears all active upgrades and destroys spawned prefabs.
        ///     Called at the end of each level.
        /// </summary>
        public void ClearUpgrades()
        {
            foreach (var kvp in _spawnedPrefabs.Where(kvp => kvp.Value))
                Destroy(kvp.Value);

            _spawnedPrefabs.Clear();
            _activeUpgrades.Clear();
            _nextSpawnPointIndex = 0;

            Debug.Log("[EnvironmentUpgradeManager] Cleared all upgrades");
        }

        /// <summary>
        ///     Serializes active upgrades for save system.
        /// </summary>
        public List<string> SerializeUpgrades()
        {
            return _activeUpgrades.Select(u => u.GetType().FullName).ToList();
        }

        /// <summary>
        ///     Restores upgrades from serialized type names (for load system).
        /// </summary>
        public void RestoreUpgrades(List<string> typeNames)
        {
            if (typeNames == null || typeNames.Count == 0) return;

            ClearUpgrades();

            foreach (var typeName in typeNames)
                try
                {
                    var type = Type.GetType(typeName);
                    if (type is null)
                    {
                        Debug.LogWarning($"[EnvironmentUpgradeManager] Could not find type: {typeName}");
                        continue;
                    }

                    if (!typeof(IEnvironmentUpgrade).IsAssignableFrom(type))
                    {
                        Debug.LogWarning(
                            $"[EnvironmentUpgradeManager] Type {typeName} does not implement IEnvironmentUpgrade");
                        continue;
                    }

                    var upgrade = (IEnvironmentUpgrade)Activator.CreateInstance(type);
                    _activeUpgrades.Add(upgrade);
                    SpawnUpgradePrefab(upgrade);

                    Debug.Log($"[EnvironmentUpgradeManager] Restored: {upgrade.DisplayName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EnvironmentUpgradeManager] Failed to restore upgrade {typeName}: {ex.Message}");
                }
        }

        private void SpawnUpgradePrefab(IEnvironmentUpgrade upgrade)
        {
            if (!upgrade.Prefab)
            {
                Debug.LogWarning($"[EnvironmentUpgradeManager] {upgrade.DisplayName} has no prefab to spawn");
                return;
            }

            if (upgradeSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[EnvironmentUpgradeManager] No spawn points configured");
                return;
            }

            if (_nextSpawnPointIndex >= upgradeSpawnPoints.Count)
            {
                Debug.LogWarning("[EnvironmentUpgradeManager] All spawn points occupied");
                return;
            }

            var spawnPoint = upgradeSpawnPoints[_nextSpawnPointIndex];
            var instance = Instantiate(upgrade.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            _spawnedPrefabs[upgrade] = instance;
            _nextSpawnPointIndex++;

            Debug.Log(
                $"[EnvironmentUpgradeManager] Spawned {upgrade.DisplayName} at spawn point {_nextSpawnPointIndex - 1}");
        }
    }
}

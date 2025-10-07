using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    /// Manages visual health bar icons representing plant infections and eggs from various pest afflictions.
    /// Displays color-coded icons for different affliction types with object pooling for performance.
    /// </summary>
    public class PlantHealthBarHandler : MonoBehaviour
    {
        /// <summary>
        /// Reference to the plant controller this health bar represents.
        /// If not assigned, will be found via GetComponentInParent in Start().
        /// </summary>
        [SerializeField] private PlantController plantController;

        /// <summary>
        /// Shared configuration containing materials and prefabs for all health bar handlers.
        /// Assign directly or place a PlantHealthBarConfig component on the PlantController hierarchy.
        /// </summary>
        [Header("Configuration")]
        [SerializeField] private PlantHealthBarConfig config;

        /// <summary>
        /// Parent GameObject for infection icons.
        /// </summary>
        public GameObject infectBarParent;

        /// <summary>
        /// Parent GameObject for egg icons.
        /// </summary>
        public GameObject eggBarParent;

        [Header("Behavior")]
        [Tooltip("If true, we reuse spawned icons and enable/disable instead of destroying them.")]
        [SerializeField]
        private bool usePooling = true;

        [Tooltip(
            "If true, we will position icons manually with fixed spacing (avoids dependence on UI layout components).")]
        [SerializeField]
        private bool overrideLayoutPositions;

        [Header("Manual Layout")]
        [Tooltip("If enabled, compute spacing from icon Renderer bounds and add a small gap.")]
        [SerializeField]
        private bool autoSpacing = true;

        [Tooltip("Gap multiplier when autoSpacing is enabled (1.15 = 15% gap).")] [SerializeField]
        private float spacingMultiplier = 1.15f;

        [Tooltip("Fallback X spacing (meters) when autoSpacing cannot determine size.")] [SerializeField]
        private float iconSpacingX = 0.05f;

        [Tooltip("Row spacing for wrapped layout (meters).")] [SerializeField]
        private float iconSpacingY = 0.05f;

        [Tooltip("Wrap to a new row after this many icons (0 = single row).")] [SerializeField]
        private int wrapAfter = 6;

        [Tooltip("Center-align each row relative to parent origin when overriding layout.")] [SerializeField]
        private bool centerAlign = true;

        [Header("Billboard/Anchor")]
        [Tooltip("If true, this handler will face the main camera each LateUpdate (billboard style).")]
        [SerializeField]
        private bool faceCamera;

        [Tooltip("Optional: Transform to rotate when faceCamera is on. Defaults to this transform.")] [SerializeField]
        private Transform billboardTransform;

        [Tooltip("If true, this handler will anchor above the plant's renderer bounds with a small offset.")]
        [SerializeField]
        private bool anchorToPlantBounds;

        [Tooltip("Optional: Transform to position when anchoring. Defaults to this transform.")] [SerializeField]
        private Transform anchorTransform;

        [Tooltip("Vertical offset above the top of the renderer bounds when anchoring to plant bounds.")]
        [SerializeField]
        private float anchorHeightOffset = 0.05f;

        /// <summary>
        /// Tracks metadata for each health bar icon
        /// </summary>
        private class IconData
        {
            public Transform IconTransform { get; }
            public Renderer CachedRenderer { get; }
            public string AfflictionSource { get; set; }

            public IconData(Transform transform, Renderer renderer, string source = null)
            {
                IconTransform = transform;
                CachedRenderer = renderer;
                AfflictionSource = source;
            }
        }

        private readonly List<IconData> _eggIcons = new();
        private readonly List<IconData> _infectIcons = new();
        private int _lastEggTotal = -1;
        private int _lastInfectTotal = -1;
        private readonly Dictionary<string, (int infect, int eggs)> _lastAfflictionState = new();
        private Renderer[] _plantRenderers;
        private Camera _mainCamera;
        private IPlantCard _plantCardInterface;
        private Dictionary<string, Material> _afflictionMaterials;

        private void Awake()
        {
            if (plantController == null)
            {
                plantController = GetComponentInParent<PlantController>();
            }

            if (config == null && plantController && plantController.TryGetComponent(out PlantHealthBarConfig controllerConfig))
            {
                config = controllerConfig;
            }

            if (config == null)
            {
                config = GetComponentInParent<PlantHealthBarConfig>();
            }

            if (config == null)
            {
                Debug.LogError($"[PlantHealthBarHandler] No PlantHealthBarConfig assigned on {gameObject.name}. Assign it directly or place PlantHealthBarConfig on the PlantController.");
                enabled = false;
                return;
            }

            if (!config.IsValid())
            {
                Debug.LogError($"[PlantHealthBarHandler] PlantHealthBarConfig is invalid on {gameObject.name}. Component will not function.");
                enabled = false;
                return;
            }

            // Initialize affliction-to-material mapping from config
            _afflictionMaterials = new Dictionary<string, Material>
            {
                ["Aphids"] = config.aphidsMaterial,
                ["MealyBugs"] = config.mealybugsMaterial,
                ["Thrips"] = config.thripsMaterial,
                ["Mildew"] = config.mildewMaterial,
                ["Fungus Gnats"] = config.gnatsMaterial,
                ["Spider Mites"] = config.spiderMitesMaterial
            };
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                Debug.LogWarning(
                    $"[PlantHealthBarHandler] Main camera not found on {gameObject.name}. Billboard/face camera features will not work.");

            if (plantController == null) plantController = GetComponentInParent<PlantController>();

            if (plantController == null)
            {
                Debug.LogError(
                    $"[PlantHealthBarHandler] No PlantController found on {gameObject.name} or its parents. Component will not function.");
                enabled = false;
                return;
            }

            // Cache plant renderers (tagged "Plant" like PlantController does)
            _plantRenderers = plantController
                .GetComponentsInChildren<Renderer>(true)
                .Where(r => r.CompareTag("Plant"))
                .ToArray();

            // Cache the IPlantCard interface for performance
            _plantCardInterface = plantController?.PlantCard as IPlantCard;

            // Prime pools with any existing children in the bar parents (so designers can place a few by hand)
            PrimeExistingIcons(infectBarParent, _infectIcons);
            PrimeExistingIcons(eggBarParent, _eggIcons);

            SpawnHearts(plantController);
        }

        /// <summary>
        /// Gets the appropriate material for a given affliction source.
        /// </summary>
        /// <param name="afflictionSource">The name of the affliction (e.g., "Aphids", "Thrips")</param>
        /// <param name="isEgg">True if this is for an egg icon, false for infection icon</param>
        /// <returns>The material to apply, or null if no match found</returns>
        private Material GetMaterialForAffliction(string afflictionSource, bool isEgg)
        {
            return isEgg ? config.eggsMaterial : _afflictionMaterials.GetValueOrDefault(afflictionSource);
        }

        private void Update()
        {
            if (_plantCardInterface == null) return;

            var infectData = _plantCardInterface.Infect;
            var currentInfectTotal = infectData.InfectTotal;
            var currentEggTotal = infectData.EggTotal;
            var totalsChanged = currentInfectTotal != _lastInfectTotal || currentEggTotal != _lastEggTotal;

            var mixChanged = false;
            if (!totalsChanged)
            {
                var state = infectData.All.ToList();
                if (state.Count != _lastAfflictionState.Count)
                {
                    mixChanged = true;
                }
                else
                {
                    foreach (var kvp in state)
                    {
                        var currentCounts = (kvp.Value.infect, kvp.Value.eggs);
                        if (!_lastAfflictionState.TryGetValue(kvp.Key, out var previousCounts) ||
                            previousCounts != currentCounts)
                        {
                            mixChanged = true;
                            break;
                        }
                    }
                }
            }

            if (!totalsChanged && !mixChanged) return;
            SyncBarsFromPlant();
        }

        private void LateUpdate()
        {
            if (!plantController) return;
            
            if (faceCamera && _mainCamera)
            {
                var cam = _mainCamera.transform;
                var rotTarget = billboardTransform ? billboardTransform : transform;
                
                if (!plantController || rotTarget != plantController.transform)
                    rotTarget.rotation =
                        Quaternion.LookRotation((rotTarget.position - cam.position).normalized, Vector3.up);
            }
            
            if (!anchorToPlantBounds) return;
            var posTarget = anchorTransform ? anchorTransform : transform;
            
            if (plantController && posTarget == plantController.transform) return;
            var hasBounds = TryGetPlantBounds(out var bounds);
            if (!hasBounds) return;
            var top = bounds.center + new Vector3(0, bounds.extents.y + anchorHeightOffset, 0);
            posTarget.position = top;
        }

        /// <summary>
        /// Initializes and spawns health bar icons for the given plant.
        /// Called during Start() to set up the initial health bar state.
        /// </summary>
        /// <param name="plant">The plant controller to visualize health for</param>
        public void SpawnHearts(PlantController plant)
        {
            SyncBarsFromPlant();
        }

        /// <summary>
        /// Syncs both infection and egg bars based on current plant affliction data.
        /// </summary>
        private void SyncBarsFromPlant()
        {
            if (_plantCardInterface == null)
                return;

            var infectData = _plantCardInterface.Infect;

            // Build lists of icons with their affliction sources
            var infectIconsToShow = new List<IconData>();
            var eggIconsToShow = new List<IconData>();
            _lastAfflictionState.Clear();

            foreach (var (source, data) in infectData.All)
            {
                _lastAfflictionState[source] = (data.infect, data.eggs);

                // Add infection icons
                for (var i = 0; i < data.infect; i++)
                {
                    infectIconsToShow.Add(new IconData(null, null, source));
                }

                // Add egg icons
                for (var i = 0; i < data.eggs; i++)
                {
                    eggIconsToShow.Add(new IconData(null, null, source));
                }
            }

            // Sync the bars with affliction-specific materials
            SyncBarWithMaterials(infectBarParent, config.heartPrefab, infectIconsToShow, _infectIcons, false);
            SyncBarWithMaterials(eggBarParent, config.eggPrefab, eggIconsToShow, _eggIcons, true);
            _lastInfectTotal = infectData.InfectTotal;
            _lastEggTotal = infectData.EggTotal;
        }

        /// <summary>
        /// Syncs a bar by creating/reusing icons and assigning materials based on affliction sources.
        /// </summary>
        /// <param name="barParent">Parent GameObject for the icons</param>
        /// <param name="prefab">Prefab to instantiate for each icon</param>
        /// <param name="iconsToShow">List of IconData specifying what icons to show and their affliction sources</param>
        /// <param name="pool">Pool of existing IconData for reuse</param>
        /// <param name="isEgg">True if syncing eggs, false if syncing infections</param>
        private void SyncBarWithMaterials(GameObject barParent, GameObject prefab, List<IconData> iconsToShow,
            List<IconData> pool, bool isEgg)
        {
            if (!barParent || !prefab) return;
            var t = barParent.transform;
            var targetCount = iconsToShow.Count;

            if (usePooling)
            {
                // Ensure we have enough pooled items
                while (pool.Count < targetCount)
                {
                    var go = Instantiate(prefab, t, false);
                    var iconRenderer = go.GetComponentInChildren<Renderer>();
                    pool.Add(new IconData(go.transform, iconRenderer));
                }

                // Assign affliction sources and materials to icons
                for (var i = 0; i < targetCount; i++)
                {
                    var iconData = pool[i];
                    var targetData = iconsToShow[i];

                    iconData.AfflictionSource = targetData.AfflictionSource;

                    // Apply material based on affliction source (use sharedMaterial to avoid instance leaks)
                    var material = GetMaterialForAffliction(targetData.AfflictionSource, isEgg);
                    if (!material)
                    {
                        Debug.LogWarning($"[PlantHealthBarHandler] No material found for affliction '{targetData.AfflictionSource}' (isEgg: {isEgg}) on {gameObject.name}");
                    }
                    else if (iconData.CachedRenderer)
                    {
                        iconData.CachedRenderer.sharedMaterial = material;
                    }

                    // Ensure icon is active
                    if (iconData.IconTransform && !iconData.IconTransform.gameObject.activeSelf)
                        iconData.IconTransform.gameObject.SetActive(true);
                }

                // Deactivate excess icons
                for (var i = targetCount; i < pool.Count; i++)
                {
                    var iconData = pool[i];
                    if (iconData?.IconTransform && iconData.IconTransform.gameObject.activeSelf)
                        iconData.IconTransform.gameObject.SetActive(false);
                }

                // Layout icons
                LayoutIconsFromData(pool, targetCount, prefab);
            }
            else
            {
                // Non-pooling fallback: simple instantiate/destroy with material assignment
                var current = t.childCount;

                // Destroy excess children
                while (current > targetCount)
                {
                    current--;
                    var child = t.GetChild(current);
                    if (child) Destroy(child.gameObject);
                }

                // Create new children or update existing ones
                for (var i = 0; i < targetCount; i++)
                {
                    Transform childTransform;
                    if (i < t.childCount)
                    {
                        childTransform = t.GetChild(i);
                    }
                    else
                    {
                        var go = Instantiate(prefab, t, false);
                        childTransform = go.transform;
                    }

                    // Apply material (use sharedMaterial to avoid instance leaks)
                    var targetData = iconsToShow[i];
                    var material = GetMaterialForAffliction(targetData.AfflictionSource, isEgg);
                    if (!material)
                    {
                        Debug.LogWarning($"[PlantHealthBarHandler] No material found for affliction '{targetData.AfflictionSource}' (isEgg: {isEgg}) on {gameObject.name}");
                        continue;
                    }
                    if (!childTransform) continue;

                    var iconRenderer = childTransform.GetComponentInChildren<Renderer>();
                    if (iconRenderer)
                    {
                        iconRenderer.sharedMaterial = material;
                    }
                }

                // Layout children
                LayoutChildren(t, targetCount, prefab);
            }
        }


        /// <summary>
        /// Populates the icon pool with existing child icons from the parent GameObject.
        /// Allows designers to pre-place icons in the scene hierarchy.
        /// </summary>
        /// <param name="parent">Parent GameObject containing pre-existing icons</param>
        /// <param name="pool">Icon pool to populate with existing children</param>
        private static void PrimeExistingIcons(GameObject parent, List<IconData> pool)
        {
            pool.Clear();
            if (!parent) return;
            var t = parent.transform;
            for (var i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (!child) continue;
                var iconRenderer = child.GetComponentInChildren<Renderer>();
                pool.Add(new IconData(child, iconRenderer));
            }
        }

        /// <summary>
        /// Positions icons from an IconData list in a grid layout with configurable spacing and wrapping.
        /// Used by material-aware syncing to arrange affliction-specific icons.
        /// </summary>
        /// <param name="pool">Pool of IconData containing transforms to position</param>
        /// <param name="targetCount">Number of icons to layout (starting from index 0)</param>
        /// <param name="prefab">Prefab reference for auto-spacing calculations</param>
        private void LayoutIconsFromData(List<IconData> pool, int targetCount, GameObject prefab)
        {
            // Gather active icons up to targetCount
            var actives = new List<Transform>(targetCount);
            for (var i = 0; i < pool.Count && actives.Count < targetCount; i++)
            {
                var iconData = pool[i];
                if (iconData?.IconTransform && iconData.IconTransform.gameObject.activeSelf)
                    actives.Add(iconData.IconTransform);
            }

            if (actives.Count == 0) return;

            // Spacing determination
            var sx = iconSpacingX;
            var sy = iconSpacingY;
            if (autoSpacing)
            {
                var (sizeX, sizeY) = TryGetIconLocalSize(actives[0], prefab);
                if (sizeX > 0f) sx = sizeX * spacingMultiplier;
                if (sizeY > 0f) sy = sizeY * spacingMultiplier;
            }

            var maxPerRow = Mathf.Max(0, wrapAfter);
            if (maxPerRow == 0)
            {
                // single row centered optionally
                var count = actives.Count;
                var startX = centerAlign ? -((count - 1) * sx) * 0.5f : 0f;
                for (var i = 0; i < count; i++)
                {
                    var tr = actives[i];
                    if (!tr) continue;
                    tr.localPosition = new Vector3(startX + i * sx, 0f, 0f);
                }

                return;
            }

            // wrapped grid layout (rows downward)
            var total = actives.Count;
            var rows = Mathf.CeilToInt(total / (float)maxPerRow);
            for (var row = 0; row < rows; row++)
            {
                var startIndex = row * maxPerRow;
                var itemsInRow = Mathf.Min(maxPerRow, total - startIndex);
                var startX = centerAlign ? -((itemsInRow - 1) * sx) * 0.5f : 0f;
                for (var col = 0; col < itemsInRow; col++)
                {
                    var idx = startIndex + col;
                    var tr = actives[idx];
                    if (!tr) continue;
                    var x = startX + col * sx;
                    var y = -(row * sy);
                    tr.localPosition = new Vector3(x, y, 0f);
                }
            }
        }


        /// <summary>
        /// Attempts to determine the local-space size of an icon for automatic spacing calculations.
        /// First tries to use an existing icon's renderer bounds,
        /// then falls back to instantiating the prefab temporarily.
        /// </summary>
        /// <param name="icon">Existing icon transform to measure (can be null)</param>
        /// <param name="prefab">Prefab to temporarily instantiate if icon is null or has no renderer</param>
        /// <returns>Tuple of (width, height) in local space, or (0, 0) if size cannot be determined</returns>
        private static (float sizeX, float sizeY) TryGetIconLocalSize(Transform icon, GameObject prefab)
        {
            // Prefer an existing active icon's renderer bounds
            var r = icon ? icon.GetComponentInChildren<Renderer>() : null;
            if (r)
            {
                var world = r.bounds.size;
                var s = icon.lossyScale;
                var lx = s.x != 0 ? world.x / s.x : world.x;
                var ly = s.y != 0 ? world.y / s.y : world.y;
                return (lx, ly);
            }

            // Fallback: temporary instance of prefab just to measure
            if (!prefab) return (0f, 0f);
            var temp = Instantiate(prefab);
            try
            {
                var rr = temp.GetComponentInChildren<Renderer>();
                if (rr)
                {
                    var sizeWorld = rr.bounds.size;
                    var s2 = temp.transform.lossyScale;
                    var lx2 = s2.x != 0 ? sizeWorld.x / s2.x : sizeWorld.x;
                    var ly2 = s2.y != 0 ? sizeWorld.y / s2.y : sizeWorld.y;
                    return (lx2, ly2);
                }
            }
            finally
            {
                Destroy(temp);
            }

            return (0f, 0f);
        }

        /// <summary>
        /// Positions child transforms in a simple horizontal row layout.
        /// Used as fallback when not using object pooling.
        /// </summary>
        /// <param name="parent">Parent transform containing children to layout</param>
        /// <param name="targetCount">Number of children to position</param>
        /// <param name="prefab">Prefab reference for auto-spacing calculations</param>
        private void LayoutChildren(Transform parent, int targetCount, GameObject prefab)
        {
            // Collect active children up to targetCount
            var actives = new List<Transform>(targetCount);
            for (var i = 0; i < parent.childCount && actives.Count < targetCount; i++)
            {
                var ch = parent.GetChild(i);
                if (ch && ch.gameObject.activeSelf) actives.Add(ch);
            }

            if (actives.Count == 0) return;

            // Spacing
            var sx = iconSpacingX;
            if (autoSpacing)
            {
                var (sizeX, _) = TryGetIconLocalSize(actives[0], prefab);
                if (sizeX > 0f) sx = sizeX * spacingMultiplier;
            }

            // Simple single-row centered
            var count = actives.Count;
            var startX = centerAlign ? -((count - 1) * sx) * 0.5f : 0f;
            for (var i = 0; i < count; i++)
            {
                var tr = actives[i];
                if (!tr) continue;
                tr.localPosition = new Vector3(startX + i * sx, 0f, 0f);
            }
        }

        /// <summary>
        /// Calculates the combined bounds of all plant renderers.
        /// Used for anchoring the health bar above the plant.
        /// </summary>
        /// <param name="bounds">Output: Combined bounds of all plant renderers</param>
        /// <returns>True if at least one valid renderer was found, false otherwise</returns>
        private bool TryGetPlantBounds(out Bounds bounds)
        {
            bounds = default;
            if (_plantRenderers == null || _plantRenderers.Length == 0) return false;

            var hasAny = false;
            foreach (var r in _plantRenderers)
            {
                if (!r || !r.enabled) continue;
                if (!hasAny)
                {
                    bounds = r.bounds;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return hasAny;
        }
    }
}

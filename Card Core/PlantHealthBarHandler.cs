using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlantHealthBarHandler : MonoBehaviour
    {
        [SerializeField] private PlantController plantController;
        public GameObject heartPrefab;
        public GameObject eggPrefab;
        public GameObject infectBarParent;
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

        private readonly List<Transform> _eggIcons = new();

        private readonly List<Transform> _infectIcons = new();
        private int _lastEggCount = -1;
        private int _lastInfectCount = -1;
        private Renderer[] _plantRenderers;
        private Camera camera1;

        private void Start()
        {
            camera1 = Camera.main;
            plantController = GetComponentInParent<PlantController>();

            if (!plantController) return;

            // Cache plant renderers (tagged "Plant" like PlantController does)
            _plantRenderers = plantController
                .GetComponentsInChildren<Renderer>(true)
                .Where(r => r.CompareTag("Plant"))
                .ToArray();

            // Prime pools with any existing children in the bar parents (so designers can place a few by hand)
            PrimeExistingIcons(infectBarParent, _infectIcons);
            PrimeExistingIcons(eggBarParent, _eggIcons);

            SpawnHearts(plantController);
        }

        private void Update()
        {
            if (!plantController) return;
            var currentInf = plantController.GetInfectLevel();
            var currentEgg = plantController.GetEggLevel();

            if (currentInf == _lastInfectCount && currentEgg == _lastEggCount) return;
            // Sync UI to new values
            SyncBar(infectBarParent, heartPrefab, currentInf, ref _lastInfectCount, _infectIcons);
            SyncBar(eggBarParent, eggPrefab, currentEgg, ref _lastEggCount, _eggIcons);
        }

        private void LateUpdate()
        {
            if (!plantController) return;

            // Simple billboard behavior to face the main camera
            if (faceCamera && camera1)
            {
                var cam = camera1.transform;
                var rotTarget = billboardTransform ? billboardTransform : transform;
                // Avoid rotating the entire plant root by accident
                if (!plantController || rotTarget != plantController.transform)
                    rotTarget.rotation =
                        Quaternion.LookRotation((rotTarget.position - cam.position).normalized, Vector3.up);
            }

            // Anchor above renderer bounds if desired
            if (!anchorToPlantBounds) return;
            var posTarget = anchorTransform ? anchorTransform : transform;
            // Avoid moving the entire plant root by accident
            if (plantController && posTarget == plantController.transform) return;
            var hasBounds = TryGetPlantBounds(out var bounds);
            if (!hasBounds) return;
            var top = bounds.center + new Vector3(0, bounds.extents.y + anchorHeightOffset, 0);
            posTarget.position = top;
        }

        public void SpawnHearts(PlantController plant)
        {
            var infLevel = plant.GetInfectLevel();
            var eggLevel = plant.GetEggLevel();
            SyncBar(infectBarParent, heartPrefab, infLevel, ref _lastInfectCount, _infectIcons);
            SyncBar(eggBarParent, eggPrefab, eggLevel, ref _lastEggCount, _eggIcons);
        }

        private void SyncBar(GameObject barParent, GameObject prefab, int targetCount, ref int cache,
            List<Transform> pool)
        {
            if (!barParent || !prefab) return;
            var t = barParent.transform;
            if (usePooling)
            {
                // Ensure we have enough pooled items
                for (var i = pool.Count; i < targetCount; i++)
                {
                    var go = Instantiate(prefab, t, false);
                    var tr = go.transform;
                    pool.Add(tr);
                }

                // Toggle active based on targetCount
                for (var i = 0; i < pool.Count; i++)
                {
                    var shouldBeActive = i < targetCount;
                    var tr = pool[i];
                    if (tr && tr.gameObject.activeSelf != shouldBeActive)
                        tr.gameObject.SetActive(shouldBeActive);
                }

                // Always layout to avoid overlapping
                LayoutIcons(pool, targetCount, prefab);
            }
            else
            {
                // Fallback to the original instantiate/destroy path
                var current = t.childCount;
                if (current < targetCount)
                    for (var i = current; i < targetCount; i++)
                        Instantiate(prefab, t, false);
                else if (current > targetCount)
                    for (var i = current - 1; i >= targetCount; i--)
                    {
                        var child = t.GetChild(i);
                        if (child) Destroy(child.gameObject);
                    }

                // Layout children even when not pooling
                LayoutChildren(t, targetCount, prefab);
            }

            cache = targetCount;
        }

        private static void PrimeExistingIcons(GameObject parent, List<Transform> pool)
        {
            pool.Clear();
            if (!parent) return;
            var t = parent.transform;
            for (var i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child) pool.Add(child);
            }
        }

        private void LayoutIcons(List<Transform> pool, int targetCount, GameObject prefab)
        {
            // Gather active icons up to targetCount
            var actives = new List<Transform>(targetCount);
            for (var i = 0; i < pool.Count && actives.Count < targetCount; i++)
            {
                var tr = pool[i];
                if (tr && tr.gameObject.activeSelf) actives.Add(tr);
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
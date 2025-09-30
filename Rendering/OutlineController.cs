using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _project.Scripts.Rendering
{
    /// <summary>
    ///     Toggles the outline flag on every renderer that uses the outline material/shader.
    ///     Attach this to a controller object; it will cache matching renderers at the start and
    ///     flip the shader boolean on demand.
    /// </summary>
    public class OutlineController : MonoBehaviour
    {
        private static readonly int DefaultPropertyId = Shader.PropertyToID("_OutlineOn");

        [SerializeField] private Renderer[] explicitTargets;
        [SerializeField] private Material outlineMaterialReference;
        [SerializeField] private string propertyName = "_OutlineOn";
        [SerializeField] private bool outlineEnabled = true;
        [SerializeField] private bool affectSharedMaterial;
        [SerializeField] private bool searchEntireScene = true;
        [SerializeField] private bool includeInactive = true;

        private readonly List<RendererContext> _rendererContexts = new();
        private readonly HashSet<Renderer> _rendererLookup = new();
        private bool _initialized;

        private MaterialPropertyBlock _propertyBlock;
        private int _propertyId = DefaultPropertyId;

        private void Awake() => Initialize();

        private void OnEnable()
        {
            Initialize();
            Apply(outlineEnabled);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _initialized = false;
            Initialize();
            Apply(outlineEnabled);
        }
#endif

        public void SetOutline(bool isEnabled)
        {
            Initialize();
            outlineEnabled = isEnabled;
            Apply(isEnabled);
        }

        public void ToggleOutline() => SetOutline(!outlineEnabled);

        public void SetTargets(params Renderer[] renderers)
        {
            explicitTargets = renderers;
            _initialized = false;
            Initialize();
            Apply(outlineEnabled);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                RefreshStaleContexts();
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyId = string.IsNullOrWhiteSpace(propertyName)
                ? DefaultPropertyId
                : Shader.PropertyToID(propertyName);

            CacheRenderers();
            _initialized = true;
        }

        private void CacheRenderers()
        {
            _rendererContexts.Clear();
            _rendererLookup.Clear();

            if (explicitTargets is { Length: > 0 })
                foreach (var renderer1 in explicitTargets)
                    AddRenderer(renderer1);

            if (searchEntireScene)
            {
                var inactiveMode = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
                var renderers = FindObjectsByType<Renderer>(inactiveMode, FindObjectsSortMode.None);
                foreach (var renderer1 in renderers) AddRenderer(renderer1);
            }
            else if (_rendererContexts.Count == 0)
            {
                foreach (var renderer1 in GetComponentsInChildren<Renderer>(includeInactive))
                    AddRenderer(renderer1);
            }

            if (_rendererContexts.Count == 0 && TryGetComponent(out Renderer fallback)) AddRenderer(fallback);
        }

        private void AddRenderer(Renderer renderer1)
        {
            if (!renderer1 || !_rendererLookup.Add(renderer1)) return;

            var sharedMaterials = renderer1.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                _rendererLookup.Remove(renderer1);
                return;
            }

            List<int> matchingIndices = null;

            for (var i = 0; i < sharedMaterials.Length; i++)
            {
                var material = sharedMaterials[i];
                if (!material) continue;
                if (!material.HasProperty(_propertyId)) continue;

                if (outlineMaterialReference)
                {
                    var matchesReference = ReferenceEquals(material, outlineMaterialReference) ||
                                           material.shader == outlineMaterialReference.shader;
                    if (!matchesReference) continue;
                }

                matchingIndices ??= new List<int>();
                matchingIndices.Add(i);
            }

            if (matchingIndices == null || matchingIndices.Count == 0)
            {
                _rendererLookup.Remove(renderer1);
                return;
            }

            _rendererContexts.Add(new RendererContext(renderer1, matchingIndices.ToArray()));
        }

        private void Apply(bool isEnabled)
        {
            if (_rendererContexts.Count == 0) return;

            if (affectSharedMaterial)
                ApplySharedMaterial(isEnabled ? 1f : 0f);
            else
                ApplyPropertyBlock(isEnabled ? 1f : 0f);
        }

        private void ApplyPropertyBlock(float floatValue)
        {
            foreach (var context in _rendererContexts)
            {
                var contextRenderer = context.Renderer;
                if (!contextRenderer) continue;

                foreach (var materialIndex in context.MaterialIndices)
                {
                    if (materialIndex < 0 || materialIndex >= contextRenderer.sharedMaterials.Length) continue;

                    _propertyBlock.Clear();
                    contextRenderer.GetPropertyBlock(_propertyBlock, materialIndex);
                    _propertyBlock.SetFloat(_propertyId, floatValue);
                    contextRenderer.SetPropertyBlock(_propertyBlock, materialIndex);
                }
            }
        }

        private void ApplySharedMaterial(float floatValue)
        {
            foreach (var material in from context in _rendererContexts
                     let renderer = context.Renderer
                     where renderer
                     let sharedMaterials = renderer.sharedMaterials
                     where sharedMaterials != null && sharedMaterials.Length != 0
                     from index in context.MaterialIndices
                     where index >= 0 && index < sharedMaterials.Length
                     select sharedMaterials[index]
                     into material
                     where material && material.HasProperty(_propertyId)
                     select material) material.SetFloat(_propertyId, floatValue);
        }

        private void RefreshStaleContexts()
        {
            var dirty = false;
            for (var i = _rendererContexts.Count - 1; i >= 0; i--)
            {
                if (_rendererContexts[i].Renderer) continue;
                _rendererContexts.RemoveAt(i);
                dirty = true;
            }

            if (!dirty) return;
            _rendererLookup.Clear();
            foreach (var context in _rendererContexts.Where(context => context.Renderer))
                _rendererLookup.Add(context.Renderer);
        }

        private readonly struct RendererContext
        {
            public RendererContext(Renderer renderer, int[] materialIndices)
            {
                Renderer = renderer;
                MaterialIndices = materialIndices;
            }

            public Renderer Renderer { get; }
            public int[] MaterialIndices { get; }
        }
    }
}
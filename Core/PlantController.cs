using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using JetBrains.Annotations;
using TMPro;
using Unity.Serialization;
using UnityEngine;
using Random = System.Random;

namespace _project.Scripts.Core
{
    [Flags]
    public enum PlantType
    {
        // ReSharper disable once UnusedMember.Global
        NotYetSelected = 0,
        Coleus = 1 << 0,
        Pepper = 1 << 1,
        Cucumber = 1 << 2,
        Chrysanthemum = 1 << 3
    }

    public class PlantController : MonoBehaviour
    {
        private readonly int _moldIntensityID = Shader.PropertyToID("_Mold_Intensity");
        private readonly int _color1 = Shader.PropertyToID("_Color");
    
        // ReSharper disable twice NotAccessedField.Local
        [SerializeField] private List<string> cAfflictions = new();
        [SerializeField] private List<string> cTreatments = new();
        [SerializeField] private List<string> uAfflictions = new();
        [SerializeField] private List<string> uTreatments = new();
        
        [SerializeField] private Shader litShader;
        [SerializeField] private Shader moldShader;
        [SerializeField] [Range(0, 1)] private float moldIntensity;

        [SerializeField] private ParticleSystem debuffSystem;
        [SerializeField] private ParticleSystem buffSystem;
        [SerializeField] private ParticleSystem thripsFX;

        [DontSerialize] public PlantCardFunctions plantCardFunctions;

        [CanBeNull] public GameObject priceFlag;
        [CanBeNull] public TextMeshPro priceFlagText;
        public PlantType type;
        public ICard PlantCard;
    
        private bool _needsShaderUpdate;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _sharedPropertyBlock;

        public List<PlantAfflictions.IAffliction> CurrentAfflictions { get; } = new();
        public List<PlantAfflictions.ITreatment> CurrentTreatments { get; } = new();
        private List<PlantAfflictions.IAffliction> PriorAfflictions { get; } = new();
        public List<PlantAfflictions.ITreatment> UsedTreatments { get; } = new();

        private void Start()
        {
            if (!TryGetComponent(out plantCardFunctions)) { }
        }

        private void Awake()
        {
            _renderers = Array.FindAll(GetComponentsInChildren<Renderer>
                (true), r => r.CompareTag("Plant"));
            _sharedPropertyBlock = new MaterialPropertyBlock();

            // ReSharper disable Twice ShaderLabShaderReferenceNotResolved
            //if (!moldShader) moldShader = Shader.Find("Shader Graphs/Mold");
            var mildewAfflictionInstance = new PlantAfflictions.MildewAffliction();
            if (!moldShader) moldShader = mildewAfflictionInstance.Shader;
            if (!litShader) litShader = Shader.Find("Shader Graphs/CustomLit");

            UpdateShaders();
        }

        private void Update()
        {
            cAfflictions = CurrentAfflictions.Select(a => a.Name).ToList();
            cTreatments = CurrentTreatments.Select(a => a.Name).ToList();
            uAfflictions = PriorAfflictions.Select(a => a.Name).ToList();
            uTreatments = UsedTreatments.Select(a => a.Name).ToList();
            if (!_needsShaderUpdate) return;
            UpdateShaders();
            _needsShaderUpdate = false;
        }

        private Color GetAfflictionColor()
        {
            return CurrentAfflictions.Count != 0 ? CurrentAfflictions[0].Color : Color.white;
        }

        public void FlagShadersUpdate()
        {
            _needsShaderUpdate = true;
        }

        public void SetMoldIntensity(float value)
        {
            if (Mathf.Approximately(moldIntensity, value)) return;
            moldIntensity = value;
            _needsShaderUpdate = true;
        }

        private void UpdateShaders()
        {
            var hasMildew = moldIntensity > 0;
            List<Material> mats = new();
            foreach (var renderer1 in _renderers)
            {
                if (!renderer1.CompareTag("Plant")) continue;

                mats.Clear();
                renderer1.GetMaterials(mats);
                foreach (var material in mats)
                {
                   // var targetShader = hasMildew ? moldShader : GetShader(renderer1);
                    var targetShader = GetShader(renderer1);
                    if (material.shader != targetShader)
                        material.shader = targetShader;
                }

                _sharedPropertyBlock.SetFloat(_moldIntensityID, moldIntensity);
                // Sets color to debug colors
               // _sharedPropertyBlock.SetColor(_color1, GetAfflictionColor());
                renderer1.SetPropertyBlock(_sharedPropertyBlock);
            }
        }

        private Shader GetShader(Renderer renderer1)
        {
            var afflictions = renderer1.GetComponentInParent<PlantController>().CurrentAfflictions;
            
            return afflictions.Any() ? CurrentAfflictions.FirstOrDefault(a => a.Shader)?.Shader : litShader;
        }

        public void RemoveAffliction(PlantAfflictions.IAffliction affliction)
        {
            if (!CurrentAfflictions.Remove(affliction)) return;
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity(0);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Stop();
                    break;
            }

            if(buffSystem) buffSystem.Play();
        }

        public void AddAffliction(PlantAfflictions.IAffliction affliction)
        {
            PriorAfflictions.Add(affliction);
            var rand = new Random();
            var randomValue = rand.NextDouble() * 0.5f + 0.5f;
            CurrentAfflictions.Add(affliction);
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity((float)randomValue);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Play();
                    break;
            }

            if (debuffSystem) debuffSystem.Play();
        }

        public bool HasHadAffliction(PlantAfflictions.IAffliction affliction)
        {
            return PriorAfflictions.Any(existing => existing.GetType() == affliction.GetType());
        }
    
        public void ProcessDay()
        {
            foreach (var treatment in CurrentTreatments)
            {
                treatment.ApplyTreatment(this);
            }
        
            CurrentAfflictions.ForEach(a => a.TickDay());
            _needsShaderUpdate = true;
        }

        // Check if the affliction is already present
        public bool HasAffliction(PlantAfflictions.IAffliction affliction)
        {
            return CurrentAfflictions.Any(existing => existing.GetType() == affliction.GetType());
        }
    }
}
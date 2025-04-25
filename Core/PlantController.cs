using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using Unity.Serialization;
using UnityEngine;

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
        private static readonly int MoldIntensityID = Shader.PropertyToID("_Mold_Intensity");
        private static readonly int Color1 = Shader.PropertyToID("_Color");
    
        // ReSharper disable twice NotAccessedField.Local
        [SerializeField] private List<string> cAfflictions = new();
        [SerializeField] private List<string> cTreatments = new();
        [SerializeField] private List<string> uTreatments = new();
        
        [SerializeField] private Shader litShader;
        [SerializeField] private Shader moldShader;
        [SerializeField] [Range(0, 1)] private float moldIntensity;

        [SerializeField] private ParticleSystem debuffSystem;
        [SerializeField] private ParticleSystem buffSystem;

        [DontSerialize] public PlantCardFunctions plantCardFunctions;

        public PlantType type;
        public ICard PlantCard;
    
        private bool _needsShaderUpdate;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _sharedPropertyBlock;

        public List<PlantAfflictions.IAffliction> CurrentAfflictions { get; } = new();
        public List<PlantAfflictions.ITreatment> CurrentTreatments { get; } = new();
        public List<PlantAfflictions.ITreatment> UsedTreatments { get; } = new();

        private void Start()
        {
            if (!TryGetComponent(out plantCardFunctions)) { }
        }

        private void Awake()
        {
            _renderers = Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r.CompareTag("Plant"));
            _sharedPropertyBlock = new MaterialPropertyBlock();

            // ReSharper disable Twice ShaderLabShaderReferenceNotResolved
            if (!moldShader) moldShader = Shader.Find("Shader Graphs/Mold");
            if (!litShader) litShader = Shader.Find("Shader Graphs/CustomLit");

            UpdateShaders();
        }

        private void Update()
        {
            cAfflictions = CurrentAfflictions.Select(a => a.Name).ToList();
            cTreatments = CurrentTreatments.Select(a => a.Name).ToList();
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
                    var targetShader = hasMildew ? moldShader : litShader;
                    if (material.shader != targetShader)
                        material.shader = targetShader;
                }

                _sharedPropertyBlock.SetFloat(MoldIntensityID, moldIntensity);
                _sharedPropertyBlock.SetColor(Color1, GetAfflictionColor());
                renderer1.SetPropertyBlock(_sharedPropertyBlock);
            }
        }

        public void RemoveAffliction(PlantAfflictions.IAffliction affliction)
        {
            if (!CurrentAfflictions.Remove(affliction)) return;
            if (affliction is PlantAfflictions.MildewAffliction)
                SetMoldIntensity(0);

            if(buffSystem) buffSystem.Play();
        }

        public void AddAffliction(PlantAfflictions.IAffliction affliction)
        {
            CurrentAfflictions.Add(affliction);
            if(debuffSystem) debuffSystem.Play();
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
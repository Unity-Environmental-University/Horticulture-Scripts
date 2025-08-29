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
    /// <summary>
    /// Enumeration of supported plant types in the game.
    /// Uses flags to allow for potential combination types in the future.
    /// </summary>
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

    /// <summary>
    /// Controls individual plant behavior including health, afflictions, treatments, and visual effects.
    /// This component manages the lifecycle of a plant from placement to death, handling all
    /// interactions with the card game system, visual feedback, and state persistence.
    /// </summary>
    /// <remarks>
    /// PlantController is the core component for plant management in the game. It integrates with:
    /// - Card system for plant cards and their properties
    /// - Affliction system for a pest/disease management
    /// - Treatment system for player interventions
    /// - Visual system for shaders, particles, and UI feedback
    /// - Save/load system for persistent plant state
    /// </remarks>
    public class PlantController : MonoBehaviour
    {
        private readonly int _moldIntensityID = Shader.PropertyToID("_Mold_Intensity");
        
        [SerializeField] private Shader litShader;
        [SerializeField] private Shader moldShader;
        [Range(0, 1)] public float moldIntensity;

        [SerializeField] private ParticleSystem buffSystem;
        [SerializeField] private ParticleSystem thripsFX;
        
        [SerializeField] public List<string> cAfflictions = new();
        [SerializeField] public List<string> cTreatments = new();
        [SerializeField] public List<string> pAfflictions = new();
        [SerializeField] public List<string> uTreatments = new();
        [SerializeField] public ParticleSystem debuffSystem;
        [SerializeField] public ParticleSystem deathFX;
        
        [DontSerialize] public PlantCardFunctions plantCardFunctions;

        [CanBeNull] public GameObject priceFlag;
        [CanBeNull] public TextMeshPro priceFlagText;
        [CanBeNull] public AudioSource audioSource;
        public PlantType type;
        // ReSharper disable once InconsistentNaming
        public ICard PlantCard;
        public GameObject prefab;

        private bool _needsShaderUpdate;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _sharedPropertyBlock;
        private bool _afflictionsChanged = true;

        public List<PlantAfflictions.IAffliction> CurrentAfflictions { get; } = new();
        public List<PlantAfflictions.ITreatment> CurrentTreatments { get; } = new();
        public List<PlantAfflictions.IAffliction> PriorAfflictions { get; } = new();
        public List<PlantAfflictions.ITreatment> UsedTreatments { get; } = new();
        
        /// <summary>
        /// Gets or sets the total egg level on this plant from all affliction sources.
        /// </summary>
        public int EggLevel
        {
            get => GetEggLevel();
            set => SetEggLevel(value);
        }

        private void Start()
        {
            if (!TryGetComponent(out plantCardFunctions)) { }
            
            if (!audioSource && TryGetComponent(out AudioSource foundSource))
                audioSource = foundSource;
            
            if (priceFlagText && PlantCard != null) priceFlagText.text = "$" + PlantCard.Value;
        }

        private void Awake()
        {
            _renderers = Array.FindAll(GetComponentsInChildren<Renderer>
                (true), r => r.CompareTag("Plant"));
            _sharedPropertyBlock = new MaterialPropertyBlock();

            // Initialize shaders for mold/disease visual effects
            var mildewAfflictionInstance = new PlantAfflictions.MildewAffliction();
            if (!moldShader) moldShader = mildewAfflictionInstance.Shader;
            // ReSharper disable once ShaderLabShaderReferenceNotResolved
            if (!litShader) litShader = Shader.Find("Shader Graphs/CustomLit");

            UpdateShaders();
        }

        private void Update()
        {
            if (_afflictionsChanged)
            {
                cAfflictions = CurrentAfflictions.Select(a => a.Name).ToList();
                cTreatments = CurrentTreatments.Select(a => a.Name).ToList();
                pAfflictions = PriorAfflictions.Select(a => a.Name).ToList();
                uTreatments = UsedTreatments.Select(a => a.Name).ToList();
                _afflictionsChanged = false;
            }
            
            if (PlantCard is { Value: <= 0 }) KillPlant();

            if (!_needsShaderUpdate) return;
            UpdateShaders();
            _needsShaderUpdate = false;
        }

        /// <summary>
        /// Marks the plant's shaders as needing an update on the next frame.
        /// </summary>
        public void FlagShadersUpdate() => _needsShaderUpdate = true;

        public void UpdatePriceFlag(int newValue)
        {
            if (priceFlagText) priceFlagText.text = "$" + newValue;
            CardGameMaster.Instance.scoreManager.CalculatePotentialProfit();
        }

        /// <summary>
        /// Sets the mold intensity for visual shader effects (0 = no mold, 1 = full coverage).
        /// </summary>
        public void SetMoldIntensity(float value)
        {
            if (Mathf.Approximately(moldIntensity, value)) return;
            moldIntensity = value;
            _needsShaderUpdate = true;
        }

        private void UpdateShaders()
        {
            List<Material> mats = new();
            foreach (var renderer1 in _renderers)
            {
                if (!renderer1.CompareTag("Plant")) continue;

                mats.Clear();
                renderer1.GetMaterials(mats);
                foreach (var material in mats)
                {
                    var targetShader = GetShader(renderer1);
                    if (material.shader != targetShader)
                        material.shader = targetShader;
                }

                _sharedPropertyBlock.SetFloat(_moldIntensityID, moldIntensity);
                renderer1.SetPropertyBlock(_sharedPropertyBlock);
            }
        }

        private Shader GetShader(Renderer renderer1)
        {
            var afflictions = renderer1.GetComponentInParent<PlantController>().CurrentAfflictions;

            return afflictions.Any() ? CurrentAfflictions.FirstOrDefault(a => a.Shader)?.Shader : litShader;
        }

        /// <summary>
        /// Removes a specific affliction from the plant and triggers healing effects.
        /// </summary>
        /// <param name="affliction">The affliction to remove</param>
        public void RemoveAffliction(PlantAfflictions.IAffliction affliction)
        {
            if (!CurrentAfflictions.Remove(affliction)) return;
            _afflictionsChanged = true;
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity(0);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Stop();
                    break;
            }

            // Note: Infect/egg reduction is now handled by treatments, not by removal
            
            
            TurnController.QueuePlantEffect(
                this,
                particle: buffSystem,
                sound: CardGameMaster.Instance.soundSystem.plantHeal,
                delay: 0.3f
                );
        }

        /// <summary>
        /// Adds an affliction to the plant, applying its effects and updating visuals.
        /// </summary>
        /// <param name="affliction">The affliction to add to the plant</param>
        public void AddAffliction(PlantAfflictions.IAffliction affliction)
        {
            PriorAfflictions.Add(affliction);
            var rand = new Random();
            var randomValue = rand.NextDouble() * 0.5f + 0.5f;
            CurrentAfflictions.Add(affliction);
            _afflictionsChanged = true;
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity((float)randomValue);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Play();
                    break;
            }

            var iCard = affliction.GetCard();
            var healthBarHandler = GetComponent<PlantHealthBarHandler>();
            
            if (iCard is IAfflictionCard afflictionInterface)
            {
                AddInfect(affliction, afflictionInterface.BaseInfectLevel);
                if(healthBarHandler) healthBarHandler.SpawnHearts(this);
                if (afflictionInterface.BaseEggLevel > 0)
                    AddEggs(affliction, afflictionInterface.BaseEggLevel);
            }

            Debug.LogWarning(name + " has " + CurrentAfflictions.Count + " afflictions. Current infect level is " +
                      GetInfectLevel());
            
            
            
            if (debuffSystem) 
                TurnController.QueuePlantEffect(
                    plant: this,
                    particle: debuffSystem,
                    sound: CardGameMaster.Instance.soundSystem.GetInsectSound(affliction),
                    delay: 0.3f);
        }

        /// <summary>
        /// Gets the total infection level across all affliction sources.
        /// </summary>
        public int GetInfectLevel()
        {
            if (PlantCard is IPlantCard plantCardInterface)
                return plantCardInterface.Infect.InfectTotal;
            return 0;
        }

        public void SetInfectLevel(int infectLevel)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            plantCardInterface.Infect.SetInfect("Manual", Mathf.Max(0, infectLevel));
            FlagShadersUpdate();
        }

        private void AddInfect(PlantAfflictions.IAffliction affliction, int amount)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            plantCardInterface.Infect.AddInfect(source, Mathf.Max(0, amount));
            FlagShadersUpdate();
        }

        private void ReduceInfect(PlantAfflictions.IAffliction affliction, int amount)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            plantCardInterface.Infect.ReduceInfect(source, Mathf.Max(0, amount));
            FlagShadersUpdate();
        }
        
        private void ReduceEggs(PlantAfflictions.IAffliction affliction, int amount)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            plantCardInterface.Infect.ReduceEggs(source, Mathf.Max(0, amount));
            FlagShadersUpdate();
        }

        private void AddEggs(PlantAfflictions.IAffliction affliction, int amount)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            plantCardInterface.Infect.AddEggs(source, Mathf.Max(0, amount));
            FlagShadersUpdate();
        }
        
        public int GetInfectFrom(PlantAfflictions.IAffliction affliction)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return 0;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            return plantCardInterface.Infect.GetInfect(source);
        }

        public int GetEggsFrom(PlantAfflictions.IAffliction affliction)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return 0;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            return plantCardInterface.Infect.GetEggs(source);
        }

        public int GetEggLevel()
        {
            if (PlantCard is IPlantCard plantCardInterface)
                return plantCardInterface.EggLevel;
            return 0;
        }

        public void SetEggLevel(int eggLevel)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            plantCardInterface.EggLevel = Mathf.Max(0, eggLevel);
            FlagShadersUpdate();
        }
        
        /// <summary>
        /// Reduces infect and egg values for a specific affliction source by the specified amounts.
        /// If both values reach zero, the affliction is automatically removed.
        /// </summary>
        /// <param name="affliction">The affliction to reduce values for</param>
        /// <param name="infectReduction">Amount to reduce infection by (negative values are clamped to 0)</param>
        /// <param name="eggReduction">Amount to reduce eggs by (negative values are clamped to 0)</param>
        public void ReduceAfflictionValues(PlantAfflictions.IAffliction affliction, int infectReduction, int eggReduction)
        {
            if (affliction == null)
            {
                Debug.LogWarning("Cannot reduce values for null affliction");
                return;
            }
            
            if (PlantCard is not IPlantCard plantCardInterface) return;
            
            // Validate reduction amounts (clamp to non-negative)
            infectReduction = Mathf.Max(0, infectReduction);
            eggReduction = Mathf.Max(0, eggReduction);
            
            var source = affliction.Name ?? affliction.GetType().Name;
            
            // Reduce infect and egg values
            if (infectReduction > 0)
                plantCardInterface.Infect.ReduceInfect(source, infectReduction);
                
            if (eggReduction > 0)
                plantCardInterface.Infect.ReduceEggs(source, eggReduction);
            
            FlagShadersUpdate();
            
            // Check if affliction should be removed (both infect and eggs are zero)
            var remainingInfect = plantCardInterface.Infect.GetInfect(source);
            var remainingEggs = plantCardInterface.Infect.GetEggs(source);
            
            if (remainingInfect <= 0 && remainingEggs <= 0)
            {
                RemoveAffliction(affliction);
            }
        }

        /// <summary>
        /// Checks if the plant has ever been affected by a specific type of affliction (for immunity mechanics).
        /// </summary>
        public bool HasHadAffliction(PlantAfflictions.IAffliction affliction)
        {
            return PriorAfflictions.Any(existing => existing.GetType() == affliction.GetType());
        }

        /// <summary>
        /// Processes all daily activities for the plant (treatments and affliction progression).
        /// Called once per game turn.
        /// </summary>
        public void ProcessDay()
        {
            foreach (var treatment in CurrentTreatments) treatment.ApplyTreatment(this);

            CurrentAfflictions.ForEach(a => a.TickDay(this));
            _needsShaderUpdate = true;
        }

        public bool HasAffliction(PlantAfflictions.IAffliction affliction)
        {
            return CurrentAfflictions.Any(existing => existing.GetType() == affliction.GetType());
        }

        private void KillPlant()
        {
            StartCoroutine(CardGameMaster.Instance.deckManager.ClearPlant(this));
        }
    }
}

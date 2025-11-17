using System;
using System.Collections;
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
    /// - the Card system for plant cards and their properties
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
        [SerializeField] private ParticleSystem gnatsFX;

        [Header("Animation System")]
        [Tooltip("Animator for affliction animations. Trigger names follow pattern: {PlantCard.Name.ToLower()}{AnimationTriggerName}\n\nExample: For Chrysanthemum with Droop affliction → 'chrysanthemumDroop' trigger")]
        [SerializeField] private Animator plantAnimator;

        [SerializeField] public List<string> cAfflictions = new();
        [SerializeField] public List<string> cTreatments = new();
        [SerializeField] public List<string> pAfflictions = new();
        [SerializeField] public List<string> uTreatments = new();
        [SerializeField] public ParticleSystem debuffSystem;
        [SerializeField] public ParticleSystem buffFX;
        [SerializeField] public ParticleSystem deathFX;
        
        [DontSerialize] public PlantCardFunctions plantCardFunctions;
        [DontSerialize] public bool canSpreadAfflictions = true;
        [DontSerialize] public bool canReceiveAfflictions = true;

        private bool _isDying;
        [CanBeNull] public GameObject priceFlag;
        [CanBeNull] public TextMeshPro priceFlagText;
        [CanBeNull] public AudioSource audioSource;
        public PlantType type;
        // ReSharper disable once InconsistentNaming
        public ICard PlantCard;
        public GameObject prefab;

        private bool _needsShaderUpdate;
        private Renderer[] _renderers;
        private readonly List<Material> _cachedMaterialList = new();
        private MaterialPropertyBlock _sharedPropertyBlock;

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

            // Initialize debug lists for Unity Inspector
            UpdateDebugLists();
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

            // Auto-discover plant animator if not assigned
            if (!plantAnimator)
                plantAnimator = GetComponentInChildren<Animator>();

            UpdateShaders();
        }

        private void Update()
        {
            if (!_isDying && PlantCard is { Value: <= 0 })
            {
                _isDying = true;

                // Immediately disable cardholders to prevent placement during death sequence
                // Note: Card cleanup is handled by DeckManager.ClearPlant() after death animation
                var location = transform.parent;
                if (location)
                {
                    var cardHolders = location.GetComponentsInChildren<PlacedCardHolder>();
                    foreach (var holder in cardHolders)
                    {
                        if (!holder) continue;

                        // Disable the cardholder immediately to prevent new card placements
                        holder.ToggleCardHolder(false);
                    }
                }

                StartCoroutine(KillPlant());
            }

            if (!_needsShaderUpdate) return;
            UpdateShaders();
            _needsShaderUpdate = false;
        }

        /// <summary>
        /// Updates the serialized string lists for Unity Inspector debugging.
        /// Called only when afflictions or treatments actually change, not every frame.
        /// </summary>
        private void UpdateDebugLists()
        {
            cAfflictions = CurrentAfflictions.Select(a => a.Name).ToList();
            cTreatments = CurrentTreatments.Select(a => a.Name).ToList();
            pAfflictions = PriorAfflictions.Select(a => a.Name).ToList();
            uTreatments = UsedTreatments.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Marks the plant's shaders as needing an update on the next frame.
        /// </summary>
        public void FlagShadersUpdate() => _needsShaderUpdate = true;

        public void UpdatePriceFlag(int newValue)
        {
            if (priceFlagText) priceFlagText.text = "$" + newValue;
            if (CardGameMaster.Instance)
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
            foreach (var renderer1 in _renderers)
            {
                if (!renderer1.CompareTag("Plant")) continue;

                var targetShader = GetShader(renderer1);
                _cachedMaterialList.Clear();
                renderer1.GetMaterials(_cachedMaterialList);
                if (!targetShader) continue;
                foreach (var material in _cachedMaterialList.Where(material => material.shader != targetShader))
                    material.shader = targetShader;

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
            UpdateDebugLists();
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity(0);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Stop();
                    break;
                case PlantAfflictions.FungusGnatsAffliction:
                    if (gnatsFX) gnatsFX.Stop();
                    break;
            }

            // Note: Infect/egg reduction is now handled by treatments, not by removal


            if (CardGameMaster.Instance)
            {
                TurnController.QueuePlantEffect(
                    this,
                    particle: buffSystem,
                    sound: CardGameMaster.Instance.soundSystem.plantHeal,
                    delay: 0.3f
                    );
            }

            // Trigger recovery animation if specified
            if (!plantAnimator || PlantCard == null ||
                string.IsNullOrEmpty(affliction.RecoveryAnimationTriggerName)) return;
            var prefix = PlantCard.Name.ToLower();
            var triggerName = $"{prefix}{affliction.RecoveryAnimationTriggerName}";
            if (HasAnimatorParameter(triggerName)) plantAnimator.SetTrigger(triggerName);
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
            UpdateDebugLists();
            switch (affliction)
            {
                case PlantAfflictions.MildewAffliction:
                    SetMoldIntensity((float)randomValue);
                    break;
                case PlantAfflictions.ThripsAffliction:
                    if (thripsFX) thripsFX.Play();
                    break;
                case PlantAfflictions.FungusGnatsAffliction:
                    if (gnatsFX) gnatsFX.Play();
                    break;
            }

            // Only add affliction values if PlantCard is present
            if (PlantCard != null)
            {
                var iCard = affliction.GetCard();
                var healthBarHandler = GetComponent<PlantHealthBarHandler>();

                if (iCard is IAfflictionCard afflictionInterface)
                {
                    AddInfect(affliction, afflictionInterface.BaseInfectLevel);
                    if (afflictionInterface.BaseEggLevel > 0)
                        AddEggs(affliction, afflictionInterface.BaseEggLevel);

                    // Update health bar UI after both infect and eggs are added
                    if (healthBarHandler) healthBarHandler.SpawnHearts(this);
                }

                var cardGameMaster = CardGameMaster.Instance;
                if (cardGameMaster && cardGameMaster.debuggingCardClass)
                {
                    Debug.Log(name + " has " + CurrentAfflictions.Count + " afflictions. Current infect level is " +
                              GetInfectLevel());
                }
            }
            else
            {
                Debug.LogWarning($"PlantController.AddAffliction: PlantCard is null on '{gameObject.name}'. " +
                                "Cannot add affliction values. Affliction visual effects will still apply.", this);
            }

            if (debuffSystem && CardGameMaster.Instance && CardGameMaster.Instance.soundSystem)
            {
                TurnController.QueuePlantEffect(
                    this,
                    debuffSystem,
                    CardGameMaster.Instance.soundSystem.GetInsectSound(affliction));
            }

            // Trigger affliction animation if specified
            if (plantAnimator && PlantCard != null && !string.IsNullOrEmpty(affliction.AnimationTriggerName))
            {
                var prefix = PlantCard.Name.ToLower();
                var triggerName = $"{prefix}{affliction.AnimationTriggerName}";

                if (HasAnimatorParameter(triggerName))
                {
                    plantAnimator.SetTrigger(triggerName);

                    var cgm = CardGameMaster.Instance;
                    if (cgm && cgm.debuggingCardClass)
                    {
                        Debug.Log($"[PlantController] Triggered animation '{triggerName}' for {affliction.Name} on {name}", this);
                    }
                }
                else
                {
                    var cgm = CardGameMaster.Instance;
                    if (cgm && cgm.debuggingCardClass)
                    {
                        Debug.LogWarning($"[PlantController] Animator parameter '{triggerName}' not found in animator controller on {name}", this);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(affliction.AnimationTriggerName))
            {
                var cgm = CardGameMaster.Instance;
                if (!cgm || !cgm.debuggingCardClass) return;
                // Only log if animation was specified but prerequisites missing
                if (!plantAnimator)
                    Debug.LogWarning($"[PlantController] Animator component missing for '{affliction.Name}' animation on {name}", this);
                else if (PlantCard == null)
                    Debug.LogWarning($"[PlantController] PlantCard reference missing for animation trigger on {name}", this);
            }
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

        public void AddInfect(PlantAfflictions.IAffliction affliction, int amount)
        {
            if (PlantCard is not IPlantCard plantCardInterface) return;
            var source = affliction?.Name ?? affliction?.GetType().Name ?? "Unknown";
            plantCardInterface.Infect.AddInfect(source, Mathf.Max(0, amount));
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

        private void SetEggLevel(int eggLevel)
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
                Debug.LogWarning("PlantController: Cannot reduce values for null affliction", this);
                return;
            }

            if (CurrentAfflictions == null || !CurrentAfflictions.Contains(affliction))
            {
                Debug.LogWarning(
                    $"PlantController: Affliction '{affliction.Name}' not present on plant '{gameObject.name}'", this);
                return;
            }

            if (PlantCard is not IPlantCard plantCardInterface)
            {
                Debug.LogWarning("PlantController: PlantCard does not implement IPlantCard interface", this);
                return;
            }

            infectReduction = Mathf.Max(0, infectReduction);
            eggReduction = Mathf.Max(0, eggReduction);

            if (infectReduction == 0 && eggReduction == 0) return;

            var source = affliction.Name ?? affliction.GetType().Name;

            if (infectReduction > 0)
                plantCardInterface.Infect.ReduceInfect(source, infectReduction);

            if (eggReduction > 0)
                plantCardInterface.Infect.ReduceEggs(source, eggReduction);

            FlagShadersUpdate();

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

        /// <summary>
        /// Checks if the plant's animator has a specific trigger parameter.
        /// Prevents errors when animation clips haven't been added yet.
        /// </summary>
        /// <param name="paramName">The trigger parameter name to check</param>
        /// <returns>True if the parameter exists, false otherwise</returns>
        private bool HasAnimatorParameter(string paramName)
        {
            return plantAnimator && plantAnimator.parameters.Any(param =>
                param.name == paramName && param.type == AnimatorControllerParameterType.Trigger);
        }

        /// <summary>
        ///     Gets the length of an animation clip by trigger name.
        ///     Attempts to find the animation clip in the animator controller and returns its duration.
        ///     Searches for clips that match the trigger name (case-insensitive) or contain it.
        /// </summary>
        /// <param name="triggerName">The trigger name to search for</param>
        /// <returns>Animation clip length in seconds, or 2.0f as fallback if not found</returns>
        private float GetAnimationClipLength(string triggerName)
        {
            if (!plantAnimator || !plantAnimator.runtimeAnimatorController)
            {
                var cgm = CardGameMaster.Instance;
                if (cgm && cgm.debuggingCardClass)
                    Debug.LogWarning($"[PlantController] No animator or controller found for animation '{triggerName}' on {name}. Using 2.0s fallback.", this);
                return 2.0f;
            }

            var clips = plantAnimator.runtimeAnimatorController.animationClips;

            // First, try exact match (case-insensitive)
            var matchingClip = clips.FirstOrDefault(clip =>
                clip.name.Equals(triggerName, StringComparison.OrdinalIgnoreCase));

            // If no exact match, try to find clip that contains the trigger name
            if (!matchingClip)
            {
                matchingClip = clips.FirstOrDefault(clip =>
                    clip.name.IndexOf(triggerName, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (matchingClip)
            {
                var cgm = CardGameMaster.Instance;
                if (cgm && cgm.debuggingCardClass)
                    Debug.Log($"[PlantController] Found animation clip '{matchingClip.name}' for trigger '{triggerName}' with duration {matchingClip.length:F2}s on {name}", this);
                return matchingClip.length;
            }

            var cgmFallback = CardGameMaster.Instance;
            if (cgmFallback && cgmFallback.debuggingCardClass)
                Debug.LogWarning($"[PlantController] No animation clip found for trigger '{triggerName}' on {name}. Using 2.0s fallback. Available clips: {string.Join(", ", clips.Select(c => c.name))}", this);

            return 2.0f;
        }

        /// <summary>
        ///     Handles plant death by triggering death animation, playing death sound, and clearing the plant.
        ///     Follows the same pattern as affliction animations with [plant name]Death trigger.
        /// </summary>
        public IEnumerator KillPlant(bool requestDeckCleanup = true)
        {
            if (plantAnimator && PlantCard != null)
            {
                var prefix = PlantCard.Name.ToLower();
                var triggerName = $"{prefix}Death";

                if (HasAnimatorParameter(triggerName))
                {
                    plantAnimator.SetTrigger(triggerName);

                    var cgm = CardGameMaster.Instance;
                    if (cgm && cgm.debuggingCardClass)
                        Debug.Log($"[PlantController] Triggered death animation '{triggerName}' on {name}", this);

                    // Play death sound
                    if (cgm && cgm.soundSystem)
                    {
                        var deathSound = cgm.soundSystem.plantDeath;
                        if (deathSound && audioSource)
                        {
                            // Reset audio properties to ensure consistent volume (matches TurnController.PlayQueuedPlantEffects behavior)
                            audioSource.pitch = 1f;
                            audioSource.volume = 1f;
                            audioSource.spatialBlend = 0f;
                            audioSource.PlayOneShot(deathSound);
                        }
                    }

                    var animationDuration = GetAnimationClipLength(triggerName);
                    yield return new WaitForSeconds(animationDuration);
                }
                else if (CardGameMaster.Instance && CardGameMaster.Instance.debuggingCardClass)
                {
                    Debug.LogWarning($"[PlantController] Death animation trigger '{triggerName}' not found on {name}",
                        this);
                }
            }

            if (!requestDeckCleanup) yield break;

            var cardGameMaster = CardGameMaster.Instance;
            if (cardGameMaster && cardGameMaster.deckManager)
                cardGameMaster.deckManager.StartCoroutine(
                    cardGameMaster.deckManager.ClearPlant(this, true));
        }
    }
}

# Horticulture Audio System Documentation

## Overview

The Horticulture Unity game features a comprehensive audio system designed to provide immersive soundscapes, responsive audio feedback, and spatial audio effects. The system is organized around several core components that handle different aspects of audio playback, from game event sounds to environmental audio mixing.

See also: `CLASSES_SYSTEM_DOCUMENTATION.md` for `PlantEffectRequest` and related domain types referenced by audio.

## Architecture

### Core Components

The audio system consists of five main components located in the `_project.Scripts.Audio` namespace:

1. **`SoundSystemMaster`** - Central audio clip repository and sound management
2. **`AudioSyncer`** - Synchronized multi-source audio playback
3. **`AudioVolumeProx`** - Distance-based volume control
4. **`AudioSwitchCollider`** - Trigger-based audio mixer control
5. **`IndoorAudioCollider`** - Smooth indoor/outdoor audio transitions

### Integration Points

- **CardGameMaster**: References `SoundSystemMaster` for centralized audio access
- **TurnController**: Manages queued plant effect audio through `PlantEffectRequest`
- **DeckManager**: Plays card and plant audio using `AudioSource.PlayClipAtPoint`
- **PlantController**: Individual plant audio sources for localized effects

## Component Details

### SoundSystemMaster

**Location**: `/Assets/_project/Scripts/Audio/SoundSystemMaster.cs`

The `SoundSystemMaster` serves as the central repository for all audio clips in the game and provides convenient access methods for retrieving sounds.

#### Public Properties

```csharp
[Header("Plant Sounds")]
public AudioClip plantSpawn;      // Plant spawning/placement sound
public AudioClip plantHeal;       // Plant healing/treatment sound  
public AudioClip plantSell;       // Plant selling sound
public AudioClip plantDeath;      // Plant death/destruction sound

[Header("Card Sounds")]
public AudioClip selectCard;      // Card selection sound
public AudioClip drawCard;        // Card drawing sound
public AudioClip placeCard;       // Card placement sound
public AudioClip unplaceCard;     // Card removal sound
public AudioClip shuffleCard;     // Deck shuffling sound

[Header("Affliction Sounds")]
public AudioClip thripsAfflicted;    // Thrips infestation sound
public AudioClip aphidsAfflicted;    // Aphids infestation sound
public AudioClip mealyBugsAfflicted; // Mealy bugs infestation sound
public AudioClip mildewAfflicted;    // Mildew affliction sound

[Header("Narration Clips")]
public AudioClip florabotNarrationAphids; // Character narration for aphids
```

#### Methods

```csharp
public AudioClip GetInsectSound(PlantAfflictions.IAffliction affliction)
```

Returns the appropriate insect sound based on the affliction type. Uses pattern matching to map afflictions to their corresponding audio clips.

**Parameters:**
- `affliction` - The plant affliction interface implementation

**Returns:** 
- `AudioClip` - The matching sound clip, or `null` if no match found

**Usage Example:**
```csharp
var sound = soundSystemMaster.GetInsectSound(new PlantAfflictions.AphidsAffliction());
audioSource.PlayOneShot(sound);
```

### AudioSyncer

**Location**: `/Assets/_project/Scripts/Audio/AudioSyncer.cs`

Synchronizes playback of multiple AudioSources to start at precisely the same time using Unity's DSP time scheduling.

#### Public Properties

```csharp
public List<AudioSource> audioSources; // List of audio sources to synchronize
public float syncDelay = 0.1f;          // Delay before synchronized playback starts
```

#### Key Features

- **Automatic validation**: Ensures all AudioSources have the same clip
- **DSP-time scheduling**: Uses `AudioSettings.dspTime` for precise synchronization
- **Error logging**: Warns if AudioSources have mismatched clips

#### Usage Example

```csharp
// Setup multiple audio sources with the same clip
AudioSyncer syncer = gameObject.AddComponent<AudioSyncer>();
syncer.audioSources.AddRange(audioSourceArray);
syncer.syncDelay = 0.05f;
// Synchronization happens automatically on Start()
```

### AudioVolumeProx

**Location**: `/Assets/_project/Scripts/Audio/AudioVolumeProx.cs`

Provides distance-based volume control for spatial audio effects, automatically adjusting AudioSource volume based on proximity to the player.

#### Public Properties

```csharp
public AudioSource audioSource;  // The audio source to control
public GameObject player;        // Reference to player GameObject
public float maxDistance = 100f; // Maximum effective distance
```

#### Volume Calculation

The volume is calculated using a linear falloff formula:

```csharp
volume = max(0, 1 - (distance / maxDistance))
```

- At distance 0: volume = 1.0
- At maxDistance: volume = 0.0
- Beyond maxDistance: volume = 0.0

#### Usage Example

```csharp
// Attach to a GameObject with environmental audio
AudioVolumeProx proximityControl = gameObject.AddComponent<AudioVolumeProx>();
proximityControl.audioSource = GetComponent<AudioSource>();
proximityControl.player = GameObject.FindGameObjectWithTag("Player");
proximityControl.maxDistance = 50f;
```

### AudioSwitchCollider

**Location**: `/Assets/_project/Scripts/Audio/AudioSwitchCollider.cs`

Provides trigger-based instant audio mixer parameter changes when the player enters/exits specific areas.

#### Public Properties

```csharp
[SerializeField] private AudioMixer mixer; // Reference to the audio mixer
```

#### Behavior

- **OnTriggerEnter**: Sets "IndoorVolume" parameter to 0 dB
- **OnTriggerExit**: Sets "IndoorVolume" parameter to -20 dB

#### Setup Requirements

1. GameObject must have a Collider with `IsTrigger = true`
2. Player GameObject must be tagged with "Player"
3. AudioMixer must have an exposed parameter named "IndoorVolume"

#### Usage Example

```csharp
// Setup on a doorway or transition area
AudioSwitchCollider switchCollider = doorwayObject.AddComponent<AudioSwitchCollider>();
switchCollider.mixer = mainAudioMixer;
```

### IndoorAudioCollider

**Location**: `/Assets/_project/Scripts/Handlers/IndoorAudioCollider.cs`

Provides smooth, coroutine-based transitions between indoor and outdoor audio settings.

#### Public Properties

```csharp
[SerializeField] private GameObject player;           // Player reference
[SerializeField] private AudioMixerGroup outdoorSounds; // Outdoor audio mixer group
public float smoothTime = 0.5f;                      // Transition duration
public float indoorVolume = -5f;                      // Indoor volume level
```

#### Features

- **Smooth transitions**: Uses coroutines and `Mathf.Lerp` for gradual volume changes
- **Interruption handling**: Stops previous transitions when new ones start
- **Volume transition speed**: Configurable via constructor parameter

#### Usage Example

```csharp
// Setup indoor/outdoor transition
IndoorAudioCollider indoorCollider = transitionArea.AddComponent<IndoorAudioCollider>();
indoorCollider.player = playerGameObject;
indoorCollider.outdoorSounds = outdoorMixerGroup;
indoorCollider.smoothTime = 0.3f;
indoorCollider.indoorVolume = -8f;
```

## Audio Effect System

### PlantEffectRequest

**Location**: `/Assets/_project/Scripts/Classes/PlantEffectClasses.cs`

Data structure for queuing plant-related audio and particle effects.

```csharp
public class PlantEffectRequest
{
    public readonly PlantController Plant;
    public readonly ParticleSystem Particle;
    public readonly AudioClip Sound;
    public readonly float Delay;
}
```

### Effect Processing

The `TurnController` processes queued plant effects in the `PlayQueuedPlantEffects()` coroutine:

```csharp
private IEnumerator PlayQueuedPlantEffects()
{
    while (PlantEffectQueue.Count > 0)
    {
        var request = PlantEffectQueue.Dequeue();
        if (request.Plant && request.Sound && request.Plant.audioSource)
        {
            request.Plant.audioSource.pitch = 1f;
            request.Plant.audioSource.volume = 1f;
            request.Plant.audioSource.spatialBlend = 0f;
            request.Plant.audioSource.PlayOneShot(request.Sound);
        }
        yield return new WaitForSeconds(request.Delay);
    }
}
```

## Integration Patterns

### CardGameMaster Integration

The `CardGameMaster` serves as the central hub for audio system access:

```csharp
public class CardGameMaster : MonoBehaviour
{
    public SoundSystemMaster soundSystem;
    public AudioSource playerHandAudioSource;
    public AudioSource robotAudioSource;
    
    // Access pattern used throughout the codebase:
    CardGameMaster.Instance.soundSystem.drawCard
    CardGameMaster.Instance.playerHandAudioSource.PlayOneShot(clip)
}
```

### Common Usage Patterns

#### 1. Immediate Audio Playback

```csharp
// For immediate 3D positioned audio
var clip = CardGameMaster.Instance.soundSystem.plantSpawn;
if (clip) AudioSource.PlayClipAtPoint(clip, worldPosition);
```

#### 2. AudioSource-based Playback

```csharp
// For controlled playback through existing AudioSource
var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
playerAudio.PlayOneShot(CardGameMaster.Instance.soundSystem.drawCard);
```

#### 3. Queued Plant Effects

```csharp
// For synchronized audio/particle effects on plants
TurnController.QueuePlantEffect(
    this,
    particle: healingParticles,
    sound: CardGameMaster.Instance.soundSystem.plantHeal,
    delay: 0.3f
);
```

## Music System

### MusicBounce

**Location**: `/Assets/_project/Scripts/Handlers/MusicBounce.cs`

Provides visual responsiveness to music by analyzing spectrum data and scaling objects accordingly.

#### Public Properties

```csharp
public float scaleMultiplier = 1.0f; // Intensity of scaling effect
public float smoothTime = 0.2f;      // Smoothing for scale transitions
public float maxScale = 2.0f;        // Maximum scale multiplier
public float minScale = 1.0f;        // Minimum scale multiplier
```

#### Spectrum Analysis

Uses Unity's `GetSpectrumData()` with FFT analysis:

```csharp
audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);
// Analyzes first 10 frequency bands for performance
```

## Configuration Guide

### Audio Mixer Setup

1. **Create AudioMixer Asset**
   - Right-click in Project → Create → Audio → Audio Mixer
   - Name it appropriately (e.g., "MainAudioMixer")

2. **Expose Parameters**
   - Select mixer in Inspector
   - Right-click parameters → Expose to Script
   - Use consistent names: "IndoorVolume", "Volume", etc.

3. **Assign Mixer Groups**
   - Create separate groups for different audio categories
   - Assign AudioSources to appropriate groups

### Component Setup Checklist

#### SoundSystemMaster
- [ ] Assign all AudioClip references in Inspector
- [ ] Verify clips are imported with correct settings
- [ ] Test `GetInsectSound()` method with all affliction types

#### AudioVolumeProx
- [ ] Assign AudioSource reference
- [ ] Set player GameObject reference
- [ ] Tune maxDistance for desired falloff
- [ ] Test volume curves at various distances

#### AudioSwitchCollider
- [ ] Set up Collider as trigger
- [ ] Assign AudioMixer reference
- [ ] Verify "IndoorVolume" parameter exists and is exposed
- [ ] Test trigger boundaries

#### IndoorAudioCollider
- [ ] Assign player and AudioMixerGroup references
- [ ] Tune smoothTime for desired transition feel
- [ ] Set appropriate indoorVolume level
- [ ] Test transition smoothness

### Performance Considerations

1. **AudioClip Import Settings**
   - Use compressed formats for longer clips
   - Use uncompressed for short, frequently-played sounds
   - Consider platform-specific compression

2. **3D Audio Optimization**
   - Limit the number of simultaneous 3D audio sources
   - Use audio LOD for distant sounds
   - Consider audio occlusion for complex environments

3. **Memory Management**
   - Load audio clips as needed rather than keeping all in memory
   - Use audio streaming for background music
   - Implement audio pooling for frequently-used effects

## Troubleshooting

### Common Issues

#### No Sound Playing
- Verify AudioSource volume is > 0
- Check AudioMixer group volume levels
- Ensure AudioClip is assigned and imported correctly
- Verify camera has AudioListener component

#### Synchronization Issues
- Check that all AudioSources in AudioSyncer have same clip
- Verify syncDelay is appropriate for audio buffer size
- Ensure audio sources aren't already playing when sync starts

#### Volume Proximity Not Working
- Verify player reference is assigned and active
- Check that maxDistance is appropriate for scene scale
- Ensure AudioSource isn't set to 2D audio

#### Mixer Transitions Not Working
- Verify exposed parameter names match code expectations
- Check that AudioMixer is assigned to collider scripts
- Ensure colliders have correct trigger settings

### Debug Tools

#### Enable Audio Debug Logging
```csharp
// Add to development builds for audio debugging
Debug.Log($"Playing audio clip: {clip.name} at position: {transform.position}");
```

#### Audio Profiler Usage
- Use Unity Profiler → Audio tab
- Monitor AudioSource count and memory usage
- Check for audio dropout or performance spikes

## Best Practices

### Audio Architecture
- Centralize audio clip references in SoundSystemMaster
- Use consistent naming conventions for clips and parameters
- Implement audio pools for frequently-used sounds

### Performance
- Limit concurrent AudioSources (typically < 32)
- Use appropriate audio compression settings
- Implement distance-based audio culling

### User Experience
- Provide master volume controls
- Implement audio accessibility options
- Use consistent audio feedback for similar actions

### Code Organization
- Keep audio logic close to gameplay logic
- Use events for decoupled audio triggering
- Document audio parameter ranges and expectations

## API Reference

### SoundSystemMaster
```csharp
public AudioClip GetInsectSound(PlantAfflictions.IAffliction affliction)
```

### TurnController
```csharp
public static void QueuePlantEffect(PlantController plant, 
                                  ParticleSystem particle = null, 
                                  AudioClip sound = null,
                                  float delay = 0.3f)
```

### AudioVolumeProx
```csharp
private static float CalculateVolume(float distance, float max)
```

### IndoorAudioCollider
```csharp
public IndoorAudioCollider(float volumeTransitionSpeed)
private IEnumerator SmoothTransition()
```

This documentation covers the complete audio system architecture and provides practical guidance for developers working with audio in the Horticulture project.

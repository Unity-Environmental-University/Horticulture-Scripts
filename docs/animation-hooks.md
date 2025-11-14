# Plant Affliction Animation Hooks

**Last Updated:** 2025-11-14
**Unity Version:** 6000.1.11f1+
**Introduced:** v1.0 (Animation Hook System)

## Overview

The animation hook system allows plant afflictions to trigger plant-specific animations when applied or removed. This provides visual feedback to players beyond the existing particle effects and shader systems, enabling skeletal animations like drooping, wilting, shaking, and recovery.

## Architecture

### Key Components

1. **IAffliction Interface** (`PlantAfflictions.cs`)
   - `AnimationTriggerName` - Optional trigger for affliction application
   - `RecoveryAnimationTriggerName` - Optional trigger for affliction removal

2. **PlantController** (`PlantController.cs`)
   - `plantAnimator` - Reference to the plant's Animator component
   - `HasAnimatorParameter()` - Safe parameter checking
   - Animation triggering in `AddAffliction()` and `RemoveAffliction()`
   - Uses `PlantCard.Name.ToLower()` to determine animation prefix automatically

### How It Works

When an affliction is added to a plant:

1. PlantController checks if the affliction has an `AnimationTriggerName`
2. If present, it gets the plant's name from `PlantCard.Name` and converts to lowercase
3. Example: `"Droop"` + `PlantCard.Name.ToLower()` = `"Droop"` + `"chrysanthemum"` → `"chrysanthemumDroop"`
4. Checks if the animator has this trigger parameter
5. If found, triggers the animation

The same process occurs in reverse for recovery animations.

### Benefits of PlantCard.Name Approach

This design offers several advantages:

- **Automatic**: Animation prefix is derived directly from the plant card's Name property
- **Zero Configuration**: No manual setup needed in Inspector or code
- **Consistent**: Plant card Name is already the source of truth for plant identity
- **Mod-Friendly**: Modders just set their PlantCard's Name property
- **Simple**: No hardcoded mapping, no serialized fields to maintain
- **Maintainable**: Adding new plant types requires zero code or configuration changes

## Adding Animation Clips to Plants

### Step 1: Animator Setup

Each plant type has an existing animator controller:

- **Mums**: `/Assets/_project/Models/Mums/mumsRig.controller`
- **Coleus**: `/Assets/_project/Models/coleus/coleusRIG.controller`
- **Pepper**: `/Assets/_project/Models/peppers/peppersRIG.controller`
- **Cucumber**: `/Assets/_project/Models/cucumbers/cucumberRig.controller`

### Step 3: Add Trigger Parameters

Open the animator controller in Unity and add trigger parameters for each affliction animation:

**For Dehydration (Droop) Animation:**
- `chrysanthemumDroop` (Trigger)
- `coleusDroop` (Trigger)
- `pepperDroop` (Trigger)
- `cucumberDroop` (Trigger)

**For Light Deficiency (Wilt) Animation:**
- `chrysanthemumWilt` (Trigger)
- `coleusWilt` (Trigger)
- `pepperWilt` (Trigger)
- `cucumberWilt` (Trigger)

**For Recovery Animation:**
- `chrysanthemumRecover` (Trigger)
- `coleusRecover` (Trigger)
- `pepperRecover` (Trigger)
- `cucumberRecover` (Trigger)

### Step 4: Create Animation Clips

1. Create animation clips for each state (e.g., `ChrysanthemumDroop.anim`)
2. Animate the plant's skeleton to show the visual effect
3. For droop: bend stems, lower leaves
4. For wilt: curl/shrivel leaves, reduce overall height
5. For recovery: reverse the affliction animation, return to healthy state

### Step 5: Create Animator States and Transitions

In the animator controller:

1. Create states for each animation (e.g., "Idle", "Drooping", "Wilting", "Recovering")
2. Add transitions from "Idle" → "Drooping" with `chrysanthemumDroop` trigger
3. Add transitions from "Drooping" → "Idle" with `chrysanthemumRecover` trigger
4. Ensure transitions have appropriate durations and blend settings

### Step 6: Assign Animator to Plant Prefab

1. Open the plant prefab (e.g., `Chrysanthemum.prefab`)
2. Ensure the plant GameObject has an Animator component
3. Assign the animator controller to the Animator component
4. The PlantController will automatically find it via `GetComponentInChildren<Animator>()`

## Naming Convention

Animation triggers follow this pattern:

```
{plantName}{AnimationTriggerName}
```

Where:
- `plantName` is automatically derived from `PlantCard.Name.ToLower()`
- `AnimationTriggerName` comes from the IAffliction interface properties

### Standard Animation Trigger Examples

These are the trigger names for built-in plant types:

| Plant Type      | PlantCard.Name    | Example Trigger          |
|-----------------|-------------------|--------------------------|
| Chrysanthemum   | `chrysanthemum`   | `chrysanthemumDroop`     |
| Coleus          | `coleus`          | `coleusWilt`             |
| Pepper          | `pepper`          | `pepperRecover`          |
| Cucumber        | `cucumber`        | `cucumberShake`          |

**For Modders**: The system automatically uses your PlantCard's Name property! Just ensure your animator trigger names match the pattern `{yourPlantName.ToLower()}{AnimationTriggerName}`.

### Affliction Animation Names

| Affliction      | AnimationTriggerName | RecoveryAnimationTriggerName |
|-----------------|---------------------|------------------------------|
| Dehydrated      | `Droop`             | `Recover`                    |
| NeedsLight      | `Wilt`              | `Recover`                    |
| Thrips          | `null`              | `null`                       |
| Mildew          | `null`              | `null`                       |
| Aphids          | `null`              | `null`                       |
| SpiderMites     | `null`              | `null`                       |
| FungusGnats     | `null`              | `null`                       |
| MealyBugs       | `null`              | `null`                       |

### Death Animation Names

Death animations use a similar pattern to affliction animations:

| Event Type | Trigger Pattern | Example |
|------------|----------------|---------|
| Plant Death | `{plantName}Death` | `chrysanthemumDeath` |

Death animations are triggered when plant value reaches 0 or below.

### Example Trigger Names

- `chrysanthemumDroop` - Chrysanthemum drooping from dehydration
- `coleusWilt` - Coleus wilting from light deficiency
- `pepperRecover` - Pepper recovering from any affliction
- `cucumberRecover` - Cucumber returning to healthy state
- `chrysanthemumDeath` - Chrysanthemum death animation
- `coleusDeath` - Coleus death animation
- `pepperDeath` - Pepper death animation
- `cucumberDeath` - Cucumber death animation

## Creating Custom Affliction Animations

To add animations to other afflictions (e.g., pest infestations):

### 1. Update Affliction Class

```csharp
public class ThripsAffliction : IAffliction
{
    // ... existing properties ...

    public string AnimationTriggerName => "Shake";  // Add this
    public string RecoveryAnimationTriggerName => "Recover";  // Add this
}
```

### 2. Add Animator Parameters

Add triggers to all plant animator controllers:
- `chrysanthemumShake`
- `coleusShake`
- `pepperShake`
- `cucumberShake`

### 3. Create Animation Clips

Create shaking animations for each plant type that show the plant trembling or vibrating (simulating pest activity).

### 4. Test In-Game

1. Apply the affliction to a plant
2. Verify the animation triggers correctly
3. Cure the affliction
4. Verify the recovery animation plays

## Graceful Degradation

The system is designed to work gracefully even when animation clips aren't ready:

- **No Animator Component**: System silently skips animation triggering
- **Missing Trigger Parameter**: `HasAnimatorParameter()` returns false, no error thrown
- **Null Animation Name**: System checks `string.IsNullOrEmpty()` before processing
- **No Plant Type**: Returns empty string, skips animation

This allows the code to be deployed before all animation assets are created.

## Animation Behavior

### When Applied (AddAffliction)

Animations triggered by `AddAffliction()`:
- Play **once** when affliction is added
- Transition to afflicted state
- Hold in that state until cured or plant dies

### When Removed (RemoveAffliction)

Animations triggered by `RemoveAffliction()`:
- Play **once** when affliction is removed
- Transition back to healthy/idle state
- Return plant to normal appearance

### On Plant Death (KillPlant)

Death animations triggered by `KillPlant()`:
- Play **once** when plant value reaches 0 or below
- Wait for animation to complete before clearing plant
- Play death sound effect alongside animation
- Uses `GetAnimationClipLength()` to determine animation duration
- Falls back to 2.0 seconds if animation clip not found
- Gracefully handles missing animator or animation clips
- Protected by `_isDying` flag to prevent multiple death sequences

### Integration with Existing Systems

Animation hooks work alongside:
- **Particle Effects**: Debuff/buff particles still play
- **Shader Effects**: Mold intensity, pest shaders still apply
- **Sound Effects**: Insect sounds, healing sounds still trigger
- **Health Bars**: UI feedback still appears

Animations are **additive**, not replacements for existing visual feedback.

## Performance Considerations

- `HasAnimatorParameter()` uses a foreach loop - called only when afflictions change, not every frame
- Animator trigger parameters are lightweight
- No string allocations during steady state (only when afflictions added/removed)
- Animation prefix is a serialized field, stored once per plant instance

## Testing

Unit tests are located in `PlayModeTest/AnimationHookTests.cs`:

- `DehydratedAffliction_HasCorrectAnimationTriggerNames`
- `NeedsLightAffliction_HasCorrectAnimationTriggerNames`
- `PlantController_HasAnimatorParameter_*` - Parameter checking logic
- `PlantController_AddAffliction_*` - Graceful handling tests
- `AllAfflictionTypes_HaveAnimationTriggerProperties` - Interface compliance
- `DeathAnimation_TriggerNames_FollowNamingConvention` - Death animation naming validation
- `PlantController_GetAnimationClipLength_*` - Animation duration detection tests
- `PlantController_KillPlant_*` - Death animation graceful handling tests

**Note**: Tests that validated `GetPlantTypeName()` have been removed as the system now uses serialized fields instead of enum mapping.

## Future Enhancements

Potential improvements to the system:

1. **Intensity-Based Animations**: Scale animation speed/intensity with infection level
2. **Looping Animations**: Continuous affliction animations (e.g., constant shaking)
3. **Blend Trees**: Smooth transitions between severity levels
4. **Animation Events**: Callbacks for sound effects or particle spawning mid-animation
5. **Stacking Animations**: Multiple afflictions triggering layered animations

## Troubleshooting

### Animation Not Playing

1. **Check PlantCard.Name**: Verify the PlantCard has a Name property set correctly
2. **Check Animator Component**: Ensure plant prefab has Animator component assigned
3. **Verify Trigger Parameter**: Open animator controller, check trigger exists
4. **Confirm Naming**: Trigger name must match exactly (case-sensitive)
5. **Verify Naming Pattern**: Ensure trigger follows `{plantName.ToLower()}{AnimationTriggerName}` pattern
6. **Enable Debug Logging**: Add Debug.Log in AddAffliction to trace execution

### Wrong Animation Playing

1. **Verify PlantCard.Name**: Check the PlantCard.Name property value matches your animator triggers
2. **Check Animator Controller**: Ensure correct controller assigned to plant
3. **Review Transitions**: Verify animator transitions use correct triggers

### Animation Plays But Looks Wrong

1. **Check Animation Clip**: Open clip, verify keyframes are correct
2. **Review Blend Settings**: Adjust transition duration and blend mode
3. **Test Skeleton Rig**: Ensure plant rig bones are named consistently

## References

- **IAffliction Interface**: `Assets/_project/Scripts/Classes/PlantAfflictions.cs:35-66`
- **PlantController**: `Assets/_project/Scripts/Core/PlantController.cs`
- **AddAffliction Method**: `PlantController.cs` - Animation triggering logic
- **RemoveAffliction Method**: `PlantController.cs` - Recovery animation logic
- **PlantCard.Name**: Property on ICard implementations that provides the plant name
- **HasAnimatorParameter Method**: `PlantController.cs` - Safe parameter checking
- **Unit Tests**: `Assets/_project/Scripts/PlayModeTest/AnimationHookTests.cs`

## Example Workflow

### Complete Animation Setup Example (Chrysanthemum Droop)

1. **Verify PlantCard Name**:
   - Open `ChrysanthemumCard.cs`
   - Confirm `Name => "Chrysanthemum";` is set correctly
   - The system will automatically use `"chrysanthemum"` (lowercase) as prefix

2. **Open Animator Controller**: `Assets/_project/Models/Mums/mumsRig.controller`

3. **Add Trigger Parameters**:
   - Right-click in Parameters panel
   - Add Parameter → Trigger
   - Name: `chrysanthemumDroop`
   - Repeat for `chrysanthemumRecover`

4. **Create Animation Clip**:
   - Right-click in Project → Create → Animation
   - Name: `ChrysanthemumDroop.anim`
   - Animate chrysanthemum skeleton to droop over 1 second

5. **Add Animator States**:
   - Right-click in animator graph → Create State → From New Blend Tree (or Empty)
   - Name: "Drooping"
   - Assign `ChrysanthemumDroop.anim` as motion

6. **Create Transitions**:
   - Right-click "Idle" → Make Transition → "Drooping"
   - Add condition: `chrysanthemumDroop` trigger
   - Right-click "Drooping" → Make Transition → "Idle"
   - Add condition: `chrysanthemumRecover` trigger

7. **Test**:
   - Play game
   - Add dehydration to chrysanthemum plant
   - Watch droop animation play
   - Cure dehydration
   - Watch recovery animation play

### Example for Modders (Custom Plant)

If you're creating a custom plant called "CustomRose":

1. **In your PlantCard implementation**:
   - Set `public string Name => "CustomRose";`
   - The system will automatically use `"customrose"` (lowercase) as prefix

2. **In your animator controller**:
   - Create triggers like: `customroseDroop`, `customroseWilt`, `customroseRecover`

3. **The system will automatically**:
   - Derive the prefix from your PlantCard.Name
   - Trigger the appropriate animations
   - No code changes needed!

---

**Questions or Issues?**
Check the troubleshooting section above or review the unit tests for reference implementations.

# Animation Timeout Watchdog System

**Last Updated:** 2024-12-17
**Location:** `Assets/_project/Scripts/Card Core/DeckManager.cs`, `TurnController.cs`

## Overview

The Animation Timeout Watchdog System prevents permanent UI deadlocks caused by failed DOTween animation callbacks. When animation sequences fail to complete (due to exceptions, scene changes, or Unity lifecycle issues), the system automatically detects and recovers from stuck animation states.

## Problem Statement

### The Issue

DOTween animation sequences use callbacks to signal completion:
```csharp
sequence.OnComplete(() => {
    UpdatingActionDisplay = false; // Reset flag when animation finishes
});
```

**Failure Scenarios:**
1. Exception thrown in animation setup/callback
2. GameObject destroyed before sequence completes
3. Scene changes interrupting animations
4. DOTween internal errors

**Result:** `UpdatingActionDisplay` flag stays `true` forever, blocking all future card operations.

### User Impact

- Players cannot draw cards
- UI becomes unresponsive
- Turn progression blocked
- Game becomes unplayable without restart

## Architecture

### Components

1. **Animation Flag**: `DeckManager.UpdatingActionDisplay`
   - Prevents concurrent animation operations
   - Guards against race conditions
   - Blocks UI interactions during animations

2. **Watchdog Coroutine**: `DeckManager.AnimationTimeoutWatchdog(float maxDuration)`
   - Monitors animation flag
   - Triggers force-reset after timeout
   - Logs diagnostic information

3. **Force Reset**: `DeckManager.ForceResetAnimationFlag()`
   - Kills running sequences
   - Clears animation flag
   - Re-enables disabled components

4. **External Trigger**: `TurnController.ForceResetDeckAnimationFlag()`
   - Allows external systems to trigger recovery
   - Provides escape hatch for edge cases

## Implementation Details

### Animation Flag Property

```csharp
private bool _updatingActionDisplay;

public bool UpdatingActionDisplay
{
    get => _updatingActionDisplay;
    private set
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_updatingActionDisplay != value && debug)
        {
            Debug.Log($"[DeckManager] updatingActionDisplay: {_updatingActionDisplay} -> {value}");

            // Log stack trace for suspicious transitions
            if (value && _updatingActionDisplay)
            {
                Debug.LogWarning($"[DeckManager] Suspicious flag transition!\n{Environment.StackTrace}");
            }
        }
        #endif
        _updatingActionDisplay = value;
    }
}
```

**Features:**
- Diagnostic logging in development builds
- Stack trace capture for suspicious transitions (setting true when already true)
- Private setter prevents external interference

### Watchdog Coroutine

```csharp
private IEnumerator AnimationTimeoutWatchdog(float maxDuration)
{
    yield return new WaitForSeconds(maxDuration);

    if (UpdatingActionDisplay)
    {
        Debug.LogError("[DeckManager] Animation watchdog timeout detected! Force-clearing flag.");
        ForceResetAnimationFlag();
    }
}
```

**Timeout Values:**
- Hand reflow animations: 2 seconds timeout (typical duration ~0.5s)
- Display sequences: 5 seconds timeout (typical duration ~2s with stagger)
- Safety margin: 2-3x expected duration

**Started From:**
- `AnimateHandReflow()` - Started immediately when animation sequence begins
- `DisplayActionCardsSequence()` - Started before sequence creation

### Force Reset Implementation

```csharp
public void ForceResetAnimationFlag()
{
    Debug.LogWarning("[DeckManager] ForceResetAnimationFlag called - clearing stuck animation state.");

    // Kill all running sequences
    SafeKillSequence(ref _currentHandSequence);
    SafeKillSequence(ref _currentDisplaySequence);

    // Force-clear the animation flag (bypass property setter to avoid logging)
    _updatingActionDisplay = false;

    // Re-enable all Click3D components that may have been disabled
    if (actionCardParent)
    {
        foreach (Transform child in actionCardParent)
        {
            var click3D = child.GetComponent<Click3D>();
            if (click3D) click3D.enabled = true;
        }
    }
}
```

**Recovery Steps:**
1. Kill running DOTween sequences (prevents memory leaks)
2. Force-clear animation flag (bypass setter to avoid recursive logging)
3. Re-enable disabled Click3D components (restore interactivity)

### Safe Sequence Cleanup

```csharp
private static void SafeKillSequence(ref Sequence sequence)
{
    if (sequence == null) return;

    try
    {
        sequence.Kill(true); // Kill and complete callbacks
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error killing DOTween sequence: {ex.Message}");
    }
    finally
    {
        sequence = null; // Always null reference to prevent reuse
    }
}
```

**Error Handling:**
- Try-catch prevents exceptions from propagating
- Finally block ensures sequence reference is cleared
- Prevents memory leaks from orphaned sequences

## Usage Patterns

### Starting Animations with Watchdog

```csharp
private void AnimateHandReflow(float duration)
{
    // Kill existing sequences
    SafeKillSequence(ref _currentHandSequence);

    // Set flag BEFORE starting watchdog
    UpdatingActionDisplay = true;

    // Start watchdog coroutine
    StartCoroutine(AnimationTimeoutWatchdog(duration + 2f));

    try
    {
        // Create and configure sequence
        _currentHandSequence = DOTween.Sequence();
        // ... animation setup ...

        _currentHandSequence.OnComplete(() =>
        {
            try
            {
                // Re-enable components, update state
            }
            finally
            {
                // ALWAYS reset flag, even on error
                UpdatingActionDisplay = false;
                _currentHandSequence = null;
            }
        });

        _currentHandSequence.Play();
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error creating animation: {ex.Message}");
        SafeKillSequence(ref _currentHandSequence);
        UpdatingActionDisplay = false; // Reset on error
    }
}
```

### Checking Animation State Before Operations

```csharp
public void DrawActionHand()
{
    // Guard clause: Don't start if animation in progress
    if (UpdatingActionDisplay) return;

    // ... draw logic ...
}

public void RedrawCards()
{
    if (UpdatingActionDisplay)
    {
        AnalyticsFunctions.RecordRedraw(..., false, "Animation in progress");
        return;
    }

    // ... redraw logic ...
}
```

### External Force Reset Trigger

```csharp
// In TurnController
public void ForceResetDeckAnimationFlag()
{
    if (debug) Debug.Log("[TurnController] Force-resetting DeckManager animation flag");
    var dm = CardGameMaster.Instance?.deckManager;
    if (dm) dm.ForceResetAnimationFlag();
}
```

**Use Cases:**
- Manual recovery via debug commands
- Scene transition cleanup
- Emergency reset in edge cases

## Diagnostic Logging

### Normal Operation

```
[DeckManager] updatingActionDisplay: false -> true
[DeckManager] updatingActionDisplay: true -> false
```

### Suspicious Transition

```
[DeckManager] updatingActionDisplay: true -> true
[DeckManager] Suspicious flag transition!
<Stack Trace>
```
**Indicates:** Code is setting flag to true when already true (possible logic error)

### Timeout Detection

```
[DeckManager] Animation watchdog timeout detected! Force-clearing flag.
[DeckManager] ForceResetAnimationFlag called - clearing stuck animation state.
```
**Indicates:** Animation did not complete within timeout, auto-recovery triggered

## Performance Considerations

### Overhead

- **Watchdog Coroutine**: Minimal overhead (single WaitForSeconds per animation)
- **Diagnostic Logging**: Only in UNITY_EDITOR and DEVELOPMENT_BUILD
- **Force Reset**: Only runs on timeout/error (should be rare)

### Memory Management

- Sequences properly killed to prevent memory leaks
- References nulled after cleanup
- No accumulation of orphaned animations

## Testing

### Manual Testing

1. **Normal Operation**:
   - Draw cards multiple times
   - Verify animations complete smoothly
   - Check Console for clean state transitions

2. **Timeout Simulation**:
   - Temporarily increase timeout duration
   - Manually destroy card objects during animation
   - Verify watchdog triggers and recovers

3. **Concurrent Operation Prevention**:
   - Rapidly click draw/redraw buttons
   - Verify operations queue or reject appropriately
   - Ensure no race conditions

### Integration Tests

```csharp
[UnityTest]
public IEnumerator TestAnimationTimeout()
{
    // Setup: Force animation flag to stuck state
    deckManager.UpdatingActionDisplay = true;

    // Wait for timeout duration
    yield return new WaitForSeconds(6f);

    // Verify: Watchdog should have reset flag
    Assert.IsFalse(deckManager.UpdatingActionDisplay);
}
```

## Common Pitfalls

### ❌ Forgetting to Start Watchdog

```csharp
// BAD: No watchdog protection
UpdatingActionDisplay = true;
_currentSequence = DOTween.Sequence();
_currentSequence.OnComplete(() => UpdatingActionDisplay = false);
_currentSequence.Play();
```

**Problem:** If OnComplete never fires, flag stuck forever.

### ✅ Always Start Watchdog

```csharp
// GOOD: Watchdog provides safety net
UpdatingActionDisplay = true;
StartCoroutine(AnimationTimeoutWatchdog(5f));
_currentSequence = DOTween.Sequence();
_currentSequence.OnComplete(() => UpdatingActionDisplay = false);
_currentSequence.Play();
```

### ❌ Not Cleaning Up on Exception

```csharp
// BAD: Flag stays true on exception
UpdatingActionDisplay = true;
CreateComplexAnimation(); // Might throw exception
UpdatingActionDisplay = false; // Never reached if exception thrown
```

### ✅ Use Try-Finally

```csharp
// GOOD: Always cleanup, even on exception
UpdatingActionDisplay = true;
try
{
    CreateComplexAnimation();
}
finally
{
    UpdatingActionDisplay = false;
}
```

### ❌ Setting Flag Without Watchdog

```csharp
// BAD: Manual flag management without safety net
public void CustomAnimation()
{
    UpdatingActionDisplay = true;
    // Custom animation logic
    UpdatingActionDisplay = false; // Might never execute
}
```

### ✅ Always Use Watchdog

```csharp
// GOOD: Watchdog protects all animation operations
public void CustomAnimation()
{
    UpdatingActionDisplay = true;
    StartCoroutine(AnimationTimeoutWatchdog(2f));
    // Animation logic with OnComplete cleanup
}
```

## Troubleshooting

### Flag Stuck After Normal Operation

**Symptoms:**
- UI unresponsive
- Cannot draw cards
- No timeout message in Console

**Diagnosis:**
1. Check Console for last state transition log
2. Look for exception during animation setup
3. Verify OnComplete callback is reachable

**Solution:**
1. Call `ForceResetAnimationFlag()` via debug command
2. Identify root cause of stuck flag
3. Add try-finally protection if missing

### Timeout Triggering Too Early

**Symptoms:**
- Frequent watchdog timeout messages
- Animations appear to complete normally

**Diagnosis:**
1. Measure actual animation duration
2. Check timeout value vs actual duration
3. Verify hardware performance (slow devices)

**Solution:**
1. Increase timeout duration
2. Optimize animation complexity
3. Consider adaptive timeout based on device performance

### Timeout Never Triggers

**Symptoms:**
- Flag stuck but no timeout message
- Watchdog appears inactive

**Diagnosis:**
1. Verify watchdog coroutine was started
2. Check if MonoBehaviour was destroyed
3. Confirm coroutine wasn't stopped externally

**Solution:**
1. Add logging to watchdog start
2. Ensure MonoBehaviour lifecycle
3. Don't stop coroutines externally

## Design Rationale

### Why Watchdog Pattern?

**Alternatives Considered:**

1. **No Protection**: Let animations fail and require manual restart
   - ❌ Poor user experience
   - ❌ Unprofessional

2. **Timeout in OnComplete**: Set timeout that resets flag if callback fires
   - ❌ Doesn't help if callback never fires
   - ❌ Adds complexity to every callback

3. **Unity's StartCoroutine with Timeout**: Use IEnumerator with timeout
   - ❌ DOTween sequences not designed for coroutine control
   - ❌ Mixing paradigms creates confusion

4. **Watchdog Coroutine** (Chosen):
   - ✅ Simple, independent monitoring
   - ✅ Works regardless of callback execution
   - ✅ Clear separation of concerns

### Why Not Async/Await?

Unity's DOTween uses callbacks, not Tasks:
- Converting to async/await adds complexity
- Watchdog pattern works with any callback system
- No dependency on Unity 2021+ async features

## Future Enhancements

Potential improvements:

1. **Adaptive Timeouts**: Adjust based on device performance
2. **Analytics Integration**: Track timeout frequency for debugging
3. **Recovery Strategies**: Multiple recovery attempts before giving up
4. **User Notification**: Optional UI message when recovery occurs
5. **Animation Queue**: Queue operations instead of blocking

## Related Documentation

- [card-core-system.md](card-core-system.md) - Card Core system architecture
- [architecture.md](architecture.md) - Overall game architecture
- DOTween Documentation - http://dotween.demigiant.com/documentation.php

---

**Key Takeaway:** The watchdog system is a safety net that prevents catastrophic UI deadlocks from animation failures. Always start a watchdog coroutine when setting the animation flag, and always clean up in finally blocks.

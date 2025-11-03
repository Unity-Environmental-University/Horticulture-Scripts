# UIInput Management Architecture

**Last Updated:** 2025-11-03
**Unity Version:** 6000.1.11f1+

## Overview

The UIInputManager system provides centralized management of Unity's EventSystem (UIInput) state to prevent race conditions between competing systems that need to enable/disable UI input.

## Table of Contents
1. [The Problem](#the-problem)
2. [The Solution](#the-solution)
3. [Architecture](#architecture)
4. [Usage Guide](#usage-guide)
5. [API Reference](#api-reference)
6. [Migration Guide](#migration-guide)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

## The Problem

### Race Condition Scenario

Prior to UIInputManager, systems directly manipulated `CardGameMaster.Instance.uiInputModule.enabled`, which caused race conditions during state transitions.

**Concrete Example: Cinematic Skip Bug**

```
Timeline:
T0: Aphids cinematic starts
T1: CutsceneUIController.OnEnable() → uiInputModule.enabled = true
T2: User clicks "Skip" button
T3: CinematicDirector.SkipScene() → Director.Stop()
T4: Card diagram popup appears
T5: PopUpController.ActivatePopUpPanel() → uiInputModule.enabled = true
T6: CutsceneUIController.OnDisable() → uiInputModule.enabled = false ❌
Result: Popup is visible but UIInput is disabled - user cannot interact!
```

**Root Cause:**
Unity's OnDisable() can execute in unpredictable order relative to other code in the same frame. When the cinematic is skipped, its OnDisable() might fire AFTER the popup has already enabled input, overriding the popup's setting.

### Why Direct Access Fails

```csharp
// ❌ PROBLEMATIC PATTERN
public class PopUpController
{
    public void ActivatePopUpPanel()
    {
        CardGameMaster.Instance.uiInputModule.enabled = true; // Set to true
    }
}

public class CutsceneUIController
{
    private void OnDisable()
    {
        CardGameMaster.Instance.uiInputModule.enabled = false; // Overrides popup!
    }
}
```

**Issues:**
1. No ownership tracking - any system can override any other
2. No protection against late OnDisable() calls
3. No way to debug which system last changed the state
4. Silent failures - no warnings when conflicts occur

## The Solution

### Ownership-Based State Management

UIInputManager introduces an **ownership model**:
- Only ONE system can own UIInput at a time
- Only the current owner can disable UIInput
- Ownership transfers explicitly via RequestEnable()
- Non-owners are blocked from disabling with a warning

### How It Prevents the Bug

```
Timeline with UIInputManager:
T0: Aphids cinematic starts
T1: CutsceneUIController.OnEnable() → RequestEnable("CutsceneUI")
    Owner: "CutsceneUI", UIInput: enabled
T2: User clicks "Skip"
T3: CinematicDirector.SkipScene() → Director.Stop()
T4: Card diagram popup appears
T5: PopUpController.ActivatePopUpPanel() → RequestEnable("PopUpController")
    Owner: "PopUpController", UIInput: enabled ✅
T6: CutsceneUIController.OnDisable() → RequestDisable("CutsceneUI")
    ⚠️ Warning: Cannot disable - owned by "PopUpController"
    Owner: "PopUpController", UIInput: enabled ✅
Result: Popup remains interactive! Bug fixed.
```

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    UIInputManager                       │
│                   (Static Class)                        │
├─────────────────────────────────────────────────────────┤
│  - OwnershipLock: object                               │
│  - _currentOwner: string                               │
├─────────────────────────────────────────────────────────┤
│  + RequestEnable(owner: string)                        │
│  + RequestDisable(owner: string)                       │
│  + ForceState(enabled, owner, reason)                  │
│  + ReleaseOwnership(owner: string)                     │
│  + IsEnabled: bool                                     │
│  + CurrentOwner: string                                │
└─────────────────────────────────────────────────────────┘
         ▲              ▲              ▲              ▲
         │              │              │              │
    ┌────┴───┐   ┌─────┴─────┐  ┌────┴────┐   ┌────┴────┐
    │ Popup  │   │ Cutscene  │  │  Shop   │   │  Turn   │
    │Controller  │    UI     │  │ Manager │   │Controller
    └────────┘   └───────────┘  └─────────┘   └─────────┘
```

### Ownership State Machine

```
                    ┌──────────────┐
                    │   No Owner   │
                    │ (enabled=any)│
                    └──────┬───────┘
                           │
              RequestEnable("SystemA")
                           │
                           ▼
                    ┌──────────────┐
              ┌────▶│ Owner: A     │──────┐
              │     │ (enabled=true)│      │
              │     └──────────────┘      │
              │                            │
   RequestEnable("B")          RequestDisable("A")
   (ownership transfer)         (authorized)
              │                            │
              │                            ▼
              │                     ┌──────────────┐
              │                     │   No Owner   │
              │                     └──────────────┘
              │
              ▼
       ┌──────────────┐
       │ Owner: B     │
       │ (enabled=true)│
       └──────────────┘
              │
       RequestDisable("A")
       (blocked - not owner)
              │
              ▼
       (No state change,
        warning logged)
```

### Thread Safety

All methods use `lock (OwnershipLock)` to ensure thread-safe access to `_currentOwner`. This prevents race conditions in the ownership tracking itself, even when Unity lifecycle methods execute in unpredictable order within a frame.

## Usage Guide

### Basic Pattern

**For Systems with OnEnable/OnDisable:**
```csharp
using _project.Scripts.UI;

public class MyUISystem : MonoBehaviour
{
    private void OnEnable()
    {
        UIInputManager.RequestEnable("MyUISystem");
    }

    private void OnDisable()
    {
        UIInputManager.RequestDisable("MyUISystem");
    }
}
```

**For Manual Control (Popups, Shops):**
```csharp
using _project.Scripts.UI;

public class MyPopup : MonoBehaviour
{
    public void Show()
    {
        popupPanel.SetActive(true);
        UIInputManager.RequestEnable("MyPopup");
    }

    public void Hide()
    {
        popupPanel.SetActive(false);
        UIInputManager.RequestDisable("MyPopup");
    }
}
```

### Owner Naming Convention

Use specific, traceable names:
- ✅ "PopUpController" (class name)
- ✅ "CutsceneUI" (component name)
- ✅ "ShopManager" (system name)
- ✅ "TurnController" (specific controller)

Avoid generic names:
- ❌ "UI"
- ❌ "Controller"
- ❌ "Manager"
- ❌ "System"

## API Reference

### RequestEnable(string owner)

Takes ownership of UIInput and enables it.

**Behavior:**
- Immediately transfers ownership to `owner`
- Enables `uiInputModule`
- Previous owner is NOT notified
- Previous owner's attempts to disable will be blocked

**Example:**
```csharp
UIInputManager.RequestEnable("PopUpController");
// Owner: "PopUpController", UIInput: enabled
```

**Thread-Safe:** Yes

---

### RequestDisable(string owner)

Releases ownership and disables UIInput, but ONLY if caller is the current owner.

**Behavior:**
- Checks if `owner` matches `_currentOwner`
- If match: disables UIInput and clears ownership
- If no match: logs warning and does nothing
- If no owner: allows disable (backward compatibility)

**Example:**
```csharp
UIInputManager.RequestEnable("SystemA");
UIInputManager.RequestDisable("SystemB");
// Warning: Cannot disable - owned by 'SystemA'

UIInputManager.RequestDisable("SystemA");
// Success: UIInput disabled, owner cleared
```

**Thread-Safe:** Yes

---

### ForceState(bool enabled, string newOwner, ForcedStateReason reason)

Bypasses ownership model to force UIInput state. **Use sparingly.**

**Parameters:**
- `enabled`: Desired state (true/false)
- `newOwner`: New owner if enabling, null if disabling
- `reason`: One of: SceneTransition, CriticalError, GamePause, SaveLoad

**Behavior:**
- Unconditionally sets UIInput state
- Logs warning in editor/development builds
- Should only be used for exceptional cases

**Example:**
```csharp
UIInputManager.ForceState(false, null, ForcedStateReason.CriticalError);
// Forces UIInput off regardless of owner
```

**Thread-Safe:** Yes

---

### ReleaseOwnership(string owner)

Releases ownership without changing the enabled state.

**Use Case:** Cleanup when transferring control to another system that will immediately take ownership.

**Example:**
```csharp
UIInputManager.RequestEnable("SystemA");
UIInputManager.ReleaseOwnership("SystemA");
// Owner: null, UIInput: still enabled
```

**Thread-Safe:** Yes

---

### IsEnabled (property)

Returns current UIInput enabled state.

**Example:**
```csharp
if (UIInputManager.IsEnabled)
{
    // UIInput is currently enabled
}
```

**Thread-Safe:** Yes
**Null-Safe:** Yes (returns false if uiInputModule not found)

---

### CurrentOwner (property)

Returns the name of the current owner, or null if no owner.

**Example:**
```csharp
Debug.Log($"Current owner: {UIInputManager.CurrentOwner ?? "none"}");
```

**Thread-Safe:** Yes

---

## Migration Guide

### From Direct Access to UIInputManager

**Old Pattern:**
```csharp
public class CutsceneUIController : MonoBehaviour
{
    private void OnEnable()
    {
        CardGameMaster.Instance.uiInputModule.enabled = true;
    }

    private void OnDisable()
    {
        CardGameMaster.Instance.uiInputModule.enabled = false;
    }
}
```

**New Pattern:**
```csharp
using _project.Scripts.UI; // Add this

public class CutsceneUIController : MonoBehaviour
{
    private void OnEnable()
    {
        UIInputManager.RequestEnable("CutsceneUI");
    }

    private void OnDisable()
    {
        UIInputManager.RequestDisable("CutsceneUI");
    }
}
```

### Migration Checklist

1. ✅ Add `using _project.Scripts.UI;`
2. ✅ Replace `uiInputModule.enabled = true` with `UIInputManager.RequestEnable("SystemName")`
3. ✅ Replace `uiInputModule.enabled = false` with `UIInputManager.RequestDisable("SystemName")`
4. ✅ Choose a specific, traceable owner name
5. ✅ Test that UIInput behaves correctly during state transitions
6. ✅ Check console for any ownership conflict warnings

## Best Practices

### ✅ DO

1. **Use RequestEnable when taking control**
   ```csharp
   UIInputManager.RequestEnable("MySystem");
   ```

2. **Use RequestDisable when releasing control**
   ```csharp
   UIInputManager.RequestDisable("MySystem");
   ```

3. **Use specific owner names for debugging**
   ```csharp
   UIInputManager.RequestEnable("PopUpController"); // Clear which system
   ```

4. **Match enable/disable pairs**
   ```csharp
   OnEnable() → RequestEnable
   OnDisable() → RequestDisable
   ```

5. **Trust the ownership model**
   - Don't check IsEnabled before RequestEnable
   - Don't force state unless absolutely necessary

### ❌ DON'T

1. **Don't directly access uiInputModule.enabled**
   ```csharp
   // ❌ BAD
   CardGameMaster.Instance.uiInputModule.enabled = true;

   // ✅ GOOD
   UIInputManager.RequestEnable("MySystem");
   ```

2. **Don't use generic owner names**
   ```csharp
   // ❌ BAD
   UIInputManager.RequestEnable("UI");

   // ✅ GOOD
   UIInputManager.RequestEnable("ShopManager");
   ```

3. **Don't assume OnDisable executes before other systems**
   ```csharp
   // ❌ BAD - Assumes OnDisable runs first
   OnDisable() { uiInputModule.enabled = false; }

   // ✅ GOOD - Ownership model handles timing
   OnDisable() { UIInputManager.RequestDisable("MySystem"); }
   ```

4. **Don't call ForceState for normal operation**
   ```csharp
   // ❌ BAD - Bypasses ownership unnecessarily
   UIInputManager.ForceState(true, "Popup", ForcedStateReason.SceneTransition);

   // ✅ GOOD - Normal ownership request
   UIInputManager.RequestEnable("Popup");
   ```

## Troubleshooting

### UIInput Not Enabling

**Symptom:** You call RequestEnable but UIInput remains disabled.

**Possible Causes:**
1. CardGameMaster.Instance is null
2. uiInputModule is null
3. Another system is calling RequestDisable immediately after

**Debugging:**
```csharp
UIInputManager.RequestEnable("MySystem");
Debug.Log($"Owner: {UIInputManager.CurrentOwner}");
Debug.Log($"Enabled: {UIInputManager.IsEnabled}");
```

**Check Console For:**
- `[UIInputManager] Cannot enable UIInput - uiInputModule not found`

---

### UIInput Not Disabling

**Symptom:** You call RequestDisable but UIInput remains enabled.

**Root Cause:** You are not the current owner.

**Console Warning:**
```
[UIInputManager] Cannot disable UIInput - owned by 'PopUpController', requested by 'CutsceneUI'
```

**Solution:**
- This is expected behavior! Another system owns UIInput
- The warning tells you which system currently owns it
- Wait for the current owner to release ownership

---

### Ownership Conflicts

**Symptom:** Console shows repeated "Cannot disable" warnings.

**Example:**
```
[UIInputManager] Cannot disable UIInput - owned by 'SystemA', requested by 'SystemB'
```

**Diagnosis:**
1. Check which systems are calling RequestDisable in their OnDisable()
2. Verify the lifecycle order - which system should own UIInput?
3. Consider if one system should ReleaseOwnership before the other takes over

**Resolution:**
- If both systems need exclusive control, ensure they don't overlap
- If ownership transfer is intentional, this is correct behavior (warning can be ignored)
- If unintentional, fix the system lifecycle to avoid overlap

---

### Null Reference Errors

**Symptom:** NullReferenceException when calling UIInputManager methods.

**Root Cause:** CardGameMaster.Instance or uiInputModule is null.

**When This Occurs:**
- During scene transitions
- Before CardGameMaster.Awake() has run
- After CardGameMaster has been destroyed

**Solution:**
- UIInputManager is null-safe - it logs warnings instead of throwing
- Check console for: `[UIInputManager] Cannot enable/disable UIInput - uiInputModule not found`
- Ensure CardGameMaster exists in the scene
- Don't call UIInput methods during scene load/unload

---

## Testing

UIInputManager has comprehensive test coverage in `UIInputManagerTests.cs`:

**Key Tests:**
- `RequestEnable_enables_UIInput` - Basic functionality
- `RequestDisable_by_non_owner_does_not_disable` - Ownership enforcement
- `RaceCondition_popup_after_cinematic_skip` - Original bug scenario
- `RaceCondition_OnDisable_after_ownership_transfer` - Late OnDisable scenario
- `ForceState_overrides_ownership` - Force state bypass
- `MultipleSystemsSequence_ShopToWinScreen` - Real-world usage

**Run Tests:**
```bash
# Unity Editor: Window > General > Test Runner
# Command Line:
/Applications/Unity/Hub/Editor/6000.2.6f2/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform playmode \
  -testResults TestResults.xml -logFile TestLog.txt \
  -projectPath . -testFilter "UIInputManagerTests" -quit
```

---

## Related Documentation

- [Cinematics System Documentation](./CinematicsSystemDocumentation.md) - UIInput usage in cinematics
- [PopUpController.cs](../UI/PopUpController.cs) - Popup UIInput management
- [CLAUDE.md](../../../CLAUDE.md) - Project architecture overview

---

## Changelog

**2025-11-03:** Initial implementation
- Created UIInputManager with ownership model
- Migrated all direct `uiInputModule.enabled` access
- Added comprehensive test suite
- Fixed cinematic skip race condition bug

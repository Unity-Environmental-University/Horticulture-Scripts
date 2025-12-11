# Troubleshooting Guide

**Common issues and their solutions when developing Horticulture.**

## 🚨 Common Issues

### Cards Not Responding to Clicks

**Symptoms**: Clicking on cards doesn't select them or place them

**Possible Causes & Solutions**:

1. **Click3D Globally Disabled**
   ```csharp
   // Check if globally disabled
   if (Click3D.click3DGloballyDisabled) {
       Click3D.click3DGloballyDisabled = false;
   }
   ```

2. **Click3D Component Missing/Disabled**
   - Check GameObject has `Click3D` component
   - Verify component is enabled in inspector
   - Ensure collider is present and enabled

3. **Collider Issues**
   - Collider might be too small
   - Layer mask might be blocking raycasts
   - Check camera raycast settings

4. **CardGameMaster isInspecting**
   ```csharp
   // Cards disabled during inspection mode
   if (CardGameMaster.Instance.isInspecting) {
       // Wait until inspection ends
   }
   ```

**Related**: [[ui-input-management|UI Input System]]

---

### Plants Not Showing Afflictions

**Symptoms**: Afflictions applied but not visible on plant

**Solutions**:

1. **Check Affliction Was Applied**
   ```csharp
   if (plant.CurrentAfflictions.Count == 0) {
       Debug.LogWarning("No afflictions on plant");
   }
   ```

2. **Visual Update Not Triggered**
   ```csharp
   plant.FlagShadersUpdate(); // Force shader update
   ```

3. **Particle System Issues**
   - Check particle prefab reference exists
   - Verify particle system is not disabled
   - Check particle system scaling

**Related**: [[Plant-System|Plant System Docs]]

---

### Save/Load Not Working

**Symptoms**: Game doesn't save or load properly, data loss

**Possible Causes**:

1. **File Permissions**
   - Check write permissions in save directory
   - Verify path exists: `Application.persistentDataPath`

2. **Serialization Issues**
   ```csharp
   // Ensure all save data classes are marked [Serializable]
   [Serializable]
   public class MyData { }
   ```

3. **Missing References**
   - CardGameMaster not in scene
   - DeckManager not initialized
   - Game state corrupted

**Debug Commands**:
```csharp
// Check if save exists
if (GameStateManager.SaveExists()) {
    Debug.Log("Save file found");
}

// Try manual save
try {
    CardGameMaster.Instance.Save();
} catch (Exception e) {
    Debug.LogError($"Save failed: {e}");
}
```

**Related**: [[game-state-system-documentation|Game State Docs]]

---

### Redraw Not Working

**Symptoms**: Redraw button doesn't work or shows wrong message

**Causes & Solutions**:

1. **Cards Already Placed This Turn**
   - Cannot redraw after placing any card this turn
   - Check if cards on table are from current turn
   
2. **Animations Running**
   - Wait for card animations to complete
   - Check `TurnController.canClickEnd`

3. **Insufficient Funds**
   ```csharp
   int currentMoney = ScoreManager.GetMoneys();
   int redrawCost = deckManager.redrawCost;
   if (currentMoney < redrawCost) {
       Debug.Log("Not enough money to redraw");
   }
   ```

**Related**: [[card-core-system#redraw-functionality|Card System - Redraw]]

---

### Visual Effects Not Playing

**Symptoms**: No particles, sounds, or animations

**Solutions**:

1. **Effect Queue Check**
   ```csharp
   // Effects queued via TurnController
   TurnController.QueuePlantEffect(plant, particle, sound, delay);
   ```

2. **Missing References**
   - Check particle system prefab
   - Verify audio clip assigned
   - Ensure sound system exists

3. **Audio Source Issues**
   - Check audio source component
   - Verify volume settings
   - Check audio listener in scene

**Related**: [[audio-system-documentation|Audio System]]

---

### Performance Issues

**Symptoms**: Low FPS, stuttering, lag

**Diagnostic Steps**:

1. **Check Frame Rate**
   ```csharp
   // Add to Update()
   if (Time.frameCount % 60 == 0) {
       Debug.Log($"FPS: {1f / Time.deltaTime}");
   }
   ```

2. **Profile the Game**
   - Open Unity Profiler (`Window > Analysis > Profiler`)
   - Record while playing
   - Look for CPU spikes or memory issues

3. **Common Performance Culprits**
   - Too many particle systems active
   - Excessive GameObject creation/destruction
   - Unoptimized shaders
   - GC allocations in Update()

**Optimization Tips**:
- Use object pooling for frequently created objects
- Cache component references
- Reduce string concatenation
- Batch UI updates

**Related**: [[Performance-Optimization|Performance Optimization]]

---

### Testing Issues

**Symptoms**: Tests failing or not running

**Solutions**:

1. **Test Assembly Not Found**
   - Verify `PlayModeTest.asmdef` exists
   - Check assembly references are correct
   - Reimport test scripts

2. **GameObject Cleanup**
   ```csharp
   [TearDown]
   public void TearDown()
   {
       // Always cleanup test objects
       if (testObject != null)
           Object.DestroyImmediate(testObject);
   }
   ```

3. **Async Test Issues**
   ```csharp
   // Use [UnityTest] for coroutines
   [UnityTest]
   public IEnumerator MyAsyncTest()
   {
       yield return SomeCoroutine();
       Assert.IsTrue(condition);
   }
   ```

**Related**: [[testing-guide|Testing Guide]]

---

### Build Errors

**Symptoms**: Build fails or game doesn't work in build

**Common Issues**:

1. **Resources Not Loaded**
   - All materials must be in `Resources/` folder
   - Check material paths are correct
   - Use `Resources.Load<Material>("path")`

2. **Scene Not in Build Settings**
   - Add scenes to Build Settings
   - Verify scene order

3. **Platform-Specific Issues**
   - Check platform player settings
   - Verify API compatibility level
   - Test on target platform

---

## 🔧 Debug Tools

### Enable Debug Logging

```csharp
// In CardGameMaster
CardGameMaster.Instance.debuggingCardClass = true;
```

### Common Debug Commands

```csharp
// Log game state
Debug.Log($"Turn: {turnController.currentTurn}, Round: {turnController.currentRound}");
Debug.Log($"Money: {ScoreManager.GetMoneys()}");
Debug.Log($"Hand size: {deckManager.actionHand.Count}");

// Log plant state
foreach (var plant in deckManager.cachedPlantControllers) {
    Debug.Log($"Plant: {plant.type}, Afflictions: {plant.CurrentAfflictions.Count}");
}
```

### Unity Console Filters

- Click "Collapse" to group similar messages
- Use search to filter specific logs
- Right-click to copy stack traces

---

## 🆘 Getting Help

If you're still stuck:

1. **Check Related Documentation**
   - [[api-reference|API Reference]]
   - [[ARCHITECTURE|Architecture Docs]]
   - System-specific documentation

2. **Search the Wiki**
   - Use Ctrl/Cmd + O to search
   - Check backlinks for related pages

3. **Review Code**
   - Look at working examples in codebase
   - Check test files for usage patterns

4. **Ask the Team**
   - Provide error messages
   - Include reproduction steps
   - Share relevant code snippets

---

## 📋 Debugging Checklist

Before reporting a bug:

- [ ] Check Unity Console for errors
- [ ] Verify all component references are assigned
- [ ] Try restarting Unity Editor
- [ ] Check if issue reproduces in fresh scene
- [ ] Review recent code changes
- [ ] Check if tests still pass
- [ ] Try with new save file (delete old save)
- [ ] Verify Unity version matches project requirements

---

## 🔗 Related Resources

- [[Quick-Reference|Quick Reference]]
- [[Common-Workflows|Common Workflows]]
- [[developer-onboarding|Developer Onboarding]]
- [[Code-Standards|Coding Standards]]

---

*Can't find your issue? Add it to this page when you solve it!*

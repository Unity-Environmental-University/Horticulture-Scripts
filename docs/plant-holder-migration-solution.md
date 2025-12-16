# PlantHolder Serialization Migration Solution

## Problem Analysis

### Root Cause
When `DeckManager.plantLocations` was changed from `List<Transform>` to `List<PlantHolder>`, Unity's serialization broke:
- Unity scene files (.unity) stored serialized references to Transform objects
- Unity cannot automatically convert Transform references to PlantHolder objects
- Result: `plantLocations` list is empty in scenes → plants don't spawn

### Impact Scope
**Files Affected:**
- `Assets/_project/Scenes/Main.unity` - Primary game scene
- `Assets/_project/Scenes/CardGame.unity` - Card game scene
- Any other scenes with DeckManager components

**Code Dependencies:**
- `DeckManager.cs` - Line 200: `public List<PlantHolder> plantLocations`
- `GameStateManager.cs` - Line 261-263: Accesses `plantLocations[i].Transform`
- `TurnController.cs` - Multiple locations querying `plantLocations`
- `PlantHolder.cs` - The wrapper class itself

## Recommended Solution: Hybrid Migration Approach

### Strategy: OnValidate + EditorScript Combo

This combines immediate auto-fixing with a one-time migration tool for robustness.

## Implemented Fix

- `Assets/_project/Scripts/Editor/PlantHolderMigrationTool.cs` migrates legacy YAML where `plantLocations` is still a `List<Transform>` (entries like `- {fileID: ...}`) into the `PlantHolder` shape (`- plantLocation: {fileID: ...}` plus an empty `placedCardHolders` list).
- Run it in Unity via: `Tools > Migration > Migrate DeckManager Plant Locations (Transform -> PlantHolder)`

#### Phase 1: OnValidate Auto-Migration (IMMEDIATE FIX)
Add backward-compatible field and auto-migration logic to DeckManager that runs in Editor.

#### Phase 2: Editor Migration Tool (SAFETY NET)
Create editor script to batch-migrate all scenes and ensure consistency.

## Implementation Plan

### Step 1: Add Backward Compatibility Field to DeckManager

**File:** `/Users/donovan/Documents/Unity Projects/Horticulture/Assets/_project/Scripts/Card Core/DeckManager.cs`

**Location:** After line 200 (current plantLocations declaration)

**Changes:**
```csharp
// NEW: Backward compatibility field for migration from List<Transform>
[SerializeField, HideInInspector]
private List<Transform> _legacyPlantLocations;

public List<PlantHolder> plantLocations;
```

**Rationale:** 
- Unity preserves old serialized data in renamed fields
- `HideInInspector` prevents confusion in Inspector
- We'll read from this during migration, then clear it

### Step 2: Add OnValidate Migration Logic to DeckManager

**File:** `/Users/donovan/Documents/Unity Projects/Horticulture/Assets/_project/Scripts/Card Core/DeckManager.cs`

**Location:** Add new method in the "Initialization" region (around line 288)

**Changes:**
```csharp
#if UNITY_EDITOR
/// <summary>
/// Automatically migrates legacy Transform list to PlantHolder list.
/// Runs in Editor when component is loaded or modified.
/// </summary>
private void OnValidate()
{
    // Only migrate if we have legacy data and current list is empty/null
    if (_legacyPlantLocations != null && _legacyPlantLocations.Count > 0)
    {
        // Initialize plantLocations if null
        plantLocations ??= new List<PlantHolder>();
        
        // Only migrate if target list is empty (avoid overwriting manual fixes)
        if (plantLocations.Count == 0)
        {
            Debug.Log($"[DeckManager] Auto-migrating {_legacyPlantLocations.Count} plant locations to PlantHolder format");
            
            foreach (var transform in _legacyPlantLocations)
            {
                if (transform)
                {
                    plantLocations.Add(new PlantHolder(transform, initializeCardHolders: false));
                }
            }
            
            // Clear legacy list after successful migration
            _legacyPlantLocations.Clear();
            
            // Mark scene dirty to save migration
            UnityEditor.EditorUtility.SetDirty(this);
            
            Debug.Log($"[DeckManager] Migration complete: {plantLocations.Count} locations migrated");
        }
    }
    
    // Initialize PlantHolders if needed (call existing method)
    if (plantLocations != null && plantLocations.Count > 0)
    {
        foreach (var holder in plantLocations)
        {
            holder?.InitializeCardHolders();
        }
    }
}
#endif
```

**Rationale:**
- `OnValidate` runs automatically when Unity loads the component in Editor
- Migration is non-destructive (only runs if target is empty)
- Marks scene dirty to ensure Unity saves the changes
- Editor-only code won't bloat runtime builds

### Step 3: Create Editor Migration Tool

**File:** `/Users/donovan/Documents/Unity Projects/Horticulture/Assets/_project/Scripts/Editor/PlantHolderMigrationTool.cs` (NEW FILE)

**Full Implementation:**
```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _project.Scripts.Editor
{
    /// <summary>
    /// Editor tool to migrate DeckManager.plantLocations from List&lt;Transform&gt; to List&lt;PlantHolder&gt;.
    /// </summary>
    public class PlantHolderMigrationTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<string> _scenePaths;
        private bool _includeDisabledScenes = true;
        
        [MenuItem("Tools/Migration/PlantHolder Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlantHolderMigrationTool>("PlantHolder Migration");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshSceneList();
        }

        private void RefreshSceneList()
        {
            // Find all scene files in the project
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            _scenePaths = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Contains("_project/Scenes")) // Focus on project scenes
                .OrderBy(path => path)
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PlantHolder Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool migrates DeckManager.plantLocations from List<Transform> to List<PlantHolder>.\n\n" +
                "How it works:\n" +
                "1. Scans selected scenes for DeckManager components\n" +
                "2. Reads legacy _legacyPlantLocations field (old Transform list)\n" +
                "3. Creates new PlantHolder objects and populates plantLocations\n" +
                "4. Saves the scene with migrated data\n\n" +
                "Safe to run multiple times (skips already-migrated scenes).",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Options
            _includeDisabledScenes = EditorGUILayout.Toggle("Include Disabled Scenes", _includeDisabledScenes);
            
            EditorGUILayout.Space(10);
            
            // Scene list
            EditorGUILayout.LabelField($"Found {_scenePaths?.Count ?? 0} Project Scenes:", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            if (_scenePaths != null)
            {
                foreach (var scenePath in _scenePaths)
                {
                    EditorGUILayout.LabelField(scenePath);
                }
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Scene List", GUILayout.Height(30)))
            {
                RefreshSceneList();
            }
            
            if (GUILayout.Button("Migrate Current Scene", GUILayout.Height(30)))
            {
                MigrateCurrentScene();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Migrate All Project Scenes", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Migrate All Scenes?",
                    $"This will open and migrate {_scenePaths.Count} scenes. Continue?",
                    "Yes, Migrate All",
                    "Cancel"))
                {
                    MigrateAllScenes();
                }
            }
            
            EditorGUILayout.Space(10);
        }

        private void MigrateCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[Migration] No active scene to migrate");
                EditorUtility.DisplayDialog("No Active Scene", "Please open a scene first.", "OK");
                return;
            }
            
            var count = MigrateScene(activeScene);
            
            if (count > 0)
            {
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[Migration] Migrated {count} DeckManager(s) in scene: {activeScene.name}");
                EditorUtility.DisplayDialog("Migration Complete", 
                    $"Successfully migrated {count} DeckManager component(s) in {activeScene.name}", "OK");
            }
            else
            {
                Debug.Log($"[Migration] No migration needed in scene: {activeScene.name}");
                EditorUtility.DisplayDialog("No Migration Needed", 
                    $"Scene '{activeScene.name}' has no DeckManagers requiring migration.", "OK");
            }
        }

        private void MigrateAllScenes()
        {
            if (_scenePaths == null || _scenePaths.Count == 0)
            {
                Debug.LogWarning("[Migration] No scenes found to migrate");
                return;
            }

            var totalMigrated = 0;
            var scenesModified = 0;

            for (var i = 0; i < _scenePaths.Count; i++)
            {
                var scenePath = _scenePaths[i];
                
                // Show progress bar
                EditorUtility.DisplayProgressBar(
                    "Migrating Scenes",
                    $"Processing: {scenePath}",
                    (float)i / _scenePaths.Count
                );

                try
                {
                    // Open scene additively to avoid losing current work
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    
                    var count = MigrateScene(scene);
                    
                    if (count > 0)
                    {
                        EditorSceneManager.SaveScene(scene);
                        totalMigrated += count;
                        scenesModified++;
                        Debug.Log($"[Migration] Migrated {count} DeckManager(s) in: {scenePath}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Migration] Failed to migrate scene {scenePath}: {ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            
            Debug.Log($"[Migration] COMPLETE: Migrated {totalMigrated} DeckManager(s) across {scenesModified} scene(s)");
            EditorUtility.DisplayDialog(
                "Migration Complete",
                $"Successfully migrated {totalMigrated} DeckManager component(s) across {scenesModified} scene(s).",
                "OK"
            );
        }

        /// <summary>
        /// Migrates all DeckManager components in the given scene.
        /// Returns the number of DeckManagers that were migrated.
        /// </summary>
        private static int MigrateScene(Scene scene)
        {
            var deckManagers = Object.FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
            var migratedCount = 0;

            foreach (var dm in deckManagers)
            {
                if (MigrateDeckManager(dm))
                {
                    migratedCount++;
                }
            }

            return migratedCount;
        }

        /// <summary>
        /// Migrates a single DeckManager from legacy Transform list to PlantHolder list.
        /// Returns true if migration was performed.
        /// </summary>
        private static bool MigrateDeckManager(DeckManager dm)
        {
            // Use SerializedObject to access private _legacyPlantLocations field
            var so = new SerializedObject(dm);
            var legacyProp = so.FindProperty("_legacyPlantLocations");
            var newProp = so.FindProperty("plantLocations");
            
            // Check if migration is needed
            if (legacyProp != null && legacyProp.arraySize > 0)
            {
                // Only migrate if target list is empty
                if (newProp == null || newProp.arraySize == 0)
                {
                    Debug.Log($"[Migration] Migrating DeckManager on GameObject: {dm.gameObject.name}");
                    
                    // Create new PlantHolder list
                    var newHolders = new List<PlantHolder>();
                    
                    for (var i = 0; i < legacyProp.arraySize; i++)
                    {
                        var transformProp = legacyProp.GetArrayElementAtIndex(i);
                        var transformRef = transformProp.objectReferenceValue as Transform;
                        
                        if (transformRef)
                        {
                            newHolders.Add(new PlantHolder(transformRef, initializeCardHolders: false));
                        }
                    }
                    
                    // Assign to plantLocations
                    dm.plantLocations = newHolders;
                    
                    // Clear legacy array
                    legacyProp.ClearArray();
                    so.ApplyModifiedProperties();
                    
                    // Mark dirty
                    EditorUtility.SetDirty(dm);
                    
                    Debug.Log($"[Migration] Migrated {newHolders.Count} plant locations for {dm.gameObject.name}");
                    return true;
                }
                else
                {
                    Debug.Log($"[Migration] DeckManager on {dm.gameObject.name} already has plantLocations, skipping");
                }
            }
            
            return false;
        }
    }
}
#endif
```

**Rationale:**
- Provides a GUI tool for batch migration
- Safe to run multiple times (checks before migrating)
- Progress feedback for large projects
- Can migrate individual scenes or all at once

### Step 4: Update PlantHolder to Support Runtime Initialization

**File:** `/Users/donovan/Documents/Unity Projects/Horticulture/Assets/_project/Scripts/Card Core/PlantHolder.cs`

**Changes:** None needed - existing implementation already supports runtime initialization via constructor

### Step 5: Create Migration Documentation

**File:** `/Users/donovan/Documents/Unity Projects/Horticulture/Assets/_project/Scripts/docs/plant-holder-migration-guide.md` (NEW FILE)

**Content:**
```markdown
# PlantHolder Migration Guide

## For Developers: What Happened?

`DeckManager.plantLocations` was refactored from `List<Transform>` to `List<PlantHolder>` to centralize plant location management and cache child components. This broke Unity scene serialization.

## Automatic Migration (Preferred)

### Option 1: OnValidate (Automatic)
1. Open any scene with a DeckManager component in Unity Editor
2. Select the DeckManager GameObject in the Hierarchy
3. Migration happens automatically via `OnValidate()`
4. Check Console for migration log messages
5. Save the scene (Ctrl/Cmd + S)

### Option 2: Migration Tool (Batch Processing)
1. In Unity Editor: `Tools > Migration > PlantHolder Migration Tool`
2. Click "Migrate Current Scene" for active scene
3. OR click "Migrate All Project Scenes" for batch migration
4. Tool shows progress and results

## Manual Migration (Fallback)

If automatic migration fails:

1. Open scene in Unity Editor
2. Find DeckManager GameObject
3. In Inspector, locate `plantLocations` field
4. Manually assign Transform references by:
   - Click "+" to add elements
   - Drag plant location GameObjects to each slot
   - CardHolders will auto-initialize at runtime

## Verification

After migration:
1. Enter Play Mode
2. Check that plants spawn at round start
3. Check Console for errors
4. If plants don't spawn: verify `plantLocations` has entries in Inspector

## Troubleshooting

### "plantLocations is empty"
- Run migration tool again
- Check if _legacyPlantLocations had data (requires scene file inspection)
- Manually assign plant location Transforms in Inspector

### "Plants still don't spawn"
- Check `DeckManager.InitializePlantHolders()` is called
- Verify plant location GameObjects exist in scene
- Check Console for null reference errors

### "Migration ran but list is still empty"
- Check if legacy field `_legacyPlantLocations` contains data
- May need to restore from version control before refactor
- Fallback: manual reassignment in Inspector

## For New Scenes

When creating new scenes:
1. Add plant location GameObjects to scene
2. Add DeckManager component
3. Drag plant locations to `plantLocations` list in Inspector
4. PlantHolders auto-create from Transform references
```

## Testing Strategy

### Test 1: Verify OnValidate Migration
1. Open Main.unity scene
2. Select DeckManager GameObject
3. Check Inspector for `plantLocations` list populated
4. Enter Play Mode
5. Verify plants spawn at round start

### Test 2: Verify Migration Tool
1. Open Migration Tool: `Tools > Migration > PlantHolder Migration Tool`
2. Click "Migrate Current Scene"
3. Check Console for migration logs
4. Verify `plantLocations` list populated in Inspector

### Test 3: Verify Runtime Functionality
1. Enter Play Mode in migrated scene
2. Start new round
3. Verify plants spawn at correct locations
4. Place treatment cards on plants
5. Save and load game state
6. Verify plants restore to correct locations

### Test 4: Verify Serialization Persistence
1. Migrate scene
2. Save scene
3. Close Unity
4. Reopen Unity and scene
5. Verify `plantLocations` still populated (not reverted)

### Test 5: Test New Scene Creation
1. Create new empty scene
2. Add DeckManager
3. Add plant location GameObjects
4. Manually assign to `plantLocations` via Inspector
5. Verify PlantHolders auto-create

## Rollback Plan

If migration causes critical issues:

### Option 1: Revert Scene Files
```bash
# If using version control
git checkout HEAD~1 -- "Assets/_project/Scenes/*.unity"
```

### Option 2: Manual Revert
1. Restore scene files from backup
2. Revert PlantHolder refactor:
   - Change `List<PlantHolder>` back to `List<Transform>`
   - Remove PlantHolder wrapper class
3. Remove migration code

## Future-Proofing

To prevent similar issues:

1. **Migration Scripts First**: Always write migration tools BEFORE refactoring serialized fields
2. **Gradual Migration**: Use backward-compatible fields during transition period
3. **Test in Isolation**: Test migration on copy of scenes before applying to all
4. **Version Control**: Commit scenes before and after migration separately
5. **Documentation**: Document serialization changes in commit messages

## Performance Considerations

- OnValidate only runs in Editor (zero runtime cost)
- PlantHolder caching reduces `GetComponentsInChildren` calls at runtime
- Migration is one-time operation per scene

## Security Considerations

- Migration tool only modifies project scenes (no external files)
- No user input validation needed (tool-only workflow)
- SerializedObject API ensures Unity validates data types

---

**Last Updated:** 2025-12-16  
**Migration Tool Location:** `Assets/_project/Scripts/Editor/PlantHolderMigrationTool.cs`  
**Related Classes:** `DeckManager`, `PlantHolder`, `GameStateManager`
```

## Risk Assessment

### Low Risk ✅
- OnValidate migration (non-destructive, skips if already migrated)
- Editor tool (explicit user action required)
- Backward-compatible field (preserves old data)

### Medium Risk ⚠️
- Scene file corruption if Unity crashes during save
- **Mitigation:** Backup scenes before migration, use version control

### High Risk ❌
- None identified (migration is additive, doesn't remove data)

## Success Criteria

1. ✅ Plants spawn correctly in all scenes after migration
2. ✅ GameStateManager save/load works with PlantHolder
3. ✅ Existing tests pass without modification
4. ✅ New scenes work with PlantHolder from Inspector
5. ✅ Migration completes in <1 minute for all scenes

## Alternative Approaches Considered

### Option B: Custom Property Drawer
**Pros:** Better Inspector UX, drag-drop Transform → auto-convert to PlantHolder  
**Cons:** Doesn't fix existing serialization, requires manual reassignment  
**Verdict:** Good for future UX, doesn't solve immediate problem

### Option C: ScenePostProcessor
**Pros:** Runs automatically on scene load without OnValidate  
**Cons:** More complex, harder to debug, less explicit  
**Verdict:** Overkill for one-time migration

### Option D: Force Manual Reassignment
**Pros:** Simple, no code needed  
**Cons:** Error-prone, time-consuming, poor DX  
**Verdict:** Acceptable fallback, not primary solution

## Implementation Checklist

- [ ] Add `_legacyPlantLocations` field to DeckManager
- [ ] Add `OnValidate()` method to DeckManager
- [ ] Create `PlantHolderMigrationTool.cs` in Editor folder
- [ ] Create migration documentation
- [ ] Test OnValidate in Main.unity scene
- [ ] Test Migration Tool on CardGame.unity scene
- [ ] Run all existing PlayMode tests
- [ ] Verify save/load functionality
- [ ] Document changes in commit message

---

## Summary

**Primary Approach:** OnValidate + Editor Tool Combo  
**Estimated Implementation Time:** 1-2 hours  
**Estimated Migration Time:** <5 minutes for all scenes  
**Risk Level:** Low  
**Reversibility:** High (keep backup scenes, version control)

# Efficacy Discovery System

**Status:** Implemented
**Version:** 1.0
**Last Updated:** 2026-01-26

## Overview

The Efficacy Discovery System is an **optional** educational game mechanic that encourages player experimentation by hiding treatment effectiveness percentages until players actually apply treatments to afflicted plants. This "learning through experimentation" approach aligns with the game's Integrated Pest Management (IPM) teaching goals.

**Default:** Discovery mode is **enabled** by default.

**Player Control:** Players can toggle discovery mode on/off and reset their discovery progress at any time.

## Player Experience

### Discovery Flow

1. **Initial State**: Player hovers a treatment card over an afflicted plant
2. **Undiscovered**: Display shows **"?"** in yellow color
3. **Application**: Player applies the treatment to the plant
4. **Revelation**: The actual efficacy percentage is revealed (e.g., "85%")
5. **Persistence**: Future hovers show the discovered percentage

### Visual Feedback

```
Undiscovered: "?" (Yellow)     - When discovery mode is ON
Low Efficacy:  "25%" (Red)     - Less than 50%
Medium:        "65%" (Yellow)  - 50-74%
High:          "90%" (Green)   - 75% or higher
```

### Player Choice & Accessibility

**Why Optional?**
- **Learning Preference**: Some players prefer guided experimentation, others want direct information
- **Replayability**: Players can toggle discovery mode for different playthroughs
- **Accessibility**: Players with cognitive differences may prefer seeing all information upfront
- **Time Constraints**: Experienced players can disable discovery mode for faster gameplay

**Discovery Mode ON** (Default):
- Educational focus - learning through experimentation
- "?" shown for undiscovered combinations
- Encourages strategic thinking about treatment choices
- Discovery tracked and persisted

**Discovery Mode OFF**:
- All efficacy percentages visible immediately
- No discovery tracking or analytics
- Direct access to treatment effectiveness data
- Faster decision-making for experienced players

## Technical Architecture

### Core Components

#### 1. TreatmentEfficacyHandler
**Location:** `Assets/_project/Scripts/Handlers/TreatmentEfficacyHandler.cs`

Central system managing both efficacy calculations and discovery tracking.

**Key Fields:**
```csharp
private HashSet<string> discoveredCombinations;  // Tracks discovered treatment-affliction pairs
```

**Discovery Key Format:**
```
"TreatmentName|AfflictionName"
Example: "Permethrin Spray|Spider Mites"
```

**Public API:**
```csharp
// Check if a combination has been discovered
bool IsDiscovered(string treatmentName, string afflictionName)

// Serialization for save/load
List<string> GetDiscoveredCombinationsForSave()
void RestoreDiscoveredCombinations(List<string> saved)

// Player control methods
bool DiscoveryModeEnabled { get; set; }  // Toggle discovery mode on/off
void ClearAllDiscoveries()               // Reset all discovery progress
int GetDiscoveryCount()                  // Get total discoveries made
```

**Discovery Logic:**
- Discovery marks when `GetRelationalEfficacy()` is called with `countInteraction: true`
- Preview mode (`countInteraction: false`) does NOT reveal combinations
- Each discovery fires an analytics event (once per combination)

#### 2. EfficacyDisplayHandler
**Location:** `Assets/_project/Scripts/Handlers/EfficacyDisplayHandler.cs`

Manages UI display of efficacy percentages and "?" symbols.

**Key Methods:**
```csharp
// Single affliction display
private void UpdateDisplay(IAffliction affliction, ITreatment treatment)

// Multiple afflictions display (average)
private void DisplayAverageEfficacy(PlantController controller, ITreatment treatment)
```

**Display Rules:**
- **Single Affliction**: Show "?" if undiscovered, percentage if discovered
- **Multiple Afflictions**: Show "?" if ANY affliction is undiscovered
  - This prevents arithmetic deduction of individual efficacies
  - Example: If average is 60% and one affliction is known (70%), player could calculate the other

#### 3. Persistence Integration
**Locations:**
- `GameStateData.cs` - Data structure: `List<string> discoveredEfficacies`
- `GameStateManager.cs` - Save/load logic

**Save Data Format:**
```json
{
  "discoveryModeEnabled": true,
  "discoveredEfficacies": [
    "Permethrin Spray|Spider Mites",
    "Soapy Water|Aphids",
    "Horticultural Oil|Thrips"
  ]
}
```

**Backward Compatibility:**
- Old saves without `discoveredEfficacies` field default to empty list
- Old saves without `discoveryModeEnabled` field default to `true` (enabled)
- Players loading old saves will need to re-discover combinations
- No errors or data corruption from missing fields

#### 4. Analytics Integration
**Locations:**
- `AnalyticsEvents.cs` - Event definition
- `AnalyticsFunctions.cs` - Recording method

**Event Parameters:**
```csharp
EfficacyDiscoveredEvent {
    string TreatmentName,
    string AfflictionName,
    int CurrentRound,
    int CurrentTurn,
    int Efficacy
}
```

**Analytics Use Cases:**
- Track learning progression (discovery order)
- Identify challenging afflictions (low discovery rates)
- Measure player engagement (discovery frequency)
- Evaluate tutorial effectiveness

## Implementation Details

### Discovery Tracking

**Name-Based vs Reference-Based:**
- System uses **name-based tracking** for mod compatibility
- Mod-loaded treatments and afflictions work seamlessly
- Names are compared using `string.Equals()` with ordinal comparison

**Performance:**
- HashSet provides O(1) lookup for discovery checks
- Typical max size: ~50 combinations (all treatments × all afflictions)
- Memory footprint: ~2.5KB
- No performance impact on UI hover operations

### Edge Cases

#### Multiple Afflictions (Average Display)
**Scenario:** Plant has 3 afflictions, treatment affects all 3
- If ANY affliction is undiscovered → Show "?"
- If ALL afflictions are discovered → Show average percentage

**Rationale:** Prevents information leakage through arithmetic

#### Thrips Special Case
**Scenario:** Thrips have larvae and adult forms
- Discovery tracks as single combination: "Treatment|Thrips"
- Revealing efficacy for larvae also reveals it for adults
- Simplifies player mental model

**Implementation:** No special handling needed - name-based tracking naturally handles this

#### Incompatible Treatments
**Scenario:** Treatment cannot affect the affliction
- Shown as "0%" (red) - NOT as "?"
- Helps players quickly identify unusable treatments
- No discovery needed (incompatibility is immediately obvious)

### Mod Compatibility

**Constraints for Mod Creators:**

⚠️ **Important:** Treatment and affliction names MUST NOT contain pipe character `|`
- Used as internal delimiter in discovery keys
- Violating this constraint will break discovery tracking

**Valid Names:**
```
✅ "Permethrin Spray"
✅ "Spider Mites (Adults)"
✅ "Neem Oil Treatment"
```

**Invalid Names:**
```
❌ "Spray|Treatment"
❌ "Rust|Fungus"
```

**Case Sensitivity:**
- Discovery matching is case-sensitive
- `ITreatment.Name` must exactly match JSON `name` field
- `IAffliction.Name` must exactly match JSON `name` field

## Usage Examples

### Developer: Checking Discovery Status
```csharp
var handler = CardGameMaster.Instance.treatmentEfficacyHandler;
bool isKnown = handler.IsDiscovered("Permethrin Spray", "Spider Mites");

if (isKnown)
    Debug.Log("Player has discovered this combination");
```

### Developer: Toggling Discovery Mode
```csharp
var handler = CardGameMaster.Instance.treatmentEfficacyHandler;

// Disable discovery mode (show all efficacy percentages)
handler.DiscoveryModeEnabled = false;

// Re-enable discovery mode
handler.DiscoveryModeEnabled = true;

// Check current state
if (handler.DiscoveryModeEnabled)
    Debug.Log("Discovery mode is active");
```

### Developer: Clearing Discovery Progress
```csharp
var handler = CardGameMaster.Instance.treatmentEfficacyHandler;

// Reset all discoveries
handler.ClearAllDiscoveries();

// Or with confirmation
int discoveryCount = handler.GetDiscoveryCount();
if (discoveryCount > 0)
{
    Debug.Log($"Clearing {discoveryCount} discoveries...");
    handler.ClearAllDiscoveries();
}
```

### Settings UI: Discovery Mode Toggle Example
```csharp
// Example settings menu implementation
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Toggle discoveryModeToggle;

    private void Start()
    {
        var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
        if (handler != null)
        {
            discoveryModeToggle.isOn = handler.DiscoveryModeEnabled;
            discoveryModeToggle.onValueChanged.AddListener(OnDiscoveryModeChanged);
        }
    }

    private void OnDiscoveryModeChanged(bool enabled)
    {
        var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
        if (handler != null)
            handler.DiscoveryModeEnabled = enabled;
    }

    public void OnResetDiscoveriesClicked()
    {
        var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
        if (handler != null)
        {
            // Show confirmation dialog first
            if (ShowConfirmationDialog("Reset all discovery progress?"))
                handler.ClearAllDiscoveries();
        }
    }
}
```

### Developer: Pre-Discovering Combinations (Tutorial)
```csharp
// Future enhancement - not yet implemented
var handler = CardGameMaster.Instance.treatmentEfficacyHandler;
handler.PreDiscoverCombination("Soapy Water", "Aphids");
```

### Player: Discovery Workflow
1. Draw treatment card (e.g., Permethrin Spray)
2. Hover over plant with Spider Mites → See "?"
3. Apply treatment → Spider Mites reduced by 85%
4. Hover again → See "85%" (green)
5. Save game → Discovery persists
6. Load game → Still shows "85%"

## Testing

### Manual Testing Checklist
- [ ] New game shows "?" for all combinations (discovery mode enabled)
- [ ] Applying treatment reveals percentage
- [ ] Different afflictions track separately
- [ ] Save/load preserves discoveries and toggle state
- [ ] Multiple afflictions show "?" if any undiscovered
- [ ] Analytics events fire on first discovery
- [ ] Old saves load without errors (defaults to enabled)
- [ ] **Toggle OFF**: All percentages visible immediately
- [ ] **Toggle ON**: "?" shows for undiscovered combinations
- [ ] **Clear**: All discoveries reset, need to rediscover
- [ ] Discovery count updates correctly

### Automated Tests
**File:** `PlayModeTest/EfficacyDiscoveryTests.cs`

Key test cases:
- Discovery marking on first use
- Preview mode doesn't mark discovery
- Display shows "?" vs percentage correctly
- Save/load roundtrip
- Backward compatibility

## Future Enhancements

### Planned Features
1. **Discovery Compendium**: Dedicated UI screen showing all discoveries
2. **Progressive Discovery**: Show "~X%" for partially discovered multi-affliction scenarios
3. **Tutorial Pre-Discovery**: Auto-reveal specific combinations in tutorial
4. **Discovery Achievements**: Unlock rewards for discovering X combinations
5. **"New!" Badge**: Visual indicator for recently discovered combinations

### Performance Optimizations
- String key caching for high-frequency lookups
- Lazy initialization of HashSet
- Discovery event batching for analytics

## Troubleshooting

### Issue: Discovery Progress Lost
**Symptoms:** Player re-enters play mode and discoveries are reset

**Cause:** Unity Editor domain reload in specific play mode settings

**Solution:** Discovery data persists via `GameStateData` save/load, not Unity serialization. Progress is maintained across game sessions through PlayerPrefs.

### Issue: Mod Combinations Not Discovering
**Symptoms:** Mod-loaded treatment doesn't mark as discovered when applied

**Cause:** Name mismatch between `ITreatment.Name` and internal tracking

**Solution:** Verify mod JSON `name` field exactly matches runtime `Name` property (case-sensitive)

### Issue: "?" Shows Yellow, Same as 50-74% Range
**Symptoms:** Player confuses undiscovered "?" with medium efficacy percentages

**Design Decision:** Intentional - yellow indicates "uncertain" or "moderate" in both cases. Consider changing "?" to white/gray in future if player feedback indicates confusion.

## Related Documentation

- [Analytics System](analytics-system.md) - Discovery event tracking
- [Mod Guide](mod-guide.md) - Constraints for mod creators
- [Game State System](game-state-system-documentation.md) - Save/load architecture
- [Card Core System](card-core-system.md) - Treatment application flow

## Changelog

### v1.0 (2026-01-26)
- Initial implementation
- Core discovery tracking with HashSet
- UI display logic for "?" vs percentages
- Save/load persistence
- Analytics integration
- Backward compatibility for old saves

# Hover System Documentation Enhancement Suggestions

This document contains optional improvements to the hover system documentation. These are not critical gaps but would enhance completeness.

## 1. Mod Developer Guidance

**Location**: Add after "Usage Examples" section in card-core-system.md

**Suggested Content**:

```markdown
### For Mod Developers: Using Hover Events

If you're creating custom cards or card holders through the mod system, you can leverage the hover event system for custom behaviors:

#### Subscribing to Hover Events in Custom Cards

```csharp
// In your custom mod card prefab script
public class ModCustomCard : MonoBehaviour
{
    private Click3D _click3D;

    private void Start()
    {
        _click3D = GetComponent<Click3D>();
        if (_click3D != null)
        {
            _click3D.HoverEntered += OnCustomHoverEnter;
            _click3D.HoverExited += OnCustomHoverExit;
        }
    }

    private void OnCustomHoverEnter(Click3D click3D)
    {
        // Custom mod behavior (e.g., show additional info, play sound)
    }

    private void OnCustomHoverExit(Click3D click3D)
    {
        // Cleanup custom behavior
    }

    private void OnDestroy()
    {
        if (_click3D != null)
        {
            _click3D.HoverEntered -= OnCustomHoverEnter;
            _click3D.HoverExited -= OnCustomHoverExit;
        }
    }
}
```

#### Best Practices for Mod Hover Behaviors

- **Always unsubscribe** in OnDestroy to prevent memory leaks
- **Check for null** before subscribing (Click3D may be disabled)
- **Keep hover logic lightweight** to avoid framerate drops
- **Test on multiple platforms** (remember hover doesn't work on touch devices)
- **Use event parameter** to access Click3D properties (handItem, isSticker, etc.)

#### Platform Compatibility

If your mod needs to work on both desktop and mobile:
- Provide alternative UI for touch platforms (buttons, taps)
- Use `Application.isMobilePlatform` to conditionally enable hover features
- Consider using `onClick3D` as a fallback for touch interactions
```

## 2. Performance Considerations

**Location**: Add to "Hover Preview System" subsection (after line 436)

**Suggested Content**:

```markdown
**Performance Notes**:
- Preview creation involves instantiating a GameObject and modifying materials
- Material modifications are cleaned up when preview is cleared to prevent memory leaks
- For cards with many child renderers, preview creation may have a small framerate cost
- Preview updates are event-driven (not per-frame), minimizing overhead
- Consider disabling hover preview on very low-end hardware via `enableHoverPreview = false`
```

## 3. Test Examples

**Location**: Add to "Testing" section (after line 792)

**Suggested Content**:

```markdown
### Hover System Testing

Test hover events and preview behavior:

```csharp
[UnityTest]
public IEnumerator Click3D_HoverEvents_ShouldFireCorrectly()
{
    // Arrange
    var cardHolder = Object.FindFirstObjectByType<PlacedCardHolder>();
    var click3D = cardHolder.GetComponentInChildren<Click3D>();
    bool hoverEntered = false;
    bool hoverExited = false;

    click3D.HoverEntered += (c) => hoverEntered = true;
    click3D.HoverExited += (c) => hoverExited = true;

    // Act
    click3D.OnMouseEnter(); // Simulate hover enter
    yield return null;
    click3D.OnMouseExit(); // Simulate hover exit
    yield return null;

    // Assert
    Assert.IsTrue(hoverEntered, "HoverEntered event should have fired");
    Assert.IsTrue(hoverExited, "HoverExited event should have fired");
}

[UnityTest]
public IEnumerator HoverPreview_ShouldDisplayWhenCardSelected()
{
    // Arrange
    var deckManager = CardGameMaster.Instance.deckManager;
    var cardHolder = Object.FindFirstObjectByType<PlacedCardHolder>();
    deckManager.DrawActionHand();
    var firstCard = deckManager.GetActionHand()[0];

    // Act
    deckManager.SelectCard(firstCard);
    cardHolder.OnHolderHoverEnter(cardHolder.GetComponent<Click3D>());
    yield return null;

    // Assert
    Assert.IsNotNull(cardHolder._previewCardClone, "Preview should be created on hover");

    // Cleanup
    cardHolder.OnHolderHoverExit(cardHolder.GetComponent<Click3D>());
}

[Test]
public void HoverPreview_ShouldClearWhenCardDeselected()
{
    // Arrange
    var deckManager = CardGameMaster.Instance.deckManager;
    var cardHolder = Object.FindFirstObjectByType<PlacedCardHolder>();

    // Act - Select and deselect
    deckManager.SelectedCardChanged?.Invoke(someCard);
    deckManager.SelectedCardChanged?.Invoke(null);

    // Assert
    Assert.IsNull(cardHolder._previewCardClone, "Preview should be cleared");
}
```
```

## 4. Event Flow Diagram

**Location**: Add to "Hover Preview System" subsection (after technical implementation list)

**Suggested Content**:

```markdown
**Event Flow**:
```
Player selects card → DeckManager.SelectedCardChanged event
                   ↓
       PlacedCardHolder.OnSelectedCardChanged()
                   ↓
       (if hovered) UpdatePreview()

Player hovers holder → Click3D.HoverEntered event
                    ↓
        PlacedCardHolder.OnHolderHoverEnter()
                    ↓
        (if card selected) UpdatePreview()

Player exits hover → Click3D.HoverExited event
                  ↓
      PlacedCardHolder.OnHolderHoverExit()
                  ↓
      ClearPreview()
```
```

## Priority Assessment

- **Mod Developer Guidance**: Medium (useful for extensibility)
- **Performance Notes**: Low (system is already optimized)
- **Test Examples**: Low (nice-to-have for regression prevention)
- **Event Flow Diagram**: Low (clarifies but not essential)

## Implementation Decision

These enhancements are **optional**. The existing documentation is complete and sufficient for all primary use cases. Implement these only if:
1. Mod developers request more guidance
2. Performance questions arise during playtesting
3. Additional test coverage is needed for CI/CD
4. Visual learners request diagrams

---

*Created by: documentation-engineer*
*Date: 2025-12-11*

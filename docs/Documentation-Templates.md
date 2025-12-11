# Documentation Templates

**Templates for creating consistent documentation pages in the Horticulture wiki.**

---

## 📄 System Documentation Template

```markdown
# [System Name] System

**Brief description of what this system does and why it exists.**

## Overview

High-level explanation of the system's purpose and role in the game.

## Architecture

### Components
- **Component 1** - What it does
- **Component 2** - What it does
- **Component 3** - What it does

### Data Flow
```
User Action
    ↓
Component 1
    ↓
Component 2
    ↓
Result
```

## Key Features

### Feature 1
Description and usage

### Feature 2
Description and usage

## API Reference

### Key Classes

#### ClassName
```csharp
public class ClassName : MonoBehaviour
{
    public void KeyMethod() { }
}
```

**Properties:**
- `Property1` - Description
- `Property2` - Description

**Methods:**
- `Method1()` - Description
- `Method2()` - Description

## Usage Examples

### Example 1: [Task Description]
```csharp
// Code example
var system = SystemManager.Instance;
system.DoSomething();
```

### Example 2: [Task Description]
```csharp
// Code example
```

## Integration Points

### With System A
How this system integrates with System A

### With System B
How this system integrates with System B

## Common Issues

### Issue 1
**Problem:** Description
**Solution:** How to fix it

### Issue 2
**Problem:** Description
**Solution:** How to fix it

## Testing

### Unit Tests
Example test structure

### Integration Tests
Example integration test

## Related Documentation

- [[Related Page 1]]
- [[Related Page 2]]
- [[api-reference|API Reference]]

---

*Last Updated: [Date]*
```

---

## 🔧 Feature Documentation Template

```markdown
# [Feature Name] Feature

**One-line description of the feature.**

## Purpose

Why this feature exists and what problem it solves.

## User-Facing Behavior

How users interact with this feature.

### Use Cases
1. Use case 1
2. Use case 2
3. Use case 3

## Technical Implementation

### Architecture
How the feature is implemented technically.

### Key Components
- **Component 1** - Role
- **Component 2** - Role

### Data Structures
```csharp
public class FeatureData
{
    public int value;
    public string state;
}
```

## Code Examples

### Basic Usage
```csharp
// Example code
```

### Advanced Usage
```csharp
// Example code
```

## Configuration

### Inspector Settings
- Setting 1: Description
- Setting 2: Description

### Code Configuration
```csharp
// Configuration example
```

## Testing

### Manual Testing Steps
1. Step 1
2. Step 2
3. Expected result

### Automated Tests
```csharp
[Test]
public void Feature_Behavior_ExpectedResult()
{
    // Test code
}
```

## Known Issues

### Issue 1
Description and workaround

## Future Enhancements

### Planned Improvement 1
Description

### Planned Improvement 2
Description

## Related Documentation

- [[Related System]]
- [[Related Feature]]

---

*Feature added in version: X.X*
*Last updated: [Date]*
```

---

## 🐛 Bug Report Template

```markdown
# [Bug Title]

## Description
Clear description of the bug.

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Impact
- **Severity**: Critical / High / Medium / Low
- **Frequency**: Always / Often / Sometimes / Rarely
- **Systems Affected**: List systems

## Environment
- Unity Version: 
- Platform: 
- Build Type: Editor / Development / Release

## Error Messages
```
Error message text
Stack trace
```

## Screenshots
[Attach if relevant]

## Root Cause (if known)
Analysis of what's causing the bug

## Proposed Solution
How to fix it

## Workaround (if available)
Temporary fix for users

## Related Issues
- Related bug #1
- Related bug #2

---

*Reported by: [Name]*
*Date: [Date]*
*Status: Open / In Progress / Fixed / Won't Fix*
```

---

## 📋 API Documentation Template

```markdown
# [API Name] API Reference

## Overview
Brief description of the API's purpose.

## Class: ClassName

### Description
What this class does.

### Inheritance
```csharp
public class ClassName : BaseClass, IInterface
```

### Properties

#### PropertyName
```csharp
public Type PropertyName { get; set; }
```
**Description:** What this property represents

**Default Value:** Default

**Example:**
```csharp
var value = instance.PropertyName;
```

### Methods

#### MethodName
```csharp
public ReturnType MethodName(ParamType param)
```

**Description:** What this method does

**Parameters:**
- `param` (ParamType) - Parameter description

**Returns:**
- `ReturnType` - Return value description

**Exceptions:**
- `ExceptionType` - When thrown

**Example:**
```csharp
var result = instance.MethodName(param);
```

**See Also:**
- [[Related API]]
- [[Related System]]

### Events

#### EventName
```csharp
public static event Action<ParamType> EventName;
```

**Description:** When this event fires

**Parameters:**
- `param` (ParamType) - Event data

**Example:**
```csharp
ClassName.EventName += HandleEvent;

void HandleEvent(ParamType param)
{
    // Handle event
}
```

## Usage Examples

### Common Use Case 1
```csharp
// Complete example
```

### Common Use Case 2
```csharp
// Complete example
```

## Best Practices

1. Practice 1
2. Practice 2
3. Practice 3

## Common Pitfalls

### Pitfall 1
**Problem:** Description
**Solution:** How to avoid

## Related APIs

- [[Related API 1]]
- [[Related API 2]]

---

*API Version: X.X*
```

---

## 📝 Workflow Guide Template

```markdown
# [Workflow Name] Workflow

**Quick description of what this workflow accomplishes.**

## When to Use

Scenarios where this workflow applies.

## Prerequisites

- Requirement 1
- Requirement 2
- Required knowledge

## Step-by-Step Guide

### Step 1: [Step Name]

**Goal:** What you're trying to achieve

**Actions:**
1. Action 1
2. Action 2

**Code Example:**
```csharp
// Code for this step
```

**Expected Result:** What should happen

### Step 2: [Step Name]

**Goal:** Next objective

**Actions:**
1. Action 1
2. Action 2

**Code Example:**
```csharp
// Code for this step
```

**Expected Result:** What should happen

### Step 3: [Step Name]

[Continue pattern...]

## Verification

How to verify the workflow completed successfully:

- [ ] Check 1
- [ ] Check 2
- [ ] Check 3

## Common Issues

### Issue 1
**Problem:** Description
**Solution:** Fix

### Issue 2
**Problem:** Description
**Solution:** Fix

## Tips & Best Practices

- Tip 1
- Tip 2
- Tip 3

## Related Workflows

- [[Related Workflow 1]]
- [[Related Workflow 2]]

## Related Documentation

- [[Related System]]
- [[API Reference]]

---

*Workflow difficulty: Beginner / Intermediate / Advanced*
```

---

## 🧪 Test Documentation Template

```markdown
# [Test Suite Name] Tests

## Overview
What these tests verify.

## Test Coverage

### Unit Tests
- Component 1 behavior
- Component 2 behavior
- Edge cases

### Integration Tests
- System A + System B interaction
- Complete workflow tests

### Manual Tests
- User-facing behavior
- Visual verification

## Test Setup

### Dependencies
- Required components
- Test data
- Mock objects

### Setup Code
```csharp
[SetUp]
public void Setup()
{
    // Setup code
}

[TearDown]
public void TearDown()
{
    // Cleanup code
}
```

## Test Cases

### Test Case 1: [Description]

**Category:** Unit / Integration / Manual

**Purpose:** What this test verifies

**Test Code:**
```csharp
[Test]
public void Method_Scenario_ExpectedBehavior()
{
    // Arrange
    
    // Act
    
    // Assert
}
```

**Expected Result:** What should pass

### Test Case 2: [Description]

[Continue pattern...]

## Running the Tests

### From Unity Editor
1. Open Test Runner
2. Select test category
3. Click Run All

### From Command Line
```bash
Unity -batchmode -runTests -testPlatform PlayMode
```

## Test Results

### Coverage Metrics
- Lines covered: X%
- Branches covered: Y%

### Known Issues
- Test that occasionally fails
- Platform-specific behavior

## Related Documentation

- [[testing-guide|Testing Guide]]
- [[System Being Tested]]

---

*Test suite last updated: [Date]*
```

---

## 📖 How to Use Templates

### Creating New Documentation

1. **Copy the relevant template**
2. **Replace all [placeholders]** with actual content
3. **Remove sections** that don't apply
4. **Add sections** if needed
5. **Link to related pages** using `[[Page Name]]`
6. **Update the date** at the bottom

### Template Selection Guide

| Documentation Type | Use Template |
|-------------------|--------------|
| New game system | System Documentation |
| New gameplay feature | Feature Documentation |
| Bug tracking | Bug Report |
| API additions | API Documentation |
| Development process | Workflow Guide |
| New test suite | Test Documentation |

### Best Practices

1. **Be Consistent** - Follow the template structure
2. **Be Complete** - Fill in all relevant sections
3. **Be Concise** - Keep it focused and clear
4. **Link Liberally** - Connect related documentation
5. **Update Regularly** - Keep documentation current
6. **Add Examples** - Show, don't just tell
7. **Include Context** - Explain the "why", not just "how"

---

## 🔗 Related Pages

- [[Code-Standards|Code Standards]]
- [[Common-Workflows|Common Workflows]]
- [[developer-onboarding|Developer Onboarding]]

---

*Use these templates to maintain consistent, high-quality documentation!*

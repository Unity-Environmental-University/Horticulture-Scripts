# Analytics System Documentation

## Overview

The Horticulture game includes comprehensive analytics tracking to measure player performance, educational outcomes, and game balance. All analytics are implemented through Unity Analytics with custom event definitions.

## Architecture

### Core Components

- **AnalyticsEvents.cs**: Event class definitions for Unity Analytics
- **AnalyticsFunctions.cs**: Static helper methods for recording events
- **Integration Points**: Called from TurnController, DeckManager, and PlantController

## Event Types

### 1. Round Events

#### RoundStartEvent
Fired at the beginning of each game round.

**Parameters:**
- `currentRound`: Round number
- `plantsCount`: Number of plants placed
- `currentScore`: Player's money at round start
- `moneyGoal`: Target money to reach
- `isTutorial`: Whether this is a tutorial round

**Usage:**
```csharp
AnalyticsFunctions.RecordRoundStart(
    currentRound: 1,
    plantsCount: 4,
    score: 20,
    goal: 500,
    isTutorial: false
);
```

#### RoundEndEvent
Fired when a round completes (all plants healthy/dead or early end).

**Parameters:**
- `currentRound`: Round number
- `totalTurns`: Number of turns taken in the round
- `finalScore`: Player's final money
- `scoreDelta`: Change in score (+/- from round start)
- `plantsHealthy`: Count of plants with no afflictions
- `plantsDead`: Count of dead plants
- `roundWon`: **Performance Metric** - Did player make profit? (scoreDelta > 0)
- `roundVictory`: **Success Metric** - Did player reach the money goal? (non-tutorial only)

**Important Distinction: roundWon vs roundVictory**

These two parameters serve different analytical purposes:

| Metric | Purpose | When True | Example |
|--------|---------|-----------|---------|
| `roundWon` | Performance indicator | Score improved (profit) | Round 1: +$50, goal=$500 → true |
| `roundVictory` | Success indicator | Goal reached (non-tutorial) | Round 5: $520, goal=$500 → true |

**Scenarios:**
```csharp
// Scenario 1: Profitable early round (no victory yet)
RecordRoundEnd(
    round: 2, turns: 4, finalScore: 150, scoreDelta: 50,
    plantsHealthy: 3, plantsDead: 0,
    roundWon: true,      // Made $50 profit
    roundVictory: false  // Goal is $500, only at $150
);

// Scenario 2: Victory achieved
RecordRoundEnd(
    round: 5, turns: 4, finalScore: 520, scoreDelta: 70,
    plantsHealthy: 4, plantsDead: 0,
    roundWon: true,     // Made $70 profit
    roundVictory: true  // Reached $500 goal!
);

// Scenario 3: Tutorial round (positive score)
RecordRoundEnd(
    round: 1, turns: 1, finalScore: 15, scoreDelta: 10,
    plantsHealthy: 1, plantsDead: 0,
    roundWon: true,      // Made profit in tutorial
    roundVictory: false  // Tutorial doesn't count as victory
);

// Scenario 4: Losing round
RecordRoundEnd(
    round: 3, turns: 4, finalScore: 120, scoreDelta: -30,
    plantsHealthy: 1, plantsDead: 2,
    roundWon: false,     // Lost money
    roundVictory: false  // Didn't reach goal
);
```

### 2. Turn Events

#### TurnStartEvent
Fired at the start of each turn.

**Parameters:**
- `currentRound`: Round number
- `currentTurn`: Turn number within round
- `cardsDrawn`: Number of cards drawn this turn
- `currentScore`: Player's current money
- `plantsWithAfflictions`: Count of afflicted plants

#### TurnEndEvent
Fired when a turn completes (player clicks "End Turn").

**Parameters:**
- `currentRound`: Round number
- `currentTurn`: Turn number
- `currentScore`: Player's current money

### 3. Treatment Events

#### TreatmentAppliedEvent
Fired when a player applies a treatment card to a plant.

**Parameters:**
- `plantName`: Type of plant treated
- `treatmentName`: Treatment card used
- `afflictionName`: Target affliction
- `treatmentSuccess`: Whether treatment was effective

#### AfflictionAppliedEvent
Fired when an affliction is added to a plant.

**Parameters:**
- `plantName`: Plant type
- `afflictionName`: Affliction type
- `currentRound`: Round number
- `currentTurn`: Turn number

### 4. Player Action Events

#### RedrawHandEvent
Fired when a player attempts to redraw their hand (both successful and blocked attempts).

**Parameters:**
- `cardsDrawn`: Comma-separated list of card names drawn (or "N/A" if blocked)
- `cardsDiscarded`: Comma-separated list of card names discarded (or "N/A" if blocked)
- `currentScore`: Player's money at time of redraw attempt
- `currentRound`: Round number
- `currentTurn`: Turn number
- `success`: Whether the redraw completed successfully (boolean)
- `blockReason`: Why the redraw was blocked (empty string if successful)

**Block Reasons:**
- `"Animation in progress"`: Card display animation is still running
- `"Cards in holders"`: Player has cards placed on plants

**Usage Examples:**

```csharp
// Successful redraw
AnalyticsFunctions.RecordRedraw(
    discarded: "HorticulturalOil,Fungicide,Panacea,InsecticideTreatment",
    drawn: "SoapyWater,Fungicide,HorticulturalOil,Panacea",
    score: 45,
    round: 3,
    turn: 2,
    success: true,
    blockReason: ""
);

// Blocked attempt - animation in progress
AnalyticsFunctions.RecordRedraw(
    discarded: "N/A",
    drawn: "N/A",
    score: 45,
    round: 3,
    turn: 2,
    success: false,
    blockReason: "Animation in progress"
);

// Blocked attempt - cards in holders
AnalyticsFunctions.RecordRedraw(
    discarded: "N/A",
    drawn: "N/A",
    score: 50,
    round: 2,
    turn: 4,
    success: false,
    blockReason: "Cards in holders"
);
```

**Data Analysis Use Cases:**

**UI/UX Improvements:**
- Filter `success=false` events to identify user frustration points
- Track `blockReason="Animation in progress"` frequency to optimize animation timing
- Analyze correlation between `blockReason="Cards in holders"` and tutorial completion

**Player Behavior:**
- Redraw frequency by round/turn indicates strategic complexity
- Compare redraw patterns between successful and struggling players
- Identify rounds where players frequently attempt blocked redraws

**Game Balance:**
- Redraw cost ($3) effectiveness analysis
- Card composition analysis from `cardsDiscarded` patterns
- Strategic card cycling patterns from `cardsDrawn` data

**Example Queries:**

```sql
-- Find players with high blocked redraw attempts (UI confusion indicator)
SELECT player_id, COUNT(*) as blocked_attempts
FROM redraw_hand_events
WHERE success = false
GROUP BY player_id
HAVING COUNT(*) > 5;

-- Analyze most common block reason
SELECT blockReason, COUNT(*) as occurrences
FROM redraw_hand_events
WHERE success = false
GROUP BY blockReason;

-- Redraw success rate by round (difficulty indicator)
SELECT currentRound,
       AVG(CAST(success AS INT)) as success_rate,
       COUNT(*) as total_attempts
FROM redraw_hand_events
GROUP BY currentRound
ORDER BY currentRound;
```

## Implementation Guidelines

### Adding Analytics Calls

1. **Null Check**: Always check if AnalyticsService is available
2. **Try-Catch**: Wrap analytics calls in try-catch to prevent gameplay disruption
3. **Performance**: Analytics calls should not block game logic

**Example:**
```csharp
try
{
    AnalyticsFunctions.RecordRoundEnd(/* parameters */);
}
catch (Exception ex)
{
    Debug.LogWarning($"[Analytics] RecordRoundEnd error: {ex.Message}");
}
```

### Testing Analytics

Analytics can be tested in Unity Editor by:
1. Enabling Unity Analytics in Project Settings
2. Using debug mode to view event payloads
3. Checking Unity Dashboard for event delivery

## Data Analysis Use Cases

### Player Progression Analysis
- Track `roundVictory` true events to measure completion rates
- Analyze rounds-to-victory averages
- Identify difficulty spikes by round number

### Performance Metrics
- `roundWon` percentage indicates player skill improvement
- Compare `roundWon` vs `roundVictory` rates to gauge difficulty curve
- Track `scoreDelta` trends for economy balance

### Educational Outcomes
- `treatmentSuccess` rates measure learning effectiveness
- Correlate treatment choices with plant/affliction combinations
- Track progression from tutorial to normal gameplay

### Game Balance
- Plant health statistics reveal difficulty tuning needs
- Turn counts per round indicate pacing issues
- Affliction spread patterns inform pest mechanics

## Privacy & Compliance

All analytics data is anonymized and handled according to Unity Analytics terms of service. No personally identifiable information is collected.

## Future Enhancements

Potential analytics additions:
- Shop purchase patterns
- Card deck composition evolution
- First-time user experience (FTUE) metrics
- Level progression timing
- Treatment cost vs benefit analysis

---

**Last Updated:** 2025-10-22
**Maintainer:** Horticulture Development Team

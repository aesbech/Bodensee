# Future Work Roadmap

This document outlines planned improvements and features that are not yet implemented but have been designed and documented.

---

## **Phase 1: Attraction Editor (3-4 hours)** 🔴 NOT IMPLEMENTED

### **Current Status**
- ✅ UI exists (`UI/AttractionEditorWindow.xaml`)
- ❌ No code-behind file
- ❌ All handlers undefined
- ❌ Cannot be used

### **What Needs to Be Done**

#### **1. Create Code-Behind File**
**File:** `UI/AttractionEditorWindow.xaml.cs`

**Required Classes:**
```csharp
public partial class AttractionEditorWindow : Window
{
    private ObservableCollection<Attraction> _attractions;
    private GameSettings _settings;

    public List<Attraction> Attractions { get; private set; }

    public AttractionEditorWindow(List<Attraction> currentAttractions)
    {
        InitializeComponent();
        _attractions = new ObservableCollection<Attraction>(currentAttractions);
        // Setup data binding
    }
}
```

#### **2. Implement Required Handlers**

**File Operations:**
- `LoadFromCSV_Click` - Import attractions from CSV
- `SaveToCSV_Click` - Export attractions to CSV
- `ResetToDefaults_Click` - Reload from Setup/GameSetup.cs defaults
- `Close_Click` - Close window

**Add Attraction:**
- `AddNature_Click` - Add new green attraction
- `AddWater_Click` - Add new blue attraction
- `AddCulture_Click` - Add new red attraction
- `AddGastronomy_Click` - Add new yellow attraction
- `AddGray_Click` - Add new gray attraction

**Filtering:**
- `FilterChanged` - Filter list by category

**Edit Panel:**
- Edit existing attraction properties
- Delete attraction
- Change priority
- Set bonus euro flag
- Set all-day action

#### **3. CSV Format Support**

**Expected Format:**
```csv
Category,EnglishName,GermanName,Value,Priority,BonusEuro,AllDayAction
Nature,Bicycle Path,Radweg,1,1,False,
Nature,Wine Path,Weinpfad,2,9,True,
Gray,Hotel,Hotel,3,1,False,GiveTour
```

**CSV Parser:**
```csharp
public static List<Attraction> LoadFromCSV(string filePath)
{
    var attractions = new List<Attraction>();
    var lines = File.ReadAllLines(filePath).Skip(1); // Skip header

    foreach (var line in lines)
    {
        var parts = line.Split(',');
        var attraction = new Attraction
        {
            Category = Enum.Parse<AttractionCategory>(parts[0]),
            EnglishName = parts[1],
            GermanName = parts[2],
            Value = int.Parse(parts[3]),
            Priority = int.Parse(parts[4]),
            BonusEuro = bool.Parse(parts[5]),
            AllDayEffect = string.IsNullOrEmpty(parts[6])
                ? null
                : Enum.Parse<AllDayAction>(parts[6])
        };
        attractions.Add(attraction);
    }

    return attractions;
}
```

#### **4. Testing Checklist**
- [ ] Load attractions from CSV
- [ ] Save attractions to CSV
- [ ] Add new attraction of each category
- [ ] Edit attraction properties
- [ ] Delete attraction
- [ ] Filter by category
- [ ] Reset to defaults
- [ ] Verify MainWindow integration

---

## **Phase 2: City Editor (6-8 hours)** 🔴 DOES NOT EXIST

### **Current Status**
- ❌ No UI exists
- ❌ All city data hardcoded in Setup/GameSetup.cs:57-116
- ❌ No visual editor

### **What Needs to Be Done**

#### **1. Create UI Files**

**File:** `UI/CityEditorWindow.xaml`

**Required UI Components:**
- List of all 20 cities
- Edit panel for selected city:
  - City name (read-only)
  - Is Port (checkbox)
  - Can Build Water (checkbox)
  - Max Attraction Spaces (1-5)
  - Morning Action (dropdown)
  - All-Day Action (dropdown)
- Connections editor:
  - List of connected cities
  - Add/Remove connections
- Save/Load from JSON
- Reset to defaults

#### **2. Create Code-Behind**

**File:** `UI/CityEditorWindow.xaml.cs`

```csharp
public partial class CityEditorWindow : Window
{
    private ObservableCollection<CityData> _cities;

    public class CityData
    {
        public string Name { get; set; }
        public bool IsPort { get; set; }
        public bool CanBuildWater { get; set; }
        public int MaxAttractionSpaces { get; set; }
        public MorningAction? MorningAction { get; set; }
        public AllDayAction? AllDayAction { get; set; }
        public List<string> Connections { get; set; }
    }

    public CityEditorWindow()
    {
        InitializeComponent();
        LoadCitiesFromSetup();
    }

    private void LoadCitiesFromSetup()
    {
        // Parse hardcoded city data from GameSetup.cs
    }

    private void SaveCitiesToJSON()
    {
        // Export to JSON format
    }
}
```

#### **3. JSON Format**

```json
{
  "cities": [
    {
      "name": "Konstanz",
      "isPort": true,
      "canBuildWater": true,
      "maxAttractionSpaces": 4,
      "morningAction": "Ferry",
      "allDayAction": "GiveTour",
      "connections": ["Radolfzelt", "Kreuzlingen"]
    }
  ]
}
```

#### **4. Integration with GameSetup**

Update `GameSetup.cs` to optionally load from JSON:
```csharp
public GameState CreateGame(
    List<(string Name, bool IsAI, string AIStrategy)> playerConfigs,
    GameSettings settings = null,
    string customCitiesPath = null)
{
    if (customCitiesPath != null)
        LoadCitiesFromJSON(customCitiesPath);
    else
        InitializeBoardDefault();

    // Rest of setup...
}
```

---

## **Phase 3: Advanced Settings (2-3 hours)** ⚠️ DESIGNED BUT NOT IMPLEMENTED

### **Recommended Additional Settings**

#### **1. Max Attraction Spaces**
**Current:** Hardcoded per city (3-5)
**Proposed:** Global defaults with city overrides

```csharp
// Add to GameSettings
public int DefaultMaxAttractionSpaces { get; set; } = 3;
public int PortMaxAttractionSpaces { get; set; } = 4;
```

**Implementation:**
```csharp
// In GameSetup.cs:131
MaxAttractionSpaces = city.IsPort ?
    state.Settings.PortMaxAttractionSpaces :
    state.Settings.DefaultMaxAttractionSpaces
```

**UI:** Add to "Game Balance Settings" GroupBox

#### **2. Port Category Rules**
**Current:** Ports count as all categories
**Proposed:** Make this configurable

```csharp
// Add to GameSettings
public bool PortBelongsToAllCategories { get; set; } = true;
```

**Implementation:**
```csharp
// In GameEngine.cs:297
if (city.IsPort && _state.Settings.PortBelongsToAllCategories)
    sameCategoryCount++; // Current behavior
```

**UI:** Add checkbox in "Game Balance Settings"

#### **3. Category Pricing Increment**
**Current:** +€1 per same-category attraction
**Proposed:** Configurable increment

```csharp
// Add to GameSettings
public int CategoryCountPricingIncrement { get; set; } = 1;
```

**Implementation:**
```csharp
// In GameEngine.cs:299
totalCost = baseCost + (sameCategoryCount * _state.Settings.CategoryCountPricingIncrement);
```

**UI:** Add to "Game Balance Settings"

#### **4. Gray Market Display Mode**
**Current:** Shows all gray attractions
**Proposed:** Option to show limited amount

```csharp
// Add to GameSettings
public enum GrayMarketMode
{
    ShowAll,        // Current behavior
    Limited         // Show MarketRefillAmount (e.g., 2)
}
public GrayMarketMode GrayDisplayMode { get; set; } = GrayMarketMode.ShowAll;
```

**Implementation:**
```csharp
// In Setup/GameSetup.cs:318-326
if (category == AttractionCategory.Gray)
{
    if (state.Settings.GrayDisplayMode == GameSettings.GrayMarketMode.ShowAll)
        state.Market.RefillGray();
    else
        state.Market.Refill(category, state.Settings.MarketRefillAmount);
}
```

**UI:** Add radio buttons in "Gray Attraction Settings"

#### **5. Starting Bus Cities**
**Current:** Fixed 4 cities (Bodman-Ludwigshafen, Friedrichshafen, Lindau, Rorschach)
**Proposed:** Configurable list

```csharp
// Add to GameSettings
public List<string> StartingBusCities { get; set; } = new List<string>
{
    "Bodman-Ludwigshafen",
    "Friedrichshafen",
    "Lindau",
    "Rorschach"
};
```

**Implementation:**
```csharp
// In GameSetup.cs:348
var startCities = state.Settings.StartingBusCities.ToArray();
```

**UI:** Multi-select dropdown or text input (comma-separated)

#### **6. Number of Buses**
**Current:** Always 4 buses
**Proposed:** Configurable 2-6 buses

```csharp
// Add to GameSettings
public int NumberOfBuses { get; set; } = 4;
```

**Implementation:**
```csharp
// In GameSetup.cs:350
for (int i = 0; i < Math.Min(state.Settings.NumberOfBuses, startCities.Length); i++)
```

**UI:** Numeric input (2-6 range)

---

## **Phase 4: Dice & Randomization Settings (1-2 hours)** 💡 OPTIONAL

### **Dice Range Configuration**

**Current:** 1-6 dice, reroll 1s
**Proposed:** Configurable dice behavior

```csharp
// Add to GameSettings
public int DiceMinValue { get; set; } = 1;
public int DiceMaxValue { get; set; } = 6;
public bool RerollOnes { get; set; } = true;
public int StartingDiceMinValue { get; set; } = 2; // Starting tourists get 2-6
```

**Implementation:**
```csharp
// In GameSetup.cs:372 and GameEngine.cs:363
int roll;
do
{
    roll = _random.Next(
        _state.Settings.StartingDiceMinValue,
        _state.Settings.DiceMaxValue + 1
    );
} while (_state.Settings.RerollOnes && roll == 1);
```

**Use Cases:**
- **D4 Mode:** Set min=1, max=4 for faster games
- **D8 Mode:** Set min=1, max=8 for longer games
- **No Reroll:** Set RerollOnes=false for harder difficulty
- **High Value:** Set min=3 for tourist-friendly games

---

## **Phase 5: Win Condition Settings (30 min)** 💡 OPTIONAL

### **Alternative Victory Conditions**

**Current:** Most money, tie-breaker = most attractions
**Proposed:** Multiple win conditions

```csharp
// Add to GameSettings
public enum WinCondition
{
    MostMoney,              // Current
    MostAttractions,        // Attractions built
    MostTouristsRuined,     // Aggressive play
    HighestNetProfit,       // Money earned - money spent
    MixedStrategy           // Points: €1 = 1pt, Attraction = 3pts
}
public WinCondition VictoryCondition { get; set; } = WinCondition.MostMoney;
```

**Implementation:**
```csharp
// In GameState.cs:398-420
public Player GetWinner()
{
    return _victoryCondition switch
    {
        WinCondition.MostMoney => Players.OrderByDescending(p => p.Money).First(),
        WinCondition.MostAttractions => /* ... */,
        WinCondition.MostTouristsRuined => /* ... */,
        // etc.
    };
}
```

---

## **Implementation Priority**

### **Must Have (Recommended Order)**
1. ✅ **3 Quick Win Settings** (DONE)
   - Tourist Pool Size ✅
   - Market Refill Amount ✅
   - Contractor Discount ✅

2. 🔴 **Attraction Editor** (Phase 1)
   - Most requested feature
   - Enables custom content without code changes

3. ⚠️ **Max Attraction Spaces** (Phase 3.1)
   - Affects game balance significantly
   - Easy to implement

### **Should Have**
4. ⚠️ **Port Category Rules** (Phase 3.2)
5. ⚠️ **Gray Market Display Mode** (Phase 3.4)
6. 🔴 **City Editor** (Phase 2)

### **Nice to Have**
7. 💡 **Category Pricing Increment** (Phase 3.3)
8. 💡 **Starting Bus Cities** (Phase 3.5)
9. 💡 **Number of Buses** (Phase 3.6)
10. 💡 **Dice Configuration** (Phase 4)
11. 💡 **Win Conditions** (Phase 5)

---

## **Testing Strategy**

After implementing each phase:

### **Unit Tests**
- Test settings save/load
- Test default values
- Test edge cases (0, negative, very large values)

### **Integration Tests**
- Test full game with modified settings
- Verify analytics capture settings correctly
- Test import/export roundtrip

### **Balance Tests**
Run 100 AI-only games with variations:
- 4 tourists per category (short game)
- 12 tourists per category (long game)
- Market refill = 1 (scarcity)
- Market refill = 4 (abundance)
- Contractor discount = 2€ (powerful)
- Contractor discount = 0€ (disabled)

**Metrics to Track:**
- Average game length (turns)
- Winner distribution by strategy
- Average money at end
- Attraction build rate

---

## **Documentation Updates Needed**

After implementing each phase:

1. Update `README.md` with new features
2. Update `docs/Implementation_Review.md`
3. Create user guides:
   - `docs/Attraction_Editor_User_Guide.md`
   - `docs/City_Editor_User_Guide.md`
   - `docs/Advanced_Settings_Guide.md`
4. Update `docs/AGENT_PROMPTS.md` with new architecture
5. Add screenshots to documentation

---

## **Phase 6: AI System Improvements (2-3 hours)** 🔴 PARTIALLY WORKING

### **Current Status**
- ✅ 4 AI strategies implemented (Aggressive, Defensive, Balanced, Opportunistic)
- ✅ AI decision making for bus selection, morning actions, movement
- ✅ AI auto-play integration in UI
- 🔴 **CRITICAL BUG**: Only BuildAttraction all-day action executes
- ❌ Missing Ferry action support
- ❌ No headless game runner for testing

### **Critical Fixes Needed**

#### **1. Fix All-Day Action Execution (30 min)** 🔴 CRITICAL

**Problem:**
```csharp
// In UI/MainWindow.xaml.cs AIPlayButton_Click (line 749)
// ONLY BuildAttraction is handled!
if (decision.AllDayAction == AllDayAction.BuildAttraction && ...)
```

**Missing handlers:**
- ❌ AddTwoPips
- ❌ RerollTourist
- ❌ GiveTour
- ❌ BuildAttractionDiscount (Contractor)
- ❌ All other all-day actions

**Solution:**
Add complete switch statement in `AIPlayButton_Click`:

```csharp
// All-day action execution
if (decision.AllDayAction.HasValue)
{
    var arrivalCity = _gameState.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);

    switch (decision.AllDayAction.Value)
    {
        case AllDayAction.BuildAttraction:
            // Existing code
            break;

        case AllDayAction.AddTwoPips:
            // Select random tourist and add pips
            var tourist = decision.SelectedBus.Tourists
                .OrderBy(t => _random.Next())
                .FirstOrDefault();
            if (tourist != null)
            {
                tourist.Money += 2;
                Log($"AI added 2 pips to tourist");
            }
            break;

        case AllDayAction.RerollTourist:
            // Reroll tourist with least money
            var poorTourist = decision.SelectedBus.Tourists
                .OrderBy(t => t.Money)
                .FirstOrDefault();
            if (poorTourist != null)
            {
                // Reroll logic
                Log($"AI rerolled tourist");
            }
            break;

        case AllDayAction.GiveTour:
            // Give immediate tour
            var extraTour = _engine.GiveBusTour(decision.SelectedBus, _currentTurnContext);
            Log($"AI gave extra tour: {extraTour.AttractionsVisited.Count} attractions");
            break;

        // Add remaining cases...
    }
}
```

#### **2. Add Ferry Action Support (30 min)**

**Problem:** AI strategies don't recognize Ferry as morning action option

**Solution:**
In AI strategies, add Ferry handling:

```csharp
// In each strategy's MakeDecision method
if (currentCity.MorningAction == MorningAction.Ferry && currentCity.IsPort)
{
    decision.MorningAction = MorningAction.Ferry;
    context.UsedMorningAction = MorningAction.Ferry;

    // Ferry allows movement to ANY port
    // Evaluate all ports instead of just connected cities
}
```

#### **3. Improve Tourist Preference Handling (30 min)**

**Problem:** AI doesn't fully consider which tourists can visit attractions

**Current code** in `EvaluateCity`:
```csharp
var eligibleTourists = bus.Tourists
    .Where(t => context.AllAttractionsAppeal || t.Category == attraction.Category)
    .Where(t => t.Money >= attraction.Value)
    .ToList();
```

**Enhancement needed:**
- Consider appeal system settings
- Factor in IgnoreFirstAppeal context
- Prioritize cities with matching tourist categories

### **Headless Game Runner (1-2 hours)** ❌ NOT IMPLEMENTED

Create `HeadlessGameRunner.cs` for automated testing:

```csharp
namespace BodenseeTourismus.Testing
{
    public class HeadlessGameRunner
    {
        private GameSettings _settings;
        private GameAnalytics _analytics;

        public HeadlessGameRunner(GameSettings settings = null)
        {
            _settings = settings ?? new GameSettings();
        }

        public GameResult RunGame(List<string> aiStrategies)
        {
            // Setup game with AI players only
            var playerConfigs = aiStrategies.Select((strategy, i) =>
                ($"AI-{strategy}", true, strategy)).ToList();

            var setup = new GameSetup();
            var state = setup.CreateGame(playerConfigs, _settings);
            var engine = new GameEngine(state);
            var aiController = new AIController(state.Random);

            // Run game loop
            while (!state.GameEnded)
            {
                var decision = aiController.GetAIDecision(state, engine,
                    state.CurrentPlayer.AIStrategy);

                // Execute decision
                ExecuteAIDecision(state, engine, decision);

                // End turn
                state.NextTurn();
            }

            return new GameResult
            {
                Winner = state.GetWinner(),
                TotalTurns = state.TurnNumber,
                FinalScores = state.Players.ToDictionary(p => p.Name, p => p.Money)
            };
        }

        public BatchResult RunBatch(int games, List<string> strategies)
        {
            var results = new List<GameResult>();

            for (int i = 0; i < games; i++)
            {
                results.Add(RunGame(strategies));
            }

            return new BatchResult
            {
                TotalGames = games,
                WinRates = CalculateWinRates(results),
                AverageTurns = results.Average(r => r.TotalTurns),
                AverageScores = CalculateAverageScores(results)
            };
        }
    }

    public class GameResult
    {
        public Player Winner { get; set; }
        public int TotalTurns { get; set; }
        public Dictionary<string, int> FinalScores { get; set; }
    }

    public class BatchResult
    {
        public int TotalGames { get; set; }
        public Dictionary<string, double> WinRates { get; set; }
        public double AverageTurns { get; set; }
        public Dictionary<string, double> AverageScores { get; set; }
    }
}
```

### **Testing Command-Line Tool (30 min)**

Create `Program.cs` for headless testing:

```csharp
// In a new Console project or Program.cs
class Program
{
    static void Main(string[] args)
    {
        var runner = new HeadlessGameRunner();

        Console.WriteLine("Running AI strategy comparison...");

        var strategies = new List<string>
        {
            "Aggressive", "Defensive", "Balanced", "Opportunistic"
        };

        var results = runner.RunBatch(100, strategies);

        Console.WriteLine($"\n=== Results from {results.TotalGames} games ===");
        Console.WriteLine("\nWin Rates:");
        foreach (var wr in results.WinRates)
        {
            Console.WriteLine($"  {wr.Key}: {wr.Value:P2}");
        }

        Console.WriteLine($"\nAverage Game Length: {results.AverageTurns:F1} turns");

        Console.WriteLine("\nAverage Scores:");
        foreach (var score in results.AverageScores)
        {
            Console.WriteLine($"  {score.Key}: €{score.Value:F1}");
        }
    }
}
```

### **Priority Order**

**Must Fix (Critical):**
1. ✅ Fix all-day action execution - **30 minutes**
   - Currently breaks AI gameplay

**Should Fix (High Priority):**
2. Add Ferry action support - **30 minutes**
   - Limits AI strategic options
3. Improve tourist preference handling - **30 minutes**
   - Makes AI smarter

**Nice to Have (Testing):**
4. Implement HeadlessGameRunner - **1-2 hours**
   - Enables automated testing
5. Create testing CLI tool - **30 minutes**
   - Strategy comparison and balance testing

**Total Time: ~2-3 hours for complete AI improvements**

---

## **Known Issues to Address**

### **Current Limitations**
1. No validation on tourist pool size (could set to 0)
2. No validation on market refill (could set to 100)
3. Settings not validated against game state
4. No warning when changing settings mid-game

### **Suggested Validations**

```csharp
// In SettingsWindow.xaml.cs ParseInt method
private int ParseInt(string text, string fieldName, int min = 0, int max = 100)
{
    if (int.TryParse(text, out int value) && value >= min && value <= max)
        return value;
    throw new Exception($"{fieldName} must be between {min} and {max}");
}
```

**Validation Rules:**
- Tourist Pool Size: 1-20 per category
- Market Refill Amount: 1-10
- Contractor Discount: 0-5
- Max Attraction Spaces: 1-10
- Number of Buses: 2-6

---

## **Contact & Contribution**

If you want to implement any of these features:

1. Start with Phase 1 (Attraction Editor) - highest value
2. Follow the implementation guidelines above
3. Test thoroughly with the testing strategy
4. Update documentation
5. Create a pull request with:
   - Feature description
   - Test results
   - Screenshots (for UI changes)
   - Updated documentation

**Estimated Total Time:**
- Phase 1: 3-4 hours
- Phase 2: 6-8 hours
- Phase 3: 2-3 hours
- Phase 4: 1-2 hours
- Phase 5: 30 minutes

**Total: ~13-18 hours** for complete implementation of all optional features.

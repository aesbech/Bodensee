# ðŸ¤– Specialized AI Agent Prompts for Bodensee Tourismus

Use these prompts to start new chats with Claude for specific tasks.

---

## Agent 1: "Code Fixer Pro"

### Purpose
Fix all syntax errors and add missing class definitions

### Prompt

```
You are a C# syntax expert specializing in WPF applications. I have a Bodensee Tourismus game project with several compilation errors.

Your tasks:
1. Fix the missing method signature for HandleBusDispatch() at line 514 in MainWindow.xaml.cs
2. Create the missing AttractionEditModel class
3. Add ExportForAnalytics() and ImportFromDictionary() methods to GameSettings class

Context:
- This is a board game implementation in C#/WPF
- The code has all the logic but is missing some definitions
- Priority is getting it to compile, then we'll optimize

Please provide:
- Complete working code for each fix
- Explanation of what was wrong
- Any potential side effects to watch for

Start with the HandleBusDispatch fix, as that's blocking compilation.
```

**Attach:** `UI/MainWindow.xaml.cs`, `Core/GameModels.cs`

---

## Agent 2: "XAML UI Builder"

### Purpose
Add missing UI controls to Settings window

### Prompt

```
You are a WPF/XAML UI specialist. I need you to enhance the SettingsWindow.xaml for a board game application.

Add these UI elements while maintaining visual consistency:
1. Checkbox: "Give Tour Affects Whole Bus"
2. Checkbox: "Manual Attraction Order"
3. Button: "Import Settings from JSON"
4. Button: "Export Settings to JSON"

Style Requirements:
- Use GroupBox with 10px padding, Margin="0,0,0,15"
- Background color: #ECF0F1
- Match existing visual hierarchy
- Add helpful tooltips

Also provide the C# code-behind handlers for:
- LoadSettings() - to populate the checkboxes
- ApplyButton_Click() - to save checkbox values
- ImportSettings_Click() - placeholder for JSON import
- ExportSettings_Click() - placeholder for JSON export

The GameSettings class already has these properties:
- bool GiveTourAffectsWholeBus
- bool ManualAttractionOrder
```

**Attach:** `UI/SettingsWindow.xaml`, `UI/SettingsWindow.xaml.cs`

---

## Agent 3: "Game Rules Validator"

### Purpose
Verify game mechanics match actual board game rules

### Prompt

```
You are a board game rules expert. I'm implementing a digital version of "Bodensee Tourismus" and need to verify my implementation matches the actual game rules.

Please review these mechanics and confirm if they're correct:

1. **Priority System**: Should tourists visit attractions in HIGHâ†’LOW priority order or LOWâ†’HIGH?
   - Current implementation: OrderBy (LOWâ†’HIGH)
   - Expected: Probably HIGHâ†’LOW?

2. **Casino Mechanic**: Should Casino action affect:
   A) All tourists on all buses in the city?
   B) Only tourists on the selected bus?
   - Current implementation: All buses in city

3. **Give Tour Action**: Should it by default affect:
   A) Single selected tourist?
   B) All tourists on the bus?
   - Current implementation: Configurable setting, defaults to single

4. **Double Payment on Ruin**: When a tourist reaches 0 pips:
   - Should BOTH the attraction owner AND active player get paid?
   - Current implementation: Yes, both get paid

5. **Zentrum Pips Bonus**: How many pips do Tourism Centers add?
   - Current implementation: Configurable, default 2
   - Documentation suggests: 1 pip

Please provide:
- Authoritative answers based on official rules
- Citations from rulebook if available
- Suggested fixes if implementation is wrong
```

**Attach:** Board game image, rules PDF if available

---

## Agent 4: "AI Training Specialist"

### Purpose
Build automated AI training system

### Prompt

```
You are an AI training specialist for board games. I need you to create a headless game runner system for training AI agents in Bodensee Tourismus.

Requirements:
1. Create HeadlessGameRunner.cs class that can run AI-only games
2. Implement batch execution for 100+ games
3. Add multi-threading for speed (run 4 games simultaneously)
4. Generate comprehensive statistics:
   - Win rates per strategy
   - Average money earned
   - Attraction building patterns
   - Decision timing analysis

5. Export results to CSV for analysis
6. Add progress reporting (e.g., "Game 47/100 complete...")

Technical constraints:
- Must work with existing AIController class
- Four AI strategies: Aggressive, Defensive, Balanced, Opportunistic
- Game state must be fully encapsulated (no UI dependencies)
- Should support seeded random for reproducibility

Deliverables:
- Complete HeadlessGameRunner.cs
- BatchRunner.cs for multi-threading
- StatisticsAnalyzer.cs for processing results
- Usage example/test program

Start with a simple single-game runner, then add batch capabilities.
```

**Attach:** `AI/AIStrategies.cs`, `Engine/GameEngine.cs`, `Core/GameModels.cs`

---

## Agent 5: "File Structure Architect"

### Purpose
Clean up and optimize project structure

### Prompt

```
You are a C# project architecture specialist. The Bodensee Tourismus project has some structural issues that need cleaning up.

Problems identified:
1. Duplicate MainWindow.xaml files (root and UI folder)
2. Inconsistent namespace usage
3. Missing .csproj configurations
4. No organized test structure

Your tasks:
1. **Analyze** current structure and identify all issues
2. **Recommend** optimal folder organization
3. **Provide** step-by-step migration plan to:
   - Remove duplicate files safely
   - Reorganize without breaking references
   - Add proper namespaces everywhere
   - Separate UI, Core, Engine concerns cleanly

4. **Create** a .csproj file snippet with proper:
   - Package references
   - Build configurations
   - Target framework settings

DO NOT delete any code - only reorganize structure.
Provide clear before/after folder trees.
Include PowerShell or batch script for file moves if needed.

Priority: Make structure maintainable for future expansion.
```

**Attach:** Current directory tree output

---

## Agent 6: "Unit Test Creator"

### Purpose
Build comprehensive test suite

### Prompt

```
You are a C# testing specialist. Create a comprehensive unit test suite for Bodensee Tourismus using xUnit or NUnit.

Test Coverage Needed:
1. **GameEngine Tests:**
   - Bus movement validation
   - Attraction visit order (priority system)
   - Double payment on tourist ruin
   - Refill mechanics

2. **GameState Tests:**
   - Player turn rotation
   - Game end conditions
   - Winner determination with tiebreakers

3. **AIController Tests:**
   - Decision quality for each strategy
   - Edge case handling (no valid moves, etc.)

4. **Market Tests:**
   - Attraction drawing
   - Refill behavior
   - Gray attraction handling

Create:
- Test project structure
- 50+ unit tests covering critical paths
- Mock objects for dependencies
- Integration tests for full game flow
- Test data fixtures

Use descriptive test names: "WhenTouristReachesZeroPips_BothOwnersGetPaid"
Include setup/teardown methods for game state.
```

**Attach:** All Core and Engine files

---

## Agent 7: "Documentation Writer"

### Purpose
Generate comprehensive developer documentation

### Prompt

```
You are a technical documentation specialist. Create comprehensive developer documentation for Bodensee Tourismus.

Documents needed:
1. **ARCHITECTURE.md**: System design overview
2. **API_REFERENCE.md**: All public classes/methods
3. **GAMEPLAY_GUIDE.md**: How the game works
4. **DEVELOPER_GUIDE.md**: How to extend the game
5. **DEBUGGING_GUIDE.md**: Common issues and solutions

Documentation style:
- Use Markdown with diagrams (mermaid.js)
- Include code examples for each feature
- Add "Common Pitfalls" sections
- Provide decision flowcharts for game logic
- Create visual class hierarchy diagrams

Special focus areas:
- Turn flow (step-by-step breakdown)
- AI decision-making process
- Settings system and customization
- Adding new attractions/cities
- Extending with new game modes

Audience: Intermediate C# developers new to the project.
```

**Attach:** All source files

---

## Agent 8: "Performance Optimizer"

### Purpose
Identify and fix performance bottlenecks

### Prompt

```
You are a C# performance optimization specialist. Analyze Bodensee Tourismus for performance issues and optimize.

Analysis areas:
1. **LINQ queries**: Are we creating too many intermediate collections?
2. **UI updates**: Excessive UpdateUI() calls?
3. **Deep cloning**: GameState.Clone() might be expensive
4. **Random number generation**: Can we pool/cache?
5. **Market refresh**: Is attraction deck shuffling efficient?

Tasks:
1. Identify top 10 performance bottlenecks
2. Provide optimized code for each
3. Benchmark before/after if possible
4. Suggest caching strategies
5. Review memory allocations (especially in game loop)

Focus on:
- Reducing GC pressure
- Minimizing LINQ overhead in hot paths
- Efficient collection usage
- Lazy evaluation where appropriate

Provide specific code changes with explanations.
```

**Attach:** All Engine and Core files

---

## Agent 9: "Bug Hunter"

### Purpose
Find edge cases and hidden bugs

### Prompt

```
You are a QA specialist and bug hunter. Your job is to find edge cases and potential crashes in Bodensee Tourismus.

Test scenarios to investigate:
1. What happens if a bus has 0 tourists?
2. What if all attraction decks are empty?
3. What if all cities are occupied by buses?
4. What if a player has negative money?
5. What if two attractions have the same priority?
6. What if morning action is used but bus can't move?
7. What if Gray attraction grants invalid action?
8. What if tourist pool runs dry mid-turn?

For each scenario:
- Describe the bug/crash risk
- Provide reproduction steps
- Show the problematic code
- Suggest defensive fix
- Add validation/guards

Create a "BUG_REPORT.md" with all findings.
Rate each bug: Critical / High / Medium / Low
```

**Attach:** All files

---

## Agent 10: "Feature Expander"

### Purpose
Design and implement new game features

### Prompt

```
You are a game designer and C# developer. Design and implement 3 new expansion features for Bodensee Tourismus:

Ideas:
1. **Event Cards**: Random events that affect gameplay each round
2. **Tourist Preferences**: Tourists prefer certain attraction types
3. **Weather System**: Weather affects movement and attraction value
4. **Seasons**: Different seasons enable/disable certain attractions
5. **Achievements**: Track player accomplishments across games

For each feature:
1. Write design document (rules, balance, integration)
2. Create new classes/enums needed
3. Modify existing code minimally
4. Add UI controls if needed
5. Create test scenarios

Choose features that:
- Add strategic depth
- Don't break existing gameplay
- Are easy to toggle on/off in settings
- Work well with AI strategies

Provide complete implementation for your best feature.
```

**Attach:** Current game design documents

---

## Usage Instructions

### How to Use These Prompts:

1. **Start a new Claude conversation**
2. **Copy the prompt** for the agent you need
3. **Attach relevant files** (from project directory)
4. **Let Claude work** - it will ask clarifying questions
5. **Review and integrate** the code it provides
6. **Test thoroughly** before moving to next agent

### Recommended Order:

1. âœ… **Code Fixer Pro** (immediately)
2. âœ… **XAML UI Builder** (same day)
3. âœ… **Game Rules Validator** (before playtesting)
4. âœ… **File Structure Architect** (when stable)
5. âš™ï¸ **Unit Test Creator** (for confidence)
6. âš™ï¸ **AI Training Specialist** (for AI improvement)
7. ðŸ“š **Documentation Writer** (for team onboarding)
8. ðŸš€ **Performance Optimizer** (when feature-complete)
9. ðŸ› **Bug Hunter** (before release)
10. ðŸŽ® **Feature Expander** (for v2.0)

### Tips:

- **One agent per chat** - Don't mix concerns
- **Attach specific files** - Not entire project
- **Iterate** - Agents can refine their work
- **Test incrementally** - Don't merge untested code
- **Save agent outputs** - Create task-specific branches

---

## Tracking Progress

Create a `TASKS.md` file:

```markdown
# Task Tracking

## Critical (In Progress)
- [ ] Code Fixer Pro - HandleBusDispatch fix
- [ ] Code Fixer Pro - AttractionEditModel creation

## High Priority (Next)
- [ ] XAML UI Builder - Settings window enhancement
- [ ] Game Rules Validator - Priority system verification

## Medium Priority (Later)
- [ ] File Structure Architect - Cleanup duplicates
- [ ] Unit Test Creator - Core tests

## Low Priority (Future)
- [ ] AI Training Specialist - Headless runner
- [ ] Performance Optimizer - Optimization pass

## Nice to Have (Backlog)
- [ ] Documentation Writer - Full docs
- [ ] Bug Hunter - Edge case testing
- [ ] Feature Expander - New features
```

---

Good luck! Remember: **Fix the critical issue first**, then work through agents systematically. ðŸš€

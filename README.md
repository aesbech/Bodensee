# 🎲 Bodensee Tourismus - Digital Board Game

A C# WPF implementation of the **Bodensee Tourismus** board game - a strategic tourism management game set around Lake Constance (Bodensee) with 20 cities, 4 buses, and 62 unique attractions.

![Game Version](https://img.shields.io/badge/version-0.10-blue)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![WPF](https://img.shields.io/badge/WPF-Desktop-green)
![License](https://img.shields.io/badge/license-MIT-orange)

---

## 📖 About the Game

Players act as tourism companies, moving buses with tourists around Lake Constance to visit attractions and earn money. The game features:

- **20 Cities** around Lake Constance with unique actions
- **4 Buses** carrying colored tourist dice
- **62 Attractions** across 4 categories (Nature 🟢, Water 🔵, Culture 🔴, Gastronomy 🟡)
- **16 Gray Utility Attractions** providing special abilities
- **Strategic Gameplay** with morning actions, all-day actions, and appeal system
- **AI Opponents** with 4 different strategies (Aggressive, Defensive, Balanced, Opportunistic)

---

## ✨ Key Features

### 🎨 Visual Board
- Beautiful game board with clickable cities
- Hover effects for ports and regular cities
- Dynamic bus indicators showing positions
- Attraction ownership dots

### 🎯 Game Mechanics
- **Priority System**: Attractions visited in HIGH→LOW priority order
- **Appeal System**: Buses stop at cities with matching tourist categories
- **Double Payment**: When tourists are ruined, both owner and active player get paid
- **Refill Modes**: Configurable tourist refill (missing slots vs. empty bus)
- **Gray Attractions**: Special utility buildings providing powerful actions

### 🤖 AI System
Four AI strategies with different playstyles:
- **Aggressive**: Maximizes immediate income
- **Defensive**: Minimizes payments to opponents
- **Balanced**: Adapts based on current position
- **Opportunistic**: Exploits game state weaknesses

### 📊 Analytics
- Comprehensive action logging
- Turn summaries with money earned/spent
- Player statistics (ROI, efficiency, strategy usage)
- CSV export for external analysis

### ⚙️ Customization
- **Game Settings**: Starting money, attraction costs, tourist settings
- **Attraction Editor**: Create/edit all 62 attractions with CSV import/export
- **Configurable Rules**: Toggle appeal system, refill modes, bonus features
- **Bilingual**: English and German attraction names

---

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 6.0 Runtime or later
- Visual Studio 2022 (for development)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/aesbech/Bodensee.git
   cd Bodensee
   ```

2. **Open in Visual Studio:**
   ```bash
   Bodensee_Tourismus.sln
   ```

3. **Build and Run:**
   - Press `F5` to compile and run
   - Game window will open at 1600x900

### First Game

1. Click **"New Game"** (or it starts automatically)
2. Select a bus by clicking one of the 4 bus buttons
3. Use morning action (optional)
4. Click **"Move"** button or click a city on the board
5. Use all-day action (optional)
6. Click **"End Turn"**
7. Repeat until game ends (tourist pool empty or attractions exhausted)

---

## 📂 Project Structure

```
Bodensee-Tourismus/
├── Core/                      # Game models and enums
│   └── GameModels.cs          # Player, Tourist, Attraction, City, Bus, GameState
├── Engine/                    # Game logic
│   └── GameEngine.cs          # Turn execution, movement, tours, building
├── Setup/                     # Game initialization
│   └── GameSetup.cs           # Board creation, attraction loading
├── UI/                        # WPF interface
│   ├── MainWindow.xaml        # Main game window (900 lines)
│   ├── MainWindow.xaml.cs     # Code-behind with board visuals (1,300 lines)
│   ├── SettingsWindow.xaml    # Settings editor
│   └── AttractionEditor.xaml  # Attraction customization
├── AI/                        # AI strategies
│   └── AIController.cs        # 4 AI strategies with decision logic
├── Analytics/                 # Game tracking
│   └── GameAnalytics.cs       # Action logging, statistics, CSV export
├── Resources/                 # Assets
│   └── Bodensee_v__0_10.png   # Game board image (1408x1056)
└── docs/                      # Documentation
    ├── INTEGRATION_GUIDE.md   # Setup instructions
    ├── Implementation_Review.md
    └── All_Changes_Implemented.md
```

---

## 🎮 Gameplay Guide

### Turn Structure
1. **Select Bus** - Choose one of 4 buses
2. **Morning Action** - Use city action (optional)
   - Increase Value (+€1 per attraction)
   - Ignore First Appeal (skip one appeal city)
   - Ferry (move to any port)
   - All Attractions Appeal (all tourists visit all attractions)
3. **Move Bus** - Navigate to adjacent city or use ferry
4. **Give Tour** - Tourists visit attractions automatically by priority
5. **All-Day Action** - Use city action (optional)
   - Build Attraction
   - Reroll Tourist
   - Add Pips to Tourist
   - Give Tour
   - Bus Dispatch
   - And more...
6. **Refill** - Add new tourists if any were ruined
7. **End Turn** - Next player's turn

### Winning Strategy
- **Build Attractions** in high-traffic cities
- **Maximize Income** by using Increase Value action
- **Control Routes** by building in strategic locations
- **Use Gray Attractions** for powerful abilities
- **Watch Opponents** and avoid enriching them

---

## ⚙️ Game Settings

### Configurable Options
- **Starting Money**: Per player (€6/€7/€8/€9)
- **Attraction Costs**: Base cost per category
- **Appeal System**: Enable/disable (affects movement rules)
- **Refill Mode**: Fill missing slots vs. fill when empty
- **Bonus Euro**: Some attractions pay €1 extra
- **Gray Attractions**: Enable utility buildings
- **Zentrum Pips Bonus**: How many pips Centers add (default: 2)
- **Casino Rerolls**: How many tourists can be rerolled per bus
- **Language**: English or German attraction names

### Quick Presets
- **Default Settings**: Balanced gameplay
- **High Money Game**: More starting money
- **Low Cost Attractions**: Cheaper building
- **No Appeal Test Mode**: Simplified movement

---

## 🛠️ Development

### Code Structure

**Core Models** (400 lines)
- `Tourist`, `Attraction`, `City`, `Bus`, `Player`, `GameState`
- Clean data models with no logic

**Game Engine** (600 lines)
- Movement validation (`CanBusMove`, `GetValidDestinations`)
- Tour execution (`GiveBusTour`, `GiveSingleTouristTour`)
- Building (`BuildAttraction`)
- Refill logic

**AI System** (800 lines)
- 4 strategy implementations
- Decision evaluation with scoring
- Coordinated multi-turn planning

**UI Layer** (1,300 lines)
- WPF with Material Design styling
- Data binding with MVVM patterns
- Visual board with Canvas/Viewbox
- Interactive city hotspots

### Adding New Features

**New Attraction:**
1. Open Attraction Editor
2. Click Add → Select category
3. Fill in name, pips, priority
4. Save to game

**New AI Strategy:**
1. Create class implementing `IAIStrategy`
2. Override `MakeDecision(GameState, GameEngine)`
3. Register in `AIController` constructor

**New City Action:**
1. Add enum value to `MorningAction` or `AllDayAction`
2. Add handler in `GameEngine`
3. Add UI handler in `MainWindow.xaml.cs`
4. Assign to city in `GameSetup`

---

## 📊 Analytics Export

Game tracks all actions:
- Bus selections
- Morning/all-day action usage
- Attractions visited
- Money earned/spent
- Tourists ruined/refilled

Export to CSV for analysis:
1. Click **"Export Data"**
2. CSV includes turn-by-turn breakdown
3. Analyze in Excel/Python

---

## 🐛 Known Issues

- ✅ **RESOLVED**: Priority system (HIGH→LOW working correctly)
- ✅ **RESOLVED**: Double payment on tourist ruin (implemented)
- ✅ **RESOLVED**: One bus per city rule (enforced)
- ⚠️ **Minor**: Zentrum default is 2 pips (rules say 1)
- ⚠️ **Minor**: Casino affects all buses in city (may need clarification)

---

## 📜 Game Rules (Danish)

See `docs/Rules_Bodensee_Tourismus_(Danish).md` for complete official rules.

**Key Rules:**
- Tourists (dice) have money equal to pips shown
- Buses stop only in cities with appeal
- Attractions visited by priority (HIGH→LOW)
- Tourist ruined when pips reach 0
- Game ends when tourist pool or attraction category exhausts

---

## 🎨 Credits

**Game Design**: Original board game "Bodensee Tourismus"  
**Digital Implementation**: Andreas Esbech  
**AI System**: Claude (Anthropic)  
**Board Image**: Custom illustration  
**Framework**: .NET 6.0, WPF, C#  

---

## 📝 License

MIT License - See LICENSE file for details

---

## 🤝 Contributing

Contributions welcome! Areas for improvement:
- Phase 2 visual enhancements (animated bus movement, zoom controls)
- More AI strategies
- Multiplayer networking
- Mobile version (Xamarin/MAUI)
- Sound effects and music

---

## 📧 Contact

**Developer**: Andreas Esbech  
**GitHub**: [@aesbech](https://github.com/aesbech)  
**Project**: [Bodensee Tourismus](https://github.com/aesbech/Bodensee)

---

## 🎯 Roadmap

### Phase 1 (Complete) ✅
- [x] Core game mechanics
- [x] Visual board integration
- [x] 4 AI strategies
- [x] Settings system
- [x] Analytics export
- [x] Attraction editor

### Phase 2 (Future)
- [ ] Animated bus movement
- [ ] Zoom and pan controls
- [ ] Route highlighting
- [ ] Tourist dice visualization
- [ ] Sound effects
- [ ] Multiplayer support

---

**Ready to play!** 🎲 Start a new game and enjoy Bodensee Tourismus!

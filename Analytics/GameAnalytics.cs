using System;
using System.Collections.Generic;
using System.Linq;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.Analytics
{
    public enum ActionType
    {
        SelectBus,
        UseMorningAction,
        MoveBus,
        VisitAttraction,
        UseAllDayAction,
        BuildAttraction,
        RerollTourist,
        AddPips,
        GiveTour,
        TouristRuined,
        RefillTourist,
        RefillAttraction,
        MoveAnotherBus
    }

    public class GameAction
    {
        public int TurnNumber { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public ActionType ActionType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Details { get; set; }

        public GameAction()
        {
            Details = new Dictionary<string, object>();
            Timestamp = DateTime.Now;
        }
    }

    public class BusStateSnapshot
    {
        public int BusId { get; set; }
        public string Location { get; set; }
        public List<TouristSnapshot> Tourists { get; set; }

        public BusStateSnapshot()
        {
            Tourists = new List<TouristSnapshot>();
        }
    }

    public class TouristSnapshot
    {
        public AttractionCategory Category { get; set; }
        public int Money { get; set; }
        public bool IsRuined => Money <= 0;
    }

    public class GameStateSnapshot
    {
        public int TurnNumber { get; set; }
        public List<PlayerStateSnapshot> Players { get; set; }
        public List<BusStateSnapshot> Buses { get; set; }
        public DateTime Timestamp { get; set; }

        public GameStateSnapshot()
        {
            Players = new List<PlayerStateSnapshot>();
            Buses = new List<BusStateSnapshot>();
            Timestamp = DateTime.Now;
        }
    }

    public class PlayerStateSnapshot
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Money { get; set; }
        public int AttractionCount { get; set; }
    }

    public class TurnSummary
    {
        public int TurnNumber { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int BusId { get; set; }
        public string StartCity { get; set; }
        public string EndCity { get; set; }
        public MorningAction? MorningActionUsed { get; set; }
        public AllDayAction? AllDayActionUsed { get; set; }
        public int AttractionsVisited { get; set; }
        public int MoneyEarned { get; set; }
        public int MoneySpent { get; set; }
        public int TouristsRuined { get; set; }
        public int TouristsAdded { get; set; }
        public int NetMoney => MoneyEarned - MoneySpent;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public List<GameAction> Actions { get; set; }

        // New: Detailed state snapshots
        public GameStateSnapshot StateBeforeTurn { get; set; }
        public GameStateSnapshot StateAfterTurn { get; set; }
        public int MoneyBefore { get; set; }
        public int MoneyAfter { get; set; }

        public TurnSummary()
        {
            Actions = new List<GameAction>();
            StartTime = DateTime.Now;
        }

        public void CompleteTurn()
        {
            EndTime = DateTime.Now;
        }
    }

    public class PlayerStatistics
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsAI { get; set; }
        public string AIStrategy { get; set; }
        
        // Financial stats
        public int TotalMoneyEarned { get; set; }
        public int TotalMoneySpent { get; set; }
        public int FinalMoney { get; set; }
        public int NetProfit => TotalMoneyEarned - TotalMoneySpent;
        
        // Building stats
        public int AttractionsBuilt { get; set; }
        public Dictionary<AttractionCategory, int> AttractionsByCategory { get; set; }
        public int TotalBuildingCost { get; set; }
        
        // Tourist stats
        public int TouristsRuined { get; set; }
        public int MoneyFromRuinedTourists { get; set; }
        
        // Action usage
        public Dictionary<MorningAction, int> MorningActionsUsed { get; set; }
        public Dictionary<AllDayAction, int> AllDayActionsUsed { get; set; }
        
        // Attraction income
        public int AttractionsVisitedByOthers { get; set; }
        public int IncomeFromAttractions { get; set; }
        
        // Efficiency metrics
        public double AverageMoneyPerTurn => TurnCount > 0 ? (double)TotalMoneyEarned / TurnCount : 0;
        public double AverageAttractionsVisitedPerTurn => TurnCount > 0 ? (double)TotalAttractionsVisited / TurnCount : 0;
        public int TurnCount { get; set; }
        public int TotalAttractionsVisited { get; set; }

        public PlayerStatistics()
        {
            AttractionsByCategory = new Dictionary<AttractionCategory, int>();
            MorningActionsUsed = new Dictionary<MorningAction, int>();
            AllDayActionsUsed = new Dictionary<AllDayAction, int>();
            
            foreach (AttractionCategory cat in Enum.GetValues(typeof(AttractionCategory)))
            {
                AttractionsByCategory[cat] = 0;
            }
        }
    }

    public class GameAnalytics
    {
        private List<GameAction> _allActions;
        private List<TurnSummary> _turnSummaries;
        private TurnSummary _currentTurn;
        private int _turnCounter;
        private Dictionary<string, object> _gameSettings;

        public GameAnalytics()
        {
            _allActions = new List<GameAction>();
            _turnSummaries = new List<TurnSummary>();
            _turnCounter = 0;
            _gameSettings = new Dictionary<string, object>();
        }

        public void RecordGameSettings(GameSettings settings)
        {
            _gameSettings = settings.ExportForAnalytics();
        }

        public void StartTurn(int playerId, string playerName, GameState gameState = null)
        {
            _turnCounter++;
            _currentTurn = new TurnSummary
            {
                TurnNumber = _turnCounter,
                PlayerId = playerId,
                PlayerName = playerName
            };

            // Capture game state at turn start
            if (gameState != null)
            {
                _currentTurn.StateBeforeTurn = CaptureGameState(gameState, _turnCounter);
                _currentTurn.MoneyBefore = gameState.CurrentPlayer.Money;
            }
        }

        public void EndTurn(GameState gameState = null)
        {
            if (_currentTurn != null)
            {
                _currentTurn.CompleteTurn();

                // Capture game state at turn end
                if (gameState != null)
                {
                    _currentTurn.StateAfterTurn = CaptureGameState(gameState, _turnCounter);
                    _currentTurn.MoneyAfter = gameState.CurrentPlayer.Money;
                }

                _turnSummaries.Add(_currentTurn);
                _currentTurn = null;
            }
        }

        private GameStateSnapshot CaptureGameState(GameState gameState, int turnNumber)
        {
            var snapshot = new GameStateSnapshot
            {
                TurnNumber = turnNumber
            };

            // Capture player states
            foreach (var player in gameState.Players)
            {
                int attractionCount = gameState.Board.Cities.Values
                    .SelectMany(c => c.Attractions)
                    .Count(a => a.OwnerId == player.Id);

                snapshot.Players.Add(new PlayerStateSnapshot
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    Money = player.Money,
                    AttractionCount = attractionCount
                });
            }

            // Capture bus states
            foreach (var bus in gameState.Board.Buses)
            {
                var busSnapshot = new BusStateSnapshot
                {
                    BusId = bus.Id,
                    Location = bus.CurrentCity
                };

                foreach (var tourist in bus.Tourists)
                {
                    busSnapshot.Tourists.Add(new TouristSnapshot
                    {
                        Category = tourist.Category,
                        Money = tourist.Money
                    });
                }

                snapshot.Buses.Add(busSnapshot);
            }

            return snapshot;
        }

        public void LogAction(int playerId, string playerName, ActionType actionType, Dictionary<string, object> details = null)
        {
            var action = new GameAction
            {
                TurnNumber = _turnCounter,
                PlayerId = playerId,
                PlayerName = playerName,
                ActionType = actionType,
                Details = details ?? new Dictionary<string, object>()
            };

            _allActions.Add(action);
            
            if (_currentTurn != null)
            {
                _currentTurn.Actions.Add(action);
                
                // Update turn summary based on action type
                switch (actionType)
                {
                    case ActionType.SelectBus:
                        _currentTurn.BusId = (int)details["BusId"];
                        _currentTurn.StartCity = (string)details["City"];
                        break;
                    case ActionType.UseMorningAction:
                        _currentTurn.MorningActionUsed = (MorningAction)details["Action"];
                        break;
                    case ActionType.MoveBus:
                        _currentTurn.EndCity = (string)details["ToCity"];
                        break;
                    case ActionType.VisitAttraction:
                        _currentTurn.AttractionsVisited++;
                        if (details.ContainsKey("MoneyEarned"))
                            _currentTurn.MoneyEarned += (int)details["MoneyEarned"];
                        break;
                    case ActionType.UseAllDayAction:
                        _currentTurn.AllDayActionUsed = (AllDayAction)details["Action"];
                        break;
                    case ActionType.BuildAttraction:
                        if (details.ContainsKey("Cost"))
                            _currentTurn.MoneySpent += (int)details["Cost"];
                        break;
                    case ActionType.TouristRuined:
                        _currentTurn.TouristsRuined++;
                        if (details.ContainsKey("MoneyEarned"))
                            _currentTurn.MoneyEarned += (int)details["MoneyEarned"];
                        break;
                    case ActionType.RefillTourist:
                        _currentTurn.TouristsAdded++;
                        break;
                }
            }
        }

        public PlayerStatistics GetPlayerStatistics(int playerId, GameState finalState)
        {
            var player = finalState.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return null;

            var stats = new PlayerStatistics
            {
                PlayerId = playerId,
                PlayerName = player.Name,
                IsAI = player.IsAI,
                AIStrategy = player.AIStrategy,
                FinalMoney = player.Money
            };

            var playerTurns = _turnSummaries.Where(t => t.PlayerId == playerId).ToList();
            stats.TurnCount = playerTurns.Count;

            foreach (var turn in playerTurns)
            {
                stats.TotalMoneyEarned += turn.MoneyEarned;
                stats.TotalMoneySpent += turn.MoneySpent;
                stats.TotalAttractionsVisited += turn.AttractionsVisited;
                stats.TouristsRuined += turn.TouristsRuined;

                if (turn.MorningActionUsed.HasValue)
                {
                    var action = turn.MorningActionUsed.Value;
                    stats.MorningActionsUsed[action] = stats.MorningActionsUsed.GetValueOrDefault(action, 0) + 1;
                }

                if (turn.AllDayActionUsed.HasValue)
                {
                    var action = turn.AllDayActionUsed.Value;
                    stats.AllDayActionsUsed[action] = stats.AllDayActionsUsed.GetValueOrDefault(action, 0) + 1;
                }
            }

            // Count attractions built
            var buildActions = _allActions.Where(a => 
                a.PlayerId == playerId && 
                a.ActionType == ActionType.BuildAttraction).ToList();
            
            stats.AttractionsBuilt = buildActions.Count;
            
            foreach (var action in buildActions)
            {
                if (action.Details.ContainsKey("Category"))
                {
                    var category = (AttractionCategory)action.Details["Category"];
                    stats.AttractionsByCategory[category]++;
                }
                if (action.Details.ContainsKey("Cost"))
                {
                    stats.TotalBuildingCost += (int)action.Details["Cost"];
                }
            }

            // Calculate income from owned attractions
            var playerAttractions = finalState.Board.Cities.Values
                .SelectMany(c => c.Attractions)
                .Where(a => a.OwnerId == playerId)
                .Select(a => a.Id)
                .ToHashSet();

            var visitActions = _allActions.Where(a =>
                a.ActionType == ActionType.VisitAttraction &&
                a.PlayerId != playerId &&
                a.Details.ContainsKey("AttractionId") &&
                playerAttractions.Contains((int)a.Details["AttractionId"])).ToList();

            stats.AttractionsVisitedByOthers = visitActions.Count;
            stats.IncomeFromAttractions = visitActions
                .Where(a => a.Details.ContainsKey("MoneyEarned"))
                .Sum(a => (int)a.Details["MoneyEarned"]);

            // Money from ruined tourists
            var ruinActions = _allActions.Where(a =>
                a.PlayerId == playerId &&
                a.ActionType == ActionType.TouristRuined).ToList();
            
            stats.MoneyFromRuinedTourists = ruinActions
                .Where(a => a.Details.ContainsKey("MoneyEarned"))
                .Sum(a => (int)a.Details["MoneyEarned"]);

            return stats;
        }

        public List<TurnSummary> GetAllTurns()
        {
            return new List<TurnSummary>(_turnSummaries);
        }

        public List<TurnSummary> GetPlayerTurns(int playerId)
        {
            return _turnSummaries.Where(t => t.PlayerId == playerId).ToList();
        }

        public List<GameAction> GetAllActions()
        {
            return new List<GameAction>(_allActions);
        }

        public Dictionary<string, object> GetGameSummary(GameState finalState)
        {
            var summary = new Dictionary<string, object>();
            
            summary["TotalTurns"] = _turnCounter;
            summary["TotalActions"] = _allActions.Count;
            summary["GameDuration"] = _turnSummaries.Count > 0 
                ? _turnSummaries.Last().EndTime - _turnSummaries.First().StartTime 
                : TimeSpan.Zero;
            
            summary["Winner"] = finalState.GetWinner()?.Name ?? "None";
            summary["FinalScores"] = finalState.Players.ToDictionary(p => p.Name, p => p.Money);
            
            var actionCounts = _allActions.GroupBy(a => a.ActionType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            summary["ActionCounts"] = actionCounts;
            
            return summary;
        }

        public string ExportToCSV()
        {
            var lines = new List<string>();

            // Game Settings Header
            if (_gameSettings != null && _gameSettings.Count > 0)
            {
                lines.Add("=== GAME SETTINGS ===");
                lines.Add("Setting,Value");
                foreach (var setting in _gameSettings)
                {
                    lines.Add($"{setting.Key},{setting.Value}");
                }
                lines.Add(""); // Empty line separator
            }

            // Turn-by-Turn Summary
            lines.Add("=== TURN SUMMARY ===");
            lines.Add("Turn,Player,BusId,FromCity,ToCity,MorningAction,AllDayAction,AttractionsVisited,MoneyBefore,MoneyAfter,NetChange,TouristsRuined,Duration");
            foreach (var turn in _turnSummaries)
            {
                lines.Add($"{turn.TurnNumber},{turn.PlayerName},{turn.BusId},{turn.StartCity},{turn.EndCity}," +
                         $"{turn.MorningActionUsed},{turn.AllDayActionUsed},{turn.AttractionsVisited}," +
                         $"{turn.MoneyBefore},{turn.MoneyAfter},{turn.MoneyAfter - turn.MoneyBefore}," +
                         $"{turn.TouristsRuined},{turn.Duration.TotalSeconds:F2}");
            }
            lines.Add("");

            // Detailed Bus State per Turn
            lines.Add("=== BUS STATE PER TURN ===");
            lines.Add("Turn,BusId,Location,Tourist1_Category,Tourist1_Money,Tourist2_Category,Tourist2_Money,Tourist3_Category,Tourist3_Money,Tourist4_Category,Tourist4_Money");
            foreach (var turn in _turnSummaries)
            {
                if (turn.StateAfterTurn != null)
                {
                    foreach (var bus in turn.StateAfterTurn.Buses)
                    {
                        var touristData = new List<string>();
                        for (int i = 0; i < 4; i++)
                        {
                            if (i < bus.Tourists.Count)
                            {
                                touristData.Add(bus.Tourists[i].Category.ToString());
                                touristData.Add(bus.Tourists[i].Money.ToString());
                            }
                            else
                            {
                                touristData.Add("");
                                touristData.Add("");
                            }
                        }
                        lines.Add($"{turn.TurnNumber},{bus.BusId},{bus.Location},{string.Join(",", touristData)}");
                    }
                }
            }
            lines.Add("");

            // Player State per Turn
            lines.Add("=== PLAYER STATE PER TURN ===");
            lines.Add("Turn,PlayerId,PlayerName,Money,AttractionCount");
            foreach (var turn in _turnSummaries)
            {
                if (turn.StateAfterTurn != null)
                {
                    foreach (var player in turn.StateAfterTurn.Players)
                    {
                        lines.Add($"{turn.TurnNumber},{player.PlayerId},{player.PlayerName},{player.Money},{player.AttractionCount}");
                    }
                }
            }
            lines.Add("");

            // Actions Header
            lines.Add("=== GAME ACTIONS ===");
            lines.Add("TurnNumber,PlayerId,PlayerName,ActionType,Details");

            // Data
            foreach (var action in _allActions)
            {
                var details = string.Join(";", action.Details.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                lines.Add($"{action.TurnNumber},{action.PlayerId},{action.PlayerName},{action.ActionType},{details}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
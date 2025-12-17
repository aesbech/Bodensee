using System;
using System.Collections.Generic;
using System.Linq;
using BodenseeTourismus.Core;
using BodenseeTourismus.Engine;
using BodenseeTourismus.Setup;
using BodenseeTourismus.Analytics;

namespace BodenseeTourismus.AI
{
    public class GameResult
    {
        public int GameNumber { get; set; }
        public string Winner { get; set; }
        public int WinnerId { get; set; }
        public Dictionary<string, int> FinalScores { get; set; }
        public Dictionary<string, PlayerStatistics> PlayerStats { get; set; }
        public int TotalTurns { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Completed { get; set; }
        public string ErrorMessage { get; set; }

        public GameResult()
        {
            FinalScores = new Dictionary<string, int>();
            PlayerStats = new Dictionary<string, PlayerStatistics>();
        }
    }

    public class BatchResult
    {
        public int TotalGames { get; set; }
        public int CompletedGames { get; set; }
        public List<GameResult> Games { get; set; }
        public Dictionary<string, int> WinCounts { get; set; }
        public Dictionary<string, double> AverageScores { get; set; }
        public Dictionary<string, double> AverageMoneyPerTurn { get; set; }
        public TimeSpan TotalDuration { get; set; }

        public BatchResult()
        {
            Games = new List<GameResult>();
            WinCounts = new Dictionary<string, int>();
            AverageScores = new Dictionary<string, double>();
            AverageMoneyPerTurn = new Dictionary<string, double>();
        }
    }

    public class HeadlessGameRunner
    {
        private GameSettings _settings;
        private Random _random;
        private bool _verbose;

        public HeadlessGameRunner(GameSettings settings = null, int? seed = null, bool verbose = false)
        {
            _settings = settings ?? new GameSettings();
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _verbose = verbose;
        }

        /// <summary>
        /// Run a single game with specified AI strategies
        /// </summary>
        /// <param name="aiStrategies">List of AI strategy names (e.g., ["Aggressive", "Defensive", "Balanced", "Opportunistic"])</param>
        /// <param name="gameNumber">Optional game number for tracking</param>
        /// <returns>Game result with statistics</returns>
        public GameResult RunGame(List<string> aiStrategies, int gameNumber = 1)
        {
            var result = new GameResult { GameNumber = gameNumber };
            var startTime = DateTime.Now;

            try
            {
                // Setup game with AI players
                var playerConfigs = aiStrategies.Select((strategy, index) =>
                    ($"AI-{strategy}", true, strategy)).ToList();

                var setup = new GameSetup();
                var gameState = setup.CreateGame(playerConfigs, _settings);
                var engine = new GameEngine(gameState);
                var analytics = new GameAnalytics();
                analytics.RecordGameSettings(_settings);
                var aiController = new AIController(_random);

                int turnLimit = 1000; // Safety limit to prevent infinite loops
                int turnCount = 0;

                Log($"[Game {gameNumber}] Starting with strategies: {string.Join(", ", aiStrategies)}");

                // Main game loop
                while (!gameState.GameEnded && turnCount < turnLimit)
                {
                    var currentPlayer = gameState.CurrentPlayer;
                    var turnContext = new TurnContext();
                    turnCount++;

                    analytics.StartTurn(currentPlayer.Id, currentPlayer.Name);

                    // Get AI decision
                    var decision = aiController.GetAIDecision(gameState, engine, currentPlayer.AIStrategy);

                    if (decision.SelectedBus == null)
                    {
                        Log($"[Turn {turnCount}] {currentPlayer.Name} cannot move any bus");
                        analytics.EndTurn();
                        gameState.NextPlayer();
                        continue;
                    }

                    turnContext.SelectedBus = decision.SelectedBus;
                    turnContext.StartCity = decision.SelectedBus.CurrentCity;

                    // Morning action
                    if (decision.MorningAction.HasValue)
                    {
                        turnContext.UsedMorningAction = decision.MorningAction.Value;
                        if (decision.MorningAction == MorningAction.IncreaseValue)
                            turnContext.IncreaseValue = true;
                        if (decision.MorningAction == MorningAction.AllAttractionsAppeal)
                            turnContext.AllAttractionsAppeal = true;
                        if (decision.MorningAction == MorningAction.IgnoreFirstAppeal)
                            turnContext.IgnoreNextAppeal = true;

                        analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                            ActionType.UseMorningAction, new Dictionary<string, object>
                            {
                                { "Action", decision.MorningAction.Value }
                            });
                    }
                    else
                    {
                        turnContext.UsedMorningAction = MorningAction.None;
                    }

                    // Move bus
                    if (!string.IsNullOrEmpty(decision.DestinationCity))
                    {
                        string fromCity = decision.SelectedBus.CurrentCity;
                        decision.SelectedBus.CurrentCity = decision.DestinationCity;
                        turnContext.HasMoved = true;

                        analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                            ActionType.MoveBus, new Dictionary<string, object>
                            {
                                { "FromCity", fromCity },
                                { "ToCity", decision.DestinationCity }
                            });

                        // Give tour
                        var tourResult = engine.GiveBusTour(decision.SelectedBus, turnContext);
                        turnContext.TouristsRuined = tourResult.TouristsRuined;

                        foreach (var attractionId in tourResult.AttractionsVisited)
                        {
                            analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                ActionType.VisitAttraction, new Dictionary<string, object>
                                {
                                    { "AttractionId", attractionId }
                                });
                        }
                    }

                    // All-day action (simplified execution - just track in analytics)
                    if (decision.AllDayAction.HasValue)
                    {
                        ExecuteAllDayAction(gameState, engine, decision, turnContext, analytics);
                        turnContext.UsedAllDayAction = true;
                    }

                    // Refill
                    engine.Refill(turnContext);

                    // Check game end
                    engine.CheckGameEnd();

                    analytics.EndTurn();

                    // Next player
                    gameState.NextPlayer();
                }

                // Game completed
                var winner = gameState.GetWinner();
                result.Completed = true;
                result.Winner = winner?.Name ?? "None";
                result.WinnerId = winner?.Id ?? -1;
                result.TotalTurns = turnCount;

                foreach (var player in gameState.Players)
                {
                    result.FinalScores[player.Name] = player.Money;
                    result.PlayerStats[player.Name] = analytics.GetPlayerStatistics(player.Id, gameState);
                }

                result.Duration = DateTime.Now - startTime;

                Log($"[Game {gameNumber}] Completed in {turnCount} turns. Winner: {result.Winner} (€{result.FinalScores[result.Winner]})");
            }
            catch (Exception ex)
            {
                result.Completed = false;
                result.ErrorMessage = ex.Message;
                Log($"[Game {gameNumber}] ERROR: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Run multiple games and collect aggregate statistics
        /// </summary>
        /// <param name="gameCount">Number of games to run</param>
        /// <param name="aiStrategies">List of AI strategy names</param>
        /// <returns>Batch results with aggregate statistics</returns>
        public BatchResult RunBatch(int gameCount, List<string> aiStrategies)
        {
            var batchResult = new BatchResult
            {
                TotalGames = gameCount
            };

            var startTime = DateTime.Now;

            Log($"Starting batch of {gameCount} games with strategies: {string.Join(", ", aiStrategies)}");

            for (int i = 1; i <= gameCount; i++)
            {
                var gameResult = RunGame(aiStrategies, i);
                batchResult.Games.Add(gameResult);

                if (gameResult.Completed)
                {
                    batchResult.CompletedGames++;

                    // Track wins
                    if (!batchResult.WinCounts.ContainsKey(gameResult.Winner))
                        batchResult.WinCounts[gameResult.Winner] = 0;
                    batchResult.WinCounts[gameResult.Winner]++;
                }
            }

            batchResult.TotalDuration = DateTime.Now - startTime;

            // Calculate aggregate statistics
            if (batchResult.CompletedGames > 0)
            {
                var completedGames = batchResult.Games.Where(g => g.Completed).ToList();

                // Average scores per player
                var allPlayerNames = completedGames.SelectMany(g => g.FinalScores.Keys).Distinct().ToList();
                foreach (var playerName in allPlayerNames)
                {
                    var scores = completedGames
                        .Where(g => g.FinalScores.ContainsKey(playerName))
                        .Select(g => g.FinalScores[playerName])
                        .ToList();

                    batchResult.AverageScores[playerName] = scores.Any() ? scores.Average() : 0;

                    // Average money per turn
                    var moneyPerTurn = completedGames
                        .Where(g => g.PlayerStats.ContainsKey(playerName))
                        .Select(g => g.PlayerStats[playerName].AverageMoneyPerTurn)
                        .ToList();

                    batchResult.AverageMoneyPerTurn[playerName] = moneyPerTurn.Any() ? moneyPerTurn.Average() : 0;
                }
            }

            Log($"Batch completed: {batchResult.CompletedGames}/{gameCount} games finished successfully");
            Log($"Win counts: {string.Join(", ", batchResult.WinCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");

            return batchResult;
        }

        private void ExecuteAllDayAction(GameState gameState, GameEngine engine, AIDecision decision, TurnContext turnContext, GameAnalytics analytics)
        {
            var currentPlayer = gameState.CurrentPlayer;

            switch (decision.AllDayAction.Value)
            {
                case AllDayAction.BuildAttraction:
                case AllDayAction.BuildAttractionDiscount:
                    if (decision.ActionParameters.ContainsKey("Attraction"))
                    {
                        var attraction = decision.ActionParameters["Attraction"] as Attraction;
                        var cityName = decision.ActionParameters["City"] as string;
                        int discount = decision.AllDayAction == AllDayAction.BuildAttractionDiscount
                            ? _settings.ContractorDiscountAmount
                            : 0;

                        if (engine.BuildAttraction(attraction, cityName, discount))
                        {
                            analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                ActionType.BuildAttraction, new Dictionary<string, object>
                                {
                                    { "AttractionId", attraction.Id },
                                    { "Category", attraction.Category },
                                    { "City", cityName }
                                });

                            // Refill market
                            if (attraction.Category == AttractionCategory.Gray)
                                gameState.Market.RefillGray();
                            else
                                gameState.Market.Refill(attraction.Category, _settings.MarketRefillAmount);
                        }
                    }
                    break;

                case AllDayAction.AddTwoPips:
                    if (decision.SelectedBus.Tourists.Any())
                    {
                        var tourist = decision.SelectedBus.Tourists[_random.Next(decision.SelectedBus.Tourists.Count)];
                        int oldValue = tourist.Money;
                        tourist.Money = Math.Min(6, tourist.Money + _settings.ZentrumPipsBonus);

                        analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                            ActionType.AddPips, new Dictionary<string, object>
                            {
                                { "OldValue", oldValue },
                                { "NewValue", tourist.Money }
                            });
                    }
                    break;

                case AllDayAction.AddTwoPipsGreen:
                case AllDayAction.AddTwoPipsBlue:
                case AllDayAction.AddTwoPipsRed:
                case AllDayAction.AddTwoPipsYellow:
                    AttractionCategory targetCategory = decision.AllDayAction.Value switch
                    {
                        AllDayAction.AddTwoPipsGreen => AttractionCategory.Nature,
                        AllDayAction.AddTwoPipsBlue => AttractionCategory.Water,
                        AllDayAction.AddTwoPipsRed => AttractionCategory.Culture,
                        AllDayAction.AddTwoPipsYellow => AttractionCategory.Gastronomy,
                        _ => AttractionCategory.Nature
                    };

                    var colorTourist = decision.SelectedBus.Tourists.FirstOrDefault(t => t.Category == targetCategory);
                    if (colorTourist != null)
                    {
                        int oldValue = colorTourist.Money;
                        colorTourist.Money = Math.Min(6, colorTourist.Money + _settings.ZentrumPipsBonus);

                        analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                            ActionType.AddPips, new Dictionary<string, object>
                            {
                                { "OldValue", oldValue },
                                { "NewValue", colorTourist.Money },
                                { "Category", targetCategory }
                            });
                    }
                    break;

                case AllDayAction.RerollTourist:
                    var cityBuses = gameState.Board.Buses
                        .Where(b => b.CurrentCity == decision.SelectedBus.CurrentCity)
                        .ToList();
                    var allTourists = cityBuses.SelectMany(b => b.Tourists).ToList();

                    if (allTourists.Any())
                    {
                        int rerollsAllowed = _settings.CasinoRerollsPerBus;
                        for (int i = 0; i < rerollsAllowed && allTourists.Any(); i++)
                        {
                            var poorestTourist = allTourists.OrderBy(t => t.Money).First();
                            int oldMoney = poorestTourist.Money;

                            int newMoney;
                            do
                            {
                                newMoney = _random.Next(1, 7);
                            } while (newMoney == 1);

                            poorestTourist.Money = newMoney;

                            analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                ActionType.RerollTourist, new Dictionary<string, object>
                                {
                                    { "OldValue", oldMoney },
                                    { "NewValue", newMoney }
                                });
                        }
                    }
                    break;

                case AllDayAction.GiveTour:
                    if (_settings.GiveTourAffectsWholeBus)
                    {
                        var extraTourResult = engine.GiveBusTour(decision.SelectedBus, turnContext);
                        turnContext.TouristsRuined += extraTourResult.TouristsRuined;

                        foreach (var attractionId in extraTourResult.AttractionsVisited)
                        {
                            analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                ActionType.VisitAttraction, new Dictionary<string, object>
                                {
                                    { "AttractionId", attractionId },
                                    { "GiveTour", true }
                                });
                        }
                    }
                    else
                    {
                        if (decision.SelectedBus.Tourists.Any())
                        {
                            var richest = decision.SelectedBus.Tourists.OrderByDescending(t => t.Money).First();
                            var singleTourResult = engine.GiveSingleTouristTour(decision.SelectedBus, richest, decision.SelectedBus.CurrentCity);

                            foreach (var attractionId in singleTourResult.AttractionsVisited)
                            {
                                analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                    ActionType.VisitAttraction, new Dictionary<string, object>
                                    {
                                        { "AttractionId", attractionId },
                                        { "GiveTour", true },
                                        { "SingleTourist", true }
                                    });
                            }
                        }
                    }
                    break;

                case AllDayAction.BusDispatch:
                    var otherBuses = gameState.Board.Buses.Where(b => b.Id != decision.SelectedBus.Id).ToList();
                    if (otherBuses.Any())
                    {
                        var busToMove = otherBuses[_random.Next(otherBuses.Count)];
                        var validDests = engine.GetValidDestinations(busToMove, new TurnContext());
                        if (validDests.Any())
                        {
                            string oldCity = busToMove.CurrentCity;
                            string newCity = validDests[_random.Next(validDests.Count)];
                            busToMove.CurrentCity = newCity;

                            analytics.LogAction(currentPlayer.Id, currentPlayer.Name,
                                ActionType.MoveAnotherBus, new Dictionary<string, object>
                                {
                                    { "BusId", busToMove.Id },
                                    { "FromCity", oldCity },
                                    { "ToCity", newCity }
                                });
                        }
                    }
                    break;
            }
        }

        private void Log(string message)
        {
            if (_verbose)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }
    }
}

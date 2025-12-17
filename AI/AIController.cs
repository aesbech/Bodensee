using System;
using System.Collections.Generic;
using System.Linq;
using BodenseeTourismus.Core;
using BodenseeTourismus.Engine;

namespace BodenseeTourismus.AI
{
    public interface IAIStrategy
    {
        string Name { get; }
        AIDecision MakeDecision(GameState state, GameEngine engine);
    }

    public class AIDecision
    {
        public Bus SelectedBus { get; set; }
        public MorningAction? MorningAction { get; set; }
        public string DestinationCity { get; set; }
        public AllDayAction? AllDayAction { get; set; }
        public Dictionary<string, object> ActionParameters { get; set; }

        public AIDecision()
        {
            ActionParameters = new Dictionary<string, object>();
        }
    }

    // Base class with common utility methods
    public abstract class BaseAIStrategy : IAIStrategy
    {
        public abstract string Name { get; }
        protected Random _random;

        protected BaseAIStrategy(Random random = null)
        {
            _random = random ?? new Random();
        }

        public abstract AIDecision MakeDecision(GameState state, GameEngine engine);

        protected double EvaluateCity(GameState state, Bus bus, string cityName, TurnContext context)
        {
            var city = state.Board.GetCity(cityName);
            if (city == null) return 0;

            double score = 0;

            // Evaluate potential income from attractions
            var myAttractions = city.Attractions.Where(a => a.OwnerId == state.CurrentPlayer.Id).ToList();
            var otherAttractions = city.Attractions.Where(a => a.OwnerId != null && a.OwnerId != state.CurrentPlayer.Id).ToList();

            foreach (var attraction in myAttractions)
            {
                // Can any tourist visit my attraction?
                var eligibleTourists = bus.Tourists
                    .Where(t => context.AllAttractionsAppeal || t.Category == attraction.Category)
                    .Where(t => t.Money >= attraction.Value)
                    .ToList();

                if (eligibleTourists.Any())
                {
                    score += attraction.Value * 2; // Double value for own attractions
                }
            }

            // Penalty for visiting other players' attractions
            foreach (var attraction in otherAttractions)
            {
                var eligibleTourists = bus.Tourists
                    .Where(t => context.AllAttractionsAppeal || t.Category == attraction.Category)
                    .Where(t => t.Money >= attraction.Value)
                    .ToList();

                if (eligibleTourists.Any())
                {
                    score -= attraction.Value * 0.5; // Penalty for enriching others
                }
            }

            // Bonus for all-day actions
            if (city.AllDayAction.HasValue)
            {
                score += EvaluateAllDayAction(state, city.AllDayAction.Value);
            }

            return score;
        }

        protected double EvaluateAllDayAction(GameState state, AllDayAction action)
        {
            switch (action)
            {
                case AllDayAction.BuildAttraction:
                    return 15; // High value
                case AllDayAction.AddTwoPips:
                    return 8;
                case AllDayAction.RerollTourist:
                    return 5;
                case AllDayAction.GiveTour:
                    return 7;
                default:
                    return 0;
            }
        }

        protected Bus SelectBestBus(GameState state, GameEngine engine)
        {
            var movableBuses = state.Board.Buses.Where(b => engine.CanBusMove(b)).ToList();
            if (!movableBuses.Any()) return null;

            return movableBuses.OrderByDescending(b => b.Tourists.Count).First();
        }

        protected bool ShouldUseFerry(GameState state, Bus bus, City currentCity, TurnContext context)
        {
            // Check if Ferry is available
            if (currentCity.MorningAction != MorningAction.Ferry || !currentCity.IsPort)
                return false;

            // Evaluate if Ferry opens better opportunities
            var normalDestinations = currentCity.Connections;
            var ferryDestinations = state.Board.Cities.Values
                .Where(c => c.IsPort && c.Name != currentCity.Name)
                .Select(c => c.Name)
                .ToList();

            // Ferry is valuable if it opens access to ports not normally reachable
            var extraPorts = ferryDestinations.Except(normalDestinations).ToList();
            if (!extraPorts.Any()) return false;

            // Check if any extra port has high value
            var bestExtraPortScore = extraPorts
                .Max(port => EvaluateCity(state, bus, port, context));
            var bestNormalScore = normalDestinations
                .Any() ? normalDestinations.Max(dest => EvaluateCity(state, bus, dest, context)) : 0;

            return bestExtraPortScore > bestNormalScore;
        }

        protected List<string> GetAllPortCities(GameState state)
        {
            return state.Board.Cities.Values
                .Where(c => c.IsPort)
                .Select(c => c.Name)
                .ToList();
        }
    }

    // Aggressive strategy: Maximize immediate income
    public class AggressiveStrategy : BaseAIStrategy
    {
        public override string Name => "Aggressive";

        public AggressiveStrategy(Random random = null) : base(random) { }

        public override AIDecision MakeDecision(GameState state, GameEngine engine)
        {
            var decision = new AIDecision();
            var context = new TurnContext();

            // Select bus with most tourists
            decision.SelectedBus = SelectBestBus(state, engine);
            if (decision.SelectedBus == null) return decision;

            var currentCity = state.Board.GetCity(decision.SelectedBus.CurrentCity);

            // Check if Ferry provides strategic advantage
            bool useFerry = ShouldUseFerry(state, decision.SelectedBus, currentCity, context);

            // Always try to increase value if available, unless Ferry is better
            if (!useFerry && currentCity.MorningAction == MorningAction.IncreaseValue)
            {
                decision.MorningAction = MorningAction.IncreaseValue;
                context.IncreaseValue = true;
            }
            else if (!useFerry && currentCity.MorningAction == MorningAction.AllAttractionsAppeal)
            {
                decision.MorningAction = MorningAction.AllAttractionsAppeal;
                context.AllAttractionsAppeal = true;
            }
            else if (useFerry)
            {
                decision.MorningAction = MorningAction.Ferry;
            }

            context.UsedMorningAction = decision.MorningAction;

            // Choose destination with highest income potential
            var destinations = engine.GetValidDestinations(decision.SelectedBus, context);
            if (destinations.Any())
            {
                var bestDestination = destinations
                    .OrderByDescending(d => EvaluateCity(state, decision.SelectedBus, d, context))
                    .First();
                decision.DestinationCity = bestDestination;
            }

            // All-day action: prioritize building attractions
            var arrivalCity = state.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);
            if (arrivalCity?.AllDayAction == AllDayAction.BuildAttraction)
            {
                decision.AllDayAction = AllDayAction.BuildAttraction;
                
                // Choose most profitable attraction to build
                var bestAttraction = ChooseBestAttractionToBuild(state, arrivalCity.Name);
                if (bestAttraction != null)
                {
                    decision.ActionParameters["Attraction"] = bestAttraction;
                    decision.ActionParameters["City"] = arrivalCity.Name;
                }
            }

            return decision;
        }

        private Attraction ChooseBestAttractionToBuild(GameState state, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            var availableAttractions = state.Market.AvailableAttractions.Values
                .SelectMany(list => list)
                .Where(a => a.Category != AttractionCategory.Water || city.CanBuildWater)
                .ToList();

            if (!availableAttractions.Any()) return null;

            // Calculate ROI for each attraction
            var bestAttraction = availableAttractions
                .Select(a => new
                {
                    Attraction = a,
                    Cost = CalculateAttractionCost(state, a, cityName),
                    Value = a.Value,
                    ROI = (double)a.Value / Math.Max(1, CalculateAttractionCost(state, a, cityName))
                })
                .Where(x => x.Cost <= state.CurrentPlayer.Money)
                .OrderByDescending(x => x.ROI)
                .FirstOrDefault();

            return bestAttraction?.Attraction;
        }

        private int CalculateAttractionCost(GameState state, Attraction attraction, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            int baseCost = attraction.Category switch
            {
                AttractionCategory.Nature => 1,
                AttractionCategory.Water => 1,
                AttractionCategory.Culture => 2,
                AttractionCategory.Gastronomy => 3,
                _ => 0
            };

            int sameCategoryCount = city.Attractions.Count(a =>
                a.Category == attraction.Category || (city.IsPort && a.OwnerId.HasValue));

            return baseCost + sameCategoryCount;
        }
    }

    // Defensive strategy: Minimize losses, protect positions
    public class DefensiveStrategy : BaseAIStrategy
    {
        public override string Name => "Defensive";

        public DefensiveStrategy(Random random = null) : base(random) { }

        public override AIDecision MakeDecision(GameState state, GameEngine engine)
        {
            var decision = new AIDecision();
            var context = new TurnContext();

            decision.SelectedBus = SelectBestBus(state, engine);
            if (decision.SelectedBus == null) return decision;

            var currentCity = state.Board.GetCity(decision.SelectedBus.CurrentCity);

            // Check if Ferry can help avoid other players
            bool useFerry = ShouldUseFerry(state, decision.SelectedBus, currentCity, context);

            // Prefer actions that avoid other players' attractions
            if (!useFerry && currentCity.MorningAction == MorningAction.IgnoreFirstAppeal)
            {
                decision.MorningAction = MorningAction.IgnoreFirstAppeal;
                context.IgnoreNextAppeal = true;
            }
            else if (useFerry)
            {
                decision.MorningAction = MorningAction.Ferry;
            }

            context.UsedMorningAction = decision.MorningAction;

            // Choose destination that minimizes payment to others
            var destinations = engine.GetValidDestinations(decision.SelectedBus, context);
            if (destinations.Any())
            {
                var safestDestination = destinations
                    .Select(d => new
                    {
                        City = d,
                        OtherPlayerIncome = CalculateOtherPlayerIncome(state, decision.SelectedBus, d, context)
                    })
                    .OrderBy(x => x.OtherPlayerIncome)
                    .ThenByDescending(x => EvaluateCity(state, decision.SelectedBus, x.City, context))
                    .First();
                
                decision.DestinationCity = safestDestination.City;
            }

            // All-day action: build attractions in defensive positions
            var arrivalCity = state.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);
            if (arrivalCity?.AllDayAction == AllDayAction.BuildAttraction)
            {
                decision.AllDayAction = AllDayAction.BuildAttraction;
                
                // Build low-cost attractions to block spots
                var cheapestAttraction = state.Market.AvailableAttractions.Values
                    .SelectMany(list => list)
                    .Where(a => a.Category != AttractionCategory.Water || arrivalCity.CanBuildWater)
                    .OrderBy(a => a.Category)
                    .FirstOrDefault();

                if (cheapestAttraction != null)
                {
                    decision.ActionParameters["Attraction"] = cheapestAttraction;
                    decision.ActionParameters["City"] = arrivalCity.Name;
                }
            }

            return decision;
        }

        private int CalculateOtherPlayerIncome(GameState state, Bus bus, string cityName, TurnContext context)
        {
            var city = state.Board.GetCity(cityName);
            if (city == null) return 0;

            int income = 0;
            var otherAttractions = city.Attractions
                .Where(a => a.OwnerId.HasValue && a.OwnerId != state.CurrentPlayer.Id)
                .ToList();

            foreach (var attraction in otherAttractions)
            {
                var eligibleTourists = bus.Tourists
                    .Where(t => context.AllAttractionsAppeal || t.Category == attraction.Category)
                    .Where(t => t.Money >= attraction.Value)
                    .Count();

                if (eligibleTourists > 0)
                    income += attraction.Value;
            }

            return income;
        }
    }

    // Balanced strategy: Mix of offense and defense
    public class BalancedStrategy : BaseAIStrategy
    {
        public override string Name => "Balanced";

        public BalancedStrategy(Random random = null) : base(random) { }

        public override AIDecision MakeDecision(GameState state, GameEngine engine)
        {
            var decision = new AIDecision();
            var context = new TurnContext();

            decision.SelectedBus = SelectBestBus(state, engine);
            if (decision.SelectedBus == null) return decision;

            var currentCity = state.Board.GetCity(decision.SelectedBus.CurrentCity);

            // Check if Ferry provides strategic advantage
            bool useFerry = ShouldUseFerry(state, decision.SelectedBus, currentCity, context);

            // Choose morning action based on situation
            if (useFerry)
            {
                decision.MorningAction = MorningAction.Ferry;
                context.UsedMorningAction = decision.MorningAction;
            }
            else if (currentCity.MorningAction.HasValue)
            {
                var myAttractionCount = state.Board.Cities.Values
                    .SelectMany(c => c.Attractions)
                    .Count(a => a.OwnerId == state.CurrentPlayer.Id);

                // If ahead, play defensively; if behind, play aggressively
                var maxOpponentMoney = state.Players
                    .Where(p => p.Id != state.CurrentPlayer.Id)
                    .Max(p => p.Money);

                if (state.CurrentPlayer.Money > maxOpponentMoney)
                {
                    decision.MorningAction = currentCity.MorningAction;
                }
                else
                {
                    decision.MorningAction = currentCity.MorningAction;
                }

                context.UsedMorningAction = decision.MorningAction;
                if (decision.MorningAction == MorningAction.IncreaseValue)
                    context.IncreaseValue = true;
                if (decision.MorningAction == MorningAction.AllAttractionsAppeal)
                    context.AllAttractionsAppeal = true;
                if (decision.MorningAction == MorningAction.IgnoreFirstAppeal)
                    context.IgnoreNextAppeal = true;
            }

            // Choose balanced destination
            var destinations = engine.GetValidDestinations(decision.SelectedBus, context);
            if (destinations.Any())
            {
                var scoredDestinations = destinations
                    .Select(d => new
                    {
                        City = d,
                        Score = EvaluateCity(state, decision.SelectedBus, d, context)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                decision.DestinationCity = scoredDestinations.First().City;
            }

            // All-day action: balanced approach
            var arrivalCity = state.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);
            if (arrivalCity?.AllDayAction.HasValue == true)
            {
                decision.AllDayAction = arrivalCity.AllDayAction;
                
                if (decision.AllDayAction == AllDayAction.BuildAttraction)
                {
                    var bestAttraction = ChooseBestAttractionBalanced(state, arrivalCity.Name);
                    if (bestAttraction != null)
                    {
                        decision.ActionParameters["Attraction"] = bestAttraction;
                        decision.ActionParameters["City"] = arrivalCity.Name;
                    }
                    else
                    {
                        decision.AllDayAction = null; // Can't afford
                    }
                }
            }

            return decision;
        }

        private Attraction ChooseBestAttractionBalanced(GameState state, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            var availableAttractions = state.Market.AvailableAttractions.Values
                .SelectMany(list => list)
                .Where(a => a.Category != AttractionCategory.Water || city.CanBuildWater)
                .ToList();

            if (!availableAttractions.Any()) return null;

            // Balance between value and cost
            var bestAttraction = availableAttractions
                .Select(a => new
                {
                    Attraction = a,
                    Cost = CalculateAttractionCost(state, a, cityName),
                    Value = a.Value,
                    Score = a.Value - CalculateAttractionCost(state, a, cityName) / 2.0
                })
                .Where(x => x.Cost <= state.CurrentPlayer.Money)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestAttraction?.Attraction;
        }

        private int CalculateAttractionCost(GameState state, Attraction attraction, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            int baseCost = attraction.Category switch
            {
                AttractionCategory.Nature => 1,
                AttractionCategory.Water => 1,
                AttractionCategory.Culture => 2,
                AttractionCategory.Gastronomy => 3,
                _ => 0
            };

            int sameCategoryCount = city.Attractions.Count(a =>
                a.Category == attraction.Category || (city.IsPort && a.OwnerId.HasValue));

            return baseCost + sameCategoryCount;
        }
    }

    // Opportunistic strategy: Exploits game state weaknesses
    public class OpportunisticStrategy : BaseAIStrategy
    {
        public override string Name => "Opportunistic";

        public OpportunisticStrategy(Random random = null) : base(random) { }

        public override AIDecision MakeDecision(GameState state, GameEngine engine)
        {
            var decision = new AIDecision();
            var context = new TurnContext();

            // Look for buses with rich tourists
            var bestBus = state.Board.Buses
                .Where(b => engine.CanBusMove(b))
                .OrderByDescending(b => b.Tourists.Sum(t => t.Money))
                .FirstOrDefault();

            decision.SelectedBus = bestBus;
            if (decision.SelectedBus == null) return decision;

            var currentCity = state.Board.GetCity(decision.SelectedBus.CurrentCity);

            // Check if Ferry opens profitable opportunities
            bool useFerry = ShouldUseFerry(state, decision.SelectedBus, currentCity, context);

            // Use whichever morning action maximizes opportunity
            if (useFerry)
            {
                decision.MorningAction = MorningAction.Ferry;
                context.UsedMorningAction = decision.MorningAction;
            }
            else if (currentCity.MorningAction.HasValue)
            {
                decision.MorningAction = currentCity.MorningAction;
                context.UsedMorningAction = decision.MorningAction;

                if (decision.MorningAction == MorningAction.IncreaseValue)
                    context.IncreaseValue = true;
                if (decision.MorningAction == MorningAction.AllAttractionsAppeal)
                    context.AllAttractionsAppeal = true;
                if (decision.MorningAction == MorningAction.IgnoreFirstAppeal)
                    context.IgnoreNextAppeal = true;
            }

            // Find city with best exploitation potential
            var destinations = engine.GetValidDestinations(decision.SelectedBus, context);
            if (destinations.Any())
            {
                var bestOpportunity = destinations
                    .Select(d => new
                    {
                        City = d,
                        Opportunity = CalculateOpportunity(state, decision.SelectedBus, d, context)
                    })
                    .OrderByDescending(x => x.Opportunity)
                    .First();

                decision.DestinationCity = bestOpportunity.City;
            }

            // Take any valuable all-day action
            var arrivalCity = state.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);
            if (arrivalCity?.AllDayAction.HasValue == true)
            {
                decision.AllDayAction = arrivalCity.AllDayAction;
                
                if (decision.AllDayAction == AllDayAction.BuildAttraction)
                {
                    // Build high-value attractions in high-traffic areas
                    var bestAttraction = ChooseOpportunisticAttraction(state, arrivalCity.Name);
                    if (bestAttraction != null)
                    {
                        decision.ActionParameters["Attraction"] = bestAttraction;
                        decision.ActionParameters["City"] = arrivalCity.Name;
                    }
                    else
                    {
                        decision.AllDayAction = null;
                    }
                }
            }

            return decision;
        }

        private double CalculateOpportunity(GameState state, Bus bus, string cityName, TurnContext context)
        {
            var city = state.Board.GetCity(cityName);
            if (city == null) return 0;

            double opportunity = 0;

            // My attractions
            var myAttractions = city.Attractions
                .Where(a => a.OwnerId == state.CurrentPlayer.Id)
                .ToList();

            foreach (var attr in myAttractions)
            {
                var richTourists = bus.Tourists
                    .Where(t => context.AllAttractionsAppeal || t.Category == attr.Category)
                    .Where(t => t.Money >= attr.Value)
                    .OrderByDescending(t => t.Money)
                    .ToList();

                if (richTourists.Any())
                {
                    // Bonus for extracting maximum value
                    opportunity += richTourists.First().Money * 1.5;
                }
            }

            // Bonus for all-day actions
            if (city.AllDayAction == AllDayAction.BuildAttraction && state.CurrentPlayer.Money >= 3)
            {
                opportunity += 10;
            }

            return opportunity;
        }

        private Attraction ChooseOpportunisticAttraction(GameState state, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            
            // Prefer high-value attractions
            var highValueAttraction = state.Market.AvailableAttractions.Values
                .SelectMany(list => list)
                .Where(a => a.Category != AttractionCategory.Water || city.CanBuildWater)
                .Where(a => CalculateAttractionCost(state, a, cityName) <= state.CurrentPlayer.Money)
                .OrderByDescending(a => a.Value)
                .FirstOrDefault();

            return highValueAttraction;
        }

        private int CalculateAttractionCost(GameState state, Attraction attraction, string cityName)
        {
            var city = state.Board.GetCity(cityName);
            int baseCost = attraction.Category switch
            {
                AttractionCategory.Nature => 1,
                AttractionCategory.Water => 1,
                AttractionCategory.Culture => 2,
                AttractionCategory.Gastronomy => 3,
                _ => 0
            };

            int sameCategoryCount = city.Attractions.Count(a =>
                a.Category == attraction.Category || (city.IsPort && a.OwnerId.HasValue));

            return baseCost + sameCategoryCount;
        }
    }

    public class AIController
    {
        private Dictionary<string, IAIStrategy> _strategies;
        private Random _random;

        public AIController(Random random = null)
        {
            _random = random ?? new Random();
            _strategies = new Dictionary<string, IAIStrategy>
            {
                { "Aggressive", new AggressiveStrategy(_random) },
                { "Defensive", new DefensiveStrategy(_random) },
                { "Balanced", new BalancedStrategy(_random) },
                { "Opportunistic", new OpportunisticStrategy(_random) }
            };
        }

        public AIDecision GetAIDecision(GameState state, GameEngine engine, string strategyName)
        {
            if (!_strategies.ContainsKey(strategyName))
            {
                strategyName = "Balanced"; // Default
            }

            return _strategies[strategyName].MakeDecision(state, engine);
        }

        public List<string> GetAvailableStrategies()
        {
            return new List<string>(_strategies.Keys);
        }
    }
}
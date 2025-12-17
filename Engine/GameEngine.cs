using System;
using System.Collections.Generic;
using System.Linq;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.Engine
{
    public class TurnContext
    {
        public Bus SelectedBus { get; set; }
        public string StartCity { get; set; }
        public MorningAction? UsedMorningAction { get; set; }
        public bool UsedAllDayAction { get; set; }
        public bool HasMoved { get; set; }
        public List<string> VisitedAttractions { get; set; }
        public int TouristsRuined { get; set; }
        public bool IgnoreNextAppeal { get; set; }
        public bool AllAttractionsAppeal { get; set; }
        public bool IncreaseValue { get; set; }

        public TurnContext()
        {
            VisitedAttractions = new List<string>();
        }
    }

    public class GameEngine
    {
        private GameState _state;

        public GameEngine(GameState state)
        {
            _state = state;
        }

        // Check if a bus can legally move (considering one bus per city rule)
        public bool CanBusMove(Bus bus)
        {
            var city = _state.Board.GetCity(bus.CurrentCity);
            if (city == null) return false;

            var busCategories = bus.GetCategories();
            if (busCategories.Count == 0) return false;

            // Check if any valid destination exists (considering all possible morning actions)
            var normalContext = new TurnContext();
            var normalDests = GetValidDestinations(bus, normalContext);
            if (normalDests.Any()) return true;

            // Check with Ignore First Appeal
            var ignoreContext = new TurnContext { IgnoreNextAppeal = true };
            var ignoreDests = GetValidDestinations(bus, ignoreContext);
            if (ignoreDests.Any()) return true;

            // Check with Ferry (if in port)
            if (city.IsPort)
            {
                var ferryContext = new TurnContext { UsedMorningAction = MorningAction.Ferry };
                var ferryDests = GetValidDestinations(bus, ferryContext);
                if (ferryDests.Any()) return true;
            }

            return false;
        }

        // Get valid destination cities for a bus
        public List<string> GetValidDestinations(Bus bus, TurnContext context)
        {
            var destinations = new List<string>();
            var city = _state.Board.GetCity(bus.CurrentCity);
            if (city == null) return destinations;

            var busCategories = bus.GetCategories();

            if (context.UsedMorningAction == MorningAction.Ferry && city.IsPort)
            {
                // Can move to any other port
                foreach (var portCity in _state.Board.Cities.Values.Where(c => c.IsPort && c.Name != city.Name))
                {
                    if (!_state.Board.Buses.Any(b => b.Id != bus.Id && b.CurrentCity == portCity.Name))
                    {
                        destinations.Add(portCity.Name);
                    }
                }
                return destinations;
            }

            // Normal movement along routes
            var visited = new HashSet<string> { bus.CurrentCity };
            var queue = new Queue<string>();
            queue.Enqueue(bus.CurrentCity);
            bool firstAppealFound = false;

            while (queue.Count > 0)
            {
                var currentCityName = queue.Dequeue();
                var currentCity = _state.Board.GetCity(currentCityName);

                foreach (var nextCityName in currentCity.Connections)
                {
                    if (visited.Contains(nextCityName)) continue;

                    var nextCity = _state.Board.GetCity(nextCityName);
                    if (nextCity == null) continue;

                    // Check if occupied
                    if (_state.Board.Buses.Any(b => b.Id != bus.Id && b.CurrentCity == nextCityName))
                        continue;

                    // Check appeal based on settings
                    bool hasAppeal;
                    if (_state.Settings.UseAppealSystem)
                    {
                        hasAppeal = nextCity.HasAppeal(busCategories);
                    }
                    else
                    {
                        // No appeal system: all cities are valid stops
                        hasAppeal = true;
                    }

                    if (hasAppeal)
                    {
                        if (context.IgnoreNextAppeal && !firstAppealFound)
                        {
                            firstAppealFound = true;
                            visited.Add(nextCityName);
                            queue.Enqueue(nextCityName);
                            continue;
                        }

                        destinations.Add(nextCityName);
                        // Don't continue past this city
                    }
                    else
                    {
                        // No appeal, can pass through
                        visited.Add(nextCityName);
                        queue.Enqueue(nextCityName);
                    }
                }
            }

            return destinations;
        }

        // Execute a tour for tourists on a bus
        public TourResult GiveBusTour(Bus bus, TurnContext context)
        {
            var result = new TourResult();
            var city = _state.Board.GetCity(bus.CurrentCity);
            if (city == null) return result;

            // Get attractions sorted by priority (HIGH priority first)
            var attractions = city.Attractions
                .Where(a => a.OwnerId.HasValue) // Only visit built attractions
                .OrderByDescending(a => a.Priority)
                .ThenBy(a => a.Id) // Stable sort for determinism
                .ToList();

            var visitedAttractionIds = new HashSet<int>();

            foreach (var attraction in attractions)
            {
                if (visitedAttractionIds.Contains(attraction.Id)) continue;

                // Find tourists that match this attraction's category
                var eligibleTourists = bus.Tourists
                    .Where(t => context.AllAttractionsAppeal || t.Category == attraction.Category)
                    .Where(t => t.Money >= attraction.Value)
                    .ToList();

                if (eligibleTourists.Count == 0) continue;

                // Choose first eligible tourist (or AI could decide)
                var tourist = eligibleTourists.First();
                
                // Tourist visits attraction
                tourist.Money -= attraction.Value;
                
                int earnedMoney = attraction.Value;
                if (context.IncreaseValue) earnedMoney += _state.Settings.IncreaseValueBonus;

                var owner = _state.Players.FirstOrDefault(p => p.Id == attraction.OwnerId);
                if (owner != null)
                {
                    owner.Money += earnedMoney;
                    result.MoneyEarned[owner.Id] = result.MoneyEarned.GetValueOrDefault(owner.Id, 0) + earnedMoney;
                }

                result.AttractionsVisited.Add(attraction.Id);
                visitedAttractionIds.Add(attraction.Id);

                // Check if tourist is ruined
                if (tourist.Money == 0)
                {
                    bus.Tourists.Remove(tourist);

                    // BOTH owner AND active player get paid when tourist is ruined
                    if (owner != null)
                    {
                        owner.Money += earnedMoney;
                        result.MoneyEarned[owner.Id] = result.MoneyEarned.GetValueOrDefault(owner.Id, 0) + earnedMoney;
                    }
                    _state.CurrentPlayer.Money += earnedMoney;
                    result.MoneyEarned[_state.CurrentPlayer.Id] = result.MoneyEarned.GetValueOrDefault(_state.CurrentPlayer.Id, 0) + earnedMoney;

                    result.TouristsRuined++;
                    result.MoneyFromRuinedTourists += earnedMoney * 2; // Both owner and active player paid
                }
            }

            return result;
        }

        // Execute single tourist tour (for Give Tour action)
        public TourResult GiveSingleTouristTour(Bus bus, Tourist tourist, string cityName)
        {
            var result = new TourResult();
            var city = _state.Board.GetCity(cityName);
            if (city == null) return result;

            var attractions = city.Attractions
                .Where(a => a.OwnerId.HasValue)
                .Where(a => a.Category == tourist.Category)
                .Where(a => tourist.Money >= a.Value)
                .OrderByDescending(a => a.Priority) // HIGH priority first
                .ToList();

            foreach (var attraction in attractions)
            {
                if (tourist.Money < attraction.Value) break;

                tourist.Money -= attraction.Value;

                int earnedMoney = attraction.GetPayment(false, _state.Settings.UseBonusEuro);

                var owner = _state.Players.FirstOrDefault(p => p.Id == attraction.OwnerId);
                if (owner != null)
                {
                    owner.Money += earnedMoney;
                    result.MoneyEarned[owner.Id] = result.MoneyEarned.GetValueOrDefault(owner.Id, 0) + earnedMoney;
                }

                result.AttractionsVisited.Add(attraction.Id);

                if (tourist.Money == 0)
                {
                    bus.Tourists.Remove(tourist);
                    
                    // BOTH owner AND active player get paid
                    if (owner != null)
                    {
                        owner.Money += earnedMoney;
                        result.MoneyEarned[owner.Id] = result.MoneyEarned.GetValueOrDefault(owner.Id, 0) + earnedMoney;
                    }
                    _state.CurrentPlayer.Money += earnedMoney;
                    result.MoneyEarned[_state.CurrentPlayer.Id] = result.MoneyEarned.GetValueOrDefault(_state.CurrentPlayer.Id, 0) + earnedMoney;
                    
                    result.TouristsRuined++;
                    result.MoneyFromRuinedTourists += earnedMoney * 2;
                    break;
                }
            }

            return result;
        }

        // Build an attraction (with optional discount from Contractor)
        public bool BuildAttraction(Attraction attraction, string cityName, int discount = 0)
        {
            var city = _state.Board.GetCity(cityName);
            if (city == null) return false;

            // Check if water attraction can be built
            if (attraction.Category == AttractionCategory.Water && !city.CanBuildWater)
                return false;

            // Check if city has space for more attractions
            if (city.Attractions.Count(a => a.OwnerId.HasValue) >= city.MaxAttractionSpaces)
                return false;

            // Calculate cost
            int totalCost;
            if (attraction.Category == AttractionCategory.Gray)
            {
                // Gray attractions have fixed cost - no category count
                totalCost = _state.Settings.GrayBaseCost;
            }
            else
            {
                int baseCost = _state.Settings.GetBaseCost(attraction.Category);
                
                // Count same category attractions (gray doesn't count as any category)
                int sameCategoryCount = city.Attractions.Count(a => 
                    a.OwnerId.HasValue && 
                    a.Category != AttractionCategory.Gray &&
                    (a.Category == attraction.Category || city.IsPort));
                
                totalCost = baseCost + sameCategoryCount;
            }

            // Apply discount (from Contractor)
            totalCost = Math.Max(0, totalCost - discount);

            if (_state.CurrentPlayer.Money < totalCost) return false;

            // Buy and place attraction
            _state.CurrentPlayer.Money -= totalCost;
            attraction.OwnerId = _state.CurrentPlayer.Id;
            city.Attractions.Add(attraction);

            // Remove from market
            _state.Market.AvailableAttractions[attraction.Category].Remove(attraction);

            return true;
        }

        // Refill phase
        public void Refill(TurnContext context)
        {
            // Refill attractions
            foreach (var category in context.VisitedAttractions
                .Select(id => _state.Board.Cities.Values
                    .SelectMany(c => c.Attractions)
                    .FirstOrDefault(a => a.Id.ToString() == id)?.Category)
                .Where(c => c.HasValue)
                .Select(c => c.Value)
                .Distinct())
            {
                _state.Market.Refill(category, _state.Settings.MarketRefillAmount);
            }

            // Refill tourists based on mode
            bool shouldRefill = false;
            int touristsToRefill = 0;

            if (_state.Settings.TouristRefillMode == GameSettings.RefillMode.FillMissing)
            {
                // Fill any missing slots
                touristsToRefill = Math.Min(context.TouristsRuined, _state.Settings.MaxTouristsPerBus - context.SelectedBus.Tourists.Count);
                shouldRefill = touristsToRefill > 0;
            }
            else // FillWhenEmpty
            {
                // Only fill if bus is completely empty
                if (context.SelectedBus.Tourists.Count == 0)
                {
                    touristsToRefill = _state.Settings.MaxTouristsPerBus;
                    shouldRefill = true;
                }
            }

            if (shouldRefill)
            {
                for (int i = 0; i < touristsToRefill; i++)
                {
                    if (_state.TouristPool.Count == 0) break;

                    var tourist = _state.TouristPool.Dequeue();
                    int roll;
                    do
                    {
                        roll = _state.Random.Next(2, 7); // 2-6 (avoiding 1)
                    } while (roll == 1);

                    tourist.Money = roll;
                    context.SelectedBus.Tourists.Add(tourist);
                }
            }
        }

        // Check if game should end
        public void CheckGameEnd()
        {
            if (_state.IsGameOver())
            {
                _state.GameEnded = true;
            }
        }
    }

    public class TourResult
    {
        public List<int> AttractionsVisited { get; set; } = new List<int>();
        public Dictionary<int, int> MoneyEarned { get; set; } = new Dictionary<int, int>();
        public int TouristsRuined { get; set; }
        public int MoneyFromRuinedTourists { get; set; }
    }
}
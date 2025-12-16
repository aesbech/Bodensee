using System;
using System.Collections.Generic;
using System.Linq;

namespace BodenseeTourismus.Core
{
    // Enums
    public enum AttractionCategory
    {
        Nature = 0,    // Green
        Water = 1,     // Blue
        Culture = 2,   // Red
        Gastronomy = 3, // Yellow
        Gray = 4       // Gray - special, no appeal, no category count
    }

    public enum MorningAction
    {
        None,
        IncreaseValue,
        IgnoreFirstAppeal,
        Ferry,
        AllAttractionsAppeal
    }

    public enum AllDayAction
    {
        None,
        BuildAttraction,
        RerollTourist,
        AddTwoPips,
        GiveTour,
        AddTwoPipsGreen,      // Color-specific zentrum actions
        AddTwoPipsBlue,
        AddTwoPipsRed,
        AddTwoPipsYellow,
        BusDispatch,          // Move another bus
        BuildAttractionDiscount // Contractor - build attraction with €1 discount
    }

    // Game settings for testing and balancing
    public class GameSettings
    {
        // Starting money
        public int Player1StartMoney { get; set; } = 6;
        public int Player2StartMoney { get; set; } = 7;
        public int Player3StartMoney { get; set; } = 8;
        public int Player4StartMoney { get; set; } = 9;
        
        // Attraction costs
        public int NatureBaseCost { get; set; } = 1;
        public int WaterBaseCost { get; set; } = 1;
        public int CultureBaseCost { get; set; } = 2;
        public int GastronomyBaseCost { get; set; } = 3;
        public int GrayBaseCost { get; set; } = 2; // Gray attractions
        
        // Appeal system
        public bool UseAppealSystem { get; set; } = true; // If false, all cities have appeal
        
        // Refill system
        public enum RefillMode
        {
            FillMissing,    // Refill any empty tourist slots
            FillWhenEmpty   // Only refill when bus has 0 tourists
        }
        public RefillMode TouristRefillMode { get; set; } = RefillMode.FillMissing;
        
        // Starting tourists per bus
        public int StartingTouristsPerBus { get; set; } = 4;
        
        // Max tourists per bus
        public int MaxTouristsPerBus { get; set; } = 4;
        
        // Increase value bonus
        public int IncreaseValueBonus { get; set; } = 1;
        
        // Enable gray attractions
        public bool EnableGrayAttractions { get; set; } = true;
        
        // Gray attraction settings
        public int ZentrumPipsBonus { get; set; } = 1; // How many pips Zentrums add (matching CSV specification)
        public int CasinoRerollsPerBus { get; set; } = 1; // How many tourists can be rerolled per bus
        
        // Bonus euro feature
        public bool UseBonusEuro { get; set; } = true; // Some attractions pay €1 more than pips shown
        
        // Give Tour settings
        public bool GiveTourAffectsWholeBus { get; set; } = false; // If true, Give Tour affects all tourists
        
        // Manual attraction order
        public bool ManualAttractionOrder { get; set; } = false; // If true, player chooses attraction visit order
        
        // Language settings
        public enum AttractionLanguage
        {
            English,
            German
        }
        public AttractionLanguage Language { get; set; } = AttractionLanguage.German;

        public int GetBaseCost(AttractionCategory category)
        {
            return category switch
            {
                AttractionCategory.Nature => NatureBaseCost,
                AttractionCategory.Water => WaterBaseCost,
                AttractionCategory.Culture => CultureBaseCost,
                AttractionCategory.Gastronomy => GastronomyBaseCost,
                _ => 0
            };
        }

        public int GetStartMoney(int playerIndex)
        {
            return playerIndex switch
            {
                0 => Player1StartMoney,
                1 => Player2StartMoney,
                2 => Player3StartMoney,
                3 => Player4StartMoney,
                _ => Player1StartMoney + playerIndex
            };
        }

        // Export settings for analytics
        public Dictionary<string, object> ExportForAnalytics()
        {
            return new Dictionary<string, object>
            {
                // Starting money
                { "Player1StartMoney", Player1StartMoney },
                { "Player2StartMoney", Player2StartMoney },
                { "Player3StartMoney", Player3StartMoney },
                { "Player4StartMoney", Player4StartMoney },

                // Attraction costs
                { "NatureBaseCost", NatureBaseCost },
                { "WaterBaseCost", WaterBaseCost },
                { "CultureBaseCost", CultureBaseCost },
                { "GastronomyBaseCost", GastronomyBaseCost },
                { "GrayBaseCost", GrayBaseCost },

                // Game mechanics
                { "UseAppealSystem", UseAppealSystem },
                { "TouristRefillMode", TouristRefillMode.ToString() },
                { "StartingTouristsPerBus", StartingTouristsPerBus },
                { "MaxTouristsPerBus", MaxTouristsPerBus },
                { "IncreaseValueBonus", IncreaseValueBonus },

                // Gray attractions
                { "EnableGrayAttractions", EnableGrayAttractions },
                { "ZentrumPipsBonus", ZentrumPipsBonus },
                { "CasinoRerollsPerBus", CasinoRerollsPerBus },

                // Bonus features
                { "UseBonusEuro", UseBonusEuro },
                { "GiveTourAffectsWholeBus", GiveTourAffectsWholeBus },
                { "ManualAttractionOrder", ManualAttractionOrder },

                // Language
                { "Language", Language.ToString() }
            };
        }

        // Import settings from dictionary
        public void ImportFromDictionary(Dictionary<string, object> dict)
        {
            // Starting money
            if (dict.ContainsKey("Player1StartMoney")) Player1StartMoney = Convert.ToInt32(dict["Player1StartMoney"]);
            if (dict.ContainsKey("Player2StartMoney")) Player2StartMoney = Convert.ToInt32(dict["Player2StartMoney"]);
            if (dict.ContainsKey("Player3StartMoney")) Player3StartMoney = Convert.ToInt32(dict["Player3StartMoney"]);
            if (dict.ContainsKey("Player4StartMoney")) Player4StartMoney = Convert.ToInt32(dict["Player4StartMoney"]);

            // Attraction costs
            if (dict.ContainsKey("NatureBaseCost")) NatureBaseCost = Convert.ToInt32(dict["NatureBaseCost"]);
            if (dict.ContainsKey("WaterBaseCost")) WaterBaseCost = Convert.ToInt32(dict["WaterBaseCost"]);
            if (dict.ContainsKey("CultureBaseCost")) CultureBaseCost = Convert.ToInt32(dict["CultureBaseCost"]);
            if (dict.ContainsKey("GastronomyBaseCost")) GastronomyBaseCost = Convert.ToInt32(dict["GastronomyBaseCost"]);
            if (dict.ContainsKey("GrayBaseCost")) GrayBaseCost = Convert.ToInt32(dict["GrayBaseCost"]);

            // Game mechanics
            if (dict.ContainsKey("UseAppealSystem")) UseAppealSystem = Convert.ToBoolean(dict["UseAppealSystem"]);
            if (dict.ContainsKey("TouristRefillMode"))
            {
                if (Enum.TryParse<RefillMode>(dict["TouristRefillMode"].ToString(), out var refillMode))
                    TouristRefillMode = refillMode;
            }
            if (dict.ContainsKey("StartingTouristsPerBus")) StartingTouristsPerBus = Convert.ToInt32(dict["StartingTouristsPerBus"]);
            if (dict.ContainsKey("MaxTouristsPerBus")) MaxTouristsPerBus = Convert.ToInt32(dict["MaxTouristsPerBus"]);
            if (dict.ContainsKey("IncreaseValueBonus")) IncreaseValueBonus = Convert.ToInt32(dict["IncreaseValueBonus"]);

            // Gray attractions
            if (dict.ContainsKey("EnableGrayAttractions")) EnableGrayAttractions = Convert.ToBoolean(dict["EnableGrayAttractions"]);
            if (dict.ContainsKey("ZentrumPipsBonus")) ZentrumPipsBonus = Convert.ToInt32(dict["ZentrumPipsBonus"]);
            if (dict.ContainsKey("CasinoRerollsPerBus")) CasinoRerollsPerBus = Convert.ToInt32(dict["CasinoRerollsPerBus"]);

            // Bonus features
            if (dict.ContainsKey("UseBonusEuro")) UseBonusEuro = Convert.ToBoolean(dict["UseBonusEuro"]);
            if (dict.ContainsKey("GiveTourAffectsWholeBus")) GiveTourAffectsWholeBus = Convert.ToBoolean(dict["GiveTourAffectsWholeBus"]);
            if (dict.ContainsKey("ManualAttractionOrder")) ManualAttractionOrder = Convert.ToBoolean(dict["ManualAttractionOrder"]);

            // Language
            if (dict.ContainsKey("Language"))
            {
                if (Enum.TryParse<AttractionLanguage>(dict["Language"].ToString(), out var language))
                    Language = language;
            }
        }
    }

    // Core Models
    public class Tourist
    {
        public int Id { get; set; }
        public AttractionCategory Category { get; set; }
        public int Money { get; set; } // Pips on dice
        
        public Tourist(int id, AttractionCategory category, int money)
        {
            Id = id;
            Category = category;
            Money = money;
        }

        public Tourist Clone()
        {
            return new Tourist(Id, Category, Money);
        }
    }

    public class Attraction
    {
        public int Id { get; set; }
        public string NameEnglish { get; set; }
        public string NameGerman { get; set; }
        public AttractionCategory Category { get; set; }
        public int Value { get; set; } // Pips shown
        public int Priority { get; set; }
        public int? OwnerId { get; set; } // Player who built it, null if base attraction
        public AllDayAction? GrantedAction { get; set; } // Action granted by gray attraction
        public bool PaysBonusEuro { get; set; } // Pays €1 more than pips (but still removes pips shown)
        
        public bool IsGrayAttraction => Category == AttractionCategory.Gray;
        
        public string GetName(GameSettings.AttractionLanguage language)
        {
            return language == GameSettings.AttractionLanguage.English ? NameEnglish : NameGerman;
        }
        
        public int GetPayment(bool increasedValue, bool useBonusEuro)
        {
            int payment = Value;
            if (PaysBonusEuro && useBonusEuro) payment += 1;
            if (increasedValue) payment += 1;
            return payment;
        }
        
        public Attraction(int id, string nameEnglish, string nameGerman, AttractionCategory category, int value, int priority, bool paysBonusEuro = false, AllDayAction? grantedAction = null)
        {
            Id = id;
            NameEnglish = nameEnglish;
            NameGerman = nameGerman;
            Category = category;
            Value = value;
            Priority = priority;
            PaysBonusEuro = paysBonusEuro;
            OwnerId = null;
            GrantedAction = grantedAction;
        }

        public Attraction Clone()
        {
            return new Attraction(Id, NameEnglish, NameGerman, Category, Value, Priority, PaysBonusEuro, GrantedAction) { OwnerId = OwnerId };
        }
    }

    public class City
    {
        public string Name { get; set; }
        public bool IsPort { get; set; }
        public bool CanBuildWater { get; set; }
        public int MaxAttractionSpaces { get; set; }
        public MorningAction? MorningAction { get; set; }
        public AllDayAction? AllDayAction { get; set; }
        public List<string> Connections { get; set; }
        public List<Attraction> Attractions { get; set; }

        public City(string name, bool isPort = false, bool canBuildWater = false)
        {
            Name = name;
            IsPort = isPort;
            CanBuildWater = canBuildWater;
            MaxAttractionSpaces = 3; // Default
            Connections = new List<string>();
            Attractions = new List<Attraction>();
        }

        public bool HasAppeal(List<AttractionCategory> busCategories)
        {
            if (IsPort) return true; // Ports appeal to all
            // Gray attractions don't contribute to appeal
            return Attractions.Any(a => a.Category != AttractionCategory.Gray && busCategories.Contains(a.Category));
        }

        public List<AllDayAction> GetAvailableAllDayActions()
        {
            var actions = new List<AllDayAction>();
            
            // City's built-in action
            if (AllDayAction.HasValue)
            {
                actions.Add(AllDayAction.Value);
            }
            
            // Actions from gray attractions
            foreach (var attraction in Attractions.Where(a => a.Category == AttractionCategory.Gray && a.OwnerId.HasValue && a.GrantedAction.HasValue))
            {
                actions.Add(attraction.GrantedAction.Value);
            }
            
            return actions;
        }
    }

    public class Bus
    {
        public int Id { get; set; }
        public string CurrentCity { get; set; }
        public List<Tourist> Tourists { get; set; }
        
        public Bus(int id, string startCity)
        {
            Id = id;
            CurrentCity = startCity;
            Tourists = new List<Tourist>();
        }

        public List<AttractionCategory> GetCategories()
        {
            return Tourists.Select(t => t.Category).Distinct().ToList();
        }

        public Bus Clone()
        {
            var bus = new Bus(Id, CurrentCity);
            bus.Tourists = Tourists.Select(t => t.Clone()).ToList();
            return bus;
        }
    }

    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Money { get; set; }
        public bool IsAI { get; set; }
        public string AIStrategy { get; set; } // For AI players
        
        public Player(int id, string name, int startMoney, bool isAI = false, string aiStrategy = null)
        {
            Id = id;
            Name = name;
            Money = startMoney;
            IsAI = isAI;
            AIStrategy = aiStrategy;
        }

        public int GetAttractionCount(GameState gameState)
        {
            return gameState.Board.Cities.Values
                .SelectMany(c => c.Attractions)
                .Count(a => a.OwnerId == Id);
        }
    }

    public class MarketPlace
    {
        public Dictionary<AttractionCategory, List<Attraction>> AvailableAttractions { get; set; }
        public Dictionary<AttractionCategory, Queue<Attraction>> Decks { get; set; }

        public MarketPlace()
        {
            AvailableAttractions = new Dictionary<AttractionCategory, List<Attraction>>();
            Decks = new Dictionary<AttractionCategory, Queue<Attraction>>();
            
            foreach (AttractionCategory category in Enum.GetValues(typeof(AttractionCategory)))
            {
                AvailableAttractions[category] = new List<Attraction>();
                Decks[category] = new Queue<Attraction>();
            }
        }

        public void Refill(AttractionCategory category)
        {
            while (AvailableAttractions[category].Count < 2 && Decks[category].Count > 0)
            {
                AvailableAttractions[category].Add(Decks[category].Dequeue());
            }
        }

        public void RefillGray()
        {
            // Gray attractions always show all available (no 2-card limit)
            AvailableAttractions[AttractionCategory.Gray].Clear();
            foreach (var attraction in Decks[AttractionCategory.Gray])
            {
                AvailableAttractions[AttractionCategory.Gray].Add(attraction);
            }
        }
    }

    public class Board
    {
        public Dictionary<string, City> Cities { get; set; }
        public List<Bus> Buses { get; set; }

        public Board()
        {
            Cities = new Dictionary<string, City>();
            Buses = new List<Bus>();
        }

        public City GetCity(string name)
        {
            return Cities.ContainsKey(name) ? Cities[name] : null;
        }
    }

    public class GameState
    {
        public Board Board { get; set; }
        public MarketPlace Market { get; set; }
        public List<Player> Players { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public bool GameEnded { get; set; }
        public Queue<Tourist> TouristPool { get; set; }
        public Random Random { get; set; }
        public GameSettings Settings { get; set; }
        
        public Player CurrentPlayer => Players[CurrentPlayerIndex];

        public GameState()
        {
            Board = new Board();
            Market = new MarketPlace();
            Players = new List<Player>();
            CurrentPlayerIndex = 0;
            GameEnded = false;
            TouristPool = new Queue<Tourist>();
            Random = new Random();
            Settings = new GameSettings(); // Default settings
        }

        public void NextPlayer()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }

        public bool IsGameOver()
        {
            // Game ends when tourist pool is empty or any category runs out
            if (TouristPool.Count == 0) return true;
            
            foreach (var category in Market.Decks.Keys)
            {
                if (Market.Decks[category].Count == 0 && Market.AvailableAttractions[category].Count == 0)
                    return true;
            }
            
            return false;
        }

        public Player GetWinner()
        {
            if (!GameEnded) return null;
            
            var maxMoney = Players.Max(p => p.Money);
            var richestPlayers = Players.Where(p => p.Money == maxMoney).ToList();
            
            if (richestPlayers.Count == 1) return richestPlayers[0];
            
            // Tiebreaker: most attractions
            var maxAttractions = richestPlayers.Max(p => p.GetAttractionCount(this));
            var winners = richestPlayers.Where(p => p.GetAttractionCount(this) == maxAttractions).ToList();
            
            return winners.First(); // If still tied, first player wins (or could implement race)
        }

        public GameState Clone()
        {
            var clone = new GameState
            {
                CurrentPlayerIndex = this.CurrentPlayerIndex,
                GameEnded = this.GameEnded,
                Random = this.Random // Share same random for consistency
            };

            // Clone players
            clone.Players = Players.Select(p => new Player(p.Id, p.Name, p.Money, p.IsAI, p.AIStrategy)).ToList();

            // Clone board
            clone.Board = new Board();
            foreach (var city in Board.Cities.Values)
            {
                var cityClone = new City(city.Name, city.IsPort, city.CanBuildWater)
                {
                    MorningAction = city.MorningAction,
                    AllDayAction = city.AllDayAction
                };
                cityClone.Connections.AddRange(city.Connections);
                cityClone.Attractions.AddRange(city.Attractions.Select(a => a.Clone()));
                clone.Board.Cities[city.Name] = cityClone;
            }

            // Clone buses
            clone.Board.Buses = Board.Buses.Select(b => b.Clone()).ToList();

            // Clone market (simplified - just count remaining)
            clone.Market = new MarketPlace();
            foreach (var category in Market.AvailableAttractions.Keys)
            {
                clone.Market.AvailableAttractions[category] = new List<Attraction>(Market.AvailableAttractions[category]);
                foreach (var attr in Market.Decks[category])
                {
                    clone.Market.Decks[category].Enqueue(attr);
                }
            }

            // Clone tourist pool
            clone.TouristPool = new Queue<Tourist>(TouristPool.Select(t => t.Clone()));

            return clone;
        }
    }
}
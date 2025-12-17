using System;
using System.Collections.Generic;
using System.Linq;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.Setup
{
    public class GameSetup
    {
        private Random _random;

        public GameSetup(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public GameState CreateGame(List<(string Name, bool IsAI, string AIStrategy)> playerConfigs, GameSettings settings = null)
        {
            var state = new GameState { Random = _random };
            
            // Apply settings
            if (settings != null)
            {
                state.Settings = settings;
            }

            // Initialize board with exact layout from image
            InitializeBoard(state);

            // Initialize players
            for (int i = 0; i < playerConfigs.Count; i++)
            {
                var config = playerConfigs[i];
                int startMoney = state.Settings.GetStartMoney(i);
                
                var player = new Player(i, config.Name, startMoney, config.IsAI, config.AIStrategy);
                state.Players.Add(player);
            }

            // Initialize market
            InitializeMarket(state);

            // Initialize tourist pool
            InitializeTouristPool(state);

            // Place buses and tourists
            PlaceBusesAndTourists(state);

            return state;
        }

        private void InitializeBoard(GameState state)
        {
            // Create all cities with exact data from the board image
            // Format: CityName, IsPort, CanBuildWater, AttractionSpaces, MorningAction, AllDayAction, Connections
            
            var cityData = new Dictionary<string, (bool IsPort, bool CanBuildWater, int AttractionSpaces, MorningAction? Morning, AllDayAction? AllDay, List<string> Connections)>
            {
                // Northwest section
                { "Bodman-Ludwigshafen", (false, false, 3, MorningAction.IncreaseValue, AllDayAction.BuildAttraction, 
                    new List<string> { "Überlingen", "Radolfzelt" }) },
                
                { "Überlingen", (false, false, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.AddTwoPips, 
                    new List<string> { "Bodman-Ludwigshafen", "Meersburg", "Ravensburg" }) },
                
                { "Radolfzelt", (false, false, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.RerollTourist, 
                    new List<string> { "Bodman-Ludwigshafen", "Konstanz" }) },
                
                { "Konstanz", (true, true, 4, MorningAction.Ferry, AllDayAction.GiveTour, 
                    new List<string> { "Radolfzelt", "Kreuzlingen" }) },
                
                { "Kreuzlingen", (true, true, 4, MorningAction.AllAttractionsAppeal, AllDayAction.BuildAttraction, 
                    new List<string> { "Konstanz", "Weinfelden" }) },
                
                // North central section
                { "Ravensburg", (false, false, 3, MorningAction.AllAttractionsAppeal, AllDayAction.GiveTour, 
                    new List<string> { "Überlingen", "Wangen" }) },
                
                { "Wangen", (false, false, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.BuildAttraction, 
                    new List<string> { "Ravensburg", "Lindau" }) },
                
                // Lake section (ports) - ONLY solid red line connections (dotted lines are ferry-only)
                { "Meersburg", (true, true, 3, MorningAction.Ferry, AllDayAction.BuildAttraction,
                    new List<string> { "Überlingen" }) },

                { "Friedrichshafen", (true, true, 5, MorningAction.Ferry, AllDayAction.GiveTour,
                    new List<string> { }) },  // Ferry-only access - no solid road connections!

                { "Lindau", (true, true, 4, MorningAction.Ferry, AllDayAction.RerollTourist,
                    new List<string> { "Wangen", "Bregenz" }) },
                
                { "Bregenz", (true, true, 3, MorningAction.IncreaseValue, AllDayAction.GiveTour, 
                    new List<string> { "Lindau", "Hard", "Dornbirn" }) },
                
                { "Hard", (true, true, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.RerollTourist, 
                    new List<string> { "Bregenz", "Rorschach" }) },
                
                { "Rorschach", (true, true, 3, MorningAction.Ferry, AllDayAction.AddTwoPips, 
                    new List<string> { "Hard", "St. Gallen", "Arbon" }) },
                
                { "Arbon", (true, true, 3, MorningAction.IncreaseValue, AllDayAction.BuildAttraction, 
                    new List<string> { "Rorschach", "Romanshorn" }) },
                
                { "Romanshorn", (true, true, 3, MorningAction.Ferry, AllDayAction.GiveTour, 
                    new List<string> { "Arbon", "Amrisvil" }) },
                
                // South section
                { "Weinfelden", (false, false, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.AddTwoPips, 
                    new List<string> { "Kreuzlingen", "Amrisvil" }) },
                
                { "Amrisvil", (false, false, 3, MorningAction.AllAttractionsAppeal, AllDayAction.RerollTourist, 
                    new List<string> { "Weinfelden", "Romanshorn", "St. Gallen", "Wil" }) },
                
                { "St. Gallen", (false, false, 3, MorningAction.IgnoreFirstAppeal, AllDayAction.AddTwoPips, 
                    new List<string> { "Amrisvil", "Rorschach", "Wil" }) },
                
                { "Wil", (false, false, 3, MorningAction.IncreaseValue, AllDayAction.BuildAttraction, 
                    new List<string> { "St. Gallen", "Amrisvil" }) },
                
                { "Dornbirn", (false, false, 3, MorningAction.AllAttractionsAppeal, AllDayAction.BuildAttraction, 
                    new List<string> { "Bregenz" }) }
            };

            // Create city objects
            foreach (var kvp in cityData)
            {
                var city = new City(kvp.Key, kvp.Value.IsPort, kvp.Value.CanBuildWater)
                {
                    MorningAction = kvp.Value.Morning,
                    AllDayAction = kvp.Value.AllDay,
                    MaxAttractionSpaces = kvp.Value.AttractionSpaces
                };
                city.Connections.AddRange(kvp.Value.Connections);
                
                // Make connections bidirectional
                state.Board.Cities[kvp.Key] = city;
            }

            // Add reverse connections (make all roads bidirectional)
            foreach (var city in state.Board.Cities.Values.ToList())
            {
                foreach (var connectedCityName in city.Connections.ToList())
                {
                    var connectedCity = state.Board.Cities[connectedCityName];
                    if (!connectedCity.Connections.Contains(city.Name))
                    {
                        connectedCity.Connections.Add(city.Name);
                    }
                }
            }
        }

        private void InitializeMarket(GameState state)
        {
            int attractionId = 1;

            // YOUR EXACT attraction list from CSV
            var attractionTemplates = new Dictionary<AttractionCategory, List<(string NameEnglish, string NameGerman, int Value, int Priority, bool PaysBonusEuro, AllDayAction? GrantedAction)>>
            {
                {
                    AttractionCategory.Water,
                    new List<(string, string, int, int, bool, AllDayAction?)>
                    {
                        // Priority 1
                        ("Open air pool", "Freibad", 1, 1, false, null),
                        // Priority 2
                        ("Paddle Board", "Stand-Up-Paddle", 1, 2, false, null),
                        // Priority 3
                        ("Surfboarding", "Wellenreiten", 1, 3, false, null),
                        // Priority 4
                        ("Beach bath", "Strandbad", 2, 4, true, null), // +€1
                        // Priority 5
                        ("Canoeing", "Kanufahren", 2, 5, false, null),
                        // Priority 6
                        ("Kayaking", "Kajakfahren", 2, 6, false, null),
                        // Priority 7
                        ("Pedalos", "Tretboot", 2, 7, false, null),
                        // Priority 8
                        ("Sailing", "Segeln", 2, 8, false, null),
                        // Priority 9
                        ("Boat Hire", "Bootsverleih", 3, 9, false, null),
                        // Priority 10
                        ("Water Park", "Wasserpark", 3, 10, true, null), // +€1
                        // Priority 11
                        ("Kite Surfing", "Kitesurfen", 4, 11, false, null),
                        // Priority 12
                        ("Water skiing", "Wasserski", 4, 12, false, null)
                    }
                },
                {
                    AttractionCategory.Nature,
                    new List<(string, string, int, int, bool, AllDayAction?)>
                    {
                        // Priority 1
                        ("Bicycle Path", "Radweg", 1, 1, false, null),
                        // Priority 2
                        ("Cable Car", "Seilbahn", 1, 2, false, null),
                        // Priority 3
                        ("Flower park", "Blumenpark", 1, 3, false, null),
                        // Priority 4
                        ("Mountain Paths", "Bergpfade", 1, 4, false, null),
                        // Priority 5
                        ("Base Jumping", "Fallschirmspringen", 2, 5, false, null),
                        // Priority 6
                        ("Mountain Biking", "Mountainbike", 2, 6, false, null),
                        // Priority 7
                        ("Mountain Rail", "Bergbahn", 2, 7, false, null),
                        // Priority 8
                        ("Rodel", "Rodelbahn", 2, 8, false, null),
                        // Priority 9
                        ("Wine Path", "Weinpfad", 2, 9, true, null), // +€1
                        // Priority 10
                        ("Animal Park", "Tierpark", 3, 10, false, null),
                        // Priority 11
                        ("Paragliding", "Gleitschirmfliegen", 3, 11, false, null),
                        // Priority 12
                        ("Theme Park", "Freizeitpark", 3, 12, true, null), // +€1
                        // Priority 13
                        ("Zeppeliner ride", "Zeppelinflug", 4, 13, false, null)
                    }
                },
                {
                    AttractionCategory.Culture,
                    new List<(string, string, int, int, bool, AllDayAction?)>
                    {
                        // Priority 1
                        ("Church", "Kirche", 1, 1, false, null),
                        // Priority 2
                        ("Town Hall", "Rathaus", 1, 2, false, null),
                        // Priority 3
                        ("Historic Rail Way", "Historische Eisenbahn", 2, 3, false, null),
                        // Priority 4
                        ("Monestary", "Kloster", 2, 4, false, null),
                        // Priority 5
                        ("Old Town", "Altstadt", 2, 5, false, null),
                        // Priority 6
                        ("Open air museum", "Freilichtmuseum", 2, 6, false, null),
                        // Priority 7
                        ("Theatre", "Theater", 2, 7, true, null), // +€1
                        // Priority 8
                        ("Art Gallery", "Kunstgalerie", 3, 8, true, null), // +€1
                        // Priority 9
                        ("Historic Museum", "Historisches Museum", 3, 9, false, null),
                        // Priority 10
                        ("Castle", "Schloss", 4, 10, false, null),
                        // Priority 11
                        ("Opera", "Oper", 5, 11, false, null)
                    }
                },
                {
                    AttractionCategory.Gastronomy,
                    new List<(string, string, int, int, bool, AllDayAction?)>
                    {
                        // Priority 1
                        ("Fast Food Stand", "Imbissbude", 1, 1, false, null),
                        // Priority 2
                        ("Bakery", "Bäckerei", 2, 2, false, null),
                        // Priority 3
                        ("Bierstube", "Bierstube", 2, 3, false, null),
                        // Priority 4
                        ("Brewery", "Brauerei", 2, 4, false, null),
                        // Priority 5
                        ("Orchards", "Obstgarten", 2, 5, true, null), // +€1
                        // Priority 6
                        ("Destillery", "Brennerei", 3, 6, true, null), // +€1
                        // Priority 7
                        ("Fish restaurant", "Fischrestaurant", 3, 7, false, null),
                        // Priority 8
                        ("Food Festival", "Foodfestival", 3, 8, false, null),
                        // Priority 9
                        ("Vinyard", "Weingut", 4, 9, false, null),
                        // Priority 10
                        ("Gourmet Restaurant", "Gourmetrestaurant", 5, 10, false, null)
                    }
                },
                {
                    AttractionCategory.Gray,
                    new List<(string, string, int, int, bool, AllDayAction?)>
                    {
                        // Utility attractions (no priority in CSV)
                        ("Bus Dispatch", "Busbahnhof", 3, 1, false, AllDayAction.BusDispatch),
                        ("Bus Dispatch", "Busbahnhof", 3, 1, false, AllDayAction.BusDispatch),
                        ("Casino", "Casino", 2, 1, false, AllDayAction.RerollTourist),
                        ("Casino", "Casino", 3, 1, false, AllDayAction.RerollTourist),
                        ("Contractor", "Unternehmer", 3, 1, false, AllDayAction.BuildAttractionDiscount),
                        ("Contractor", "Unternehmer", 3, 1, false, AllDayAction.BuildAttractionDiscount),
                        ("Kulturzentrum", "Kulturzentrum", 3, 2, false, AllDayAction.AddTwoPipsRed),
                        ("Kulturzentrum", "Kulturzentrum", 4, 2, false, AllDayAction.AddTwoPipsRed),
                        ("Genusszentrum", "Genusszentrum", 3, 2, false, AllDayAction.AddTwoPipsYellow),
                        ("Genusszentrum", "Genusszentrum", 4, 2, false, AllDayAction.AddTwoPipsYellow),
                        ("Hotel", "Hotel", 3, 1, false, AllDayAction.GiveTour),
                        ("Hotel", "Hotel", 3, 1, false, AllDayAction.GiveTour),
                        ("Naturzentrum", "Naturzentrum", 3, 2, false, AllDayAction.AddTwoPipsGreen),
                        ("Naturzentrum", "Naturzentrum", 3, 2, false, AllDayAction.AddTwoPipsGreen),
                        ("Wasserzentrum", "Wasserzentrum", 3, 2, false, AllDayAction.AddTwoPipsBlue),
                        ("Wasserzentrum", "Wasserzentrum", 4, 2, false, AllDayAction.AddTwoPipsBlue)
                    }
                }
            };

            // Create and shuffle attractions for each category
            foreach (var category in attractionTemplates.Keys)
            {
                // Skip gray if disabled in settings
                if (category == AttractionCategory.Gray && !state.Settings.EnableGrayAttractions)
                    continue;

                var attractions = attractionTemplates[category]
                    .Select(template => new Attraction(attractionId++, template.NameEnglish, template.NameGerman, category, template.Value, template.Priority, template.PaysBonusEuro, template.GrantedAction))
                    .OrderBy(x => _random.Next())
                    .ToList();

                foreach (var attraction in attractions)
                {
                    state.Market.Decks[category].Enqueue(attraction);
                }

                // Draw initial attractions
                if (category == AttractionCategory.Gray)
                {
                    state.Market.RefillGray(); // Show all gray attractions
                }
                else
                {
                    state.Market.Refill(category, state.Settings.MarketRefillAmount);
                }
            }
        }

        private void InitializeTouristPool(GameState state)
        {
            int touristId = 1;

            // Create tourists of each color (configurable pool size)
            foreach (AttractionCategory category in Enum.GetValues(typeof(AttractionCategory)))
            {
                // Skip Gray category - tourists don't have gray category
                if (category == AttractionCategory.Gray) continue;

                for (int i = 0; i < state.Settings.TouristPoolSizePerCategory; i++)
                {
                    var tourist = new Tourist(touristId++, category, 0);
                    state.TouristPool.Enqueue(tourist);
                }
            }
        }

        private void PlaceBusesAndTourists(GameState state)
        {
            // Exact starting positions from the board image
            var startCities = new[] { "Bodman-Ludwigshafen", "Friedrichshafen", "Lindau", "Rorschach" };

            for (int i = 0; i < 4; i++)
            {
                var bus = new Bus(i, startCities[i]);
                
                // Draw random tourists for this bus based on settings
                int touristsToAdd = state.Settings.StartingTouristsPerBus;
                for (int j = 0; j < touristsToAdd; j++)
                {
                    if (state.TouristPool.Count == 0) break;

                    var touristIndex = _random.Next(state.TouristPool.Count);
                    var tourist = state.TouristPool.Skip(touristIndex).First();
                    
                    // Remove from pool
                    var poolList = state.TouristPool.ToList();
                    poolList.RemoveAt(touristIndex);
                    state.TouristPool = new Queue<Tourist>(poolList);

                    // Roll dice (reroll 1s)
                    int roll;
                    do
                    {
                        roll = _random.Next(1, 7); // 1-6
                    } while (roll == 1);

                    tourist.Money = roll;
                    bus.Tourists.Add(tourist);
                }

                state.Board.Buses.Add(bus);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using BodenseeTourismus.Core;
using BodenseeTourismus.Engine;
using BodenseeTourismus.Analytics;
using BodenseeTourismus.AI;
using BodenseeTourismus.Setup;

namespace BodenseeTourismus.UI
{
    public partial class MainWindow : Window
    {
        private GameState _gameState;
        private GameEngine _engine;
        private GameAnalytics _analytics;
        private AIController _aiController;
        private TurnContext _currentTurnContext;
        private GameSettings _gameSettings;
        
        // View models for data binding
        private List<PlayerViewModel> _playerVMs;
        private List<BusViewModel> _busVMs;
        private List<CityViewModel> _cityVMs;
        private List<MarketCategoryViewModel> _marketVMs;

        // City coordinates for board visualization (Canvas positions + offset for centering)
        private readonly Dictionary<string, (double X, double Y)> _cityCoordinates = new Dictionary<string, (double, double)>
        {
            // Northwest
            { "Bodman-Ludwigshafen", (120, 80) },
            { "Überlingen", (340, 180) },
            { "Radolfzell", (140, 260) },
            { "Konstanz", (320, 340) },
            { "Kreuzlingen", (340, 480) },
            // North
            { "Ravensburg", (920, 80) },
            { "Wangen", (1180, 160) },
            // Lake ports
            { "Meersburg", (480, 260) },
            { "Friedrichshafen", (820, 340) },
            { "Lindau", (1080, 360) },
            { "Bregenz", (1180, 580) },
            { "Hard", (1020, 680) },
            { "Rorschach", (880, 820) },
            { "Arbon", (740, 560) },
            { "Romanshorn", (560, 600) },
            // South
            { "Weinfelden", (260, 640) },
            { "Amrisvil", (480, 760) },
            { "St. Gallen", (620, 880) },
            { "Wil", (200, 840) },
            { "Dornbirn", (1260, 720) }
        };

        public MainWindow()
        {
            InitializeComponent();
            _gameSettings = new GameSettings(); // Default settings
            StartNewGame();
        }

        private void StartNewGame()
        {
            // Configure players (you can add a setup dialog)
            var playerConfigs = new List<(string Name, bool IsAI, string AIStrategy)>
            {
                ("Player 1", false, null),
                ("AI - Aggressive", true, "Aggressive"),
                ("AI - Defensive", true, "Defensive"),
                ("AI - Balanced", true, "Balanced")
            };

            var setup = new GameSetup();
            _gameState = setup.CreateGame(playerConfigs, _gameSettings);
            _engine = new GameEngine(_gameState);
            _analytics = new GameAnalytics();
            _analytics.RecordGameSettings(_gameSettings); // Record settings for analytics
            _aiController = new AIController(_gameState.Random);

            _currentTurnContext = null;

            Log("Game started!");
            Log($"Settings: Appeal={_gameSettings.UseAppealSystem}, Refill={_gameSettings.TouristRefillMode}, Gray={_gameSettings.EnableGrayAttractions}");
            UpdateUI();
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            _currentTurnContext = new TurnContext();
            _analytics.StartTurn(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name, _gameState);

            Log($"--- {_gameState.CurrentPlayer.Name}'s turn ---");
            UpdateUI();

            // Auto-play for AI
            if (_gameState.CurrentPlayer.IsAI)
            {
                AIPlayButton_Click(null, null);
            }
        }

        private void UpdateUI()
        {
            // Update top bar
            CurrentPlayerText.Text = _gameState.CurrentPlayer.Name;
            CurrentPlayerMoney.Text = _gameState.CurrentPlayer.Money.ToString();
            
            if (_gameState.GameEnded)
            {
                var winner = _gameState.GetWinner();
                GameStatusText.Text = $"Game Over! Winner: {winner.Name} with €{winner.Money}";
            }
            else if (_currentTurnContext?.SelectedBus == null)
            {
                GameStatusText.Text = "Select a bus to begin your turn";
            }
            else
            {
                GameStatusText.Text = $"Bus selected in {_currentTurnContext.SelectedBus.CurrentCity}";
            }
            
            // Update players panel
            _playerVMs = _gameState.Players.Select(p => new PlayerViewModel
            {
                Name = p.Name,
                Money = p.Money,
                AttractionCount = p.GetAttractionCount(_gameState),
                AIInfo = p.IsAI ? $"AI: {p.AIStrategy}" : ""
            }).ToList();
            PlayersPanel.ItemsSource = _playerVMs;
            
            // Update buses panel
            _busVMs = _gameState.Board.Buses.Select(b => new BusViewModel
            {
                BusId = b.Id,
                BusName = $"Bus {b.Id + 1}",
                Location = b.CurrentCity,
                Tourists = b.Tourists.Select(t => new TouristViewModel
                {
                    Money = t.Money,
                    CategoryColor = GetCategoryBrush(t.Category)
                }).ToList()
            }).ToList();
            BusesPanel.ItemsSource = _busVMs;
            
            // Update cities panel
            _cityVMs = _gameState.Board.Cities.Values.Select(c => new CityViewModel
            {
                CityName = c.Name,
                PortInfo = c.IsPort ? "⚓ Port" : "",
                BusInfo = _gameState.Board.Buses.Any(b => b.CurrentCity == c.Name) 
                    ? $"🚌 Bus {_gameState.Board.Buses.First(b => b.CurrentCity == c.Name).Id + 1}"
                    : "",
                MorningActionText = c.MorningAction.HasValue ? $"Morning: {c.MorningAction}" : "",
                AllDayActionText = c.AllDayAction.HasValue ? $"All-day: {c.AllDayAction}" : "",
                GrayActionsText = GetGrayActionsText(c),
                Attractions = c.Attractions.Where(a => a.OwnerId.HasValue).Select(a => new AttractionViewModel
                {
                    Name = a.GetName(_gameState.Settings.Language),
                    Value = a.Value,
                    CategoryColor = a.IsGrayAttraction ? Brushes.Gray : GetCategoryBrush(a.Category),
                    IsGray = a.IsGrayAttraction
                }).ToList()
            }).ToList();
            CitiesPanel.ItemsSource = _cityVMs;
            
            // Update market panel
            _marketVMs = new List<MarketCategoryViewModel>();
            foreach (AttractionCategory category in Enum.GetValues(typeof(AttractionCategory)))
            {
                if (!_gameState.Market.AvailableAttractions[category].Any())
                    continue;

                var categoryVM = new MarketCategoryViewModel
                {
                    CategoryName = category.ToString(),
                    CategoryColor = GetCategoryBrush(category),
                    Attractions = _gameState.Market.AvailableAttractions[category].Select(a => new AttractionViewModel
                    {
                        Id = a.Id,
                        Name = a.GetName(_gameState.Settings.Language),
                        Value = a.Value,
                        Priority = a.Priority,
                        CategoryColor = GetCategoryBrush(category),
                        IsGray = a.Category == AttractionCategory.Gray,
                        GrantedActionText = a.GrantedAction.HasValue ? a.GrantedAction.Value.ToString() : ""
                    }).ToList()
                };
                _marketVMs.Add(categoryVM);
            }
            MarketPanel.ItemsSource = _marketVMs;
            
            // Update turn buttons
            bool busSelected = _currentTurnContext?.SelectedBus != null;
            bool hasMoved = _currentTurnContext?.HasMoved ?? false;
            bool hasUsedAllDayAction = _currentTurnContext?.UsedAllDayAction ?? false;

            UseMorningActionButton.IsEnabled = busSelected && _currentTurnContext?.UsedMorningAction == null && !hasMoved;
            MoveButton.IsEnabled = busSelected && _currentTurnContext?.UsedMorningAction != null;
            UseAllDayActionButton.IsEnabled = busSelected && !string.IsNullOrEmpty(_currentTurnContext?.SelectedBus?.CurrentCity);

            // Can only end turn after moving OR using an all-day action (which can substitute for movement)
            EndTurnButton.IsEnabled = busSelected && (hasMoved || hasUsedAllDayAction);

            // Update board visualizations
            UpdateBusVisuals();
            UpdateAttractionDots();
            HighlightValidDestinations();
        }

        private Brush GetCategoryBrush(AttractionCategory category)
        {
            return category switch
            {
                AttractionCategory.Nature => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                AttractionCategory.Water => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                AttractionCategory.Culture => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                AttractionCategory.Gastronomy => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                AttractionCategory.Gray => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => Brushes.Gray
            };
        }

        private string GetGrayActionsText(City city)
        {
            var grayActions = city.Attractions
                .Where(a => a.IsGrayAttraction && a.OwnerId.HasValue && a.GrantedAction.HasValue)
                .Select(a => $"Gray: {a.GrantedAction}")
                .ToList();
            
            return grayActions.Any() ? string.Join(", ", grayActions) : "";
        }

        private void SelectBusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTurnContext == null || _gameState.CurrentPlayer.IsAI) return;
            
            var button = sender as Button;
            int busId = (int)button.Tag;
            var bus = _gameState.Board.Buses.FirstOrDefault(b => b.Id == busId);
            
            if (bus == null || !_engine.CanBusMove(bus))
            {
                MessageBox.Show("This bus cannot move!", "Invalid Selection");
                return;
            }
            
            _currentTurnContext.SelectedBus = bus;
            _currentTurnContext.StartCity = bus.CurrentCity;
            
            _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name, 
                ActionType.SelectBus, new Dictionary<string, object> 
                { 
                    { "BusId", busId }, 
                    { "City", bus.CurrentCity } 
                });
            
            Log($"Selected Bus {busId + 1} in {bus.CurrentCity}");
            UpdateUI();
        }

        private void UseMorningActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTurnContext?.SelectedBus == null) return;
            
            var city = _gameState.Board.GetCity(_currentTurnContext.SelectedBus.CurrentCity);
            if (!city.MorningAction.HasValue)
            {
                MessageBox.Show("No morning action available in this city!", "No Action");
                _currentTurnContext.UsedMorningAction = MorningAction.None;
                UpdateUI();
                return;
            }
            
            _currentTurnContext.UsedMorningAction = city.MorningAction.Value;
            
            switch (city.MorningAction.Value)
            {
                case MorningAction.IncreaseValue:
                    _currentTurnContext.IncreaseValue = true;
                    Log("Using Increase Value - tourists pay €1 more");
                    break;
                case MorningAction.IgnoreFirstAppeal:
                    _currentTurnContext.IgnoreNextAppeal = true;
                    Log("Using Ignore First Appeal");
                    break;
                case MorningAction.Ferry:
                    Log("Using Ferry - can move to any port");
                    break;
                case MorningAction.AllAttractionsAppeal:
                    _currentTurnContext.AllAttractionsAppeal = true;
                    Log("Using All Attractions Appeal");
                    break;
            }
            
            _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                ActionType.UseMorningAction, new Dictionary<string, object>
                {
                    { "Action", city.MorningAction.Value }
                });
            
            UpdateUI();
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTurnContext?.SelectedBus == null) return;
            
            var destinations = _engine.GetValidDestinations(_currentTurnContext.SelectedBus, _currentTurnContext);
            
            if (!destinations.Any())
            {
                MessageBox.Show("No valid destinations!", "Cannot Move");
                return;
            }
            
            // Show destination selection dialog
            var dialog = new DestinationDialog(destinations);
            if (dialog.ShowDialog() == true)
            {
                var destination = dialog.SelectedDestination;
                _currentTurnContext.SelectedBus.CurrentCity = destination;
                _currentTurnContext.HasMoved = true;

                Log($"Bus moved to {destination}");
                
                _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                    ActionType.MoveBus, new Dictionary<string, object>
                    {
                        { "FromCity", _currentTurnContext.StartCity },
                        { "ToCity", destination }
                    });
                
                // Give tour
                var tourResult = _engine.GiveBusTour(_currentTurnContext.SelectedBus, _currentTurnContext);
                Log($"Tour complete: {tourResult.AttractionsVisited.Count} attractions visited, {tourResult.TouristsRuined} tourists ruined");
                
                foreach (var attractionId in tourResult.AttractionsVisited)
                {
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.VisitAttraction, new Dictionary<string, object>
                        {
                            { "AttractionId", attractionId }
                        });
                }
                
                _currentTurnContext.TouristsRuined = tourResult.TouristsRuined;
                UpdateUI();
            }
        }

        private void UseAllDayActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTurnContext?.SelectedBus == null) return;
            
            var city = _gameState.Board.GetCity(_currentTurnContext.SelectedBus.CurrentCity);
            if (!city.AllDayAction.HasValue)
            {
                MessageBox.Show("No all-day action available!", "No Action");
                return;
            }
            
            switch (city.AllDayAction.Value)
            {
                case AllDayAction.BuildAttraction:
                    HandleBuildAttraction(city);
                    break;
                    
                case AllDayAction.RerollTourist:
                    HandleRerollTourist();
                    break;
                    
                case AllDayAction.AddTwoPips:
                    HandleAddTwoPips(null); // Any color
                    break;
                    
                case AllDayAction.AddTwoPipsGreen:
                    HandleAddTwoPips(AttractionCategory.Nature);
                    break;
                    
                case AllDayAction.AddTwoPipsBlue:
                    HandleAddTwoPips(AttractionCategory.Water);
                    break;
                    
                case AllDayAction.AddTwoPipsRed:
                    HandleAddTwoPips(AttractionCategory.Culture);
                    break;
                    
                case AllDayAction.AddTwoPipsYellow:
                    HandleAddTwoPips(AttractionCategory.Gastronomy);
                    break;
                    
                case AllDayAction.GiveTour:
                    HandleGiveTour();
                    break;
                    
                case AllDayAction.BusDispatch:
                    HandleBusDispatch();
                    break;
                    
                case AllDayAction.BuildAttractionDiscount:
                    HandleContractor(city);
                    break;
            }

            // If used during morning phase (before moving), mark that a morning-phase action was taken
            if (!_currentTurnContext.HasMoved)
            {
                _currentTurnContext.UsedMorningAction = MorningAction.None;
            }

            _currentTurnContext.UsedAllDayAction = true;
            UpdateUI();
        }

        private void HandleBuildAttraction(City city)
        {
            var buildDialog = new BuildAttractionDialog(_gameState, city.Name, 0);
            if (buildDialog.ShowDialog() == true)
            {
                var attraction = buildDialog.SelectedAttraction;
                if (_engine.BuildAttraction(attraction, city.Name, 0))
                {
                    string attractionName = attraction.GetName(_gameState.Settings.Language);
                    Log($"Built {attractionName} in {city.Name}");
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.BuildAttraction, new Dictionary<string, object>
                        {
                            { "AttractionId", attraction.Id },
                            { "Category", attraction.Category },
                            { "City", city.Name }
                        });

                    // Refill market
                    if (attraction.Category == AttractionCategory.Gray)
                        _gameState.Market.RefillGray();
                    else
                        _gameState.Market.Refill(attraction.Category, _gameState.Settings.MarketRefillAmount);
                }
            }
        }

        private void HandleContractor(City city)
        {
            int discount = _gameSettings.ContractorDiscountAmount;
            var buildDialog = new BuildAttractionDialog(_gameState, city.Name, discount);
            if (buildDialog.ShowDialog() == true)
            {
                var attraction = buildDialog.SelectedAttraction;
                if (_engine.BuildAttraction(attraction, city.Name, discount))
                {
                    string attractionName = attraction.GetName(_gameState.Settings.Language);
                    Log($"Built {attractionName} in {city.Name} with Contractor discount");
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.BuildAttraction, new Dictionary<string, object>
                        {
                            { "AttractionId", attraction.Id },
                            { "Category", attraction.Category },
                            { "City", city.Name },
                            { "ContractorDiscount", true }
                        });

                    // Refill market
                    if (attraction.Category == AttractionCategory.Gray)
                        _gameState.Market.RefillGray();
                    else
                        _gameState.Market.Refill(attraction.Category, _gameState.Settings.MarketRefillAmount);
                }
            }
        }

        private void HandleRerollTourist()
        {
            if (_currentTurnContext?.SelectedBus == null) return;

            // Casino only works on tourists in the CURRENT CITY's bus
            var cityBuses = _gameState.Board.Buses
                .Where(b => b.CurrentCity == _currentTurnContext.SelectedBus.CurrentCity)
                .ToList();

            var allTouristsInCity = cityBuses.SelectMany(b => b.Tourists).ToList();

            if (!allTouristsInCity.Any())
            {
                MessageBox.Show("No tourists on buses in this city to reroll!", "Cannot Reroll");
                return;
            }

            int rerollsAllowed = _gameState.Settings.CasinoRerollsPerBus;
            int rerollsUsed = 0;

            while (rerollsUsed < rerollsAllowed && allTouristsInCity.Any())
            {
                var dialog = new TouristSelectionDialog(allTouristsInCity, 
                    $"Select tourist to reroll ({rerollsUsed + 1}/{rerollsAllowed})");
                
                if (dialog.ShowDialog() == true && dialog.SelectedTourist != null)
                {
                    var tourist = dialog.SelectedTourist;
                    int oldValue = tourist.Money;
                    
                    // Reroll (avoid 1s)
                    int roll;
                    do
                    {
                        roll = _gameState.Random.Next(1, 7);
                    } while (roll == 1);
                    
                    tourist.Money = roll;
                    Log($"Rerolled tourist: {oldValue} → {tourist.Money} pips");
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.RerollTourist, new Dictionary<string, object>
                        {
                            { "OldValue", oldValue },
                            { "NewValue", tourist.Money }
                        });
                    
                    rerollsUsed++;
                    
                    // Ask if they want to reroll another (if allowed)
                    if (rerollsUsed < rerollsAllowed)
                    {
                        var result = MessageBox.Show($"Reroll another tourist? ({rerollsUsed}/{rerollsAllowed} used)", 
                            "Continue?", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                            break;
                    }
                }
                else
                {
                    break; // User cancelled
                }
            }
        }

        private void HandleAddTwoPips(AttractionCategory? requiredCategory)
        {
            if (_currentTurnContext?.SelectedBus == null || !_currentTurnContext.SelectedBus.Tourists.Any())
            {
                MessageBox.Show("No tourists on this bus!", "Cannot Add Pips");
                return;
            }

            var eligibleTourists = requiredCategory.HasValue
                ? _currentTurnContext.SelectedBus.Tourists.Where(t => t.Category == requiredCategory.Value).ToList()
                : _currentTurnContext.SelectedBus.Tourists.ToList();

            if (!eligibleTourists.Any())
            {
                string colorName = requiredCategory.HasValue ? requiredCategory.Value.ToString() : "any";
                MessageBox.Show($"No {colorName} tourists on this bus!", "Cannot Add Pips");
                return;
            }

            string title = requiredCategory.HasValue 
                ? $"Select {requiredCategory.Value} tourist to add {_gameState.Settings.ZentrumPipsBonus} pips"
                : $"Select tourist to add {_gameState.Settings.ZentrumPipsBonus} pips";

            var dialog = new TouristSelectionDialog(eligibleTourists, title);
            if (dialog.ShowDialog() == true && dialog.SelectedTourist != null)
            {
                var tourist = dialog.SelectedTourist;
                int oldValue = tourist.Money;
                tourist.Money = Math.Min(6, tourist.Money + _gameState.Settings.ZentrumPipsBonus); // Cap at 6
                
                Log($"Added {_gameState.Settings.ZentrumPipsBonus} pips to tourist: {oldValue} → {tourist.Money} pips");
                _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                    ActionType.AddPips, new Dictionary<string, object>
                    {
                        { "OldValue", oldValue },
                        { "NewValue", tourist.Money },
                        { "Category", tourist.Category.ToString() },
                        { "PipsAdded", _gameState.Settings.ZentrumPipsBonus }
                    });
            }
        }

        private void HandleGiveTour()
        {
            if (_currentTurnContext?.SelectedBus == null || !_currentTurnContext.SelectedBus.Tourists.Any())
            {
                MessageBox.Show("No tourists on this bus!", "Cannot Give Tour");
                return;
            }

            // Check setting: whole bus or single tourist
            if (_gameState.Settings.GiveTourAffectsWholeBus)
            {
                // Give tour to entire bus
                var tourResult = _engine.GiveBusTour(_currentTurnContext.SelectedBus, _currentTurnContext);
                Log($"Give Tour (whole bus): {tourResult.AttractionsVisited.Count} attractions visited, {tourResult.TouristsRuined} tourists ruined");
                
                foreach (var attractionId in tourResult.AttractionsVisited)
                {
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.VisitAttraction, new Dictionary<string, object>
                        {
                            { "AttractionId", attractionId },
                            { "GiveTour", true }
                        });
                }
            }
            else
            {
                // Give tour to single tourist
                var dialog = new TouristSelectionDialog(_currentTurnContext.SelectedBus.Tourists, "Select tourist for Give Tour");
                if (dialog.ShowDialog() == true && dialog.SelectedTourist != null)
                {
                    var tourist = dialog.SelectedTourist;
                    var tourResult = _engine.GiveSingleTouristTour(_currentTurnContext.SelectedBus, tourist, _currentTurnContext.SelectedBus.CurrentCity);
                    
                    Log($"Give Tour (single tourist): {tourResult.AttractionsVisited.Count} attractions visited, tourist ruined: {tourResult.TouristsRuined > 0}");
                    
                    foreach (var attractionId in tourResult.AttractionsVisited)
                    {
                        _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                            ActionType.VisitAttraction, new Dictionary<string, object>
                            {
                                { "AttractionId", attractionId },
                                { "GiveTour", true },
                                { "SingleTourist", true }
                            });
                    }
                }
            }
        }

        private void HandleBusDispatch()
        {
            if (_currentTurnContext?.SelectedBus == null) return;

            // Get all other buses in occupied cities
            var otherBuses = _gameState.Board.Buses
                .Where(b => b.Id != _currentTurnContext.SelectedBus.Id)
                .ToList();

            if (!otherBuses.Any())
            {
                MessageBox.Show("No other buses to move!", "Cannot Dispatch");
                return;
            }

            var busDialog = new BusSelectionDialog(otherBuses, _gameState.Board);
            if (busDialog.ShowDialog() == true && busDialog.SelectedBus != null)
            {
                var busToMove = busDialog.SelectedBus;
                var currentCity = _gameState.Board.GetCity(busToMove.CurrentCity);

                // Get valid adjacent cities (not occupied)
                var validDestinations = currentCity.Connections
                    .Where(cityName => !_gameState.Board.Buses.Any(b => b.CurrentCity == cityName))
                    .ToList();

                if (!validDestinations.Any())
                {
                    MessageBox.Show("Selected bus has no valid moves!", "Cannot Dispatch");
                    return;
                }

                var destDialog = new DestinationDialog(validDestinations);
                if (destDialog.ShowDialog() == true)
                {
                    string oldCity = busToMove.CurrentCity;
                    busToMove.CurrentCity = destDialog.SelectedDestination;
                    
                    Log($"Dispatched Bus {busToMove.Id + 1} from {oldCity} to {destDialog.SelectedDestination}");
                    _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                        ActionType.MoveBus, new Dictionary<string, object>
                        {
                            { "BusId", busToMove.Id },
                            { "FromCity", oldCity },
                            { "ToCity", destDialog.SelectedDestination },
                            { "BusDispatch", true }
                        });
                }
            }
        }

        private void EndTurnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTurnContext == null) return;

            // Validate that player has moved or used an all-day action
            if (!_currentTurnContext.HasMoved && !_currentTurnContext.UsedAllDayAction)
            {
                MessageBox.Show("You must move your bus or use an all-day action before ending your turn!", "Cannot End Turn");
                return;
            }

            // Refill
            _engine.Refill(_currentTurnContext);
            
            // Check game end
            _engine.CheckGameEnd();

            _analytics.EndTurn(_gameState);

            if (_gameState.GameEnded)
            {
                var winner = _gameState.GetWinner();
                Log($"=== GAME OVER ===");
                Log($"Winner: {winner.Name} with €{winner.Money}!");
                
                MessageBox.Show($"Game Over!\nWinner: {winner.Name}\nMoney: €{winner.Money}", 
                    "Game Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateUI();
                return;
            }
            
            // Next player
            _gameState.NextPlayer();
            StartPlayerTurn();
        }

        private void AIPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_gameState.CurrentPlayer.IsAI) return;
            
            var decision = _aiController.GetAIDecision(_gameState, _engine, _gameState.CurrentPlayer.AIStrategy);
            
            if (decision.SelectedBus != null)
            {
                _currentTurnContext.SelectedBus = decision.SelectedBus;
                _currentTurnContext.StartCity = decision.SelectedBus.CurrentCity;
                Log($"AI selected Bus {decision.SelectedBus.Id + 1}");
                
                // Morning action
                if (decision.MorningAction.HasValue)
                {
                    _currentTurnContext.UsedMorningAction = decision.MorningAction.Value;
                    if (decision.MorningAction == MorningAction.IncreaseValue)
                        _currentTurnContext.IncreaseValue = true;
                    if (decision.MorningAction == MorningAction.AllAttractionsAppeal)
                        _currentTurnContext.AllAttractionsAppeal = true;
                    if (decision.MorningAction == MorningAction.IgnoreFirstAppeal)
                        _currentTurnContext.IgnoreNextAppeal = true;
                    Log($"AI used morning action: {decision.MorningAction}");
                }
                else
                {
                    _currentTurnContext.UsedMorningAction = MorningAction.None;
                }
                
                // Move
                if (!string.IsNullOrEmpty(decision.DestinationCity))
                {
                    decision.SelectedBus.CurrentCity = decision.DestinationCity;
                    Log($"AI moved to {decision.DestinationCity}");
                    
                    // Tour
                    var tourResult = _engine.GiveBusTour(decision.SelectedBus, _currentTurnContext);
                    Log($"AI tour: {tourResult.AttractionsVisited.Count} attractions, {tourResult.TouristsRuined} tourists ruined");
                    _currentTurnContext.TouristsRuined = tourResult.TouristsRuined;
                }
                
                // All-day action - complete implementation for all action types
                if (decision.AllDayAction.HasValue)
                {
                    var arrivalCity = _gameState.Board.GetCity(decision.DestinationCity ?? decision.SelectedBus.CurrentCity);

                    switch (decision.AllDayAction.Value)
                    {
                        case AllDayAction.BuildAttraction:
                            if (decision.ActionParameters.ContainsKey("Attraction"))
                            {
                                var attraction = decision.ActionParameters["Attraction"] as Attraction;
                                var cityName = decision.ActionParameters["City"] as string;
                                if (_engine.BuildAttraction(attraction, cityName))
                                {
                                    Log($"AI built {attraction.GetName(_gameSettings.Language)} in {cityName}");

                                    // Refill market - Gray attractions use special refill
                                    if (attraction.Category == AttractionCategory.Gray)
                                        _gameState.Market.RefillGray();
                                    else
                                        _gameState.Market.Refill(attraction.Category, _gameState.Settings.MarketRefillAmount);
                                }
                            }
                            break;

                        case AllDayAction.BuildAttractionDiscount:
                            // Contractor - build with discount (already in decision parameters)
                            if (decision.ActionParameters.ContainsKey("Attraction"))
                            {
                                var attraction = decision.ActionParameters["Attraction"] as Attraction;
                                var cityName = decision.ActionParameters["City"] as string;
                                int discount = _gameSettings.ContractorDiscountAmount;
                                if (_engine.BuildAttraction(attraction, cityName, discount))
                                {
                                    Log($"AI built {attraction.GetName(_gameSettings.Language)} with €{discount} discount in {cityName}");

                                    // Refill market - Gray attractions use special refill
                                    if (attraction.Category == AttractionCategory.Gray)
                                        _gameState.Market.RefillGray();
                                    else
                                        _gameState.Market.Refill(attraction.Category, _gameState.Settings.MarketRefillAmount);
                                }
                            }
                            break;

                        case AllDayAction.AddTwoPips:
                            if (decision.SelectedBus.Tourists.Any())
                            {
                                var randomTourist = decision.SelectedBus.Tourists[_gameState.Random.Next(decision.SelectedBus.Tourists.Count)];
                                int oldValue = randomTourist.Money;
                                randomTourist.Money = Math.Min(6, randomTourist.Money + _gameState.Settings.ZentrumPipsBonus);
                                Log($"AI added {_gameState.Settings.ZentrumPipsBonus} pips to {randomTourist.Category} tourist: €{oldValue} → €{randomTourist.Money}");
                            }
                            break;

                        case AllDayAction.AddTwoPipsGreen:
                        case AllDayAction.AddTwoPipsBlue:
                        case AllDayAction.AddTwoPipsRed:
                        case AllDayAction.AddTwoPipsYellow:
                            // Color-specific zentrum actions
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
                                colorTourist.Money = Math.Min(6, colorTourist.Money + _gameState.Settings.ZentrumPipsBonus);
                                Log($"AI added {_gameState.Settings.ZentrumPipsBonus} pips to {targetCategory} tourist: €{oldValue} → €{colorTourist.Money}");
                            }
                            break;

                        case AllDayAction.RerollTourist:
                            // Casino works on all buses in current city
                            var cityBuses = _gameState.Board.Buses
                                .Where(b => b.CurrentCity == decision.SelectedBus.CurrentCity)
                                .ToList();
                            var allTouristsInCity = cityBuses.SelectMany(b => b.Tourists).ToList();

                            if (allTouristsInCity.Any())
                            {
                                int rerollsAllowed = _gameState.Settings.CasinoRerollsPerBus;
                                for (int i = 0; i < rerollsAllowed && allTouristsInCity.Any(); i++)
                                {
                                    // AI strategy: reroll poorest tourist
                                    var poorestTourist = allTouristsInCity.OrderBy(t => t.Money).First();
                                    int oldMoney = poorestTourist.Money;

                                    // Reroll, avoiding 1s
                                    int newMoney;
                                    do
                                    {
                                        newMoney = _gameState.Random.Next(1, 7);
                                    } while (newMoney == 1);

                                    poorestTourist.Money = newMoney;
                                    Log($"AI rerolled {poorestTourist.Category} tourist: €{oldMoney} → €{newMoney}");
                                }
                            }
                            break;

                        case AllDayAction.GiveTour:
                            // Give immediate extra tour - respect setting for whole bus vs single tourist
                            if (_gameState.Settings.GiveTourAffectsWholeBus)
                            {
                                // Whole bus tour
                                var extraTourResult = _engine.GiveBusTour(decision.SelectedBus, _currentTurnContext);
                                Log($"AI gave extra tour (whole bus): {extraTourResult.AttractionsVisited.Count} attractions visited, {extraTourResult.TouristsRuined} ruined");
                                _currentTurnContext.TouristsRuined += extraTourResult.TouristsRuined;
                            }
                            else
                            {
                                // Single tourist tour - AI picks richest tourist to maximize value
                                if (decision.SelectedBus.Tourists.Any())
                                {
                                    var richestTourist = decision.SelectedBus.Tourists.OrderByDescending(t => t.Money).First();
                                    var singleTourResult = _engine.GiveSingleTouristTour(decision.SelectedBus, richestTourist, decision.SelectedBus.CurrentCity);
                                    Log($"AI gave extra tour (single tourist): {singleTourResult.AttractionsVisited.Count} attractions visited, ruined: {singleTourResult.TouristsRuined > 0}");
                                }
                            }
                            break;

                        case AllDayAction.BusDispatch:
                            // Move another bus - AI needs to pick which bus and where
                            var otherBuses = _gameState.Board.Buses.Where(b => b.Id != decision.SelectedBus.Id).ToList();
                            if (otherBuses.Any())
                            {
                                var busToMove = otherBuses[_gameState.Random.Next(otherBuses.Count)];
                                var validDests = _engine.GetValidDestinations(busToMove, new TurnContext());
                                if (validDests.Any())
                                {
                                    var randomDest = validDests[_gameState.Random.Next(validDests.Count)];
                                    busToMove.CurrentCity = randomDest;
                                    Log($"AI dispatched Bus {busToMove.Id + 1} to {randomDest}");
                                }
                            }
                            break;

                        default:
                            Log($"AI attempted to use {decision.AllDayAction} but no handler implemented");
                            break;
                    }
                }
                
                UpdateUI();
                
                // Auto-end turn for AI
                System.Threading.Thread.Sleep(1000); // Brief pause so humans can see
                EndTurnButton_Click(null, null);
            }
        }

        private void BuyAttractionButton_Click(object sender, RoutedEventArgs e)
        {
            // This would need proper implementation with city selection
            MessageBox.Show("Use the Build Attraction action during your turn!", "Info");
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Start a new game?", "New Game", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                StartNewGame();
            }
        }

        private void EditAttractionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Convert current attractions to edit models
            var currentAttractions = _gameState.Market.Decks.Values
                .SelectMany(deck => deck)
                .Concat(_gameState.Market.AvailableAttractions.Values.SelectMany(list => list))
                .Concat(_gameState.Board.Cities.Values.SelectMany(c => c.Attractions))
                .Distinct()
                .Select(a => new AttractionEditModel
                {
                    Id = a.Id,
                    NameEnglish = a.NameEnglish,
                    NameGerman = a.NameGerman,
                    Category = a.Category,
                    Value = a.Value,
                    Priority = a.Priority,
                    PaysBonusEuro = a.PaysBonusEuro,
                    GrantedAction = a.GrantedAction
                }).ToList();

            var editor = new AttractionEditorWindow(currentAttractions);
            if (editor.ShowDialog() == true)
            {
                // Save edited attractions for next game
                // You could implement this to update the game setup
                MessageBox.Show("Attraction changes will apply to the next new game.", "Saved");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_gameSettings);
            if (settingsWindow.ShowDialog() == true && settingsWindow.Applied)
            {
                _gameSettings = settingsWindow.Settings;
                MessageBox.Show("Settings updated!\n\nStart a new game for changes to take effect.", 
                    "Settings Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"bodensee_game_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, _analytics.ExportToCSV());
                MessageBox.Show("Data exported successfully!", "Export Complete");
            }
        }

        // ==================== BOARD VISUALIZATION METHODS ====================

        private void CityButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string cityName = button.Tag.ToString();

            var city = _gameState.Board.GetCity(cityName);
            if (city == null) return;

            // Build info message
            string info = $"City: {cityName}\n";
            if (city.MorningAction.HasValue)
                info += $"Morning Action: {city.MorningAction}\n";
            if (city.AllDayAction.HasValue)
                info += $"All-Day Action: {city.AllDayAction}\n";

            var attractions = city.Attractions.Where(a => a.OwnerId.HasValue).ToList();
            if (attractions.Any())
            {
                info += $"\nAttractions ({attractions.Count}):\n";
                foreach (var attr in attractions)
                {
                    var owner = _gameState.Players.FirstOrDefault(p => p.Id == attr.OwnerId);
                    info += $"  - {attr.GetName(_gameSettings.Language)} ({attr.Value}€) - {owner?.Name}\n";
                }
            }

            var busInCity = _gameState.Board.Buses.FirstOrDefault(b => b.CurrentCity == cityName);
            if (busInCity != null)
            {
                info += $"\nBus {busInCity.Id + 1} is here ({busInCity.Tourists.Count} tourists)\n";
            }

            MessageBox.Show(info, $"City Info: {cityName}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateBusVisuals()
        {
            // Clear existing bus icons
            BusIconsContainer.Children.Clear();

            // Create bus icon for each bus
            foreach (var bus in _gameState.Board.Buses)
            {
                if (!_cityCoordinates.TryGetValue(bus.CurrentCity, out var coords))
                    continue;

                // Create bus container
                var busIcon = new Canvas
                {
                    Width = 60,
                    Height = 20
                };

                // Create 4 tourist slot rectangles (2x2 grid)
                for (int slotIndex = 0; slotIndex < 4; slotIndex++)
                {
                    var tourist = slotIndex < bus.Tourists.Count ? bus.Tourists[slotIndex] : null;

                    var slotRect = new Border
                    {
                        Width = 28,
                        Height = 8,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(2)
                    };

                    // Position in 2x2 grid
                    double slotX = (slotIndex % 2) * 30;
                    double slotY = (slotIndex / 2) * 10;

                    Canvas.SetLeft(slotRect, slotX);
                    Canvas.SetTop(slotRect, slotY);

                    if (tourist != null && tourist.Money > 0)
                    {
                        // Active tourist - show category color and money
                        slotRect.Background = GetCategoryBrush(tourist.Category);

                        var moneyText = new TextBlock
                        {
                            Text = tourist.Money.ToString(),
                            Foreground = Brushes.White,
                            FontSize = 7,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        slotRect.Child = moneyText;
                    }
                    else
                    {
                        // Empty or ruined slot - grey
                        slotRect.Background = Brushes.Gray;
                        slotRect.Opacity = 0.5;
                    }

                    busIcon.Children.Add(slotRect);
                }

                // Add bus number label above icon
                var busLabel = new TextBlock
                {
                    Text = $"🚌{bus.Id + 1}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    Padding = new Thickness(2, 0, 2, 0)
                };

                Canvas.SetLeft(busLabel, 15);
                Canvas.SetTop(busLabel, -12);
                busIcon.Children.Add(busLabel);

                // Position bus icon on board
                Canvas.SetLeft(busIcon, coords.X - 15); // Center the icon
                Canvas.SetTop(busIcon, coords.Y + 5);
                Canvas.SetZIndex(busIcon, 100);

                // Add tooltip with detailed info
                var tooltipText = $"Bus {bus.Id + 1} in {bus.CurrentCity}\n";
                tooltipText += $"Tourists: {bus.Tourists.Count}\n";
                foreach (var t in bus.Tourists)
                {
                    tooltipText += $"  {t.Category}: €{t.Money}\n";
                }
                busIcon.ToolTip = tooltipText.TrimEnd();

                BusIconsContainer.Children.Add(busIcon);
            }
        }

        private void HighlightValidDestinations()
        {
            // Clear previous highlights
            ClearHighlights();

            if (_currentTurnContext?.SelectedBus == null) return;

            var validDestinations = _engine.GetValidDestinations(_currentTurnContext.SelectedBus, _currentTurnContext);

            // Find the canvas that contains the board
            var canvas = FindVisualChild<Canvas>(CenterPanel);
            if (canvas == null) return;

            // Draw highlight circles on valid destinations
            foreach (var destCity in validDestinations)
            {
                if (_cityCoordinates.TryGetValue(destCity, out var coords))
                {
                    var highlight = new Ellipse
                    {
                        Width = 80,
                        Height = 80,
                        Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)), // Semi-transparent green
                        Stroke = Brushes.LimeGreen,
                        StrokeThickness = 3,
                        Tag = "Highlight" // Tag for easy removal
                    };

                    Canvas.SetLeft(highlight, coords.X - 25); // Center the highlight
                    Canvas.SetTop(highlight, coords.Y - 25);
                    Canvas.SetZIndex(highlight, 50); // Below buses but above board

                    canvas.Children.Add(highlight);
                }
            }

            // Draw connection lines from current city to valid destinations
            if (_currentTurnContext.SelectedBus != null)
            {
                DrawConnectionLines(_currentTurnContext.SelectedBus.CurrentCity, validDestinations, canvas);
            }
        }

        private void DrawConnectionLines(string fromCity, List<string> toCities, Canvas canvas)
        {
            if (!_cityCoordinates.TryGetValue(fromCity, out var fromCoords)) return;

            foreach (var toCity in toCities)
            {
                if (_cityCoordinates.TryGetValue(toCity, out var toCoords))
                {
                    var line = new Line
                    {
                        X1 = fromCoords.X + 15,
                        Y1 = fromCoords.Y + 15,
                        X2 = toCoords.X + 15,
                        Y2 = toCoords.Y + 15,
                        Stroke = Brushes.Yellow,
                        StrokeThickness = 4,
                        Opacity = 0.7,
                        Tag = "Highlight" // Tag for easy removal
                    };

                    Canvas.SetZIndex(line, 40); // Below highlights
                    canvas.Children.Add(line);
                }
            }
        }

        private void ClearHighlights()
        {
            var canvas = FindVisualChild<Canvas>(CenterPanel);
            if (canvas == null) return;

            // Remove all elements tagged as "Highlight"
            var toRemove = canvas.Children.OfType<FrameworkElement>()
                .Where(e => e.Tag?.ToString() == "Highlight")
                .ToList();

            foreach (var element in toRemove)
            {
                canvas.Children.Remove(element);
            }
        }

        private void UpdateAttractionDots()
        {
            // Clear existing dots
            AttractionDotsContainer.Children.Clear();

            // Draw colored dots for each built attraction
            foreach (var city in _gameState.Board.Cities.Values)
            {
                var builtAttractions = city.Attractions.Where(a => a.OwnerId.HasValue).ToList();
                if (!builtAttractions.Any()) continue;

                if (_cityCoordinates.TryGetValue(city.Name, out var coords))
                {
                    int dotIndex = 0;
                    foreach (var attraction in builtAttractions.Take(5)) // Max 5 dots per city
                    {
                        var owner = _gameState.Players.FirstOrDefault(p => p.Id == attraction.OwnerId);
                        if (owner == null) continue;

                        // Create small colored dot
                        var dot = new Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Fill = GetPlayerColor(owner.Id),
                            Stroke = Brushes.White,
                            StrokeThickness = 1
                        };

                        // Position dots in a small grid around city center
                        double offsetX = (dotIndex % 3) * 12 - 12;
                        double offsetY = (dotIndex / 3) * 12 + 40;

                        Canvas.SetLeft(dot, coords.X + offsetX);
                        Canvas.SetTop(dot, coords.Y + offsetY);
                        Canvas.SetZIndex(dot, 95);

                        AttractionDotsContainer.Children.Add(dot);
                        dotIndex++;
                    }
                }
            }
        }

        private Brush GetPlayerColor(int playerId)
        {
            var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Orange, Brushes.Purple };
            return playerId < colors.Length ? colors[playerId] : Brushes.Gray;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        // ==================== END BOARD VISUALIZATION ====================

        private void Log(string message)
        {
            GameLogText.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
            LogScrollViewer.ScrollToEnd();
        }
    }

    // View Models
    public class PlayerViewModel
    {
        public string Name { get; set; }
        public int Money { get; set; }
        public int AttractionCount { get; set; }
        public string AIInfo { get; set; }
    }

    public class BusViewModel
    {
        public int BusId { get; set; }
        public string BusName { get; set; }
        public string Location { get; set; }
        public List<TouristViewModel> Tourists { get; set; }
    }

    public class TouristViewModel
    {
        public int Money { get; set; }
        public Brush CategoryColor { get; set; }
    }

    public class CityViewModel
    {
        public string CityName { get; set; }
        public string PortInfo { get; set; }
        public string BusInfo { get; set; }
        public string MorningActionText { get; set; }
        public string AllDayActionText { get; set; }
        public string GrayActionsText { get; set; }
        public List<AttractionViewModel> Attractions { get; set; }
    }

    public class AttractionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public int Priority { get; set; }
        public Brush CategoryColor { get; set; }
        public bool IsGray { get; set; }
        public string GrantedActionText { get; set; }
    }

    public class MarketCategoryViewModel
    {
        public string CategoryName { get; set; }
        public Brush CategoryColor { get; set; }
        public List<AttractionViewModel> Attractions { get; set; }
    }

    // Simple dialogs
    public class DestinationDialog : Window
    {
        public string SelectedDestination { get; private set; }

        public DestinationDialog(List<string> destinations)
        {
            Title = "Select Destination";
            Width = 300;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var listBox = new ListBox { Margin = new Thickness(10) };
            foreach (var dest in destinations)
            {
                listBox.Items.Add(dest);
            }

            var button = new Button
            {
                Content = "Select",
                Margin = new Thickness(10),
                Height = 30
            };
            button.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    SelectedDestination = listBox.SelectedItem.ToString();
                    DialogResult = true;
                }
            };

            var panel = new DockPanel();
            DockPanel.SetDock(button, Dock.Bottom);
            panel.Children.Add(button);
            panel.Children.Add(listBox);

            Content = panel;
        }
    }

    public class BuildAttractionDialog : Window
    {
        public Attraction SelectedAttraction { get; private set; }

        public BuildAttractionDialog(GameState state, string cityName, int discount)
        {
            string title = discount > 0 
                ? $"Build Attraction in {cityName} (€{discount} Discount!)"
                : $"Build Attraction in {cityName}";
            
            Title = title;
            Width = 450;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var listBox = new ListBox { Margin = new Thickness(10) };
            var city = state.Board.GetCity(cityName);

            foreach (var category in state.Market.AvailableAttractions.Keys)
            {
                foreach (var attr in state.Market.AvailableAttractions[category])
                {
                    if (attr.Category == AttractionCategory.Water && !city.CanBuildWater)
                        continue;

                    int totalCost;
                    if (attr.Category == AttractionCategory.Gray)
                    {
                        // Gray: fixed cost, no category count
                        totalCost = state.Settings.GrayBaseCost;
                    }
                    else
                    {
                        int baseCost = state.Settings.GetBaseCost(attr.Category);
                        int sameCategoryCount = city.Attractions.Count(a =>
                            a.OwnerId.HasValue && 
                            a.Category != AttractionCategory.Gray &&
                            (a.Category == attr.Category || city.IsPort));
                        totalCost = baseCost + sameCategoryCount;
                    }

                    // Apply discount
                    int discountedCost = Math.Max(0, totalCost - discount);
                    string costDisplay = discount > 0 && totalCost != discountedCost
                        ? $"€{totalCost} → €{discountedCost}"
                        : $"€{discountedCost}";

                    string grayInfo = attr.Category == AttractionCategory.Gray 
                        ? $" [GRAY → {attr.GrantedAction}]" 
                        : "";
                    
                    string bonusInfo = attr.PaysBonusEuro && state.Settings.UseBonusEuro
                        ? " [+€1 Bonus]"
                        : "";
                    
                    string attractionName = attr.GetName(state.Settings.Language);
                    
                    listBox.Items.Add(new
                    {
                        Attraction = attr,
                        Display = $"{attractionName} - Value: {attr.Value}, Cost: {costDisplay}{grayInfo}{bonusInfo}"
                    });
                }
            }

            listBox.DisplayMemberPath = "Display";

            var button = new Button
            {
                Content = "Build",
                Margin = new Thickness(10),
                Height = 30
            };
            button.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    dynamic item = listBox.SelectedItem;
                    SelectedAttraction = item.Attraction;
                    DialogResult = true;
                }
            };

            var panel = new DockPanel();
            DockPanel.SetDock(button, Dock.Bottom);
            panel.Children.Add(button);
            panel.Children.Add(listBox);

            Content = panel;
        }
    }

    public class TouristSelectionDialog : Window
    {
        public Tourist SelectedTourist { get; private set; }

        public TouristSelectionDialog(List<Tourist> tourists, string title)
        {
            Title = title;
            Width = 350;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var listBox = new ListBox { Margin = new Thickness(10) };
            
            foreach (var tourist in tourists)
            {
                var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                
                var colorRect = new System.Windows.Shapes.Rectangle
                {
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                
                colorRect.Fill = tourist.Category switch
                {
                    AttractionCategory.Nature => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    AttractionCategory.Water => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    AttractionCategory.Culture => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    AttractionCategory.Gastronomy => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    _ => Brushes.Gray
                };
                
                var text = new TextBlock
                {
                    Text = $"{tourist.Category} tourist - {tourist.Money} pips",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                };
                
                item.Children.Add(colorRect);
                item.Children.Add(text);
                item.Tag = tourist;
                
                listBox.Items.Add(item);
            }

            var button = new Button
            {
                Content = "Select",
                Margin = new Thickness(10),
                Height = 30
            };
            button.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    var item = (StackPanel)listBox.SelectedItem;
                    SelectedTourist = (Tourist)item.Tag;
                    DialogResult = true;
                }
            };

            var panel = new DockPanel();
            DockPanel.SetDock(button, Dock.Bottom);
            panel.Children.Add(button);
            panel.Children.Add(listBox);

            Content = panel;
        }
    }

    public class BusSelectionDialog : Window
    {
        public Bus SelectedBus { get; private set; }

        public BusSelectionDialog(List<Bus> buses, Board board)
        {
            Title = "Select Bus to Dispatch";
            Width = 400;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var listBox = new ListBox { Margin = new Thickness(10) };
            
            foreach (var bus in buses)
            {
                var item = new StackPanel { Margin = new Thickness(5) };
                
                var header = new TextBlock
                {
                    Text = $"🚌 Bus {bus.Id + 1} - Currently in {bus.CurrentCity}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                
                var touristInfo = new TextBlock
                {
                    Text = $"Tourists: {bus.Tourists.Count}",
                    FontSize = 12,
                    Foreground = Brushes.Gray
                };
                
                item.Children.Add(header);
                item.Children.Add(touristInfo);
                item.Tag = bus;
                
                listBox.Items.Add(item);
            }

            var button = new Button
            {
                Content = "Select This Bus",
                Margin = new Thickness(10),
                Height = 30
            };
            button.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    var item = (StackPanel)listBox.SelectedItem;
                    SelectedBus = (Bus)item.Tag;
                    DialogResult = true;
                }
            };

            var panel = new DockPanel();
            DockPanel.SetDock(button, Dock.Bottom);
            panel.Children.Add(button);
            panel.Children.Add(listBox);

            Content = panel;
        }
    }
}
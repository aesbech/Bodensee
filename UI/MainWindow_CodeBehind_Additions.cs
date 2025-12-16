// ========================================
// CODE ADDITIONS FOR MainWindow.xaml.cs
// Board Visual Integration by Sarah Mitchell
// ========================================

// ADD TO CLASS-LEVEL FIELDS (near the top):
private Dictionary<string, Point> _cityCoordinates;

// ADD TO CONSTRUCTOR (at the end, before StartNewGame()):
private void InitializeCityCoordinates()
{
    _cityCoordinates = new Dictionary<string, Point>
    {
        // Northwest
        { "Bodman-Ludwigshafen", new Point(120, 80) },
        { "Ãœberlingen", new Point(340, 180) },
        { "Radolfzelt", new Point(140, 260) },
        { "Ravensburg", new Point(920, 80) },
        { "Wangen", new Point(1180, 160) },
        
        // Lake Ports
        { "Konstanz", new Point(320, 340) },
        { "Meersburg", new Point(480, 260) },
        { "Friedrichshafen", new Point(820, 340) },
        { "Lindau", new Point(1080, 360) },
        { "Bregenz", new Point(1180, 580) },
        { "Hard", new Point(1020, 680) },
        { "Rorschach", new Point(880, 820) },
        { "Arbon", new Point(740, 560) },
        { "Romanshorn", new Point(560, 600) },
        
        // South
        { "Kreuzlingen", new Point(340, 480) },
        { "Weinfelden", new Point(260, 640) },
        { "Amrisvil", new Point(480, 760) },
        { "St. Gallen", new Point(620, 880) },
        { "Wil", new Point(200, 840) },
        { "Dornbirn", new Point(1260, 720) }
    };
}

// MODIFY CONSTRUCTOR to call InitializeCityCoordinates:
public MainWindow()
{
    InitializeComponent();
    _gameSettings = new GameSettings(); // Default settings
    InitializeCityCoordinates(); // ADD THIS LINE
    StartNewGame();
}

// ADD AT END OF UpdateUI() METHOD:
private void UpdateUI()
{
    // ... all existing UpdateUI code ...
    
    // ADD THIS AT THE VERY END:
    UpdateBoardVisuals();
}

// ADD THESE NEW METHODS:

/// <summary>
/// Updates bus positions and attraction dots on the visual board
/// </summary>
private void UpdateBoardVisuals()
{
    if (_gameState == null || _gameState.Board == null) return;
    
    // Update bus indicators
    var busIndicators = new[] { Bus1Indicator, Bus2Indicator, Bus3Indicator, Bus4Indicator };
    
    for (int i = 0; i < _gameState.Board.Buses.Count && i < busIndicators.Length; i++)
    {
        var bus = _gameState.Board.Buses[i];
        var indicator = busIndicators[i];
        
        if (_cityCoordinates.ContainsKey(bus.CurrentCity))
        {
            var coord = _cityCoordinates[bus.CurrentCity];
            Canvas.SetLeft(indicator, coord.X - 15); // Center 30px circle
            Canvas.SetTop(indicator, coord.Y - 15);
            indicator.Visibility = Visibility.Visible;
        }
        else
        {
            indicator.Visibility = Visibility.Collapsed;
        }
    }
    
    // Update attraction dots
    UpdateAttractionDots();
}

/// <summary>
/// Updates attraction ownership dots on the board
/// </summary>
private void UpdateAttractionDots()
{
    if (AttractionDotsContainer == null) return;
    
    AttractionDotsContainer.Children.Clear();
    
    foreach (var city in _gameState.Board.Cities.Values)
    {
        if (!_cityCoordinates.ContainsKey(city.Name)) continue;
        
        var baseCoord = _cityCoordinates[city.Name];
        var ownedAttractions = city.Attractions.Where(a => a.OwnerId.HasValue).ToList();
        
        // Display up to 3 dots per city (max attraction spaces)
        for (int i = 0; i < Math.Min(ownedAttractions.Count, city.MaxAttractionSpaces); i++)
        {
            var attraction = ownedAttractions[i];
            var owner = _gameState.Players.FirstOrDefault(p => p.Id == attraction.OwnerId);
            if (owner == null) continue;
            
            // Position dots in a row below the city center
            double offsetX = baseCoord.X - 20 + (i * 15);
            double offsetY = baseCoord.Y + 25;
            
            var dot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            
            // Color based on player (matching bus colors)
            dot.Fill = owner.Id switch
            {
                0 => new SolidColorBrush(Color.FromRgb(244, 67, 54)),   // Red
                1 => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Blue
                2 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),   // Yellow
                3 => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
                _ => Brushes.Gray
            };
            
            // Gray attractions use gray color
            if (attraction.IsGrayAttraction)
            {
                dot.Fill = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
            
            Canvas.SetLeft(dot, offsetX);
            Canvas.SetTop(dot, offsetY);
            Panel.SetZIndex(dot, 90);
            
            AttractionDotsContainer.Children.Add(dot);
        }
    }
}

/// <summary>
/// Handles clicks on city buttons on the visual board
/// </summary>
private void CityButton_Click(object sender, RoutedEventArgs e)
{
    if (sender is not Button button || button.Tag is not string cityName)
        return;
    
    // During Move phase: select city as destination
    if (_currentTurnContext?.UsedMorningAction != null && 
        _currentTurnContext.SelectedBus != null &&
        _currentTurnContext.SelectedBus.CurrentCity != cityName)
    {
        var destinations = _engine.GetValidDestinations(_currentTurnContext.SelectedBus, _currentTurnContext);
        
        if (destinations.Contains(cityName))
        {
            // Valid destination - execute move
            _currentTurnContext.SelectedBus.CurrentCity = cityName;
            
            Log($"Bus moved to {cityName}");
            
            _analytics.LogAction(_gameState.CurrentPlayer.Id, _gameState.CurrentPlayer.Name,
                ActionType.MoveBus, new Dictionary<string, object>
                {
                    { "FromCity", _currentTurnContext.StartCity },
                    { "ToCity", cityName }
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
            return;
        }
        else
        {
            MessageBox.Show($"Cannot move to {cityName} - not a valid destination!", 
                "Invalid Move", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    // Otherwise: show city details (informational click)
    ShowCityDetails(cityName);
}

/// <summary>
/// Shows detailed information about a city
/// </summary>
private void ShowCityDetails(string cityName)
{
    var city = _gameState.Board.GetCity(cityName);
    if (city == null) return;
    
    var details = new System.Text.StringBuilder();
    details.AppendLine($"=== {cityName} ===");
    details.AppendLine();
    
    if (city.IsPort)
        details.AppendLine("âš“ PORT CITY");
    
    if (city.MorningAction.HasValue)
        details.AppendLine($"â˜€ï¸ Morning: {city.MorningAction}");
    
    if (city.AllDayAction.HasValue)
        details.AppendLine($"ðŸŒ All-Day: {city.AllDayAction}");
    
    details.AppendLine();
    details.AppendLine($"Attractions ({city.Attractions.Count(a => a.OwnerId.HasValue)}/{city.MaxAttractionSpaces}):");
    
    foreach (var attraction in city.Attractions.Where(a => a.OwnerId.HasValue))
    {
        var owner = _gameState.Players.FirstOrDefault(p => p.Id == attraction.OwnerId);
        string ownerName = owner?.Name ?? "Unknown";
        string attractionName = attraction.GetName(_gameState.Settings.Language);
        details.AppendLine($"  â€¢ {attractionName} ({attraction.Value} pips) - {ownerName}");
    }
    
    var busList = _gameState.Board.Buses.Where(b => b.CurrentCity == cityName).ToList();
    if (busList.Any())
    {
        details.AppendLine();
        details.AppendLine("ðŸšŒ Buses here:");
        foreach (var bus in busList)
        {
            details.AppendLine($"  Bus {bus.Id + 1} ({bus.Tourists.Count} tourists)");
        }
    }
    
    MessageBox.Show(details.ToString(), $"City Details: {cityName}", 
        MessageBoxButton.OK, MessageBoxImage.Information);
}

// ========================================
// END OF CODE ADDITIONS
// ========================================

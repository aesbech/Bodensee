using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.UI
{
    public partial class BuildAttractionDialog : Window
    {
        private GameState _gameState;
        private int _discount;

        public string? SelectedCityName { get; private set; }
        public Attraction? SelectedAttraction { get; private set; }

        public BuildAttractionDialog(GameState gameState, int discount = 0)
        {
            InitializeComponent();
            _gameState = gameState;
            _discount = discount;

            LoadCities();
            LoadAttractions();

            Title = discount > 0 ? $"Build Attraction (€{discount} Discount)" : "Build Attraction";
        }

        private void LoadCities()
        {
            var cityOptions = new List<CityOption>();

            foreach (var city in _gameState.Board.Cities.Values)
            {
                int availableSpaces = city.MaxAttractionSpaces - city.Attractions.Count(a => a.OwnerId.HasValue);
                if (availableSpaces > 0)
                {
                    var option = new CityOption
                    {
                        CityName = city.Name,
                        DisplayText = $"{city.Name} ({availableSpaces} space{(availableSpaces > 1 ? "s" : "")} available)"
                    };
                    cityOptions.Add(option);
                }
            }

            CitiesPanel.ItemsSource = cityOptions;
        }

        private void LoadAttractions()
        {
            var attractionOptions = new List<AttractionOption>();
            int playerMoney = _gameState.CurrentPlayer.Money;

            // Group attractions by category
            foreach (AttractionCategory category in Enum.GetValues(typeof(AttractionCategory)))
            {
                var attractionsInCategory = _gameState.Market.AvailableAttractions[category];
                if (!attractionsInCategory.Any()) continue;

                foreach (var attraction in attractionsInCategory)
                {
                    int cost = _gameState.Market.GetCost(attraction.Category) - _discount;
                    bool canAfford = playerMoney >= cost;

                    var option = new AttractionOption
                    {
                        Attraction = attraction,
                        DisplayText = $"{attraction.GetName(_gameState.Settings.Language)} - {attraction.Value} pips - €{cost}" +
                                      (attraction.PaysBonusEuro ? " (+€1 bonus)" : "") +
                                      (attraction.GrantedAction.HasValue ? $" [Grants: {attraction.GrantedAction}]" : ""),
                        BackgroundBrush = GetCategoryBrush(category),
                        CanAfford = canAfford
                    };
                    attractionOptions.Add(option);
                }
            }

            AttractionsPanel.ItemsSource = attractionOptions;
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

        private void CityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string cityName)
            {
                SelectedCityName = cityName;
                UpdateBuildButton();
            }
        }

        private void AttractionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Attraction attraction)
            {
                SelectedAttraction = attraction;
                UpdateBuildButton();
            }
        }

        private void UpdateBuildButton()
        {
            BuildButton.IsEnabled = SelectedCityName != null && SelectedAttraction != null;
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CityOption
    {
        public string CityName { get; set; } = "";
        public string DisplayText { get; set; } = "";
    }

    public class AttractionOption
    {
        public Attraction Attraction { get; set; } = null!;
        public string DisplayText { get; set; } = "";
        public Brush BackgroundBrush { get; set; } = Brushes.Gray;
        public bool CanAfford { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BodenseeTourismus.UI
{
    public partial class DestinationDialog : Window
    {
        public string? SelectedDestination { get; private set; }

        public DestinationDialog(List<string> destinations)
        {
            InitializeComponent();
            LoadDestinations(destinations);
        }

        public DestinationDialog(Dictionary<string, List<string>> routeInfo)
        {
            InitializeComponent();
            LoadRoutesWithPassThrough(routeInfo);
        }

        private void LoadDestinations(List<string> destinations)
        {
            var routes = new List<RouteOption>();
            foreach (var dest in destinations)
            {
                routes.Add(new RouteOption
                {
                    Destination = dest,
                    DestinationText = $"→ {dest}",
                    PassThroughCities = new List<string>(),
                    PassThroughVisibility = Visibility.Collapsed
                });
            }
            RoutesPanel.ItemsSource = routes;
        }

        private void LoadRoutesWithPassThrough(Dictionary<string, List<string>> routeInfo)
        {
            var routes = new List<RouteOption>();
            foreach (var kvp in routeInfo)
            {
                var dest = kvp.Key;
                var passThroughCities = kvp.Value;

                var option = new RouteOption
                {
                    Destination = dest,
                    DestinationText = $"→ {dest}",
                    PassThroughCities = passThroughCities,
                    PassThroughVisibility = passThroughCities.Any() ? Visibility.Visible : Visibility.Collapsed
                };
                routes.Add(option);
            }
            RoutesPanel.ItemsSource = routes;
        }

        private void DestinationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string destination)
            {
                SelectedDestination = destination;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class RouteOption
    {
        public string Destination { get; set; } = "";
        public string DestinationText { get; set; } = "";
        public List<string> PassThroughCities { get; set; } = new List<string>();
        public Visibility PassThroughVisibility { get; set; }
    }
}

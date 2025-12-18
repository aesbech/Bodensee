using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.UI
{
    public partial class AttractionEditorWindow : Window
    {
        private List<AttractionModel> _attractions;
        private AttractionModel? _selectedAttraction;

        public AttractionEditorWindow()
        {
            InitializeComponent();
            LoadDefaultAttractions();
            RefreshGrid();
            UpdateStatistics();
        }

        private void LoadDefaultAttractions()
        {
            // Load default attractions from GameSetup
            _attractions = new List<AttractionModel>();
            // For now, just create empty list - user can add attractions
        }

        private void RefreshGrid()
        {
            var filtered = _attractions.AsEnumerable();

            if (FilterNature.IsChecked == true)
                filtered = filtered.Where(a => a.Category == AttractionCategory.Nature);
            else if (FilterWater.IsChecked == true)
                filtered = filtered.Where(a => a.Category == AttractionCategory.Water);
            else if (FilterCulture.IsChecked == true)
                filtered = filtered.Where(a => a.Category == AttractionCategory.Culture);
            else if (FilterGastronomy.IsChecked == true)
                filtered = filtered.Where(a => a.Category == AttractionCategory.Gastronomy);
            else if (FilterGray.IsChecked == true)
                filtered = filtered.Where(a => a.Category == AttractionCategory.Gray);

            AttractionsGrid.ItemsSource = filtered.ToList();
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            var stats = $"Total: {_attractions.Count}\n";
            stats += $"Nature: {_attractions.Count(a => a.Category == AttractionCategory.Nature)}\n";
            stats += $"Water: {_attractions.Count(a => a.Category == AttractionCategory.Water)}\n";
            stats += $"Culture: {_attractions.Count(a => a.Category == AttractionCategory.Culture)}\n";
            stats += $"Gastronomy: {_attractions.Count(a => a.Category == AttractionCategory.Gastronomy)}\n";
            stats += $"Gray: {_attractions.Count(a => a.Category == AttractionCategory.Gray)}";
            StatsText.Text = stats;
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            RefreshGrid();
        }

        private void AttractionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AttractionsGrid.SelectedItem is AttractionModel selected)
            {
                _selectedAttraction = selected;
                LoadAttractionIntoEditor(selected);
                EditPanel.IsEnabled = true;
            }
            else
            {
                EditPanel.IsEnabled = false;
            }
        }

        private void LoadAttractionIntoEditor(AttractionModel attraction)
        {
            EditId.Text = attraction.Id.ToString();
            EditCategory.SelectedIndex = (int)attraction.Category;
            EditNameEnglish.Text = attraction.NameEnglish;
            EditNameGerman.Text = attraction.NameGerman;
            EditValue.Text = attraction.Value.ToString();
            EditPriority.Text = attraction.Priority.ToString();
            EditBonusEuro.IsChecked = attraction.PaysBonusEuro;

            if (attraction.GrantedAction.HasValue)
                EditGrayEffect.SelectedItem = EditGrayEffect.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == attraction.GrantedAction.ToString());
            else
                EditGrayEffect.SelectedIndex = 0;
        }

        private void EditCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Gray category enables the gray effect dropdown
            if (EditGrayEffect != null)
                EditGrayEffect.IsEnabled = EditCategory.SelectedIndex == 4; // Gray
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAttraction == null) return;

            try
            {
                _selectedAttraction.Category = (AttractionCategory)EditCategory.SelectedIndex;
                _selectedAttraction.NameEnglish = EditNameEnglish.Text;
                _selectedAttraction.NameGerman = EditNameGerman.Text;
                _selectedAttraction.Value = int.Parse(EditValue.Text);
                _selectedAttraction.Priority = (AttractPriority)int.Parse(EditPriority.Text);
                _selectedAttraction.PaysBonusEuro = EditBonusEuro.IsChecked == true;

                if (EditGrayEffect.SelectedIndex > 0 && EditGrayEffect.SelectedItem is ComboBoxItem item)
                {
                    if (Enum.TryParse<AllDayAction>(item.Content.ToString(), out var action))
                        _selectedAttraction.GrantedAction = action;
                }
                else
                {
                    _selectedAttraction.GrantedAction = null;
                }

                RefreshGrid();
                StatusText.Text = "Changes saved successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAttraction_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAttraction == null) return;

            var result = MessageBox.Show(
                $"Delete {_selectedAttraction.NameEnglish}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _attractions.Remove(_selectedAttraction);
                _selectedAttraction = null;
                RefreshGrid();
                StatusText.Text = "Attraction deleted";
            }
        }

        private void DuplicateAttraction_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAttraction == null) return;

            var newId = _attractions.Max(a => a.Id) + 1;
            var duplicate = new AttractionModel
            {
                Id = newId,
                Category = _selectedAttraction.Category,
                NameEnglish = _selectedAttraction.NameEnglish + " (Copy)",
                NameGerman = _selectedAttraction.NameGerman + " (Kopie)",
                Value = _selectedAttraction.Value,
                Priority = _selectedAttraction.Priority,
                PaysBonusEuro = _selectedAttraction.PaysBonusEuro,
                GrantedAction = _selectedAttraction.GrantedAction
            };

            _attractions.Add(duplicate);
            RefreshGrid();
            StatusText.Text = $"Created duplicate with ID {newId}";
        }

        private void AddNature_Click(object sender, RoutedEventArgs e) => AddNewAttraction(AttractionCategory.Nature);
        private void AddWater_Click(object sender, RoutedEventArgs e) => AddNewAttraction(AttractionCategory.Water);
        private void AddCulture_Click(object sender, RoutedEventArgs e) => AddNewAttraction(AttractionCategory.Culture);
        private void AddGastronomy_Click(object sender, RoutedEventArgs e) => AddNewAttraction(AttractionCategory.Gastronomy);
        private void AddGray_Click(object sender, RoutedEventArgs e) => AddNewAttraction(AttractionCategory.Gray);

        private void AddNewAttraction(AttractionCategory category)
        {
            var newId = _attractions.Any() ? _attractions.Max(a => a.Id) + 1 : 1;
            var newAttraction = new AttractionModel
            {
                Id = newId,
                Category = category,
                NameEnglish = $"New {category} Attraction",
                NameGerman = $"Neue {category} Attraktion",
                Value = 1,
                Priority = AttractPriority.Medium
            };

            _attractions.Add(newAttraction);
            RefreshGrid();
            StatusText.Text = $"Added new {category} attraction";
        }

        private void LoadFromCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("CSV import not yet implemented", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveToCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("CSV export not yet implemented", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all attractions to default values?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                LoadDefaultAttractions();
                RefreshGrid();
                StatusText.Text = "Reset to defaults";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Model class for the editor
    public class AttractionModel
    {
        public int Id { get; set; }
        public AttractionCategory Category { get; set; }
        public string NameEnglish { get; set; } = "";
        public string NameGerman { get; set; } = "";
        public int Value { get; set; }
        public AttractPriority Priority { get; set; }
        public bool PaysBonusEuro { get; set; }
        public AllDayAction? GrantedAction { get; set; }
    }
}

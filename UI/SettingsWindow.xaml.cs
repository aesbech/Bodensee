using System;
using System.Windows;
using BodenseeTourismus.Core;

namespace BodenseeTourismus.UI
{
    public partial class SettingsWindow : Window
    {
        public GameSettings Settings { get; private set; }
        public bool Applied { get; private set; }

        public SettingsWindow(GameSettings currentSettings)
        {
            InitializeComponent();
            Settings = currentSettings;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load starting money
            Player1MoneyBox.Text = Settings.Player1StartMoney.ToString();
            Player2MoneyBox.Text = Settings.Player2StartMoney.ToString();
            Player3MoneyBox.Text = Settings.Player3StartMoney.ToString();
            Player4MoneyBox.Text = Settings.Player4StartMoney.ToString();

            // Load attraction costs
            NatureCostBox.Text = Settings.NatureBaseCost.ToString();
            WaterCostBox.Text = Settings.WaterBaseCost.ToString();
            CultureCostBox.Text = Settings.CultureBaseCost.ToString();
            GastronomyCostBox.Text = Settings.GastronomyBaseCost.ToString();
            GrayCostBox.Text = Settings.GrayBaseCost.ToString();

            // Load game mechanics
            UseAppealCheckBox.IsChecked = Settings.UseAppealSystem;
            EnableGrayCheckBox.IsChecked = Settings.EnableGrayAttractions;

            // Load gray attraction settings
            ZentrumPipsBox.Text = Settings.ZentrumPipsBonus.ToString();
            CasinoRerollsBox.Text = Settings.CasinoRerollsPerBus.ToString();
            
            // Load bonus euro setting
            UseBonusEuroCheckBox.IsChecked = Settings.UseBonusEuro;
            
            // Load language
            if (Settings.Language == GameSettings.AttractionLanguage.English)
                LanguageEnglishRadio.IsChecked = true;
            else
                LanguageGermanRadio.IsChecked = true;

            // Load refill mode
            if (Settings.TouristRefillMode == GameSettings.RefillMode.FillMissing)
                RefillMissingRadio.IsChecked = true;
            else
                RefillWhenEmptyRadio.IsChecked = true;

            // Load tourist settings
            StartingTouristsBox.Text = Settings.StartingTouristsPerBus.ToString();
            MaxTouristsBox.Text = Settings.MaxTouristsPerBus.ToString();
            IncreaseValueBox.Text = Settings.IncreaseValueBonus.ToString();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse and validate all inputs
                Settings.Player1StartMoney = ParseInt(Player1MoneyBox.Text, "Player 1 Money");
                Settings.Player2StartMoney = ParseInt(Player2MoneyBox.Text, "Player 2 Money");
                Settings.Player3StartMoney = ParseInt(Player3MoneyBox.Text, "Player 3 Money");
                Settings.Player4StartMoney = ParseInt(Player4MoneyBox.Text, "Player 4 Money");

                Settings.NatureBaseCost = ParseInt(NatureCostBox.Text, "Nature Cost");
                Settings.WaterBaseCost = ParseInt(WaterCostBox.Text, "Water Cost");
                Settings.CultureBaseCost = ParseInt(CultureCostBox.Text, "Culture Cost");
                Settings.GastronomyBaseCost = ParseInt(GastronomyCostBox.Text, "Gastronomy Cost");
                Settings.GrayBaseCost = ParseInt(GrayCostBox.Text, "Gray Cost");

                Settings.UseAppealSystem = UseAppealCheckBox.IsChecked ?? true;
                Settings.EnableGrayAttractions = EnableGrayCheckBox.IsChecked ?? true;

                // Gray attraction settings
                Settings.ZentrumPipsBonus = ParseInt(ZentrumPipsBox.Text, "Zentrum Pips Bonus");
                Settings.CasinoRerollsPerBus = ParseInt(CasinoRerollsBox.Text, "Casino Rerolls");

                // Bonus euro setting
                Settings.UseBonusEuro = UseBonusEuroCheckBox.IsChecked ?? true;

                // Language
                Settings.Language = LanguageEnglishRadio.IsChecked == true
                    ? GameSettings.AttractionLanguage.English
                    : GameSettings.AttractionLanguage.German;

                Settings.TouristRefillMode = RefillMissingRadio.IsChecked == true
                    ? GameSettings.RefillMode.FillMissing
                    : GameSettings.RefillMode.FillWhenEmpty;

                Settings.StartingTouristsPerBus = ParseInt(StartingTouristsBox.Text, "Starting Tourists");
                Settings.MaxTouristsPerBus = ParseInt(MaxTouristsBox.Text, "Max Tourists");
                Settings.IncreaseValueBonus = ParseInt(IncreaseValueBox.Text, "Increase Value Bonus");

                Applied = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid input: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Applied = false;
            DialogResult = false;
        }

        private int ParseInt(string text, string fieldName)
        {
            if (int.TryParse(text, out int value) && value >= 0)
                return value;
            throw new Exception($"{fieldName} must be a non-negative integer");
        }

        // Preset buttons
        private void DefaultPreset_Click(object sender, RoutedEventArgs e)
        {
            Settings = new GameSettings(); // Reset to defaults
            LoadSettings();
        }

        private void HighMoneyPreset_Click(object sender, RoutedEventArgs e)
        {
            Settings.Player1StartMoney = 10;
            Settings.Player2StartMoney = 11;
            Settings.Player3StartMoney = 12;
            Settings.Player4StartMoney = 13;
            Settings.IncreaseValueBonus = 2;
            LoadSettings();
        }

        private void LowCostPreset_Click(object sender, RoutedEventArgs e)
        {
            Settings.NatureBaseCost = 1;
            Settings.WaterBaseCost = 1;
            Settings.CultureBaseCost = 1;
            Settings.GastronomyBaseCost = 2;
            Settings.GrayBaseCost = 1;
            LoadSettings();
        }

        private void NoAppealPreset_Click(object sender, RoutedEventArgs e)
        {
            Settings.UseAppealSystem = false;
            LoadSettings();
            MessageBox.Show(
                "Appeal system disabled!\n\n" +
                "Buses can now stop at any adjacent city regardless of tourist colors.\n" +
                "This creates a simpler movement system for testing.",
                "No Appeal Mode",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
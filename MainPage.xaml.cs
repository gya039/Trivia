using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace TriviaApp
{
    public partial class MainPage : ContentPage
    {
        private int playerCount;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnPlayerCountSelected(object sender, EventArgs e)
        {

            foreach (var child in playerNameInputs.Children.OfType<VisualElement>())
            {
                child.IsVisible = false;
            }

      
            var button = sender as Button;
            if (button == null) return;

            if (!int.TryParse(button.Text.Split(' ')[0], out playerCount))
            {
                DisplayAlert("Error", "Invalid player count selected.", "OK");
                return;
            }

      
            for (int i = 0; i < playerCount; i++)
            {
                if (playerNameInputs.Children[i] is VisualElement element)
                {
                    element.IsVisible = true;
                }
            }

   
            startGameButton.IsEnabled = true;
        }

        private async void OnStartGameClicked(object sender, EventArgs e)
        {
             var playerNames = playerNameInputs.Children
                .Take(playerCount)
                .OfType<Entry>()
                .Select((entry, index) => string.IsNullOrWhiteSpace(entry.Text) ? $"Player {index + 1}" : entry.Text)
                .ToArray();

            if (playerNames.Length == 0)
            {
                await DisplayAlert("Error", "Please enter at least one player name.", "OK");
                return;
            }

            try
            {
                await Navigation.PushAsync(new GamePage(playerNames));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start the game: {ex.Message}", "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new SettingsPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open settings: {ex.Message}", "OK");
            }
        }
    }
}
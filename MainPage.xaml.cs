using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace TriviaApp
{
    public partial class MainPage : ContentPage
    {
        private int teamCount;

        public MainPage()
        {
            InitializeComponent();
            CheckForSavedGame();
        }

        // checks for saved game
        private void CheckForSavedGame()
        {
            loadGameButton.IsVisible = Preferences.ContainsKey("SavedGame");
        }

        private async void OnLoadGameClicked(object sender, EventArgs e)
        {
            if (!Preferences.ContainsKey("SavedGame"))
            {
                await DisplayAlert("Error", "No saved game found.", "OK");
                return;
            }

            try
            {
                await Navigation.PushAsync(new GamePage(loadGame: true));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load the game: {ex.Message}", "OK");
            }
        }
        //team count that helps the rest of the code
        private async void OnTeamCountSelected(object sender, EventArgs e)
        {
            foreach (var child in teamNameInputs.Children.OfType<VisualElement>())
            {
                child.IsVisible = false;
                child.Opacity = 0;
            }

            var button = sender as Button;
            if (button == null || !int.TryParse(button.Text.Split(' ')[0], out teamCount))
            {
                await DisplayAlert("Error", "Invalid team count selected.", "OK");
                return;
            }

            teamNameInputs.IsVisible = true;
            for (int i = 0; i < teamCount; i++)
            {
                if (teamNameInputs.Children[i] is VisualElement element)
                {
                    element.IsVisible = true;
                    await element.FadeTo(1, 300, Easing.CubicIn);
                }
            }

            startGameButton.IsVisible = true;
            startGameButton.IsEnabled = true;
            await startGameButton.FadeTo(1, 500, Easing.CubicIn);
        }

        // logic 
        private async void OnStartGameClicked(object sender, EventArgs e)
        {
            var teamNames = teamNameInputs.Children
                .Take(teamCount)
                .OfType<Entry>()
                .Select((entry, index) => string.IsNullOrWhiteSpace(entry.Text) ? $"Team {index + 1}" : entry.Text)
                .ToArray();

            if (teamNames.Length == 0)
            {
                await DisplayAlert("Error", "Please enter at least one team name.", "OK");
                return;
            }

            bool startConfirmation = await DisplayAlert("Start Game", $"Are you ready to start the game with {teamCount} teams?", "Yes", "Cancel");
            if (!startConfirmation) return;

            try
            {
                await Navigation.PushAsync(new GamePage(teamNames));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start the game: {ex.Message}", "OK");
            }
        }
    }
}

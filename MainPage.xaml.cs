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

        
            var button = (Button)sender;
            playerCount = int.Parse(button.Text.Split(' ')[0]); 
           
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
                .Select(entry => entry.Text ?? $"Player {playerNameInputs.Children.IndexOf(entry) + 1}")
                .ToArray();

            await Navigation.PushAsync(new GamePage(playerNames));
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }
    }
}

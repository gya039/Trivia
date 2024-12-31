using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; 
namespace TriviaApp
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(questionsEntry.Text, out int questionCount) || questionCount <= 0)
            {
                await DisplayAlert("Error", "Please enter a valid number of questions.", "OK");
                return;
            }

            if (!int.TryParse(timeLimitEntry.Text, out int timeLimit) || timeLimit <= 0)
            {
                await DisplayAlert("Error", "Please enter a valid time limit.", "OK");
                return;
            }

            Preferences.Set("QuestionCount", questionCount);
            Preferences.Set("TimeLimit", timeLimit);

            await DisplayAlert("Settings Saved", "Your settings have been saved!", "OK");
            await Navigation.PopAsync();
        }
    }

}


using System.Net;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace TriviaApp
{
    public partial class GamePage : ContentPage
    {
        private enum GameRound
        {
            SingleJeopardy,
            DoubleJeopardy,
            FinalJeopardy
        }

        private GameRound currentRound = GameRound.SingleJeopardy;
        private int currentDollarAmount = 200;
        private string currentQuestion;
        private string[] currentAnswers;
        private string correctAnswer;
        private int[] scores;
        private int currentPlayerIndex = 0;
        private string[] playerNames;
        private Dictionary<string, List<(int value, string question, string[] answers, string correctAnswer)>> preloadedQuestionsByCategory = new();
        private bool isFinalJeopardy = false;
        private int[] wagers;
        private bool categoriesUpdatedForRound = false;

        public GamePage(string[] playerNames)
        {
            InitializeComponent();
            this.playerNames = playerNames;
            this.scores = new int[playerNames.Length];
            UpdateScoreLabel();
            UpdateScoreboard();
            UpdateCurrentPlayer();
        }

        public GamePage(bool loadGame)
        {
            InitializeComponent();

            if (loadGame)
            {
                LoadGame();
            }
            else
            {
                throw new ArgumentException("Constructor is for loading saved games only.");
            }
        }
        // to refresh
        private void UpdateCategoriesForRound()
        {
            if (categoriesUpdatedForRound)
                return;

            if (currentRound == GameRound.DoubleJeopardy)
            {
                string[] newCategories = { "History", "Celebrities", "General Knowledge", "Science", "Music" };

                for (int i = 0; i < newCategories.Length; i++)
                {
                    var label = ChangingGrid.Children.OfType<Label>().ElementAt(i);
                    label.Text = newCategories[i];

                    foreach (var button in ChangingGrid.Children.OfType<Button>().Where(b => Grid.GetColumn(b) == i))
                    {
                        var value = int.Parse(button.CommandParameter.ToString().Split('|')[1]);
                        button.CommandParameter = $"{newCategories[i]}|{value * 2}";
                        button.Text = $"${value * 2}";
                    }
                }
            }
            else if (currentRound == GameRound.FinalJeopardy)
            {
                ChangingGrid.Children.Clear();
                DisplayAlert("Final Jeopardy", "Prepare for the final round!", "OK");
            }
            categoriesUpdatedForRound = true;
        }
        // instantly show
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            foreach (var button in ChangingGrid.Children.OfType<Button>())
            {
                button.IsEnabled = false;
                button.BackgroundColor = Color.FromHex("#A9A9A9");
            }
            await Task.Delay(5000);
            foreach (var button in ChangingGrid.Children.OfType<Button>())
            {
                button.IsEnabled = true;
                button.BackgroundColor = Color.FromHex("#ffc72c");
            }
        }


        // populating ther Questions
        private async Task<List<(int value, string question, string[] answers, string correctAnswer)>> LoadCategoryQuestions(string categoryName, string url)
        {
            List<(int, string, string[], string)> questions = new();

            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return questions;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                if (!jsonDocument.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                {
                    return questions;
                }

                foreach (var questionData in results.EnumerateArray())
                {
                    string question = WebUtility.HtmlDecode(questionData.GetProperty("question").GetString());
                    string correctAnswer = WebUtility.HtmlDecode(questionData.GetProperty("correct_answer").GetString());
                    var incorrectAnswers = questionData.GetProperty("incorrect_answers").EnumerateArray();

                    string[] answers = new string[4]
                    {
                        WebUtility.HtmlDecode(correctAnswer),
                        WebUtility.HtmlDecode(incorrectAnswers.ElementAt(0).GetString()),
                        WebUtility.HtmlDecode(incorrectAnswers.ElementAt(1).GetString()),
                        WebUtility.HtmlDecode(incorrectAnswers.ElementAt(2).GetString())
                    };
                    ShuffleArray(answers);

                    questions.Add((0, question, answers, correctAnswer));
                }
            }
            catch (Exception ex)
            {
            }

            return questions;
        }



        private void RefreshUI()
        {
            UpdateScoreboard();

            UpdateCurrentPlayer();

            if (currentRound != GameRound.FinalJeopardy)
            {
                UpdateCategoriesForRound();
                foreach (var button in ChangingGrid.Children.OfType<Button>())
                {
                    button.IsEnabled = true;
                    button.BackgroundColor = Color.FromHex("#ffc72c");
                }
            }
            else
            {
                foreach (var button in ChangingGrid.Children.OfType<Button>())
                {
                    button.IsEnabled = false;
                    button.BackgroundColor = Color.FromHex("#ffc72c");
                }
            }
        }
        // More for testing but left it in just incase
        private async void OnSkipRoundClicked(object sender, EventArgs e)
        {
            if (currentRound == GameRound.SingleJeopardy)
            {
                currentRound = GameRound.DoubleJeopardy;
                categoriesUpdatedForRound = false;
                UpdateCategoriesForRound();
                await DisplayAlert("Round 2", "Now entering Double Jeopardy!", "OK");
            }
            else if (currentRound == GameRound.DoubleJeopardy)
            {
                currentRound = GameRound.FinalJeopardy;
                isFinalJeopardy = true;
                categoriesUpdatedForRound = false;
                UpdateCategoriesForRound();
                await StartFinalJeopardy();
            }
            else if (currentRound == GameRound.FinalJeopardy)
            {
                await DisplayAlert("Error", "You are already in the Final Jeopardy round.", "OK");
            }
        }

        // to do with pressing the $ buttons 
        private async void OnCategorySelected(object sender, EventArgs e)
        {
            if (sender is not Button button)
            {
                await DisplayAlert("Error", "Invalid button.", "OK");
                return;
            }

            var command = button.CommandParameter?.ToString()?.Split('|');
            if (command == null || command.Length != 2)
            {
                await DisplayAlert("Error", "Invalid button or command parameter.", "OK");
                return;
            }

            string category = command[0];
            if (!int.TryParse(command[1], out int value))
            {
                await DisplayAlert("Error", "Invalid value parameter.", "OK");
                return;
            }

            currentDollarAmount = value;
            if (currentRound == GameRound.DoubleJeopardy)
            {
            }
            if (!preloadedQuestionsByCategory.ContainsKey(category) || !preloadedQuestionsByCategory[category].Any())
            {
                string apiUrl = GetApiUrlForCategory(category);
                preloadedQuestionsByCategory[category] = await LoadCategoryQuestions(category, apiUrl);

                if (!preloadedQuestionsByCategory[category].Any())
                {
                    await DisplayAlert("Error", "No questions available for this category.", "OK");
                    return;
                }
            }

            var question = preloadedQuestionsByCategory[category].First();
            preloadedQuestionsByCategory[category].Remove(question);
            currentQuestion = question.question;
            currentAnswers = question.answers;
            correctAnswer = question.correctAnswer;
            button.IsEnabled = false;
            button.BackgroundColor = Color.FromHex("#FFD700");
            button.TextColor = Color.FromHex("#FFFFFF");
            await Navigation.PushAsync(new Answering(
                currentQuestion,
                currentAnswers,
                correctAnswer,
                currentDollarAmount,
                playerNames,
                isCorrect =>
                {
                    UpdateScoreboard();
                    currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Length;
                    UpdateCurrentPlayer();
                },
                isFinalJeopardy: currentRound == GameRound.FinalJeopardy,
                scores: scores,
                updateScoresCallback: updatedScores =>
                {

                    for (int i = 0; i < scores.Length; i++)
                    {
                        scores[i] = updatedScores[i];
                    }
                    UpdateScoreboard();
                }
            ));
        }

        private void UpdateCurrentPlayer()
        {
            currentPlayerLabel.Text = $"Current Player: {playerNames[currentPlayerIndex]}";
        }

        private void UpdateScoreLabel()
        {
            UpdateScoreboard();
        }
        // self explanitory
        private void UpdateScoreboard(int[] updatedScores = null)
        {
            if (updatedScores != null)
            {
                for (int i = 0; i < scores.Length; i++)
                {
                    scores[i] = updatedScores[i];
                }
            }

            scoreboardStack.Children.Clear();
            for (int i = 0; i < playerNames.Length; i++)
            {
                scoreboardStack.Children.Add(new Label
                {
                    Text = $"{playerNames[i]}: ${scores[i]}",
                    FontSize = 18,
                    TextColor = Color.FromHex("FFFFFF"),
                    HorizontalOptions = LayoutOptions.Center
                });
            }
        }

        //Api for categories
        private string GetApiUrlForCategory(string category)
        {
            return category switch
            {
                "Animals" => "https://opentdb.com/api.php?amount=5&category=27&type=multiple",
                "Mythology" => "https://opentdb.com/api.php?amount=5&category=20&type=multiple",
                "Sports" => "https://opentdb.com/api.php?amount=5&category=21&type=multiple",
                "Geography" => "https://opentdb.com/api.php?amount=5&category=22&type=multiple",
                "Politics" => "https://opentdb.com/api.php?amount=5&category=24&type=multiple",
                "History" => "https://opentdb.com/api.php?amount=5&category=23&type=multiple",
                "Celebrities" => "https://opentdb.com/api.php?amount=5&category=26&type=multiple",
                "General Knowledge" => "https://opentdb.com/api.php?amount=5&category=9&type=multiple",
                "Science" => "https://opentdb.com/api.php?amount=5&category=17&type=multiple",
                "Music" => "https://opentdb.com/api.php?amount=5&category=12&type=multiple",
                _ => throw new ArgumentException($"Invalid category: {category}")
            };
        }
        // makes it random everytime
        private void ShuffleArray(string[] array)
        {
            Random rand = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }
        private async Task StartFinalJeopardy()
        {
            ChangingGrid.Children.Clear();
            await DisplayAlert("Final Jeopardy", "It's time to place your wagers! Please enter your wager below.", "OK");

            wagers = new int[playerNames.Length];
            await DisplayWagerPrompt();

            if (!preloadedQuestionsByCategory.ContainsKey("Final Jeopardy"))
            {
                preloadedQuestionsByCategory["Final Jeopardy"] = await LoadCategoryQuestions(
                    "Final Jeopardy",
                    "https://opentdb.com/api.php?amount=1&difficulty=hard&type=multiple"
                );
            }

            var finalQuestion = preloadedQuestionsByCategory["Final Jeopardy"].FirstOrDefault();

            if (finalQuestion != default)
            {
                currentQuestion = finalQuestion.question;
                currentAnswers = finalQuestion.answers;
                correctAnswer = finalQuestion.correctAnswer;

                await Navigation.PushAsync(new Answering(
                    currentQuestion,
                    currentAnswers,
                    correctAnswer,
                    0,
                    playerNames,
                    isCorrect => { },
                    isFinalJeopardy: true,
                    wagers: wagers,
                    scores: scores,
                    updateScoresCallback: updatedScores =>
                    {
                        scores = updatedScores;
                        AnnounceFinalJeopardyWinner();
                    }
                ));
            }
            else
            {
                await DisplayAlert("Error", "Failed to load the Final Jeopardy question. Please try again.", "OK");
            }
        }
        // wager 
        private async Task DisplayWagerPrompt()
        {
            if (wagers == null || wagers.Length != playerNames.Length)
            {
                wagers = new int[playerNames.Length];
            }

            for (int i = 0; i < playerNames.Length; i++)
            {
                int maxWager = scores[i] < 0 ? 0 : scores[i];
                string result;

                do
                {
                    result = await DisplayPromptAsync("Wager",
                        $"{playerNames[i]}, how much would you like to wager? (Max: ${maxWager})",
                        initialValue: "0", keyboard: Keyboard.Numeric, maxLength: 5);

                    if (int.TryParse(result, out int wager) && wager >= 0 && wager <= maxWager)
                    {
                        wagers[i] = wager;
                        break;
                    }

                    await DisplayAlert("Invalid Wager", $"Please enter a valid wager between 0 and ${maxWager}.", "OK");
                }
                while (true);
            }
        }
        // save game
        private async Task SaveGameAsync()
        {
            var gameState = new GameState
            {
                PlayerNames = playerNames,
                Scores = scores,
                CurrentPlayerIndex = currentPlayerIndex,
                CurrentRound = currentRound.ToString(),
                PreloadedQuestions = preloadedQuestionsByCategory,
                CurrentDollarAmount = currentDollarAmount
            };

            var json = JsonSerializer.Serialize(gameState);
            Preferences.Set("SavedGame", json);
            await DisplayAlert("Game Saved", "Your game has been saved successfully!", "OK");
        }
        // load game
        private void LoadGame()
        {
            var savedGameJson = Preferences.Get("SavedGame", null);
            if (string.IsNullOrEmpty(savedGameJson))
            {
                DisplayAlert("Error", "No saved game found.", "OK");
                return;
            }

            try
            {
                var gameState = JsonSerializer.Deserialize<GameState>(savedGameJson);

                playerNames = gameState.PlayerNames;
                scores = gameState.Scores;
                currentPlayerIndex = gameState.CurrentPlayerIndex;
                currentRound = Enum.Parse<GameRound>(gameState.CurrentRound);
                preloadedQuestionsByCategory = gameState.PreloadedQuestions;
                currentDollarAmount = gameState.CurrentDollarAmount;

                categoriesUpdatedForRound = false;

                RefreshUI();
                UpdateCategoriesForRound();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to load the game state: {ex.Message}", "OK");
            }
        }
        // show winner
        private void AnnounceFinalJeopardyWinner()
        {
            int maxScore = scores.Max();
            var winners = playerNames
                .Where((_, index) => scores[index] == maxScore)
                .ToArray();

            string message = winners.Length > 1
                ? $"It's a tie! Winners: {string.Join(", ", winners)} with ${maxScore}."
                : $"The winner is {winners[0]} with ${maxScore}!";

            DisplayAlert("Game Over", message, "OK");

            Navigation.PopToRootAsync();
        }

        private async void OnSaveGameClicked(object sender, EventArgs e)
        {
            await SaveGameAsync();
        }
    }
}

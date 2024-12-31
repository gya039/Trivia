using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
        private int questionCount = 0;
        private string currentQuestion;
        private string[] currentAnswers;
        private string correctAnswer;
        private int[] scores;
        private int currentPlayerIndex = 0;
        private string[] playerNames;
        private Dictionary<string, List<(int value, string question, string[] answers, string correctAnswer)>> preloadedQuestionsByCategory = new();

        public GamePage(string[] playerNames)
        {
            InitializeComponent();
            this.playerNames = playerNames;
            this.scores = new int[playerNames.Length];
            PreloadCategories();
            UpdateScoreLabel();
        }

        private async Task PreloadCategories()
        {
            var categoriesRound1 = new Dictionary<string, string>
            {
                { "Animals", "https://opentdb.com/api.php?amount=5&category=27&type=multiple" },
                { "Mythology", "https://opentdb.com/api.php?amount=5&category=20&type=multiple" },
                { "Sports", "https://opentdb.com/api.php?amount=5&category=21&type=multiple" },
                { "Geography", "https://opentdb.com/api.php?amount=5&category=22&type=multiple" },
                { "Politics", "https://opentdb.com/api.php?amount=5&category=24&type=multiple" }
            };

            var categoriesRound2 = new Dictionary<string, string>
            {
                { "History", "https://opentdb.com/api.php?amount=5&category=23&type=multiple" },
                { "Celebrities", "https://opentdb.com/api.php?amount=5&category=26&type=multiple" },
                { "General Knowledge", "https://opentdb.com/api.php?amount=5&category=9&type=multiple" },
                { "Science", "https://opentdb.com/api.php?amount=5&category=17&type=multiple" },
                { "Music", "https://opentdb.com/api.php?amount=5&category=12&type=multiple" }
            };

            var categoriesFinalRound = new Dictionary<string, string>
            {
                { "Hard Question", "https://opentdb.com/api.php?amount=1&difficulty=hard&type=multiple" }
            };

    
            foreach (var category in categoriesRound1)
            {
                preloadedQuestionsByCategory[category.Key] = await LoadCategoryQuestions(category.Key, category.Value);
            }

          
            foreach (var category in categoriesRound2)
            {
                preloadedQuestionsByCategory[category.Key] = await LoadCategoryQuestions(category.Key, category.Value);
            }

         
            foreach (var category in categoriesFinalRound)
            {
                preloadedQuestionsByCategory[category.Key] = await LoadCategoryQuestions(category.Key, category.Value);
            }
        }

        private async Task<List<(int value, string question, string[] answers, string correctAnswer)>> LoadCategoryQuestions(string categoryName, string url)
        {
            List<(int, string, string[], string)> questions = new();
            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch questions for {categoryName}: {response.StatusCode}");
                    return questions;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                var results = jsonDocument.RootElement.GetProperty("results");

                int[] values = { 200, 400, 600, 800, 1000 };
                int index = 0;

                foreach (var questionData in results.EnumerateArray())
                {
                    if (index >= values.Length) break;

                    string question = questionData.GetProperty("question").GetString();
                    string correctAnswer = questionData.GetProperty("correct_answer").GetString();
                    var incorrectAnswers = questionData.GetProperty("incorrect_answers").EnumerateArray();

                    string[] answers = new string[4]
                    {
                        correctAnswer,
                        incorrectAnswers.ElementAt(0).GetString(),
                        incorrectAnswers.ElementAt(1).GetString(),
                        incorrectAnswers.ElementAt(2).GetString()
                    };
                    ShuffleArray(answers);

                    questions.Add((values[index], question, answers, correctAnswer));
                    index++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading questions for {categoryName}: {ex.Message}");
            }

            return questions;
        }

      
        private async void OnSkipToRound2Clicked(object sender, EventArgs e)
        {
            currentRound = GameRound.DoubleJeopardy;  
            currentDollarAmount = 400;  

            UpdateCategoriesForRound();
            await DisplayAlert("Round 2", "Now entering Double Jeopardy!", "OK");

        
            UpdateDollarAmounts();
        }

        private void UpdateCategoriesForRound()
        {
            if (currentRound == GameRound.SingleJeopardy)
            {
           
            }
            else if (currentRound == GameRound.DoubleJeopardy)
            {
               
                ChangingGrid.Children.OfType<Label>().ElementAt(0).Text = "History";
                ChangingGrid.Children.OfType<Label>().ElementAt(1).Text = "Celebrities";
                ChangingGrid.Children.OfType<Label>().ElementAt(2).Text = "General Knowledge";
                ChangingGrid.Children.OfType<Label>().ElementAt(3).Text = "Science";
                ChangingGrid.Children.OfType<Label>().ElementAt(4).Text = "Music";
            }
        }

        private void UpdateDollarAmounts()
        {
            foreach (Button button in ChangingGrid.Children.OfType<Button>())
            {
                if (button.CommandParameter != null)
                {
                    var command = button.CommandParameter.ToString().Split('|');
                    if (command.Length == 2)
                    {
                        int value = int.Parse(command[1]);
                        button.Text = $"${value * 2}"; 
                    }
                }
            }
        }

        private async void OnCategorySelected(object sender, EventArgs e)
        {
            Button button = sender as Button;
            var command = button?.CommandParameter?.ToString().Split('|');
            if (command == null || command.Length != 2)
            {
                await DisplayAlert("Error", "Invalid button or command parameter.", "OK");
                return;
            }

            string category = command[0];
            int value = int.Parse(command[1]);
            currentDollarAmount = value;

            
            if (currentRound == GameRound.DoubleJeopardy)
            {
                currentDollarAmount = value * 2;
            }

            if (!preloadedQuestionsByCategory.ContainsKey(category) || !preloadedQuestionsByCategory[category].Any())
            {
                string apiUrl = GetApiUrlForCategory(category);
                preloadedQuestionsByCategory[category] = await LoadCategoryQuestions(category, apiUrl);
            }

            if (preloadedQuestionsByCategory[category].Any())
            {
                var question = preloadedQuestionsByCategory[category].First();
                preloadedQuestionsByCategory[category].Remove(question);

                currentQuestion = question.question;
                currentAnswers = question.answers;
                correctAnswer = question.correctAnswer;

                await Navigation.PushAsync(new Answering(
                 currentQuestion,
                 currentAnswers,
                  correctAnswer,
                currentDollarAmount,
                 playerNames,
                 (isCorrect, buzzerPlayer) =>
                {
        Console.WriteLine($"[DEBUG] Received result: isCorrect={isCorrect}, buzzerPlayer={buzzerPlayer}");

        int buzzerPlayerIndex = Array.IndexOf(playerNames, buzzerPlayer);

        if (buzzerPlayerIndex >= 0)
        {
            Console.WriteLine($"[DEBUG] Updating score for player: {buzzerPlayer}");

           
            if (isCorrect)
            {
                scores[buzzerPlayerIndex] += currentDollarAmount;
            }
            else
            {
                scores[buzzerPlayerIndex] -= currentDollarAmount;
            }

            Console.WriteLine($"[DEBUG] Scores updated: {string.Join(", ", scores)}");
        }
        else
        {
            Console.WriteLine($"[ERROR] Player '{buzzerPlayer}' not found in playerNames.");
        }

        UpdateScoreLabel();
    }));
                button.IsEnabled = false;
            }
        }


        private void UpdateScoreLabel()
        {
            string scoreText = "Scores: ";
            for (int i = 0; i < playerNames.Length; i++)
            {
                scoreText += $"{playerNames[i]}: {scores[i]}  ";  
            }
            scoreLabel.Text = scoreText;  
            Console.WriteLine($"Updated Scores: {scoreText}");  
        }


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

        private void AnnounceWinner()
        {
            int maxScore = scores.Max();
            var winners = playerNames.Where((_, index) => scores[index] == maxScore).ToList();

            string winnerText = winners.Count > 1 ? "Winners" : "Winner";
            DisplayAlert("Game Over", $"{winnerText}: {string.Join(", ", winners)} with {maxScore} points!", "OK");
        }

        private void StartFinalJeopardy()
        {
            if (scores.Any(score => score > 0))
            {
                DisplayAlert("Final Jeopardy", "Time to place your wagers!", "OK");
            }
            else
            {
                AnnounceWinner();
            }
        }
    }
}
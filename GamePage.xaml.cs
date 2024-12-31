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
        private System.Timers.Timer buzzerTimer;
        private int currentPlayerScoreValue;
        private bool isTimerRunning = false;
        private System.Timers.Timer timer;
        private string[] playerNames;
        private int currentPlayerIndex = 0;
        private int[] scores;
        private Dictionary<string, List<(int value, string question, string[] answers, string correctAnswer)>> preloadedQuestionsByCategory = new();

        private string currentBuzzerPlayer = "";
        private bool isBuzzerTimerRunning = false;

        public GamePage(string[] playerNames)
        {
            InitializeComponent();
            this.playerNames = playerNames;
            this.scores = new int[playerNames.Length];
            PreloadCategories();
            SetupBuzzers();
        }

        private async Task PreloadCategories()
        {
            var categories = new Dictionary<string, string>
            {
                { "Animals", "https://opentdb.com/api.php?amount=5&category=27&type=multiple" },
                { "Mythology", "https://opentdb.com/api.php?amount=5&category=20&type=multiple" },
                { "Sports", "https://opentdb.com/api.php?amount=5&category=21&type=multiple" },
                { "Geography", "https://opentdb.com/api.php?amount=5&category=22&type=multiple" },
                { "Politics", "https://opentdb.com/api.php?amount=5&category=24&type=multiple" },
                { "History", "https://opentdb.com/api.php?amount=5&category=23&type=multiple" },
                { "Celebrities", "https://opentdb.com/api.php?amount=5&category=26&type=multiple" },
                { "General Knowledge", "https://opentdb.com/api.php?amount=5&category=9&type=multiple" }
            };

            foreach (var category in categories)
            {
                Console.WriteLine($"Preloading questions for category: {category.Key}");
                preloadedQuestionsByCategory[category.Key] = await LoadCategoryQuestions(category.Key, category.Value);
            }

            foreach (var category in preloadedQuestionsByCategory)
            {
                Console.WriteLine($"Category: {category.Key}, Questions Loaded: {category.Value?.Count ?? 0}");
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

            Console.WriteLine($"Loaded {questions.Count} questions for {categoryName}");
            return questions;
        }

        private void SetupBuzzers()
        {
            if (playerNames.Length == 1)
            {
                buzzerButton1.IsVisible = false;
            }
            else
            {
                for (int i = 0; i < playerNames.Length; i++)
                {
                    var buzzerButton = (Button)this.FindByName($"buzzerButton{i + 1}");
                    buzzerButton.IsVisible = true;
                }
            }
        }

        private async void OnBuzzIn(object sender, EventArgs e)
        {
            if (!isTimerRunning) return;

            Button buzzerButton = sender as Button;
            string buzzerPlayer = buzzerButton?.Text;

            if (string.IsNullOrEmpty(currentBuzzerPlayer))
            {
                currentBuzzerPlayer = buzzerPlayer;
                DisableBuzzers();
                timer.Stop();
                isTimerRunning = false;
                StartBuzzerTimer();
                await DisplayAlert("Buzzer", $"{currentBuzzerPlayer} buzzed in!", "OK");
            }
            else
            {
                await DisplayAlert("Buzzer", "Someone has already buzzed in!", "OK");
            }
        }

        private void DisableBuzzers()
        {
            buzzerButton1.IsEnabled = false;
            buzzerButton2.IsEnabled = false;
            buzzerButton3.IsEnabled = false;
            buzzerButton4.IsEnabled = false;
        }

        private void StartBuzzerTimer()
        {
            buzzerTimer = new System.Timers.Timer(5000);
            buzzerTimer.Elapsed += OnBuzzerTimerElapsed;
            buzzerTimer.Start();
            isBuzzerTimerRunning = true;
        }

        private void OnBuzzerTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            buzzerTimer.Stop();
            isBuzzerTimerRunning = false;
            Device.BeginInvokeOnMainThread(() => OnAnswerClicked(null, null));
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
                    isCorrect =>
                    {
                        if (isCorrect)
                        {
                            scores[currentPlayerIndex] += currentDollarAmount;
                        }
                        else
                        {
                            scores[currentPlayerIndex] -= currentDollarAmount;
                        }

                        UpdateScoreLabel();
                        currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Length;
                    }));
                button.IsEnabled = false;
            }
            else
            {
                await DisplayAlert("Error", $"No questions available for category: {category}.", "OK");
            }
        }





        private void DisplayQuestion()
        {
            if (string.IsNullOrEmpty(currentQuestion))
            {
                return;
            }

            questionLabel.Text = System.Net.WebUtility.HtmlDecode(currentQuestion);
            answerButton1.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[0]);
            answerButton2.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[1]);
            answerButton3.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[2]);
            answerButton4.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[3]);

            answerButton1.IsEnabled = true;
            answerButton2.IsEnabled = true;
            answerButton3.IsEnabled = true;
            answerButton4.IsEnabled = true;

            ResetButtonColors();
            currentPlayerLabel.Text = $"Current Player: {playerNames[currentPlayerIndex]}";
        }

        private async void OnAnswerClicked(object sender, EventArgs e)
        {
            if (!isTimerRunning) return;

            Button clickedButton = sender as Button;
            string selectedAnswer = clickedButton?.Text;

            if (selectedAnswer == correctAnswer)
            {
                scores[currentPlayerIndex] += currentPlayerScoreValue;
            }
            else
            {
                scores[currentPlayerIndex] -= currentPlayerScoreValue;
            }

            UpdateScoreLabel();
            HighlightButtons(selectedAnswer);
            DisableAnswerButtons();

            await Task.Delay(1000);
            DisplayCorrectAnswer();

            currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Length;

            bool anyQuestionsLeft = preloadedQuestionsByCategory.Values.Any(category => category.Any());

            if (!anyQuestionsLeft)
            {
                if (currentRound == GameRound.SingleJeopardy)
                {
                    currentRound = GameRound.DoubleJeopardy;
                }
                else if (currentRound == GameRound.DoubleJeopardy)
                {
                    currentRound = GameRound.FinalJeopardy;
                }
            }

            await Task.Delay(1000);
        }

        private void UpdateScoreLabel()
        {
            string scoreText = "Scores: ";
            for (int i = 0; i < playerNames.Length; i++)
            {
                scoreText += $"{playerNames[i]}: {scores[i]}  ";
            }
            scoreLabel.Text = scoreText;
        }

        private void ResetTimer()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            timer = new System.Timers.Timer(10000);
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
            isTimerRunning = true;
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
                _ => throw new ArgumentException($"Invalid category: {category}")
            };
        }

        private void DisableAnswerButtons()
        {
            answerButton1.IsEnabled = false;
            answerButton2.IsEnabled = false;
            answerButton3.IsEnabled = false;
            answerButton4.IsEnabled = false;
        }

        private void DisplayCorrectAnswer()
        {
            correctAnswerLabel.Text = $"The correct answer was: {correctAnswer}";
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isTimerRunning)
            {
                isTimerRunning = false;
                Device.BeginInvokeOnMainThread(() =>
                {
                    OnAnswerClicked(null, null);
                });
            }
        }

        private void StartTimer()
        {
            if (!isTimerRunning)
            {
                timer.Start();
                isTimerRunning = true;
            }
        }

        private void HighlightButtons(string selectedAnswer)
        {
            ResetButtonColors();
            HighlightButton(correctAnswer, Color.FromArgb("#00FF00"));
            if (selectedAnswer != correctAnswer)
            {
                HighlightButton(selectedAnswer, Color.FromArgb("#FF0000"));
            }
        }

        private void HighlightButton(string answer, Color color)
        {
            if (answer == answerButton1.Text) answerButton1.BackgroundColor = color;
            if (answer == answerButton2.Text) answerButton2.BackgroundColor = color;
            if (answer == answerButton3.Text) answerButton3.BackgroundColor = color;
            if (answer == answerButton4.Text) answerButton4.BackgroundColor = color;
        }

        private void ResetButtonColors()
        {
            answerButton1.BackgroundColor = Color.FromArgb("#1f92b8");
            answerButton2.BackgroundColor = Color.FromArgb("#1f92b8");
            answerButton3.BackgroundColor = Color.FromArgb("#1f92b8");
            answerButton4.BackgroundColor = Color.FromArgb("#1f92b8");
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
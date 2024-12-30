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
        private readonly string[] playerNames;
        private readonly int[] scores;
        private readonly Dictionary<string, List<(int value, string question, string[] answers, string correctAnswer)>> preloadedQuestionsByCategory = new();

        private int currentPlayerIndex;
        private int questionCount;
        private int totalQuestions;
        private int timeLimit;
        private string currentQuestion = string.Empty;
        private string[] currentAnswers = Array.Empty<string>();
        private string correctAnswer = string.Empty;
        private System.Timers.Timer? timer;
        private bool isTimerRunning;

        public GamePage(string[] playerNames)
        {
            InitializeComponent();

            this.playerNames = playerNames;
            scores = new int[playerNames.Length];
            totalQuestions = Preferences.Get("QuestionCount", 5);
            timeLimit = Preferences.Get("TimeLimit", 10);

            ResetButtonColors();
            _ = PreloadQuestionsAsync();
        }

        private async Task PreloadQuestionsAsync()
        {
            try
            {
                loadingSpinner.IsVisible = true;
                loadingSpinner.IsRunning = true;

                using HttpClient client = new HttpClient();
                var categories = new Dictionary<int, string>
                {
                    { 27, "Animals" },
                    { 20, "Mythology" },
                    { 21, "Sports" },
                    { 22, "Geography" },
                    { 24, "Politics" },
                    { 23, "History" },
                    { 26, "Celebrities" },
                    { 9, "General Knowledge" }
                };

                foreach (var category in categories)
                {
                    await Task.Delay(500);
                    var response = await client.GetAsync($"https://opentdb.com/api.php?amount=5&category={category.Key}&type=multiple");

                    if (!response.IsSuccessStatusCode) continue;

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(jsonResponse);

                    if (jsonDocument?.RootElement.TryGetProperty("results", out var results) != true) continue;

                    var categoryQuestions = new List<(int value, string question, string[] answers, string correctAnswer)>();
                    int[] values = { 200, 400, 600, 800, 1000 };

                    int index = 0;
                    foreach (var questionData in results.EnumerateArray())
                    {
                        if (index >= values.Length) break;

                        string question = questionData.GetProperty("question").GetString() ?? string.Empty;
                        string correct = questionData.GetProperty("correct_answer").GetString() ?? string.Empty;
                        var incorrectAnswers = questionData.GetProperty("incorrect_answers").EnumerateArray().Select(a => a.GetString() ?? string.Empty).ToList();

                        var answers = incorrectAnswers.Append(correct).ToArray();
                        ShuffleArray(answers);

                        categoryQuestions.Add((values[index], question, answers, correct));
                        index++;
                    }

                    if (categoryQuestions.Any())
                        preloadedQuestionsByCategory[category.Value] = categoryQuestions;
                }
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load questions.", "OK");
            }
            finally
            {
                loadingSpinner.IsVisible = false;
                loadingSpinner.IsRunning = false;
            }
        }

        private void OnCategorySelected(object sender, EventArgs e)
        {
            if (sender is not Button selectedButton) return;

            int value = int.Parse(selectedButton.CommandParameter.ToString());
            string category = ((Grid)selectedButton.Parent)?.Children.OfType<Label>().FirstOrDefault(l => Grid.GetColumn(l) == Grid.GetColumn(selectedButton))?.Text;

            if (string.IsNullOrWhiteSpace(category) || !preloadedQuestionsByCategory.TryGetValue(category, out var questions)) return;

            var selectedQuestion = questions.FirstOrDefault(q => q.value == value);
            if (selectedQuestion == default) return;

            preloadedQuestionsByCategory[category].Remove(selectedQuestion);

            currentQuestion = selectedQuestion.question;
            currentAnswers = selectedQuestion.answers;
            correctAnswer = selectedQuestion.correctAnswer;

            DisplayQuestion();
            ResetTimer();
            StartTimer();

            selectedButton.IsEnabled = false;
        }

        private void DisplayQuestion()
        {
            ResetButtonColors();
            questionLabel.Text = System.Net.WebUtility.HtmlDecode(currentQuestion);
            answerButton1.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[0]);
            answerButton2.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[1]);
            answerButton3.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[2]);
            answerButton4.Text = System.Net.WebUtility.HtmlDecode(currentAnswers[3]);

            currentPlayerLabel.Text = $"Current Player: {playerNames[currentPlayerIndex]}";
        }

        private void StartTimer()
        {
            timer?.Dispose();
            timer = new System.Timers.Timer(1000) { AutoReset = true };
            timer.Elapsed += (s, e) => Dispatcher.Dispatch(UpdateTimer);
            timer.Start();
        }

        private void ResetTimer()
        {
            timer?.Dispose();
            timeLimit = Preferences.Get("TimeLimit", 10);
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            timerLabel.Text = $"Time: {timeLimit--}s";
            if (timeLimit < 0) EndTurn();
        }

        private async void OnAnswerClicked(object sender, EventArgs e)
        {
            if (!isTimerRunning || sender is not Button clickedButton) return;

            timer.Stop();
            isTimerRunning = false;

            if (clickedButton.Text == correctAnswer) scores[currentPlayerIndex]++;

            currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Length;
            questionCount++;
            UpdateScoreLabel();

            await Task.Delay(1000);
            if (questionCount >= totalQuestions) EndGame();
        }

        private void UpdateScoreLabel()
        {
            scoreLabel.Text = string.Join("  ", playerNames.Select((p, i) => $"{p}: {scores[i]}"));
        }

        private void EndGame()
        {
            var winner = playerNames.Zip(scores).OrderByDescending(p => p.Second).First();
            DisplayAlert("Game Over", $"{winner.First} wins with {winner.Second} points!", "OK");
        }

        private void ResetButtonColors()
        {
            foreach (var button in new[] { answerButton1, answerButton2, answerButton3, answerButton4 })
                button.BackgroundColor = Color.FromArgb("#1f92b8");
        }

        private void ShuffleArray(string[] array)
        {
            Random rand = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        private void EndTurn()
        {
            timer?.Stop();
            isTimerRunning = false;
            OnAnswerClicked(null, EventArgs.Empty);
        }
    }
}

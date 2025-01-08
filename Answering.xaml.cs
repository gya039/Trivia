using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace TriviaApp
{
    public partial class Answering : ContentPage
    {
        private string question;
        private string[] answers;
        private string correctAnswer;
        private int currentDollarAmount;
        private string[] playerNames;
        private int activePlayerIndex = -1;
        private Action<bool> onAnswerCompleted;
        private bool turnEnded = false;
        private bool isFinalJeopardy;
        private int currentPlayerIndex;
        private bool[] playersAnswered;
        private int[] wagers;
        private int[] scores;
        private bool timerRunning = false;
        private Action<int[]> updateScoresCallback;

        public Answering(string question, string[] answers, string correctAnswer, int currentDollarAmount, string[] playerNames, Action<bool> onAnswerCompleted, bool isFinalJeopardy = false, int[] wagers = null, int[] scores = null, Action<int[]> updateScoresCallback = null)
        {
            InitializeComponent();
            this.question = question;
            this.answers = answers;
            this.correctAnswer = correctAnswer;
            this.currentDollarAmount = currentDollarAmount;
            this.playerNames = playerNames;
            this.onAnswerCompleted = onAnswerCompleted;
            this.isFinalJeopardy = isFinalJeopardy;
            this.wagers = wagers ?? new int[playerNames.Length];
            this.scores = scores ?? new int[playerNames.Length];
            this.updateScoresCallback = updateScoresCallback;

            if (isFinalJeopardy)
            {
                playersAnswered = new bool[playerNames.Length];
                currentPlayerIndex = 0;
            }

            InitializeUI();
        }

        private void InitializeUI()
        {
            questionLabel.Text = question;
            answerButton1.Text = answers[0];
            answerButton2.Text = answers[1];
            answerButton3.Text = answers[2];
            answerButton4.Text = answers[3];

            if (isFinalJeopardy)
            {
                buzzerButtonsStack.IsVisible = false;
                StartFinalJeopardyTurn();
            }
            else
            {
                buzzerButton1.IsVisible = playerNames.Length >= 1;
                buzzerButton2.IsVisible = playerNames.Length >= 2;
                buzzerButton3.IsVisible = playerNames.Length >= 3;
                buzzerButton4.IsVisible = playerNames.Length >= 4;
                StartCountdown();
            }
        }

        private void StartCountdown()
        {
            int countdown = 10;
            timerLabel.Text = $"Time Left: {countdown}s";

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (activePlayerIndex != -1)
                {
                    return false;
                }

                countdown--;
                timerLabel.Text = $"Time Left: {countdown}s";

                if (countdown <= 0)
                {
                    timerLabel.Text = "Time's Up!";
                    if (activePlayerIndex == -1)
                    {
                        DisplayAlert("Time's Up!", "No one buzzed in! Moving to the next question.", "OK");
                        Navigation.PopAsync();
                        return false;
                    }

                    return false;
                }

                return true;
            });
        }

        private void StartFinalJeopardyTurn()
        {
            if (currentPlayerIndex >= playerNames.Length)
            {
                RevealFinalJeopardyResult();
                return;
            }

            int countdown = 15;
            timerLabel.Text = $"{playerNames[currentPlayerIndex]}'s Turn: {countdown}s";
            playerTurnLabel.Text = $"{playerNames[currentPlayerIndex]}'s Turn";
            timerRunning = true;

            EnableAnswerButtons();

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (currentPlayerIndex >= playerNames.Length)
                {
                    return false;
                }

                countdown--;
                timerLabel.Text = $"{playerNames[currentPlayerIndex]}'s Turn: {countdown}s";

                if (countdown <= 0 || playersAnswered[currentPlayerIndex])
                {
                    if (!playersAnswered[currentPlayerIndex])
                    {
                        DisplayAlert("Time's Up!", $"{playerNames[currentPlayerIndex]} did not answer in time.", "OK");
                        playersAnswered[currentPlayerIndex] = true;
                    }

                    currentPlayerIndex++;
                    timerRunning = false;

                    if (currentPlayerIndex >= playerNames.Length)
                    {
                        RevealFinalJeopardyResult();
                    }
                    else
                    {
                        StartFinalJeopardyTurn();
                    }

                    return false;
                }

                return true;
            });
        }
        private void EndFinalJeopardy()
        {
            Navigation.PopAsync();
        }

        private void OnAnswerClicked(object sender, EventArgs e)
        {
            if (!isFinalJeopardy)
            {
                var standardAnswerButton = (Button)sender;
                bool standardIsCorrect = standardAnswerButton.Text == correctAnswer;

                scores[activePlayerIndex] += standardIsCorrect ? currentDollarAmount : -currentDollarAmount;

                updateScoresCallback?.Invoke(scores.ToArray());
                onAnswerCompleted?.Invoke(standardIsCorrect);

                activePlayerIndex = -1;
                turnEnded = true;
                Navigation.PopAsync();
                return;
            }

            if (playersAnswered[currentPlayerIndex])
            {
                DisplayAlert("Error", $"{playerNames[currentPlayerIndex]} has already answered.", "OK");
                return;
            }

            var finalAnswerButton = (Button)sender;
            bool finalIsCorrect = finalAnswerButton.Text == correctAnswer;

            playersAnswered[currentPlayerIndex] = true;
            scores[currentPlayerIndex] += finalIsCorrect ? wagers[currentPlayerIndex] : -wagers[currentPlayerIndex];

            DisplayAlert("Answer Locked", $"{playerNames[currentPlayerIndex]} locked their answer.", "OK");

            DisableAnswerButtons();

            currentPlayerIndex++;
            StartFinalJeopardyTurn();
        }

        private void RevealFinalJeopardyResult()
        {
            string resultMessage = "Final Jeopardy Results:\n\n";

            for (int i = 0; i < playerNames.Length; i++)
            {
                bool isCorrect = scores[i] >= wagers[i];
                resultMessage += $"{playerNames[i]}: {scores[i]} ({(isCorrect ? "Correct" : "Incorrect")})\n";
            }

            DisplayAlert("Correct Answer", $"The correct answer is: {correctAnswer}\n\n{resultMessage}", "OK");

            Navigation.PopAsync();
        }

        private void EnableAnswerButtons()
        {
            answerButton1.IsEnabled = true;
            answerButton2.IsEnabled = true;
            answerButton3.IsEnabled = true;
            answerButton4.IsEnabled = true;
        }

        private void DisableAnswerButtons()
        {
            answerButton1.IsEnabled = false;
            answerButton2.IsEnabled = false;
            answerButton3.IsEnabled = false;
            answerButton4.IsEnabled = false;
        }

        private void OnBuzzIn(object sender, EventArgs e)
        {
            var buzzer = (Button)sender;
            activePlayerIndex = buzzer == buzzerButton1 ? 0
                             : buzzer == buzzerButton2 ? 1
                             : buzzer == buzzerButton3 ? 2
                             : 3;

            ResetBuzzerStyles();
            buzzer.Style = (Style)Resources["ActiveBuzzerStyle"];

            EnableAnswerButtons();
        }

        private void ResetBuzzerStyles()
        {
            buzzerButton1.Style = (Style)Resources["InactiveBuzzerStyle"];
            buzzerButton2.Style = (Style)Resources["InactiveBuzzerStyle"];
            buzzerButton3.Style = (Style)Resources["InactiveBuzzerStyle"];
            buzzerButton4.Style = (Style)Resources["InactiveBuzzerStyle"];
        }

        private void OnReturnToGameBoard(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}

using System;
using Microsoft.Maui.Controls;

namespace TriviaApp
{
    public partial class Answering : ContentPage
    {
        private string correctAnswer;
        private int currentDollarAmount;
        private Action<bool, string> onAnswerCompleted;
        private bool isTimerRunning = false;
        private System.Timers.Timer timer;
        private string currentBuzzerPlayer = "";
        private string[] playerNames;

        public Answering(string question, string[] answers, string correctAnswer, int dollarValue, string[] playerNames, Action<bool, string> onAnswerCompleted)
        {
            InitializeComponent();
            this.correctAnswer = correctAnswer;
            this.currentDollarAmount = dollarValue;
            this.playerNames = playerNames;
            this.onAnswerCompleted = onAnswerCompleted;

            questionLabel.Text = question;
            answerButton1.Text = answers[0];
            answerButton2.Text = answers[1];
            answerButton3.Text = answers[2];
            answerButton4.Text = answers[3];

            SetupBuzzers();
            StartTimer();
        }

        private void SetupBuzzers()
        {
            if (playerNames.Length == 1)
            {
          
                currentBuzzerPlayer = playerNames[0];
                EnableAnswerButtons();
                buzzerButtonsStack.IsVisible = false;
            }
            else
            {
              
                buzzerButton1.IsVisible = playerNames.Length > 0;
                buzzerButton2.IsVisible = playerNames.Length > 1;
                buzzerButton3.IsVisible = playerNames.Length > 2;
                buzzerButton4.IsVisible = playerNames.Length > 3;
            }
        }

        private void OnBuzzIn(object sender, EventArgs e)
        {
            if (!isTimerRunning) return;

            Button buzzerButton = sender as Button;

          
            currentBuzzerPlayer = buzzerButton?.Text.Replace(" Buzzer", "").Trim();

            Console.WriteLine($"[DEBUG] Player who buzzed in: {currentBuzzerPlayer}");

            DisableBuzzers();
            EnableAnswerButtons();

            DisplayAlert("Buzzed In", $"{currentBuzzerPlayer} buzzed in!", "OK");
        }



        private void DisableBuzzers()
        {
            buzzerButton1.IsEnabled = false;
            buzzerButton2.IsEnabled = false;
            buzzerButton3.IsEnabled = false;
            buzzerButton4.IsEnabled = false;
        }

        private void EnableAnswerButtons()
        {
            answerButton1.IsEnabled = true;
            answerButton2.IsEnabled = true;
            answerButton3.IsEnabled = true;
            answerButton4.IsEnabled = true;
        }

        private void OnAnswerClicked(object sender, EventArgs e)
        {
            if (!isTimerRunning || string.IsNullOrEmpty(currentBuzzerPlayer)) return;

            Button clickedButton = sender as Button;
            string selectedAnswer = clickedButton?.Text;

            timer.Stop();
            isTimerRunning = false;

            bool isCorrect = selectedAnswer == correctAnswer;

            Console.WriteLine($"[DEBUG] Player: {currentBuzzerPlayer}, Answer: {selectedAnswer}, Correct: {isCorrect}");
 
            onAnswerCompleted?.Invoke(isCorrect, currentBuzzerPlayer);

            
            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                Navigation.PopAsync();
                return false;
            });
        }



        private void HighlightButtons(string selectedAnswer)
        {
            ResetButtonColors();
            if (selectedAnswer == correctAnswer)
            {
                HighlightButton(selectedAnswer, Color.FromArgb("#00FF00")); 
            }
            else
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

        private void StartTimer()
        {
            timer = new System.Timers.Timer(10000); 
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
            isTimerRunning = true;
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            isTimerRunning = false;

            Device.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Time's Up", "You didn't answer in time!", "OK");
                onAnswerCompleted?.Invoke(false, currentBuzzerPlayer); 
                Navigation.PopAsync();
            });
        }

        private void OnReturnToGameBoard(object sender, EventArgs e)
        {
            timer?.Stop();
            Navigation.PopAsync();
        }
    }
}
public class GameState
{
    public string[] PlayerNames { get; set; }
    public int[] Scores { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public string CurrentRound { get; set; } 
    public Dictionary<string, List<(int, string, string[], string)>> PreloadedQuestions { get; set; }
    public int CurrentDollarAmount { get; set; }
}

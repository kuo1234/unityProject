public static class RoundResultStore
{
    public static int Score { get; private set; }
    public static int Mistakes { get; private set; }
    public static int Accuracy { get; private set; }
    public static bool HasResult { get; private set; }

    public static void Save(int score, int mistakes, int accuracy)
    {
        Score = score;
        Mistakes = mistakes;
        Accuracy = accuracy;
        HasResult = true;
    }

    public static void Clear()
    {
        Score = 0;
        Mistakes = 0;
        Accuracy = 0;
        HasResult = false;
    }
}

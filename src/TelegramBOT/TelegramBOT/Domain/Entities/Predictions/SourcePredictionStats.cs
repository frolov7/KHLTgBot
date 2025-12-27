namespace TelegramBOT.Domain.Entities.Predictions
{
    public class SourcePredictionStats
    {
        public string Source { get; set; } = null!;

        public int Total { get; set; }
        public int Win { get; set; }
        public int Lose { get; set; }

        public double HitRate { get; set; }
        public double Rating { get; set; }

        public Dictionary<string, TypeStats> Types { get; set; } = new();

        public string? BestType { get; set; }

        public List<bool> LastResults { get; set; } = new();
    }
}

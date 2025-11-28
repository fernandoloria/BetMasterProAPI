namespace BetMasterApiCore.Models
{
    public class SportDto
    {
        public int SportId { get; set; }
        public string SportName { get; set; } = "";
        public bool IsTournament { get; set; }
        public int TotalLeagues { get; set; }
        public int TotalGames { get; set; }
        public int Orden { get; set; }
    }
}

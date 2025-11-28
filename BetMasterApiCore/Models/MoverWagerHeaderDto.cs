namespace BetMasterApiCore.Models
{

    public class MoverWagerHeaderDto
    {
        public int IdLiveWager { get; set; }
        public int IdPlayer { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public int IdWagerType { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
        public string Description { get; set; }
        public int Result { get; set; }
        public bool Graded { get; set; }
        public int NumDetails { get; set; }
        public int DgsIdWager { get; set; }
        public string Player { get; set; }
        public string Agent { get; set; }

        public string SportName { get; set; }

        public string LeagueName { get; set; }
        public List<MoverWagerDetailDto> Details { get; set; }
    }
}

namespace BetMasterApiCore.Models
{

    public class MoverWagerDetailDto
    {
        public int IdLiveWagerDetail { get; set; }
        public int IdLiveWager { get; set; }
        public int FixtureId { get; set; }
        public int MarketId { get; set; }
        public string LineId { get; set; }
        public string BaseLine { get; set; }
        public string Line { get; set; }
        public int Odds { get; set; }
        public decimal Price { get; set; }
        public string PickTeam { get; set; }
        public int Result { get; set; }
        public string CompleteDescription { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
    }
}

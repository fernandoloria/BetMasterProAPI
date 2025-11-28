namespace BetMasterApiCore.Models
{
    public class LSportSimpleInsertParlay
    {
        public string HeaderDescription { get; set; }
        public string DetailDescription { get; set; }
        public int MarketId { get; set; }
        public int FixtureId { get; set; }
        public string Points { get; set; }
        public string Odds { get; set; }
        public Decimal Risk { get; set; }
        public Decimal Win { get; set; }
        public string WagerSelectionPlay { get; set; }
        public int IdPlayer { get; set; }
        public int IdWagerType { get; set; }
        public int IsLive { get; set; }
        public string SideName { get; set; }
        public int NumTeams { get; set; }
        public string KeyDetails { get; set; }
    }
}

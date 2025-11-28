namespace BetMasterApiCore.Models
{
    public class LSportSimpleInsertStraight
    {
        public string HeaderDescription { get; set; }
        public string DetailDescription { get; set; }
        public int MarketId { get; set; }
        public int FixtureId { get; set; }
        public Decimal Line { get; set; }
        public Decimal Odds { get; set; }
        public Decimal Risk { get; set; }
        public Decimal Win { get; set; }
        public int WagerSelection { get; set; }
        public int IdPlayer { get; set; }
        public int IdWagerType { get; set; }
        public int IsLive { get; set; }
        public string SideName { get; set; }
    }
}

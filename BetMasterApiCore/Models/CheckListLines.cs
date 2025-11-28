namespace BetMasterApiCore.Models
{
    public class CheckListLines
    {
        public int FixtureId { get; set; }
        public int MarketId { get; set; }
        public long BetId { get; set; }
        public Bet? BetInfo { get; set; }

    }
}

namespace BetMasterApiCore.Models
{
    public class PlayerLimitsHierarchyStraight
    {
        public int PlayerId { get; set; }
        public int? SportId { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSportLimit { get; set; }
        public bool IsLeagueLimit { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotAmtGame { get; set; }
    }
}

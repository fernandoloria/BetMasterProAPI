namespace BetMasterApiCore.Models
{
    public class PlayerLimitsDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int IdWagerType { get; set; }
        public int SportId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureId { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPayout { get; set; }
    }
}

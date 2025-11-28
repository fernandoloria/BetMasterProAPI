namespace BetMasterApiCore.Models
{
    public class WagerDetailCompleteDescriptionModel
    {
        public string? SportName { get; set; }
        public string? HomeTeam { get; set; }
        public string? VisitorTeam { get; set; }
        public int MarketId { get; set; }
        public string? MarketName { get; set; }
        public string? Name { get; set; }
        public string? BaseLine { get; set; }
        public string? Line { get; set; }
        public int? Odds1 { get; set; }
        public bool? IsTournament { get; set; }
        public string? LeagueName { get; set; }
        public int FixtureId { get; set; }
    }
}
namespace BetMasterApiCore.Models
{

    public class FixtureDto
    {
        public int FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int StatusId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public DateTime EventStartDate { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
    }
}

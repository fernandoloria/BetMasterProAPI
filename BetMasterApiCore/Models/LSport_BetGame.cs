namespace BetMasterApiCore.Models
{

    public class LSport_BetGame
    {
        public int FixtureId { get; set; }
        public string? VisitorTeam { get; set; }
        public string? HomeTeam { get; set; }
        public string SportName { get; set; }
        public string LeagueName { get; set; }
        public bool? IsTournament { get; set; }
        public List<LSport_EventPropDto> Selections { get; set; }
    }
}

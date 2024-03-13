namespace WolfApiCore.Models
{
    public class CreateStraightWagerModel
    {
        public LSport_EventPropDto? PropSelected { get; set; }
        public int? FixtureId { get; set; }
        public int? IdPlayer { get; set; }
        public string? HomeTeam { get; set; }
        public string? VisitorTeam { get; set; }
        public string? SportName { get; set; }
        public bool? IsMobile { get; set; }
        public string? LeagueName { get; set; }
        public bool? IsTournament { get; set; }
    }
}

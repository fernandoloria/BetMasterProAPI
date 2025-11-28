namespace BetMasterApiCore.Models
{

    //public class LSport_ScreenLocationDto
    //{
    //    public bool ShowLocation { get; set; }
    //    public string LocationName { get; set; }
    //    public int LocationId { get; set; }
    //    public List<LSport_ScreenLeagueDto>? Leagues { get; set; }
    //}

    public class LSport_ScreenLeagueDto
    {
        public bool ShowLeague { get; set; }
        public string LeagueName { get; set; }
        public int LeagueId { get; set; }
        public bool? IsTournament { get; set; }
        public List<LSportGameDto>? Games { get; set; }
    }
}

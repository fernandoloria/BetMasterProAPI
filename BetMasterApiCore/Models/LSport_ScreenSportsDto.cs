namespace BetMasterApiCore.Models
{

    //*********  methods to show scores ***********

    public class LSport_ScreenSportsDto
    {
        public bool ShowSport { get; set; }
        public string SportName { get; set; }
        public int SportId { get; set; }
        //  public List<LSport_ScreenLocationDto>? Locations { get; set; }
        public List<LSport_ScreenLeagueDto>? Leagues { get; set; }
    }
}

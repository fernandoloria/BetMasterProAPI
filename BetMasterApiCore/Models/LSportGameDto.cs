namespace BetMasterApiCore.Models
{
    public class LSportGameDto
    {
        //  public int MsgSeq { get; set; }
        //  public string? MsgGuid { get; set; }
        public int FixtureId { get; set; }
        //public int Status { get; set; }
        public int StatusId { get; set; }
        public string? Status_Description { get; set; }
        public string? Status_DescriptionCss { get; set; }
        public int CurrentPeriod { get; set; }
        public bool PremiumGame { get; set; }
        public int BetCount { get; set; }
        public int GameTime { get; set; }
        public string PeriodDesc { get; set; }
        public string GameStatus { get; set; }
        public int VisitorScore { get; set; }
        public int HomeScore { get; set; }
        public DateTime ScoreCreatedDateTime { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public DateTime EventFixtureDateTime { get; set; }
        public int VisitorRotation { get; set; }
        public int HomeRotation { get; set; }
        public int VisitorTeamId { get; set; }
        public int HomeTeamId { get; set; }
        public DateTime EventCreatedDateTime { get; set; }
        public int IdGame { get; set; }
        public string? VisitorTeam { get; set; }
        public string? HomeTeam { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int? GameId { get; set; }
        public bool ShowLines { get; set; }
        public int TotalLines { get; set; }
        //public LSport_EventValuesDto? Line { get; set; }
        public List<LSport_EventPropMarketDto>? PropMarkets { get; set; }
        //public LSport_MainLine? MainLine { get; set; }
        public bool? IsTournament { get; set; }
    }
}

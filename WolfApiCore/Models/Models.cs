using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;

namespace WolfApiCore.Models
{
    public class LSportGameDto
    {
        //  public int MsgSeq { get; set; }
        //  public string? MsgGuid { get; set; }
        public Int32 FixtureId { get; set; }
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
        public Int32 SportId { get; set; }
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
        public LSport_EventValuesDto? Line { get; set; }
        public List<LSport_EventPropMarketDto>? PropMarkets { get; set; }
        public LSport_MainLine? MainLine { get; set; }
    }

    public class LSport_MainLine
    {
        public LSport_ItemMainLine? SpredVisitor { get; set; }
        public LSport_ItemMainLine? SpredHome { get; set; }
        public LSport_ItemMainLine? TotalOver { get; set; }
        public LSport_ItemMainLine? TotalUnder { get; set; }
        public LSport_ItemMainLine? MlVisitor { get; set; }
        public LSport_ItemMainLine? MlHome { get; set; }
        public LSport_ItemMainLine? MlDraw { get; set; }
    }

    public class LSport_ItemMainLine
    {
        public int MarketId { get; set; }
        public string? MarketName { get; set; }
        public string? MarketLine { get; set; }
        public LSport_EventPropDto? Props { get; set; }
    }


    public class LSport_LocationDto
    {
        public int LocationID { get; set; }
        public string? LocationName { get; set; }
    }

    public class LSport_SportsDto
    {
        public int SportID { get; set; }
        public string? SportName { get; set; }
    }

    public class LSport_Leagues
    {
        public int LeagueID { get; set; }
        public int SportID { get; set; }
        public string? LeagueName { get; set; }
        public int LocationID { get; set; }
    }

    //*********  methods to show scores ***********

    public class LSport_ScreenSportsDto
    {
        public bool ShowSport { get; set; }
        public string SportName { get; set; }
        public int SportId { get; set; }
        //  public List<LSport_ScreenLocationDto>? Locations { get; set; }
        public List<LSport_ScreenLeagueDto>? Leagues { get; set; }
    }

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
        public List<LSportGameDto>? Games { get; set; }
    }

    public class LSport_EventValuesDto
    {
        public Int32? FixtureId { get; set; }
        public int MarketID { get; set; }
        public decimal? VisitorSpread { get; set; }
        public decimal? VisitorSpreadOdds { get; set; }
        public decimal? HomeSpread { get; set; }
        public decimal? HomeSpreadOdds { get; set; }
        public decimal? HomeML { get; set; }
        public decimal? VisitorML { get; set; }
        public decimal? Total { get; set; }
        public decimal? TotalOver { get; set; }
        public decimal? TotalUnder { get; set; }
        public bool ShowInScreen { get; set; }
        public string? VisitorTeam { get; set; }
        public string? HomeTeam { get; set; }
        public string? VisitorSpreadStr { get; set; }
        public string? VisitorMLStr { get; set; }
        public string? VisitorTotalStr { get; set; }
        public string? HomeSpreadStr { get; set; }
        public string? HomeMLStr { get; set; }
        public string? HomeTotalStr { get; set; }
        public string? VisitorSpreadCss { get; set; }
        public string? HomeSpreadCss { get; set; }
        public string? VisitorTotalCss { get; set; }
        public string? HomeTotalCss { get; set; }
        public string? VisitorMLCss { get; set; }
        public string? HomeMLCss { get; set; }

        public bool VSpreadSelected { get; set; }
        public bool VTotalSelected { get; set; }
        public bool VMLSelected { get; set; }
        public bool HSpreadSelected { get; set; }
        public bool HTotalSelected { get; set; }
        public bool HMLSelected { get; set; }
    }

    public class LSport_EventPropMarketDto
    {
        public int MarketID { get; set; }
        public string MarketName { get; set; }
        public string? MainLine { get; set; }
        public bool? IsMain { get; set; }
        public bool? IsGameProp { get; set; }
        public bool? IsPlayerProp { get; set; }
        public bool? IsTnt { get; set; }
        public bool? AllowMarketParlay { get; set; }
        public List<LSport_EventPropDto> Props { get; set; }
    }

    public class LSport_EventPropDto
    {
        public string? IdL1 { get; set; }
        public string? IdL2 { get; set; }
        public Int32 FixtureId { get; set; }
        public int MarketId { get; set; }
        public string? Line1 { get; set; }
        public decimal? Line2 { get; set; }
        public decimal? Line3 { get; set; }
        public int? Odds1 { get; set; }
        public int? Odds2 { get; set; }
        public int? Odds3 { get; set; }
        // public DateTime LastUpdateDateTime { get; set; }
        public string? MarketName { get; set; }

        public decimal? Price { get; set; }

        public string? Name { get; set; }
        public string? OriginalName { get; set; }
        public string? BaseLine { get; set; }
        public string? MarketValue { get; set; }
        public string? CssStyle1 { get; set; }
        public string? CssStyle2 { get; set; }
        public string? CssStyle3 { get; set; }
        public bool IsSelected { get; set; }

        public decimal? BsWinAmount { get; set; }
        public decimal? BsRiskAmount { get; set; }
        public int? BsBetResult { get; set; }
        public string? BsTicketNumber { get; set; }
        public string? BsMessage { get; set; }
        public int StatusForWager { get; set; }
    }

    public class LSport_BetSlipObj
    {
        //public int fixtureId { get; set; }
        //public Int64 parlayRisk { get; set; }
        //public Int64 parlayWin { get; set; }
        public int IdPlayer { get; set; }
        //public string parlayTicket { get; set; }
        //public string parlayTicketDesc { get; set; }
        public bool AcceptLineChange { get; set; }
        //public List<LSport_BetSlipSelectionObj> selections { get; set; }

        public List<LSport_BetGame> Events { get; set; }
        public decimal ParlayWinAmount { get; set; }
        public decimal ParlayRiskAmount { get; set; }
        public int ParlayBetResult { get; set; }
        public string ParlayBetTicket { get; set; }
        public string ParlayMessage { get; set; }
        public bool? IsMobile { get; set; }
    }

    public class LSport_BetGame
    {
        public int FixtureId { get; set; }
        public string VisitorTeam { get; set; }
        public string HomeTeam { get; set; }
        public string SportName { get; set; }
        public List<LSport_EventPropDto> Selections { get; set; }
    }

    public class LSport_BetSlipSelectionObj
    {
        public int idplayer { get; set; }
        public int type { get; set; }
        //public int fixtureId { get; set; }
        //public int sportId { get; set; }
        //public int locationId { get; set; }
        //public int leagueId { get; set; }
        public Int64 risk { get; set; }
        public Int64 win { get; set; }
        // public int bsid { get; set; }
        public int result { get; set; }
        public string? ticketNumber { get; set; }
        public string? message { get; set; }
        public string ticketDesc { get; set; }
        public bool isValidForWager { get; set; }
        public int reasonInvalidForWager { get; set; }
        public LSport_EventPropDto? prop { get; set; }
        public LSport_EventPropDto? oldProp { get; set; }
    }

    public class LineChangedDto
    {
        public int LineType { get; set; } //1 line   2 prop
        public bool LineChanged { get; set; }
        public string? Message1 { get; set; }
        public string? Message2 { get; set; }
    }

    public class CompletePropMarket
    {
        public Int64 Id { get; set; }
        public int FixtureId { get; set; }
        public string? Name { get; set; }
        public string? Line { get; set; }
        public string? BaseLine { get; set; }
        public int LineStatus { get; set; }
        public string? PriceUS { get; set; }
        public int MarketId { get; set; }
        public string? MarketName { get; set; }
        public string? MainLine { get; set; }
        public string? Price { get; set; }
        public bool? IsMain { get; set; }
        public bool? IsGameProp { get; set; }
        public bool? IsPlayerProp { get; set; }
        public bool? IsTnt { get; set; }
        public bool? AllowMarketParlay { get; }
    }

    public class DgsCredentials
    {
        public string LoginName { get; set; }
        public string Password { get; set; }
    }

    public class DgsUserInfo
    {
        public int IdUser { get; set; }
        public string Name { get; set; }
    }

    public class MoverWagerHeaderDto
    {
        public int IdLiveWager { get; set; }
        public int IdPlayer { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public int IdWagerType { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
        public string Description { get; set; }
        public int Result { get; set; }
        public bool Graded { get; set; }
        public int NumDetails { get; set; }
        public int DgsIdWager { get; set; }
        public string Player { get; set; }
        public string Agent { get; set; }
        public List<MoverWagerDetailDto> Details { get; set; }
    }

    public class MoverWagerDetailDto
    {
        public int IdLiveWagerDetail { get; set; }
        public int IdLiveWager { get; set; }
        public int FixtureId { get; set; }
        public int MarketId { get; set; }
        public string LineId { get; set; }
        public string BaseLine { get; set; }
        public string Line { get; set; }
        public int Odds { get; set; }
        public decimal Price { get; set; }
        public string PickTeam { get; set; }
        public int Result { get; set; }
        public string CompleteDescription { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
    }

    /********************************************************************************************************************************************************************/
    /********************************************************************************************************************************************************************/
    /********************************************************************************************************************************************************************/

    public class FixtureDb
    {
        public int FixtureId { get; set; }
        public int SportId { get; set; }
        public int LocationId { get; set; }
        public int LeagueId { get; set; }
    }

    public class FixtureParticipanTable
    {
        public int FixtureId { get; set; }
        public int ParticipantId { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public int Rot { get; set; }
    }

    public class FixtureApiDto
    {
        public Header Header { get; set; }
        public List<Body> Body { get; set; }
    }

    public class ReceiveRMQ
    {
        public Header Header { get; set; }
        public Body Body { get; set; }
    }

    public class Header
    {
        public int Type { get; set; }
        public int MsgId { get; set; }
        public string MsgGuid { get; set; }
        public long ServerTimestamp { get; set; }
        public string CreationDate { get; set; }
    }

    public class Body
    {
        public int FixtureId { get; set; }
        public Fixture Fixture { get; set; }
        public Livescore Livescore { get; set; }
        public List<Market> Markets { get; set; }
        public List<Event> Events { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int LocationId { get; set; }
        public int SportId { get; set; }
        public string Season { get; set; }
        public int Type { get; set; }
        public Competition Competition { get; set; }
        public List<Competition> Competitions { get; set; }
    }

    public class Fixture
    {
        public Sport Sport { get; set; }
        public Location Location { get; set; }
        public League League { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime OriginalDate { get; set; }
        public DateTime FilterDate
        {
            get
            {
                return new DateTime(StartDate.Year, StartDate.Month, StartDate.Day);
            }
        }
        public DateTime LastUpdate { get; set; }
        public int Status { get; set; }
        public List<Participant> Participants { get; set; }
        public List<FixtureExtraDatum> FixtureExtraData { get; set; }
    }

    public class Sport
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Participant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameToLower
        {
            get
            {
                if (Name.Contains("("))
                {
                    int index = Name.IndexOf("(");
                    var dataName = Name.Substring(0, index).Trim().ToLower();
                    return dataName;
                }
                else
                {
                    return Name.Trim().ToLower();
                }
            }
        }
        public string Position { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int IsActive { get; set; }
        public List<ExtraDatum> ExtraData { get; set; }
    }

    public class ExtraDatum
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class FixtureExtraDatum
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Livescore
    {
        public Scoreboard Scoreboard { get; set; }
        public List<Period> Periods { get; set; }
        public List<LivescoreExtraDatum> LivescoreExtraData { get; set; }
        public List<Statistic> Statistics { get; set; }
    }

    public class Scoreboard
    {
        public int Status { get; set; }
        public int CurrentPeriod { get; set; }
        public string Time { get; set; }
        public List<Result> Results { get; set; }
        public List<int> Score { get; set; }
    }

    public class Result
    {
        public string Position { get; set; }
        public string Value { get; set; }
    }

    public class Period
    {
        public int Type { get; set; }
        public bool IsFinished { get; set; }
        public bool IsConfirmed { get; set; }
        public List<Result> Results { get; set; }
        public List<Incident> Incidents { get; set; }
        public object SubPeriods { get; set; }
        public int SequenceNumber { get; set; }
        public List<int> Score { get; set; }
    }


    public class Incident
    {
        public int Period { get; set; }
        public int IncidentType { get; set; }
        public int Seconds { get; set; }
        public string ParticipantPosition { get; set; }
        public List<Result> Results { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public List<int> Score { get; set; }
    }

    public class LivescoreExtraDatum
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Statistic
    {
        public int Type { get; set; }
        public List<Result> Results { get; set; }
        public List<Incident> Incidents { get; set; }
    }

    public class Market
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Provider> Providers { get; set; }
        public string MainLine { get; set; }
        public List<Bet> Bets { get; set; }
    }

    public class Provider
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<Bet> Bets { get; set; }
        public List<string> Bet { get; set; }
    }

    public class Bet
    {
        public object Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public string StartPrice { get; set; }
        public string Price { get; set; }
        public string PriceIN { get; set; }
        public string PriceUS { get; set; }
        public int PriceUSInt
        {
            get
            {

                _ = int.TryParse(PriceUS, out int res);
                return res;
            }
        }
        public string PriceUK { get; set; }
        public string PriceMA { get; set; }
        public string PriceHK { get; set; }
        public int Settlement { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Line { get; set; }
        public string BaseLine { get; set; }
        public long ParticipantId { set; get; }
        public string ProviderBetId { get; set; }
    }

    public class OutrightLeague
    {
        public Sport Sport { get; set; }
        public Location Location { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Status { get; set; }
        public List<ExtraDatum> ExtraData { get; set; }
    }

    public class OutrightFixture
    {
        public Sport Sport { get; set; }
        public Location Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Status { get; set; }
        public List<Participant> Participants { get; set; }
        public List<ExtraDatum> ExtraData { get; set; }
    }

    //public class ExtraDatum
    //{
    //    public string Name { get; set; }
    //    public string Value { get; set; }
    //}

    public class Competition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public List<Event> Events { get; set; }
    }

    //public class Competition
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public int Type { get; set; }
    //    public List<Event> Events { get; set; }
    //}

    public class Event
    {
        public int FixtureId { get; set; }
        public Fixture Fixture { get; set; }
        public Livescore Livescore { get; set; }
        public List<Market> Markets { get; set; }
        public OutrightLeague OutrightLeague { get; set; }
        public OutrightFixture OutrightFixture { get; set; }
    }

    public class BetCheck : Bet
    {
        public int FixtureId { get; set; }
    }

    public class CheckListLines
    {
        public int FixtureId { get; set; }
        public int MarketId { get; set; }
        public long BetId { get; set; }
        public Bet? BetInfo { get; set; }

    }//end namespace
}
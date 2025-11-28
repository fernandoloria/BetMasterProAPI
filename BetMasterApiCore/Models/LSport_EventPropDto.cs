namespace BetMasterApiCore.Models
{

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
}

namespace BetMasterApiCore.Models
{

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
}

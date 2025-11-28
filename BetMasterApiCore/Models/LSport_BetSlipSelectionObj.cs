namespace BetMasterApiCore.Models
{

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
}

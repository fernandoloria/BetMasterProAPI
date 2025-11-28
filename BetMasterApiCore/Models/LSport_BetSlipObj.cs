namespace BetMasterApiCore.Models
{

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
}

namespace BetMasterApiCore.Models
{

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
        public string? Explanation { get; set; }
        public List<LSport_EventPropDto> Props { get; set; }
    }
}

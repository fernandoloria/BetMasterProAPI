namespace BetMasterApiCore.Models
{

    public class LSport_ItemMainLine
    {
        public int MarketId { get; set; }
        public string? MarketName { get; set; }
        public string? MarketLine { get; set; }
        public LSport_EventPropDto? Props { get; set; }
    }
}

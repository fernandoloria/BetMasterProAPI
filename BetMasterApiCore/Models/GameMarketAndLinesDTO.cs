namespace BetMasterApiCore.Models
{


    //RLM: aplanar el juego con los markets y lineas, extends LSportsGameDTO       
    public class GameMarketAndLinesDTO: LSportGameDto
    {
        public Int64 Id { get; set; } //lineId
        public new int FixtureId { get; set; }
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
        public string? Explanation { get; set; }
    }
}

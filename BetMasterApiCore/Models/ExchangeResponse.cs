namespace BetMasterApiCore.Models
{
    public class ExchangeResponse
    {
        public string Jwt { get; set; }
        public int IdPlayer { get; set; }
        public int IdCall { get; set; }
        public SiteInfo Site { get; set; }
    }
}

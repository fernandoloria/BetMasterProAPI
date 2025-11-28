namespace BetMasterApiCore.Models
{
    public class PlayerInfoDto
    {
        public string Player { get; set; }
        public int IdPlayer { get; set; }
        public int WagerType { get; set; }
        public List<string> Leagues { get; set; }
        public int IdWagerType { get; set; }
        public int IdBook { get; set; }
        public int IdProfile { get; set; }
        public int IdProfileLimits { get; set; }
        public int IdLanguage { get; set; }
        public string NhlLine { get; set; }
        public string MblLine { get; set; }
        public int IdLineType { get; set; }
        public string LineStyle { get; set; }
        public float UTC { get; set; }
        public int IdTimeZone { get; set; }
        public string TimeZoneDesc { get; set; }
        public int IdAgent { get; set; }
        public Decimal CurrentBalance { get; set; }
        public Decimal AmountAtRisk { get; set; }
        public Decimal Available { get; set; }
        public int IdCurrency { get; set; }
        public string Currency { get; set; }
        public string CurrencyDesc { get; set; }
        public int PitcherDefault { get; set; }
        public int GMT { get; set; }
        public bool Access { get; set; }
        //  public string Password { get; set; }
        public int SecondsDelay { get; set; }
    }
}

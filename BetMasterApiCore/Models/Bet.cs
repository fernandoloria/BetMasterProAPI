namespace BetMasterApiCore.Models
{

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
        public object AlternateId { get; set; }
    }
}
